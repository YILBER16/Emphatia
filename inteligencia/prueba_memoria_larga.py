#!/usr/bin/env python3
"""Prueba visible de memoria de conversación larga por usuario y sesión."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from prueba_conversacion import conversation_summary
from servidor_simulado import (
    build_contextual_reply,
    infer_emotion,
    merge_conversation_history,
    sanitize_preferred_name,
)


def main() -> int:
    student_user_id = 1
    session_id = "static-session-registered-user-1"
    name = sanitize_preferred_name("Andrea")
    messages = [
        "Hola, me llamo Andrea y hoy necesito hablar.",
        "Me siento ansiosa por un examen que tengo manana.",
        "No se como organizar todo lo que debo estudiar.",
        "Tambien temo decepcionar a mi familia.",
        "Anoche no pude descansar porque seguia pensando en eso.",
        "Siento tristeza cuando imagino que no voy a cumplir.",
        "Creo que necesito dividir el estudio en pasos pequenos.",
        "Me siento un poco mas tranquila al decirlo en voz alta.",
        "Quiero hablar con mi familia sin sentir que los decepciono.",
        "Gracias por escucharme; ya se cual sera mi primer paso.",
    ]
    history: list[dict] = []

    print("=== PRUEBA ESTATICA LARGA DE MEMORIA ===", flush=True)
    print(f"Usuario registrado: {student_user_id}", flush=True)
    print(f"Sesion: {session_id}", flush=True)
    print(f"Nombre preferido: {name}", flush=True)

    for number, message in enumerate(messages, start=1):
        emotion, confidence = infer_emotion(message)
        response = build_contextual_reply(message, name, emotion, "low", history)
        history = merge_conversation_history(
            history,
            [
                {"speaker": "usuario", "text": message},
                {"speaker": "ia", "text": response},
            ],
        )
        print(f"\n--- TURNO {number} ---", flush=True)
        print(f"USUARIO: {message}", flush=True)
        print(f"C RECUERDA: {len(history) // 2 - 1} turnos anteriores", flush=True)
        print(f"C EMOCION: {emotion} | confianza={confidence:.2f}", flush=True)
        print(f"IA: {response}", flush=True)

    summary = conversation_summary(history)
    print("\n=== RESUMEN COMPLETO ===", flush=True)
    print(summary, flush=True)
    print("\n=== COMPROBACION DE GUARDADO ===", flush=True)
    print(f"student_user_id: {student_user_id}", flush=True)
    print(f"session_id: {session_id}", flush=True)
    print(f"turnos guardados: {len(history) // 2}", flush=True)
    print(f"turnos en resumen: {summary.count('Turno ')}", flush=True)
    print("Destino B: accompaniment_sessions.conversation_summary", flush=True)
    print("Destino C: datos/inteligencia/memory/conversations/{session_id}.json", flush=True)

    if len(history) != 20 or summary.count("Turno ") != 10:
        print("RESULTADO: ERROR, faltan turnos", flush=True)
        return 1
    print("RESULTADO: PRUEBA ESTATICA LARGA OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
