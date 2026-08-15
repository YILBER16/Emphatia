# Aprendizaje — Rol A (Avatar / Unity)

**Misión actual:** M1-A · Login y sesión  
**Carpeta de trabajo:** `cliente-unity/`

---

## Qué es mi rol

Soy el **Rol A — Avatar**. Soy la cara del sistema EmpathIA: el estudiante me ve, me habla y me oye.

En Unity me toca:

- Pantalla de login y sesión
- Captura de micrófono
- Estados de UI (`idle` / `listening` / `processing` / `speaking`)
- Reproducir el audio TTS de la respuesta
- Aplicar el `ExpressionPacket` (labios y cara) que define D

**No “pienso”.** No hago IA, STT, LLM ni TTS. Solo consumo lo que me entrega el **Servidor B** en `http://127.0.0.1:8000`.

---

## Estructura

```text
cliente-unity/
├── APRENDIZAJE.md
├── README.md
└── avatar/                    ← proyecto Unity 6
    └── Assets/Scripts/Empathia/
        ├── EmpathiaAuthState.cs      ← token + session.id en memoria
        ├── EmpathiaApiModels.cs      ← DTOs JSON
        ├── EmpathiaApiClient.cs      ← HTTP a B (login / sesión / close)
        └── LoginScreenController.cs  ← UI Sprint 1 (se monta sola)
```

---

## Sprint 1 — Login y sesión (flujo)

1. B arriba: `cd backend` → `php artisan serve --host=127.0.0.1 --port=8000`
2. Abrir `cliente-unity/avatar/` en Unity 6 y Play en cualquier escena.
3. UI aparece sola (`LoginScreenController` bootstrap).
4. **Entrar** con `estudiante1` / `password` → ver token parcial.
5. **Crear sesión** → ver `session.id` (o mensaje si `SESSION_ALREADY_ACTIVE`).
6. **Cerrar sesión** cuando haga falta.

### Endpoints que uso (solo B)

| Acción | Método |
|--------|--------|
| Login | `POST /api/v1/auth/login` |
| Crear sesión | `POST /api/v1/accompaniment/sessions` + `Authorization: Bearer …` |
| Cerrar sesión | `POST /api/v1/accompaniment/sessions/{id}/close` |

**Nunca** llamo a `http://127.0.0.1:8100` (Inteligencia / C).

### Errores en español (vistos / esperados)

| Situación | Mensaje UI |
|-----------|------------|
| B apagado | No se pudo conectar… ¿está encendido en :8000? |
| Clave mal | Usuario o contraseña incorrectos. |
| Sesión ya activa | Ya hay una sesión activa. Ciérrala… |
| HTTP cleartext | Player Settings → Allow downloads over HTTP: Development Only (ya configurado) |

### Nota de prueba A↔B

- **Fecha:** (completar al probar)
- **Resultado:** pendiente de prueba con B arriba
- **Si falla:** anotar código HTTP / `error.code` aquí

---

## Carpetas que no toco

| Carpeta | Por qué |
|---------|---------|
| `servidor/` / `backend/` | Es de B |
| `inteligencia/` | Es de C; **nunca** `:8100` |
| `expresion/` | Es de D |
| `contratos/` | Solo con review |

---

## Evidencia Sprint 0

- [x] Unity Hub + Editor 6
- [x] Proyecto en `cliente-unity/avatar/`
- [x] `APRENDIZAJE.md` explicable en ~30 s
