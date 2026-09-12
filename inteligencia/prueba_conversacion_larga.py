#!/usr/bin/env python3
"""Prueba larga y visible de continuidad conversacional."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from prueba_conversacion import conversation_summary, humanized_reply, introduction_reply
from servidor_simulado import infer_emotion, sanitize_preferred_name


def main() -> int:
    name = sanitize_preferred_name("Andrea")
    messages = [
        "Hoy me siento muy ansiosa por los exámenes y no sé cómo organizarme.",
        "Además, temo decepcionar a mi familia si no saco buenas notas.",
        "Me cuesta descansar porque sigo pensando en todo lo que podría salir mal.",
        "Creo que necesito ordenar mis pendientes y hablar con alguien sin sentirme juzgada.",
        "Al decirlo en voz alta me siento un poco más tranquila.",
        "Quiero empezar por estudiar una materia y después hablar con mi familia.",
    ]
    history: list[dict] = [
        {"speaker": "ia", "text": introduction_reply()},
        {"speaker": "usuario", "text": "Me gustaría que me llamaras Andrea."},
    ]

    print("=== PRUEBA LARGA DE CONVERSACION ===", flush=True)
    print(f"IA: {introduction_reply()}", flush=True)
    print("Usuario: Me gustaría que me llamaras Andrea.", flush=True)
    print(f"Sistema: nombre validado = {name}", flush=True)

    for number, message in enumerate(messages, start=1):
        emotion, confidence = infer_emotion(message)
        response = humanized_reply(name, emotion, history)
        print(f"\n--- TURNO {number} ---", flush=True)
        print(f"Usuario: {message}", flush=True)
        print(f"C: emoción={emotion}, confianza={confidence:.2f}", flush=True)
        print(f"IA: {response}", flush=True)
        history.extend([
            {"speaker": "usuario", "text": message},
            {"speaker": "ia", "text": response},
        ])

    summary = conversation_summary(history)
    print("\n=== RESUMEN QUE B GUARDARIA ===", flush=True)
    print(summary, flush=True)
    print("\n=== UBICACION EN B ===", flush=True)
    print("Base: C:\\Emphatia\\backend\\database\\database.sqlite", flush=True)
    print("Tabla: accompaniment_sessions", flush=True)
    print("Columna: conversation_summary", flush=True)
    print(f"Turnos resumidos: {summary.count('Turno ')}", flush=True)
    print("\nRESULTADO: PRUEBA LARGA OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
