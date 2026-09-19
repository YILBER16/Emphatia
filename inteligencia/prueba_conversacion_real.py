#!/usr/bin/env python3
"""Prueba HTTP del flujo real del servidor C."""

from __future__ import annotations

import json
import sys
import uuid
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

BASE_URL = "http://127.0.0.1:8100"
TOKEN = "empathia-internal-dev-token"


def request_json(path: str, payload: dict | None = None) -> dict:
    data = json.dumps(payload, ensure_ascii=False).encode("utf-8") if payload is not None else None
    request = Request(
        f"{BASE_URL}{path}",
        data=data,
        headers={
            "Content-Type": "application/json",
            "X-Internal-Token": TOKEN,
        },
        method="POST" if payload is not None else "GET",
    )
    with urlopen(request, timeout=10) as response:
        return json.loads(response.read().decode("utf-8"))


def main() -> int:
    session_id = str(uuid.uuid4())
    student_id = str(uuid.uuid4())
    history: list[dict] = []
    messages = [
        "Me siento muy ansiosa por los exámenes y no sé por dónde empezar.",
        "Me siento triste porque temo decepcionar a mi familia.",
    ]

    print("=== PRUEBA REAL HTTP: C ===", flush=True)
    try:
        health = request_json("/internal/v1/health")
        print(f"Salud C: {json.dumps(health, ensure_ascii=False)}", flush=True)
    except (HTTPError, URLError, TimeoutError, json.JSONDecodeError) as error:
        print(f"ERROR: C no está disponible en {BASE_URL}: {error}", flush=True)
        return 1

    for number, message in enumerate(messages, start=1):
        payload = {
            "request_id": str(uuid.uuid4()),
            "session_id": session_id,
            "turn_id": str(uuid.uuid4()),
            "student_id": student_id,
            "locale": "es",
            "preferred_name": "Andrea",
            "text": message,
            "conversation_history": history,
        }
        try:
            result = request_json("/internal/v1/infer/turn", payload)
        except (HTTPError, URLError, TimeoutError, json.JSONDecodeError) as error:
            print(f"ERROR en turno {number}: {error}", flush=True)
            return 1

        emotion = result.get("emotion", {})
        reply = result.get("reply", {})
        transcript = result.get("transcript", {})
        print(f"\nTURNO {number}", flush=True)
        print(f"Usuario: {message}", flush=True)
        print(
            f"Emocion detectada: {emotion.get('label')} "
            f"(confianza {emotion.get('confidence')})",
            flush=True,
        )
        print(f"IA: {reply.get('text')}", flush=True)
        history.extend([
            {"speaker": "usuario", "text": transcript.get("text", message)},
            {"speaker": "ia", "text": reply.get("text", "")},
        ])

    print("\nRESULTADO: FLUJO REAL C OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
