using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Empathia
{
    /// <summary>
    /// Pantalla A: login → sesión → turno (WAV/mic) → events → reply + TTS.
    /// Se monta sola al cargar la escena.
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
        InputField _baseUrl;
        InputField _user;
        InputField _pass;
        Text _status;
        Text _stateLabel;
        Text _replyLabel;
        Button _loginBtn;
        Button _sessionBtn;
        Button _closeBtn;
        Button _turnWavBtn;
        Button _turnMicBtn;
        bool _busy;
        bool _uiBuilt;
        Font _uiFont;

        void Awake()
        {
            EnsureComponents();
            EnsureEventSystem();
            BuildUi();
            SetUiState("idle");
            SetStatus("Listo para autenticar.\nLab: estudiante1 / password\nSolo B (:8000), nunca :8100.");
            SetReply("(sin respuesta aún)");
            Debug.Log("[Empathia] LoginScreenController activo — interfaz de inicio de sesión lista.");
        }

        void EnsureComponents()
        {
            _api = GetComponent<EmpathiaApiClient>();
            if (_api == null)
                _api = gameObject.AddComponent<EmpathiaApiClient>();

            _audio = GetComponent<AudioSource>();
            if (_audio == null)
                _audio = gameObject.AddComponent<AudioSource>();
        }

        Font UiFont()
        {
            if (_uiFont != null)
                return _uiFont;

            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_uiFont == null)
                _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_uiFont == null)
                _uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica" }, 16);
            return _uiFont;
        }

        void EnsureEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing == null)
            {
                var es = new GameObject("EventSystem");
                existing = es.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            if (existing.GetComponent<InputSystemUIInputModule>() == null
                && existing.GetComponent<StandaloneInputModule>() == null)
            {
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (existing.GetComponent<StandaloneInputModule>() == null)
                existing.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildUi()
        {
            if (_uiBuilt)
                return;
            _uiBuilt = true;

            var canvasGo = new GameObject("EmpathiaLoginCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo a pantalla completa
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(canvasGo.transform, false);
            var bg = backdrop.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.09f, 0.12f, 1f);
            bg.raycastTarget = false;
            var bgRt = backdrop.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var panel = CreatePanel(canvasGo.transform);

            // —— Bloque autenticación ——
            var brand = CreateLabel(panel, "EmpathIA", 34, FontStyle.Bold, 0);
            brand.alignment = TextAnchor.MiddleCenter;
            brand.color = new Color(0.55f, 0.82f, 1f, 1f);

            var title = CreateLabel(panel, "Inicio de sesión", 24, FontStyle.Bold, 2);
            title.alignment = TextAnchor.MiddleCenter;

            var subtitle = CreateLabel(panel, "Prueba de autenticación contra el servidor B", 14, FontStyle.Normal, 8);
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.color = new Color(1f, 1f, 1f, 0.65f);

            _baseUrl = CreateInput(panel, "Servidor (Base URL)", EmpathiaAuthState.BaseUrl);
            _user = CreateInput(panel, "Usuario", "estudiante1");
            _pass = CreateInput(panel, "Contraseña", "password");
            _pass.contentType = InputField.ContentType.Password;

            _loginBtn = CreateButton(panel, "Iniciar sesión", OnLoginClicked, primary: true);

            _status = CreateLabel(panel, "Ingresa tus datos y pulsa Iniciar sesión.", 14, FontStyle.Normal, 4);
            _status.alignment = TextAnchor.UpperLeft;
            var statusLe = _status.GetComponent<LayoutElement>();
            statusLe.preferredHeight = 72;
            statusLe.minHeight = 56;

            // —— Acciones posteriores (sesión / turno) ——
            var sep = CreateLabel(panel, "── Después del login ──", 13, FontStyle.Italic, 2);
            sep.alignment = TextAnchor.MiddleCenter;
            sep.color = new Color(1f, 1f, 1f, 0.45f);

            var row = CreateRow(panel);
            _sessionBtn = CreateButton(row.transform, "Crear sesión", OnCreateSessionClicked);
            _closeBtn = CreateButton(row.transform, "Cerrar sesión", OnCloseSessionClicked);

            var row2 = CreateRow(panel);
            _turnWavBtn = CreateButton(row2.transform, "Turno WAV prueba", OnTurnWavClicked);
            _turnMicBtn = CreateButton(row2.transform, "Turno micrófono (3s)", OnTurnMicClicked);

            _stateLabel = CreateLabel(panel, "Estado UI: idle", 15, FontStyle.Bold, 2);
            _replyLabel = CreateLabel(panel, "Respuesta: (sin respuesta aún)", 14, FontStyle.Normal, 0);
            _replyLabel.color = new Color(0.75f, 0.95f, 0.8f, 1f);
            var replyLe = _replyLabel.GetComponent<LayoutElement>();
            replyLe.preferredHeight = 48;
        }

        Transform CreatePanel(Transform parent)
        {
            var go = new GameObject("LoginCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // Tamaño fijo (evita que ContentSizeFitter deje el panel en 0)
            rt.sizeDelta = new Vector2(560, 720);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 32, 28);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return go.transform;
        }

        Text CreateLabel(Transform parent, string text, int size, FontStyle style, int bottomPad)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = UiFont();
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<LayoutElement>().preferredHeight = size + 10 + bottomPad;
            go.GetComponent<LayoutElement>().minHeight = size + 8;
            return t;
        }

        InputField CreateInput(Transform parent, string placeholder, string value)
        {
            var wrap = new GameObject(placeholder + "Wrap", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            wrap.transform.SetParent(parent, false);
            var v = wrap.GetComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;
            wrap.GetComponent<LayoutElement>().preferredHeight = 58;

            CreateLabel(wrap.transform, placeholder, 12, FontStyle.Normal, 0);

            var go = new GameObject(placeholder, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(wrap.transform, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            go.GetComponent<LayoutElement>().preferredHeight = 30;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = UiFont();
            text.fontSize = 15;
            text.color = Color.white;
            text.supportRichText = false;
            var textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 4);
            textRt.offsetMax = new Vector2(-10, -4);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            phGo.transform.SetParent(go.transform, false);
            var ph = phGo.GetComponent<Text>();
            ph.font = text.font;
            ph.fontSize = 15;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(1f, 1f, 1f, 0.35f);
            ph.text = placeholder;
            var phRt = ph.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(10, 4);
            phRt.offsetMax = new Vector2(-10, -4);

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = ph;
            input.text = value;
            return input;
        }

        GameObject CreateRow(Transform parent)
        {
            var go = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            go.GetComponent<LayoutElement>().preferredHeight = 40;
            return go;
        }

        Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, bool primary = false)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = primary
                ? new Color(0.15f, 0.62f, 0.45f, 1f)
                : new Color(0.22f, 0.48f, 0.78f, 1f);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = primary ? 48 : 38;
            if (primary)
                le.minHeight = 48;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = label;
            t.font = UiFont();
            t.fontSize = primary ? 18 : 14;
            t.fontStyle = primary ? FontStyle.Bold : FontStyle.Normal;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);
            return btn;
        }

        void OnLoginClicked()
        {
            if (_busy)
                return;
            EmpathiaAuthState.BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.text)
                ? "http://127.0.0.1:8000/api/v1"
                : _baseUrl.text.Trim();

            SetBusy(true);
            SetUiState("processing");
            SetStatus("Autenticando contra B…");
            StartCoroutine(_api.Login(_user.text.Trim(), _pass.text, (ok, msg) =>
            {
                SetBusy(false);
                SetUiState("idle");
                if (ok)
                {
                    SetStatus(
                        "Login OK\n"
                        + "Usuario: " + EmpathiaAuthState.Username + "\n"
                        + "Token: " + EmpathiaAuthState.TokenPreview + "\n"
                        + "Ya puedes crear una sesión.");
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
                SetStatus("Generando WAV de prueba (silencio corto)…");
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

            SetReply(result.ReplyText);
            SetStatus("Respuesta lista.\nTranscript: " + result.Transcript
                      + "\nTTS: " + result.TtsUrl
                      + "\n" + msg);
            Debug.Log("[Empathia] reply_text=" + result.ReplyText);

            SetUiState("speaking");
            var ttsOk = false;
            var ttsMsg = "";
            yield return _api.DownloadAndPlayTts(result.TtsUrl, _audio, (success, message) =>
            {
                ttsOk = success;
                ttsMsg = message;
            });

            SetStatus((ttsOk ? ttsMsg : "Error TTS: " + ttsMsg)
                      + "\n\nRespuesta:\n" + result.ReplyText);

            if (ttsOk && _audio != null && _audio.clip != null)
                yield return new WaitForSeconds(_audio.clip.length + 0.1f);

            SetBusy(false);
            SetUiState("idle");
        }

        IEnumerator CaptureMic(float seconds, System.Action<byte[]> onDone)
        {
            byte[] result = null;
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

            result = EmpathiaWav.FromMicrophoneClip(clip, rate);
            if (clip != null)
                Destroy(clip);
            onDone(result);
        }

        void SetBusy(bool busy)
        {
            _busy = busy;
            _loginBtn.interactable = !busy;
            _sessionBtn.interactable = !busy;
            _closeBtn.interactable = !busy;
            _turnWavBtn.interactable = !busy;
            _turnMicBtn.interactable = !busy;
        }

        void SetUiState(string state)
        {
            if (_stateLabel != null)
                _stateLabel.text = "Estado UI: " + state;
        }

        void SetStatus(string message)
        {
            if (_status != null)
                _status.text = message;
        }

        void SetReply(string reply)
        {
            if (_replyLabel != null)
                _replyLabel.text = "Respuesta: " + (string.IsNullOrEmpty(reply) ? "(vacía)" : reply);
        }
    }
}
