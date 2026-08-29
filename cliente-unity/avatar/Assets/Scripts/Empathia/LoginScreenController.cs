using System;
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
    /// Login EmpathIA: UI estilo mockup 1920×1080 (fondo + tarjeta blanca).
    /// </summary>
    public class LoginScreenController : MonoBehaviour
    {
        const int MaxMicSeconds = 12;
        const int MicSampleRate = 16000;
        const float RefW = 1920f;
        const float RefH = 1080f;

        static readonly Color Navy = new Color(0.12f, 0.14f, 0.22f, 1f);
        static readonly Color Muted = new Color(0.42f, 0.45f, 0.55f, 1f);
        static readonly Color FieldBg = new Color(0.96f, 0.96f, 0.98f, 1f);
        static readonly Color FieldBorder = new Color(0.86f, 0.87f, 0.91f, 1f);
        static readonly Color Purple = new Color(0.55f, 0.28f, 0.95f, 1f);
        static readonly Color Blue = new Color(0.18f, 0.55f, 0.98f, 1f);
        static readonly Color CardGlass = new Color(1f, 1f, 1f, 0.78f);

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
        EmpathiaLocalStt _localStt;
        TMP_FontAsset _tmpFont;
        Sprite _roundLg;
        Sprite _roundSm;
        Sprite _roundPill;
        Sprite _gradBtn;

        RectTransform _rootRt;
        RectTransform _cardRt;
        RectTransform _labRt;
        RectTransform _confirmRt;
        RectTransform _healthRt;
        CanvasScaler _scaler;

        GameObject _loginView;
        GameObject _confirmView;
        GameObject _healthView;
        GameObject _cardShadowGo;

        TMP_InputField _baseUrl;
        TMP_InputField _user;
        TMP_InputField _pass;
        TMP_InputField _typedMessage;
        TextMeshProUGUI _status;
        TextMeshProUGUI _state;
        TextMeshProUGUI _reply;
        TextMeshProUGUI _transcript;
        TextMeshProUGUI _loginStatus;
        TextMeshProUGUI _welcomeTitle;
        TextMeshProUGUI _welcomeSub;
        Button _loginBtn;
        Button _registerBtn;
        Button _checkBBtn;
        Button _confirmBtn;
        Button _recordBtn;
        TextMeshProUGUI _recordBtnLabel;
        Button _sendTextBtn;
        Button _eyeBtn;
        TextMeshProUGUI _recordHint;
        bool _showPass;
        bool _busy;
        bool _recording;
        bool _stopRecording;
        string _micDevice;
        AudioClip _micClip;
        bool _built;
        Vector2 _lastScreen;
        enum UiScreen { Login, Confirm, Health }
        UiScreen _screen = UiScreen.Login;

        void Awake()
        {
            try
            {
                _api = GetComponent<EmpathiaApiClient>() ?? gameObject.AddComponent<EmpathiaApiClient>();
                _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                _localStt = GetComponent<EmpathiaLocalStt>() ?? gameObject.AddComponent<EmpathiaLocalStt>();
                EnsureEventSystem();
                ApplyDisplayQuality();
                BuildUi();
                ApplyLayout();
                SetLoginStatus("Comprobando conexión con B…");
                StartCoroutine(CheckConnectionToB(silent: false));
                Debug.Log("[Empathia] UI 1920x1080 @60 · Login → Salud. Game view 1920x1080 + Play.");
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
                ApplyLayout();
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

        void ApplyDisplayQuality()
        {
            // Resolución PC HD y objetivo 60 FPS (1080p60)
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
        }

        TMP_FontAsset GetTmpFont()
        {
            if (_tmpFont != null)
                return _tmpFont;
            _tmpFont = TMP_Settings.defaultFontAsset;
            if (_tmpFont == null)
                _tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return _tmpFont;
        }

        Sprite RoundSprite(int size, int radius)
        {
            if (size >= 240 && radius >= 40)
            {
                if (_roundLg == null) _roundLg = BuildRoundedSprite(size, radius);
                return _roundLg;
            }
            if (radius >= 40)
            {
                if (_roundPill == null) _roundPill = BuildRoundedSprite(128, 48);
                return _roundPill;
            }
            if (_roundSm == null) _roundSm = BuildRoundedSprite(128, 28);
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
                    float dx = 0f, dy = 0f;
                    if (x < r && y < r) { dx = r - x - 0.5f; dy = r - y - 0.5f; }
                    else if (x > max - r && y < r) { dx = x - (max - r) - 0.5f; dy = r - y - 0.5f; }
                    else if (x < r && y > max - r) { dx = r - x - 0.5f; dy = y - (max - r) - 0.5f; }
                    else if (x > max - r && y > max - r) { dx = x - (max - r) - 0.5f; dy = y - (max - r) - 0.5f; }

                    byte a = 255;
                    if (dx != 0f || dy != 0f)
                    {
                        var dist = Mathf.Sqrt(dx * dx + dy * dy);
                        const float aa = 1.5f;
                        if (dist >= r + aa) a = 0;
                        else if (dist > r - aa)
                            a = (byte)Mathf.Clamp(Mathf.RoundToInt(((r + aa) - dist) / (aa * 2f) * 255f), 0, 255);
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        Sprite GradientButtonSprite()
        {
            if (_gradBtn != null) return _gradBtn;
            const int w = 256, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[w * h];
            float rr = h * 0.5f;
            float cy = h * 0.5f;
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var t = x / (w - 1f);
                    var c = Color.Lerp(Purple, Blue, t);
                    float dist;
                    if (x >= rr && x <= w - 1 - rr)
                        dist = Mathf.Abs(y - cy);
                    else
                        dist = Vector2.Distance(new Vector2(x, y), new Vector2(x < rr ? rr : w - 1 - rr, cy));

                    byte a = 255;
                    const float aa = 1.2f;
                    if (dist >= rr + aa) a = 0;
                    else if (dist > rr - aa)
                        a = (byte)Mathf.Clamp(Mathf.RoundToInt(((rr + aa) - dist) / (aa * 2f) * 255f), 0, 255);

                    pixels[y * w + x] = new Color32(
                        (byte)Mathf.RoundToInt(c.r * 255f),
                        (byte)Mathf.RoundToInt(c.g * 255f),
                        (byte)Mathf.RoundToInt(c.b * 255f), a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            _gradBtn = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(rr, rr, rr, rr));
            return _gradBtn;
        }

        static Sprite BuildIconSprite(string kind)
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var clear = new Color32(0, 0, 0, 0);
            var ink = new Color32(120, 125, 145, 255);
            var pixels = new Color32[s * s];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = clear;

            void Disc(float cx, float cy, float r, Color32 col)
            {
                for (var y = 0; y < s; y++)
                for (var x = 0; x < s; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d <= r) pixels[y * s + x] = col;
                    else if (d < r + 1.2f)
                    {
                        var a = (byte)((r + 1.2f - d) / 1.2f * col.a);
                        if (a > pixels[y * s + x].a) pixels[y * s + x] = new Color32(col.r, col.g, col.b, a);
                    }
                }
            }

            void Rect(int x0, int y0, int x1, int y1, Color32 col)
            {
                for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    if (x >= 0 && x < s && y >= 0 && y < s)
                        pixels[y * s + x] = col;
            }

            if (kind == "user")
            {
                Disc(32, 42, 10, ink);
                Disc(32, 18, 14, ink);
                Rect(18, 0, 46, 12, clear);
            }
            else if (kind == "lock")
            {
                Rect(20, 8, 44, 32, ink);
                for (var y = 32; y < 50; y++)
                for (var x = 22; x < 42; x++)
                {
                    var d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(32, 32)) - 10f);
                    if (d < 2.2f) pixels[y * s + x] = ink;
                }
                Disc(32, 20, 3, clear);
            }
            else // eye
            {
                for (var y = 0; y < s; y++)
                for (var x = 0; x < s; x++)
                {
                    var nx = (x - 32) / 22f;
                    var ny = (y - 32) / 12f;
                    if (nx * nx + ny * ny <= 1f) pixels[y * s + x] = ink;
                }
                Disc(32, 32, 6, clear);
                Disc(32, 32, 3.5f, ink);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        void ApplyRounded(Image img, Sprite sprite, float ppu = 1.2f)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppu;
        }

        void BuildUi()
        {
            if (_built) return;
            _built = true;

            var canvasGo = new GameObject("EmpathiaLoginCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.Normal |
                AdditionalCanvasShaderChannels.Tangent;

            _scaler = canvasGo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(RefW, RefH);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _rootRt = canvasGo.GetComponent<RectTransform>();

            // Fondo mockup 16:9 (cover)
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            StretchFull(bgRt);
            var raw = bgGo.GetComponent<RawImage>();
            var tex = Resources.Load<Texture2D>("Empathia/LoginBackground");
            if (tex != null)
            {
                raw.texture = tex;
                raw.color = Color.white;
                var ar = bgGo.GetComponent<AspectRatioFitter>();
                ar.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                ar.aspectRatio = tex.width / (float)Mathf.Max(1, tex.height);
            }
            else
            {
                raw.texture = BuildFallbackGradient(64, 36);
                raw.color = Color.white;
                bgGo.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.None;
            }

            BuildLoginView(canvasGo.transform);
            BuildConfirmView(canvasGo.transform);
            BuildHealthView(canvasGo.transform);
            ShowScreen(UiScreen.Login);
        }

        void BuildLoginView(Transform canvas)
        {
            _loginView = new GameObject("LoginView", typeof(RectTransform));
            _loginView.transform.SetParent(canvas, false);
            StretchFull(_loginView.GetComponent<RectTransform>());

            var shadow = CreateImage(_loginView.transform, "CardShadow", new Color(0.25f, 0.2f, 0.45f, 0.18f));
            ApplyRounded(shadow, RoundSprite(256, 48), 1.0f);
            _cardShadowGo = shadow.gameObject;
            var shadowRt = shadow.rectTransform;
            shadowRt.anchorMin = shadowRt.anchorMax = shadowRt.pivot = new Vector2(0.5f, 0.42f);
            shadowRt.sizeDelta = new Vector2(510, 470);
            shadowRt.anchoredPosition = new Vector2(0, -6);

            var card = CreateImage(_loginView.transform, "Card", CardGlass);
            ApplyRounded(card, RoundSprite(256, 48), 1.05f);
            _cardRt = card.rectTransform;
            _cardRt.anchorMin = _cardRt.anchorMax = _cardRt.pivot = new Vector2(0.5f, 0.42f);
            _cardRt.sizeDelta = new Vector2(500, 480);
            _cardRt.anchoredPosition = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(_cardRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            StretchFull(contentRt);
            contentRt.offsetMin = new Vector2(36, 24);
            contentRt.offsetMax = new Vector2(-36, -24);
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.spacing = 12;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.padding = new RectOffset(0, 0, 4, 0);

            _baseUrl = AddCompactInput(content.transform, "Servidor", EmpathiaAuthState.BaseUrl);
            _user = AddIconInput(content.transform, "Usuario o correo electrónico", "estudiante1", "user", false);
            _pass = AddIconInput(content.transform, "Contraseña", "password", "lock", true);

            _checkBBtn = AddOutlineButton(content.transform, "Probar conexión B", 52, OnCheckConnectionB);
            _loginBtn = AddGradientButton(content.transform, "Iniciar sesión", 64, OnLogin);
            _registerBtn = AddOutlineButton(content.transform, "Registrarse", 58, () =>
            {
                SetLoginStatus("Registro: próximamente (usa estudiante1 / password).");
            });

            AddLabel(content.transform, "Tu bienestar emocional importa.", 14, FontStyles.Normal, Muted, 20, TextAlignmentOptions.Center);
            _loginStatus = AddLabel(content.transform, "", 12, FontStyles.Normal, new Color(0.75f, 0.25f, 0.35f), 36, TextAlignmentOptions.Center);
        }

        void OnCheckConnectionB()
        {
            if (_busy) return;
            EmpathiaAuthState.BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.text)
                ? "http://192.168.1.69:8000/api/v1"
                : _baseUrl.text.Trim();
            if (EmpathiaAuthState.BaseUrl.IndexOf("0.0.0.0", StringComparison.Ordinal) >= 0)
            {
                EmpathiaAuthState.BaseUrl = "http://192.168.1.69:8000/api/v1";
                if (_baseUrl != null) _baseUrl.text = EmpathiaAuthState.BaseUrl;
                SetLoginStatus("0.0.0.0 no es URL de cliente. Usando http://192.168.1.69:8000/api/v1");
            }
            StartCoroutine(CheckConnectionToB(silent: false));
        }

        IEnumerator CheckConnectionToB(bool silent)
        {
            SetBusy(true);
            if (!silent)
                SetLoginStatus("Comprobando B en " + EmpathiaAuthState.BaseUrl + " …");

            var ok = false;
            var msg = "";
            yield return _api.CheckHealth((success, message) =>
            {
                ok = success;
                msg = message;
            });

            SetBusy(false);
            SetLoginStatus(msg);
            if (ok)
                Debug.Log("[Empathia] " + msg);
            else
                Debug.LogWarning("[Empathia] " + msg);
        }

        void BuildConfirmView(Transform canvas)
        {
            _confirmView = new GameObject("ConfirmView", typeof(RectTransform));
            _confirmView.transform.SetParent(canvas, false);
            StretchFull(_confirmView.GetComponent<RectTransform>());

            var dim = CreateImage(_confirmView.transform, "Dim", new Color(0.1f, 0.08f, 0.18f, 0.35f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            var card = CreateImage(_confirmView.transform, "ConfirmCard", CardGlass);
            ApplyRounded(card, RoundSprite(256, 48), 1.05f);
            _confirmRt = card.rectTransform;
            _confirmRt.anchorMin = _confirmRt.anchorMax = _confirmRt.pivot = new Vector2(0.5f, 0.5f);
            _confirmRt.sizeDelta = new Vector2(520, 360);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(_confirmRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            StretchFull(contentRt);
            contentRt.offsetMin = new Vector2(36, 28);
            contentRt.offsetMax = new Vector2(-36, -28);
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.spacing = 12;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            AddLabel(content.transform, "Salud", 14, FontStyles.Bold, Purple, 20, TextAlignmentOptions.Center);
            AddLabel(content.transform, "Inicio de sesión confirmado", 26, FontStyles.Bold, Navy, 36, TextAlignmentOptions.Center);
            AddLabel(content.transform, "Tu cuenta está lista. Continúa para entrar a tu espacio de bienestar.", 15, FontStyles.Normal, Muted, 48, TextAlignmentOptions.Center);
            _confirmBtn = AddGradientButton(content.transform, "Entrar a Salud", 64, OnConfirmEnterHealth);
            AddOutlineButton(content.transform, "Volver", 56, () => ShowScreen(UiScreen.Login));
        }

        void BuildHealthView(Transform canvas)
        {
            _healthView = new GameObject("HealthView", typeof(RectTransform));
            _healthView.transform.SetParent(canvas, false);
            StretchFull(_healthView.GetComponent<RectTransform>());

            var card = CreateImage(_healthView.transform, "HealthCard", CardGlass);
            ApplyRounded(card, RoundSprite(256, 48), 1.05f);
            _healthRt = card.rectTransform;
            _healthRt.anchorMin = _healthRt.anchorMax = _healthRt.pivot = new Vector2(0.5f, 0.55f);
            _healthRt.sizeDelta = new Vector2(780, 480);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(_healthRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            StretchFull(contentRt);
            contentRt.offsetMin = new Vector2(40, 28);
            contentRt.offsetMax = new Vector2(-40, -28);
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            AddLabel(content.transform, "Pestaña Salud", 13, FontStyles.Bold, Purple, 18, TextAlignmentOptions.Center);
            _welcomeTitle = AddLabel(content.transform, "¡Bienvenido!", 34, FontStyles.Bold, Navy, 44, TextAlignmentOptions.Center);
            _welcomeSub = AddLabel(content.transform, "Este es tu espacio de acompañamiento emocional.", 16, FontStyles.Normal, Muted, 28, TextAlignmentOptions.Center);
            _recordHint = AddLabel(content.transform, "Grabar = audio → B (sin esperar respuesta)", 14, FontStyles.Normal, Muted, 24, TextAlignmentOptions.Center);

            _recordBtn = AddGradientButton(content.transform, "Grabar audio", 72, OnRecordPressed, 22f);
            var textTf = _recordBtn.transform.Find("Text");
            _recordBtnLabel = textTf != null
                ? textTf.GetComponent<TextMeshProUGUI>()
                : _recordBtn.GetComponentInChildren<TextMeshProUGUI>();

            _typedMessage = AddCompactInput(content.transform, "O escribe un mensaje a B", "");
            _sendTextBtn = AddOutlineButton(content.transform, "Enviar texto a B", 48, OnSendTypedText);

            _state = AddLabel(content.transform, "Estado UI: idle", 13, FontStyles.Bold, Navy, 20, TextAlignmentOptions.Center);
            _status = AddLabel(content.transform, "", 13, FontStyles.Normal, Muted, 36, TextAlignmentOptions.Center);
            _transcript = AddLabel(content.transform, "Tu texto: (aún no hay)", 13, FontStyles.Normal, Navy, 40, TextAlignmentOptions.Center);
            _reply = AddLabel(content.transform, "Respuesta EmpathIA: (sin respuesta)", 13, FontStyles.Normal, new Color(0.2f, 0.55f, 0.4f), 40, TextAlignmentOptions.Center);

            _labRt = _healthRt;
        }

        void OnRecordPressed()
        {
            if (_recording)
            {
                _stopRecording = true;
                if (_recordBtnLabel != null)
                {
                    _recordBtnLabel.text = "Deteniendo…";
                    _recordBtnLabel.ForceMeshUpdate();
                }
                return;
            }

            if (_busy)
                return;

            // Cambia el texto al instante (antes de sesión/mic)
            SetRecordButtonUi(true);
            StartCoroutine(RecordAudioTurn());
        }

        void SetRecordButtonUi(bool recording)
        {
            if (_recordBtnLabel == null && _recordBtn != null)
            {
                var textTf = _recordBtn.transform.Find("Text");
                _recordBtnLabel = textTf != null
                    ? textTf.GetComponent<TextMeshProUGUI>()
                    : _recordBtn.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (_recordBtnLabel != null)
            {
                _recordBtnLabel.text = recording ? "Detener audio" : "Grabar audio";
                _recordBtnLabel.ForceMeshUpdate();
            }

            if (_recordHint != null)
                _recordHint.text = recording
                    ? "Grabando… Detener envía a B"
                    : "Grabar = solo envía audio a B. Texto escrito = /active/text";
        }

        void ShowScreen(UiScreen screen)
        {
            _screen = screen;
            if (_loginView != null) _loginView.SetActive(screen == UiScreen.Login);
            if (_confirmView != null) _confirmView.SetActive(screen == UiScreen.Confirm);
            if (_healthView != null) _healthView.SetActive(screen == UiScreen.Health);
        }

        void OnConfirmEnterHealth()
        {
            var name = string.IsNullOrWhiteSpace(EmpathiaAuthState.Username)
                ? (_user != null ? _user.text.Trim() : "usuario")
                : EmpathiaAuthState.Username;
            if (_welcomeTitle != null)
                _welcomeTitle.text = "¡Bienvenido, " + name + "!";
            if (_welcomeSub != null)
                _welcomeSub.text = "Grabar envía audio a B. No esperamos respuesta por ahora.";
            SetTranscript("(aún no hay)");
            SetReply("(sin respuesta)");
            SetStatus("Listo. Graba o escribe un mensaje para B.");
            SetState("idle");
            ShowScreen(UiScreen.Health);
            StartCoroutine(EnsureSessionThenReady());
        }

        IEnumerator EnsureSessionThenReady()
        {
            if (EmpathiaAuthState.HasSession)
                yield break;

            var ok = false;
            var msg = "";
            yield return _api.CreateSession((success, message) =>
            {
                ok = success;
                msg = message;
            });

            if (ok)
            {
                Debug.Log("[Empathia] Sesión B lista: " + EmpathiaAuthState.SessionId);
                SetStatus("Sesión B lista. Graba o escribe texto a B.");
                yield break;
            }

            Debug.LogWarning("[Empathia] Aún sin sesión B (se reintenta al enviar): " + msg);
            SetStatus("Listo. Al enviar se crea sesión y POST /active/text.");
        }

        void ApplyLayout()
        {
            _lastScreen = new Vector2(Screen.width, Screen.height);
            if (_scaler == null) return;

            var aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
            _scaler.matchWidthOrHeight = Mathf.Abs(aspect - (RefW / RefH)) < 0.08f ? 0.5f : (aspect >= 1.4f ? 0.5f : 0.7f);

            if (_cardRt != null)
            {
                _cardRt.anchorMin = _cardRt.anchorMax = new Vector2(0.5f, 0.42f);
                _cardRt.sizeDelta = new Vector2(500, 480);
            }
            if (_confirmRt != null)
                _confirmRt.sizeDelta = new Vector2(520, 360);
            if (_healthRt != null)
                _healthRt.sizeDelta = new Vector2(780, 500);
        }

        Image CreateImage(Transform parent, string name, Color color)
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

        static Texture2D BuildFallbackGradient(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var u = x / (w - 1f);
                var v = y / (h - 1f);
                var c1 = Color.Lerp(new Color(1f, 0.72f, 0.45f), new Color(0.55f, 0.85f, 0.95f), u);
                var c2 = Color.Lerp(new Color(0.85f, 0.45f, 0.85f), new Color(0.45f, 0.55f, 0.95f), u);
                tex.SetPixel(x, y, Color.Lerp(c2, c1, v));
            }
            tex.Apply(false, true);
            return tex;
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
            t.raycastTarget = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleWidth = 1f;
            return t;
        }

        TMP_InputField AddIconInput(Transform parent, string placeholder, string value, string iconKind, bool password)
        {
            var fieldGo = new GameObject(placeholder, typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            fieldGo.transform.SetParent(parent, false);
            var fieldImg = fieldGo.GetComponent<Image>();
            fieldImg.color = FieldBg;
            ApplyRounded(fieldImg, RoundSprite(128, 28), 1.35f);
            var outline = fieldGo.AddComponent<Outline>();
            outline.effectColor = FieldBorder;
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var le = fieldGo.GetComponent<LayoutElement>();
            le.preferredHeight = 54;
            le.minHeight = 54;
            le.flexibleWidth = 1f;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(fieldGo.transform, false);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = BuildIconSprite(iconKind);
            icon.color = Muted;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(22, 22);
            iconRt.anchoredPosition = new Vector2(28, 0);

            float rightPad = password ? 48f : 14f;

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(fieldGo.transform, false);
            var areaRt = textArea.GetComponent<RectTransform>();
            StretchFull(areaRt);
            areaRt.offsetMin = new Vector2(48, 10);
            areaRt.offsetMax = new Vector2(-rightPad, -10);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textArea.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = GetTmpFont();
            text.fontSize = 16;
            text.color = Navy;
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
            ph.fontStyle = FontStyles.Normal;
            ph.color = new Color(Muted.r, Muted.g, Muted.b, 0.85f);
            ph.text = placeholder;
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            StretchFull(ph.rectTransform);

            var input = fieldGo.GetComponent<TMP_InputField>();
            input.textViewport = areaRt;
            input.textComponent = text;
            input.placeholder = ph;
            input.fontAsset = GetTmpFont();
            input.pointSize = 16;
            input.text = value ?? "";
            input.caretColor = Purple;
            input.selectionColor = new Color(Purple.r, Purple.g, Purple.b, 0.25f);
            if (password)
            {
                input.contentType = TMP_InputField.ContentType.Password;
                _eyeBtn = AddEyeToggle(fieldGo.transform, input);
            }
            return input;
        }

        Button AddEyeToggle(Transform parent, TMP_InputField input)
        {
            var go = new GameObject("Eye", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0.01f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(44, 0);
            rt.anchoredPosition = new Vector2(-4, 0);

            var eyeImgGo = new GameObject("EyeIcon", typeof(RectTransform), typeof(Image));
            eyeImgGo.transform.SetParent(go.transform, false);
            var eyeImg = eyeImgGo.GetComponent<Image>();
            eyeImg.sprite = BuildIconSprite("eye");
            eyeImg.color = Muted;
            eyeImg.preserveAspect = true;
            eyeImg.raycastTarget = false;
            var eyeRt = eyeImg.rectTransform;
            eyeRt.anchorMin = eyeRt.anchorMax = new Vector2(0.5f, 0.5f);
            eyeRt.sizeDelta = new Vector2(22, 22);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                _showPass = !_showPass;
                input.contentType = _showPass
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;
                input.ForceLabelUpdate();
                eyeImg.color = _showPass ? Purple : Muted;
            });
            return btn;
        }

        TMP_InputField AddCompactInput(Transform parent, string placeholder, string value)
        {
            var fieldGo = new GameObject(placeholder, typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            fieldGo.transform.SetParent(parent, false);
            var fieldImg = fieldGo.GetComponent<Image>();
            fieldImg.color = FieldBg;
            ApplyRounded(fieldImg, RoundSprite(128, 28), 1.4f);
            fieldGo.GetComponent<LayoutElement>().preferredHeight = 36;
            fieldGo.GetComponent<LayoutElement>().minHeight = 36;

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(fieldGo.transform, false);
            var areaRt = textArea.GetComponent<RectTransform>();
            StretchFull(areaRt);
            areaRt.offsetMin = new Vector2(12, 6);
            areaRt.offsetMax = new Vector2(-12, -6);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textArea.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = GetTmpFont();
            text.fontSize = 13;
            text.color = Navy;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false;
            StretchFull(text.rectTransform);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            var ph = phGo.GetComponent<TextMeshProUGUI>();
            ph.font = GetTmpFont();
            ph.fontSize = 13;
            ph.color = Muted;
            ph.text = placeholder;
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            StretchFull(ph.rectTransform);

            var input = fieldGo.GetComponent<TMP_InputField>();
            input.textViewport = areaRt;
            input.textComponent = text;
            input.placeholder = ph;
            input.fontAsset = GetTmpFont();
            input.pointSize = 13;
            input.text = value ?? "";
            return input;
        }

        Transform AddRow(Transform parent, float height)
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
            le.preferredHeight = height;
            le.minHeight = height;
            return go.transform;
        }

        Button AddGradientButton(Transform parent, string label, float height, UnityEngine.Events.UnityAction onClick, float fontSize = 20f)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = GradientButtonSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.pixelsPerUnitMultiplier = 1.05f;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleWidth = 1f;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<TextMeshProUGUI>();
            t.text = label;
            t.font = GetTmpFont();
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
            StretchFull(t.rectTransform);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.95f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.95f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            return btn;
        }

        Button AddOutlineButton(Transform parent, string label, float height, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            ApplyRounded(img, RoundSprite(128, 48), 1.15f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.62f, 0.45f, 0.95f, 1f);
            outline.effectDistance = new Vector2(1.6f, -1.6f);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<TextMeshProUGUI>();
            t.text = label;
            t.font = GetTmpFont();
            t.fontSize = 17;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Purple;
            t.raycastTarget = false;
            StretchFull(t.rectTransform);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            return btn;
        }

        Button AddSmallButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            ApplyRounded(img, RoundSprite(128, 28), 1.3f);
            go.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<TextMeshProUGUI>();
            t.text = label;
            t.font = GetTmpFont();
            t.fontSize = 13;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.enableAutoSizing = true;
            t.fontSizeMin = 10;
            t.fontSizeMax = 13;
            t.raycastTarget = false;
            StretchFull(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(4, 2);
            t.rectTransform.offsetMax = new Vector2(-4, -2);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            return btn;
        }

        void OnLogin()
        {
            if (_busy) return;
            EmpathiaAuthState.BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.text)
                ? "http://192.168.1.69:8000/api/v1"
                : _baseUrl.text.Trim();
            // 0.0.0.0 es solo bind de artisan; el cliente debe usar la IP LAN de B.
            if (EmpathiaAuthState.BaseUrl.IndexOf("0.0.0.0", StringComparison.Ordinal) >= 0)
            {
                EmpathiaAuthState.BaseUrl = "http://192.168.1.69:8000/api/v1";
                if (_baseUrl != null) _baseUrl.text = EmpathiaAuthState.BaseUrl;
                SetLoginStatus("0.0.0.0 no sirve como URL de cliente. Usando http://192.168.1.69:8000/api/v1");
                return;
            }

            SetBusy(true);
            SetLoginStatus("Autenticando contra B…");
            StartCoroutine(_api.Login(_user.text.Trim(), _pass.text, (ok, msg) =>
            {
                SetBusy(false);
                if (ok)
                {
                    SetLoginStatus("Login OK. Confirma para continuar.");
                    ShowScreen(UiScreen.Confirm);
                    Debug.Log("[Empathia] " + msg);
                }
                else
                {
                    SetLoginStatus("Error: " + msg);
                    Debug.Log("[Empathia] ERROR " + msg);
                }
            }));
        }

        IEnumerator RecordAudioTurn()
        {
            if (_busy || _recording) yield break;

            _stopRecording = false;
            SetRecordButtonUi(true);
            SetState("listening");
            SetTranscript("(grabando… habla y Detener)");
            SetReply("(sin respuesta)");
            SetStatus("Permiso de micrófono…");

            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                SetRecordButtonUi(false);
                SetBusy(false);
                SetState("idle");
                SetStatus("Sin permiso de mic. Usa Enviar texto a B.");
                yield break;
            }

            var devices = Microphone.devices;
            if (devices == null || devices.Length == 0)
            {
                SetRecordButtonUi(false);
                SetBusy(false);
                SetState("idle");
                SetStatus("No hay micrófono. Usa Enviar texto a B.");
                yield break;
            }

            var device = devices[0];
            Debug.Log("[Empathia] Grabando mic='" + device + "' → B /turns");

            AudioClip clip = null;
            try
            {
                if (Microphone.IsRecording(device))
                    Microphone.End(device);
                clip = Microphone.Start(device, false, MaxMicSeconds + 1, MicSampleRate);
            }
            catch (Exception ex)
            {
                SetRecordButtonUi(false);
                SetBusy(false);
                SetState("idle");
                SetStatus("No se pudo abrir el mic: " + ex.Message);
                yield break;
            }

            if (clip == null)
            {
                SetRecordButtonUi(false);
                SetBusy(false);
                SetState("idle");
                SetStatus("Microphone.Start falló.");
                yield break;
            }

            _recording = true;
            _busy = false;
            if (_recordBtn != null) _recordBtn.interactable = true;

            // Esperar buffer
            var warm = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - warm < 0.2f)
                yield return null;

            var started = Time.realtimeSinceStartup;
            var peak = 0f;
            while (!_stopRecording && (Time.realtimeSinceStartup - started) < MaxMicSeconds)
            {
                var pos = Microphone.GetPosition(device);
                if (pos > 256)
                {
                    var samples = new float[256];
                    var start = Mathf.Max(0, pos - samples.Length);
                    if (clip.GetData(samples, start))
                    {
                        for (var i = 0; i < samples.Length; i++)
                        {
                            var a = Mathf.Abs(samples[i]);
                            if (a > peak) peak = a;
                        }
                    }
                }

                var pct = Mathf.Clamp01(peak * 8f) * 100f;
                var t = Time.realtimeSinceStartup - started;
                SetTranscript("Grabando " + t.ToString("0.0") + " s · mic " + pct.ToString("0") + "%");
                SetStatus("Habla… Detener (máx " + MaxMicSeconds + " s)");
                yield return null;
            }

            var samplesRecorded = 0;
            try
            {
                samplesRecorded = Microphone.GetPosition(device);
            }
            catch
            {
                samplesRecorded = clip.samples;
            }

            try
            {
                if (Microphone.IsRecording(device))
                    Microphone.End(device);
            }
            catch
            {
                // ignore
            }

            _recording = false;
            _stopRecording = false;
            SetRecordButtonUi(false);
            SetBusy(true);

            if (samplesRecorded < MicSampleRate / 5 || peak < 0.004f)
            {
                SetBusy(false);
                SetState("idle");
                SetTranscript("(audio muy corto / silencioso)");
                SetStatus("No hubo audio útil. Habla más cerca o usa Enviar texto a B.");
                yield break;
            }

            var wav = EmpathiaWav.FromMicrophoneClip(clip, samplesRecorded, MicSampleRate);
            Debug.Log("[Empathia] WAV bytes=" + wav.Length + " peak=" + peak);

            if (!EmpathiaAuthState.HasSession)
            {
                SetStatus("Creando sesión en B…");
                var sessionOk = false;
                var sessionMsg = "";
                yield return _api.CreateSession((ok, msg) =>
                {
                    sessionOk = ok;
                    sessionMsg = msg;
                });
                if (!sessionOk || !EmpathiaAuthState.HasSession)
                {
                    SetBusy(false);
                    SetState("idle");
                    SetStatus("Sin sesión B: " + sessionMsg);
                    yield break;
                }
            }

            SetState("processing");
            SetTranscript("(enviando audio a B…)");
            SetReply("(no esperamos respuesta)");
            SetStatus("Subiendo audio a B…");

            var sendOk = false;
            var sendMsg = "";
            yield return _api.UploadTurnAudio(
                wav,
                status => SetStatus(status),
                (ok, msg) =>
                {
                    sendOk = ok;
                    sendMsg = msg;
                });

            if (!sendOk)
            {
                SetBusy(false);
                SetState("idle");
                SetStatus("No se envió a B: " + sendMsg);
                Debug.LogWarning("[Empathia] UploadTurnAudio: " + sendMsg);
                yield break;
            }

            SetTranscript("Audio enviado (" + wav.Length + " bytes)");
            SetReply("(mira la consola de B)");
            SetStatus("Listo. En consola de B: [A→B AUDIO→TEXTO] …");
            Debug.Log("[Empathia] " + sendMsg + " — el texto sale en artisan serve de B");
            SetBusy(false);
            SetState("idle");
        }

        IEnumerator EnsureSessionAndPostText(string message)
        {
            SetBusy(true);
            SetState("processing");

            if (!EmpathiaAuthState.HasSession)
            {
                SetStatus("Creando sesión en B…");
                var sessionOk = false;
                var sessionMsg = "";
                yield return _api.CreateSession((ok, msg) =>
                {
                    sessionOk = ok;
                    sessionMsg = msg;
                });
                if (!sessionOk)
                {
                    // Con el B nuevo, active/text puede resolver la sesión activa.
                    Debug.LogWarning("[Empathia] CreateSession: " + sessionMsg + " — pruebo alias active");
                }
                else
                {
                    Debug.Log("[Empathia] Sesión OK: " + EmpathiaAuthState.SessionId);
                }
            }

            SetStatus("POST .../sessions/active/text …");
            var sendOk = false;
            var sendMsg = "";
            SessionTextResponse parsed = null;
            yield return _api.SendActiveText(message, (ok, msg, response) =>
            {
                sendOk = ok;
                sendMsg = msg;
                parsed = response;
            });

            if (!sendOk)
            {
                SetBusy(false);
                SetState("idle");
                SetReply("(error)");
                SetStatus("B no recibió el texto: " + sendMsg);
                Debug.LogWarning("[Empathia] Falló POST /active/text: " + sendMsg);
                yield break;
            }

            var reply = parsed != null && !string.IsNullOrWhiteSpace(parsed.reply_text)
                ? parsed.reply_text.Trim()
                : (parsed != null && !string.IsNullOrWhiteSpace(parsed.received_text)
                    ? ("B recibió: " + parsed.received_text.Trim())
                    : "(B recibió el texto)");
            var transcript = parsed != null && !string.IsNullOrWhiteSpace(parsed.transcript)
                ? parsed.transcript.Trim()
                : message;

            SetTranscript(transcript);
            SetReply(reply);
            Debug.Log("[Empathia] B recibió texto. Respuesta: " + reply);
            Debug.Log("[Empathia] Raw B: " + sendMsg);
            SetStatus("Texto enviado a B (active/text). Mira artisan serve.");
            SetBusy(false);
            SetState("idle");
        }

        void OnSendTypedText()
        {
            if (_busy) return;
            var msg = _typedMessage != null ? _typedMessage.text.Trim() : "";
            if (string.IsNullOrWhiteSpace(msg))
            {
                SetStatus("Escribe un mensaje primero.");
                return;
            }
            StartCoroutine(EnsureSessionAndPostText(msg));
        }

        void SetBusy(bool busy)
        {
            _busy = busy;
            if (_loginBtn != null) _loginBtn.interactable = !busy;
            if (_registerBtn != null) _registerBtn.interactable = !busy;
            if (_checkBBtn != null) _checkBBtn.interactable = !busy;
            if (_confirmBtn != null) _confirmBtn.interactable = !busy;
            // Durante grabación el botón debe seguir activo para el 2.º toque
            if (_recordBtn != null)
                _recordBtn.interactable = _recording || !busy;
            if (_sendTextBtn != null)
                _sendTextBtn.interactable = !busy;
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

        void SetLoginStatus(string s)
        {
            if (_loginStatus != null)
                _loginStatus.text = s ?? "";
        }

        void SetReply(string s)
        {
            if (_reply != null)
                _reply.text = "Respuesta EmpathIA: " + s;
        }

        void SetTranscript(string s)
        {
            if (_transcript != null)
                _transcript.text = "Tu texto: " + s;
        }
    }
}
