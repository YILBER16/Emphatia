# ADR-007 — Fase 0 usa SQLite; MySQL en Fase 1

## Estado

Aceptado (piloto lab).

## Contexto

El diseño fija MySQL como verdad institucional. Configurar MySQL en Laragon no debe bloquear el slice vertical de Fase 0.

## Decisión

- Fase 0: SQLite (`backend/database/database.sqlite`) + `INTEL_STUB=true`.
- Fase 1: migrar connection a MySQL según arquitectura.

## Consecuencias

El schema de dominio se escribe portable (migrations). No usar features solo-SQLite en queries de negocio.
