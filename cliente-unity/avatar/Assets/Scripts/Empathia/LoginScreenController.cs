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
    /// Login A: UI responsiva (adapta tarjeta, tipografía y filas a ancho/alto).
    /// Visible en la pestaña Game durante Play.
    /// </summary>
    public class LoginScreenController : MonoBehaviour
    {
        const float MicSeconds = 3f;
        const float NarrowBreakpoint = 700f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<LoginScreenController>() != null)
                return;

            var go = new GameObject("EmpathiaLogin");
            DontDestroyOnLoad(go);
            go.AddComponent<EmpathiaApiClient>();
            go.AddComponent<AudioSource>();
            go.AddComponent<LoginScreenController>();
        }

        EmpathiaApiClient _api;
        AudioSource _audio;
        Font _font;

        RectTransform _cardRt;
        RectTransform _contentRt;
        VerticalLayoutGroup _contentLayout;
        LayoutElement _cardLayout;
        CanvasScaler _scaler;
        Transform _row1;
        Transform _row2;
        HorizontalLayoutGroup _row1Layout;
        HorizontalLayoutGroup _row2Layout;

        InputField _baseUrl;
        InputField _user;
        InputField _pass;
        Text _brand;
        Text _title;
        Text _subtitle;
        Text _status;
        Text _state;
        Text _reply;
        Text _sep;
        Button _loginBtn;
        Button _sessionBtn;
        Button _closeBtn;
        Button _turnWavBtn;
        Button _turnMicBtn;
        bool _busy;
        bool _built;
        Vector2 _lastScreen;
        bool _narrow;

        void Awake()
        {
            try
            {
                _api = GetComponent<EmpathiaApiClient>() ?? gameObject.AddComponent<EmpathiaApiClient>();
                _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                EnsureEventSystem();
                BuildUi();
                ApplyResponsiveLayout(force: true);
                SetStatus("Sesión/login listos para probar.\nServidor B por defecto: 192.168.1.78:8000\nLab: estudiante1 / password");
                Debug.Log("[Empathia] UI responsiva lista. Usa pestaña Game + Play (Ctrl+P).");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Empathia] Error creando UI: " + ex);
            }
        }

        void Update()
        {
            if (!_built)
                return;
            var size = new Vector2(Screen.width, Screen.height);
            if (size != _lastScreen)
                ApplyResponsiveLayout(force: false);
        }

        void EnsureEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            if (es.GetComponent<StandaloneInputModule>() == null)
                es.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        Font GetFont()
        {
            if (_font != null)
                return _font;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                    ?? Font.CreateDynamicFontFromOSFont("Arial", 16);
            return _font;
        }

        void BuildUi()
        {
            if (_built)
                return;
            _built = true;

            var canvasGo = new GameObject("EmpathiaLoginCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            _scaler = canvasGo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = CreateImage(canvasGo.transform, "BG", new Color(0.07f, 0.1f, 0.14f, 1f));
            StretchFull(bg.rectTransform);

            // Contenedor seguro (márgenes % de pantalla)
            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(canvasGo.transform, false);
            var safeRt = safe.GetComponent<RectTransform>();
            StretchFull(safeRt);

            var card = CreateImage(safe.transform, "Card", new Color(0.14f, 0.18f, 0.24f, 0.98f));
            _cardRt = card.rectTransform;
            _cardRt.anchorMin = _cardRt.anchorMax = _cardRt.pivot = new Vector2(0.5f, 0.5f);
            _cardLayout = card.gameObject.AddComponent<LayoutElement>();

            // Scroll interno para pantallas bajas
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollGo.transform.SetParent(card.transform, false);
            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.01f);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            StretchFull(scrollRt);
            scrollRt.offsetMin = new Vector2(12, 12);
            scrollRt.offsetMax = new Vector2(-12, -12);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(scrollGo.transform, false);
            _contentRt = content.GetComponent<RectTransform>();
            _contentRt.anchorMin = new Vector2(0, 1);
            _contentRt.anchorMax = new Vector2(1, 1);
            _contentRt.pivot = new Vector2(0.5f, 1);
            _contentRt.anchoredPosition = Vector2.zero;
            _contentRt.sizeDelta = new Vector2(0, 0);

            _contentLayout = content.GetComponent<VerticalLayoutGroup>();
            _contentLayout.padding = new RectOffset(20, 20, 16, 16);
            _contentLayout.spacing = 8;
            _contentLayout.childAlignment = TextAnchor.UpperCenter;
            _contentLayout.childControlWidth = true;
            _contentLayout.childControlHeight = false;
            _contentLayout.childForceExpandWidth = true;
            _contentLayout.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = _contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            _brand = AddLabel(_contentRt, "EmpathIA", 32, FontStyle.Bold, new Color(0.55f, 0.85f, 1f), 40, TextAnchor.MiddleCenter);
            _title = AddLabel(_contentRt, "Inicio de sesión", 24, FontStyle.Bold, Color.white, 34, TextAnchor.MiddleCenter);
            _subtitle = AddLabel(_contentRt, "Autenticación contra servidor B", 14, FontStyle.Normal, new Color(1, 1, 1, 0.7f), 24, TextAnchor.MiddleCenter);

            _baseUrl = AddInput(_contentRt, "Servidor", EmpathiaAuthState.BaseUrl);
            _user = AddInput(_contentRt, "Usuario", "estudiante1");
            _pass = AddInput(_contentRt, "Contraseña", "password");
            _pass.contentType = InputField.ContentType.Password;

            _loginBtn = AddButton(_contentRt, "Iniciar sesión", new Color(0.16f, 0.65f, 0.45f), 46, OnLogin);
            _state = AddLabel(_contentRt, "Estado UI: idle", 14, FontStyle.Bold, Color.white, 24, TextAnchor.MiddleLeft);
            _status = AddLabel(_contentRt, "", 13, FontStyle.Normal, new Color(0.9f, 0.92f, 0.95f), 88, TextAnchor.UpperLeft);

            _sep = AddLabel(_contentRt, "── Después del login ──", 12, FontStyle.Italic, new Color(1, 1, 1, 0.45f), 22, TextAnchor.MiddleCenter);

            _row1 = AddRow(_contentRt);
            _row1Layout = _row1.GetComponent<HorizontalLayoutGroup>();
            _sessionBtn = AddButton(_row1, "Crear sesión", new Color(0.22f, 0.48f, 0.78f), 38, OnCreateSession);
            _closeBtn = AddButton(_row1, "Cerrar sesión", new Color(0.22f, 0.48f, 0.78f), 38, OnCloseSession);

            _row2 = AddRow(_contentRt);
            _row2Layout = _row2.GetComponent<HorizontalLayoutGroup>();
            _turnWavBtn = AddButton(_row2, "Turno WAV", new Color(0.22f, 0.48f, 0.78f), 38, () => StartCoroutine(RunTurn(false)));
            _turnMicBtn = AddButton(_row2, "Turno mic 3s", new Color(0.22f, 0.48f, 0.78f), 38, () => StartCoroutine(RunTurn(true)));

            _reply = AddLabel(_contentRt, "Respuesta: (sin respuesta)", 13, FontStyle.Normal, new Color(0.75f, 0.95f, 0.8f), 48, TextAnchor.UpperLeft);
        }

        void ApplyResponsiveLayout(bool force)
        {
            _lastScreen = new Vector2(Screen.width, Screen.height);
            if (_scaler == null || _cardRt == null)
                return;

            var aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
            // Más ancho → priorizar altura; más alto/estrecho → priorizar ancho
            _scaler.matchWidthOrHeight = aspect >= 1.2f ? 0.35f : 0.65f;

            var refW = _scaler.referenceResolution.x;
            var scale = Mathf.Lerp(Screen.width / refW, Screen.height / _scaler.referenceResolution.y, _scaler.matchWidthOrHeight);
            var canvasW = Screen.width / Mathf.Max(0.01f, scale);
            var canvasH = Screen.height / Mathf.Max(0.01f, scale);

            var narrow = canvasW < NarrowBreakpoint;
            if (!force && narrow == _narrow)
            {
                // Solo recalcular tamaño de tarjeta
            }
            _narrow = narrow;

            var marginX = narrow ? canvasW * 0.04f : canvasW * 0.08f;
            var marginY = narrow ? canvasH * 0.03f : canvasH * 0.06f;
            var cardW = Mathf.Clamp(canvasW - marginX * 2f, 320f, 560f);
            var cardH = Mathf.Clamp(canvasH - marginY * 2f, 420f, 760f);

            _cardRt.sizeDelta = new Vector2(cardW, cardH);
            if (_cardLayout != null)
            {
                _cardLayout.preferredWidth = cardW;
                _cardLayout.preferredHeight = cardH;
            }

            if (_contentLayout != null)
            {
                var pad = narrow ? 14 : 22;
                _contentLayout.padding = new RectOffset(pad, pad, pad, pad);
                _contentLayout.spacing = narrow ? 6 : 10;
            }

            // Filas: en pantallas estrechas apilar botones
            ConfigureRow(_row1Layout, _row1, narrow);
            ConfigureRow(_row2Layout, _row2, narrow);

            SetTextSize(_brand, narrow ? 26 : 32);
            SetTextSize(_title, narrow ? 20 : 24);
            SetTextSize(_subtitle, narrow ? 12 : 14);
            SetTextSize(_status, narrow ? 12 : 13);
            SetTextSize(_reply, narrow ? 12 : 13);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRt);
        }

        static void ConfigureRow(HorizontalLayoutGroup h, Transform row, bool narrow)
        {
            if (h == null || row == null)
                return;

            // HorizontalLayoutGroup no apila; cambiamos preferredHeight del row y usamos
            // childForceExpand. Para apilar de verdad, usamos VerticalLayoutGroup swap.
            var vertical = row.GetComponent<VerticalLayoutGroup>();
            var le = row.GetComponent<LayoutElement>();

            if (narrow)
            {
                h.enabled = false;
                if (vertical == null)
                {
                    vertical = row.gameObject.AddComponent<VerticalLayoutGroup>();
                    vertical.spacing = 8;
                    vertical.childForceExpandWidth = true;
                    vertical.childForceExpandHeight = false;
                    vertical.childControlWidth = true;
                    vertical.childControlHeight = false;
                }
                vertical.enabled = true;
                if (le != null)
                    le.preferredHeight = 86;
            }
            else
            {
                if (vertical != null)
                    vertical.enabled = false;
                h.enabled = true;
                h.spacing = 8;
                h.childForceExpandWidth = true;
                h.childForceExpandHeight = true;
                if (le != null)
                    le.preferredHeight = 40;
            }
        }

        static void SetTextSize(Text t, int size)
        {
            if (t != null)
                t.fontSize = size;
        }

        static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Text AddLabel(Transform parent, string text, int size, FontStyle style, Color color, float height, TextAnchor align)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = GetFont();
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height * 0.5f;
            le.flexibleWidth = 1;
            return t;
        }

        InputField AddInput(Transform parent, string title, string value)
        {
            var wrap = new GameObject(title + "Wrap", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            wrap.transform.SetParent(parent, false);
            var v = wrap.GetComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;
            wrap.GetComponent<LayoutElement>().preferredHeight = 58;
            wrap.GetComponent<LayoutElement>().flexibleWidth = 1;

            AddLabel(wrap.transform, title, 12, FontStyle.Normal, new Color(1, 1, 1, 0.75f), 18, TextAnchor.MiddleLeft);

            var fieldGo = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            fieldGo.transform.SetParent(wrap.transform, false);
            fieldGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.12f);
            fieldGo.GetComponent<LayoutElement>().preferredHeight = 34;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(fieldGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = 15;
            text.color = Color.white;
            text.supportRichText = false;
            StretchFull(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(10, 4);
            text.rectTransform.offsetMax = new Vector2(-10, -4);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            phGo.transform.SetParent(fieldGo.transform, false);
            var ph = phGo.GetComponent<Text>();
            ph.font = GetFont();
            ph.fontSize = 15;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(1, 1, 1, 0.35f);
            ph.text = title;
            StretchFull(ph.rectTransform);
            ph.rectTransform.offsetMin = new Vector2(10, 4);
            ph.rectTransform.offsetMax = new Vector2(-10, -4);

            var input = fieldGo.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = ph;
            input.text = value ?? "";
            return input;
        }

        Transform AddRow(Transform parent)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            h.childControlWidth = true;
            h.childControlHeight = true;
            go.GetComponent<LayoutElement>().preferredHeight = 40;
            go.GetComponent<LayoutElement>().flexibleWidth = 1;
            return go.transform;
        }

        Button AddButton(Transform parent, string label, Color color, float height, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = 32;
            le.flexibleWidth = 1;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = label;
            t.font = GetFont();
            t.fontSize = 15;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            StretchFull(t.rectTransform);

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);
            return btn;
        }

        void OnLogin()
        {
            if (_busy)
                return;
            EmpathiaAuthState.BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.text)
                ? "http://192.168.1.78:8000/api/v1"
                : _baseUrl.text.Trim();

            SetBusy(true);
            SetState("processing");
            SetStatus("Autenticando contra B…");
            StartCoroutine(_api.Login(_user.text.Trim(), _pass.text, (ok, msg) =>
            {
                SetBusy(false);
                SetState("idle");
                SetStatus(ok
                    ? "Login OK\nUsuario: " + EmpathiaAuthState.Username + "\nToken: " + EmpathiaAuthState.TokenPreview
                    : "Error de autenticación:\n" + msg);
                Debug.Log(ok ? "[Empathia] " + msg : "[Empathia] ERROR " + msg);
            }));
        }

        void OnCreateSession()
        {
            if (_busy)
                return;
            SetBusy(true);
            SetState("processing");
            SetStatus("Creando sesión…");
            StartCoroutine(_api.CreateSession((ok, msg) =>
            {
                SetBusy(false);
                SetState("idle");
                SetStatus(ok ? msg : "Error: " + msg);
            }));
        }

        void OnCloseSession()
        {
            if (_busy)
                return;
            SetBusy(true);
            SetState("processing");
            SetStatus("Cerrando sesión…");
            StartCoroutine(_api.CloseSession((ok, msg) =>
            {
                SetBusy(false);
                SetState("idle");
                SetStatus(ok ? msg : "Error: " + msg);
            }));
        }

        IEnumerator RunTurn(bool useMic)
        {
            SetBusy(true);
            SetReply("(esperando…)");
            byte[] wav = null;

            if (useMic)
            {
                SetState("listening");
                SetStatus("Grabando micrófono…");
                yield return CaptureMic(bytes => wav = bytes);
                if (wav == null)
                {
                    SetBusy(false);
                    SetState("idle");
                    SetStatus("No se pudo grabar mic. Usa Turno WAV.");
                    yield break;
                }
            }
            else
            {
                SetState("listening");
                SetStatus("Generando WAV de prueba…");
                wav = EmpathiaWav.BuildSilentWav();
                yield return null;
            }

            SetState("processing");
            TurnResultInfo result = null;
            var ok = false;
            var msg = "";
            yield return _api.RunTurn(wav, SetStatus, (success, info, message) =>
            {
                ok = success;
                result = info;
                msg = message;
            });

            if (!ok || result == null)
            {
                SetBusy(false);
                SetState("idle");
                SetReply("(error)");
                SetStatus("Error: " + msg);
                yield break;
            }

            SetReply(result.ReplyText ?? "(vacía)");
            SetState("speaking");
            var ttsOk = false;
            var ttsMsg = "";
            yield return _api.DownloadAndPlayTts(result.TtsUrl, _audio, (s, m) =>
            {
                ttsOk = s;
                ttsMsg = m;
            });
            SetStatus((ttsOk ? ttsMsg : "Error TTS: " + ttsMsg) + "\n" + result.ReplyText);
            if (ttsOk && _audio.clip != null)
                yield return new WaitForSeconds(_audio.clip.length + 0.1f);
            SetBusy(false);
            SetState("idle");
        }

        IEnumerator CaptureMic(System.Action<byte[]> done)
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                done(null);
                yield break;
            }

            var device = Microphone.devices[0];
            var clip = Microphone.Start(device, false, Mathf.CeilToInt(MicSeconds) + 1, 16000);
            var t0 = Time.realtimeSinceStartup;
            while (Microphone.GetPosition(device) <= 0 && Time.realtimeSinceStartup - t0 < 2f)
                yield return null;
            yield return new WaitForSeconds(MicSeconds);
            Microphone.End(device);
            var bytes = EmpathiaWav.FromMicrophoneClip(clip);
            if (clip != null)
                Destroy(clip);
            done(bytes);
        }

        void SetBusy(bool busy)
        {
            _busy = busy;
            if (_loginBtn != null) _loginBtn.interactable = !busy;
            if (_sessionBtn != null) _sessionBtn.interactable = !busy;
            if (_closeBtn != null) _closeBtn.interactable = !busy;
            if (_turnWavBtn != null) _turnWavBtn.interactable = !busy;
            if (_turnMicBtn != null) _turnMicBtn.interactable = !busy;
        }

        void SetState(string s)
        {
            if (_state != null)
                _state.text = "Estado UI: " + s;
        }

        void SetStatus(string s)
        {
            if (_status != null)
                _status.text = s;
        }

        void SetReply(string s)
        {
            if (_reply != null)
                _reply.text = "Respuesta: " + s;
        }
    }
}
