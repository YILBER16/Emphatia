using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Empathia
{
    /// <summary>
    /// Boca mínima en Speaking: blendshape si el avatar lo tiene, si no un óvalo de UI.
    /// Usa el ExpressionPacket de B o una heurística de vocales.
    /// </summary>
    [DefaultExecutionOrder(21000)]
    public class EmpathiaMouthDriver : MonoBehaviour
    {
        const float JawOpenMax = 0.55f;

        Image _mouth;
        SkinnedMeshRenderer[] _meshes;
        int[] _jawOpen;
        int[] _pucker;
        int[] _shrugLower;
        int[] _vOpen;
        int[] _vWide;
        int[] _vTight;
        int[] _vTightO;
        int[] _vExplosive;
        int[] _vDental;
        int[] _vAffricate;
        int[] _vLipOpen;
        int[] _smileL;
        int[] _smileR;
        int[] _blinkL;
        int[] _blinkR;
        int[] _browInL;
        int[] _browInR;
        int[] _squintL;
        int[] _squintR;
        float _smileNow;
        float _blinkT = 1f;
        float _nextBlinkAt;
        int _blinksLeft;
        bool _useVisemes;
        float[] _env;
        float _voiceStart;
        float _voiceEnd;
        bool _mouthGateOpen;
        float _loudSmooth;
        Transform _head;
        Quaternion _headRest;
        bool _headReady;
        string _reply;
        string _viseme = "sil";
        LipFrame[] _frames;

        struct LipFrame
        {
            public float T;
            public string Viseme;
        }
        bool _playing;
        float _elapsed;
        float _duration;
        float _jawNow;
        float _puckerNow;
        float _shrugNow;
        float _openNow;
        float _wideNow;
        float _tightNow;
        float _tightONow;
        float _explosiveNow;
        float _dentalNow;
        float _affricateNow;
        float _lipOpenNow;

        struct MouthShape
        {
            public float Open;
            public float Wide;
            public float Tight;
            public float TightO;
            public float Explosive;
            public float Dental;
            public float Affricate;
            public float LipOpen;
        }

        public void BindUi(Image mouth)
        {
            _mouth = mouth;
            CacheMeshes();
            ApplyPose(0f, 0f, 0f, instant: true);
        }

        public void StartSpeaking(ExpressionPacketDto packet, string replyText)
        {
            CacheMeshes();
            _reply = replyText ?? "";
            BuildTimeline();
            _duration = 0f;
            _elapsed = 0f;
            _viseme = "sil";
            _playing = true;
            _mouthGateOpen = false;
            _loudSmooth = 0f;
            ApplyPose(0f, 0f, 0f, instant: true);
        }

        public void SetSpeechWindow(AudioClip clip)
        {
            if (clip == null)
            {
                _duration = 1f;
                _voiceStart = 0f;
                _voiceEnd = 1f;
                _env = null;
                return;
            }

            _duration = Mathf.Max(0.3f, clip.length);
            BuildEnvelope(clip);
        }

        public void Tick(float elapsedSeconds)
        {
            if (!_playing)
                return;
            _elapsed = elapsedSeconds;
            var clock = Mathf.Max(0f, elapsedSeconds - 0.03f);
            _elapsed = clock;
            _viseme = VisemeAt(clock);
            PoseFor(_viseme, out var jaw, out var pucker, out var shrug);
            ApplyPose(jaw, pucker, shrug, instant: false);
        }

        public void Stop()
        {
            _playing = false;
            _viseme = "sil";
            ApplyPose(0f, 0f, 0f, instant: true);
            if (_headReady && _head != null)
                _head.localRotation = _headRest;
        }

        void CacheMeshes()
        {
            if (_meshes != null)
                return;

            _meshes = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _jawOpen = new int[_meshes.Length];
            _pucker = new int[_meshes.Length];
            _shrugLower = new int[_meshes.Length];
            _vOpen = new int[_meshes.Length];
            _vWide = new int[_meshes.Length];
            _vTight = new int[_meshes.Length];
            _vTightO = new int[_meshes.Length];
            _vExplosive = new int[_meshes.Length];
            _vDental = new int[_meshes.Length];
            _vAffricate = new int[_meshes.Length];
            _vLipOpen = new int[_meshes.Length];
            _smileL = new int[_meshes.Length];
            _smileR = new int[_meshes.Length];
            _blinkL = new int[_meshes.Length];
            _blinkR = new int[_meshes.Length];
            _browInL = new int[_meshes.Length];
            _browInR = new int[_meshes.Length];
            _squintL = new int[_meshes.Length];
            _squintR = new int[_meshes.Length];
            for (var i = 0; i < _meshes.Length; i++)
            {
                _jawOpen[i] = FindShape(_meshes[i], "jaw_open", "jawopen", "mouth_open", "mouthopen");
                _pucker[i] = FindShapeExact(_meshes[i], "mouth_pucker", "mouthpucker");
                _shrugLower[i] = FindShape(_meshes[i], "mouth_shrug_lower", "mouthshruglower");
                _vOpen[i] = FindShapeExact(_meshes[i], "v_open");
                _vWide[i] = FindShapeExact(_meshes[i], "v_wide");
                _vTight[i] = FindShapeExact(_meshes[i], "v_tight");
                _vTightO[i] = FindShapeExact(_meshes[i], "v_tight_o");
                _vExplosive[i] = FindShapeExact(_meshes[i], "v_explosive");
                _vDental[i] = FindShapeExact(_meshes[i], "v_dental_lip");
                _vAffricate[i] = FindShapeExact(_meshes[i], "v_affricate");
                _vLipOpen[i] = FindShapeExact(_meshes[i], "v_lip_open");
                _smileL[i] = FindShapeExact(_meshes[i], "mouth_smile_l");
                _smileR[i] = FindShapeExact(_meshes[i], "mouth_smile_r");
                _blinkL[i] = FindShapeExact(_meshes[i], "eye_blink_l");
                _blinkR[i] = FindShapeExact(_meshes[i], "eye_blink_r");
                _browInL[i] = FindShapeExact(_meshes[i], "brow_raise_inner_l");
                _browInR[i] = FindShapeExact(_meshes[i], "brow_raise_inner_r");
                _squintL[i] = FindShapeExact(_meshes[i], "eye_squint_l");
                _squintR[i] = FindShapeExact(_meshes[i], "eye_squint_r");
                if (_vOpen[i] >= 0)
                    _useVisemes = true;
            }
        }

        static int FindShapeExact(SkinnedMeshRenderer mesh, params string[] keys)
        {
            if (mesh == null || mesh.sharedMesh == null)
                return -1;

            var count = mesh.sharedMesh.blendShapeCount;
            for (var i = 0; i < count; i++)
            {
                var name = mesh.sharedMesh.GetBlendShapeName(i);
                if (string.IsNullOrEmpty(name))
                    continue;
                var lower = name.ToLowerInvariant().Replace(" ", "");
                var slash = lower.LastIndexOf('/');
                if (slash >= 0)
                    lower = lower.Substring(slash + 1);
                var dot = lower.LastIndexOf('.');
                if (dot >= 0)
                    lower = lower.Substring(dot + 1);
                for (var k = 0; k < keys.Length; k++)
                {
                    if (lower == keys[k])
                        return i;
                }
            }

            return -1;
        }

        static int FindShape(SkinnedMeshRenderer mesh, params string[] keys)
        {
            if (mesh == null || mesh.sharedMesh == null)
                return -1;

            var count = mesh.sharedMesh.blendShapeCount;
            for (var i = 0; i < count; i++)
            {
                var name = mesh.sharedMesh.GetBlendShapeName(i);
                if (string.IsNullOrEmpty(name))
                    continue;
                var lower = name.ToLowerInvariant().Replace(" ", "");
                for (var k = 0; k < keys.Length; k++)
                {
                    if (lower.Contains(keys[k]))
                        return i;
                }
            }

            return -1;
        }

        void BuildTimeline()
        {
            var text = _reply ?? "";
            var slots = new List<string>();
            var weights = new List<float>();
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                var next = i + 1 < text.Length ? text[i + 1] : '\0';
                if ((ch == 'c' || ch == 'C') && (next == 'h' || next == 'H'))
                {
                    slots.Add("CH");
                    weights.Add(0.7f);
                    i++;
                    continue;
                }

                var lower = char.ToLowerInvariant(ch);
                var prev = i > 0 ? char.ToLowerInvariant(text[i - 1]) : '\0';
                var nxt = char.ToLowerInvariant(next);
                if (lower == 'h')
                    continue;
                if (lower == 'u' && ((prev == 'q' && IsEi(nxt)) || (prev == 'g' && IsEi(nxt))))
                    continue;
                if (lower == 'l' && nxt == 'l')
                {
                    AddSlot(slots, weights, "ih", 0.9f);
                    i++;
                    continue;
                }

                if (lower == 'y')
                {
                    if (IsVowelChar(nxt))
                        AddSlot(slots, weights, "yy", 0.5f);
                    else
                        AddSlot(slots, weights, "ih", 2.2f);
                    continue;
                }

                if (lower == 'r' && nxt == 'r')
                {
                    AddSlot(slots, weights, "rr", 0.75f);
                    i++;
                    continue;
                }

                if (lower == 'r')
                {
                    var fuerte = prev == '\0' || !char.IsLetter(prev) || prev == 'n' || prev == 'l' || prev == 's';
                    AddSlot(slots, weights, fuerte ? "rr" : "RR", fuerte ? 0.7f : 0.32f);
                    continue;
                }

                if (lower == 'd' && IsVowelChar(prev) && IsVowelChar(nxt))
                {
                    AddSlot(slots, weights, "TH", 0.6f);
                    continue;
                }

                if (lower == 'j' || (lower == 'g' && IsEi(nxt)))
                {
                    AddSlot(slots, weights, "hx", 0.55f);
                    continue;
                }

                if (lower == 'c' && !IsEi(nxt))
                {
                    AddSlot(slots, weights, "kk", 0.55f);
                    continue;
                }

                if (lower == 'ñ')
                {
                    AddSlot(slots, weights, "ny", 0.65f);
                    continue;
                }

                var vis = LetterToOvr(ch);
                AddSlot(slots, weights, vis, SlotWeight(ch, vis));
            }

            if (slots.Count == 0)
            {
                _frames = new[] { new LipFrame { T = 0f, Viseme = "sil" } };
                return;
            }

            var total = 0f;
            for (var i = 0; i < weights.Count; i++)
                total += weights[i];
            _frames = new LipFrame[slots.Count];
            var cursor = 0f;
            for (var i = 0; i < slots.Count; i++)
            {
                _frames[i] = new LipFrame { T = cursor / total, Viseme = slots[i] };
                cursor += weights[i];
            }
        }

        static void AddSlot(List<string> slots, List<float> weights, string viseme, float weight)
        {
            slots.Add(viseme);
            weights.Add(weight);
        }

        static bool IsVowel(string viseme)
        {
            return viseme == "aa" || viseme == "E" || viseme == "ih" || viseme == "oh" || viseme == "ou";
        }

        static bool IsVowelChar(char ch)
        {
            switch (ch)
            {
                case 'a':
                case 'á':
                case 'e':
                case 'é':
                case 'i':
                case 'í':
                case 'o':
                case 'ó':
                case 'u':
                case 'ú':
                case 'ü':
                    return true;
                default:
                    return false;
            }
        }

        static bool IsEi(char ch)
        {
            return ch == 'e' || ch == 'é' || ch == 'i' || ch == 'í';
        }

        static float SlotWeight(char ch, string viseme)
        {
            if (IsVowel(viseme))
                return 2.2f;
            if (viseme != "sil")
                return 0.28f;
            switch (ch)
            {
                case '.':
                case '!':
                case '?':
                case '…':
                    return 2.8f;
                case ',':
                case ';':
                case ':':
                    return 1.6f;
                case ' ':
                    return 0.85f;
                default:
                    return 0.45f;
            }
        }

        string VisemeAt(float seconds)
        {
            if (_frames == null || _frames.Length == 0)
                return "sil";

            var span = _duration > 0.05f ? _duration : 1f;
            var u = Mathf.Clamp01(seconds / span);
            var viseme = _frames[0].Viseme;
            for (var i = 0; i < _frames.Length; i++)
            {
                if (_frames[i].T > u)
                    break;
                viseme = _frames[i].Viseme;
            }

            return viseme;
        }

        static string LetterToOvr(char ch)
        {
            switch (char.ToLowerInvariant(ch))
            {
                case 'a':
                case 'á':
                    return "aa";
                case 'e':
                case 'é':
                    return "E";
                case 'i':
                case 'í':
                case 'y':
                    return "ih";
                case 'o':
                case 'ó':
                    return "oh";
                case 'u':
                case 'ú':
                case 'ü':
                case 'w':
                    return "ou";
                case 'm':
                case 'p':
                case 'b':
                    return "PP";
                case 'f':
                    return "FF";
                case 'v':
                    return "PP";
                case 's':
                case 'z':
                case 'c':
                    return "SS";
                case 'n':
                    return "nn";
                case 't':
                case 'd':
                case 'l':
                    return "DD";
                case 'r':
                    return "RR";
                case 'k':
                case 'g':
                case 'q':
                case 'j':
                case 'x':
                    return "kk";
                case 'h':
                case ' ':
                case '.':
                case ',':
                case '?':
                case '!':
                case '¿':
                case '¡':
                    return "sil";
                default:
                    return "DD";
            }
        }

        static void PoseFor(string viseme, out float jaw, out float pucker, out float shrug)
        {
            jaw = 0.2f;
            pucker = 0f;
            shrug = 0f;
            switch (viseme)
            {
                case "aa":
                    jaw = 1f;
                    shrug = 0.35f;
                    break;
                case "E":
                    jaw = 0.4f;
                    break;
                case "I":
                    jaw = 0.25f;
                    break;
                case "O":
                    jaw = 0.5f;
                    pucker = 0.7f;
                    break;
                case "U":
                    jaw = 0.3f;
                    pucker = 1f;
                    break;
                case "PP":
                    jaw = 0f;
                    pucker = 0.25f;
                    break;
                case "sil":
                    jaw = 0f;
                    break;
            }
        }

        void ApplyPose(float jaw, float pucker, float shrug, bool instant)
        {
            var t = instant ? 1f : 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            _jawNow = Mathf.Lerp(_jawNow, jaw, t);
            _puckerNow = Mathf.Lerp(_puckerNow, pucker, t);
            _shrugNow = Mathf.Lerp(_shrugNow, shrug, t);

            if (_mouth != null)
            {
                var rt = _mouth.rectTransform;
                rt.sizeDelta = new Vector2(42f, Mathf.Lerp(5f, 14f, _jawNow));
            }

            if (_meshes == null)
                return;

            var jawWeight = _useVisemes ? 0f : _jawNow * JawOpenMax;
            var puckerWeight = _useVisemes ? 0f : _puckerNow * 0.2f;
            var shrugWeight = _useVisemes ? 0f : _shrugNow * 0.15f;
            for (var i = 0; i < _meshes.Length; i++)
            {
                var mesh = _meshes[i];
                if (mesh == null)
                    continue;
                SetWeight(mesh, _jawOpen[i], jawWeight);
                SetWeight(mesh, _pucker[i], puckerWeight);
                SetWeight(mesh, _shrugLower[i], shrugWeight);
            }
            if (_useVisemes)
                ApplyVisemeWeights(instant);
        }

        void ApplyVisemeWeights(bool instant)
        {
            var shape = _playing ? VisemeBlend(_elapsed) : default;
            var sum = shape.Open + shape.Wide + shape.Tight + shape.TightO
                + shape.Explosive + shape.Dental + shape.Affricate + shape.LipOpen;
            var rate = sum < 6f ? 16f : 8f;
            var t = instant ? 1f : 1f - Mathf.Exp(-rate * Time.unscaledDeltaTime);
            _openNow = Mathf.Lerp(_openNow, shape.Open, t);
            _wideNow = Mathf.Lerp(_wideNow, shape.Wide, t);
            _tightNow = Mathf.Lerp(_tightNow, shape.Tight, t);
            _tightONow = Mathf.Lerp(_tightONow, shape.TightO, t);
            _explosiveNow = Mathf.Lerp(_explosiveNow, shape.Explosive, t);
            _dentalNow = Mathf.Lerp(_dentalNow, shape.Dental, t);
            _affricateNow = Mathf.Lerp(_affricateNow, shape.Affricate, t);
            _lipOpenNow = Mathf.Lerp(_lipOpenNow, shape.LipOpen, t);

            for (var i = 0; i < _meshes.Length; i++)
            {
                var mesh = _meshes[i];
                if (mesh == null)
                    continue;
                SetWeight(mesh, _vOpen[i], _openNow);
                SetWeight(mesh, _vWide[i], _wideNow);
                SetWeight(mesh, _vTight[i], _tightNow);
                SetWeight(mesh, _vTightO[i], _tightONow);
                SetWeight(mesh, _vExplosive[i], _explosiveNow);
                SetWeight(mesh, _vDental[i], _dentalNow);
                SetWeight(mesh, _vAffricate[i], _affricateNow);
                SetWeight(mesh, _vLipOpen[i], _lipOpenNow);
            }
        }

        void WriteVisemes()
        {
            if (_meshes == null)
                return;
            for (var i = 0; i < _meshes.Length; i++)
            {
                var mesh = _meshes[i];
                if (mesh == null)
                    continue;
                SetWeight(mesh, _vOpen[i], _openNow);
                SetWeight(mesh, _vWide[i], _wideNow);
                SetWeight(mesh, _vTight[i], _tightNow);
                SetWeight(mesh, _vTightO[i], _tightONow);
                SetWeight(mesh, _vExplosive[i], _explosiveNow);
                SetWeight(mesh, _vDental[i], _dentalNow);
                SetWeight(mesh, _vAffricate[i], _affricateNow);
                SetWeight(mesh, _vLipOpen[i], _lipOpenNow);
            }
        }

        void LateUpdate()
        {
            CacheMeshes();
            if (_meshes == null)
                return;
            TickBlink();
            var blink = BlinkAmount();
            var smileTarget = _playing ? 12f : 36f;
            _smileNow = Mathf.Lerp(_smileNow, smileTarget, 1f - Mathf.Exp(-2.2f * Time.unscaledDeltaTime));
            for (var i = 0; i < _meshes.Length; i++)
            {
                var mesh = _meshes[i];
                if (mesh == null)
                    continue;
                SetWeight(mesh, _smileL[i], _smileNow);
                SetWeight(mesh, _smileR[i], _smileNow);
                SetWeight(mesh, _blinkL[i], blink);
                SetWeight(mesh, _blinkR[i], blink);
            }

            WriteVisemes();
        }

        void TickBlink()
        {
            if (_nextBlinkAt <= 0f)
                _nextBlinkAt = Time.unscaledTime + Random.Range(1.6f, 3.2f);
            if (_blinkT >= 1f && Time.unscaledTime >= _nextBlinkAt)
            {
                _blinkT = 0f;
                _blinksLeft = Random.value < 0.22f ? 1 : 0;
                var wait = _playing ? Random.Range(2.2f, 4.4f) : Random.Range(2.8f, 5.6f);
                _nextBlinkAt = Time.unscaledTime + wait;
            }

            if (_blinkT >= 1f)
                return;
            _blinkT += Time.unscaledDeltaTime / 0.14f;
            if (_blinkT < 1f || _blinksLeft <= 0)
                return;
            _blinksLeft--;
            _blinkT = 0f;
        }

        float BlinkAmount()
        {
            if (_blinkT >= 1f)
                return 0f;
            return Mathf.Sin(Mathf.Clamp01(_blinkT) * Mathf.PI) * 90f;
        }

        void BuildEnvelope(AudioClip clip)
        {
            _voiceStart = 0f;
            _voiceEnd = clip.length;
            _env = null;
            var channels = Mathf.Max(1, clip.channels);
            var samples = new float[clip.samples * channels];
            if (!clip.GetData(samples, 0))
                return;

            var hop = Mathf.Max(1, clip.frequency / 40);
            var frames = Mathf.Max(1, clip.samples / hop);
            _env = new float[frames];
            var peak = 0.0001f;
            for (var f = 0; f < frames; f++)
            {
                double sum = 0;
                var count = 0;
                var start = f * hop * channels;
                var end = Mathf.Min(samples.Length, start + hop * channels);
                for (var s = start; s < end; s++)
                {
                    var v = samples[s];
                    sum += v * v;
                    count++;
                }

                var rms = count > 0 ? Mathf.Sqrt((float)(sum / count)) : 0f;
                _env[f] = rms;
                if (rms > peak)
                    peak = rms;
            }

            for (var f = 0; f < frames; f++)
                _env[f] /= peak;

            _voiceStart = 0f;
            _voiceEnd = clip.length;
            var found = false;
            for (var f = 0; f < frames; f++)
            {
                if (_env[f] < 0.12f)
                    continue;
                var t = f / 40f;
                if (!found)
                {
                    _voiceStart = t;
                    found = true;
                }

                _voiceEnd = t;
            }

            if (_voiceEnd - _voiceStart < 0.25f)
            {
                _voiceStart = 0f;
                _voiceEnd = clip.length;
            }
        }

        float LoudnessAt(float seconds)
        {
            if (_env == null || _env.Length == 0)
                return 1f;
            var f = Mathf.Clamp(Mathf.RoundToInt(seconds * 40f), 0, _env.Length - 1);
            return _env[f];
        }

        void LookAtCamera()
        {
            if (!_headReady)
            {
                var avatar = GameObject.Find("Convai Character");
                if (avatar == null)
                    avatar = GameObject.Find("personaje");
                if (avatar != null)
                {
                    var all = avatar.GetComponentsInChildren<Transform>(true);
                    for (var i = 0; i < all.Length; i++)
                    {
                        var n = all[i].name.ToLowerInvariant();
                        if (n == "head" || n.EndsWith("_head") || n.Contains("cc_base_head"))
                        {
                            _head = all[i];
                            _headRest = all[i].localRotation;
                            break;
                        }
                    }
                }

                _headReady = true;
            }

            if (_head == null || _head.parent == null)
                return;

            var cam = Camera.main;
            if (cam == null)
            {
                var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                for (var c = 0; c < cams.Length; c++)
                {
                    if (cams[c] != null && cams[c].enabled && cams[c].targetTexture == null)
                    {
                        cam = cams[c];
                        break;
                    }
                }
            }

            if (cam == null)
                return;

            var toCam = cam.transform.position - _head.position;
            if (toCam.sqrMagnitude < 0.01f)
                return;

            var localDir = _head.parent.InverseTransformDirection(toCam);
            var yaw = Mathf.Clamp(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg, -14f, 14f);
            var pitch = Mathf.Clamp(-Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg, -8f, 8f);
            var aim = _headRest * Quaternion.Euler(pitch, yaw, 0f);
            _head.localRotation = Quaternion.Slerp(_head.localRotation, aim, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
        }

        MouthShape VisemeBlend(float seconds)
        {
            if (string.IsNullOrEmpty(_reply) || _frames == null || _frames.Length == 0)
                return default;

            var loudRaw = LoudnessAt(seconds);
            var follow = loudRaw < _loudSmooth ? 0.8f : 0.35f;
            _loudSmooth = Mathf.Lerp(_loudSmooth, loudRaw, follow);
            if (!_mouthGateOpen && _loudSmooth > 0.18f)
                _mouthGateOpen = true;
            else if (_mouthGateOpen && _loudSmooth < 0.12f)
                _mouthGateOpen = false;
            if (!_mouthGateOpen || seconds < _voiceStart + 0.05f)
                return default;
            var spanStart = _voiceEnd > _voiceStart + 0.2f ? _voiceStart : 0f;
            var spanEnd = _voiceEnd > _voiceStart + 0.2f ? _voiceEnd : (_duration > 0.05f ? _duration : 1f);
            var u = Mathf.Clamp01(Mathf.InverseLerp(spanStart, spanEnd, seconds));
            var i0 = 0;
            for (var i = 0; i < _frames.Length - 1; i++)
            {
                if (_frames[i + 1].T <= u)
                    i0 = i + 1;
            }

            var i1 = Mathf.Min(i0 + 1, _frames.Length - 1);
            var denom = _frames[i1].T - _frames[i0].T;
            var raw = denom > 0.0001f ? Mathf.Clamp01((u - _frames[i0].T) / denom) : 0f;
            var nextPause = _frames[i1].Viseme == "sil";
            var hold = _frames[i0].Viseme == "sil"
                ? 0.88f
                : IsVowel(_frames[i0].Viseme) ? (nextPause ? 0.35f : 0.78f) : 0.16f;
            var frac = raw < hold ? 0f : Mathf.SmoothStep(0f, 1f, (raw - hold) / (1f - hold));
            var a = ShapeFor(_frames[i0].Viseme);
            var b = ShapeFor(_frames[i1].Viseme);
            return new MouthShape
            {
                Open = Mathf.Lerp(a.Open, b.Open, frac),
                Wide = Mathf.Lerp(a.Wide, b.Wide, frac),
                Tight = Mathf.Lerp(a.Tight, b.Tight, frac),
                TightO = Mathf.Lerp(a.TightO, b.TightO, frac),
                Explosive = Mathf.Lerp(a.Explosive, b.Explosive, frac),
                Dental = Mathf.Lerp(a.Dental, b.Dental, frac),
                Affricate = Mathf.Lerp(a.Affricate, b.Affricate, frac),
                LipOpen = Mathf.Lerp(a.LipOpen, b.LipOpen, frac)
            };
        }

        static MouthShape ShapeFor(string viseme)
        {
            var shape = new MouthShape();
            switch (viseme)
            {
                case "aa":
                    shape.Open = 48f;
                    break;
                case "E":
                    shape.Wide = 68f;
                    shape.Open = 26f;
                    break;
                case "ih":
                    shape.Wide = 72f;
                    shape.Open = 32f;
                    break;
                case "oh":
                    shape.TightO = 74f;
                    shape.Open = 28f;
                    break;
                case "ou":
                    shape.Tight = 62f;
                    shape.TightO = 16f;
                    shape.Open = 24f;
                    break;
                case "PP":
                    shape.Explosive = 42f;
                    shape.LipOpen = 16f;
                    shape.Open = 14f;
                    break;
                case "FF":
                    shape.Dental = 55f;
                    shape.Open = 22f;
                    break;
                case "SS":
                    shape.Affricate = 28f;
                    shape.Wide = 20f;
                    shape.Open = 26f;
                    break;
                case "CH":
                    shape.Affricate = 48f;
                    shape.Tight = 12f;
                    shape.Open = 24f;
                    break;
                case "DD":
                    shape.LipOpen = 26f;
                    shape.Open = 24f;
                    break;
                case "nn":
                    shape.LipOpen = 20f;
                    shape.Open = 22f;
                    break;
                case "ny":
                    shape.LipOpen = 18f;
                    shape.Wide = 26f;
                    shape.Open = 20f;
                    break;
                case "TH":
                    shape.Dental = 28f;
                    shape.Open = 22f;
                    break;
                case "kk":
                    shape.Open = 30f;
                    break;
                case "hx":
                    shape.Open = 32f;
                    break;
                case "yy":
                    shape.Wide = 58f;
                    shape.Open = 30f;
                    shape.Affricate = 16f;
                    break;
                case "RR":
                    shape.Open = 34f;
                    shape.LipOpen = 14f;
                    break;
                case "rr":
                    shape.Open = 44f;
                    shape.LipOpen = 12f;
                    break;
            }

            return shape;
        }

        static void SetWeight(SkinnedMeshRenderer mesh, int index, float weight)
        {
            if (index < 0)
                return;
            mesh.SetBlendShapeWeight(index, weight);
        }
    }
}
