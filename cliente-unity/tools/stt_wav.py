#!/usr/bin/env python3
"""Transcribe a 16-bit mono/stereo WAV to text (local helper for Rol A).

Uses the SpeechRecognition package + Google Web Speech (needs internet).
Prints one JSON line: {"ok": true, "text": "..."} or {"ok": false, "error": "..."}.
"""
from __future__ import annotations

import json
import sys


def main() -> int:
    if len(sys.argv) < 2:
        print(json.dumps({"ok": False, "error": "usage: stt_wav.py <wav_path> [lang]"}, ensure_ascii=False))
        return 2

    wav_path = sys.argv[1]
    lang = sys.argv[2] if len(sys.argv) > 2 else "es-ES"

    try:
        import speech_recognition as sr
    except ImportError:
        print(json.dumps({
            "ok": False,
            "error": "Falta paquete SpeechRecognition. Ejecuta: py -3 -m pip install SpeechRecognition",
        }, ensure_ascii=False))
        return 1

    recognizer = sr.Recognizer()
    try:
        with sr.AudioFile(wav_path) as source:
            audio = recognizer.record(source)
    except Exception as ex:
        print(json.dumps({"ok": False, "error": f"No se pudo leer WAV: {ex}"}, ensure_ascii=False))
        return 1

    if not audio.frame_data:
        print(json.dumps({"ok": False, "error": "WAV vacío"}, ensure_ascii=False))
        return 1

    try:
        text = recognizer.recognize_google(audio, language=lang)
    except sr.UnknownValueError:
        print(json.dumps({"ok": False, "error": "No se entendió el audio. Habla más claro y cerca del mic."}, ensure_ascii=False))
        return 1
    except sr.RequestError as ex:
        print(json.dumps({"ok": False, "error": f"STT online no disponible: {ex}"}, ensure_ascii=False))
        return 1
    except Exception as ex:
        print(json.dumps({"ok": False, "error": str(ex)}, ensure_ascii=False))
        return 1

    text = (text or "").strip()
    if not text:
        print(json.dumps({"ok": False, "error": "Transcripción vacía"}, ensure_ascii=False))
        return 1

    print(json.dumps({"ok": True, "text": text}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
