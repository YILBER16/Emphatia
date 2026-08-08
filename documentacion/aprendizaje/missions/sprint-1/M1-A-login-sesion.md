# M1-A — Login y sesión con el servidor

## ID

`M1-A-login-sesion`

## Título

Conectar el avatar al API real de B (login + sesión)

## Rol

A

## Objetivo

Que Unity (o el cliente de prueba) obtenga un **token real** del servidor y pueda **crear una sesión** de acompañamiento, mostrando estados claros al usuario.

## Tiempo estimado

90–150 minutos

## Competencias que desarrolla

- Cliente HTTP desde Unity  
- Manejo de token Bearer  
- UX de error en cliente  

## Conocimientos previos

- Sprint 0 A (Unity Hub / proyecto en `cliente-unity/`)  
- Servidor B accesible en `127.0.0.1:8000` (PC propio o piloto)  

## Entregables

1. Escena o flujo: login → Login OK con token (parcial visible)  
2. Crear sesión autenticada (o botón listo + evidencia de response)  
3. Actualización de `cliente-unity/APRENDIZAJE.md` con el flujo y errores vistos  
4. Nota de prueba A↔B (funcionó / no / por qué)  

## Criterios de aceptación

- [ ] POST `/api/v1/auth/login` con `estudiante1` / `password`  
- [ ] Token guardado en memoria/sesión de juego  
- [ ] UI muestra éxito o error en español  
- [ ] Intento de `POST /api/v1/accompaniment/sessions` con Bearer  
- [ ] No se llama a `:8100`  
- [ ] Checklist Sprint 1 A en verde  

## Cómo validar el resultado

1. B tiene `artisan serve` arriba.  
2. Desde A: login → ver token.  
3. Crear sesión → ver `session.id` o error `SESSION_ALREADY_ACTIVE` manejado.  
4. Mentor ve la pantalla ≤ 2 min.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Connection refused | Servidor apagado o IP/puerto mal |
| 401 | Usuario/clave o body JSON mal formado |
| SESSION_ALREADY_ACTIVE | Pedir a B cerrar sesión o usar close |
| Unity bloquea HTTP cleartext | Permitir HTTP local en settings del player |

## Qué NO debo hacer

- Hablar con Inteligencia directo  
- Subir audio/turno completo si aún no estabilizas login (bonus solo)  
- Modificar `backend/`  

## Bonus (si terminas temprano)

- Poll de `/events` mostrando el último `type` en UI  
- Estados visibles: idle / listening (aunque listening sea solo label)  

## Referencias

- README de B (“Para A”)  
- `contratos/api-rest/v1/openapi.yaml`  
- `documentacion/equipo/guia-rol-A-avatar.md`  
- Guion: `documentacion/equipo/clase-2.md`  

## Evidencia de cierre

Pantalla Login OK + token + (ideal) session id.
