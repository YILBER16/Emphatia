using System;
using System.Reflection;
using UnityEngine;

public class OVRLipSyncToARKitBlendshapes : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer faceRenderer;
    [SerializeField] private SkinnedMeshRenderer secondaryMouthRenderer;
    [SerializeField] private MonoBehaviour ovrLipSyncContext;

    [Header("Secondary Mouth Mesh")]
    [SerializeField] private bool syncSecondaryMouthMesh = true;

    [Header("Timing")]
    [SerializeField, Range(1f, 40f)] private float attackSpeed = 18f;
    [SerializeField, Range(1f, 40f)] private float releaseSpeed = 30f;

    [Header("ARKit Blendshape Names")]
    [SerializeField] private string jawOpenName = "jawOpen";
    [SerializeField] private string mouthCloseName = "mouthClose";
    [SerializeField] private string mouthFunnelName = "mouthFunnel";
    [SerializeField] private string mouthStretchLeftName = "mouthStretchLeft";
    [SerializeField] private string mouthStretchRightName = "mouthStretchRight";
    [SerializeField] private string mouthSmileLeftName = "mouthSmileLeft";
    [SerializeField] private string mouthSmileRightName = "mouthSmileRight";
    [SerializeField] private string mouthPressLeftName = "mouthPressLeft";
    [SerializeField] private string mouthPressRightName = "mouthPressRight";
    [SerializeField] private string tongueOutName = "tongueOut";

    [Header("Jaw Control")]
    [SerializeField, Range(0f, 1f)] private float jawOpenMax = 0.34f;
    [SerializeField, Range(0f, 1f)] private float jawHardCap = 0.28f;
    [SerializeField, Range(0.8f, 3f)] private float jawCompression = 2.1f;
    [SerializeField, Range(0f, 0.2f)] private float jawCloseBias = 0.03f;
    [SerializeField, Range(0f, 0.9f)] private float sustainedVowelJawReduction = 0.22f;

    [Header("Max Weights (0-1)")]
    [SerializeField, Range(0f, 1f)] private float mouthCloseMax = 0.6f;
    [SerializeField, Range(0f, 1f)] private float mouthFunnelMax = 0.45f;
    [SerializeField, Range(0f, 1f)] private float mouthStretchMax = 0.35f;
    [SerializeField, Range(0f, 1f)] private float mouthSmileMax = 0.14f;
    [SerializeField, Range(0f, 1f)] private float mouthPressMax = 0.25f;
    [SerializeField, Range(0f, 1f)] private float tongueOutMax = 0.25f;

    private Type contextBaseType;
    private MethodInfo getCurrentPhonemeFrameMethod;
    private FieldInfo visemesField;
    private PropertyInfo visemesProperty;

    private int jawOpenIndex = -1;
    private int jawOpenSecondaryIndex = -1;
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
    private int mouthPressLeftIndex = -1;
    private int mouthPressLeftSecondaryIndex = -1;
    private int mouthPressRightIndex = -1;
    private int mouthPressRightSecondaryIndex = -1;
    private int tongueOutIndex = -1;
    private int tongueOutSecondaryIndex = -1;

    private float jawOpenCurrent;
    private float mouthCloseCurrent;
    private float mouthFunnelCurrent;
    private float mouthStretchLeftCurrent;
    private float mouthStretchRightCurrent;
    private float mouthSmileLeftCurrent;
    private float mouthSmileRightCurrent;
    private float mouthPressLeftCurrent;
    private float mouthPressRightCurrent;
    private float tongueOutCurrent;

    private bool ovrReady;
    private bool warnedMissingOvr;

    private void Start()
    {
        CacheBlendshapeIndices();
        TrySetupOvrBridge();
    }

    private void Update()
    {
        if (faceRenderer == null)
            return;

        if (!ovrReady)
        {
            TrySetupOvrBridge();
            return;
        }

        float[] visemes = ReadVisemes();
        if (visemes == null || visemes.Length < 15)
            return;

        float pp = visemes[1];
        float ff = visemes[2];
        float th = visemes[3];
        float dd = visemes[4];
        float ch = visemes[6];
        float ss = visemes[7];
        float aa = visemes[10];
        float eh = visemes[11];
        float ih = visemes[12];
        float oh = visemes[13];
        float ou = visemes[14];

        float vowelEnergy = Mathf.Clamp01(aa * 1f + eh * 0.75f + ih * 0.65f + oh * 0.8f + ou * 0.75f);
        float consonantEnergy = Mathf.Clamp01(dd * 0.2f + ch * 0.2f + ss * 0.15f + ff * 0.1f);
        float jawRaw = Mathf.Clamp01(vowelEnergy + consonantEnergy * 0.2f);
        float jawShaped = Mathf.Pow(jawRaw, jawCompression);
        float jawTarget = Mathf.Min(jawShaped * jawOpenMax, Mathf.Min(jawHardCap, 0.28f));
        jawTarget = Mathf.Max(0f, jawTarget - jawCloseBias);
        jawTarget *= Mathf.Lerp(1f, 1f - sustainedVowelJawReduction, vowelEnergy);
        float closeTarget = Mathf.Clamp01(pp) * mouthCloseMax;
        float funnelTarget = Mathf.Clamp01(ou * 0.85f + oh * 0.55f) * mouthFunnelMax;
        float stretchTarget = Mathf.Clamp01(eh * 0.75f + ih * 0.65f + ss * 0.35f + ff * 0.25f) * mouthStretchMax;
        float smileTarget = Mathf.Clamp01(eh * 0.45f + ih * 0.35f) * mouthSmileMax;
        float pressTarget = Mathf.Clamp01(ff * 0.9f + ss * 0.2f) * mouthPressMax;
        float tongueTarget = Mathf.Clamp01(th) * tongueOutMax;

        ApplySmoothed(jawOpenIndex, jawOpenSecondaryIndex, ref jawOpenCurrent, jawTarget);
        ApplySmoothed(mouthCloseIndex, mouthCloseSecondaryIndex, ref mouthCloseCurrent, closeTarget);
        ApplySmoothed(mouthFunnelIndex, mouthFunnelSecondaryIndex, ref mouthFunnelCurrent, funnelTarget);
        ApplySmoothed(mouthStretchLeftIndex, mouthStretchLeftSecondaryIndex, ref mouthStretchLeftCurrent, stretchTarget);
        ApplySmoothed(mouthStretchRightIndex, mouthStretchRightSecondaryIndex, ref mouthStretchRightCurrent, stretchTarget);
        ApplySmoothed(mouthSmileLeftIndex, mouthSmileLeftSecondaryIndex, ref mouthSmileLeftCurrent, smileTarget);
        ApplySmoothed(mouthSmileRightIndex, mouthSmileRightSecondaryIndex, ref mouthSmileRightCurrent, smileTarget);
        ApplySmoothed(mouthPressLeftIndex, mouthPressLeftSecondaryIndex, ref mouthPressLeftCurrent, pressTarget);
        ApplySmoothed(mouthPressRightIndex, mouthPressRightSecondaryIndex, ref mouthPressRightCurrent, pressTarget);
        ApplySmoothed(tongueOutIndex, tongueOutSecondaryIndex, ref tongueOutCurrent, tongueTarget);
    }

    private void OnValidate()
    {
        jawOpenMax = Mathf.Clamp01(jawOpenMax);
        jawHardCap = Mathf.Clamp(jawHardCap, 0f, 0.28f);
        jawCompression = Mathf.Clamp(jawCompression, 0.8f, 3f);
        jawCloseBias = Mathf.Clamp(jawCloseBias, 0f, 0.2f);
        sustainedVowelJawReduction = Mathf.Clamp(sustainedVowelJawReduction, 0f, 0.9f);
        mouthCloseMax = Mathf.Clamp01(mouthCloseMax);
        mouthFunnelMax = Mathf.Clamp01(mouthFunnelMax);
        mouthStretchMax = Mathf.Clamp01(mouthStretchMax);
        mouthSmileMax = Mathf.Clamp01(mouthSmileMax);
        mouthPressMax = Mathf.Clamp01(mouthPressMax);
        tongueOutMax = Mathf.Clamp01(tongueOutMax);

        if (!Application.isPlaying)
            CacheBlendshapeIndices();
    }

    private void TrySetupOvrBridge()
    {
        if (ovrReady)
            return;

        if (ovrLipSyncContext == null)
            AutoAssignContext();

        if (ovrLipSyncContext == null)
        {
            WarnMissingOvr("No hay OVRLipSyncContext en este GameObject ni en hijos.");
            return;
        }

        contextBaseType = FindTypeByName("OVRLipSyncContextBase");
        if (contextBaseType == null)
        {
            WarnMissingOvr("No se encontro el tipo OVRLipSyncContextBase. Importa el paquete Oculus OVR LipSync.");
            return;
        }

        if (!contextBaseType.IsInstanceOfType(ovrLipSyncContext))
        {
            WarnMissingOvr("El componente asignado no es un OVRLipSyncContextBase valido.");
            return;
        }

        getCurrentPhonemeFrameMethod = contextBaseType.GetMethod("GetCurrentPhonemeFrame", BindingFlags.Public | BindingFlags.Instance);
        if (getCurrentPhonemeFrameMethod == null)
        {
            WarnMissingOvr("No se encontro el metodo GetCurrentPhonemeFrame en OVRLipSyncContextBase.");
            return;
        }

        Type frameType = getCurrentPhonemeFrameMethod.ReturnType;
        visemesField = frameType.GetField("Visemes", BindingFlags.Public | BindingFlags.Instance);
        visemesProperty = frameType.GetProperty("Visemes", BindingFlags.Public | BindingFlags.Instance);

        if (visemesField == null && visemesProperty == null)
        {
            WarnMissingOvr("No se pudo acceder a Visemes en el frame de OVR LipSync.");
            return;
        }

        ovrReady = true;
        warnedMissingOvr = false;
    }

    private float[] ReadVisemes()
    {
        object frame = getCurrentPhonemeFrameMethod.Invoke(ovrLipSyncContext, null);
        if (frame == null)
            return null;

        if (visemesField != null)
            return visemesField.GetValue(frame) as float[];

        return visemesProperty.GetValue(frame) as float[];
    }

    private void AutoAssignContext()
    {
        contextBaseType = FindTypeByName("OVRLipSyncContextBase");
        if (contextBaseType == null)
            return;

        MonoBehaviour[] candidates = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] != null && contextBaseType.IsInstanceOfType(candidates[i]))
            {
                ovrLipSyncContext = candidates[i];
                return;
            }
        }
    }

    private static Type FindTypeByName(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type[] types;
            try
            {
                types = assemblies[i].GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            if (types == null)
                continue;

            for (int j = 0; j < types.Length; j++)
            {
                if (types[j] != null && types[j].Name == typeName)
                    return types[j];
            }
        }

        return null;
    }

    private void ApplySmoothed(int blendshapeIndex, int secondaryBlendshapeIndex, ref float current, float target)
    {
        if (blendshapeIndex < 0)
            return;

        float clampedTarget = Mathf.Clamp01(target);
        float speed = clampedTarget > current ? attackSpeed : releaseSpeed;
        current = Mathf.Lerp(current, clampedTarget, 1f - Mathf.Exp(-speed * Time.deltaTime));
        faceRenderer.SetBlendShapeWeight(blendshapeIndex, current);

        if (syncSecondaryMouthMesh && secondaryMouthRenderer != null && secondaryBlendshapeIndex >= 0)
            secondaryMouthRenderer.SetBlendShapeWeight(secondaryBlendshapeIndex, current);
    }

    private void CacheBlendshapeIndices()
    {
        jawOpenIndex = -1;
        jawOpenSecondaryIndex = -1;
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
        mouthPressLeftIndex = -1;
        mouthPressLeftSecondaryIndex = -1;
        mouthPressRightIndex = -1;
        mouthPressRightSecondaryIndex = -1;
        tongueOutIndex = -1;
        tongueOutSecondaryIndex = -1;

        if (faceRenderer == null || faceRenderer.sharedMesh == null)
            return;

        Mesh mesh = faceRenderer.sharedMesh;
        jawOpenIndex = mesh.GetBlendShapeIndex(jawOpenName);
        mouthCloseIndex = mesh.GetBlendShapeIndex(mouthCloseName);
        mouthFunnelIndex = mesh.GetBlendShapeIndex(mouthFunnelName);
        mouthStretchLeftIndex = mesh.GetBlendShapeIndex(mouthStretchLeftName);
        mouthStretchRightIndex = mesh.GetBlendShapeIndex(mouthStretchRightName);
        mouthSmileLeftIndex = mesh.GetBlendShapeIndex(mouthSmileLeftName);
        mouthSmileRightIndex = mesh.GetBlendShapeIndex(mouthSmileRightName);
        mouthPressLeftIndex = mesh.GetBlendShapeIndex(mouthPressLeftName);
        mouthPressRightIndex = mesh.GetBlendShapeIndex(mouthPressRightName);
        tongueOutIndex = mesh.GetBlendShapeIndex(tongueOutName);

        if (secondaryMouthRenderer != null && secondaryMouthRenderer.sharedMesh != null)
        {
            Mesh secondaryMesh = secondaryMouthRenderer.sharedMesh;
            jawOpenSecondaryIndex = secondaryMesh.GetBlendShapeIndex(jawOpenName);
            mouthCloseSecondaryIndex = secondaryMesh.GetBlendShapeIndex(mouthCloseName);
            mouthFunnelSecondaryIndex = secondaryMesh.GetBlendShapeIndex(mouthFunnelName);
            mouthStretchLeftSecondaryIndex = secondaryMesh.GetBlendShapeIndex(mouthStretchLeftName);
            mouthStretchRightSecondaryIndex = secondaryMesh.GetBlendShapeIndex(mouthStretchRightName);
            mouthSmileLeftSecondaryIndex = secondaryMesh.GetBlendShapeIndex(mouthSmileLeftName);
            mouthSmileRightSecondaryIndex = secondaryMesh.GetBlendShapeIndex(mouthSmileRightName);
            mouthPressLeftSecondaryIndex = secondaryMesh.GetBlendShapeIndex(mouthPressLeftName);
            mouthPressRightSecondaryIndex = secondaryMesh.GetBlendShapeIndex(mouthPressRightName);
            tongueOutSecondaryIndex = secondaryMesh.GetBlendShapeIndex(tongueOutName);
        }
    }

    private void WarnMissingOvr(string message)
    {
        if (warnedMissingOvr)
            return;

        warnedMissingOvr = true;
        Debug.LogWarning($"[OVRLipSyncToARKitBlendshapes] {message}", this);
    }
}
