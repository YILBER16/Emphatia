using System;
using System.Reflection;
using Convai.Domain.DomainEvents.Runtime;
using Convai.Domain.EventSystem;
using Convai.Modules.BodyAnimation.Components;
using Convai.Modules.BodyAnimation.Data;
using Convai.Modules.BodyLanguage.Components;
using Convai.Modules.Emotion.Components;
using Convai.Modules.Gaze.Components;
using Convai.Runtime.Components;
using Convai.Runtime.Embodiment;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Empathia
{
    /// <summary>
    /// Login EmpathIA: UI estilo mockup 1920×1080 (fondo + tarjeta blanca).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class LoginScreenController : MonoBehaviour
    {
        const int MaxMicSeconds = 60;
        const int MicSampleRate = 16000;
        const float RefW = 1920f;
        const float RefH = 1080f;
        const float LoginCardW = 680f;
        const float LoginCardTop = 0.72f;
        const float LoginCardMaxH = 560f;
        const float LoginFieldH = 76f;
        const float LoginDropItemH = 68f;

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
            go.AddComponent<EmpathiaMouthDriver>();
            go.AddComponent<LoginScreenController>();
        }

        EmpathiaApiClient _api;
        AudioSource _audio;
        EmpathiaMouthDriver _mouth;
        TMP_FontAsset _tmpFont;
        Sprite _roundLg;
        Sprite _roundSm;
        Sprite _roundPill;
        Sprite _gradBtn;
        bool _uiFromScene;
        Camera _framedCam;
        Vector3 _camHoldPos;
        Vector3 _camLook;
        bool _holdCam;

        [Header("UI en escena (Isaac puede moverla)")]
        [SerializeField] RectTransform _rootRt;
        [SerializeField] RectTransform _cardRt;
        [SerializeField] RectTransform _bgRt;
        [SerializeField] RectTransform _labRt;
        [SerializeField] RectTransform _confirmRt;
        [SerializeField] RectTransform _healthRt;
        [SerializeField] CanvasScaler _scaler;

        [SerializeField] GameObject _loginView;
        [SerializeField] GameObject _confirmView;
        [SerializeField] GameObject _healthView;
        [SerializeField] GameObject _reportView;
        [SerializeField] GameObject _pickStudentView;
        [SerializeField] GameObject _cardShadowGo;
        [SerializeField] Transform _studentListContent;
        [SerializeField] TextMeshProUGUI _pickStatus;

        [SerializeField] TMP_InputField _baseUrl;
        [SerializeField] TMP_InputField _user;
        [SerializeField] TMP_InputField _pass;
        [SerializeField] TMP_InputField _regName;
        [SerializeField] TMP_InputField _regDoc;
        [SerializeField] TMP_Dropdown _regCampus;
        [SerializeField] TMP_Dropdown _regGrade;
        [SerializeField] TMP_Dropdown _regShift;
        [SerializeField] GameObject _studentLoginPanel;
        [SerializeField] GameObject _registerPanel;
        [SerializeField] GameObject _adultLoginPanel;
        [SerializeField] Transform _loginListContent;
        [SerializeField] Button _refreshListBtn;
        StudentListItem[] _directoryStudents;
        [SerializeField] Button _createStudentBtn;
        [SerializeField] Button _backToLoginBtn;
        [SerializeField] TMP_InputField _typedMessage;
        [SerializeField] TextMeshProUGUI _status;
        [SerializeField] TextMeshProUGUI _state;
        [SerializeField] TextMeshProUGUI _reply;
        [SerializeField] TextMeshProUGUI _transcript;
        [SerializeField] TextMeshProUGUI _loginStatus;
        [SerializeField] TextMeshProUGUI _loginHint;
        [SerializeField] GameObject _alertModal;
        [SerializeField] TextMeshProUGUI _alertTitle;
        [SerializeField] TextMeshProUGUI _alertBody;
        [SerializeField] Button _alertCloseBtn;
        [SerializeField] GameObject _settingsView;
        [SerializeField] TextMeshProUGUI _settingsStatus;
        [SerializeField] TMP_Dropdown _micDropdown;
        [SerializeField] Button _settingsGearBtn;
        [SerializeField] Button _settingsSaveBtn;
        [SerializeField] Button _settingsCloseBtn;
        [SerializeField] TextMeshProUGUI _welcomeTitle;
        [SerializeField] TextMeshProUGUI _welcomeSub;
        [SerializeField] Button _loginBtn;
        [SerializeField] Button _registerBtn;
        [SerializeField] Button _checkBBtn;
        [SerializeField] Button _logoutBtn;
        [SerializeField] TextMeshProUGUI _reportTitle;
        [SerializeField] TextMeshProUGUI _reportMeta;
        [SerializeField] TextMeshProUGUI _reportBody;
        [SerializeField] Button _reportBackBtn;
        [SerializeField] Button _confirmBtn;
        [SerializeField] Button _confirmBackBtn;
        [SerializeField] Button _pickRefreshBtn;
        [SerializeField] Button _pickBackBtn;
        [SerializeField] Button _recordBtn;
        [SerializeField] TextMeshProUGUI _recordBtnLabel;
        [SerializeField] Button _sendTextBtn;
        [SerializeField] Button _eyeBtn;
        [SerializeField] TextMeshProUGUI _recordHint;
        bool _showPass;
        bool _busy;
        bool _recording;
        bool _stopRecording;
        string _micDevice;
        AudioClip _micClip;
        bool _built;
        Vector2 _lastScreen;
        Coroutine _fitCardCo;
        enum UiScreen { Login, Confirm, Health, PickStudent, Report }
        UiScreen _screen = UiScreen.Login;

        void Awake()
        {
            try
            {
                SilenceConvaiConnection();
                _api = GetComponent<EmpathiaApiClient>() ?? gameObject.AddComponent<EmpathiaApiClient>();
                _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
                _audio.loop = false;
                _audio.mute = false;
                _audio.volume = 1f;
                _audio.spatialBlend = 0f;
                if (FindAnyObjectByType<AudioListener>() == null)
                    gameObject.AddComponent<AudioListener>();
                _mouth = GetComponent<EmpathiaMouthDriver>() ?? gameObject.AddComponent<EmpathiaMouthDriver>();
                EnsureEventSystem();
                ApplyDisplayQuality();
                EmpathiaAuthState.RestoreSettings();
                _uiFromScene = _loginView != null;
                if (_uiFromScene)
                {
                    _built = true;
                    ApplyRuntimeSkin();
                    BindMouthFromScene();
                    EnsureReportView();
                    WireUi();
                    ShowScreen(UiScreen.Login);
                }
                else
                {
                    BuildUi();
                    ApplyLayout();
                }
                SetLoginStatus("Inicia sesión del psicoorientador.");
                StartCoroutine(CheckConnectionToB(silent: true));
                Debug.Log(_uiFromScene
                    ? "[Empathia] UI desde la escena Login. Mueve el Canvas en Hierarchy."
                    : "[Empathia] UI creada por código. Menú EmpathIA → Guardar UI en la escena Login.");
                Camera.onPreCull += LockSessionCamera;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Empathia] Error creando UI: " + ex);
            }
        }

        void OnDestroy()
        {
            Camera.onPreCull -= LockSessionCamera;
            if (_bodyGraph.IsValid())
                _bodyGraph.Destroy();
        }

        void LockSessionCamera(Camera cam)
        {
            if (!_holdCam || cam == null || cam != _framedCam)
                return;
            cam.transform.position = _camHoldPos;
            cam.transform.LookAt(_camLook);
            cam.fieldOfView = 18f;
            cam.rect = new Rect(0.56f, 0f, 0.44f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.86f, 0.9f, 0.98f, 1f);
            cam.usePhysicalProperties = false;
            SharpenSessionCamera(cam);
        }

        static void SharpenSessionCamera(Camera cam)
        {
            var extra = cam.GetComponent<UniversalAdditionalCameraData>();
            if (extra != null)
            {
                extra.renderPostProcessing = false;
                extra.antialiasing = AntialiasingMode.None;
                extra.dithering = false;
            }

            var volumes = FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < volumes.Length; i++)
            {
                var volume = volumes[i];
                if (volume == null)
                    continue;
                var profile = volume.profile;
                if (profile == null)
                    continue;
                if (profile.TryGet(out DepthOfField dof))
                    dof.active = false;
                if (profile.TryGet(out MotionBlur motion))
                    motion.active = false;
            }
        }

        void Update()
        {
            if (!_built || _uiFromScene)
                return;
            var size = new Vector2(Screen.width, Screen.height);
            if (size != _lastScreen)
                ApplyLayout();
        }

        void LateUpdate()
        {
            if (_screen != UiScreen.Health)
                return;
            CropChatBackground();
            FadeBodyClip();
            if (!_holdCam || _framedCam == null)
                return;
            _framedCam.transform.position = _camHoldPos;
            _framedCam.transform.LookAt(_camLook);
            _framedCam.fieldOfView = 18f;
            _framedCam.rect = new Rect(0.56f, 0f, 0.44f, 1f);
            _framedCam.clearFlags = CameraClearFlags.SolidColor;
            _framedCam.backgroundColor = new Color(0.86f, 0.9f, 0.98f, 1f);
            foreach (var behaviour in _framedCam.GetComponentsInParent<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Name == "ConvaiOrbitCamera")
                    behaviour.enabled = false;
            }
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

            if (kind == "gear")
            {
                for (var i = 0; i < 8; i++)
                {
                    var ang = i * Mathf.PI * 2f / 8f;
                    Disc(32 + Mathf.Cos(ang) * 20f, 32 + Mathf.Sin(ang) * 20f, 7f, ink);
                }
                Disc(32, 32, 16, ink);
                Disc(32, 32, 7, clear);
            }
            else if (kind == "user")
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
            else if (kind == "down")
            {
                for (var y = 18; y <= 42; y++)
                for (var x = 12; x <= 52; x++)
                {
                    var t = (y - 18) / 24f;
                    var half = 4f + t * 16f;
                    if (Mathf.Abs(x - 32) <= half)
                        pixels[y * s + x] = ink;
                }
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

            // Fondo completo en 1920×1080 (sin recortar el logo de arriba)
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
            bgGo.transform.SetParent(canvasGo.transform, false);
            _bgRt = bgGo.GetComponent<RectTransform>();
            var raw = bgGo.GetComponent<RawImage>();
            var tex = Resources.Load<Texture2D>("Empathia/LoginBackground");
            raw.texture = tex != null ? tex : BuildFallbackGradient(64, 36);
            raw.color = Color.white;
            PlaceBackground();

            BuildLoginView(canvasGo.transform);
            BuildPickStudentView(canvasGo.transform);
            BuildConfirmView(canvasGo.transform);
            BuildHealthView(canvasGo.transform);
            BuildReportView(canvasGo.transform);
            BuildSettingsView(canvasGo.transform);
            BuildSettingsGear(canvasGo.transform);
            BuildAlertModal(canvasGo.transform);
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

            var card = CreateImage(_loginView.transform, "Card", CardGlass);
            ApplyRounded(card, RoundSprite(256, 48), 1.05f);
            _cardRt = card.rectTransform;
            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(48, 48, 40, 36);
            cardLayout.spacing = 18;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            var cardFitter = card.gameObject.AddComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            PlaceLoginCard();

            _studentLoginPanel = CreateLoginPanel(_cardRt, "StudentFields");
            AddLabel(_studentLoginPanel.transform, "Elige tu nombre", 20, FontStyles.Bold, Navy, 28, TextAlignmentOptions.Center);
            _loginListContent = CreateScrollList(_studentLoginPanel.transform, 180);
            _refreshListBtn = AddOutlineButton(_studentLoginPanel.transform, "Actualizar lista", 50, () => StartCoroutine(LoadDirectoryList()));

            _registerPanel = CreateLoginPanel(_cardRt, "RegisterFields");
            _regDoc = AddIconInput(_registerPanel.transform, "Número de documento", "", "lock", false);
            _regName = AddIconInput(_registerPanel.transform, "Nombre y apellido", "", "user", false);
            _regCampus = AddOptionDropdown(
                _registerPanel.transform,
                "Sede registro",
                "Sede Principal",
                "Sede Jhon F. kennedy",
                "Sede Gustavo Rojas Pinilla",
                "Sede Villa Paraguay");
            _regGrade = AddOptionDropdown(
                _registerPanel.transform,
                "Grado registro",
                GradesForCampus("Sede Principal"));
            _regCampus.onValueChanged.AddListener(_ => RefreshRegisterGrades());
            RefreshRegisterGrades();
            _regShift = AddOptionDropdown(
                _registerPanel.transform,
                "Jornada registro",
                "mañana",
                "tarde");
            _createStudentBtn = AddGradientButton(_registerPanel.transform, "Crear perfil", 64, OnCreateStudent, 22f);
            _backToLoginBtn = AddOutlineButton(_registerPanel.transform, "Volver", 56, () => ShowRegisterForm(false));
            _registerPanel.SetActive(false);

            _adultLoginPanel = CreateLoginPanel(_cardRt, "AdultFields");
            AddLabel(_adultLoginPanel.transform, "Iniciar sesión", 26, FontStyles.Bold, Navy, 40, TextAlignmentOptions.Center);
            _user = AddIconInput(_adultLoginPanel.transform, "Usuario del psicoorientador", "orientador1", "user", false);
            _pass = AddIconInput(_adultLoginPanel.transform, "Contraseña", "password", "lock", true);
            _loginBtn = AddGradientButton(_adultLoginPanel.transform, "Iniciar sesión", 64, OnLogin, 22f);

            _registerBtn = AddOutlineButton(_cardRt, "Registrarse", 58, OnRegister);
            _loginHint = AddLabel(_cardRt, "La lista muestra solo el nombre. Al entrar usa todos los datos del perfil.", 16, FontStyles.Normal, Muted, 36, TextAlignmentOptions.Center);
            _loginStatus = AddLabel(_cardRt, "", 15, FontStyles.Normal, Muted, 26, TextAlignmentOptions.Center);
            ShowStaffLogin(true);
        }

        GameObject CreateLoginPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.spacing = 16;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            go.GetComponent<LayoutElement>().flexibleWidth = 1f;
            return go;
        }

        void ShowStaffLogin(bool showStaff)
        {
            if (_adultLoginPanel != null)
                _adultLoginPanel.SetActive(showStaff);
            if (_loginHint != null)
                _loginHint.gameObject.SetActive(!showStaff);

            if (showStaff)
            {
                if (_studentLoginPanel != null)
                    _studentLoginPanel.SetActive(false);
                if (_registerPanel != null)
                    _registerPanel.SetActive(false);
                if (_registerBtn != null)
                    _registerBtn.gameObject.SetActive(false);
                SetLoginStatus("Inicia sesión del psicoorientador.");
                RefreshLoginCardLayout();
                QueueLoginCardFit();
                return;
            }

            ShowRegisterForm(false);
        }

        void OnRegister()
        {
            if (string.IsNullOrEmpty(EmpathiaAuthState.AdultToken))
            {
                ShowAlertModal("Falta el ingreso", "Primero inicia sesión como psicoorientador.");
                ShowStaffLogin(true);
                return;
            }

            ShowRegisterForm(true);
        }

        void ShowRegisterForm(bool register)
        {
            if (_studentLoginPanel != null)
                _studentLoginPanel.SetActive(!register);
            if (_registerPanel != null)
                _registerPanel.SetActive(register);
            if (_registerBtn != null)
                _registerBtn.gameObject.SetActive(!register);
            SetLoginStatus(register
                ? "Completa tus datos para crear el perfil."
                : "Elige tu nombre en la lista.");
            if (!register)
                StartCoroutine(LoadDirectoryList());
            RefreshLoginCardLayout();
            QueueLoginCardFit();
        }

        void RefreshRegisterGrades()
        {
            if (_regGrade == null)
                return;
            SetDropdownOptions(_regGrade, GradesForCampus(SelectedOption(_regCampus)), SelectedOption(_regGrade));
        }

        static void SplitNombreApellido(string fullName, out string nombres, out string apellidos)
        {
            var text = (fullName ?? "").Trim();
            var split = text.LastIndexOf(' ');
            if (split <= 0)
            {
                nombres = text;
                apellidos = text;
                return;
            }

            nombres = text.Substring(0, split).Trim();
            apellidos = text.Substring(split + 1).Trim();
            if (string.IsNullOrWhiteSpace(nombres))
                nombres = text;
            if (string.IsNullOrWhiteSpace(apellidos))
                apellidos = text;
        }

        static int EdadForGrade(string grado)
        {
            switch (grado)
            {
                case "Jardín":
                    return 5;
                case "Transición":
                    return 6;
                case "1°":
                    return 7;
                case "2°":
                    return 8;
                case "3°":
                    return 9;
                case "4°":
                    return 10;
                case "5°":
                    return 11;
                case "6°":
                    return 12;
                case "7°":
                    return 13;
                case "8°":
                    return 14;
                case "9°":
                    return 15;
                case "10°":
                    return 16;
                case "11°":
                    return 17;
                default:
                    return 13;
            }
        }

        void OnCreateStudent()
        {
            if (_busy) return;
            ApplyServerFromUi();

            var documento = _regDoc != null ? _regDoc.text.Trim() : "";
            var nombreCompleto = _regName != null ? _regName.text.Trim() : "";
            var sede = SelectedOption(_regCampus);
            var grado = SelectedOption(_regGrade);
            var jornada = SelectedOption(_regShift);

            if (string.IsNullOrWhiteSpace(documento) || string.IsNullOrWhiteSpace(nombreCompleto)
                || string.IsNullOrWhiteSpace(sede) || string.IsNullOrWhiteSpace(grado)
                || string.IsNullOrWhiteSpace(jornada))
            {
                ShowAlertModal("Datos incompletos", "Completa documento, nombre y apellido, sede, grado y jornada.");
                return;
            }

            SplitNombreApellido(nombreCompleto, out var nombres, out var apellidos);

            SetBusy(true);
            SetLoginStatus("Registrando estudiante…");
            StartCoroutine(_api.RegisterStudent(
                nombres,
                apellidos,
                documento,
                grado,
                sede,
                jornada,
                EdadForGrade(grado),
                "0000000000",
                "pendiente",
                (ok, msg) =>
                {
                    SetBusy(false);
                    if (!ok)
                    {
                        ShowAlertModal("No se pudo registrar", msg);
                        return;
                    }

                    ShowRegisterForm(false);
                    ShowAlertModal("Registro listo", msg);
                }));
        }

        Transform CreateScrollList(Transform parent, float height)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.transform.SetParent(parent, false);
            scrollGo.GetComponent<Image>().color = FieldBg;
            ApplyRounded(scrollGo.GetComponent<Image>(), RoundSprite(128, 24), 1f);
            var le = scrollGo.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleWidth = 1f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<RectTransform>().offsetMin = new Vector2(8, 8);
            viewport.GetComponent<RectTransform>().offsetMax = new Vector2(-8, -8);

            var list = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(viewport.transform, false);
            var listRt = list.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0, 1);
            listRt.anchorMax = new Vector2(1, 1);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = Vector2.zero;
            listRt.sizeDelta = new Vector2(0, 0);
            var listV = list.GetComponent<VerticalLayoutGroup>();
            listV.spacing = 8;
            listV.childControlWidth = true;
            listV.childControlHeight = true;
            listV.childForceExpandWidth = true;
            listV.childForceExpandHeight = false;
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = listRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            return list.transform;
        }

        IEnumerator LoadDirectoryList()
        {
            if (_loginListContent == null)
                yield break;

            ApplyServerFromUi();

            SetBusy(true);
            SetLoginStatus("Cargando estudiantes…");
            for (var i = _loginListContent.childCount - 1; i >= 0; i--)
                Destroy(_loginListContent.GetChild(i).gameObject);

            var ok = false;
            var msg = "";
            StudentListItem[] items = null;
            yield return _api.ListStudents((success, message, data) =>
            {
                ok = success;
                msg = message;
                items = data;
            });

            SetBusy(false);
            if (!ok)
            {
                _directoryStudents = new StudentListItem[0];
                SetLoginStatus("");
                ShowAlertModal("No se pudo cargar la lista", msg);
                RefreshLoginCardLayout();
                yield break;
            }

            _directoryStudents = items ?? new StudentListItem[0];
            foreach (var item in _directoryStudents)
            {
                if (item == null)
                    continue;
                var captured = item;
                AddOutlineButton(_loginListContent, captured.PreviewName, 56, () => OnPickDirectoryStudent(captured));
            }

            SetLoginStatus(_directoryStudents.Length == 0
                ? "No hay estudiantes. Pulsa Registrarse."
                : "Elige tu nombre. Hay " + _directoryStudents.Length + " perfil(es).");
            var rows = _directoryStudents.Length;
            SetStudentScrollHeight(rows == 0 ? 80f : Mathf.Clamp(rows * 64f + 16f, 80f, 180f));
            RefreshLoginCardLayout();
            QueueLoginCardFit();
        }

        void OnPickDirectoryStudent(StudentListItem item)
        {
            if (_busy || item == null)
                return;

            SetBusy(true);
            SetLoginStatus("Ingresando como " + item.PreviewName + "…");
            StartCoroutine(_api.EnterAsDirectoryStudent(item, (ok, msg) =>
            {
                SetBusy(false);
                if (!ok)
                {
                    ShowAlertModal("No se pudo ingresar", msg);
                    return;
                }

                Debug.Log("[Empathia] Ingreso con perfil completo: " + item.PreviewName
                    + " doc=" + (item.documento_numero ?? "")
                    + " grado=" + (item.grado ?? "")
                    + " sede=" + (item.sede ?? "")
                    + " jornada=" + (item.jornada ?? ""));
                SetLoginStatus("Ingreso OK. Confirma para continuar.");
                ShowScreen(UiScreen.Confirm);
            }));
        }

        void OnCheckConnectionB()
        {
            if (_busy) return;
            ApplyServerFromUi();
            StartCoroutine(CheckConnectionToB(silent: false));
        }

        IEnumerator CheckConnectionToB(bool silent)
        {
            SetBusy(true);
            if (!silent)
            {
                SetLoginStatus("Comprobando el servidor…");
                SetSettingsStatus("Comprobando el servidor…");
            }

            var ok = false;
            var msg = "";
            yield return _api.CheckHealth((success, message) =>
            {
                ok = success;
                msg = message;
            });

            SetBusy(false);
            if (ok)
            {
                Debug.Log("[Empathia] " + msg);
                if (!silent && !string.IsNullOrEmpty(EmpathiaAuthState.AdultToken))
                    yield return LoadDirectoryList();
                else if (!silent)
                {
                    SetLoginStatus("Conexión OK. Inicia sesión del psicoorientador.");
                    SetSettingsStatus("Conexión OK.");
                }
            }
            else
            {
                SetSettingsStatus("Sin conexión. Revisa la dirección del servidor.");
                ShowAlertModal("Sin conexión", msg);
                Debug.LogWarning("[Empathia] " + msg);
            }
        }

        void BuildPickStudentView(Transform canvas)
        {
            _pickStudentView = new GameObject("PickStudentView", typeof(RectTransform));
            _pickStudentView.transform.SetParent(canvas, false);
            StretchFull(_pickStudentView.GetComponent<RectTransform>());

            var dim = CreateImage(_pickStudentView.transform, "Dim", new Color(0.1f, 0.08f, 0.18f, 0.35f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            var card = CreateImage(_pickStudentView.transform, "PickCard", CardGlass);
            ApplyRounded(card, RoundSprite(256, 48), 1.05f);
            var cardRt = card.rectTransform;
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(560, 520);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(cardRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            StretchFull(contentRt);
            contentRt.offsetMin = new Vector2(28, 24);
            contentRt.offsetMax = new Vector2(-28, -24);
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            AddLabel(content.transform, "Elegir estudiante", 26, FontStyles.Bold, Navy, 36, TextAlignmentOptions.Center);
            AddLabel(content.transform, "Elige el perfil del estudiante.", 14, FontStyles.Normal, Muted, 24, TextAlignmentOptions.Center);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.transform.SetParent(content.transform, false);
            scrollGo.GetComponent<Image>().color = FieldBg;
            ApplyRounded(scrollGo.GetComponent<Image>(), RoundSprite(128, 24), 1f);
            scrollGo.GetComponent<LayoutElement>().preferredHeight = 280;
            scrollGo.GetComponent<LayoutElement>().flexibleHeight = 1f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<RectTransform>().offsetMin = new Vector2(8, 8);
            viewport.GetComponent<RectTransform>().offsetMax = new Vector2(-8, -8);

            var list = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(viewport.transform, false);
            var listRt = list.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0, 1);
            listRt.anchorMax = new Vector2(1, 1);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = Vector2.zero;
            listRt.sizeDelta = new Vector2(0, 0);
            var listV = list.GetComponent<VerticalLayoutGroup>();
            listV.spacing = 8;
            listV.childControlWidth = true;
            listV.childControlHeight = true;
            listV.childForceExpandWidth = true;
            listV.childForceExpandHeight = false;
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = listRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            _studentListContent = list.transform;

            _pickStatus = AddLabel(content.transform, "", 13, FontStyles.Normal, Muted, 28, TextAlignmentOptions.Center);
            _pickRefreshBtn = AddOutlineButton(content.transform, "Actualizar lista", 48, () => StartCoroutine(LoadStudentList()));
            _pickBackBtn = AddOutlineButton(content.transform, "Volver", 48, OnPickBack);
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

            AddLabel(content.transform, "EmpathIA", 14, FontStyles.Bold, Purple, 20, TextAlignmentOptions.Center);
            AddLabel(content.transform, "Inicio de sesión confirmado", 26, FontStyles.Bold, Navy, 36, TextAlignmentOptions.Center);
            AddLabel(content.transform, "Tu cuenta está lista. Continúa para entrar a tu espacio de bienestar.", 15, FontStyles.Normal, Muted, 48, TextAlignmentOptions.Center);
            _confirmBtn = AddGradientButton(content.transform, "Continuar", 64, OnConfirmEnterHealth);
            _confirmBackBtn = AddOutlineButton(content.transform, "Volver", 56, () => ShowScreen(UiScreen.Login));
        }

        void BuildHealthView(Transform canvas)
        {
            _healthView = new GameObject("HealthView", typeof(RectTransform));
            _healthView.transform.SetParent(canvas, false);
            StretchFull(_healthView.GetComponent<RectTransform>());

            var card = CreateImage(_healthView.transform, "HealthCard", CardGlass);
            ApplyRounded(card, RoundSprite(256, 48), 1.05f);
            _healthRt = card.rectTransform;
            _healthRt.anchorMin = _healthRt.anchorMax = new Vector2(0.03f, 0.5f);
            _healthRt.pivot = new Vector2(0f, 0.5f);
            _healthRt.anchoredPosition = Vector2.zero;
            _healthRt.sizeDelta = new Vector2(860, 900);

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

            BindMouthHint(content.transform);
            _welcomeTitle = AddLabel(content.transform, "¡Bienvenido!", 34, FontStyles.Bold, Navy, 44, TextAlignmentOptions.Center);
            _welcomeSub = AddLabel(content.transform, "Este es tu espacio de acompañamiento emocional.", 16, FontStyles.Normal, Muted, 28, TextAlignmentOptions.Center);
            _recordHint = AddLabel(content.transform, "Pulsa Hablar, di algo y luego Detener.", 14, FontStyles.Normal, Muted, 24, TextAlignmentOptions.Center);

            _recordBtn = AddGradientButton(content.transform, "Hablar", 72, OnRecordPressed, 22f);
            var textTf = _recordBtn.transform.Find("Text");
            _recordBtnLabel = textTf != null
                ? textTf.GetComponent<TextMeshProUGUI>()
                : _recordBtn.GetComponentInChildren<TextMeshProUGUI>();

            _typedMessage = AddCompactInput(content.transform, "Escribe un mensaje", "");
            _sendTextBtn = AddOutlineButton(content.transform, "Enviar", 48, OnSendTypedText);

            _state = AddLabel(content.transform, "", 13, FontStyles.Bold, Navy, 20, TextAlignmentOptions.Center);
            if (_state != null)
                _state.gameObject.SetActive(false);
            _status = AddLabel(content.transform, "", 13, FontStyles.Normal, Muted, 36, TextAlignmentOptions.Center);
            AddLabel(content.transform, "Tú", 12, FontStyles.Bold, Muted, 18, TextAlignmentOptions.Center);
            _transcript = AddLabel(content.transform, "(aún no hay mensaje)", 15, FontStyles.Normal, Navy, 44, TextAlignmentOptions.Center);
            AddLabel(content.transform, "EmpathIA", 12, FontStyles.Bold, Muted, 18, TextAlignmentOptions.Center);
            _reply = AddLabel(content.transform, "(sin respuesta)", 15, FontStyles.Normal, new Color(0.2f, 0.55f, 0.4f), 48, TextAlignmentOptions.Center);
            _logoutBtn = AddOutlineButton(content.transform, "Terminar conversación", 48, OnEndConversation);

            _labRt = _healthRt;
        }

        void EnsureReportView()
        {
            if (_reportView != null)
                return;
            Transform canvas = null;
            if (_healthView != null)
                canvas = _healthView.transform.parent;
            else if (_loginView != null)
                canvas = _loginView.transform.parent;
            if (canvas == null)
                return;
            BuildReportView(canvas);
        }

        void BuildReportView(Transform canvas)
        {
            _reportView = new GameObject("ReportView", typeof(RectTransform));
            _reportView.transform.SetParent(canvas, false);
            StretchFull(_reportView.GetComponent<RectTransform>());

            var card = CreateImage(_reportView.transform, "ReportCard", CardGlass);
            ApplyRounded(card, RoundSprite(256, 48), 1.05f);
            var cardRt = card.rectTransform;
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.50f);
            cardRt.sizeDelta = new Vector2(780, 640);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(cardRt, false);
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

            AddLabel(content.transform, "EmpathIA", 14, FontStyles.Bold, Purple, 20, TextAlignmentOptions.Center);
            _reportTitle = AddLabel(content.transform, "Reporte de esta charla", 26, FontStyles.Bold, Navy, 40, TextAlignmentOptions.Center);
            _reportMeta = AddLabel(content.transform, "", 15, FontStyles.Normal, Muted, 56, TextAlignmentOptions.Center);
            var list = CreateScrollList(content.transform, 300);
            _reportBody = AddLabel(list, "El resumen aparecerá aquí.", 15, FontStyles.Normal, Navy, 80, TextAlignmentOptions.TopLeft);
            var bodyLe = _reportBody.GetComponent<LayoutElement>();
            bodyLe.minHeight = 80;
            bodyLe.preferredHeight = -1;
            bodyLe.flexibleHeight = 0;
            var fitter = _reportBody.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _reportBackBtn = AddGradientButton(content.transform, "Volver a la lista", 56, OnReportBack);
            _reportView.SetActive(false);
        }

        void TogglePassword()
        {
            if (_pass == null)
                return;
            _showPass = !_showPass;
            _pass.contentType = _showPass
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            _pass.ForceLabelUpdate();
            if (_eyeBtn == null)
                return;
            var eyeTf = _eyeBtn.transform.Find("EyeIcon");
            var eyeImg = eyeTf != null ? eyeTf.GetComponent<Image>() : _eyeBtn.GetComponentInChildren<Image>();
            if (eyeImg != null && eyeImg.gameObject != _eyeBtn.gameObject)
                eyeImg.color = _showPass ? Purple : Muted;
        }

        void OnPickBack()
        {
            EmpathiaAuthState.ClearAll();
            ShowScreen(UiScreen.Login);
        }

        public bool HasSceneUi => _loginView != null;

        public void BakeUiInEditor()
        {
            if (_loginView != null)
                return;
            _built = false;
            _uiFromScene = false;
            BuildUi();
        }

        void BindMouthFromScene()
        {
            if (_mouth == null)
                _mouth = GetComponent<EmpathiaMouthDriver>() ?? gameObject.AddComponent<EmpathiaMouthDriver>();
            var mouthTf = transform.Find("EmpathiaLoginCanvas/HealthView/HealthCard/Content/MouthHint/Mouth");
            if (mouthTf == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                for (var i = 0; i < images.Length; i++)
                {
                    if (images[i] != null && images[i].gameObject.name == "Mouth")
                    {
                        _mouth.BindUi(images[i]);
                        return;
                    }
                }
                return;
            }
            var mouth = mouthTf.GetComponent<Image>();
            if (mouth != null)
                _mouth.BindUi(mouth);
        }

        void WireUi()
        {
            BindClick(_refreshListBtn, () => StartCoroutine(LoadDirectoryList()));
            BindClick(_createStudentBtn, OnCreateStudent);
            BindClick(_backToLoginBtn, () => ShowRegisterForm(false));
            BindClick(_loginBtn, OnLogin);
            BindClick(_registerBtn, OnRegister);
            BindClick(_checkBBtn, OnCheckConnectionB);
            BindClick(_confirmBtn, OnConfirmEnterHealth);
            BindClick(_confirmBackBtn, () => ShowScreen(UiScreen.Login));
            BindClick(_pickRefreshBtn, () => StartCoroutine(LoadStudentList()));
            BindClick(_pickBackBtn, OnPickBack);
            BindClick(_recordBtn, OnRecordPressed);
            BindClick(_sendTextBtn, OnSendTypedText);
            SetButtonLabel(_logoutBtn, "Terminar conversación");
            BindClick(_logoutBtn, OnEndConversation);
            BindClick(_reportBackBtn, OnReportBack);
            BindClick(_settingsGearBtn, ToggleSettings);
            BindClick(_settingsSaveBtn, OnSaveSettings);
            BindClick(_settingsCloseBtn, () => ShowSettings(false));
            BindClick(_alertCloseBtn, HideAlertModal);
            BindClick(_eyeBtn, TogglePassword);
        }

        static void BindClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        static void SetButtonLabel(Button button, string label)
        {
            if (button == null || string.IsNullOrEmpty(label))
                return;
            var t = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null)
                t.text = label;
        }

        public void ApplyRuntimeSkin()
        {
            var images = GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null)
                    continue;
                var n = img.gameObject.name;
                if (n == "Card" || n == "ConfirmCard" || n == "HealthCard" || n == "SettingsCard"
                    || n == "AlertCard" || n == "PickCard")
                    ApplyRounded(img, RoundSprite(256, 48), 1.05f);
                else if (n == "CardShadow")
                    ApplyRounded(img, RoundSprite(256, 48), 1.0f);
                else if (n == "Mouth")
                    ApplyRounded(img, RoundSprite(64, 24), 1.4f);
                else if (n == "SettingsGear")
                    ApplyRounded(img, RoundSprite(128, 48), 1.2f);
            }

            if (_loginBtn != null) SkinGradient(_loginBtn);
            if (_createStudentBtn != null) SkinGradient(_createStudentBtn);
            if (_confirmBtn != null) SkinGradient(_confirmBtn);
            if (_recordBtn != null) SkinGradient(_recordBtn);
            if (_settingsSaveBtn != null) SkinGradient(_settingsSaveBtn);
            if (_alertCloseBtn != null) SkinGradient(_alertCloseBtn);

            if (_settingsGearBtn != null)
            {
                var iconTf = _settingsGearBtn.transform.Find("Icon");
                var icon = iconTf != null ? iconTf.GetComponent<Image>() : null;
                if (icon != null)
                    icon.sprite = BuildIconSprite("gear");
            }
        }

        void SkinGradient(Button button)
        {
            var img = button.GetComponent<Image>();
            if (img == null)
                return;
            img.sprite = GradientButtonSprite();
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.05f;
        }

        void BindMouthHint(Transform parent)
        {
            var row = new GameObject("MouthHint", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 36f;
            row.GetComponent<LayoutElement>().minHeight = 36f;

            var mouth = CreateImage(row.transform, "Mouth", new Color(0.45f, 0.22f, 0.32f, 0.9f));
            ApplyRounded(mouth, RoundSprite(64, 24), 1.4f);
            var rt = mouth.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(42f, 6f);

            if (_mouth == null)
                _mouth = GetComponent<EmpathiaMouthDriver>() ?? gameObject.AddComponent<EmpathiaMouthDriver>();
            _mouth.BindUi(mouth);
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
                _recordBtnLabel.text = recording ? "Detener" : "Hablar";
                _recordBtnLabel.ForceMeshUpdate();
            }

            if (_recordHint != null)
                _recordHint.text = recording
                    ? "Grabando… pulsa Detener cuando termines."
                    : "Pulsa Hablar, di algo y luego Detener.";
        }

        void ShowScreen(UiScreen screen)
        {
            _screen = screen;
            HideAlertModal();
            ShowSettings(false);
            if (_loginView != null) _loginView.SetActive(screen == UiScreen.Login);
            if (_pickStudentView != null) _pickStudentView.SetActive(screen == UiScreen.PickStudent);
            if (_confirmView != null) _confirmView.SetActive(screen == UiScreen.Confirm);
            if (_healthView != null) _healthView.SetActive(screen == UiScreen.Health);
            if (_reportView != null) _reportView.SetActive(screen == UiScreen.Report);
            ApplyChatStage(screen == UiScreen.Health);
        }

        void ApplyChatStage(bool chat)
        {
            if (_bgRt == null)
            {
                var bg = GameObject.Find("Background");
                if (bg != null)
                    _bgRt = bg.GetComponent<RectTransform>();
            }

            if (_healthRt == null && _healthView != null)
            {
                var card = _healthView.transform.Find("HealthCard");
                if (card != null)
                    _healthRt = card as RectTransform;
            }

            CropChatBackground(chat);
            if (!chat && _framedCam != null)
            {
                _holdCam = false;
                _framedCam.rect = new Rect(0f, 0f, 1f, 1f);
            }

            if (_healthRt != null && chat)
            {
                _healthRt.anchorMin = new Vector2(0.03f, 0.06f);
                _healthRt.anchorMax = new Vector2(0.52f, 0.94f);
                _healthRt.pivot = new Vector2(0.5f, 0.5f);
                _healthRt.offsetMin = Vector2.zero;
                _healthRt.offsetMax = Vector2.zero;
            }

            SetSessionCharacterVisible(chat);
            if (chat)
                StartCoroutine(FrameSessionCharacter());
        }

        void CropChatBackground()
        {
            CropChatBackground(true);
        }

        void CropChatBackground(bool chat)
        {
            var rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < rects.Length; i++)
            {
                var rt = rects[i];
                if (rt == null || !rt.gameObject.scene.IsValid())
                    continue;
                var raw = rt.GetComponent<RawImage>();
                if (raw == null)
                    continue;
                var fullBleed = rt.anchorMin.x <= 0.02f && rt.anchorMax.x >= 0.9f && rt.anchorMax.y >= 0.9f;
                if (rt.name != "Background" && !fullBleed)
                    continue;
                if (chat)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = new Vector2(0.56f, 1f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    raw.raycastTarget = false;
                }
                else if (rt.name == "Background")
                {
                    StretchFull(rt);
                }
            }
        }

        IEnumerator FrameSessionCharacter()
        {
            yield return null;
            var avatar = FindNamedInScene("Convai Character");
            if (avatar == null)
                avatar = FindNamedInScene("personaje");
            var leftover = FindNamedInScene("personaje");
            if (leftover != null && leftover != avatar)
                leftover.SetActive(false);
            var cam = FindSessionCamera();
            if (avatar != null)
                ActivateChain(avatar);
            if (cam != null)
                ActivateChain(cam.gameObject);
            if (cam != null)
                cam.enabled = true;
            if (avatar == null || cam == null)
                yield break;
            yield return null;

            foreach (var behaviour in cam.GetComponentsInParent<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Name == "ConvaiOrbitCamera")
                    behaviour.enabled = false;
            }

            var renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers == null || renderers.Length == 0)
                yield break;

            for (var r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] == null)
                    continue;
                renderers[r].enabled = true;
                renderers[r].gameObject.SetActive(true);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            var look = bounds.center + Vector3.up * (bounds.extents.y * 0.68f);
            var toCamera = cam.transform.position - look;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.01f)
                toCamera = Vector3.back;
            avatar.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);

            cam.fieldOfView = 18f;
            var dist = Mathf.Clamp(bounds.size.y * 0.5f, 0.48f, 0.95f);
            SoftenEyeShine(avatar);
            cam.transform.position = look + avatar.transform.forward * dist;
            cam.transform.LookAt(look);
            _camLook = look;
            _camHoldPos = cam.transform.position;
            _holdCam = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.86f, 0.9f, 0.98f, 1f);
            cam.rect = new Rect(0.56f, 0f, 0.44f, 1f);
            _framedCam = cam;
            if (!cam.CompareTag("MainCamera"))
                cam.tag = "MainCamera";
            EnableEmbodiment(avatar, cam);
            yield return null;
            var emotion = avatar.GetComponent<ConvaiEmotionController>();
            if (emotion != null)
            {
                emotion.SetMood("trust", 0.35f, 1.2f);
            }
            Debug.Log("[Empathia] Personaje encuadrado: " + avatar.name);
        }

        static void SilenceConvaiConnection()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                    continue;
                var typeName = behaviour.GetType().Name;
                if (typeName == "ConvaiManager"
                    || typeName == "ConvaiRoomManager"
                    || typeName == "ConvaiLipSyncComponent"
                    || typeName == "ConvaiPlayer"
                    || typeName == "ConvaiIdleResetDriver"
                    || typeName == "ConvaiBodyAnimationController")
                    behaviour.enabled = false;
                if (typeName == "ConnectionStatusIndicator")
                    behaviour.gameObject.SetActive(false);
            }
        }

        void ApplyReplyEmotion(string reply)
        {
            var avatar = GameObject.Find("Convai Character");
            if (avatar == null)
                avatar = GameObject.Find("personaje");
            if (avatar == null)
                return;

            var emotion = avatar.GetComponent<ConvaiEmotionController>();
            if (emotion == null)
                return;

            var label = "trust";
            var intensity = 0.35f;
            var text = (reply ?? "").ToLowerInvariant();
            if (text.Contains("triste") || text.Contains("lo siento") || text.Contains("difícil") || text.Contains("dificil"))
            {
                label = "sadness";
                intensity = 0.45f;
            }
            else if (text.Contains("preocup") || text.Contains("miedo") || text.Contains("ansie"))
            {
                label = "fear";
                intensity = 0.4f;
            }
            else if (text.Contains("enojo") || text.Contains("molest"))
            {
                label = "anger";
                intensity = 0.4f;
            }
            else if (text.Contains("me alegra") || text.Contains("qué bien") || text.Contains("que bien") || text.Contains("feliz"))
            {
                label = "joy";
                intensity = 0.55f;
            }

            emotion.SetMood(label, intensity * 0.35f, 0.8f);
        }

        static void SoftenEyeShine(GameObject avatar)
        {
            var renderers = avatar.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;
                var n = renderer.gameObject.name.ToLowerInvariant();
                if (!n.Contains("eye") && !n.Contains("tear") && !n.Contains("cornea"))
                    continue;
                var mats = renderer.materials;
                for (var m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null)
                        continue;
                    if (mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", 0.12f);
                    if (mat.HasProperty("_Glossiness"))
                        mat.SetFloat("_Glossiness", 0.12f);
                    if (mat.HasProperty("_GlossMapScale"))
                        mat.SetFloat("_GlossMapScale", 0.12f);
                    if (mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 0f);
                    if (mat.HasProperty("_SpecularHighlights"))
                        mat.SetFloat("_SpecularHighlights", 0f);
                    if (mat.HasProperty("_EnvironmentReflections"))
                        mat.SetFloat("_EnvironmentReflections", 0f);
                    if (mat.HasProperty("_SpecularColor"))
                        mat.SetColor("_SpecularColor", new Color(0.03f, 0.03f, 0.03f, 1f));
                    if (n.Contains("tear") || n.Contains("occlus"))
                    {
                        if (mat.HasProperty("_BaseColor"))
                        {
                            var c = mat.GetColor("_BaseColor");
                            c.a = 0.12f;
                            mat.SetColor("_BaseColor", c);
                        }
                        if (mat.HasProperty("_Color"))
                        {
                            var c = mat.GetColor("_Color");
                            c.a = 0.12f;
                            mat.SetColor("_Color", c);
                        }
                    }
                }
            }

            var lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null || light.type != LightType.Directional)
                    continue;
                if (light.intensity > 0.85f)
                    light.intensity = 0.85f;
            }
        }

        static void KeepCharacterLocal(GameObject avatar)
        {
            var character = avatar.GetComponent<ConvaiCharacter>();
            if (character == null)
                return;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var autoConnect = typeof(ConvaiCharacter).GetField("_autoConnect", flags);
            if (autoConnect != null)
                autoConnect.SetValue(character, false);
            var ready = typeof(ConvaiCharacter).GetField("_isCharacterReady", flags);
            if (ready != null)
                ready.SetValue(character, true);
            character.enabled = true;
        }

        static Animator _bodyAnimator;
        static AnimationClip _idleClip;
        static AnimationClip _talkClip;
        static PlayableGraph _bodyGraph;
        static AnimationMixerPlayable _bodyMixer;
        static AnimationClipPlayable _talkPlayable;
        static float _talkBlend;
        static float _talkGoal;
        const float BodyFadeSeconds = 0.85f;

        static void EnsureBodyMotion(GameObject avatar)
        {
            var animator = avatar.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = avatar.AddComponent<Animator>();
            if (animator.avatar == null || !animator.avatar.isHuman)
            {
#if UNITY_EDITOR
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                    "Packages/com.convai.convai-sdk-for-unity/SamplesShared/Characters/Sofia/Sofia.Fbx");
                for (var i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is Avatar human && human.isHuman)
                    {
                        animator.avatar = human;
                        break;
                    }
                }
#endif
            }

            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = null;
            _bodyAnimator = animator;

            var convaiMotion = avatar.GetComponent<ConvaiBodyAnimationController>();
            if (convaiMotion != null)
                convaiMotion.enabled = false;

#if UNITY_EDITOR
            var set = UnityEditor.AssetDatabase.LoadAssetAtPath<ConvaiBodyAnimationSet>(
                "Packages/com.convai.convai-sdk-for-unity/SamplesShared/Profiles/Embodiment/BodyAnimation/ConvaiBodyAnimationSet_Female.asset");
            if (set != null)
            {
                if (set.Idles != null && set.Idles.Count > 0)
                    _idleClip = set.Idles[0].Clip;
                if (set.Talks != null && set.Talks.Count > 0)
                    _talkClip = set.Talks[0].Clip;
            }
#endif
            EnsureBodyGraph();
        }

        static void EnsureBodyGraph()
        {
            if (_bodyAnimator == null || _idleClip == null || _bodyGraph.IsValid())
                return;

            _bodyGraph = PlayableGraph.Create("EmpathiaBody");
            _bodyGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _bodyMixer = AnimationMixerPlayable.Create(_bodyGraph, 2);
            var idle = AnimationClipPlayable.Create(_bodyGraph, _idleClip);
            _bodyGraph.Connect(idle, 0, _bodyMixer, 0);
            if (_talkClip != null)
            {
                _talkPlayable = AnimationClipPlayable.Create(_bodyGraph, _talkClip);
                _bodyGraph.Connect(_talkPlayable, 0, _bodyMixer, 1);
            }

            _talkBlend = 0f;
            _talkGoal = 0f;
            _bodyMixer.SetInputWeight(0, 1f);
            _bodyMixer.SetInputWeight(1, 0f);
            var output = AnimationPlayableOutput.Create(_bodyGraph, "Body", _bodyAnimator);
            output.SetSourcePlayable(_bodyMixer);
            _bodyGraph.Play();
        }

        static void FadeBodyClip()
        {
            if (!_bodyGraph.IsValid() || !_bodyMixer.IsValid())
                return;
            var step = Time.deltaTime / BodyFadeSeconds;
            _talkBlend = Mathf.MoveTowards(_talkBlend, _talkGoal, step);
            var talk = Mathf.SmoothStep(0f, 1f, _talkBlend);
            _bodyMixer.SetInputWeight(0, 1f - talk);
            _bodyMixer.SetInputWeight(1, talk);
        }

        void NotifyBodySpeaking(bool speaking)
        {
            var avatar = GameObject.Find("Convai Character");
            if (avatar == null)
                avatar = GameObject.Find("personaje");
            if (avatar == null)
                return;

            if (_bodyAnimator == null)
                EnsureBodyMotion(avatar);
            EnsureBodyGraph();
            var next = speaking && _talkClip != null ? 1f : 0f;
            if (next > _talkGoal && _talkPlayable.IsValid())
                _talkPlayable.SetTime(0);
            _talkGoal = next;

            var character = avatar.GetComponent<ConvaiCharacter>();
            var context = avatar.GetComponent<EmbodimentContext>();
            if (character == null || context == null || context.EventHub == null)
                return;
            if (string.IsNullOrWhiteSpace(character.CharacterId))
                return;

            context.EventHub.Publish(CharacterSpeechStateChanged.Create(character.CharacterId, speaking));
            context.EventHub.Publish(speaking
                ? CharacterAudioPlaybackStateChanged.Started(character.CharacterId)
                : CharacterAudioPlaybackStateChanged.Stopped(character.CharacterId));
        }

        static void EnableEmbodiment(GameObject avatar, Camera cam)
        {
            if (avatar == null)
                return;

            KeepCharacterLocal(avatar);
            EnsureBodyMotion(avatar);

            var gaze = avatar.GetComponent<ConvaiGazeController>();
            if (gaze == null)
                gaze = avatar.AddComponent<ConvaiGazeController>();
            gaze.enabled = true;
            gaze.EyeContactMode = GazeEyeContactMode.AlwaysLock;
            if (cam != null)
                gaze.PlayerAnchorOverride = cam.transform;

            var body = avatar.GetComponent<ConvaiBodyLanguageController>();
            if (body == null)
                body = avatar.AddComponent<ConvaiBodyLanguageController>();
            body.enabled = true;

            var emotion = avatar.GetComponent<ConvaiEmotionController>();
            if (emotion == null)
                emotion = avatar.AddComponent<ConvaiEmotionController>();
            emotion.enabled = true;
        }

        static void ActivateChain(GameObject go)
        {
            var chain = new System.Collections.Generic.List<Transform>();
            var t = go.transform;
            while (t != null)
            {
                chain.Add(t);
                t = t.parent;
            }

            for (var i = chain.Count - 1; i >= 0; i--)
            {
                if (!chain[i].gameObject.activeSelf)
                    chain[i].gameObject.SetActive(true);
            }
        }

        static GameObject FindNamedInScene(string objectName)
        {
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null || t.name != objectName || !t.gameObject.scene.IsValid())
                    continue;
                return t.gameObject;
            }

            return null;
        }

        static Camera FindSessionCamera()
        {
            var cams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Camera fallback = null;
            for (var i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam == null || cam.targetTexture != null)
                    continue;
                if (fallback == null)
                    fallback = cam;
                if (cam.CompareTag("MainCamera"))
                    return cam;
                var parentBehaviours = cam.GetComponentsInParent<MonoBehaviour>(true);
                for (var b = 0; b < parentBehaviours.Length; b++)
                {
                    if (parentBehaviours[b] != null && parentBehaviours[b].GetType().Name == "ConvaiOrbitCamera")
                        return cam;
                }
            }

            return fallback != null ? fallback : Camera.main;
        }

        void SetSessionCharacterVisible(bool visible)
        {
            var avatar = FindNamedInScene("Convai Character");
            if (avatar == null)
                avatar = FindNamedInScene("personaje");

            if (avatar != null && avatar.activeSelf != visible)
                avatar.SetActive(visible);
        }

        IEnumerator LoadStudentList()
        {
            SetBusy(true);
            if (_pickStatus != null)
                _pickStatus.text = EmpathiaText.ForUi("Cargando estudiantes…");

            if (_studentListContent != null)
            {
                for (var i = _studentListContent.childCount - 1; i >= 0; i--)
                    Destroy(_studentListContent.GetChild(i).gameObject);
            }

            var ok = false;
            var msg = "";
            StudentListItem[] items = null;
            yield return _api.ListStudents((success, message, data) =>
            {
                ok = success;
                msg = message;
                items = data;
            });

            SetBusy(false);
            if (!ok)
            {
                ShowAlertModal("Error", msg);
                if (_pickStatus != null)
                    _pickStatus.text = EmpathiaText.ForUi("No se pudieron cargar los estudiantes.");
                yield break;
            }

            if (items == null || items.Length == 0)
            {
                if (_pickStatus != null)
                    _pickStatus.text = EmpathiaText.ForUi("No hay estudiantes activos. El admin debe crear perfiles en B.");
                yield break;
            }

            if (_pickStatus != null)
                _pickStatus.text = EmpathiaText.ForUi(msg);

            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.id))
                    continue;
                var label = string.IsNullOrEmpty(item.display_name) ? item.nombre_preferencia : item.display_name;
                var sub = (item.grado ?? "") + " · " + (item.sede ?? "") + " · " + (item.jornada ?? "");
                var capturedId = item.id;
                var capturedName = label;
                var btn = AddOutlineButton(_studentListContent, label + "\n" + sub, 64, () => OnPickStudent(capturedId, capturedName));
                btn.GetComponent<LayoutElement>().preferredHeight = 64;
            }
        }

        void OnPickStudent(string studentUserId, string displayName)
        {
            if (_busy) return;
            SetBusy(true);
            if (_pickStatus != null)
                _pickStatus.text = EmpathiaText.ForUi("Abriendo sesión de " + displayName + "…");

            StartCoroutine(_api.AssumeStudent(studentUserId, (ok, msg) =>
            {
                SetBusy(false);
                if (!ok)
                {
                    ShowAlertModal("No se pudo abrir la sesión", msg);
                    if (_pickStatus != null)
                        _pickStatus.text = EmpathiaText.ForUi("No se pudo abrir la sesión.");
                    Debug.LogWarning("[Empathia] Assume: " + msg);
                    return;
                }

                Debug.Log("[Empathia] " + msg);
                ShowScreen(UiScreen.Confirm);
            }));
        }

        void OnConfirmEnterHealth()
        {
            var name = !string.IsNullOrWhiteSpace(EmpathiaAuthState.StudentDisplayName)
                ? EmpathiaAuthState.StudentDisplayName
                : (string.IsNullOrWhiteSpace(EmpathiaAuthState.Username)
                    ? (_user != null ? _user.text.Trim() : "usuario")
                    : EmpathiaAuthState.Username);
            if (_welcomeTitle != null)
                _welcomeTitle.text = "¡Bienvenido, " + name + "!";
            if (_welcomeSub != null)
                _welcomeSub.text = "Este es tu espacio de acompañamiento emocional.";
            SetTranscript("(aún no hay mensaje)");
            SetReply("(sin respuesta)");
            SetStatus("Puedes hablar o escribir.");
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
                SetStatus("Listo. Puedes hablar o escribir.");
                yield break;
            }

            Debug.LogWarning("[Empathia] Aún sin sesión B (se reintenta al enviar): " + msg);
            SetStatus("Puedes hablar o escribir.");
        }

        void ApplyLayout()
        {
            if (_uiFromScene)
                return;
            _lastScreen = new Vector2(Screen.width, Screen.height);
            if (_scaler == null) return;

            var aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
            _scaler.matchWidthOrHeight = Mathf.Abs(aspect - (RefW / RefH)) < 0.08f ? 0.5f : (aspect >= 1.4f ? 0.5f : 0.7f);

            PlaceBackground();
            RefreshLoginCardLayout();
            if (_confirmRt != null)
                _confirmRt.sizeDelta = new Vector2(520, 360);
            if (_healthRt != null && _screen != UiScreen.Health)
                _healthRt.sizeDelta = new Vector2(780, 620);
        }

        void PlaceBackground()
        {
            if (_bgRt == null)
                return;

            StretchFull(_bgRt);
        }

        void PlaceLoginCard()
        {
            RefreshLoginCardLayout();
        }

        void SetStudentScrollHeight(float height)
        {
            if (_studentLoginPanel == null)
                return;
            var scroll = _studentLoginPanel.GetComponentInChildren<ScrollRect>(true);
            if (scroll == null)
                return;
            var le = scroll.GetComponent<LayoutElement>();
            if (le == null)
                return;
            le.preferredHeight = height;
            le.minHeight = height;
        }

        void QueueLoginCardFit()
        {
            if (!isActiveAndEnabled || !Application.isPlaying)
                return;
            if (_fitCardCo != null)
                StopCoroutine(_fitCardCo);
            _fitCardCo = StartCoroutine(FitLoginCardNextFrame());
        }

        IEnumerator FitLoginCardNextFrame()
        {
            yield return null;
            RefreshLoginCardLayout();
        }

        float MaxVisibleLoginCardHeight()
        {
            var canvas = _cardRt != null ? _cardRt.GetComponentInParent<Canvas>() : null;
            var canvasRt = canvas != null ? canvas.transform as RectTransform : null;
            var canvasH = canvasRt != null ? canvasRt.rect.height : RefH;
            const float margin = 40f;

            if (_cardRt == null || canvasRt == null)
                return Mathf.Min(LoginCardMaxH, canvasH * 0.52f);

            var corners = new Vector3[4];
            _cardRt.GetWorldCorners(corners);
            Camera cam = null;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            Vector2 topLocal;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt,
                    RectTransformUtility.WorldToScreenPoint(cam, corners[1]),
                    cam,
                    out topLocal))
                return Mathf.Min(LoginCardMaxH, canvasH * 0.52f);

            var canvasBottom = -canvasH * canvasRt.pivot.y;
            return Mathf.Clamp(topLocal.y - canvasBottom - margin, 280f, LoginCardMaxH);
        }

        void FollowCardShadow()
        {
            if (_cardShadowGo == null || _cardRt == null)
                return;
            var shadowRt = _cardShadowGo.GetComponent<RectTransform>();
            if (shadowRt == null)
                return;
            shadowRt.anchorMin = _cardRt.anchorMin;
            shadowRt.anchorMax = _cardRt.anchorMax;
            shadowRt.pivot = _cardRt.pivot;
            shadowRt.sizeDelta = _cardRt.sizeDelta + new Vector2(18f, 18f);
            shadowRt.anchoredPosition = _cardRt.anchoredPosition + new Vector2(0f, -8f);
        }

        void RefreshLoginCardLayout()
        {
            if (_cardRt == null)
                return;

            if (_cardRt.GetComponent<RectMask2D>() == null)
                _cardRt.gameObject.AddComponent<RectMask2D>();

            var fitter = _cardRt.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_cardRt);

            var maxH = MaxVisibleLoginCardHeight();
            var h = Mathf.Clamp(_cardRt.rect.height, 260f, maxH);

            if (fitter != null)
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            if (!_uiFromScene)
            {
                _cardRt.anchorMin = _cardRt.anchorMax = new Vector2(0.5f, LoginCardTop);
                _cardRt.pivot = new Vector2(0.5f, 1f);
                _cardRt.sizeDelta = new Vector2(LoginCardW, h);
                _cardRt.anchoredPosition = Vector2.zero;
            }
            else
            {
                _cardRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            }

            FollowCardShadow();
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
            t.textWrappingMode = TextWrappingModes.Normal;
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
            le.preferredHeight = LoginFieldH;
            le.minHeight = LoginFieldH;
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
            iconRt.sizeDelta = new Vector2(28, 28);
            iconRt.anchoredPosition = new Vector2(32, 0);

            float rightPad = password ? 48f : 14f;

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(fieldGo.transform, false);
            var areaRt = textArea.GetComponent<RectTransform>();
            StretchFull(areaRt);
            areaRt.offsetMin = new Vector2(56, 12);
            areaRt.offsetMax = new Vector2(-rightPad, -12);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textArea.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = GetTmpFont();
            text.fontSize = 22;
            text.color = Navy;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = false;
            StretchFull(text.rectTransform);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            var ph = phGo.GetComponent<TextMeshProUGUI>();
            ph.font = GetTmpFont();
            ph.fontSize = 22;
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
            input.pointSize = 22;
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

        static string SelectedOption(TMP_Dropdown dropdown)
        {
            if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
                return "";
            var index = Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1);
            return (dropdown.options[index].text ?? "").Trim();
        }

        static string[] GradesForCampus(string campus)
        {
            switch (campus)
            {
                case "Sede Principal":
                    return new[] { "6°", "7°", "8°", "9°", "10°", "11°" };
                case "Sede Jhon F. kennedy":
                    return new[] { "Jardín", "Transición", "1°", "Aceleración del Aprendizaje" };
                case "Sede Gustavo Rojas Pinilla":
                    return new[] { "2°", "3°", "4°", "5°" };
                case "Sede Villa Paraguay":
                    return new[] { "Jardín", "Transición", "1°", "2°", "3°", "4°", "5°" };
                default:
                    return new[] { "6°" };
            }
        }

        static void SetDropdownOptions(TMP_Dropdown dropdown, string[] options, string prefer)
        {
            if (dropdown == null || options == null || options.Length == 0)
                return;

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            var index = 0;
            if (!string.IsNullOrEmpty(prefer))
            {
                for (var i = 0; i < options.Length; i++)
                {
                    if (options[i] == prefer)
                    {
                        index = i;
                        break;
                    }
                }
            }

            dropdown.value = index;
            dropdown.RefreshShownValue();

            if (dropdown.template != null)
                dropdown.template.sizeDelta = new Vector2(0, Mathf.Min(LoginDropItemH * options.Length + 16f, 440f));
        }

        TMP_Dropdown AddOptionDropdown(Transform parent, string placeholder, params string[] options)
        {
            var fieldGo = new GameObject(placeholder, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown), typeof(LayoutElement));
            fieldGo.transform.SetParent(parent, false);
            var fieldImg = fieldGo.GetComponent<Image>();
            fieldImg.color = FieldBg;
            ApplyRounded(fieldImg, RoundSprite(128, 28), 1.35f);
            var outline = fieldGo.AddComponent<Outline>();
            outline.effectColor = FieldBorder;
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var le = fieldGo.GetComponent<LayoutElement>();
            le.preferredHeight = LoginFieldH;
            le.minHeight = LoginFieldH;
            le.flexibleWidth = 1f;

            var captionGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            captionGo.transform.SetParent(fieldGo.transform, false);
            var caption = captionGo.GetComponent<TextMeshProUGUI>();
            caption.font = GetTmpFont();
            caption.fontSize = 22;
            caption.color = Navy;
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            caption.raycastTarget = false;
            StretchFull(caption.rectTransform);
            caption.rectTransform.offsetMin = new Vector2(16, 8);
            caption.rectTransform.offsetMax = new Vector2(-36, -8);

            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowGo.transform.SetParent(fieldGo.transform, false);
            var arrow = arrowGo.GetComponent<Image>();
            arrow.sprite = BuildIconSprite("down");
            arrow.color = Muted;
            arrow.preserveAspect = true;
            arrow.raycastTarget = false;
            var arrowRt = arrow.rectTransform;
            arrowRt.anchorMin = new Vector2(1, 0.5f);
            arrowRt.anchorMax = new Vector2(1, 0.5f);
            arrowRt.pivot = new Vector2(1, 0.5f);
            arrowRt.sizeDelta = new Vector2(16, 16);
            arrowRt.anchoredPosition = new Vector2(-16, 0);

            var templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            templateGo.transform.SetParent(fieldGo.transform, false);
            templateGo.GetComponent<LayoutElement>().ignoreLayout = true;
            var templateImg = templateGo.GetComponent<Image>();
            templateImg.color = Color.white;
            ApplyRounded(templateImg, RoundSprite(128, 20), 1.2f);
            var templateRt = templateGo.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0, 0);
            templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot = new Vector2(0.5f, 1f);
            templateRt.sizeDelta = new Vector2(0, 360);
            templateRt.anchoredPosition = new Vector2(0, -4);
            var overlay = templateGo.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 80;
            templateGo.AddComponent<GraphicRaycaster>();

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(templateGo.transform, false);
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            StretchFull(viewportRt);
            viewportRt.offsetMin = new Vector2(4, 4);
            viewportRt.offsetMax = new Vector2(-4, -4);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0, LoginDropItemH);

            var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image));
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemBg = itemGo.GetComponent<Image>();
            itemBg.color = new Color(1f, 1f, 1f, 0.01f);
            var itemRt = itemGo.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 0.5f);
            itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.pivot = new Vector2(0.5f, 0.5f);
            itemRt.sizeDelta = new Vector2(0, LoginDropItemH);

            var checkGo = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(itemGo.transform, false);
            var checkImg = checkGo.GetComponent<Image>();
            checkImg.color = Purple;
            var checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0, 0.5f);
            checkRt.anchorMax = new Vector2(0, 0.5f);
            checkRt.pivot = new Vector2(0.5f, 0.5f);
            checkRt.sizeDelta = new Vector2(8, 8);
            checkRt.anchoredPosition = new Vector2(16, 0);

            var itemLabelGo = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            itemLabelGo.transform.SetParent(itemGo.transform, false);
            var itemLabel = itemLabelGo.GetComponent<TextMeshProUGUI>();
            itemLabel.font = GetTmpFont();
            itemLabel.fontSize = 20;
            itemLabel.color = Navy;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.textWrappingMode = TextWrappingModes.NoWrap;
            itemLabel.overflowMode = TextOverflowModes.Ellipsis;
            itemLabel.raycastTarget = false;
            StretchFull(itemLabel.rectTransform);
            itemLabel.rectTransform.offsetMin = new Vector2(32, 4);
            itemLabel.rectTransform.offsetMax = new Vector2(-10, -4);

            var toggle = itemGo.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg;
            toggle.graphic = checkImg;
            toggle.isOn = true;

            var colors = toggle.colors;
            colors.highlightedColor = new Color(0.93f, 0.90f, 1f, 1f);
            colors.selectedColor = new Color(0.90f, 0.86f, 1f, 1f);
            toggle.colors = colors;

            var scroll = templateGo.GetComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = viewportRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var dropdown = fieldGo.GetComponent<TMP_Dropdown>();
            dropdown.targetGraphic = fieldImg;
            dropdown.template = templateRt;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            templateGo.SetActive(false);
            return dropdown;
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
            btn.onClick.AddListener(TogglePassword);
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
            text.textWrappingMode = TextWrappingModes.NoWrap;
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
            t.fontSize = 20;
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
            ApplyServerFromUi();

            SetBusy(true);
            SetLoginStatus("Autenticando…");
            StartCoroutine(_api.Login(_user.text.Trim(), _pass.text, (ok, msg) =>
            {
                SetBusy(false);
                if (ok)
                {
                    Debug.Log("[Empathia] " + msg);
                    if (EmpathiaAuthState.IsAdultStaff)
                    {
                        ShowStaffLogin(false);
                        SetLoginStatus("Listo. Elige un perfil o regístralo.");
                    }
                    else
                    {
                        // Demo legado: estudiante1 con password
                        SetLoginStatus("Ingreso listo. Confirma para continuar.");
                        ShowScreen(UiScreen.Confirm);
                    }
                }
                else
                {
                    ShowAlertModal("No se pudo iniciar sesión", msg);
                    Debug.Log("[Empathia] ERROR " + msg);
                }
            }));
        }

        IEnumerator RecordAudioTurn()
        {
            if (_busy || _recording) yield break;

            // C tiene whisper=stub → /turns devuelve texto genérico.
            // Flujo correcto: mic → WAV → STT local (tu voz) → POST /active/text a B.
            _stopRecording = false;
            SetRecordButtonUi(true);
            SetState("listening");
            SetTranscript("(habla ahora…)");
            SetReply("(sin respuesta)");
            SetStatus("Grabando… pulsa Detener cuando termines.");

            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                SetRecordButtonUi(false);
                SetBusy(false);
                SetState("idle");
                SetStatus("Sin permiso de micrófono en Unity.");
                yield break;
            }

            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                SetRecordButtonUi(false);
                SetBusy(false);
                SetState("idle");
                SetStatus("Sin micrófono. Ábrelo en Configuración (engranaje) o en Windows → Sonido.");
                yield break;
            }

            _micDevice = ResolveMicDevice();
            Debug.Log("[Empathia] Mic: " + _micDevice);
            _micClip = Microphone.Start(_micDevice, false, MaxMicSeconds, MicSampleRate);
            if (_micClip == null)
            {
                SetRecordButtonUi(false);
                SetBusy(false);
                SetState("idle");
                SetStatus("No se pudo abrir el micrófono.");
                yield break;
            }

            // Esperar a que el mic arranque de verdad
            var micWait = 0f;
            while (!(Microphone.GetPosition(_micDevice) > 0) && micWait < 2f)
            {
                micWait += Time.unscaledDeltaTime;
                yield return null;
            }

            _recording = true;
            _busy = false;
            if (_recordBtn != null) _recordBtn.interactable = true;

            var started = Time.realtimeSinceStartup;
            var peak = 0f;
            var samplesBuf = new float[256];
            while (!_stopRecording && (Time.realtimeSinceStartup - started) < MaxMicSeconds - 0.25f)
            {
                var pos = Microphone.GetPosition(_micDevice);
                if (pos > samplesBuf.Length && _micClip != null)
                {
                    var startRead = Mathf.Max(0, pos - samplesBuf.Length);
                    if (_micClip.GetData(samplesBuf, startRead))
                    {
                        for (var i = 0; i < samplesBuf.Length; i++)
                        {
                            var a = Mathf.Abs(samplesBuf[i]);
                            if (a > peak) peak = a;
                        }
                    }
                }

                var secs = Time.realtimeSinceStartup - started;
                var pct = Mathf.Clamp01(peak * 4f);
                SetStatus("Grabando… " + secs.ToString("0") + " s · nivel " + (pct * 100f).ToString("0") + "% — Detener");
                SetTranscript(pct < 0.02f
                    ? "(sin señal de mic — habla más fuerte)"
                    : "(capturando audio… " + secs.ToString("0") + " s)");
                yield return null;
            }

            var samples = Microphone.GetPosition(_micDevice);
            Microphone.End(_micDevice);
            _recording = false;
            _stopRecording = false;
            SetRecordButtonUi(false);

            if (samples < MicSampleRate / 5)
            {
                SetBusy(false);
                SetState("idle");
                SetTranscript("(sin texto)");
                SetStatus("Audio demasiado corto. Graba al menos ~1 s.");
                yield break;
            }

            if (peak < 0.01f)
            {
                SetBusy(false);
                SetState("idle");
                SetTranscript("(sin texto)");
                SetStatus("El mic no captó voz (nivel ~0). Revisa micrófono / permisos Windows.");
                Debug.LogWarning("[Empathia] Mic peak demasiado bajo: " + peak);
                yield break;
            }

            var wav = EmpathiaWav.FromMicrophoneClip(_micClip, samples, MicSampleRate);
            _micClip = null;
            Debug.Log("[Empathia] WAV bytes=" + wav.Length + " samples=" + samples + " peak=" + peak.ToString("0.000"));

            SetBusy(true);
            SetState("processing");
            SetStatus("Pasando tu voz a texto…");
            SetTranscript("(convirtiendo tu voz…)");

            var sttOk = false;
            string spoken = null;
            string sttErr = null;
            yield return EmpathiaWavStt.TranscribeWav(wav, (ok, text, err) =>
            {
                sttOk = ok;
                spoken = text;
                sttErr = err;
            });

            if (!sttOk || string.IsNullOrWhiteSpace(spoken))
            {
                SetBusy(false);
                SetState("idle");
                SetTranscript("(sin texto)");
                SetStatus("No pude sacar texto de tu audio: " + (sttErr ?? "error"));
                Debug.LogWarning("[Empathia] STT local falló: " + sttErr);
                yield break;
            }

            ApplySpokenText(spoken);
            Debug.Log("[Empathia] Texto de TU audio: " + spoken);
            SetStatus("Enviando tu mensaje…");
            yield return EnsureSessionAndPostText(spoken);
        }

        void ApplySpokenText(string spoken)
        {
            SetTranscript(spoken);
            if (_typedMessage != null)
                _typedMessage.text = spoken;
        }

        IEnumerator EnsureSessionAndPostText(string message)
        {
            SetBusy(true);
            SetState("processing");

            if (!EmpathiaAuthState.HasSession)
            {
                SetStatus("Creando la conversación…");
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

            SetStatus("Enviando tu mensaje…");
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
                SetStatus("No se pudo enviar el mensaje: " + sendMsg);
                Debug.LogWarning("[Empathia] Falló POST /active/text: " + sendMsg);
                yield break;
            }

            var transcript = parsed != null && !string.IsNullOrWhiteSpace(parsed.transcript)
                ? parsed.transcript.Trim()
                : message;
            SetTranscript(transcript);
            if (_typedMessage != null && !string.IsNullOrWhiteSpace(message))
                _typedMessage.text = message;

            // Respuesta inmediata del POST (puede ser placeholder). Luego poll /events → turn.result.
            var immediateReply = parsed != null && !string.IsNullOrWhiteSpace(parsed.reply_text)
                ? parsed.reply_text.Trim()
                : null;
            if (!string.IsNullOrWhiteSpace(immediateReply))
                SetReply(immediateReply);

            var turnId = parsed != null && parsed.turn != null ? parsed.turn.id : null;
            if (string.IsNullOrEmpty(EmpathiaAuthState.SessionId)
                && parsed != null
                && !string.IsNullOrEmpty(parsed.session_id))
            {
                EmpathiaAuthState.SessionId = parsed.session_id;
            }

            if (!EmpathiaAuthState.HasSession)
            {
                SetBusy(false);
                SetState("idle");
                SetStatus("Mensaje enviado. Esperando respuesta…");
                yield break;
            }

            SetStatus("EmpathIA está pensando…");
            TurnResultInfo turn = null;
            var pollOk = false;
            var pollMsg = "";
            yield return _api.PollTurnResult(
                turnId,
                status => SetStatus(status),
                (ok, info, msg) =>
                {
                    pollOk = ok;
                    turn = info;
                    pollMsg = msg;
                });

            if (pollOk && turn != null && !turn.IsError)
            {
                if (!string.IsNullOrWhiteSpace(turn.Transcript))
                    SetTranscript(turn.Transcript.Trim());
                var reply = !string.IsNullOrWhiteSpace(turn.ReplyText)
                    ? turn.ReplyText.Trim()
                    : (immediateReply ?? "(sin reply_text)");
                SetReply(reply);
                Debug.Log("[Empathia] turn.result reply: " + reply);
                SetStatus("Reproduciendo la voz…");
                yield return PlayTurnTts(turn);
            }
            else if (!string.IsNullOrWhiteSpace(immediateReply))
            {
                // B antiguo: solo reply en el POST, sin evento.
                SetReply(immediateReply);
                Debug.LogWarning("[Empathia] Sin turn.result (" + pollMsg + "). Uso reply del POST.");
                SetStatus("Llegó el texto, pero no el evento de voz.");
                SetBusy(false);
                SetState("idle");
                yield break;
            }
            else
            {
                var detail = turn != null && turn.IsError
                    ? EmpathiaApiClient.MapTurnError(turn.ErrorCode, turn.ErrorMessage)
                    : (string.IsNullOrWhiteSpace(pollMsg) ? "sin turn.result" : pollMsg);
                SetReply("(sin respuesta)");
                SetStatus(detail);
                Debug.LogWarning("[Empathia] " + detail);
                ShowAlertModal("No se pudo responder", detail);
                SetBusy(false);
                SetState("idle");
                yield break;
            }

            SetBusy(false);
            SetState("idle");
        }

        /// <summary>
        /// Tras turn.result: estado speaking + descarga/reproducción de TTS (Bearer).
        /// </summary>
        IEnumerator PlayTurnTts(TurnResultInfo turn)
        {
            if (turn == null)
                yield break;

            var ttsUrl = turn.TtsUrl;
            if (string.IsNullOrWhiteSpace(ttsUrl) && !string.IsNullOrWhiteSpace(turn.TurnId))
                ttsUrl = EmpathiaApiClient.BuildTtsUrl(turn.TurnId, null);

            if (_audio == null)
                _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            SetState("speaking");
            ApplyReplyEmotion(turn.ReplyText);
            if (_mouth != null)
                _mouth.StartSpeaking(turn.Expression, turn.ReplyText);

            if (string.IsNullOrWhiteSpace(ttsUrl))
            {
                SetStatus("Hay texto, pero no llegó el audio.");
                yield return SpeakMouthOnly(2.4f);
                yield break;
            }

            SetStatus("Descargando la voz…");

            var playOk = false;
            var playMsg = "";
            yield return _api.DownloadAndPlayTts(ttsUrl, _audio, (ok, msg) =>
            {
                playOk = ok;
                playMsg = msg;
            });

            if (!playOk)
            {
                Debug.LogWarning("[Empathia] TTS de B no sonó: " + playMsg);
                SetStatus("Sin archivo de voz. Leyendo el texto en voz alta…");
                var localOk = false;
                var localMsg = "";
                yield return EmpathiaLocalTts.Speak(turn.ReplyText, _audio, (ok, msg) =>
                {
                    localOk = ok;
                    localMsg = msg;
                });
                if (!localOk)
                {
                    SetStatus("Texto OK. Sin voz: " + playMsg);
                    ShowAlertModal("Sin voz", playMsg + " " + localMsg);
                    yield return SpeakMouthOnly(2.4f);
                    yield break;
                }

                playMsg = localMsg;
            }

            SetStatus("Reproduciendo…");
            if (_mouth != null && _audio != null && _audio.clip != null)
                _mouth.SetSpeechWindow(_audio.clip);
            NotifyBodySpeaking(true);
            var waited = 0f;
            while (_audio != null && _audio.isPlaying && waited < 60f)
            {
                waited += Time.unscaledDeltaTime;
                if (_mouth != null)
                    _mouth.Tick(_audio.time);
                yield return null;
            }

            if (_mouth != null)
                _mouth.Stop();
            NotifyBodySpeaking(false);
            ApplyReplyEmotion(null);
            SetStatus("Listo.");
        }

        IEnumerator SpeakMouthOnly(float seconds)
        {
            var waited = 0f;
            while (waited < seconds)
            {
                waited += Time.unscaledDeltaTime;
                if (_mouth != null)
                    _mouth.Tick(waited);
                yield return null;
            }

            if (_mouth != null)
                _mouth.Stop();
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
            if (_refreshListBtn != null) _refreshListBtn.interactable = !busy;
            if (_registerBtn != null) _registerBtn.interactable = !busy;
            if (_createStudentBtn != null) _createStudentBtn.interactable = !busy;
            if (_backToLoginBtn != null) _backToLoginBtn.interactable = !busy;
            if (_regCampus != null) _regCampus.interactable = !busy;
            if (_regGrade != null) _regGrade.interactable = !busy;
            if (_regShift != null) _regShift.interactable = !busy;
            if (_checkBBtn != null) _checkBBtn.interactable = !busy;
            if (_settingsSaveBtn != null) _settingsSaveBtn.interactable = !busy;
            if (_confirmBtn != null) _confirmBtn.interactable = !busy;
            if (_logoutBtn != null) _logoutBtn.interactable = !busy;
            if (_reportBackBtn != null) _reportBackBtn.interactable = !busy;
            // Durante grabación el botón debe seguir activo para el 2.º toque
            if (_recordBtn != null)
                _recordBtn.interactable = _recording || !busy;
            if (_sendTextBtn != null)
                _sendTextBtn.interactable = !busy;
        }

        void SetState(string s)
        {
            if (_mouth != null && s != "speaking")
                _mouth.Stop();
        }

        void SetStatus(string s)
        {
            if (_status != null)
                _status.text = EmpathiaText.ForUi(s);
        }

        void SetLoginStatus(string s)
        {
            if (_loginStatus != null)
                _loginStatus.text = EmpathiaText.ForUi(s ?? "");
        }

        void SetSettingsStatus(string s)
        {
            if (_settingsStatus != null)
                _settingsStatus.text = EmpathiaText.ForUi(s ?? "");
        }

        void ApplyServerFromUi()
        {
            EmpathiaAuthState.BaseUrl = _baseUrl != null && !string.IsNullOrWhiteSpace(_baseUrl.text)
                ? _baseUrl.text.Trim()
                : (string.IsNullOrWhiteSpace(EmpathiaAuthState.BaseUrl)
                    ? "http://127.0.0.1:8000/api/v1"
                    : EmpathiaAuthState.BaseUrl);
        }

        void ApplyMicFromUi()
        {
            if (_micDropdown == null || _micDropdown.options == null || _micDropdown.options.Count == 0)
                return;
            if (_micDropdown.value <= 0)
            {
                EmpathiaAuthState.MicDevice = "";
                return;
            }

            EmpathiaAuthState.MicDevice = SelectedOption(_micDropdown);
        }

        static string ResolveMicDevice()
        {
            var devices = Microphone.devices;
            if (devices == null || devices.Length == 0)
                return null;

            var preferred = EmpathiaAuthState.MicDevice;
            if (!string.IsNullOrEmpty(preferred))
            {
                for (var i = 0; i < devices.Length; i++)
                {
                    if (devices[i] == preferred)
                        return devices[i];
                }
            }

            return devices[0];
        }

        void FillMicDropdown()
        {
            var names = new List<string> { "Micrófono predeterminado" };
            var devices = Microphone.devices;
            if (devices != null)
            {
                for (var i = 0; i < devices.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(devices[i]))
                        names.Add(devices[i]);
                }
            }

            var prefer = "Micrófono predeterminado";
            if (!string.IsNullOrWhiteSpace(EmpathiaAuthState.MicDevice)
                && names.Contains(EmpathiaAuthState.MicDevice))
                prefer = EmpathiaAuthState.MicDevice;
            SetDropdownOptions(_micDropdown, names.ToArray(), prefer);
        }

        void BuildSettingsGear(Transform canvas)
        {
            var go = new GameObject("SettingsGear", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas, false);
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            ApplyRounded(img, RoundSprite(128, 48), 1.2f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(56f, 56f);
            rt.anchoredPosition = new Vector2(-28f, -24f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = BuildIconSprite("gear");
            icon.color = Navy;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(28f, 28f);

            _settingsGearBtn = go.GetComponent<Button>();
            _settingsGearBtn.targetGraphic = img;
            _settingsGearBtn.onClick.AddListener(ToggleSettings);
        }

        void BuildSettingsView(Transform canvas)
        {
            _settingsView = new GameObject("SettingsView", typeof(RectTransform));
            _settingsView.transform.SetParent(canvas, false);
            StretchFull(_settingsView.GetComponent<RectTransform>());

            var dim = CreateImage(_settingsView.transform, "Dim", new Color(0.08f, 0.07f, 0.14f, 0.48f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            var card = CreateImage(_settingsView.transform, "SettingsCard", Color.white);
            ApplyRounded(card, RoundSprite(256, 48), 1.05f);
            var cardRt = card.rectTransform;
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(560f, 520f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(cardRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            StretchFull(contentRt);
            contentRt.offsetMin = new Vector2(36, 28);
            contentRt.offsetMax = new Vector2(-36, -28);
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.spacing = 12;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            AddLabel(content.transform, "Configuración", 26, FontStyles.Bold, Navy, 40, TextAlignmentOptions.Center);
            AddLabel(content.transform, "Servidor y micrófono de este computador.", 14, FontStyles.Normal, Muted, 36, TextAlignmentOptions.Center);
            _baseUrl = AddIconInput(content.transform, "Dirección del servidor", EmpathiaAuthState.BaseUrl, "lock", false);
            _micDropdown = AddOptionDropdown(content.transform, "Micrófono", "Micrófono predeterminado");
            _checkBBtn = AddOutlineButton(content.transform, "Probar conexión", 50, OnCheckConnectionB);
            _settingsStatus = AddLabel(content.transform, "", 14, FontStyles.Normal, Muted, 28, TextAlignmentOptions.Center);
            _settingsSaveBtn = AddGradientButton(content.transform, "Guardar", 56, OnSaveSettings, 20f);
            _settingsCloseBtn = AddOutlineButton(content.transform, "Cerrar", 50, () => ShowSettings(false));
            _settingsView.SetActive(false);
        }

        void ToggleSettings()
        {
            var open = _settingsView != null && _settingsView.activeSelf;
            ShowSettings(!open);
        }

        void ShowSettings(bool show)
        {
            if (_settingsView == null)
                return;
            if (show)
            {
                if (_baseUrl != null)
                    _baseUrl.text = EmpathiaAuthState.BaseUrl ?? "";
                FillMicDropdown();
                SetSettingsStatus("");
                _settingsView.SetActive(true);
                _settingsView.transform.SetAsLastSibling();
                if (_settingsGearBtn != null)
                    _settingsGearBtn.transform.SetAsLastSibling();
                if (_alertModal != null && _alertModal.activeSelf)
                    _alertModal.transform.SetAsLastSibling();
            }
            else
            {
                _settingsView.SetActive(false);
            }
        }

        void OnSaveSettings()
        {
            ApplyServerFromUi();
            ApplyMicFromUi();
            EmpathiaAuthState.PersistSettings();
            SetSettingsStatus("Guardado.");
            ShowSettings(false);
        }

        void OnEndConversation()
        {
            if (_busy || _recording)
                return;
            StartCoroutine(EndConversationThenReport());
        }

        IEnumerator EndConversationThenReport()
        {
            SetBusy(true);
            if (_mouth != null)
                _mouth.Stop();
            if (_audio != null)
                _audio.Stop();
            SetStatus("Cerrando la conversación…");

            var sessionId = EmpathiaAuthState.SessionId;
            if (string.IsNullOrEmpty(sessionId))
                sessionId = EmpathiaAuthState.SavedSessionId;

            if (!string.IsNullOrEmpty(sessionId))
            {
                yield return _api.CloseSessionById(sessionId, (ok, msg) =>
                {
                    if (!ok)
                        Debug.LogWarning("[Empathia] Cerrar charla: " + msg);
                });
            }

            SessionSummaryDto summary = null;
            if (!string.IsNullOrEmpty(sessionId))
            {
                yield return _api.FetchSessionSummary(sessionId, (ok, msg, data) =>
                {
                    if (!ok)
                        Debug.LogWarning("[Empathia] Resumen: " + msg);
                    summary = data;
                });
            }

            SetBusy(false);
            FillReport(summary);
            ShowScreen(UiScreen.Report);
        }

        void FillReport(SessionSummaryDto summary)
        {
            EnsureReportView();
            var name = summary != null && !string.IsNullOrWhiteSpace(summary.student_name)
                ? summary.student_name
                : EmpathiaAuthState.StudentDisplayName;
            if (_reportTitle != null)
            {
                _reportTitle.text = string.IsNullOrWhiteSpace(name)
                    ? "Reporte de esta charla"
                    : "Reporte de " + name;
            }

            if (_reportMeta != null)
            {
                if (summary == null)
                {
                    _reportMeta.text = "No hubo una charla guardada esta vez.";
                }
                else
                {
                    var risk = summary.risk_count > 0
                        ? "Señales de alerta: " + summary.risk_count
                        : "Sin señales de alerta.";
                    _reportMeta.text = "Turnos: " + summary.turn_count
                        + "  ·  " + FormatEmotions(summary.emotion_counts)
                        + "\n" + risk;
                }
            }

            if (_reportBody != null)
            {
                var raw = summary != null ? summary.conversation_summary : null;
                _reportBody.text = string.IsNullOrWhiteSpace(raw)
                    ? "Esta conversación no dejó un resumen. Si hablaron, revisa que B esté encendido."
                    : FormatSummaryForUi(raw);
            }

            SetButtonLabel(
                _reportBackBtn,
                !string.IsNullOrEmpty(EmpathiaAuthState.AdultToken)
                    ? "Volver a la lista"
                    : "Volver al inicio");
        }

        static string FormatEmotions(EmotionCountDto[] counts)
        {
            if (counts == null || counts.Length == 0)
                return "Sin etiquetas de emoción.";

            var parts = new List<string>();
            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] == null || string.IsNullOrWhiteSpace(counts[i].label))
                    continue;
                parts.Add(EmotionLabelEs(counts[i].label) + " (" + counts[i].count + ")");
            }

            return parts.Count == 0
                ? "Sin etiquetas de emoción."
                : "Emociones: " + string.Join(", ", parts.ToArray());
        }

        static string EmotionLabelEs(string label)
        {
            switch ((label ?? "").Trim().ToLowerInvariant())
            {
                case "sadness":
                case "sad":
                    return "tristeza";
                case "happiness":
                case "joy":
                case "happy":
                    return "alegría";
                case "anger":
                case "angry":
                    return "enojo";
                case "fear":
                    return "miedo";
                case "surprise":
                    return "sorpresa";
                case "disgust":
                    return "disgusto";
                case "neutral":
                    return "calma";
                default:
                    return label;
            }
        }

        static string FormatSummaryForUi(string raw)
        {
            var lines = raw.Replace("\r\n", "\n").Split('\n');
            var blocks = new List<string>();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                var pretty = line
                    .Replace(" | ", "\n")
                    .Replace("Usuario:", "Estudiante:")
                    .Replace("IA:", "EmpathIA:");
                var open = pretty.IndexOf('[');
                var close = pretty.IndexOf(']');
                if (open >= 0 && close > open)
                {
                    var label = pretty.Substring(open + 1, close - open - 1);
                    pretty = pretty.Substring(0, open)
                        + "· "
                        + EmotionLabelEs(label)
                        + pretty.Substring(close + 1);
                }
                blocks.Add(pretty.Trim());
            }

            return blocks.Count == 0 ? raw : string.Join("\n\n", blocks.ToArray());
        }

        void OnReportBack()
        {
            if (_busy)
                return;

            if (EmpathiaAuthState.TryRestoreAdult())
            {
                ShowSettings(false);
                ShowScreen(UiScreen.Login);
                ShowStaffLogin(false);
                SetLoginStatus("Elige el siguiente estudiante.");
                return;
            }

            OnLogout();
        }

        void OnLogout()
        {
            if (_busy || _recording)
                return;
            EmpathiaAuthState.ClearAll();
            ShowSettings(false);
            ShowStaffLogin(true);
            SetLoginStatus("Inicia sesión del psicoorientador.");
            ShowScreen(UiScreen.Login);
        }

        void BuildAlertModal(Transform canvas)
        {
            _alertModal = new GameObject("AlertModal", typeof(RectTransform));
            _alertModal.transform.SetParent(canvas, false);
            StretchFull(_alertModal.GetComponent<RectTransform>());

            var dim = CreateImage(_alertModal.transform, "Dim", new Color(0.08f, 0.07f, 0.14f, 0.48f));
            StretchFull(dim.rectTransform);
            dim.raycastTarget = true;

            var card = CreateImage(_alertModal.transform, "AlertCard", Color.white);
            ApplyRounded(card, RoundSprite(256, 48), 1.05f);
            var cardRt = card.rectTransform;
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(560, 340);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(cardRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            StretchFull(contentRt);
            contentRt.offsetMin = new Vector2(36, 28);
            contentRt.offsetMax = new Vector2(-36, -28);
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.spacing = 14;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            _alertTitle = AddLabel(content.transform, "Aviso", 26, FontStyles.Bold, Navy, 40, TextAlignmentOptions.Center);
            _alertBody = AddLabel(content.transform, "", 17, FontStyles.Normal, Muted, 160, TextAlignmentOptions.Center);
            _alertCloseBtn = AddGradientButton(content.transform, "Cerrar", 58, HideAlertModal, 20f);
            _alertModal.SetActive(false);
        }

        void ShowAlertModal(string title, string message)
        {
            if (_alertModal == null)
                return;
            if (_alertTitle != null)
                _alertTitle.text = EmpathiaText.ForUi(title ?? "Aviso");
            if (_alertBody != null)
                _alertBody.text = EmpathiaText.ForUi(CleanAlertMessage(message));
            _alertModal.SetActive(true);
            _alertModal.transform.SetAsLastSibling();
            SetLoginStatus("");
        }

        void HideAlertModal()
        {
            if (_alertModal != null)
                _alertModal.SetActive(false);
        }

        static string CleanAlertMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Ocurrió un problema. Intenta de nuevo.";

            var text = message.Trim();
            if (text.StartsWith("Error:", System.StringComparison.OrdinalIgnoreCase))
                text = text.Substring(6).Trim();

            if (text.IndexOf('{') >= 0 || text.IndexOf("\"status\"") >= 0)
            {
                if (text.IndexOf("timed out", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("Cannot connect", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("Connection", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "No hay conexión con el servidor. Revisa que B esté encendido.";
                return "No se pudo completar la acción. Intenta de nuevo.";
            }

            if (text.Length > 220)
                return text.Substring(0, 217) + "…";
            return text;
        }

        void SetReply(string s)
        {
            if (_reply != null)
                _reply.text = EmpathiaText.ForUi(s);
        }

        void SetTranscript(string s)
        {
            if (_transcript != null)
                _transcript.text = EmpathiaText.ForUi(s);
        }
    }
}
