#!/usr/bin/env python3
"""Prueba local visible de una conversación de apoyo emocional."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from servidor_simulado import (
    infer_emotion,
    active_prompt_name,
    load_prompt,
    sanitize_preferred_name,
    select_prompt_key,
)


def introduction_reply() -> str:
    return (
        "Hola, soy EmpathIA, una IA de apoyo emocional. Estoy aquí para escucharte "
        "con respeto y sin juzgarte. ¿Cómo te gustaría que te llamara?"
    )


def humanized_reply(name: str, emotion: str, history: list[dict]) -> str:
    greeting = f"{name}, " if name else ""
    substantive_history = [
        item for item in history
        if item.get("speaker") == "usuario"
        and not str(item.get("text", "")).startswith("Me gustaría que me llamaras")
    ]
    if substantive_history:
        if emotion == "sadness":
            return (
                f"{greeting}entiendo que te duela sentir que puedes decepcionar a tu familia. "
                "Lo que sientes merece ser escuchado; ¿qué te gustaría que ellos comprendieran "
                "de lo que estás viviendo?"
            )
        return (
            f"{greeting}gracias por volver sobre esto. Noto que sigues intentando "
            "entender lo que estás sintiendo. ¿Qué necesitas ahora?"
        )
    if emotion == "anxiety":
        return (
            f"{greeting}suena a que tienes muchas cosas dando vueltas. "
            "Podemos ir paso a paso; ¿qué parte te preocupa más ahora?"
        )
    return f"{greeting}gracias por contármelo. ¿Qué es lo que más te está pesando ahora?"


def main() -> int:
    requested_name = "Andrea"
    name = sanitize_preferred_name(requested_name)
    messages = [
        "Me siento muy ansiosa por los exámenes y no sé por dónde empezar.",
        "Me siento triste porque temo decepcionar a mi familia.",
    ]
    history: list[dict] = [
        {"speaker": "ia", "text": introduction_reply()},
        {"speaker": "usuario", "text": f"Me gustaría que me llamaras {requested_name}."},
    ]

    print("=== PRUEBA ESTATICA: APOYO EMOCIONAL ===", flush=True)
    print("\nINICIO", flush=True)
    print(f"IA: {introduction_reply()}", flush=True)
    print(f"Usuario: Me gustaría que me llamaras {requested_name}.", flush=True)
    print(f"Sistema: nombre validado = {name}", flush=True)
    if not name:
        print("ERROR: el nombre preferido no fue validado", flush=True)
        return 1

    for number, message in enumerate(messages, start=1):
        emotion, confidence = infer_emotion(message)
        prompt_key = select_prompt_key(emotion)
        prompt_id = active_prompt_name(prompt_key)
        prompt, _ = load_prompt(
            prompt_id,
            message,
            name,
            history,
        )
        response = humanized_reply(name, emotion, history)
        print(f"\nTURNO {number}", flush=True)
        print(f"Usuario: {message}", flush=True)
        print(f"C procesa: emoción={emotion}, confianza={confidence:.2f}", flush=True)
        print(f"C selecciona prompt: {prompt_id}", flush=True)
        print(f"IA: {response}", flush=True)
        if (
            message not in prompt
            or name not in prompt
            or "Historial breve" not in prompt
            or not response.endswith("?")
        ):
            print("ERROR: el prompt no contiene el contexto esperado", flush=True)
            return 1
        history.extend([
            {"speaker": "usuario", "text": message},
            {"speaker": "ia", "text": response},
        ])

    print("\nRESULTADO: PRUEBA OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
