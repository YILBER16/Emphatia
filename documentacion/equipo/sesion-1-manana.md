# Sesión 1 — Plan para el mentor (mañana)

**Duración:** 60–75 minutos  
**Meta:** cada estudiante sabe su rol, ve que el proyecto ya funciona, y se va con **una misión** + un prompt para Cursor.

No expliques arquitectura completa.

---

## Antes de que lleguen (15–20 min, tú solo)

- [ ] PC con Laragon / PHP listo  
- [ ] Repo abierto en Cursor  
- [ ] Servidor: `cd servidor` → `php artisan serve --host=127.0.0.1 --port=8000`  
- [ ] Probar `.\herramientas\prueba-humo-fase0.ps1` → debe decir `PHASE 0 SMOKE OK`  
- [ ] Tener listos los 4 archivos de `documentacion/equipo/hojas-rol/`  
- [ ] Tener abierto `documentacion/equipo/camino-simple.md` (solo tú)  
- [ ] Decidir roles si ellos no eligen  

**Plan B si falla la prueba de humo:** metáfora + hojas de rol igual. No debuguees 40 minutos delante de ellos.

---

## Asignación de roles

| Perfil | Rol | Carpeta |
|--------|-----|---------|
| Visual / juegos | **A** Avatar | `cliente-unity/` |
| Ordenado / sistemas | **B** Servidor | `servidor/` |
| Curioso por IA | **C** Inteligencia | `inteligencia/` |
| Detalle / animación | **D** Expresión | `expresion/` |

Anota nombres en `documentacion/equipo/inicio.md`.

---

## Guion

### 0–5 min · Calma

> “EmpathIA es un prototipo: un avatar que escucha y responde.  
> No vamos a construir una empresa. Ya hay una base.  
> Cada uno tendrá un rol y una misión chiquita.  
> Usamos Cursor: ustedes dicen qué quieren, la IA ayuda, ustedes prueban.”

### 5–12 min · Metáfora

```text
Estudiante habla
    → A (Unity) escucha y muestra el avatar
    → B (Servidor) organiza
    → C (Inteligencia) piensa y arma la voz
    → vuelve a A (habla y mueve la boca)
         D enseña cómo se mueve la cara
```

Regla de oro:

> “Cada quien su carpeta. No tocamos la del compañero. Si no entendemos, preguntamos.”

### 12–20 min · Roles + hoja

Entregan solo su archivo en `hojas-rol/`.

### 20–35 min · Demo

1. Arranca `servidor`  
2. Corre la prueba de humo  
3. Di: “Esto es el esqueleto. Ustedes le ponen cara e inteligencia.”

### 35–45 min · Reglas Cursor

1. Abrir el repo  
2. Pegar el prompt de su misión  
3. Pedir: “explícame en 5 líneas, en español”  
4. Si hay error: pegarlo a Cursor  
5. Al final: 3 bullets (hice / no entendí / necesito)

### 45–60 min · Empiezan la misión

Tú das vueltas 5 min por persona.

### Cierre

> “La próxima vez traen su avance aunque esté incompleto.”

---

## Prompts del día (español + carpetas en español)

### A

```text
Somos el módulo A de EmpathIA (Unity 6). Carpeta: cliente-unity/
NO toques servidor/ ni inteligencia/.
Objetivo de hoy: escena simple con usuario, contraseña y botón Entrar.
Al entrar, guarda un token (texto) y muestra "Login OK".
Crea un README corto en español: qué falta para conectar al API
http://127.0.0.1:8000/api/v1/auth/login
Explica cada archivo nuevo en español, muy simple.
```

### B

```text
Somos el módulo B de EmpathIA (Laravel en la carpeta servidor/).
NO reescribas la arquitectura.
Objetivo de hoy: ampliar servidor/README.md con 5 ejemplos curl para Windows:
login, crear sesión, subir turno, ver events, cerrar sesión.
Usuario estudiante1 / password. Base http://127.0.0.1:8000/api/v1
Texto en español claro para compañeros de bachillerato.
```

### C

```text
Somos el módulo C de EmpathIA (carpeta inteligencia/).
Lee servidor_simulado.py y el README.
Objetivo de hoy:
1) inteligencia/APRENDIZAJE.md explicando el simulador en español simple
2) carpetas vacías stt/, llm/, tts/, memory/ con un archivo vacío o .gitkeep
3) qué irá en cada carpeta después (sin programarlo aún)
No conectes Ollama todavía.
```

### D

```text
Somos el módulo D de EmpathIA.
Lee expresion/fixtures/paquete-expresion-ejemplo.json
y contratos/expresion/v1/enums.md
Crea expresion/APRENDIZAJE.md en español simple:
qué es un visema, qué hacen los gestos del ejemplo,
lista de morphs que pediremos a Unity.
Sin código de Unity todavía.
```

---

## Qué NO hacer hoy

- No repartir el backlog completo  
- No explicar WebSockets, Ollama, ADRs  
- No pedir que instalen todo el stack cada uno  

## Tu éxito al terminar

- [ ] 4 roles con nombre  
- [ ] Cada uno con hoja + prompt  
- [ ] Vieron la prueba de humo  
- [ ] Empezaron en Cursor  
- [ ] Fecha de la próxima revisión
