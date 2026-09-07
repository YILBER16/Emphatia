# Checklist Fase 5 — Demo integrado perfiles sin password

ADR: [ADR-009](../../decisiones/ADR-009-perfiles-estudiante-sin-password.md)

Script: `herramientas/demo-perfiles-estudiante.ps1`

## Demo B (API)

- [ ] `php artisan serve --host=0.0.0.0 --port=8000`
- [ ] Admin crea 1–2 perfiles (`POST /admin/students`) y guarda `access_code`
- [ ] Regenera código de uno (`.../regenerate-code`)
- [ ] Counselor lista activos (`GET /students`) — sin `access_code`
- [ ] Counselor `assume` → token estudiante
- [ ] Con token estudiante: sesión + `POST .../active/text` + `turn.result` en events

## Demo A (Unity)

- [ ] `git pull` en `STID`
- [ ] Base URL = IP de B (`http://192.168.1.31:8000/api/v1` o la actual)
- [ ] Login `orientador1` / `password`
- [ ] Lista muestra perfiles activos
- [ ] Elegir estudiante → Confirm → Salud → texto → respuesta

## Docs

- [x] ADR-009 cerrado (Fases 0–5)
- [x] Hojas de rol A/B actualizadas
- [x] README / APRENDIZAJE B y A
- [x] Script demo PowerShell

## Fuera / legado

- [x] `estudiante1` / `password` solo demo lab (documentado)

**Fecha:** 2026-09-05  
**Validado B:** ________  
**Validado A:** ________
