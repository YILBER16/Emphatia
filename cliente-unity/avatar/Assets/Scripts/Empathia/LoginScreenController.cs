using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Empathia
{
    /// <summary>
    /// Pantalla Sprint 1: login → token parcial → crear/cerrar sesión.
    /// Se monta sola al cargar la escena (no hace falta cablear en el editor).
    /// </summary>
    public class LoginScreenController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<LoginScreenController>() != null)
                return;

            var go = new GameObject("EmpathiaLogin");
            go.AddComponent<EmpathiaApiClient>();
            go.AddComponent<LoginScreenController>();
            DontDestroyOnLoad(go);
        }

        EmpathiaApiClient _api;
        InputField _baseUrl;
        InputField _user;
        InputField _pass;
        Text _status;
        Text _stateLabel;
        Button _loginBtn;
        Button _sessionBtn;
        Button _closeBtn;
        bool _busy;

        void Awake()
        {
            _api = GetComponent<EmpathiaApiClient>();
            EnsureEventSystem();
            BuildUi();
            SetUiState("idle");
            SetStatus("Listo. Solo hablo con B en :8000 (nunca :8100).");
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("EmpathiaLoginCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = CreatePanel(canvasGo.transform);

            CreateLabel(panel, "EmpathIA — Rol A · Sprint 1", 28, FontStyle.Bold, 16);
            CreateLabel(panel, "Login y sesión contra Backend B", 16, FontStyle.Normal, 8);

            _baseUrl = CreateInput(panel, "Base URL", EmpathiaAuthState.BaseUrl);
            _user = CreateInput(panel, "Usuario", "estudiante1");
            _pass = CreateInput(panel, "Contraseña", "password");
            _pass.contentType = InputField.ContentType.Password;

            var row = CreateRow(panel);
            _loginBtn = CreateButton(row.transform, "Entrar", OnLoginClicked);
            _sessionBtn = CreateButton(row.transform, "Crear sesión", OnCreateSessionClicked);
            _closeBtn = CreateButton(row.transform, "Cerrar sesión", OnCloseSessionClicked);

            _stateLabel = CreateLabel(panel, "Estado UI: idle", 16, FontStyle.Bold, 12);
            _status = CreateLabel(panel, "", 15, FontStyle.Normal, 0);
            _status.alignment = TextAnchor.UpperLeft;
            var statusRt = _status.GetComponent<RectTransform>();
            statusRt.sizeDelta = new Vector2(720, 160);
        }

        Transform CreatePanel(Transform parent)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(760, 520);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 24);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.transform;
        }

        Text CreateLabel(Transform parent, string text, int size, FontStyle style, int bottomPad)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null)
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            v.spacing = 4;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;
            wrap.GetComponent<LayoutElement>().preferredHeight = 64;

            CreateLabel(wrap.transform, placeholder, 13, FontStyle.Normal, 0);

            var go = new GameObject(placeholder, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(wrap.transform, false);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            go.GetComponent<LayoutElement>().preferredHeight = 34;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
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
            ph.fontSize = 16;
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
            h.spacing = 12;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            go.GetComponent<LayoutElement>().preferredHeight = 44;
            return go;
        }

        Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.85f, 1f);
            go.GetComponent<LayoutElement>().preferredHeight = 40;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = label;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null)
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = 16;
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
            SetStatus("Conectando con B…");
            StartCoroutine(_api.Login(_user.text.Trim(), _pass.text, (ok, msg) =>
            {
                SetBusy(false);
                SetUiState(ok ? "idle" : "idle");
                SetStatus(ok
                    ? msg + "\nUsuario: " + EmpathiaAuthState.Username
                    : "Error: " + msg);
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

        void SetBusy(bool busy)
        {
            _busy = busy;
            _loginBtn.interactable = !busy;
            _sessionBtn.interactable = !busy;
            _closeBtn.interactable = !busy;
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
    }
}
