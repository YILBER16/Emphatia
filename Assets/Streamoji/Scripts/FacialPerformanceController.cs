using UnityEngine;

public class FacialPerformanceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer faceRenderer;
    [SerializeField] private SkinnedMeshRenderer teethRenderer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform headBone;
    [SerializeField] private string jawOpenName = "jawOpen";

    [Header("Global")]
    [SerializeField, Range(0f, 1f)] private float globalWeight = 1f;
    [SerializeField] private bool additiveToCurrent = true;
    [SerializeField] private bool authoritativeEyes = true;
    [SerializeField] private bool syncTeethWithJaw = true;

    [Header("Voice Energy")]
    [SerializeField, Range(0f, 1f)] private float noiseGate = 0.01f;
    [SerializeField, Range(1f, 80f)] private float energySensitivity = 28f;
    [SerializeField, Range(1f, 40f)] private float energyAttack = 16f;
    [SerializeField, Range(1f, 40f)] private float energyRelease = 8f;
    [SerializeField, Range(-1f, 1f)] private float baseMood = 0f;

    [Header("Blink")]
    [SerializeField] private string eyeBlinkLeftName = "eyeBlinkLeft";
    [SerializeField] private string eyeBlinkRightName = "eyeBlinkRight";
    [SerializeField, Range(0f, 1f)] private float eyeBlinkMax = 0.95f;
    [SerializeField, Range(0.8f, 8f)] private float blinkIntervalMin = 2.2f;
    [SerializeField, Range(0.8f, 8f)] private float blinkIntervalMax = 4.5f;
    [SerializeField, Range(6f, 40f)] private float blinkCloseSpeed = 30f;
    [SerializeField, Range(6f, 40f)] private float blinkOpenSpeed = 20f;
    [SerializeField, Range(0f, 1f)] private float doubleBlinkChance = 0.2f;
    [SerializeField, Range(0.05f, 0.6f)] private float doubleBlinkGapMin = 0.09f;
    [SerializeField, Range(0.05f, 0.6f)] private float doubleBlinkGapMax = 0.2f;
    [SerializeField, Range(0f, 1f)] private float longPauseChance = 0.15f;
    [SerializeField, Range(1f, 6f)] private float longPauseMultiplier = 1.8f;

    [Header("Eyes")]
    [SerializeField] private string eyeLookUpLeftName = "eyeLookUpLeft";
    [SerializeField] private string eyeLookUpRightName = "eyeLookUpRight";
    [SerializeField] private string eyeLookDownLeftName = "eyeLookDownLeft";
    [SerializeField] private string eyeLookDownRightName = "eyeLookDownRight";
    [SerializeField] private string eyeLookInLeftName = "eyeLookInLeft";
    [SerializeField] private string eyeLookInRightName = "eyeLookInRight";
    [SerializeField] private string eyeLookOutLeftName = "eyeLookOutLeft";
    [SerializeField] private string eyeLookOutRightName = "eyeLookOutRight";
    [SerializeField] private string eyeSquintLeftName = "eyeSquintLeft";
    [SerializeField] private string eyeSquintRightName = "eyeSquintRight";
    [SerializeField, Range(0f, 1f)] private float gazeMax = 0.1f;
    [SerializeField, Range(0f, 1f)] private float eyeSquintMax = 0f;

    [Header("Brows/Cheeks/Mouth Expression")]
    [SerializeField] private string browInnerUpName = "browInnerUp";
    [SerializeField] private string browDownLeftName = "browDownLeft";
    [SerializeField] private string browDownRightName = "browDownRight";
    [SerializeField] private string cheekSquintLeftName = "cheekSquintLeft";
    [SerializeField] private string cheekSquintRightName = "cheekSquintRight";
    [SerializeField] private string mouthSmileLeftName = "mouthSmileLeft";
    [SerializeField] private string mouthSmileRightName = "mouthSmileRight";
    [SerializeField, Range(0f, 1f)] private float browInnerUpMax = 0.2f;
    [SerializeField, Range(0f, 1f)] private float browDownMax = 0.14f;
    [SerializeField, Range(0f, 1f)] private float cheekSquintMax = 0.12f;
    [SerializeField, Range(0f, 1f)] private float mouthSmileMax = 0.08f;

    [Header("Head Motion")]
    [SerializeField, Range(0f, 1f)] private float headMotionWeight = 0.5f;
    [SerializeField, Range(0f, 8f)] private float headNodAmplitude = 2.0f;
    [SerializeField, Range(0f, 8f)] private float headTiltAmplitude = 1.6f;
    [SerializeField, Range(0f, 4f)] private float headNoiseFrequency = 1.15f;
    [SerializeField, Range(1f, 30f)] private float headMotionSmoothing = 10f;
    [SerializeField, Range(1f, 30f)] private float headSpeechSmoothing = 8f;

    private readonly float[] samples = new float[256];

    private int eyeBlinkLeftIndex = -1;
    private int eyeBlinkRightIndex = -1;
    private int jawOpenFaceIndex = -1;
    private int jawOpenTeethIndex = -1;
    private int eyeLookUpLeftIndex = -1;
    private int eyeLookUpRightIndex = -1;
    private int eyeLookDownLeftIndex = -1;
    private int eyeLookDownRightIndex = -1;
    private int eyeLookInLeftIndex = -1;
    private int eyeLookInRightIndex = -1;
    private int eyeLookOutLeftIndex = -1;
    private int eyeLookOutRightIndex = -1;
    private int eyeSquintLeftIndex = -1;
    private int eyeSquintRightIndex = -1;
    private int browInnerUpIndex = -1;
    private int browDownLeftIndex = -1;
    private int browDownRightIndex = -1;
    private int cheekSquintLeftIndex = -1;
    private int cheekSquintRightIndex = -1;
    private int mouthSmileLeftIndex = -1;
    private int mouthSmileRightIndex = -1;

    private float energyCurrent;
    private float energyPrevious;
    private float speechIntensity;

    private float blinkWeight;
    private float blinkTimer;
    private float nextBlinkTime;
    private bool blinkClosing;
    private bool blinkInProgress;
    private bool pendingDoubleBlink;

    private float gazeXCurrent;
    private float gazeYCurrent;
    private float gazeXTarget;
    private float gazeYTarget;
    private float nextSaccadeTime;

    private float eyeSquintLeftCurrent;
    private float eyeSquintRightCurrent;
    private float browInnerUpCurrent;
    private float browDownLeftCurrent;
    private float browDownRightCurrent;
    private float cheekSquintLeftCurrent;
    private float cheekSquintRightCurrent;
    private float mouthSmileLeftCurrent;
    private float mouthSmileRightCurrent;

    private float eyeBlinkLeftApplied;
    private float eyeBlinkRightApplied;
    private float eyeLookUpLeftApplied;
    private float eyeLookUpRightApplied;
    private float eyeLookDownLeftApplied;
    private float eyeLookDownRightApplied;
    private float eyeLookInLeftApplied;
    private float eyeLookInRightApplied;
    private float eyeLookOutLeftApplied;
    private float eyeLookOutRightApplied;
    private float eyeSquintLeftApplied;
    private float eyeSquintRightApplied;
    private float browInnerUpApplied;
    private float browDownLeftApplied;
    private float browDownRightApplied;
    private float cheekSquintLeftApplied;
    private float cheekSquintRightApplied;
    private float mouthSmileLeftApplied;
    private float mouthSmileRightApplied;

    private Quaternion headBaseLocalRotation;
    private bool headRotationCached;
    private float headSpeechCurrent = 0.5f;

    private void Start()
    {
        CacheBlendshapeIndices();
        ScheduleNextBlink();
        ScheduleNextSaccade();

        if (headBone != null)
        {
            headBaseLocalRotation = headBone.localRotation;
            headRotationCached = true;
        }
    }

    private void Update()
    {
        UpdateVoiceEnergy();
        UpdateBlink();
        UpdateGazeTargets();
        UpdateExpressionState();
        UpdateHeadMotion();
    }

    private void LateUpdate()
    {
        if (faceRenderer == null)
            return;

        // Eyes
        ApplyOverlay(eyeBlinkLeftIndex, blinkWeight * eyeBlinkMax, ref eyeBlinkLeftApplied, !authoritativeEyes);
        ApplyOverlay(eyeBlinkRightIndex, blinkWeight * eyeBlinkMax, ref eyeBlinkRightApplied, !authoritativeEyes);

        float lookUp = Mathf.Max(0f, gazeYCurrent) * gazeMax;
        float lookDown = Mathf.Max(0f, -gazeYCurrent) * gazeMax;

        float lookInLeft = Mathf.Max(0f, gazeXCurrent) * gazeMax;
        float lookOutLeft = Mathf.Max(0f, -gazeXCurrent) * gazeMax;
        float lookInRight = Mathf.Max(0f, -gazeXCurrent) * gazeMax;
        float lookOutRight = Mathf.Max(0f, gazeXCurrent) * gazeMax;

        ApplyOverlay(eyeLookUpLeftIndex, lookUp, ref eyeLookUpLeftApplied, !authoritativeEyes);
        ApplyOverlay(eyeLookUpRightIndex, lookUp, ref eyeLookUpRightApplied, !authoritativeEyes);
        ApplyOverlay(eyeLookDownLeftIndex, lookDown, ref eyeLookDownLeftApplied, !authoritativeEyes);
        ApplyOverlay(eyeLookDownRightIndex, lookDown, ref eyeLookDownRightApplied, !authoritativeEyes);
        ApplyOverlay(eyeLookInLeftIndex, lookInLeft, ref eyeLookInLeftApplied, !authoritativeEyes);
        ApplyOverlay(eyeLookInRightIndex, lookInRight, ref eyeLookInRightApplied, !authoritativeEyes);
        ApplyOverlay(eyeLookOutLeftIndex, lookOutLeft, ref eyeLookOutLeftApplied, !authoritativeEyes);
        ApplyOverlay(eyeLookOutRightIndex, lookOutRight, ref eyeLookOutRightApplied, !authoritativeEyes);
        ApplyOverlay(eyeSquintLeftIndex, eyeSquintLeftCurrent, ref eyeSquintLeftApplied, !authoritativeEyes);
        ApplyOverlay(eyeSquintRightIndex, eyeSquintRightCurrent, ref eyeSquintRightApplied, !authoritativeEyes);

        // Expression
        ApplyOverlay(browInnerUpIndex, browInnerUpCurrent, ref browInnerUpApplied, additiveToCurrent);
        ApplyOverlay(browDownLeftIndex, browDownLeftCurrent, ref browDownLeftApplied, additiveToCurrent);
        ApplyOverlay(browDownRightIndex, browDownRightCurrent, ref browDownRightApplied, additiveToCurrent);
        ApplyOverlay(cheekSquintLeftIndex, cheekSquintLeftCurrent, ref cheekSquintLeftApplied, additiveToCurrent);
        ApplyOverlay(cheekSquintRightIndex, cheekSquintRightCurrent, ref cheekSquintRightApplied, additiveToCurrent);
        ApplyOverlay(mouthSmileLeftIndex, mouthSmileLeftCurrent, ref mouthSmileLeftApplied, additiveToCurrent);
        ApplyOverlay(mouthSmileRightIndex, mouthSmileRightCurrent, ref mouthSmileRightApplied, additiveToCurrent);

        if (syncTeethWithJaw && teethRenderer != null && jawOpenFaceIndex >= 0 && jawOpenTeethIndex >= 0)
        {
            float jaw = faceRenderer.GetBlendShapeWeight(jawOpenFaceIndex);
            teethRenderer.SetBlendShapeWeight(jawOpenTeethIndex, jaw);
        }
    }

    private void OnValidate()
    {
        globalWeight = Mathf.Clamp01(globalWeight);
        noiseGate = Mathf.Clamp01(noiseGate);
        eyeBlinkMax = Mathf.Clamp01(eyeBlinkMax);
        blinkIntervalMin = Mathf.Max(0.1f, blinkIntervalMin);
        blinkIntervalMax = Mathf.Max(blinkIntervalMin, blinkIntervalMax);
        doubleBlinkGapMin = Mathf.Clamp(doubleBlinkGapMin, 0.05f, 0.6f);
        doubleBlinkGapMax = Mathf.Clamp(doubleBlinkGapMax, doubleBlinkGapMin, 0.6f);
        doubleBlinkChance = Mathf.Clamp01(doubleBlinkChance);
        longPauseChance = Mathf.Clamp01(longPauseChance);
        longPauseMultiplier = Mathf.Clamp(longPauseMultiplier, 1f, 6f);
        gazeMax = Mathf.Clamp01(gazeMax);
        eyeSquintMax = Mathf.Clamp01(eyeSquintMax);
        browInnerUpMax = Mathf.Clamp01(browInnerUpMax);
        browDownMax = Mathf.Clamp01(browDownMax);
        cheekSquintMax = Mathf.Clamp01(cheekSquintMax);
        mouthSmileMax = Mathf.Clamp01(mouthSmileMax);
        headMotionSmoothing = Mathf.Clamp(headMotionSmoothing, 1f, 30f);
        headSpeechSmoothing = Mathf.Clamp(headSpeechSmoothing, 1f, 30f);

        if (!Application.isPlaying)
            CacheBlendshapeIndices();
    }

    private void UpdateVoiceEnergy()
    {
        if (audioSource == null)
        {
            speechIntensity = Mathf.Lerp(speechIntensity, 0f, Time.deltaTime * 4f);
            return;
        }

        audioSource.GetOutputData(samples, 0);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        float rms = Mathf.Sqrt(sum / samples.Length);
        float gated = Mathf.Max(0f, rms - noiseGate);
        float targetEnergy = Mathf.Clamp01(gated * energySensitivity);

        float speed = targetEnergy > energyCurrent ? energyAttack : energyRelease;
        energyCurrent = Mathf.Lerp(energyCurrent, targetEnergy, 1f - Mathf.Exp(-speed * Time.deltaTime));
        speechIntensity = energyCurrent;
    }

    private void UpdateBlink()
    {
        blinkTimer += Time.deltaTime;

        if (!blinkInProgress && blinkTimer >= nextBlinkTime)
        {
            blinkInProgress = true;
            blinkClosing = true;
        }

        if (!blinkInProgress)
            return;

        if (blinkClosing)
        {
            blinkWeight = Mathf.MoveTowards(blinkWeight, 1f, blinkCloseSpeed * Time.deltaTime);
            if (blinkWeight >= 0.999f)
                blinkClosing = false;
        }
        else
        {
            blinkWeight = Mathf.MoveTowards(blinkWeight, 0f, blinkOpenSpeed * Time.deltaTime);
            if (blinkWeight <= 0.001f)
            {
                blinkWeight = 0f;
                blinkInProgress = false;
                ScheduleNextBlink();
            }
        }
    }

    private void UpdateGazeTargets()
    {
        if (Time.time >= nextSaccadeTime)
        {
            gazeXTarget = Random.Range(-0.8f, 0.8f);
            gazeYTarget = Random.Range(-0.2f, 0.2f);
            ScheduleNextSaccade();
        }

        float gazeSmooth = 1f - Mathf.Exp(-6f * Time.deltaTime);
        gazeXCurrent = Mathf.Lerp(gazeXCurrent, gazeXTarget, gazeSmooth);
        gazeYCurrent = Mathf.Lerp(gazeYCurrent, gazeYTarget, gazeSmooth);
    }

    private void UpdateExpressionState()
    {
        float intensity = speechIntensity;
        float rise = Mathf.Max(0f, intensity - energyPrevious);
        energyPrevious = intensity;

        float valence = Mathf.Clamp(baseMood + (Mathf.Sin(Time.time * 0.23f) * 0.08f), -1f, 1f);
        float positive = Mathf.Max(0f, valence);
        float negative = Mathf.Max(0f, -valence);

        float smileTarget = Mathf.Clamp01(positive * 0.45f + intensity * 0.12f) * mouthSmileMax;
        float browUpTarget = Mathf.Clamp01(intensity * 0.22f + rise * 1.1f) * browInnerUpMax;
        float browDownTarget = Mathf.Clamp01(negative * 0.45f + intensity * 0.06f) * browDownMax;
        float cheekTarget = Mathf.Clamp01(smileTarget * 1.4f + intensity * 0.1f) * cheekSquintMax;
        float eyeSquintTarget = Mathf.Clamp01(smileTarget * 1.2f + intensity * 0.05f) * eyeSquintMax;

        eyeSquintLeftCurrent = SmoothValue(eyeSquintLeftCurrent, eyeSquintTarget, 18f, 10f);
        eyeSquintRightCurrent = SmoothValue(eyeSquintRightCurrent, eyeSquintTarget * 0.98f, 18f, 10f);
        browInnerUpCurrent = SmoothValue(browInnerUpCurrent, browUpTarget, 14f, 8f);
        browDownLeftCurrent = SmoothValue(browDownLeftCurrent, browDownTarget * 0.96f, 12f, 8f);
        browDownRightCurrent = SmoothValue(browDownRightCurrent, browDownTarget, 12f, 8f);
        cheekSquintLeftCurrent = SmoothValue(cheekSquintLeftCurrent, cheekTarget, 16f, 9f);
        cheekSquintRightCurrent = SmoothValue(cheekSquintRightCurrent, cheekTarget * 0.97f, 16f, 9f);
        mouthSmileLeftCurrent = SmoothValue(mouthSmileLeftCurrent, smileTarget, 10f, 6f);
        mouthSmileRightCurrent = SmoothValue(mouthSmileRightCurrent, smileTarget * 0.98f, 10f, 6f);
    }

    private void UpdateHeadMotion()
    {
        if (headBone == null || !headRotationCached)
            return;

        float t = Time.time;
        float speechBoostTarget = 0.5f + speechIntensity * 0.5f;
        float speechSmooth = 1f - Mathf.Exp(-headSpeechSmoothing * Time.deltaTime);
        headSpeechCurrent = Mathf.Lerp(headSpeechCurrent, speechBoostTarget, speechSmooth);
        float weight = headMotionWeight * globalWeight;

        float nod = Mathf.Sin(t * headNoiseFrequency * 1.2f) * headNodAmplitude * headSpeechCurrent * weight;
        float tilt = Mathf.Sin(t * headNoiseFrequency * 0.9f + 1.37f) * headTiltAmplitude * headSpeechCurrent * weight;

        Quaternion additive = Quaternion.Euler(nod, 0f, tilt);
        Quaternion targetRotation = headBaseLocalRotation * additive;
        float motionSmooth = 1f - Mathf.Exp(-headMotionSmoothing * Time.deltaTime);
        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, targetRotation, motionSmooth);
    }

    private float SmoothValue(float current, float target, float attack, float release)
    {
        float clampedTarget = Mathf.Clamp01(target);
        float speed = clampedTarget > current ? attack : release;
        return Mathf.Lerp(current, clampedTarget, 1f - Mathf.Exp(-speed * Time.deltaTime));
    }

    private void ApplyOverlay(int blendshapeIndex, float overlay, ref float lastApplied, bool additive)
    {
        if (blendshapeIndex < 0)
            return;

        float scaledOverlay = Mathf.Clamp01(overlay) * globalWeight;
        float current = faceRenderer.GetBlendShapeWeight(blendshapeIndex);
        float baseValue = additive ? Mathf.Clamp01(current - lastApplied) : 0f;
        float finalValue = Mathf.Clamp01(baseValue + scaledOverlay);
        faceRenderer.SetBlendShapeWeight(blendshapeIndex, finalValue);
        lastApplied = scaledOverlay;
    }

    private void CacheBlendshapeIndices()
    {
        eyeBlinkLeftIndex = -1;
        eyeBlinkRightIndex = -1;
        jawOpenFaceIndex = -1;
        jawOpenTeethIndex = -1;
        eyeLookUpLeftIndex = -1;
        eyeLookUpRightIndex = -1;
        eyeLookDownLeftIndex = -1;
        eyeLookDownRightIndex = -1;
        eyeLookInLeftIndex = -1;
        eyeLookInRightIndex = -1;
        eyeLookOutLeftIndex = -1;
        eyeLookOutRightIndex = -1;
        eyeSquintLeftIndex = -1;
        eyeSquintRightIndex = -1;
        browInnerUpIndex = -1;
        browDownLeftIndex = -1;
        browDownRightIndex = -1;
        cheekSquintLeftIndex = -1;
        cheekSquintRightIndex = -1;
        mouthSmileLeftIndex = -1;
        mouthSmileRightIndex = -1;

        if (faceRenderer == null || faceRenderer.sharedMesh == null)
            return;

        Mesh mesh = faceRenderer.sharedMesh;
        jawOpenFaceIndex = mesh.GetBlendShapeIndex(jawOpenName);
        eyeBlinkLeftIndex = mesh.GetBlendShapeIndex(eyeBlinkLeftName);
        eyeBlinkRightIndex = mesh.GetBlendShapeIndex(eyeBlinkRightName);
        eyeLookUpLeftIndex = mesh.GetBlendShapeIndex(eyeLookUpLeftName);
        eyeLookUpRightIndex = mesh.GetBlendShapeIndex(eyeLookUpRightName);
        eyeLookDownLeftIndex = mesh.GetBlendShapeIndex(eyeLookDownLeftName);
        eyeLookDownRightIndex = mesh.GetBlendShapeIndex(eyeLookDownRightName);
        eyeLookInLeftIndex = mesh.GetBlendShapeIndex(eyeLookInLeftName);
        eyeLookInRightIndex = mesh.GetBlendShapeIndex(eyeLookInRightName);
        eyeLookOutLeftIndex = mesh.GetBlendShapeIndex(eyeLookOutLeftName);
        eyeLookOutRightIndex = mesh.GetBlendShapeIndex(eyeLookOutRightName);
        eyeSquintLeftIndex = mesh.GetBlendShapeIndex(eyeSquintLeftName);
        eyeSquintRightIndex = mesh.GetBlendShapeIndex(eyeSquintRightName);
        browInnerUpIndex = mesh.GetBlendShapeIndex(browInnerUpName);
        browDownLeftIndex = mesh.GetBlendShapeIndex(browDownLeftName);
        browDownRightIndex = mesh.GetBlendShapeIndex(browDownRightName);
        cheekSquintLeftIndex = mesh.GetBlendShapeIndex(cheekSquintLeftName);
        cheekSquintRightIndex = mesh.GetBlendShapeIndex(cheekSquintRightName);
        mouthSmileLeftIndex = mesh.GetBlendShapeIndex(mouthSmileLeftName);
        mouthSmileRightIndex = mesh.GetBlendShapeIndex(mouthSmileRightName);

        if (teethRenderer != null && teethRenderer.sharedMesh != null)
            jawOpenTeethIndex = teethRenderer.sharedMesh.GetBlendShapeIndex(jawOpenName);
    }

    private void ScheduleNextBlink()
    {
        blinkTimer = 0f;

        if (pendingDoubleBlink)
        {
            pendingDoubleBlink = false;
            nextBlinkTime = Random.Range(doubleBlinkGapMin, doubleBlinkGapMax);
            return;
        }

        float interval = Random.Range(blinkIntervalMin, blinkIntervalMax);
        pendingDoubleBlink = Random.value < doubleBlinkChance;

        if (Random.value < longPauseChance)
            interval *= longPauseMultiplier;

        nextBlinkTime = interval;
    }

    private void ScheduleNextSaccade()
    {
        nextSaccadeTime = Time.time + Random.Range(0.7f, 2f);
    }
}
