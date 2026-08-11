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
    /// Login A: UI responsiva, sin desbordes (scroll + máscara) y bordes redondeados.
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
        Sprite _roundLg;
        Sprite _roundSm;

        RectTransform _cardRt;
        RectTransform _viewportRt;
        RectTransform _contentRt;
        VerticalLayoutGroup _contentLayout;
        CanvasScaler _scaler;
        ScrollRect _scroll;
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
                ApplyResponsiveLayout();
                SetStatus("Login listo.\nB: 192.168.1.78:8000\nLab: estudiante1 / password");
                Debug.Log("[Empathia] UI sin desbordes + bordes redondeados. Pestaña Game + Play.");
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
                ApplyResponsiveLayout();
        }

        void EnsureEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
                es = new GameObject("EventSystem").AddComponent<EventSystem>();

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

        Sprite RoundSprite(bool large)
        {
            if (large)
            {
                if (_roundLg == null)
                    _roundLg = BuildRoundedSprite(64, 18);
                return _roundLg;
            }

            if (_roundSm == null)
                _roundSm = BuildRoundedSprite(64, 14);
            return _roundSm;
        }

        static Sprite BuildRoundedSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[size * size];
            float r = radius;
            float max = size - 1;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    float dx = 0f;
                    float dy = 0f;
                    if (x < r && y < r)
                    {
                        dx = r - x;
                        dy = r - y;
                    }
                    else if (x > max - r && y < r)
                    {
                        dx = x - (max - r);
                        dy = r - y;
                    }
                    else if (x < r && y > max - r)
                    {
                        dx = r - x;
                        dy = y - (max - r);
                    }
                    else if (x > max - r && y > max - r)
                    {
                        dx = x - (max - r);
                        dy = y - (max - r);
                    }

                    byte a = 255;
                    if (dx > 0f || dy > 0f)
                    {
                        var dist = Mathf.Sqrt(dx * dx + dy * dy);
                        a = dist <= r - 0.5f ? (byte)255 : dist >= r + 0.5f ? (byte)0 : (byte)Mathf.Clamp(Mathf.RoundToInt((r + 0.5f - dist) * 255f), 0, 255);
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        void ApplyRounded(Image img, bool large)
        {
            img.sprite = RoundSprite(large);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
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

            var bg = CreateImage(canvasGo.transform, "BG", new Color(0.07f, 0.1f, 0.14f, 1f), rounded: false);
            StretchFull(bg.rectTransform);

            var card = CreateImage(canvasGo.transform, "Card", new Color(0.14f, 0.18f, 0.24f, 1f), rounded: true, large: true);
            _cardRt = card.rectTransform;
            _cardRt.anchorMin = _cardRt.anchorMax = _cardRt.pivot = new Vector2(0.5f, 0.5f);

            // Viewport + Mask (recorta todo lo que sobresalga)
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(card.transform, false);
            _viewportRt = viewportGo.GetComponent<RectTransform>();
            StretchFull(_viewportRt);
            _viewportRt.offsetMin = new Vector2(16, 16);
            _viewportRt.offsetMax = new Vector2(-16, -16);
            var vpImg = viewportGo.GetComponent<Image>();
            vpImg.color = Color.white;
            ApplyRounded(vpImg, large: true);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewportGo.transform, false);
            _contentRt = content.GetComponent<RectTransform>();
            _contentRt.anchorMin = new Vector2(0f, 1f);
            _contentRt.anchorMax = new Vector2(1f, 1f);
            _contentRt.pivot = new Vector2(0.5f, 1f);
            _contentRt.anchoredPosition = Vector2.zero;
            _contentRt.sizeDelta = new Vector2(0f, 0f);

            _contentLayout = content.GetComponent<VerticalLayoutGroup>();
            _contentLayout.padding = new RectOffset(18, 18, 12, 18);
            _contentLayout.spacing = 8;
            _contentLayout.childAlignment = TextAnchor.UpperCenter;
            _contentLayout.childControlWidth = true;
            _contentLayout.childControlHeight = true;
            _contentLayout.childForceExpandWidth = true;
            _contentLayout.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll = card.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _contentRt;
            _scroll.viewport = _viewportRt;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 40f;
            _scroll.inertia = true;

            _brand = AddLabel(_contentRt, "EmpathIA", 30, FontStyle.Bold, new Color(0.55f, 0.85f, 1f), 36, TextAnchor.MiddleCenter);
            _title = AddLabel(_contentRt, "Inicio de sesión", 22, FontStyle.Bold, Color.white, 30, TextAnchor.MiddleCenter);
            _subtitle = AddLabel(_contentRt, "Autenticación contra servidor B", 13, FontStyle.Normal, new Color(1, 1, 1, 0.7f), 22, TextAnchor.MiddleCenter);

            _baseUrl = AddInput(_contentRt, "Servidor", EmpathiaAuthState.BaseUrl);
            _user = AddInput(_contentRt, "Usuario", "estudiante1");
            _pass = AddInput(_contentRt, "Contraseña", "password");
            _pass.contentType = InputField.ContentType.Password;

            _loginBtn = AddButton(_contentRt, "Iniciar sesión", new Color(0.16f, 0.65f, 0.45f), 44, OnLogin);
            _state = AddLabel(_contentRt, "Estado UI: idle", 14, FontStyle.Bold, Color.white, 22, TextAnchor.MiddleLeft);
            _status = AddLabel(_contentRt, "", 12, FontStyle.Normal, new Color(0.9f, 0.92f, 0.95f), 72, TextAnchor.UpperLeft);

            AddLabel(_contentRt, "── Después del login ──", 12, FontStyle.Italic, new Color(1, 1, 1, 0.45f), 20, TextAnchor.MiddleCenter);

            _row1 = AddRow(_contentRt);
            _row1Layout = _row1.GetComponent<HorizontalLayoutGroup>();
            _sessionBtn = AddButton(_row1, "Crear sesión", new Color(0.22f, 0.48f, 0.78f), 36, OnCreateSession);
            _closeBtn = AddButton(_row1, "Cerrar sesión", new Color(0.22f, 0.48f, 0.78f), 36, OnCloseSession);

            _row2 = AddRow(_contentRt);
            _row2Layout = _row2.GetComponent<HorizontalLayoutGroup>();
            _turnWavBtn = AddButton(_row2, "Turno WAV", new Color(0.22f, 0.48f, 0.78f), 36, () => StartCoroutine(RunTurn(false)));
            _turnMicBtn = AddButton(_row2, "Turno mic 3s", new Color(0.22f, 0.48f, 0.78f), 36, () => StartCoroutine(RunTurn(true)));

            _reply = AddLabel(_contentRt, "Respuesta: (sin respuesta)", 12, FontStyle.Normal, new Color(0.75f, 0.95f, 0.8f), 44, TextAnchor.UpperLeft);
        }

        void ApplyResponsiveLayout()
        {
            _lastScreen = new Vector2(Screen.width, Screen.height);
            if (_scaler == null || _cardRt == null)
                return;

            var aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
            _scaler.matchWidthOrHeight = aspect >= 1.15f ? 0.3f : 0.7f;

            var scale = Mathf.Lerp(
                Screen.width / _scaler.referenceResolution.x,
                Screen.height / _scaler.referenceResolution.y,
                _scaler.matchWidthOrHeight);
            var canvasW = Screen.width / Mathf.Max(0.01f, scale);
            var canvasH = Screen.height / Mathf.Max(0.01f, scale);

            _narrow = canvasW < NarrowBreakpoint;

            // Tarjeta casi a pantalla completa (evita recortes)
            var cardW = Mathf.Clamp(canvasW * (_narrow ? 0.96f : 0.9f), 300f, 560f);
            var cardH = Mathf.Clamp(canvasH * 0.94f, 460f, canvasH * 0.96f);
            _cardRt.sizeDelta = new Vector2(cardW, cardH);

            if (_contentLayout != null)
            {
                var pad = _narrow ? 12 : 18;
                _contentLayout.padding = new RectOffset(pad, pad, 10, 16);
                _contentLayout.spacing = _narrow ? 6 : 8;
            }

            ConfigureRow(_row1Layout, _row1, _narrow);
            ConfigureRow(_row2Layout, _row2, _narrow);

            SetTextSize(_brand, _narrow ? 24 : 30);
            SetTextSize(_title, _narrow ? 18 : 22);
            SetTextSize(_subtitle, _narrow ? 11 : 13);

            // URL larga: tamaño adaptable
            if (_baseUrl != null && _baseUrl.textComponent != null)
            {
                _baseUrl.textComponent.resizeTextForBestFit = true;
                _baseUrl.textComponent.resizeTextMinSize = 9;
                _baseUrl.textComponent.resizeTextMaxSize = 14;
                _baseUrl.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            Canvas.ForceUpdateCanvases();
            if (_contentRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRt);
            if (_scroll != null)
                _scroll.verticalNormalizedPosition = 1f;
        }

        static void ConfigureRow(HorizontalLayoutGroup h, Transform row, bool narrow)
        {
            if (h == null || row == null)
                return;

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
                    vertical.childControlHeight = true;
                }

                vertical.enabled = true;
                if (le != null)
                {
                    le.preferredHeight = 84;
                    le.minHeight = 84;
                }
            }
            else
            {
                if (vertical != null)
                    vertical.enabled = false;
                h.enabled = true;
                if (le != null)
                {
                    le.preferredHeight = 38;
                    le.minHeight = 38;
                }
            }
        }

        static void SetTextSize(Text t, int size)
        {
            if (t != null)
                t.fontSize = size;
        }

        Image CreateImage(Transform parent, string name, Color color, bool rounded, bool large = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            if (rounded)
                ApplyRounded(img, large);
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
            le.minHeight = height;
            le.flexibleWidth = 1f;
            return t;
        }

        InputField AddInput(Transform parent, string title, string value)
        {
            var wrap = new GameObject(title + "Wrap", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            wrap.transform.SetParent(parent, false);
            var v = wrap.GetComponent<VerticalLayoutGroup>();
            v.spacing = 3;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            var wrapLe = wrap.GetComponent<LayoutElement>();
            wrapLe.preferredHeight = 56;
            wrapLe.minHeight = 56;
            wrapLe.flexibleWidth = 1f;

            AddLabel(wrap.transform, title, 12, FontStyle.Normal, new Color(1, 1, 1, 0.75f), 16, TextAnchor.MiddleLeft);

            var fieldGo = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            fieldGo.transform.SetParent(wrap.transform, false);
            var fieldImg = fieldGo.GetComponent<Image>();
            fieldImg.color = new Color(1f, 1f, 1f, 0.14f);
            ApplyRounded(fieldImg, large: false);
            fieldGo.GetComponent<LayoutElement>().preferredHeight = 34;
            fieldGo.GetComponent<LayoutElement>().minHeight = 34;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(fieldGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = 14;
            text.color = Color.white;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            StretchFull(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(12, 4);
            text.rectTransform.offsetMax = new Vector2(-12, -4);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            phGo.transform.SetParent(fieldGo.transform, false);
            var ph = phGo.GetComponent<Text>();
            ph.font = GetFont();
            ph.fontSize = 14;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(1, 1, 1, 0.35f);
            ph.text = title;
            StretchFull(ph.rectTransform);
            ph.rectTransform.offsetMin = new Vector2(12, 4);
            ph.rectTransform.offsetMax = new Vector2(-12, -4);

            var input = fieldGo.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = ph;
            input.text = value ?? "";
            input.lineType = InputField.LineType.SingleLine;
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
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 38;
            le.minHeight = 38;
            le.flexibleWidth = 1f;
            return go.transform;
        }

        Button AddButton(Transform parent, string label, Color color, float height, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            ApplyRounded(img, large: false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleWidth = 1f;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = label;
            t.font = GetFont();
            t.fontSize = 14;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 10;
            t.resizeTextMaxSize = 15;
            StretchFull(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(8, 2);
            t.rectTransform.offsetMax = new Vector2(-8, -2);

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
