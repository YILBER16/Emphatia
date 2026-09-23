using UnityEngine;
using UnityEngine.UI;

namespace Empathia
{
    /// <summary>
    /// Boca mínima en Speaking: blendshape si el avatar lo tiene, si no un óvalo de UI.
    /// Usa el ExpressionPacket de B o una heurística de vocales.
    /// </summary>
    public class EmpathiaMouthDriver : MonoBehaviour
    {
        Image _mouth;
        SkinnedMeshRenderer[] _meshes;
        int[] _jawIndex;
        ExpressionPacketDto _packet;
        string _reply;
        bool _playing;
        float _elapsed;

        public void BindUi(Image mouth)
        {
            _mouth = mouth;
            CacheMeshes();
            ApplyOpen(0f);
        }

        public void StartSpeaking(ExpressionPacketDto packet, string replyText)
        {
            CacheMeshes();
            _packet = packet;
            _reply = replyText ?? "";
            _elapsed = 0f;
            _playing = true;
            ApplyOpen(OpenAt(0f));
        }

        public void Tick(float elapsedSeconds)
        {
            if (!_playing)
                return;
            _elapsed = elapsedSeconds;
            ApplyOpen(OpenAt(_elapsed));
        }

        public void Stop()
        {
            _playing = false;
            _packet = null;
            ApplyOpen(0f);
        }

        void CacheMeshes()
        {
            if (_meshes != null)
                return;

            _meshes = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            _jawIndex = new int[_meshes.Length];
            for (var i = 0; i < _meshes.Length; i++)
                _jawIndex[i] = FindJawShape(_meshes[i]);
        }

        static int FindJawShape(SkinnedMeshRenderer mesh)
        {
            if (mesh == null || mesh.sharedMesh == null)
                return -1;

            var count = mesh.sharedMesh.blendShapeCount;
            for (var i = 0; i < count; i++)
            {
                var name = mesh.sharedMesh.GetBlendShapeName(i);
                if (string.IsNullOrEmpty(name))
                    continue;
                var lower = name.ToLowerInvariant();
                if (lower.Contains("jawopen") || lower.Contains("jaw_open")
                    || lower.Contains("mouthopen") || lower.Contains("mouth_open")
                    || lower.Contains("viseme_aa") || lower == "aa")
                    return i;
            }

            return -1;
        }

        float OpenAt(float seconds)
        {
            var lips = _packet != null ? _packet.lips : null;
            if (lips != null && lips.Length > 0)
            {
                var tMs = Mathf.Max(0, Mathf.RoundToInt(seconds * 1000f));
                var cue = lips[0];
                for (var i = 0; i < lips.Length; i++)
                {
                    if (lips[i] == null)
                        continue;
                    if (lips[i].t_ms <= tMs)
                        cue = lips[i];
                    else
                        break;
                }

                return VisemeOpen(cue.viseme) * Mathf.Clamp01(cue.weight <= 0f ? 1f : cue.weight);
            }

            if (string.IsNullOrWhiteSpace(_reply))
                return 0.15f + 0.2f * (0.5f + 0.5f * Mathf.Sin(seconds * 8f));

            var idx = Mathf.Clamp(Mathf.FloorToInt(seconds * 6f), 0, _reply.Length - 1);
            return VisemeOpen(VowelToViseme(_reply[idx]));
        }

        static string VowelToViseme(char ch)
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
                    return "I";
                case 'o':
                case 'ó':
                    return "O";
                case 'u':
                case 'ú':
                    return "U";
                case 'm':
                case 'p':
                case 'b':
                    return "PP";
                default:
                    return "sil";
            }
        }

        static float VisemeOpen(string viseme)
        {
            switch (viseme)
            {
                case "aa":
                    return 0.85f;
                case "O":
                    return 0.7f;
                case "U":
                    return 0.55f;
                case "E":
                case "I":
                    return 0.45f;
                case "FF":
                case "TH":
                    return 0.28f;
                case "DD":
                case "kk":
                case "CH":
                case "SS":
                case "nn":
                case "RR":
                    return 0.35f;
                case "PP":
                case "sil":
                default:
                    return 0.05f;
            }
        }

        void ApplyOpen(float open)
        {
            open = Mathf.Clamp01(open);
            if (_mouth != null)
            {
                var rt = _mouth.rectTransform;
                rt.sizeDelta = new Vector2(42f, Mathf.Lerp(5f, 22f, open));
            }

            if (_meshes == null)
                return;

            for (var i = 0; i < _meshes.Length; i++)
            {
                if (_meshes[i] == null || _jawIndex[i] < 0)
                    continue;
                _meshes[i].SetBlendShapeWeight(_jawIndex[i], open * 100f);
            }
        }
    }
}
