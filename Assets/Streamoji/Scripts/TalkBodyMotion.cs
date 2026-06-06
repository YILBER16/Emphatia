using UnityEngine;

public class TalkBodyMotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform hips;
    [SerializeField] private Transform spine;
    [SerializeField] private Transform chest;

    [Header("Audio Drive")]
    [SerializeField, Range(0f, 1f)] private float noiseGate = 0.01f;
    [SerializeField, Range(1f, 80f)] private float sensitivity = 22f;
    [SerializeField, Range(1f, 40f)] private float attack = 14f;
    [SerializeField, Range(1f, 40f)] private float release = 10f;

    [Header("Body Motion")]
    [SerializeField, Range(0f, 10f)] private float hipsSwayDeg = 1.2f;
    [SerializeField, Range(0f, 10f)] private float spinePitchDeg = 2.0f;
    [SerializeField, Range(0f, 10f)] private float chestPitchDeg = 2.8f;
    [SerializeField, Range(0f, 10f)] private float chestRollDeg = 1.8f;
    [SerializeField, Range(0f, 4f)] private float baseFrequency = 1.1f;
    [SerializeField, Range(1f, 30f)] private float rotationSmoothing = 12f;

    private readonly float[] samples = new float[256];

    private float speechEnergy;
    private Quaternion hipsBaseLocalRotation;
    private Quaternion spineBaseLocalRotation;
    private Quaternion chestBaseLocalRotation;
    private bool cachedBases;
    private bool useChest;
    private bool chestIsSameAsSpine;

    private void Start()
    {
        CacheBaseRotations();
    }

    private void LateUpdate()
    {
        if (!cachedBases)
            CacheBaseRotations();

        if (!cachedBases)
            return;

        float energy = ReadSpeechEnergy();

        float t = Time.time;
        float motionAmount = 0.35f + (energy * 0.65f);

        float hipsYaw = Mathf.Sin(t * baseFrequency * 0.7f + 0.9f) * hipsSwayDeg * motionAmount;
        float spinePitch = Mathf.Sin(t * baseFrequency * 1.1f) * spinePitchDeg * motionAmount;
        float chestPitch = Mathf.Sin(t * baseFrequency * 1.55f + 0.6f) * chestPitchDeg * motionAmount;
        float chestRoll = Mathf.Sin(t * baseFrequency * 1.25f + 1.4f) * chestRollDeg * motionAmount;

        Quaternion hipsTarget = hipsBaseLocalRotation * Quaternion.Euler(0f, hipsYaw, 0f);
        Quaternion spineTarget = spineBaseLocalRotation * Quaternion.Euler(spinePitch, 0f, 0f);
        Quaternion chestTarget = chestBaseLocalRotation * Quaternion.Euler(chestPitch, 0f, chestRoll);

        float smooth = 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime);

        hips.localRotation = Quaternion.Slerp(hips.localRotation, hipsTarget, smooth);
        spine.localRotation = Quaternion.Slerp(spine.localRotation, spineTarget, smooth);

        if (!useChest)
            return;

        // If chest and spine point to the same transform, apply a softer chest layer to avoid doubling.
        float chestBlend = chestIsSameAsSpine ? smooth * 0.45f : smooth;
        chest.localRotation = Quaternion.Slerp(chest.localRotation, chestTarget, chestBlend);
    }

    private void OnValidate()
    {
        noiseGate = Mathf.Clamp01(noiseGate);
        sensitivity = Mathf.Clamp(sensitivity, 1f, 80f);
        attack = Mathf.Clamp(attack, 1f, 40f);
        release = Mathf.Clamp(release, 1f, 40f);
        rotationSmoothing = Mathf.Clamp(rotationSmoothing, 1f, 30f);
    }

    private void CacheBaseRotations()
    {
        if (hips == null || spine == null)
        {
            cachedBases = false;
            return;
        }

        useChest = chest != null;
        chestIsSameAsSpine = useChest && chest == spine;

        hipsBaseLocalRotation = hips.localRotation;
        spineBaseLocalRotation = spine.localRotation;
        chestBaseLocalRotation = useChest ? chest.localRotation : Quaternion.identity;
        cachedBases = true;
    }

    private float ReadSpeechEnergy()
    {
        if (audioSource == null)
        {
            speechEnergy = Mathf.Lerp(speechEnergy, 0f, Time.deltaTime * 4f);
            return speechEnergy;
        }

        audioSource.GetOutputData(samples, 0);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        float rms = Mathf.Sqrt(sum / samples.Length);
        float gated = Mathf.Max(0f, rms - noiseGate);
        float target = Mathf.Clamp01(gated * sensitivity);

        float speed = target > speechEnergy ? attack : release;
        speechEnergy = Mathf.Lerp(speechEnergy, target, 1f - Mathf.Exp(-speed * Time.deltaTime));

        return speechEnergy;
    }
}
