# EmpathIA — WebSocket session protocol v1

Base URL (piloto): `ws://127.0.0.1:8000/ws/v1/accompaniment/{session_id}?ticket={ws_ticket}`

## Envelope

Every message (client ↔ server):

```json
{
  "v": 1,
  "type": "event.name",
  "ts": "2026-07-31T00:00:00Z",
  "session_id": "uuid",
  "payload": {}
}
```

## Client → Server

| type | payload |
|------|---------|
| `client.hello` | `{ "client": "unity", "protocol_v": 1 }` |
| `client.heartbeat` | `{ "ui_state": "listening\|speaking\|idle" }` |
| `client.abort_turn` | `{ "turn_id"?: "uuid" }` |
| `client.close_session` | `{}` |

Audio does **not** travel over WS in v1 (REST multipart).

## Server → Client

| type | payload summary |
|------|-----------------|
| `session.ready` | session_id, student_user_id, locale |
| `session.state` | state: idle \| listening \| processing \| speaking \| closed \| aborted |
| `turn.accepted` | turn_id, sequence_no, client_turn_key |
| `turn.processing` | turn_id, stage |
| `turn.result` | see schema `schemas/turn-result.payload.json` |
| `turn.error` | turn_id, code, message, retryable |
| `session.closed` | reason |
| `server.error` | code, message |

## Phase 0 interim

Until Laravel Reverb (or equivalent) is wired in F1.4, the backend exposes:

`GET /api/v1/accompaniment/sessions/{session_id}/events`

which returns the **same envelopes** queued for the session. Smoke tests and Unity can poll this endpoint. The event `type` values remain identical to this WS contract.
