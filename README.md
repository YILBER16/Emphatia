# Emphatia - Prueba Avatar (Unity)

## Contexto del proyecto
Este repositorio contiene una prueba funcional de avatar 3D con enfoque en expresividad facial y sincronizacion labial en tiempo real dentro de Unity.

El objetivo principal es validar:
- Carga y visualizacion de avatar Streamoji.
- Control de blendshapes faciales estilo ARKit.
- Lip sync por audio (modo simple por energia y modo basado en visemas OVR).
- Gesticulacion facial complementaria (parpadeo, mirada, expresion).
- Movimiento corporal procedural durante el habla.

## Stack tecnico
- Motor: Unity 6
- Version de editor: 6000.4.10f1
- Pipeline: URP (Universal Render Pipeline)
- Input: New Input System

Dependencias relevantes en Packages/manifest.json:
- com.unity.render-pipelines.universal
- com.unity.inputsystem
- com.unity.cloud.gltfast

## Estructura relevante
- Assets/Streamoji/
  - Animations/
  - Prefabs/
  - Scenes/
  - Scripts/
- Assets/Scenes/
- ProjectSettings/
- Packages/

## Escenas principales
- Assets/Scenes/SampleScene.unity
- Assets/Streamoji/Scenes/StreamojiAvatarScene.unity

## Scripts clave de la prueba
- Assets/Streamoji/Scripts/SimpleJawOpenFromAudio.cs
  - Lip sync rapido basado en energia RMS y espectro.
- Assets/Streamoji/Scripts/OVRLipSyncToARKitBlendshapes.cs
  - Mapeo de visemas OVR a blendshapes ARKit (si el paquete OVR esta disponible).
- Assets/Streamoji/Scripts/FacialPerformanceController.cs
  - Capa de performance facial: parpadeo, mirada, cejas, sonrisa y suavizados.
- Assets/Streamoji/Scripts/TalkBodyMotion.cs
  - Movimiento corporal procedural guiado por energia de voz.

## Flujo de uso recomendado
1. Abrir el proyecto con Unity 6000.4.10f1.
2. Cargar una de las escenas principales.
3. Verificar referencias en inspector de scripts:
   - AudioSource
   - SkinnedMeshRenderer de cabeza
   - SkinnedMeshRenderer secundario (dientes, si aplica)
   - Huesos de cuerpo (hips/spine/chest en TalkBodyMotion)
4. Activar un solo controlador de lip sync a la vez:
   - SimpleJawOpenFromAudio o
   - OVRLipSyncToARKitBlendshapes
5. Ajustar parametros de jaw, smoothing, parpadeo y expresion hasta lograr naturalidad.

## Estado actual
- Prototipo funcional para pruebas visuales de expresividad.
- Ajustes orientados a naturalidad conversacional (evitar ojos entrecerrados permanentes y jawOpen excesivo).
- Soporte para sincronizar malla secundaria de boca/dientes en los controladores de lip sync.

## Notas de versionado
Este repositorio ignora carpetas generadas por Unity (Library, Temp, Logs, UserSettings) mediante .gitignore para mantener el historial limpio y compatible con GitHub.
