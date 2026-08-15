using System;
using System.Collections;
using System.Text;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

namespace Empathia
{
    /// <summary>
    /// Speech-to-text local en Windows (DictationRecognizer).
    /// </summary>
    public class EmpathiaLocalStt : MonoBehaviour
    {
        readonly StringBuilder _finalText = new StringBuilder();
        string _hypothesis = "";
        bool _running;
        string _lastError;
        bool _privacyBlocked;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        DictationRecognizer _dictation;
#endif

        public bool IsRunning => _running;
        public string Hypothesis => _hypothesis ?? "";
        public string FinalText => _finalText.ToString().Trim();
        public string LastError => _lastError;
        public bool PrivacyBlocked => _privacyBlocked;

        public bool StartListening()
        {
            _lastError = null;
            _privacyBlocked = false;
            _hypothesis = "";
            _finalText.Length = 0;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                StopListeningInternal(dispose: true);
                _dictation = new DictationRecognizer();
                _dictation.AutoSilenceTimeoutSeconds = 60f;
                _dictation.InitialSilenceTimeoutSeconds = 20f;
                _dictation.DictationHypothesis += OnHypothesis;
                _dictation.DictationResult += OnResult;
                _dictation.DictationComplete += OnComplete;
                _dictation.DictationError += OnError;
                _dictation.Start();
                _running = true;
                Debug.Log("[Empathia] STT local iniciado. Status=" + _dictation.Status);
                return true;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _running = false;
                Debug.LogWarning("[Empathia] STT local no pudo iniciar: " + ex.Message);
                return false;
            }
#else
            _lastError = "STT local solo está disponible en Windows.";
            return false;
#endif
        }

        public IEnumerator SoftStop(float waitForFinalSeconds = 1.25f)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_dictation != null)
            {
                try
                {
                    if (_dictation.Status == SpeechSystemStatus.Running)
                        _dictation.Stop();
                }
                catch
                {
                    // ignore
                }
            }
#endif
            _running = false;

            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < waitForFinalSeconds)
                yield return null;

            StopListeningInternal(dispose: true);
        }

        public string CurrentBestText()
        {
            var text = FinalText;
            if (string.IsNullOrWhiteSpace(text))
                text = Hypothesis;
            return (text ?? "").Trim();
        }

        public static void OpenWindowsSpeechSettings()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                Application.OpenURL("ms-settings:privacy-speech");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Empathia] No se pudo abrir ajustes de Voz: " + ex.Message);
            }
#endif
        }

        void OnDestroy()
        {
            StopListeningInternal(dispose: true);
        }

        void StopListeningInternal(bool dispose)
        {
            _running = false;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_dictation == null)
                return;
            try
            {
                if (_dictation.Status == SpeechSystemStatus.Running)
                    _dictation.Stop();
            }
            catch
            {
                // ignore
            }

            if (!dispose)
                return;

            try
            {
                _dictation.DictationHypothesis -= OnHypothesis;
                _dictation.DictationResult -= OnResult;
                _dictation.DictationComplete -= OnComplete;
                _dictation.DictationError -= OnError;
                _dictation.Dispose();
            }
            catch
            {
                // ignore
            }

            _dictation = null;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        void OnHypothesis(string text)
        {
            _hypothesis = text ?? "";
        }

        void OnResult(string text, ConfidenceLevel confidence)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            if (_finalText.Length > 0)
                _finalText.Append(' ');
            _finalText.Append(text.Trim());
            _hypothesis = "";
            Debug.Log("[Empathia] STT local parcial: " + text);
        }

        void OnComplete(DictationCompletionCause cause)
        {
            _running = false;
            Debug.Log("[Empathia] STT local complete: " + cause);
        }

        void OnError(string error, int hresult)
        {
            // 0x80045509 = speech privacy policy not accepted
            if (hresult == unchecked((int)0x80045509))
                _privacyBlocked = true;
            _lastError = error + " (0x" + hresult.ToString("X8") + ")";
            _running = false;
            Debug.LogWarning("[Empathia] STT local error: " + _lastError);
        }
#endif

        public IEnumerator ListenUntil(Func<bool> shouldStop, float maxSeconds, Action<string> onLiveText)
        {
            var t0 = Time.realtimeSinceStartup;
            while (!shouldStop() && Time.realtimeSinceStartup - t0 < maxSeconds)
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                if (_dictation != null && _dictation.Status == SpeechSystemStatus.Failed)
                {
                    _lastError = _lastError ?? "DictationRecognizer Status=Failed";
                    break;
                }
#endif
                var live = CurrentBestText();
                onLiveText?.Invoke(string.IsNullOrWhiteSpace(live) ? "(escuchando… habla ahora)" : live);
                yield return null;
            }

            yield return SoftStop();
            var result = CurrentBestText();
            onLiveText?.Invoke(string.IsNullOrWhiteSpace(result) ? "(sin texto)" : result);
        }
    }
}
