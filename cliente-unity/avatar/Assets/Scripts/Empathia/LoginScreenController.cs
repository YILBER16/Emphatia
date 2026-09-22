using System;
using System.Collections;
using System.Collections.Generic;
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
        const int MaxMicSeconds = 60;
        const int MicSampleRate = 16000;
        const float RefW = 1920f;
        const float RefH = 1080f;
        const float LoginCardW = 760f;
        const float LoginCardTop = 0.74f;
        const float LoginCardBottom = 0.03f;
        const float LoginFieldH = 70f;
        const float LoginDropItemH = 64f;

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
        TMP_FontAsset _tmpFont;
        Sprite _roundLg;
        Sprite _roundSm;
        Sprite _roundPill;
        Sprite _gradBtn;

        RectTransform _rootRt;
        RectTransform _cardRt;
        RectTransform _bgRt;
        RectTransform _labRt;
        RectTransform _confirmRt;
        RectTransform _healthRt;
        CanvasScaler _scaler;

        GameObject _loginView;
        GameObject _confirmView;
        GameObject _healthView;
        GameObject _pickStudentView;
        GameObject _cardShadowGo;
        Transform _studentListContent;
        TextMeshProUGUI _pickStatus;

        TMP_InputField _baseUrl;
        TMP_InputField _user;
        TMP_InputField _pass;
        TMP_InputField _regName;
        TMP_InputField _regDoc;
        TMP_Dropdown _regCampus;
        TMP_Dropdown _regGrade;
        TMP_Dropdown _regShift;
        GameObject _studentLoginPanel;
        GameObject _registerPanel;
        GameObject _adultLoginPanel;
        Transform _loginListContent;
        Button _refreshListBtn;
        StudentListItem[] _directoryStudents;
        Button _createStudentBtn;
        Button _backToLoginBtn;
        TMP_InputField _typedMessage;
        TextMeshProUGUI _status;
        TextMeshProUGUI _state;
        TextMeshProUGUI _reply;
        TextMeshProUGUI _transcript;
        TextMeshProUGUI _loginStatus;
        TextMeshProUGUI _loginHint;
        GameObject _alertModal;
        TextMeshProUGUI _alertTitle;
        TextMeshProUGUI _alertBody;
        Button _alertCloseBtn;
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
        enum UiScreen { Login, Confirm, Health, PickStudent }
        UiScreen _screen = UiScreen.Login;

        void Awake()
        {
            try
            {
                _api = GetComponent<EmpathiaApiClient>() ?? gameObject.AddComponent<EmpathiaApiClient>();
                _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
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
            PlaceLoginCard();

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(_cardRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            StretchFull(contentRt);
            contentRt.offsetMin = new Vector2(44, 28);
            contentRt.offsetMax = new Vector2(-44, -28);
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.spacing = 16;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.padding = new RectOffset(0, 0, 4, 0);

            _baseUrl = AddCompactInput(content.transform, "Servidor", EmpathiaAuthState.BaseUrl);

            _studentLoginPanel = CreateLoginPanel(content.transform, "StudentFields");
            AddLabel(_studentLoginPanel.transform, "Elige tu nombre", 20, FontStyles.Bold, Navy, 28, TextAlignmentOptions.Center);
            _loginListContent = CreateScrollList(_studentLoginPanel.transform, 260);
            _refreshListBtn = AddOutlineButton(_studentLoginPanel.transform, "Actualizar lista", 50, () => StartCoroutine(LoadDirectoryList()));

            _registerPanel = CreateLoginPanel(content.transform, "RegisterFields");
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
            _backToLoginBtn = AddOutlineButton(_registerPanel.transform, "Volver", 54, () => ShowRegisterForm(false));
            _registerPanel.SetActive(false);

            _adultLoginPanel = CreateLoginPanel(content.transform, "AdultFields");
            _user = AddIconInput(_adultLoginPanel.transform, "Usuario del psicoorientador", "orientador1", "user", false);
            _pass = AddIconInput(_adultLoginPanel.transform, "Contraseña", "password", "lock", true);
            _checkBBtn = AddOutlineButton(_adultLoginPanel.transform, "Probar conexión B", 48, OnCheckConnectionB);
            _loginBtn = AddGradientButton(_adultLoginPanel.transform, "Iniciar sesión", 58, OnLogin);

            _registerBtn = AddOutlineButton(content.transform, "Registrarse", 62, OnRegister);
            _loginHint = AddLabel(content.transform, "La lista muestra solo el nombre. Al entrar usa todos los datos del perfil.", 16, FontStyles.Normal, Muted, 36, TextAlignmentOptions.Center);
            _loginStatus = AddLabel(content.transform, "", 14, FontStyles.Normal, Muted, 24, TextAlignmentOptions.Center);
            ShowStaffLogin(true);
        }

        GameObject CreateLoginPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.spacing = 14;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            go.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
            EmpathiaAuthState.BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.text)
                ? "http://127.0.0.1:8000/api/v1"
                : _baseUrl.text.Trim();

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

            EmpathiaAuthState.BaseUrl = _baseUrl != null && !string.IsNullOrWhiteSpace(_baseUrl.text)
                ? _baseUrl.text.Trim()
                : EmpathiaAuthState.BaseUrl;

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
            EmpathiaAuthState.BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.text)
                ? "http://127.0.0.1:8000/api/v1"
                : _baseUrl.text.Trim();
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
            if (ok)
            {
                Debug.Log("[Empathia] " + msg);
                if (!silent && !string.IsNullOrEmpty(EmpathiaAuthState.AdultToken))
                    yield return LoadDirectoryList();
                else if (!silent)
                    SetLoginStatus("Conexión OK. Inicia sesión del psicoorientador.");
            }
            else
            {
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
            AddLabel(content.transform, "Perfiles activos creados por el admin en B.", 14, FontStyles.Normal, Muted, 24, TextAlignmentOptions.Center);

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
            AddOutlineButton(content.transform, "Actualizar lista", 48, () => StartCoroutine(LoadStudentList()));
            AddOutlineButton(content.transform, "Volver", 48, () =>
            {
                EmpathiaAuthState.ClearAll();
                ShowScreen(UiScreen.Login);
            });
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
            _recordHint = AddLabel(content.transform, "Grabar = tu voz → texto local → B", 14, FontStyles.Normal, Muted, 24, TextAlignmentOptions.Center);

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
                    ? "Grabando tu mic… Detener transcribe en local"
                    : "Grabar = tu voz → texto local → B";
        }

        void ShowScreen(UiScreen screen)
        {
            _screen = screen;
            HideAlertModal();
            if (_loginView != null) _loginView.SetActive(screen == UiScreen.Login);
            if (_pickStudentView != null) _pickStudentView.SetActive(screen == UiScreen.PickStudent);
            if (_confirmView != null) _confirmView.SetActive(screen == UiScreen.Confirm);
            if (_healthView != null) _healthView.SetActive(screen == UiScreen.Health);
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
                _welcomeSub.text = !string.IsNullOrEmpty(EmpathiaAuthState.AdultToken)
                    ? "Adulto eligió estudiante → sesión B."
                    : "Login + sesión + audio/texto a B.";
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

            PlaceBackground();
            PlaceLoginCard();
            if (_confirmRt != null)
                _confirmRt.sizeDelta = new Vector2(520, 360);
            if (_healthRt != null)
                _healthRt.sizeDelta = new Vector2(780, 500);
        }

        void PlaceBackground()
        {
            if (_bgRt == null)
                return;

            StretchFull(_bgRt);
        }

        void PlaceLoginCard()
        {
            if (_cardRt != null)
            {
                _cardRt.anchorMin = new Vector2(0.5f, LoginCardBottom);
                _cardRt.anchorMax = new Vector2(0.5f, LoginCardTop);
                _cardRt.pivot = new Vector2(0.5f, 0.5f);
                _cardRt.sizeDelta = new Vector2(LoginCardW, 0);
                _cardRt.anchoredPosition = Vector2.zero;
            }

            if (_cardShadowGo == null)
                return;

            var shadowRt = _cardShadowGo.GetComponent<RectTransform>();
            if (shadowRt == null)
                return;

            shadowRt.anchorMin = new Vector2(0.5f, LoginCardBottom);
            shadowRt.anchorMax = new Vector2(0.5f, LoginCardTop);
            shadowRt.pivot = new Vector2(0.5f, 0.5f);
            shadowRt.sizeDelta = new Vector2(LoginCardW + 16f, 0);
            shadowRt.anchoredPosition = new Vector2(0, -6);
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
            text.fontSize = 20;
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
            ph.fontSize = 20;
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
            input.pointSize = 20;
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
            caption.fontSize = 20;
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
            EmpathiaAuthState.BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.text)
                ? "http://127.0.0.1:8000/api/v1"
                : _baseUrl.text.Trim();

            SetBusy(true);
            SetLoginStatus("Autenticando contra B…");
            StartCoroutine(_api.Login(_user.text.Trim(), _pass.text, (ok, msg) =>
            {
                SetBusy(false);
                if (ok)
                {
                    Debug.Log("[Empathia] " + msg);
                    if (EmpathiaAuthState.IsAdultStaff)
                    {
                        ShowStaffLogin(false);
                        SetLoginStatus("Sesión lista. Elige un perfil o regístralo.");
                    }
                    else
                    {
                        // Demo legado: estudiante1 con password
                        SetLoginStatus("Login OK (demo estudiante). Confirma para continuar.");
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
            SetStatus("Grabando tu voz… Detener = transcribir en local");

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
                SetStatus("Sin micrófono. Revisa Windows → Sonido → Entrada.");
                yield break;
            }

            _micDevice = Microphone.devices[0];
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
            SetStatus("Transcribiendo TU audio en local…");
            SetTranscript("(convirtiendo tu voz a texto…)");

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
            SetStatus("Texto listo. Enviando a B (/active/text)…");
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
                SetStatus("Texto enviado, pero sin session.id para leer /events.");
                yield break;
            }

            SetStatus("Esperando turn.result en /events…");
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
                SetStatus("Respuesta de B/C lista. Reproduciendo TTS…");
                yield return PlayTurnTts(turn);
            }
            else if (!string.IsNullOrWhiteSpace(immediateReply))
            {
                // B antiguo: solo reply en el POST, sin evento.
                SetReply(immediateReply);
                Debug.LogWarning("[Empathia] Sin turn.result (" + pollMsg + "). Uso reply del POST.");
                SetStatus("Texto OK. Respuesta del POST (sin evento). " + pollMsg);
                SetBusy(false);
                SetState("idle");
                yield break;
            }
            else
            {
                SetReply("(sin respuesta)");
                SetStatus("Texto enviado, pero sin turn.result: " + pollMsg);
                Debug.LogWarning("[Empathia] " + pollMsg);
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

            if (string.IsNullOrWhiteSpace(ttsUrl))
            {
                SetStatus("Sin URL de TTS en turn.result (texto OK).");
                yield break;
            }

            if (_audio == null)
                _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            SetState("speaking");
            SetStatus("Descargando TTS…");

            var playOk = false;
            var playMsg = "";
            yield return _api.DownloadAndPlayTts(ttsUrl, _audio, (ok, msg) =>
            {
                playOk = ok;
                playMsg = msg;
            });

            if (!playOk)
            {
                SetStatus("Texto OK. TTS no sonó: " + playMsg);
                Debug.LogWarning("[Empathia] TTS: " + playMsg);
                yield break;
            }

            SetStatus("Speaking… " + playMsg);
            // Esperar a que termine el clip (o un tope de seguridad).
            var waited = 0f;
            while (_audio != null && _audio.isPlaying && waited < 60f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            SetStatus("Turno completo: texto + TTS.");
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
                _state.text = EmpathiaText.ForUi("Estado UI: " + s);
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
                _reply.text = EmpathiaText.ForUi("Respuesta EmpathIA: " + s);
        }

        void SetTranscript(string s)
        {
            if (_transcript != null)
                _transcript.text = EmpathiaText.ForUi("Tu texto: " + s);
        }
    }
}
