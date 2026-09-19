import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from prueba_conversacion import conversation_summary, humanized_reply, introduction_reply
from servidor_simulado import (
    build_contextual_reply,
    conversation_memory_path,
    load_conversation_memory,
    merge_conversation_history,
    prompt_history,
    purge_conversation_memory,
    save_conversation_memory,
    detect_risk_signals,
    extract_preferred_name,
    infer_emotion,
    sanitize_preferred_name,
)


class RespuestasConversacionTests(unittest.TestCase):
    def test_introduction_is_warm_and_invites_preferred_name(self):
        response = introduction_reply()
        self.assertIn("IA de apoyo emocional", response)
        self.assertIn("escucharte", response)
        self.assertIn("¿Cómo te gustaría que te llamara?", response)

    def test_anxiety_response_uses_name_and_one_open_question(self):
        name = sanitize_preferred_name("Andrea")
        message = "Me siento muy ansiosa por los exámenes."
        emotion, confidence = infer_emotion(message)
        response = humanized_reply(name, emotion, [])

        self.assertEqual(emotion, "anxiety")
        self.assertGreaterEqual(confidence, 0.72)
        self.assertTrue(response.startswith("Andrea,"))
        self.assertEqual(response.count("?"), 1)
        self.assertNotIn("diagnóstico", response.lower())

    def test_follow_up_response_recognizes_sadness_and_context(self):
        history = [
            {"speaker": "usuario", "text": "Me sentía ansiosa por los exámenes."},
            {"speaker": "ia", "text": "Podemos ir paso a paso."},
        ]
        message = "Me siento triste porque temo decepcionar a mi familia."
        emotion, _ = infer_emotion(message)
        response = humanized_reply("Andrea", emotion, history)

        self.assertEqual(emotion, "sadness")
        self.assertIn("Andrea", response)
        self.assertIn("familia", response)
        self.assertIn("¿", response)
        self.assertNotIn("no te preocupes", response.lower())

    def test_emergency_is_not_treated_as_only_an_emotion(self):
        message = "No quiero seguir viviendo y necesito ayuda."
        emotion, _ = infer_emotion(message)
        signals, risk_level = detect_risk_signals(message)

        self.assertEqual(emotion, "neutral")
        self.assertEqual(risk_level, "emergency")
        self.assertEqual(signals[0]["code"], "SAFETY_CONCERN")
        self.assertIn("no quiero seguir viviendo", signals[0]["evidence"])

    def test_summary_contains_every_user_and_ai_exchange(self):
        history = [
            {"speaker": "usuario", "text": "Estoy ansiosa."},
            {"speaker": "ia", "text": "Podemos ir paso a paso."},
            {"speaker": "usuario", "text": "Ahora estoy más tranquila."},
            {"speaker": "ia", "text": "Me alegra escucharlo."},
        ]
        summary = conversation_summary(history)

        self.assertIn("Turno 1", summary)
        self.assertIn("Turno 2", summary)
        self.assertIn("Estoy ansiosa", summary)
        self.assertIn("Me alegra escucharlo", summary)
        self.assertEqual(summary.count("Turno "), 2)

    def test_extracts_name_and_responds_to_message_content(self):
        self.assertEqual(extract_preferred_name("Hola, me llamo Steve"), "Steve")
        response = build_contextual_reply("Hola, me llamo Steve", "Steve", "neutral", "low", [])
        self.assertIn("Steve", response)

    def test_each_message_topic_gets_a_different_follow_up(self):
        history = [{"speaker": "usuario", "text": "Ya hablamos antes"}]
        family = build_contextual_reply(
            "Me preocupa decepcionar a mi familia",
            "Steve",
            "neutral",
            "low",
            history,
        )
        rest = build_contextual_reply(
            "No puedo descansar porque sigo pensando",
            "Steve",
            "neutral",
            "low",
            history,
        )
        self.assertNotEqual(family, rest)
        self.assertIn("familia", family)
        self.assertIn("descansar", rest)

    def test_conversation_memory_survives_between_turns(self):
        session_id = "test-memory-session"
        purge_conversation_memory(session_id)
        save_conversation_memory(session_id, [{"speaker": "usuario", "text": "Me llamo Steve"}])
        self.assertEqual(load_conversation_memory(session_id)[0]["text"], "Me llamo Steve")
        self.assertTrue(purge_conversation_memory(session_id))
        self.assertFalse(conversation_memory_path(session_id).exists())

    def test_prompt_history_limit_does_not_delete_stored_history(self):
        history = [
            {"speaker": speaker, "text": f"{speaker}-{index}"}
            for index in range(14)
            for speaker in ("usuario", "ia")
        ]
        self.assertEqual(len(prompt_history(history)), 24)
        self.assertEqual(len(history), 28)

    def test_turn_memory_merges_without_losing_previous_exchanges(self):
        stored = [
            {"speaker": "usuario", "text": "Hablamos del examen"},
            {"speaker": "ia", "text": "Recuerdo esa preocupacion"},
        ]
        request = [
            {"speaker": "usuario", "text": "Hablamos del examen"},
            {"speaker": "ia", "text": "Recuerdo esa preocupacion"},
            {"speaker": "usuario", "text": "Ahora quiero hablar de mi familia"},
        ]
        merged = merge_conversation_history(stored, request)

        self.assertEqual(len(merged), 3)
        self.assertEqual(merged[0]["text"], "Hablamos del examen")
        self.assertEqual(merged[-1]["text"], "Ahora quiero hablar de mi familia")

    def test_repeated_ai_replies_are_kept_as_distinct_turns(self):
        history = [
            {"speaker": "usuario", "text": "Primer mensaje"},
            {"speaker": "ia", "text": "¿Qué necesitas ahora?"},
            {"speaker": "usuario", "text": "Segundo mensaje"},
            {"speaker": "ia", "text": "¿Qué necesitas ahora?"},
        ]
        merged = merge_conversation_history([], history)
        self.assertEqual(len(merged), 4)
        self.assertEqual(merged.count({"speaker": "ia", "text": "¿Qué necesitas ahora?"}), 2)


if __name__ == "__main__":
    unittest.main()
