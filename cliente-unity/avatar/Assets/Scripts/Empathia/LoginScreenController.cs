using System.Collections;
using UnityEngine;

namespace Empathia
{
    /// <summary>
    /// Login A con OnGUI (siempre visible en Play) + flujo sesión/turno.
    /// </summary>
    public class LoginScreenController : MonoBehaviour
    {
        const float MicSeconds = 3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<LoginScreenController>() != null)
                return;

            var go = new GameObject("EmpathiaLogin");
            go.AddComponent<EmpathiaApiClient>();
            go.AddComponent<AudioSource>();
            go.AddComponent<LoginScreenController>();
            DontDestroyOnLoad(go);
        }

        EmpathiaApiClient _api;
        AudioSource _audio;

        string _baseUrl = "http://127.0.0.1:8000/api/v1";
        string _user = "estudiante1";
        string _pass = "password";
        string _status = "Pulsa «Iniciar sesión» para autenticar contra B.";
        string _uiState = "idle";
        string _reply = "(sin respuesta aún)";
        bool _busy;

        GUIStyle _titleStyle;
        GUIStyle _labelStyle;
        GUIStyle _boxStyle;
        GUIStyle _buttonStyle;
        GUIStyle _statusStyle;
        bool _stylesReady;

        void Awake()
        {
            _api = GetComponent<EmpathiaApiClient>();
            if (_api == null)
                _api = gameObject.AddComponent<EmpathiaApiClient>();

            _audio = GetComponent<AudioSource>();
            if (_audio == null)
                _audio = gameObject.AddComponent<AudioSource>();

            if (!string.IsNullOrEmpty(EmpathiaAuthState.BaseUrl))
                _baseUrl = EmpathiaAuthState.BaseUrl;

            Debug.Log("[Empathia] LoginScreenController listo. Debes estar en PLAY (▶) para ver el login.");
        }

        void OnGUI()
        {
            EnsureStyles();

            // Fondo
            GUI.color = new Color(0.06f, 0.09f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var w = Mathf.Min(520f, Screen.width - 40f);
            var h = Mathf.Min(620f, Screen.height - 40f);
            var x = (Screen.width - w) * 0.5f;
            var y = (Screen.height - h) * 0.5f;
            var card = new Rect(x, y, w, h);

            GUI.Box(card, GUIContent.none, _boxStyle);

            GUILayout.BeginArea(new Rect(x + 28, y + 24, w - 56, h - 48));

            GUILayout.Label("EmpathIA", _titleStyle);
            GUILayout.Label("Inicio de sesión", _labelStyle);
            GUILayout.Space(6);
            GUILayout.Label("Prueba de autenticación (solo servidor B :8000)", _statusStyle);
            GUILayout.Space(14);

            GUILayout.Label("Servidor (Base URL)", _statusStyle);
            GUI.enabled = !_busy;
            _baseUrl = GUILayout.TextField(_baseUrl, GUILayout.Height(28));

            GUILayout.Space(8);
            GUILayout.Label("Usuario", _statusStyle);
            _user = GUILayout.TextField(_user, GUILayout.Height(28));

            GUILayout.Space(8);
            GUILayout.Label("Contraseña", _statusStyle);
            _pass = GUILayout.PasswordField(_pass, '•', GUILayout.Height(28));

            GUILayout.Space(14);
            if (GUILayout.Button("Iniciar sesión", _buttonStyle, GUILayout.Height(42)))
                OnLoginClicked();

            GUILayout.Space(10);
            GUILayout.Label("Estado UI: " + _uiState, _labelStyle);
            GUILayout.Label(_status, _statusStyle, GUILayout.MinHeight(70));

            GUILayout.Space(8);
            GUILayout.Label("── Después del login ──", _statusStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Crear sesión", GUILayout.Height(32)))
                OnCreateSessionClicked();
            if (GUILayout.Button("Cerrar sesión", GUILayout.Height(32)))
                OnCloseSessionClicked();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Turno WAV prueba", GUILayout.Height(32)))
                OnTurnWavClicked();
            if (GUILayout.Button("Turno mic (3s)", GUILayout.Height(32)))
                OnTurnMicClicked();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Respuesta: " + _reply, _statusStyle);

            GUI.enabled = true;
            GUILayout.EndArea();
        }

        void EnsureStyles()
        {
            if (_stylesReady)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.85f, 1f) },
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                wordWrap = true,
            };
            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.92f, 0.95f) },
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
            };
            _boxStyle = new GUIStyle(GUI.skin.box);
            _stylesReady = true;
        }

        void OnLoginClicked()
        {
            if (_busy)
                return;

            EmpathiaAuthState.BaseUrl = string.IsNullOrWhiteSpace(_baseUrl)
                ? "http://127.0.0.1:8000/api/v1"
                : _baseUrl.Trim();

            SetBusy(true);
            SetUiState("processing");
            SetStatus("Autenticando contra B…");
            StartCoroutine(_api.Login(_user.Trim(), _pass, (ok, msg) =>
            {
                SetBusy(false);
                SetUiState("idle");
                if (ok)
                {
                    SetStatus(
                        "Login OK\nUsuario: " + EmpathiaAuthState.Username
                        + "\nToken: " + EmpathiaAuthState.TokenPreview
                        + "\nYa puedes crear una sesión.");
                }
                else
                {
                    SetStatus("Error de autenticación:\n" + msg);
                }

                Debug.Log(ok ? "[Empathia] " + msg : "[Empathia] ERROR " + msg);
            }));
        }

        void OnCreateSessionClicked()
        {
            if (_busy)
                return;
            SetBusy(true);
            SetUiState("processing");
            SetStatus("Creando sesión…");
            StartCoroutine(_api.CreateSession((ok, msg) =>
            {
                SetBusy(false);
                SetUiState("idle");
                SetStatus(ok
                    ? msg + "\nToken: " + EmpathiaAuthState.TokenPreview
                    : "Error: " + msg);
                Debug.Log(ok ? "[Empathia] " + msg : "[Empathia] ERROR " + msg);
            }));
        }

        void OnCloseSessionClicked()
        {
            if (_busy)
                return;
            SetBusy(true);
            SetUiState("processing");
            SetStatus("Cerrando sesión…");
            StartCoroutine(_api.CloseSession((ok, msg) =>
            {
                SetBusy(false);
                SetUiState("idle");
                SetStatus(ok ? msg : "Error: " + msg);
                Debug.Log(ok ? "[Empathia] " + msg : "[Empathia] ERROR " + msg);
            }));
        }

        void OnTurnWavClicked()
        {
            if (_busy)
                return;
            StartCoroutine(RunTurnFlow(useMic: false));
        }

        void OnTurnMicClicked()
        {
            if (_busy)
                return;
            StartCoroutine(RunTurnFlow(useMic: true));
        }

        IEnumerator RunTurnFlow(bool useMic)
        {
            SetBusy(true);
            SetReply("(esperando…)");

            byte[] wav = null;
            if (useMic)
            {
                SetUiState("listening");
                SetStatus("Grabando micrófono " + MicSeconds + "s…");
                yield return CaptureMic(MicSeconds, bytes => { wav = bytes; });
                if (wav == null)
                {
                    SetBusy(false);
                    SetUiState("idle");
                    SetStatus("Error: no se pudo grabar micrófono. Usa «Turno WAV prueba».");
                    yield break;
                }
            }
            else
            {
                SetUiState("listening");
                SetStatus("Generando WAV de prueba…");
                wav = EmpathiaWav.BuildSilentWav(0.35f, 16000);
                yield return null;
            }

            SetUiState("processing");
            TurnResultInfo result = null;
            var ok = false;
            var msg = "";

            yield return _api.RunTurn(
                wav,
                status => SetStatus(status),
                (success, info, message) =>
                {
                    ok = success;
                    result = info;
                    msg = message;
                });

            if (!ok || result == null)
            {
                SetBusy(false);
                SetUiState("idle");
                SetReply("(error)");
                SetStatus("Error: " + msg);
                Debug.LogError("[Empathia] " + msg);
                yield break;
            }

            SetReply(string.IsNullOrEmpty(result.ReplyText) ? "(vacía)" : result.ReplyText);
            SetStatus("Respuesta lista.\nTranscript: " + result.Transcript
                      + "\nTTS: " + result.TtsUrl + "\n" + msg);

            SetUiState("speaking");
            var ttsOk = false;
            var ttsMsg = "";
            yield return _api.DownloadAndPlayTts(result.TtsUrl, _audio, (success, message) =>
            {
                ttsOk = success;
                ttsMsg = message;
            });

            SetStatus((ttsOk ? ttsMsg : "Error TTS: " + ttsMsg) + "\n\nRespuesta:\n" + result.ReplyText);

            if (ttsOk && _audio != null && _audio.clip != null)
                yield return new WaitForSeconds(_audio.clip.length + 0.1f);

            SetBusy(false);
            SetUiState("idle");
        }

        IEnumerator CaptureMic(float seconds, System.Action<byte[]> onDone)
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                onDone(null);
                yield break;
            }

            var device = Microphone.devices[0];
            const int rate = 16000;
            var clip = Microphone.Start(device, false, Mathf.CeilToInt(seconds) + 1, rate);
            var t0 = Time.realtimeSinceStartup;
            while (Microphone.GetPosition(device) <= 0 && Time.realtimeSinceStartup - t0 < 2f)
                yield return null;

            yield return new WaitForSeconds(seconds);
            Microphone.End(device);

            var result = EmpathiaWav.FromMicrophoneClip(clip, rate);
            if (clip != null)
                Destroy(clip);
            onDone(result);
        }

        void SetBusy(bool busy) => _busy = busy;
        void SetUiState(string state) => _uiState = state;
        void SetStatus(string message) => _status = message;
        void SetReply(string reply) => _reply = reply;
    }
}
