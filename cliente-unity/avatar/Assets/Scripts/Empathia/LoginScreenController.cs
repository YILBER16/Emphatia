using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Empathia
{
    /// <summary>
    /// Login A: UI de alta calidad (TextMeshPro + sprites HD + CanvasScaler nítido).
    /// </summary>
    public class LoginScreenController : MonoBehaviour
    {
        const float MicSeconds = 3f;
        const float NarrowBreakpoint = 900f;

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
        TMP_FontAsset _tmpFont;
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

        TMP_InputField _baseUrl;
        TMP_InputField _user;
        TMP_InputField _pass;
        TextMeshProUGUI _brand;
        TextMeshProUGUI _title;
        TextMeshProUGUI _subtitle;
        TextMeshProUGUI _status;
        TextMeshProUGUI _state;
        TextMeshProUGUI _reply;
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
                Debug.Log("[Empathia] UI alta calidad (TMP + HD). Pestaña Game + Play.");
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

        TMP_FontAsset GetTmpFont()
        {
            if (_tmpFont != null)
                return _tmpFont;

            _tmpFont = TMP_Settings.defaultFontAsset;
            if (_tmpFont == null)
            {
                _tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            return _tmpFont;
        }

        Sprite RoundSprite(bool large)
        {
            if (large)
            {
                if (_roundLg == null)
                    _roundLg = BuildRoundedSprite(256, 48);
                return _roundLg;
            }

            if (_roundSm == null)
                _roundSm = BuildRoundedSprite(128, 28);
            return _roundSm;
        }

        static Sprite BuildRoundedSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
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
                        dx = r - x - 0.5f;
                        dy = r - y - 0.5f;
                    }
                    else if (x > max - r && y < r)
                    {
                        dx = x - (max - r) - 0.5f;
                        dy = r - y - 0.5f;
                    }
                    else if (x < r && y > max - r)
                    {
                        dx = r - x - 0.5f;
                        dy = y - (max - r) - 0.5f;
                    }
                    else if (x > max - r && y > max - r)
                    {
                        dx = x - (max - r) - 0.5f;
                        dy = y - (max - r) - 0.5f;
                    }

                    byte a = 255;
                    if (dx != 0f || dy != 0f)
                    {
                        var dist = Mathf.Sqrt(dx * dx + dy * dy);
                        // Antialias suave (~1.5 px)
                        var aa = 1.5f;
                        if (dist >= r + aa)
                            a = 0;
                        else if (dist > r - aa)
                            a = (byte)Mathf.Clamp(Mathf.RoundToInt(((r + aa) - dist) / (aa * 2f) * 255f), 0, 255);
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
            img.pixelsPerUnitMultiplier = large ? 1.15f : 1.25f;
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
            canvas.pixelPerfect = false;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.Normal |
                AdditionalCanvasShaderChannels.Tangent;

            _scaler = canvasGo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Referencia alta para más nitidez en pantallas grandes / HiDPI
            _scaler.referenceResolution = new Vector2(2560, 1440);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;
            _scaler.referencePixelsPerUnit = 100f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = CreateImage(canvasGo.transform, "BG", new Color(0.06f, 0.08f, 0.12f, 1f), rounded: false);
            StretchFull(bg.rectTransform);

            var card = CreateImage(canvasGo.transform, "Card", new Color(0.13f, 0.17f, 0.23f, 1f), rounded: true, large: true);
            _cardRt = card.rectTransform;
            _cardRt.anchorMin = _cardRt.anchorMax = _cardRt.pivot = new Vector2(0.5f, 0.5f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(card.transform, false);
            _viewportRt = viewportGo.GetComponent<RectTransform>();
            StretchFull(_viewportRt);
            _viewportRt.offsetMin = new Vector2(20, 20);
            _viewportRt.offsetMax = new Vector2(-20, -20);
            var vpImg = viewportGo.GetComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0.002f);
            vpImg.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewportGo.transform, false);
            _contentRt = content.GetComponent<RectTransform>();
            _contentRt.anchorMin = new Vector2(0f, 1f);
            _contentRt.anchorMax = new Vector2(1f, 1f);
            _contentRt.pivot = new Vector2(0.5f, 1f);
            _contentRt.anchoredPosition = Vector2.zero;
            _contentRt.sizeDelta = Vector2.zero;

            _contentLayout = content.GetComponent<VerticalLayoutGroup>();
            _contentLayout.padding = new RectOffset(22, 22, 14, 22);
            _contentLayout.spacing = 10;
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
            _scroll.scrollSensitivity = 45f;

            _brand = AddLabel(_contentRt, "EmpathIA", 36, FontStyles.Bold, new Color(0.55f, 0.86f, 1f), 42, TextAlignmentOptions.Center);
            _title = AddLabel(_contentRt, "Inicio de sesión", 26, FontStyles.Bold, Color.white, 34, TextAlignmentOptions.Center);
            _subtitle = AddLabel(_contentRt, "Autenticación contra servidor B", 15, FontStyles.Normal, new Color(1, 1, 1, 0.72f), 24, TextAlignmentOptions.Center);

            _baseUrl = AddInput(_contentRt, "Servidor", EmpathiaAuthState.BaseUrl);
            _user = AddInput(_contentRt, "Usuario", "estudiante1");
            _pass = AddInput(_contentRt, "Contraseña", "password");
            _pass.contentType = TMP_InputField.ContentType.Password;

            _loginBtn = AddButton(_contentRt, "Iniciar sesión", new Color(0.16f, 0.66f, 0.46f), 48, OnLogin);
            _state = AddLabel(_contentRt, "Estado UI: idle", 15, FontStyles.Bold, Color.white, 24, TextAlignmentOptions.Left);
            _status = AddLabel(_contentRt, "", 14, FontStyles.Normal, new Color(0.92f, 0.94f, 0.96f), 78, TextAlignmentOptions.TopLeft);

            AddLabel(_contentRt, "── Después del login ──", 13, FontStyles.Italic, new Color(1, 1, 1, 0.45f), 22, TextAlignmentOptions.Center);

            _row1 = AddRow(_contentRt);
            _row1Layout = _row1.GetComponent<HorizontalLayoutGroup>();
            _sessionBtn = AddButton(_row1, "Crear sesión", new Color(0.22f, 0.5f, 0.8f), 40, OnCreateSession);
            _closeBtn = AddButton(_row1, "Cerrar sesión", new Color(0.22f, 0.5f, 0.8f), 40, OnCloseSession);

            _row2 = AddRow(_contentRt);
            _row2Layout = _row2.GetComponent<HorizontalLayoutGroup>();
            _turnWavBtn = AddButton(_row2, "Turno WAV", new Color(0.22f, 0.5f, 0.8f), 40, () => StartCoroutine(RunTurn(false)));
            _turnMicBtn = AddButton(_row2, "Turno mic 3s", new Color(0.22f, 0.5f, 0.8f), 40, () => StartCoroutine(RunTurn(true)));

            _reply = AddLabel(_contentRt, "Respuesta: (sin respuesta)", 14, FontStyles.Normal, new Color(0.75f, 0.95f, 0.82f), 48, TextAlignmentOptions.TopLeft);
        }

        void ApplyResponsiveLayout()
        {
            _lastScreen = new Vector2(Screen.width, Screen.height);
            if (_scaler == null || _cardRt == null)
                return;

            var aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
            _scaler.matchWidthOrHeight = aspect >= 1.15f ? 0.25f : 0.75f;

            var scale = Mathf.Lerp(
                Screen.width / _scaler.referenceResolution.x,
                Screen.height / _scaler.referenceResolution.y,
                _scaler.matchWidthOrHeight);
            var canvasW = Screen.width / Mathf.Max(0.01f, scale);
            var canvasH = Screen.height / Mathf.Max(0.01f, scale);

            _narrow = canvasW < NarrowBreakpoint;

            var cardW = Mathf.Clamp(canvasW * (_narrow ? 0.96f : 0.88f), 340f, 640f);
            var cardH = Mathf.Clamp(canvasH * 0.94f, 520f, canvasH * 0.96f);
            _cardRt.sizeDelta = new Vector2(cardW, cardH);

            if (_contentLayout != null)
            {
                var pad = _narrow ? 14 : 22;
                _contentLayout.padding = new RectOffset(pad, pad, 12, 18);
                _contentLayout.spacing = _narrow ? 8 : 10;
            }

            ConfigureRow(_row1Layout, _row1, _narrow);
            ConfigureRow(_row2Layout, _row2, _narrow);

            SetFontSize(_brand, _narrow ? 28 : 36);
            SetFontSize(_title, _narrow ? 20 : 26);
            SetFontSize(_subtitle, _narrow ? 13 : 15);

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
                    le.preferredHeight = 92;
                    le.minHeight = 92;
                }
            }
            else
            {
                if (vertical != null)
                    vertical.enabled = false;
                h.enabled = true;
                if (le != null)
                {
                    le.preferredHeight = 42;
                    le.minHeight = 42;
                }
            }
        }

        static void SetFontSize(TextMeshProUGUI t, float size)
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

        TextMeshProUGUI AddLabel(Transform parent, string text, float size, FontStyles style, Color color, float height, TextAlignmentOptions align)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.font = GetTmpFont();
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            // Mejor nitidez en pantallas HiDPI
            t.enableAutoSizing = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleWidth = 1f;
            return t;
        }

        TMP_InputField AddInput(Transform parent, string title, string value)
        {
            var wrap = new GameObject(title + "Wrap", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            wrap.transform.SetParent(parent, false);
            var v = wrap.GetComponent<VerticalLayoutGroup>();
            v.spacing = 4;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            var wrapLe = wrap.GetComponent<LayoutElement>();
            wrapLe.preferredHeight = 64;
            wrapLe.minHeight = 64;
            wrapLe.flexibleWidth = 1f;

            AddLabel(wrap.transform, title, 13, FontStyles.Normal, new Color(1, 1, 1, 0.78f), 18, TextAlignmentOptions.Left);

            var fieldGo = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            fieldGo.transform.SetParent(wrap.transform, false);
            var fieldImg = fieldGo.GetComponent<Image>();
            fieldImg.color = new Color(1f, 1f, 1f, 0.12f);
            ApplyRounded(fieldImg, large: false);
            fieldGo.GetComponent<LayoutElement>().preferredHeight = 38;
            fieldGo.GetComponent<LayoutElement>().minHeight = 38;

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(fieldGo.transform, false);
            var areaRt = textArea.GetComponent<RectTransform>();
            StretchFull(areaRt);
            areaRt.offsetMin = new Vector2(14, 6);
            areaRt.offsetMax = new Vector2(-14, -6);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textArea.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = GetTmpFont();
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = false;
            StretchFull(text.rectTransform);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            var ph = phGo.GetComponent<TextMeshProUGUI>();
            ph.font = GetTmpFont();
            ph.fontSize = 16;
            ph.fontStyle = FontStyles.Italic;
            ph.color = new Color(1, 1, 1, 0.35f);
            ph.text = title;
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            StretchFull(ph.rectTransform);

            var input = fieldGo.GetComponent<TMP_InputField>();
            input.textViewport = areaRt;
            input.textComponent = text;
            input.placeholder = ph;
            input.fontAsset = GetTmpFont();
            input.pointSize = 16;
            input.text = value ?? "";
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.3f, 0.6f, 1f, 0.35f);
            return input;
        }

        Transform AddRow(Transform parent)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            h.childControlWidth = true;
            h.childControlHeight = true;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 42;
            le.minHeight = 42;
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

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<TextMeshProUGUI>();
            t.text = label;
            t.font = GetTmpFont();
            t.fontSize = 16;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.enableAutoSizing = true;
            t.fontSizeMin = 11;
            t.fontSizeMax = 16;
            t.raycastTarget = false;
            StretchFull(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(10, 4);
            t.rectTransform.offsetMax = new Vector2(-10, -4);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
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
