using UnityEngine;

public class SimpleJawOpenFromAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SkinnedMeshRenderer faceRenderer;
    [SerializeField] private SkinnedMeshRenderer secondaryMouthRenderer;
    [SerializeField] private string jawBlendshapeName = "jawOpen";

    [Header("Secondary Mouth Mesh")]
    [SerializeField] private bool syncSecondaryMouthMesh = true;

    [Header("Primary Jaw")]
    [SerializeField, Range(1f, 120f)] private float sensitivity = 14f;
    [SerializeField, Range(0f, 1f)] private float noiseGate = 0.008f;
    [SerializeField, Range(0.5f, 3f)] private float responseCurve = 1.35f;
    [SerializeField, Range(0f, 1f)] private float maxWeight = 0.24f;
    [SerializeField, Range(0f, 1f)] private float hardSafetyCap = 0.3f;
    [SerializeField, Range(0f, 0.2f)] private float jawCloseBias = 0.03f;
    [SerializeField, Range(0f, 0.9f)] private float sustainedSpeechJawReduction = 0.2f;
    [SerializeField, Range(1f, 40f)] private float attackSpeed = 14f;
    [SerializeField, Range(1f, 40f)] private float releaseSpeed = 24f;

    [Header("Secondary Mouth (Optional)")]
    [SerializeField] private bool driveSecondaryBlendshapes = true;
    [SerializeField] private string mouthCloseBlendshapeName = "mouthClose";
    [SerializeField] private string mouthFunnelBlendshapeName = "mouthFunnel";
    [SerializeField] private string mouthStretchLeftBlendshapeName = "mouthStretchLeft";
    [SerializeField] private string mouthStretchRightBlendshapeName = "mouthStretchRight";
    [SerializeField] private string mouthSmileLeftBlendshapeName = "mouthSmileLeft";
    [SerializeField] private string mouthSmileRightBlendshapeName = "mouthSmileRight";
    [SerializeField, Range(0f, 1f)] private float mouthCloseMax = 0.18f;
    [SerializeField, Range(0f, 1f)] private float mouthFunnelMax = 0.16f;
    [SerializeField, Range(0f, 1f)] private float mouthStretchMax = 0.1f;
    [SerializeField, Range(0f, 1f)] private float mouthSmileMax = 0.05f;
    [SerializeField, Range(20f, 300f)] private float spectrumBoost = 120f;

    private readonly float[] samples = new float[256];
    private readonly float[] spectrum = new float[256];

    private int jawIndex = -1;
    private int jawSecondaryIndex = -1;
    private int mouthCloseIndex = -1;
    private int mouthCloseSecondaryIndex = -1;
    private int mouthFunnelIndex = -1;
    private int mouthFunnelSecondaryIndex = -1;
    private int mouthStretchLeftIndex = -1;
    private int mouthStretchLeftSecondaryIndex = -1;
    private int mouthStretchRightIndex = -1;
    private int mouthStretchRightSecondaryIndex = -1;
    private int mouthSmileLeftIndex = -1;
    private int mouthSmileLeftSecondaryIndex = -1;
    private int mouthSmileRightIndex = -1;
    private int mouthSmileRightSecondaryIndex = -1;

    private float jawCurrent;
    private float mouthCloseCurrent;
    private float mouthFunnelCurrent;
    private float mouthStretchLeftCurrent;
    private float mouthStretchRightCurrent;
    private float mouthSmileLeftCurrent;
    private float mouthSmileRightCurrent;

    private void Start()
    {
        CacheBlendshapeIndices();
    }

    private void Update()
    {
        if (audioSource == null || faceRenderer == null || jawIndex < 0)
            return;

        audioSource.GetOutputData(samples, 0);
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        float rms = Mathf.Sqrt(sum / samples.Length);
        float gated = Mathf.Max(0f, rms - noiseGate);
        float articulation = Mathf.Clamp01(gated * sensitivity);
        articulation = Mathf.Pow(articulation, responseCurve);

        float low = GetSpectrumBand(2, 16);
        float mid = GetSpectrumBand(16, 56);
        float high = GetSpectrumBand(56, 128);

        float roundness = Mathf.Clamp01((low - (mid * 0.6f)) * 0.9f + articulation * 0.2f);
        float stretch = Mathf.Clamp01((high - (low * 0.35f)) * 1.1f + articulation * 0.15f);
        float smile = Mathf.Clamp01(high * 0.45f + articulation * 0.1f);
        float close = Mathf.Clamp01((1f - articulation) * 0.28f);

        float safeMax = Mathf.Min(Mathf.Clamp01(Mathf.Min(maxWeight, hardSafetyCap)), 0.3f);
        float jawTarget = Mathf.Max(0f, articulation * safeMax - jawCloseBias);
        jawTarget *= Mathf.Lerp(1f, 1f - sustainedSpeechJawReduction, articulation);
        ApplySmoothedBlendshape(jawIndex, jawSecondaryIndex, ref jawCurrent, jawTarget);

        if (!driveSecondaryBlendshapes)
            return;

        float speechAmount = Mathf.Clamp01(articulation * 2f);

        float mouthCloseTarget = speechAmount * close * mouthCloseMax;
        float mouthFunnelTarget = speechAmount * roundness * mouthFunnelMax;
        float mouthStretchTarget = speechAmount * stretch * mouthStretchMax;
        float mouthSmileTarget = speechAmount * smile * mouthSmileMax;

        ApplySmoothedBlendshape(mouthCloseIndex, mouthCloseSecondaryIndex, ref mouthCloseCurrent, mouthCloseTarget);
        ApplySmoothedBlendshape(mouthFunnelIndex, mouthFunnelSecondaryIndex, ref mouthFunnelCurrent, mouthFunnelTarget);
        ApplySmoothedBlendshape(mouthStretchLeftIndex, mouthStretchLeftSecondaryIndex, ref mouthStretchLeftCurrent, mouthStretchTarget);
        ApplySmoothedBlendshape(mouthStretchRightIndex, mouthStretchRightSecondaryIndex, ref mouthStretchRightCurrent, mouthStretchTarget);
        ApplySmoothedBlendshape(mouthSmileLeftIndex, mouthSmileLeftSecondaryIndex, ref mouthSmileLeftCurrent, mouthSmileTarget);
        ApplySmoothedBlendshape(mouthSmileRightIndex, mouthSmileRightSecondaryIndex, ref mouthSmileRightCurrent, mouthSmileTarget);
    }

    private void OnValidate()
    {
        maxWeight = Mathf.Clamp01(maxWeight);
        hardSafetyCap = Mathf.Clamp(hardSafetyCap, 0f, 0.3f);
        jawCloseBias = Mathf.Clamp(jawCloseBias, 0f, 0.2f);
        sustainedSpeechJawReduction = Mathf.Clamp(sustainedSpeechJawReduction, 0f, 0.9f);
        mouthCloseMax = Mathf.Clamp01(mouthCloseMax);
        mouthFunnelMax = Mathf.Clamp01(mouthFunnelMax);
        mouthStretchMax = Mathf.Clamp01(mouthStretchMax);
        mouthSmileMax = Mathf.Clamp01(mouthSmileMax);

        if (!Application.isPlaying)
            CacheBlendshapeIndices();
    }

    private float GetSpectrumBand(int start, int end)
    {
        if (end <= start)
            return 0f;

        float sum = 0f;
        int clampedStart = Mathf.Clamp(start, 0, spectrum.Length - 1);
        int clampedEnd = Mathf.Clamp(end, clampedStart + 1, spectrum.Length);

        for (int i = clampedStart; i < clampedEnd; i++)
            sum += spectrum[i];

        float avg = sum / (clampedEnd - clampedStart);
        return Mathf.Clamp01(avg * spectrumBoost);
    }

    private void ApplySmoothedBlendshape(int blendshapeIndex, int secondaryBlendshapeIndex, ref float current, float target)
    {
        if (blendshapeIndex < 0)
            return;

        float clampedTarget = Mathf.Clamp01(target);
        float smooth = clampedTarget > current ? attackSpeed : releaseSpeed;
        current = Mathf.Lerp(current, clampedTarget, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        faceRenderer.SetBlendShapeWeight(blendshapeIndex, current);

        if (syncSecondaryMouthMesh && secondaryMouthRenderer != null && secondaryBlendshapeIndex >= 0)
            secondaryMouthRenderer.SetBlendShapeWeight(secondaryBlendshapeIndex, current);
    }

    private void CacheBlendshapeIndices()
    {
        jawIndex = -1;
        jawSecondaryIndex = -1;
        mouthCloseIndex = -1;
        mouthCloseSecondaryIndex = -1;
        mouthFunnelIndex = -1;
        mouthFunnelSecondaryIndex = -1;
        mouthStretchLeftIndex = -1;
        mouthStretchLeftSecondaryIndex = -1;
        mouthStretchRightIndex = -1;
        mouthStretchRightSecondaryIndex = -1;
        mouthSmileLeftIndex = -1;
        mouthSmileLeftSecondaryIndex = -1;
        mouthSmileRightIndex = -1;
        mouthSmileRightSecondaryIndex = -1;

        if (faceRenderer == null || faceRenderer.sharedMesh == null)
            return;

        jawIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(jawBlendshapeName);
        mouthCloseIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(mouthCloseBlendshapeName);
        mouthFunnelIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(mouthFunnelBlendshapeName);
        mouthStretchLeftIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(mouthStretchLeftBlendshapeName);
        mouthStretchRightIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(mouthStretchRightBlendshapeName);
        mouthSmileLeftIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(mouthSmileLeftBlendshapeName);
        mouthSmileRightIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(mouthSmileRightBlendshapeName);

        if (secondaryMouthRenderer != null && secondaryMouthRenderer.sharedMesh != null)
        {
            jawSecondaryIndex = secondaryMouthRenderer.sharedMesh.GetBlendShapeIndex(jawBlendshapeName);
            mouthCloseSecondaryIndex = secondaryMouthRenderer.sharedMesh.GetBlendShapeIndex(mouthCloseBlendshapeName);
            mouthFunnelSecondaryIndex = secondaryMouthRenderer.sharedMesh.GetBlendShapeIndex(mouthFunnelBlendshapeName);
            mouthStretchLeftSecondaryIndex = secondaryMouthRenderer.sharedMesh.GetBlendShapeIndex(mouthStretchLeftBlendshapeName);
            mouthStretchRightSecondaryIndex = secondaryMouthRenderer.sharedMesh.GetBlendShapeIndex(mouthStretchRightBlendshapeName);
            mouthSmileLeftSecondaryIndex = secondaryMouthRenderer.sharedMesh.GetBlendShapeIndex(mouthSmileLeftBlendshapeName);
            mouthSmileRightSecondaryIndex = secondaryMouthRenderer.sharedMesh.GetBlendShapeIndex(mouthSmileRightBlendshapeName);
        }

        if (jawIndex < 0)
            Debug.LogWarning($"Blendshape '{jawBlendshapeName}' no encontrado en {faceRenderer.sharedMesh.name}", this);
    }
}
