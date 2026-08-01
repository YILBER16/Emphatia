#!/usr/bin/env python3
"""EmpathIA Intelligence stub (module C) — Phase 0.

Listens on 127.0.0.1:8100. No Whisper/Ollama/Kokoro.
Implements internal InferTurn + health + memory stubs.
"""

from __future__ import annotations

import json
import os
import shutil
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlparse

HOST = "127.0.0.1"
PORT = int(os.environ.get("INTEL_PORT", "8100"))
INTERNAL_TOKEN = os.environ.get("INTEL_INTERNAL_TOKEN", "empathia-internal-dev-token")
REPO_ROOT = Path(__file__).resolve().parents[1]
DATA_ROOT = Path(os.environ.get("EMPATHIA_DATA_ROOT", str(REPO_ROOT / "datos")))
FIXTURE_EXPRESSION = REPO_ROOT / "expresion" / "fixtures" / "paquete-expresion-ejemplo.json"
SILENT_WAV = Path(__file__).resolve().parent / "fixtures" / "silent.wav"


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


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt: str, *args) -> None:
        print(f"[intelligence-stub] {self.address_string()} {fmt % args}")

    def do_GET(self) -> None:  # noqa: N802
        path = urlparse(self.path).path
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

            payload = {
                "request_id": request_id,
                "transcript": {
                    "text": "Hola, hoy me siento un poco cansado pero quiero hablar.",
                    "confidence": 0.91,
                },
                "emotion": {"label": "sadness", "confidence": 0.62},
                "risk_signals": [],
                "reply": {
                    "text": (
                        "Gracias por contármelo. Estoy aquí para acompañarte. "
                        "¿Quieres contarme un poco más sobre cómo te ha ido el día?"
                    ),
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
                    "stt": "stub-whisper",
                    "llm": "stub-ollama",
                    "tts": "stub-kokoro",
                },
                "metrics": {
                    "stt_ms": 50,
                    "analysis_ms": 20,
                    "llm_ms": 80,
                    "tts_ms": 40,
                    "total_ms": 190,
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
    server.serve_forever()


if __name__ == "__main__":
    main()
