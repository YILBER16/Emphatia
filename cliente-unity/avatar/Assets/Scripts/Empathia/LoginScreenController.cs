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
    /// Login A: UI uGUI (Canvas) visible en la pestaña Game durante Play.
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
            DontDestroyOnLoad(go);
            go.AddComponent<EmpathiaApiClient>();
            go.AddComponent<AudioSource>();
            go.AddComponent<LoginScreenController>();
        }

        EmpathiaApiClient _api;
        AudioSource _audio;
        Font _font;

        InputField _baseUrl;
        InputField _user;
        InputField _pass;
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

        void Awake()
        {
            try
            {
                _api = GetComponent<EmpathiaApiClient>() ?? gameObject.AddComponent<EmpathiaApiClient>();
                _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                EnsureEventSystem();
                BuildUi();
                SetStatus("Listo. Abre la pestaña GAME y usa Iniciar sesión.\nLab: estudiante1 / password");
                Debug.Log("[Empathia] UI de login lista. Mira la pestaña Game (no Scene).");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Empathia] Error creando UI: " + ex);
            }
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
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = CreateImage(canvasGo.transform, "BG", new Color(0.07f, 0.1f, 0.14f, 1f));
            StretchFull(bg.rectTransform);

            var card = CreateImage(canvasGo.transform, "Card", new Color(0.14f, 0.18f, 0.24f, 1f));
            var cardRt = card.rectTransform;
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(480, 580);

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 24);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            AddLabel(card.transform, "EmpathIA", 30, FontStyle.Bold, new Color(0.55f, 0.85f, 1f), 36);
            AddLabel(card.transform, "Inicio de sesión", 22, FontStyle.Bold, Color.white, 30);
            AddLabel(card.transform, "Autenticación contra servidor B", 13, FontStyle.Normal, new Color(1, 1, 1, 0.7f), 22);

            _baseUrl = AddInput(card.transform, "Servidor", EmpathiaAuthState.BaseUrl);
            _user = AddInput(card.transform, "Usuario", "estudiante1");
            _pass = AddInput(card.transform, "Contraseña", "password");
            _pass.contentType = InputField.ContentType.Password;

            _loginBtn = AddButton(card.transform, "Iniciar sesión", new Color(0.16f, 0.65f, 0.45f), 44, OnLogin);
            _state = AddLabel(card.transform, "Estado UI: idle", 14, FontStyle.Bold, Color.white, 22);
            _status = AddLabel(card.transform, "", 13, FontStyle.Normal, new Color(0.9f, 0.92f, 0.95f), 70);

            AddLabel(card.transform, "── Después del login ──", 12, FontStyle.Italic, new Color(1, 1, 1, 0.45f), 20);

            var row1 = AddRow(card.transform);
            _sessionBtn = AddButton(row1, "Crear sesión", new Color(0.22f, 0.48f, 0.78f), 36, OnCreateSession);
            _closeBtn = AddButton(row1, "Cerrar sesión", new Color(0.22f, 0.48f, 0.78f), 36, OnCloseSession);

            var row2 = AddRow(card.transform);
            _turnWavBtn = AddButton(row2, "Turno WAV", new Color(0.22f, 0.48f, 0.78f), 36, () => StartCoroutine(RunTurn(false)));
            _turnMicBtn = AddButton(row2, "Turno mic 3s", new Color(0.22f, 0.48f, 0.78f), 36, () => StartCoroutine(RunTurn(true)));

            _reply = AddLabel(card.transform, "Respuesta: (sin respuesta)", 13, FontStyle.Normal, new Color(0.75f, 0.95f, 0.8f), 40);
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

        Text AddLabel(Transform parent, string text, int size, FontStyle style, Color color, float height)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = GetFont();
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            go.GetComponent<LayoutElement>().minHeight = height * 0.6f;
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
            wrap.GetComponent<LayoutElement>().preferredHeight = 54;

            AddLabel(wrap.transform, title, 12, FontStyle.Normal, new Color(1, 1, 1, 0.75f), 18);

            var fieldGo = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            fieldGo.transform.SetParent(wrap.transform, false);
            fieldGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.12f);
            fieldGo.GetComponent<LayoutElement>().preferredHeight = 30;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(fieldGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = 15;
            text.color = Color.white;
            text.supportRichText = false;
            StretchFull(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(8, 4);
            text.rectTransform.offsetMax = new Vector2(-8, -4);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            phGo.transform.SetParent(fieldGo.transform, false);
            var ph = phGo.GetComponent<Text>();
            ph.font = GetFont();
            ph.fontSize = 15;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(1, 1, 1, 0.35f);
            ph.text = title;
            StretchFull(ph.rectTransform);
            ph.rectTransform.offsetMin = new Vector2(8, 4);
            ph.rectTransform.offsetMax = new Vector2(-8, -4);

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
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            go.GetComponent<LayoutElement>().preferredHeight = 36;
            return go.transform;
        }

        Button AddButton(Transform parent, string label, Color color, float height, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            go.GetComponent<LayoutElement>().minHeight = height;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = label;
            t.font = GetFont();
            t.fontSize = 15;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
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
            if (_loginBtn != null)
                _loginBtn.interactable = !busy;
            if (_sessionBtn != null)
                _sessionBtn.interactable = !busy;
            if (_closeBtn != null)
                _closeBtn.interactable = !busy;
            if (_turnWavBtn != null)
                _turnWavBtn.interactable = !busy;
            if (_turnMicBtn != null)
                _turnMicBtn.interactable = !busy;
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
