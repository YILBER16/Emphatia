#!/usr/bin/env python3
"""EmpathIA Intelligence stub (module C) — Phase 0.

Listens on 0.0.0.0:8100 (LAN). Override with INTEL_HOST / INTEL_PORT.
Implements internal InferTurn + health + memory stubs.
"""

from __future__ import annotations

import json
import os
import re
import shutil
import time
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlparse

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
    return json.loads(raw.decode("utf-8") or "{}")


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


def load_prompt(prompt_id: str, student_text: str, preferred_name: str = "") -> tuple[str, str]:
    registry = json.loads(PROMPTS_REGISTRY.read_text(encoding="utf-8"))
    prompt_definition = registry["prompts"][prompt_id]
    prompt_path = PROMPTS_ROOT / prompt_definition["path"]
    prompt = prompt_path.read_text(encoding="utf-8")
    prompt = prompt.replace("{{student_text}}", student_text)
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
    prompt, prompt_name = load_prompt(prompt_name, student_text, preferred_name)
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
                        "memory": "stub",
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
        json_response(self, 404, {"error": {"code": "NOT_FOUND", "message": path}})

    def do_POST(self) -> None:  # noqa: N802
        path = urlparse(self.path).path
        if not authorized(self):
            json_response(self, 401, {"error": {"code": "UNAUTHORIZED", "message": "Invalid internal token"}})
            return

        if path == "/internal/v1/memory/prepare":
            _ = read_json(self)
            json_response(self, 200, {"ok": True, "memory": {"ready": True}})
            return

        if path == "/internal/v1/memory/purge":
            body = read_json(self)
            student_id = body.get("student_id", "unknown")
            target = DATA_ROOT / "intelligence" / "memory" / str(student_id)
            if target.exists():
                shutil.rmtree(target, ignore_errors=True)
            json_response(self, 200, {"ok": True, "purged": student_id})
            return

        if path == "/internal/v1/infer/turn":
            body = read_json(self)
            turn_id = body.get("turn_id") or str(uuid.uuid4())
            request_id = body.get("request_id") or str(uuid.uuid4())
            audio_path = (body.get("audio") or {}).get("path") if isinstance(body.get("audio"), dict) else None
            student_text = body.get("text") if isinstance(body.get("text"), str) else ""
            student_text = student_text.strip()
            emotion_input = body.get("emotion") if isinstance(body.get("emotion"), dict) else {}
            emotion_label = emotion_input.get("label") if isinstance(emotion_input.get("label"), str) else ""
            risk_level = body.get("risk_level") if isinstance(body.get("risk_level"), str) else "low"
            preferred_name = sanitize_preferred_name(body.get("preferred_name"))
            prompt_version = active_prompt_name(select_prompt_key(emotion_label, risk_level))

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
                    )
                    print(f"[C] GEMINI respuesta session={body.get('session_id')} | {reply_text}", flush=True)
                else:
                    greeting = f"{preferred_name}, gracias por contármelo. " if preferred_name else "Gracias por contármelo. "
                    reply_text = (
                        f"{greeting}Suena a que estás llevando bastante encima. "
                        "Podemos ir paso a paso; ¿qué es lo que más te está pesando ahora?"
                    )
                    llm_version = "stub-ollama"
                    llm_ms = 80
            else:
                transcript, stt_version, stt_ms = infer_transcript(audio_path)
                reply_text = (
                    "Gracias por contármelo. Estoy aquí para acompañarte. "
                    "¿Quieres contarme un poco más sobre cómo te ha ido el día?"
                )
                llm_version = "stub-ollama"
                llm_ms = 80

            out_dir = DATA_ROOT / "audio" / "output" / str(body.get("session_id", "session"))
            out_dir.mkdir(parents=True, exist_ok=True)
            out_path = out_dir / f"{turn_id}.wav"
            if SILENT_WAV.exists():
                shutil.copyfile(SILENT_WAV, out_path)
            else:
                out_path.write_bytes(_minimal_wav())

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
            tts_ms = 40
            total_ms = stt_ms + analysis_ms + llm_ms + tts_ms

            payload = {
                "request_id": request_id,
                "transcript": transcript,
                "emotion": {"label": emotion_label or "sadness", "confidence": 0.62},
                "risk_signals": [],
                "reply": {
                    "text": reply_text,
                    "guardrail_flags": [],
                },
                "tts": {
                    "path": str(out_path),
                    "format": "wav",
                    "duration_ms": duration_ms,
                },
                "timing": {"quality": "low", "cues": cues},
                "expression": expression,
                "memory": {"updated": True},
                "model_versions": {
                    "stt": stt_version,
                    "llm": llm_version,
                    "prompt": prompt_version,
                    "tts": "stub-kokoro",
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
