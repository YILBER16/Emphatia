# M0-B — Entorno Laravel (Servidor)

## ID

`M0-B-entorno-laravel`

## Título

Levantar el servidor Laravel y comprender el flujo de la API

## Rol

B

## Objetivo

Que el API EmpathIA responda en tu PC (`health` OK), que entiendas el rol de orquestador, y que sepas cómo se valida el esqueleto con la prueba de humo — **sin** nuevas features.

## Tiempo estimado

60–90 minutos

## Competencias que desarrolla

- Setup backend local  
- Lectura de healthchecks  
- Ownership del front-door  

## Conocimientos previos

- M0-COMUN  
- Laragon o PHP+Composer disponibles  

## Entregables

1. `php artisan serve --host=127.0.0.1 --port=8000` funcionando  
2. Respuesta OK de `GET http://127.0.0.1:8000/api/v1/health`  
3. `servidor/APRENDIZAJE.md` (o sección en README) explicando: qué es B, qué orquesta, qué no hace  
4. (Ideal) prueba de humo OK en tu PC: `herramientas/prueba-humo-fase0.ps1`  

## Criterios de aceptación

- [ ] Health devuelve status ok o degradado documentado  
- [ ] Sabes usuarios seed (`estudiante1` / `password`)  
- [ ] Explicas el flujo A→B→C en 1 minuto  
- [ ] Sabes que `INTEL_STUB=true` permite trabajar sin C real  
- [ ] Checklist Sprint 0 B en verde  

## Cómo validar el resultado

1. Arranca el servidor.  
2. Abre health en navegador o curl.  
3. (Ideal) Ejecuta la prueba de humo y muestra `PHASE 0 SMOKE OK`.  
4. Lee 30 s de tu APRENDIZAJE al mentor.  

## Errores comunes

| Error | Qué hacer |
|-------|-----------|
| Puerto 8000 ocupado | Cierra el otro `artisan`/`php -S` |
| `vendor` faltante | `composer install` en `servidor/` |
| Abriste solo `backend/` y te confundiste | Usa `servidor/` (puede ser enlace al mismo código) |
| Empiezas a reescribir arquitectura | Stop — Sprint 0 es entorno |

## Qué NO debo hacer

- Implementar WebSockets nuevos  
- Migrar a MySQL obligatorio hoy (salvo que el mentor lo pida)  
- Dejar que A hable con C  

## Referencias técnicas

- `documentacion/manuales/arranque-parada.md` / `puertos.md`  
- `documentacion/equipo/guia-rol-B-servidor.md`  
- `contratos/api-rest/v1/openapi.yaml` (lectura)  

## Evidencia de cierre

Health OK (+ humo si aplica) + APRENDIZAJE.
