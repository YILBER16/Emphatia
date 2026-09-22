#!/usr/bin/env python3
"""EmpathIA Intelligence service (module C) — Phase 0.

Listens on 0.0.0.0:8100 (LAN). Override with INTEL_HOST / INTEL_PORT.
Implements internal InferTurn + health + memory stubs.
"""

from __future__ import annotations

import base64
import json
import os
import re
import shutil
import subprocess
import time
import unicodedata
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse

HOST = os.environ.get("INTEL_HOST", "0.0.0.0")
PORT = int(os.environ.get("INTEL_PORT", "8100"))
INTERNAL_TOKEN = os.environ.get("INTEL_INTERNAL_TOKEN", "empathia-internal-dev-token")
VERTEX_AI_ENABLED = os.environ.get("VERTEX_AI_ENABLED", "false").lower() in {"1", "true", "yes"}
VERTEX_AI_PROJECT = os.environ.get("VERTEX_AI_PROJECT", "")
VERTEX_AI_LOCATION = os.environ.get("VERTEX_AI_LOCATION", "us-central1")
VERTEX_AI_MODEL = os.environ.get("VERTEX_AI_MODEL", "gemini-2.5-flash")
GOOGLE_API_KEY = os.environ.get("GOOGLE_API_KEY", "")
REPO_ROOT = Path(__file__).resolve().parents[1]
DATA_ROOT = Path(os.environ.get("EMPATHIA_DATA_ROOT", str(REPO_ROOT / "datos")))
CONVERSATION_MEMORY_ROOT = DATA_ROOT / "inteligencia" / "memory" / "conversations"
MEMORY_PROMPT_TURNS = 12
PROMPTS_ROOT = Path(__file__).resolve().parent / "prompts"
PROMPTS_REGISTRY = PROMPTS_ROOT / "registry.json"
FIXTURE_EXPRESSION = REPO_ROOT / "expresion" / "fixtures" / "paquete-expresion-ejemplo.json"
SILENT_WAV = Path(__file__).resolve().parent / "fixtures" / "silent.wav"
STUB_TRANSCRIPT_TEXT = "Hola, hoy me siento un poco cansado pero quiero hablar."
_WHISPER_MODEL = None
_WHISPER_MODEL_CONFIG = None


def json_response(handler: BaseHTTPRequestHandler, status: int, payload: dict) -> None:
    body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    handler.send_response(status)
    handler.send_header("Content-Type", "application/json; charset=utf-8")
    handler.send_header("Content-Length", str(len(body)))
    handler.end_headers()
    handler.wfile.write(body)


def read_json(handler: BaseHTTPRequestHandler) -> dict:
    length = int(handler.headers.get("Content-Length", "0"))
    raw = handler.rfile.read(length) if length else b"{}"
    try:
        decoded = raw.decode("utf-8")
    except UnicodeDecodeError:
        decoded = raw.decode("utf-8", errors="replace")
        print("[C] JSON recibido con codificación inválida; se sustituyeron caracteres", flush=True)
    try:
        payload = json.loads(decoded or "{}")
    except json.JSONDecodeError as error:
        print(f"[C] JSON inválido: {error}", flush=True)
        return {}
    return payload if isinstance(payload, dict) else {}


def authorized(handler: BaseHTTPRequestHandler) -> bool:
    return handler.headers.get("X-Internal-Token") == INTERNAL_TOKEN


def vertex_health() -> dict:
    missing = []
    authentication = "api_key" if GOOGLE_API_KEY else "application_default_credentials"
    if not GOOGLE_API_KEY:
        if not VERTEX_AI_PROJECT:
            missing.append("VERTEX_AI_PROJECT")
        if not os.environ.get("GOOGLE_APPLICATION_CREDENTIALS"):
            missing.append("GOOGLE_APPLICATION_CREDENTIALS or GOOGLE_API_KEY")

    return {
        "enabled": VERTEX_AI_ENABLED,
        "configured": VERTEX_AI_ENABLED and not missing,
        "authentication": authentication,
        "project": VERTEX_AI_PROJECT or None,
        "location": VERTEX_AI_LOCATION,
        "model": VERTEX_AI_MODEL,
        "missing": missing,
    }


def sanitize_preferred_name(value: object) -> str:
    if not isinstance(value, str):
        return ""

    name = " ".join(value.strip().split())
    if not name or len(name) > 40 or len(name.split()) > 2:
        return ""
    if not re.fullmatch(r"[A-Za-zÁÉÍÓÚÜÑáéíóúüñ'\- ]+", name):
        return ""
    return name


def extract_preferred_name(student_text: str) -> str:
    match = re.search(
        r"\b(?:me llamo|quiero que me llames|llámame|llamame)\s+([A-Za-zÁÉÍÓÚÜÑáéíóúüñ'\- ]{1,40})",
        student_text,
        re.IGNORECASE,
    )
    if not match:
        return ""
    return sanitize_preferred_name(match.group(1).strip(" .,!?:;"))


def preferred_name_from_history(history: list[dict]) -> str:
    for item in reversed(history):
        if item.get("speaker") != "usuario":
            continue
        name = extract_preferred_name(str(item.get("text", "")))
        if name:
            return name
    return ""


def last_user_message(history: list[dict]) -> str:
    for item in reversed(history):
        if item.get("speaker") == "usuario" and item.get("text"):
            return str(item["text"])
    return ""


def conversation_bridge(history: list[dict]) -> str:
    previous = last_user_message(history)
    if not previous:
        return ""
    return f"Antes me contaste: «{previous}»."


def clean_message_excerpt(message: str, limit: int = 180) -> str:
    compact = " ".join(message.strip().split())
    if len(compact) <= limit:
        return compact
    return compact[: limit - 3].rstrip() + "..."


def conversation_memory_path(session_id: object) -> Path:
    safe_session_id = re.sub(r"[^A-Za-z0-9_-]", "_", str(session_id or "unknown"))
    return CONVERSATION_MEMORY_ROOT / f"{safe_session_id}.json"


def load_conversation_memory(session_id: object) -> list[dict]:
    path = conversation_memory_path(session_id)
    if not path.exists():
        return []
    try:
        content = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return []
    return content if isinstance(content, list) else []


def save_conversation_memory(session_id: object, history: list[dict]) -> bool:
    path = conversation_memory_path(session_id)
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        temporary_path = path.with_suffix(".tmp")
        temporary_path.write_text(
            json.dumps(annotate_memory_history(history), ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        temporary_path.replace(path)
        return True
    except OSError as error:
        print(f"[C] memoria error session={session_id}: {error}", flush=True)
        return False


def prompt_history(history: list[dict]) -> list[dict]:
    return history[-(MEMORY_PROMPT_TURNS * 2):]


def annotate_memory_history(history: list[dict]) -> list[dict]:
    annotated = []
    turn_number = 0
    for item in history:
        if not isinstance(item, dict) or not item.get("text"):
            continue
        speaker = item.get("speaker", "usuario")
        if speaker == "usuario":
            turn_number += 1
        annotated.append({
            "turn": item.get("turn", turn_number),
            "speaker": speaker,
            "text": str(item["text"]),
        })
    return annotated


def remembered_context(history: list[dict]) -> str:
    """Return a compact reminder from older turns for long conversations."""
    user_messages = [
        str(item["text"])
        for item in history
        if item.get("speaker") == "usuario" and item.get("text")
    ]
    if len(user_messages) <= MEMORY_PROMPT_TURNS:
        return ""
    return " | ".join(user_messages[:-MEMORY_PROMPT_TURNS][-3:])


def merge_conversation_history(stored_history: list[dict], request_history: list[dict]) -> list[dict]:
    """Keep C's full session memory while accepting newer context from B."""
    stored = [
        {"turn": item.get("turn"), "speaker": item.get("speaker", "usuario"), "text": str(item["text"])}
        for item in stored_history
        if isinstance(item, dict) and item.get("text")
    ]
    requested = [
        {"turn": item.get("turn"), "speaker": item.get("speaker", "usuario"), "text": str(item["text"])}
        for item in request_history
        if isinstance(item, dict) and item.get("text")
    ]
    if not stored:
        return requested
    if not requested:
        return stored
    if requested[:len(stored)] == stored:
        return requested
    if stored[:len(requested)] == requested:
        return stored

    overlap = 0
    max_overlap = min(len(stored), len(requested))
    for size in range(max_overlap, 0, -1):
        if stored[-size:] == requested[:size]:
            overlap = size
            break
    return stored + requested[overlap:]


def purge_conversation_memory(session_id: object) -> bool:
    path = conversation_memory_path(session_id)
    try:
        if not path.exists():
            return False
        path.unlink()
        return True
    except OSError as error:
        print(f"[C] memoria purge error session={session_id}: {error}", flush=True)
        return False


def normalize_emotion_label(value: str) -> str:
    aliases = {
        "alegria": "joy",
        "joy": "joy",
        "neutral": "neutral",
        "tristeza": "sadness",
        "sadness": "sadness",
        "enojo": "anger",
        "ira": "anger",
        "anger": "anger",
        "miedo": "fear",
        "fear": "fear",
        "ansiedad": "anxiety",
        "anxiety": "anxiety",
    }
    return aliases.get(value.strip().lower(), "")


def infer_emotion(student_text: str) -> tuple[str, float]:
    """Classify the predominant emotion using normalized phrase signals."""
    text = " ".join(
        unicodedata.normalize("NFKD", student_text.lower())
        .encode("ascii", "ignore")
        .decode("ascii")
        .split()
    )
    emotion_signals = {
        "anxiety": (
            "ansioso", "ansiosa", "ansiedad", "preocupado", "preocupada",
            "nervioso", "nerviosa", "estresado", "estresada", "estres",
            "no puedo dejar de pensar", "me desborda",
        ),
        "sadness": (
            "triste", "llorar", "lloro", "deprimido", "deprimida", "sin ganas",
            "depresion", "depresin", "desmotivado", "desmotivada", "no quiero salir",
            "no quiero hacer nada", "me siento vacio", "me siento vacia", "me duele mucho",
        ),
        "anger": (
            "enojado", "enojada", "enojo", "ira", "rabia", "furioso", "furiosa",
            "me da mucha bronca",
        ),
        "fear": (
            "miedo", "asustado", "asustada", "temor", "panico", "me da miedo",
            "tengo terror",
        ),
        "joy": (
            "feliz", "contento", "contenta", "alegre", "emocionado", "emocionada",
            "me hace ilusion", "orgulloso", "orgullosa",
        ),
    }
    scores = {
        label: sum(1 for signal in signals if signal in text)
        for label, signals in emotion_signals.items()
    }
    label, score = max(scores.items(), key=lambda item: item[1])
    if score == 0:
        return "neutral", 0.55
    return label, min(0.95, 0.72 + (score * 0.1))


def detect_risk_signals(student_text: str) -> tuple[list[dict], str]:
    """Detect safety signals independently from the emotional state."""
    text = " ".join(
        unicodedata.normalize("NFKD", student_text.lower())
        .encode("ascii", "ignore")
        .decode("ascii")
        .split()
    )
    emergency_terms = (
        "quiero hacerme dano",
        "quiero suicidarme",
        "me voy a suicidar",
        "no quiero seguir viviendo",
    )
    high_terms = (
        "me hice dano",
        "me estoy haciendo dano",
        "tengo un plan para hacerme dano",
        "estoy en peligro",
    )
    medium_terms = (
        "no puedo mas",
        "no encuentro salida",
        "me siento atrapado",
        "me siento atrapada",
        "no quiero salir",
        "no quiero hacer nada",
        "estoy desmotivado",
        "estoy desmotivada",
    )
    for severity, terms in (("high", high_terms), ("medium", medium_terms), ("emergency", emergency_terms)):
        for term in terms:
            if term in text:
                return [
                    {
                        "code": "SAFETY_CONCERN",
                        "severity": severity,
                        "evidence": term,
                        "confidence": 0.9 if severity == "emergency" else 0.8,
                    }
                ], severity
    return [], "low"


def build_contextual_reply(
    student_text: str,
    preferred_name: str,
    emotion_label: str,
    risk_level: str,
    conversation_history: list[dict],
) -> str:
    """Build a response around the user's actual message and conversation state."""
    text = " ".join(
        unicodedata.normalize("NFKD", student_text.lower())
        .encode("ascii", "ignore")
        .decode("ascii")
        .split()
    )
    greeting = f"{preferred_name}, " if preferred_name else ""
    has_history = any(item.get("speaker") == "usuario" for item in conversation_history)

    if risk_level == "emergency":
        return (
            f"{greeting}lamento que estés pasando por esto. Tu seguridad importa mucho: busca ahora mismo "
            "a un adulto de confianza y contacta a los servicios de emergencia de tu localidad. "
            "¿Hay alguien contigo que pueda acompañarte en este momento?"
        )
    if risk_level == "high":
        return (
            f"{greeting}gracias por confiarme algo tan importante. No tienes que afrontar esto a solas; "
            "busquemos a una persona adulta que pueda acompañarte hoy. ¿A quién podrías avisarle ahora?"
        )
    if extract_preferred_name(student_text):
        return (
            f"Mucho gusto, {preferred_name or 'gracias por decírmelo'}. Quiero conocerte a tu ritmo y "
            "escuchar lo que hoy te resulte más importante. ¿Qué te gustaría contarme primero?"
        )
    if "familia" in text or "decepcionar" in text or "papa" in text or "mama" in text:
        return (
            f"{greeting}entiendo que esto te toque tan de cerca; cuando pensamos en la familia, la presión "
            "puede sentirse muy pesada. No tienes que demostrarme nada ni explicarlo perfectamente. "
            "¿Qué te gustaría que ellos entendieran de lo que estás viviendo?"
        )
    if "descans" in text or "dormir" in text or "pensando" in text or "no puedo parar" in text:
        return (
            f"{greeting}suena agotador intentar descansar mientras las preocupaciones siguen dando vueltas. "
            "Podemos separar lo urgente de lo que puede esperar y tomar una cosa a la vez. "
            "¿Qué pensamiento aparece con más fuerza cuando intentas dormir?"
        )
    if "organizar" in text or "pendiente" in text or "estudiar" in text or "examen" in text:
        return (
            f"{greeting}veo que estás intentando encontrar una forma concreta de recuperar un poco de control. "
            "Podemos convertirlo en un paso pequeño y realista, sin exigirte resolverlo todo hoy. "
            "¿Qué tarea te daría más alivio si la dejaras encaminada primero?"
        )
    if "tranquil" in text or "mejor" in text or "gracias" in text:
        return (
            f"{greeting}me alegra saber que notas aunque sea un poco de alivio, y gracias por contármelo. "
            "Podemos quedarnos con lo que te ayudó y pensar cómo repetirlo cuando vuelva la preocupación. "
            "¿Qué cambió dentro de ti o a tu alrededor para sentirte así?"
        )
    if emotion_label == "sadness" and has_history and risk_level == "medium":
        bridge = conversation_bridge(conversation_history)
        current = clean_message_excerpt(student_text)
        return (
            f"{greeting}te estoy escuchando. {bridge} Ahora me dices «{current}». "
            "Suena a que la ruptura no solo te puso triste, sino que también te está quitando energía "
            "para salir y hacer cosas. No quiero asumir cómo te encuentras: ¿estás a salvo ahora mismo "
            "y has pensado en hacerte daño?"
        )
    if emotion_label == "sadness" and has_history:
        bridge = conversation_bridge(conversation_history)
        current = clean_message_excerpt(student_text)
        return (
            f"{greeting}te estoy escuchando. {bridge} Ahora también me dices «{current}». "
            "Una infidelidad y el final de una relación pueden dejar mucha tristeza, "
            "aislamiento y preguntas difíciles; no tienes que resolverlo todo de una vez. "
            "¿Qué te está pesando más ahora: la traición, la soledad o no saber cómo volver a empezar?"
        )
    if emotion_label == "sadness":
        return (
            f"{greeting}puedo notar que esto te está doliendo, y tiene sentido que necesites espacio para "
            "decirlo sin que te apuren. Estoy aquí para escucharte y entender el motivo, no para juzgarte. "
            "¿Qué parte de todo esto pesa más ahora mismo?"
        )
    if emotion_label == "anxiety":
        return (
            f"{greeting}te escucho; parece que estás intentando manejar demasiadas preocupaciones a la vez. "
            "No hace falta resolverlas todas en este momento: podemos mirar primero la que más aprieta. "
            "¿Qué es lo que más te preocupa ahora?"
        )
    if emotion_label == "joy":
        return (
            f"{greeting}me alegra escuchar esa parte positiva de lo que cuentas. Quiero entender qué significa "
            "para ti y acompañarte también en los momentos que te hacen bien. ¿Qué fue lo mejor de hoy?"
        )
    if has_history:
        bridge = conversation_bridge(conversation_history)
        current = clean_message_excerpt(student_text)
        return (
            f"{greeting}te estoy siguiendo. {bridge} Ahora me dices «{current}». "
            "Quiero entender qué cambió y qué relación tiene con lo que veníamos hablando, sin asumir por ti. "
            "¿Qué parte de lo que acabas de contar te gustaría explorar primero?"
        )
    current = clean_message_excerpt(student_text)
    return (
        f"{greeting}gracias por confiarme esto. Escucho que dices «{current}» y quiero comprender qué significa "
        "para ti, no responderte con una frase automática. ¿Qué fue lo más importante de ese momento?"
    )


def load_prompt(
    prompt_id: str,
    student_text: str,
    preferred_name: str = "",
    conversation_history: list[dict] | None = None,
) -> tuple[str, str]:
    registry = json.loads(PROMPTS_REGISTRY.read_text(encoding="utf-8"))
    prompt_definition = registry["prompts"][prompt_id]
    prompt_path = PROMPTS_ROOT / prompt_definition["path"]
    prompt = prompt_path.read_text(encoding="utf-8")
    prompt = prompt.replace("{{student_text}}", student_text)
    if conversation_history:
        history_lines = [
            f"{item.get('speaker', 'usuario')}: {item.get('text', '').strip()}"
            for item in conversation_history
            if isinstance(item, dict) and item.get("text")
        ]
        if history_lines:
            prompt += "\n\nHistorial breve de esta conversación:\n" + "\n".join(history_lines)
    if preferred_name:
        prompt += (
            "\n\nNombre preferido del estudiante: "
            f"{preferred_name}\n"
            "Usa este nombre solo cuando sea natural. No lo repitas en cada respuesta "
            "y no inventes apodos.\n"
        )
    return prompt, prompt_id


def active_prompt_name(prompt_key: str) -> str:
    registry = json.loads(PROMPTS_REGISTRY.read_text(encoding="utf-8"))
    return registry["active"].get(prompt_key, registry["active"]["general"])


def select_prompt_key(emotion_label: str = "", risk_level: str = "low") -> str:
    normalized_risk = risk_level.strip().lower()
    if normalized_risk in {"emergency", "emergencia", "urgent", "urgente"}:
        return "emergency"
    if normalized_risk in {"high", "critical", "immediate", "alto", "critico"}:
        return "risk_high"
    if normalized_risk in {"medium", "moderate", "medio", "moderado"}:
        return "risk_medium"

    emotion_to_prompt = {
        "sadness": "sadness",
        "tristeza": "sadness",
        "anxiety": "anxiety",
        "ansiedad": "anxiety",
        "fatigue": "fatigue",
        "cansancio": "fatigue",
        "frustration": "frustration",
        "frustracion": "frustration",
        "loneliness": "loneliness",
        "soledad": "loneliness",
        "fear": "fear",
        "miedo": "fear",
        "anger": "anger",
        "enojo": "anger",
        "ira": "anger",
        "guilt": "guilt",
        "culpa": "guilt",
        "shame": "shame",
        "vergüenza": "shame",
        "verguenza": "shame",
        "exam_pressure": "exam_pressure",
        "presion_examenes": "exam_pressure",
        "presion academica": "exam_pressure",
        "bullying": "bullying",
        "acoso": "bullying",
        "family_conflict": "family_conflict",
        "conflicto familiar": "family_conflict",
        "no_talk": "no_talk",
        "silencio": "no_talk",
        "no quiere hablar": "no_talk",
    }
    return emotion_to_prompt.get(emotion_label.strip().lower(), "general")


def generate_vertex_reply(
    student_text: str,
    emotion_label: str = "",
    risk_level: str = "low",
    preferred_name: str = "",
    conversation_history: list[dict] | None = None,
) -> tuple[str, str, str, int]:
    if not VERTEX_AI_ENABLED:
        raise RuntimeError("VERTEX_AI_DISABLED")

    preferred_name = sanitize_preferred_name(preferred_name)
    started = time.perf_counter()
    from google import genai

    if GOOGLE_API_KEY:
        client = genai.Client(api_key=GOOGLE_API_KEY)
        llm_version = f"gemini-api-key:{VERTEX_AI_MODEL}"
    else:
        if not VERTEX_AI_PROJECT:
            raise RuntimeError("VERTEX_AI_PROJECT is required when GOOGLE_API_KEY is not set")
        client = genai.Client(
            vertexai=True,
            project=VERTEX_AI_PROJECT,
            location=VERTEX_AI_LOCATION,
        )
        llm_version = f"vertex:{VERTEX_AI_MODEL}"
    prompt_key = select_prompt_key(emotion_label, risk_level)
    prompt_name = active_prompt_name(prompt_key)
    prompt, prompt_name = load_prompt(prompt_name, student_text, preferred_name, conversation_history)
    response = client.models.generate_content(
        model=VERTEX_AI_MODEL,
        contents=prompt,
    )
    reply_text = (response.text or "").strip()
    if not reply_text:
        raise RuntimeError("VERTEX_EMPTY_RESPONSE")

    elapsed_ms = max(1, int((time.perf_counter() - started) * 1000))
    return reply_text, llm_version, prompt_name, elapsed_ms


def _get_whisper_model():
    global _WHISPER_MODEL, _WHISPER_MODEL_CONFIG

    model_name = os.environ.get("INTEL_WHISPER_MODEL", "small")
    device = os.environ.get("INTEL_WHISPER_DEVICE", "cpu")
    compute_type = os.environ.get("INTEL_WHISPER_COMPUTE_TYPE", "int8")
    config = (model_name, device, compute_type)

    if _WHISPER_MODEL is not None and _WHISPER_MODEL_CONFIG == config:
        return _WHISPER_MODEL, model_name

    from faster_whisper import WhisperModel

    _WHISPER_MODEL = WhisperModel(model_name, device=device, compute_type=compute_type)
    _WHISPER_MODEL_CONFIG = config
    return _WHISPER_MODEL, model_name


def infer_transcript(audio_path: str | None) -> tuple[dict, str, int]:
    started = time.perf_counter()

    fallback = {
        "text": STUB_TRANSCRIPT_TEXT,
        "confidence": 0.91,
    }
    if not audio_path:
        return fallback, "stub-whisper", 50

    audio_file = Path(audio_path)
    if not audio_file.exists():
        print(f"[intelligence-stub] audio not found for whisper: {audio_file}", flush=True)
        return fallback, "stub-whisper", 50

    try:
        model, model_name = _get_whisper_model()
        segments, info = model.transcribe(str(audio_file), language="es")
        text = " ".join(segment.text.strip() for segment in segments if segment.text).strip()
        if not text:
            text = STUB_TRANSCRIPT_TEXT

        elapsed_ms = max(1, int((time.perf_counter() - started) * 1000))
        confidence = float(getattr(info, "language_probability", 0.8))
        confidence = max(0.0, min(1.0, confidence))

        return {
            "text": text,
            "confidence": confidence,
        }, f"faster-whisper:{model_name}", elapsed_ms
    except Exception as exc:
        print(f"[intelligence-stub] whisper fallback: {exc}", flush=True)
        return fallback, "stub-whisper", 50


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt: str, *args) -> None:
        print(f"[intelligence-stub] {self.address_string()} {fmt % args}")

    def do_GET(self) -> None:  # noqa: N802
        path = urlparse(self.path).path
        if path == "/":
            json_response(
                self,
                200,
                {
                    "status": "ok",
                    "service": "intelligence-stub",
                    "message": "Stub activo. Usa GET /internal/v1/health para health.",
                    "endpoints": [
                        "/internal/v1/health",
                        "/internal/v1/vertex/health",
                        "/internal/v1/infer/turn",
                        "/internal/v1/audio/tts",
                        "/internal/v1/memory/prepare",
                        "/internal/v1/memory/purge",
                    ],
                },
            )
            return
        if path == "/internal/v1/health":
            json_response(
                self,
                200,
                {
                    "status": "ok",
                    "components": {
                        "whisper": "stub",
                        "ollama": "stub",
                        "tts": "stub",
                        "memory": "session-file",
                    },
                },
            )
            return
        if path == "/internal/v1/vertex/health":
            if not authorized(self):
                json_response(self, 401, {"error": {"code": "UNAUTHORIZED", "message": "Invalid internal token"}})
                return
            json_response(self, 200, {"status": "ok", "vertex": vertex_health()})
            return
        if path == "/internal/v1/audio/tts":
            if not authorized(self):
                json_response(self, 401, {"error": {"code": "UNAUTHORIZED", "message": "Invalid internal token"}})
                return
            query = parse_qs(urlparse(self.path).query)
            turn_id = (query.get("turn_id") or [""])[0].strip()
            wav = _find_tts_wav(turn_id) if turn_id else None
            if wav is None:
                json_response(self, 404, {"error": {"code": "NOT_FOUND", "message": "TTS audio missing"}})
                return
            data = wav.read_bytes()
            self.send_response(200)
            self.send_header("Content-Type", "audio/wav")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)
            return
        json_response(self, 404, {"error": {"code": "NOT_FOUND", "message": path}})

    def do_POST(self) -> None:  # noqa: N802
        path = urlparse(self.path).path
        if not authorized(self):
            json_response(self, 401, {"error": {"code": "UNAUTHORIZED", "message": "Invalid internal token"}})
            return

        if path == "/internal/v1/memory/prepare":
            body = read_json(self)
            session_id = body.get("session_id")
            if not session_id:
                json_response(self, 422, {"error": {"code": "SESSION_REQUIRED", "message": "session_id is required"}})
                return
            history = load_conversation_memory(session_id)
            path = conversation_memory_path(session_id)
            was_created = not path.exists()
            if was_created and not save_conversation_memory(session_id, history):
                json_response(self, 500, {"error": {"code": "MEMORY_UNAVAILABLE", "message": "Could not initialize memory"}})
                return
            print(f"[C] memoria preparada turnos={len(history) // 2} session={session_id}", flush=True)
            json_response(self, 200, {"ok": True, "memory": {"ready": True, "updated": was_created}})
            return

        if path == "/internal/v1/memory/purge":
            body = read_json(self)
            session_id = body.get("session_id")
            if not session_id:
                json_response(self, 422, {"error": {"code": "SESSION_REQUIRED", "message": "session_id is required"}})
                return
            purged = purge_conversation_memory(session_id)
            print(f"[C] memoria purgada session={session_id} purged={purged}", flush=True)
            json_response(self, 200, {"ok": True, "memory": {"purged": purged}, "session_id": session_id})
            return

        if path == "/internal/v1/infer/turn":
            body = read_json(self)
            turn_id = body.get("turn_id") or str(uuid.uuid4())
            request_id = body.get("request_id") or str(uuid.uuid4())
            audio_path = (body.get("audio") or {}).get("path") if isinstance(body.get("audio"), dict) else None
            student_text = body.get("text") if isinstance(body.get("text"), str) else ""
            student_text = student_text.strip()
            emotion_input = body.get("emotion") if isinstance(body.get("emotion"), dict) else {}
            emotion_label = (
                normalize_emotion_label(emotion_input.get("label"))
                if isinstance(emotion_input.get("label"), str)
                else ""
            )
            emotion_confidence = emotion_input.get("confidence") if isinstance(
                emotion_input.get("confidence"), (int, float)
            ) else None
            request_history = body.get("conversation_history")
            if not isinstance(request_history, list):
                request_history = []
            stored_history = load_conversation_memory(body.get("session_id"))
            full_history = merge_conversation_history(stored_history, request_history)
            conversation_history = prompt_history(full_history)
            older_context = remembered_context(full_history)
            if older_context:
                conversation_history = [
                    {"speaker": "memoria", "text": f"Temas anteriores recordados: {older_context}"},
                    *conversation_history,
                ]
            risk_level = body.get("risk_level") if isinstance(body.get("risk_level"), str) else "low"
            preferred_name = sanitize_preferred_name(body.get("preferred_name"))
            if not preferred_name and student_text:
                preferred_name = extract_preferred_name(student_text)
            if not preferred_name:
                preferred_name = preferred_name_from_history(full_history)
            if student_text and not emotion_label:
                emotion_label, emotion_confidence = infer_emotion(student_text)
            risk_signals, detected_risk_level = detect_risk_signals(student_text)
            if detected_risk_level != "low" and risk_level == "low":
                risk_level = detected_risk_level

            if student_text:
                print(f"[C] TEXTO de B session={body.get('session_id')} | {student_text}", flush=True)
                transcript, stt_version, stt_ms = (
                    {"text": student_text, "confidence": 1.0},
                    "text-from-b",
                    5,
                )
                if VERTEX_AI_ENABLED:
                    reply_text, llm_version, prompt_version, llm_ms = generate_vertex_reply(
                        student_text,
                        emotion_label,
                        risk_level,
                        preferred_name,
                        conversation_history,
                    )
                    print(f"[C] GEMINI respuesta session={body.get('session_id')} | {reply_text}", flush=True)
                else:
                    print(f"[C] STUB reply session={body.get('session_id')} motivo=VERTEX_AI_ENABLED=false", flush=True)
                    reply_text = build_contextual_reply(
                        student_text,
                        preferred_name,
                        emotion_label,
                        risk_level,
                        conversation_history,
                    )
                    llm_version = "stub-ollama"
                    llm_ms = 80
            else:
                transcript, stt_version, stt_ms = infer_transcript(audio_path)
                student_text = transcript["text"]
                if not emotion_label:
                    emotion_label, emotion_confidence = infer_emotion(student_text)
                risk_signals, detected_risk_level = detect_risk_signals(student_text)
                if detected_risk_level != "low" and risk_level == "low":
                    risk_level = detected_risk_level
                if VERTEX_AI_ENABLED:
                    reply_text, llm_version, prompt_version, llm_ms = generate_vertex_reply(
                        student_text,
                        emotion_label,
                        risk_level,
                        preferred_name,
                        conversation_history,
                    )
                    print(f"[C] GEMINI respuesta session={body.get('session_id')} | {reply_text}", flush=True)
                else:
                    print(f"[C] STUB reply session={body.get('session_id')} motivo=VERTEX_AI_ENABLED=false", flush=True)
                    reply_text = build_contextual_reply(
                        student_text,
                        preferred_name,
                        emotion_label,
                        risk_level,
                        conversation_history,
                    )
                    llm_version = "stub-ollama"
                    llm_ms = 80

            if not conversation_history and not preferred_name:
                introduction = "Hola, soy EmpathIA, una IA de apoyo emocional."
                introduction += " ¿Cómo te gustaría que te llamara?"
                reply_text = f"{introduction} {reply_text}"

            emotion_confidence = emotion_confidence if emotion_confidence is not None else 0.62
            prompt_version = active_prompt_name(select_prompt_key(emotion_label, risk_level))

            updated_history = [
                *full_history,
                {"speaker": "usuario", "text": student_text},
                {"speaker": "ia", "text": reply_text},
            ]
            updated_history = merge_conversation_history([], updated_history)
            memory_updated = save_conversation_memory(body.get("session_id"), updated_history)
            print(
                f"[C] memoria turnos={len(updated_history) // 2} session={body.get('session_id')} updated={memory_updated}",
                flush=True,
            )

            out_dir = DATA_ROOT / "audio" / "output" / str(body.get("session_id", "session"))
            out_dir.mkdir(parents=True, exist_ok=True)
            out_path = out_dir / f"{turn_id}.wav"
            tts_started = time.perf_counter()
            tts_version = synthesize_reply_wav(reply_text, out_path)
            tts_ms = max(1, int((time.perf_counter() - tts_started) * 1000))
            audio_b64 = base64.b64encode(out_path.read_bytes()).decode("ascii") if out_path.exists() else ""

            expression = {}
            if FIXTURE_EXPRESSION.exists():
                expression = json.loads(FIXTURE_EXPRESSION.read_text(encoding="utf-8"))
                expression["turn_id"] = turn_id

            duration_ms = int(expression.get("duration_ms", 2400))
            cues = [
                {"t_ms": lip["t_ms"], "viseme": lip["viseme"]}
                for lip in expression.get("lips", [])
            ]

            analysis_ms = 20
            total_ms = stt_ms + analysis_ms + llm_ms + tts_ms

            payload = {
                "request_id": request_id,
                "transcript": transcript,
                "emotion": {"label": emotion_label, "confidence": emotion_confidence},
                "risk_signals": risk_signals,
                "reply": {
                    "text": reply_text,
                    "guardrail_flags": [],
                },
                "tts": {
                    "path": str(out_path),
                    "format": "wav",
                    "duration_ms": duration_ms,
                    "audio_b64": audio_b64,
                },
                "timing": {"quality": "low", "cues": cues},
                "expression": expression,
                "memory": {"updated": memory_updated},
                "model_versions": {
                    "stt": stt_version,
                    "llm": llm_version,
                    "prompt": prompt_version,
                    "tts": tts_version,
                },
                "metrics": {
                    "stt_ms": stt_ms,
                    "analysis_ms": analysis_ms,
                    "llm_ms": llm_ms,
                    "tts_ms": tts_ms,
                    "total_ms": total_ms,
                },
            }
            json_response(self, 200, payload)
            return

        json_response(self, 404, {"error": {"code": "NOT_FOUND", "message": path}})


def _find_tts_wav(turn_id: str) -> Path | None:
    root = DATA_ROOT / "audio" / "output"
    if not turn_id or not root.exists():
        return None
    matches = list(root.rglob(f"{turn_id}.wav"))
    return matches[0] if matches else None


def synthesize_reply_wav(text: str, out_path: Path) -> str:
    """Escribe un WAV con voz. Prueba el paquete de C y, si no, SAPI de Windows."""
    spoken = (text or "").strip() or "Estoy aquí para acompañarte."
    out_path.parent.mkdir(parents=True, exist_ok=True)

    if _tts_with_windows_sapi(spoken, out_path):
        return "windows-sapi"
    if _tts_with_pyttsx3(spoken, out_path):
        return "pyttsx3"

    if SILENT_WAV.exists():
        shutil.copyfile(SILENT_WAV, out_path)
    else:
        out_path.write_bytes(_minimal_wav())
    print("[C] TTS cayó a silencio. Instala voz o pyttsx3.", flush=True)
    return "stub-kokoro"


def _tts_with_windows_sapi(text: str, out_path: Path) -> bool:
    txt_path = out_path.with_suffix(".txt")
    ps1_path = out_path.with_suffix(".ps1")
    try:
        txt_path.write_text(text, encoding="utf-8")
        ps1_path.write_text(
            "\n".join(
                [
                    "Add-Type -AssemblyName System.Speech",
                    f"$txt = Get-Content -LiteralPath '{txt_path}' -Raw -Encoding UTF8",
                    "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer",
                    "$s.Rate = -1",
                    "$es = $s.GetInstalledVoices() | ForEach-Object { $_.VoiceInfo } |",
                    "  Where-Object { $_.Culture.Name -like 'es*' } | Select-Object -First 1",
                    "if ($es) { $s.SelectVoice($es.Name) }",
                    f"$s.SetOutputToWaveFile('{out_path}')",
                    "$s.Speak($txt)",
                    "$s.Dispose()",
                ]
            ),
            encoding="utf-8",
        )
        completed = subprocess.run(
            [
                "powershell",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                str(ps1_path),
            ],
            capture_output=True,
            text=True,
            timeout=40,
            check=False,
        )
        return completed.returncode == 0 and out_path.exists() and out_path.stat().st_size > 44
    except Exception as exc:
        print(f"[C] TTS SAPI falló: {exc}", flush=True)
        return False
    finally:
        for extra in (txt_path, ps1_path):
            try:
                extra.unlink(missing_ok=True)
            except OSError:
                pass


def _tts_with_pyttsx3(text: str, out_path: Path) -> bool:
    try:
        import pyttsx3
    except Exception:
        return False
    try:
        engine = pyttsx3.init()
        for voice in engine.getProperty("voices") or []:
            name = f"{getattr(voice, 'id', '')} {getattr(voice, 'name', '')} {getattr(voice, 'languages', '')}"
            if "es" in name.lower() or "span" in name.lower():
                engine.setProperty("voice", voice.id)
                break
        engine.save_to_file(text, str(out_path))
        engine.runAndWait()
        return out_path.exists() and out_path.stat().st_size > 44
    except Exception as exc:
        print(f"[C] TTS pyttsx3 falló: {exc}", flush=True)
        return False


def _minimal_wav() -> bytes:
    """PCM WAV silence ~0.25s, 16-bit mono 16kHz."""
    import struct

    sample_rate = 16000
    num_samples = sample_rate // 4
    data = b"\x00\x00" * num_samples
    byte_rate = sample_rate * 2
    block_align = 2
    bits_per_sample = 16
    data_size = len(data)
    riff_size = 36 + data_size
    header = struct.pack(
        "<4sI4s4sIHHIIHH4sI",
        b"RIFF",
        riff_size,
        b"WAVE",
        b"fmt ",
        16,
        1,
        1,
        sample_rate,
        byte_rate,
        block_align,
        bits_per_sample,
        b"data",
        data_size,
    )
    return header + data


def main() -> None:
    DATA_ROOT.mkdir(parents=True, exist_ok=True)
    (DATA_ROOT / "audio" / "output").mkdir(parents=True, exist_ok=True)
    if not SILENT_WAV.exists():
        SILENT_WAV.parent.mkdir(parents=True, exist_ok=True)
        SILENT_WAV.write_bytes(_minimal_wav())

    server = ThreadingHTTPServer((HOST, PORT), Handler)
    print(f"EmpathIA intelligence stub on http://{HOST}:{PORT}", flush=True)
    print(f"DATA_ROOT={DATA_ROOT}", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("Stopping intelligence stub...", flush=True)
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
