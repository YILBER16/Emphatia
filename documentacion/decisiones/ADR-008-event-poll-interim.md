# ADR-008 — Event poll interim hasta WebSocket real (F1.4)

## Estado

Aceptado (solo Fase 0 / hasta F1.4).

## Contexto

El contrato v1 define WebSocket. Montar Reverb/broadcasting no es gate de Fase 0.

## Decisión

`GET /api/v1/accompaniment/sessions/{id}/events` entrega los **mismos envelopes** que WS v1. Unity/smoke pueden hacer poll. F1.4 implementa WS nativo sin cambiar `type` ni payloads.

## Consecuencias

Clientes no deben asumir que el poll existe post-F1.4 como canal primario; migrarán a WS.
