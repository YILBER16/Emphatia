using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Empathia
{
    /// <summary>
    /// Cliente HTTP hacia Backend B. Nunca llama a inteligencia (:8100).
    /// </summary>
    public class EmpathiaApiClient : MonoBehaviour
    {
        const float PollIntervalSeconds = 0.5f;
        const float TurnTimeoutSeconds = 25f;

        public IEnumerator CheckHealth(Action<bool, string> onDone)
        {
            var url = EmpathiaAuthState.BaseUrl.TrimEnd('/') + "/health";
            yield return SendJson(
                "GET",
                url,
                "{}",
                bearer: null,
                (ok, code, text) =>
                {
                    if (!ok)
                    {
                        onDone(false, MapError(code, text, "No hay conexión con B en " + EmpathiaAuthState.BaseUrl));
                        return;
                    }

                    var status = "ok";
                    try
                    {
                        var parsed = JsonUtility.FromJson<HealthResponse>(text);
                        if (parsed != null && !string.IsNullOrEmpty(parsed.status))
                            status = parsed.status;
                    }
                    catch
                    {
                        // body crudo abajo
                    }

                    onDone(true, "Conexión OK con B (" + status + "). " + (text ?? ""));
                });
        }

        public IEnumerator Login(string username, string password, Action<bool, string> onDone)
        {
            var body = new LoginRequest { username = username, password = password };
            yield return SendJson(
                "POST",
                EmpathiaAuthState.BaseUrl.TrimEnd('/') + "/auth/login",
                JsonUtility.ToJson(body),
                bearer: null,
                (ok, code, text) =>
                {
                    if (!ok)
                    {
                        onDone(false, MapError(code, text, "No se pudo iniciar sesión."));
                        return;
                    }

                    var parsed = JsonUtility.FromJson<LoginResponse>(text);
                    if (parsed == null || string.IsNullOrEmpty(parsed.token))
                    {
                        onDone(false, "Respuesta de login sin token.");
                        return;
                    }

                    EmpathiaAuthState.Token = parsed.token;
                    EmpathiaAuthState.Username = parsed.user != null ? parsed.user.username : username;
                    EmpathiaAuthState.ClearSessionMemory();
                    onDone(true, "Login OK. Token: " + EmpathiaAuthState.TokenPreview);
                });
        }

        public IEnumerator CreateSession(Action<bool, string> onDone)
        {
            if (!EmpathiaAuthState.HasToken)
            {
                onDone(false, "Primero haz login.");
                yield break;
            }

            var body = new CreateSessionRequest();
            long lastCode = 0;
            string lastText = "";
            var created = false;
            var createMsg = "";

            yield return SendJson(
                "POST",
                EmpathiaAuthState.BaseUrl.TrimEnd('/') + "/accompaniment/sessions",
                JsonUtility.ToJson(body),
                EmpathiaAuthState.Token,
                (ok, code, text) =>
                {
                    lastCode = code;
                    lastText = text ?? "";
                    if (ok)
                    {
                        var parsed = JsonUtility.FromJson<CreateSessionResponse>(text);
                        if (parsed == null || parsed.session == null || string.IsNullOrEmpty(parsed.session.id))
                        {
                            createMsg = "Respuesta de sesión sin id.";
                            return;
                        }

                        EmpathiaAuthState.SessionId = parsed.session.id;
                        created = true;
                        createMsg = "Sesión creada. Id: " + EmpathiaAuthState.SessionId;
                        return;
                    }

                    if (TryAdoptSessionFromBody(text))
                    {
                        created = true;
                        createMsg = "Sesión activa reutilizada. Id: " + EmpathiaAuthState.SessionId;
                    }
                });

            if (created)
            {
                onDone(true, createMsg);
                yield break;
            }

            if (lastCode == 409 || ExtractErrorCode(lastText) == "SESSION_ALREADY_ACTIVE")
            {
                // Reutilizar la sesión activa: NO hace falta crear otra para poder hacer POST /turns.
                if (TryAdoptSessionFromBody(lastText))
                {
                    onDone(true, "Sesión activa reutilizada. Id: " + EmpathiaAuthState.SessionId);
                    yield break;
                }

                var savedId = EmpathiaAuthState.SavedSessionId;
                if (!string.IsNullOrEmpty(savedId))
                {
                    EmpathiaAuthState.SessionId = savedId;
                    Debug.Log("[Empathia] Reutilizando sesión guardada para /turns: " + savedId);
                    onDone(true, "Reutilizando sesión guardada. Id: " + savedId);
                    yield break;
                }

                var adopted = false;
                var adoptMsg = "";
                yield return FetchActiveSession((ok, msg) =>
                {
                    adopted = ok;
                    adoptMsg = msg;
                });
                if (adopted)
                {
                    onDone(true, adoptMsg);
                    yield break;
                }

                onDone(false,
                    "Hay una sesión activa en B y Unity no conoce su id. " +
                    "Pide a B: php artisan empathia:close-active-sessions " +
                    "(o reiniciar B con el fix de auto-cierre). " +
                    MapError(lastCode, lastText, "SESSION_ALREADY_ACTIVE"));
                Debug.LogWarning("[Empathia] CreateSession 409 body: " + lastText);
                yield break;
            }

            onDone(false, MapError(lastCode, lastText, "No se pudo crear la sesión."));
        }

        public IEnumerator CloseSessionById(string sessionId, Action<bool, string> onDone)
        {
            if (!EmpathiaAuthState.HasToken)
            {
                onDone(false, "Primero haz login.");
                yield break;
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                onDone(false, "sessionId vacío.");
                yield break;
            }

            var url = EmpathiaAuthState.BaseUrl.TrimEnd('/')
                      + "/accompaniment/sessions/"
                      + sessionId
                      + "/close";

            yield return SendJson(
                "POST",
                url,
                "{}",
                EmpathiaAuthState.Token,
                (ok, code, text) =>
                {
                    if (!ok)
                    {
                        onDone(false, MapError(code, text, "No se pudo cerrar la sesión."));
                        return;
                    }

                    EmpathiaAuthState.ClearSessionMemory();
                    if (EmpathiaAuthState.SavedSessionId == sessionId)
                        EmpathiaAuthState.ForgetSavedSession();
                    onDone(true, "Sesión cerrada: " + sessionId);
                });
        }

        public IEnumerator FetchActiveSession(Action<bool, string> onDone)
        {
            if (!EmpathiaAuthState.HasToken)
            {
                onDone(false, "Primero haz login.");
                yield break;
            }

            yield return SendJson(
                "GET",
                EmpathiaAuthState.BaseUrl.TrimEnd('/') + "/accompaniment/sessions/active",
                "{}",
                EmpathiaAuthState.Token,
                (ok, code, text) =>
                {
                    if (!ok)
                    {
                        onDone(false, MapError(code, text, "No se pudo consultar la sesión activa."));
                        return;
                    }

                    if (!TryAdoptSessionFromBody(text))
                    {
                        onDone(false, "No hay sesión activa en B.");
                        return;
                    }

                    onDone(true, "Sesión activa tomada. Id: " + EmpathiaAuthState.SessionId);
                });
        }

        public IEnumerator CloseActiveSessionOnServer(Action<bool, string> onDone)
        {
            if (!EmpathiaAuthState.HasToken)
            {
                onDone(false, "Primero haz login.");
                yield break;
            }

            yield return SendJson(
                "POST",
                EmpathiaAuthState.BaseUrl.TrimEnd('/') + "/accompaniment/sessions/active/close",
                "{}",
                EmpathiaAuthState.Token,
                (ok, code, text) =>
                {
                    if (!ok)
                    {
                        onDone(false, MapError(code, text, "No se pudo cerrar la sesión activa."));
                        return;
                    }

                    EmpathiaAuthState.ClearSessionMemory();
                    onDone(true, "Sesión activa cerrada en B.");
                });
        }

        static bool TryAdoptSessionFromBody(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                var parsed = JsonUtility.FromJson<CreateSessionResponse>(text);
                if (parsed != null && parsed.session != null && !string.IsNullOrEmpty(parsed.session.id))
                {
                    EmpathiaAuthState.SessionId = parsed.session.id;
                    return true;
                }
            }
            catch
            {
                // fallback abajo
            }

            // Extrae UUID aunque el JSON venga con otros campos.
            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                "\"id\"\\s*:\\s*\"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\"");
            if (!match.Success)
                return false;

            EmpathiaAuthState.SessionId = match.Groups[1].Value;
            return true;
        }

        public IEnumerator CloseSession(Action<bool, string> onDone)
        {
            if (!EmpathiaAuthState.HasToken)
            {
                onDone(false, "Primero haz login.");
                yield break;
            }

            if (!EmpathiaAuthState.HasSession)
            {
                onDone(false, "No hay session.id en memoria. Si B dice SESSION_ALREADY_ACTIVE, pide a B cerrarla o usa el id conocido.");
                yield break;
            }

            var url = EmpathiaAuthState.BaseUrl.TrimEnd('/')
                      + "/accompaniment/sessions/"
                      + EmpathiaAuthState.SessionId
                      + "/close";

            yield return SendJson(
                "POST",
                url,
                "{}",
                EmpathiaAuthState.Token,
                (ok, code, text) =>
                {
                    if (!ok)
                    {
                        onDone(false, MapError(code, text, "No se pudo cerrar la sesión."));
                        return;
                    }

                    EmpathiaAuthState.ClearSession();
                    onDone(true, "Sesión cerrada.");
                });
        }

        /// <summary>
        /// Envía texto a B: POST .../sessions/active/text (o session.id).
        /// </summary>
        public IEnumerator SendSessionText(string message, Action<bool, string, SessionTextResponse> onDone)
        {
            if (!EmpathiaAuthState.HasToken)
            {
                onDone(false, "Primero haz login.", null);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                onDone(false, "Texto vacío.", null);
                yield break;
            }

            // Preferir alias active (B lo documentó así).
            var sessionKey = "active";
            if (!string.IsNullOrEmpty(EmpathiaAuthState.SessionId))
                sessionKey = EmpathiaAuthState.SessionId;

            // Si no hay sesión en memoria, usar active; B resuelve la activa.
            if (string.IsNullOrEmpty(EmpathiaAuthState.SessionId))
                sessionKey = "active";

            var url = EmpathiaAuthState.BaseUrl.TrimEnd('/')
                      + "/accompaniment/sessions/"
                      + sessionKey
                      + "/text";

            var turnKey = System.Guid.NewGuid().ToString();
            var body = new SessionTextRequest
            {
                text = message.Trim(),
                message = message.Trim(),
                client_turn_key = turnKey,
            };

            Debug.Log("[Empathia] POST " + url + " | key=" + turnKey + " | " + message.Trim());

            yield return SendJson(
                "POST",
                url,
                JsonUtility.ToJson(body),
                EmpathiaAuthState.Token,
                (ok, code, text) =>
                {
                    if (!ok)
                    {
                        // Si falló con UUID, reintentar con active lo hace el caller; aquí solo reportamos.
                        onDone(false, MapError(code, text, "No se pudo enviar texto a B."), null);
                        return;
                    }

                    SessionTextResponse parsed = null;
                    try
                    {
                        parsed = JsonUtility.FromJson<SessionTextResponse>(text);
                    }
                    catch
                    {
                        // ignore
                    }

                    if (parsed != null && !string.IsNullOrEmpty(parsed.session_id))
                        EmpathiaAuthState.SessionId = parsed.session_id;
                    if (parsed != null && parsed.turn != null && !string.IsNullOrEmpty(parsed.turn.session_id))
                        EmpathiaAuthState.SessionId = parsed.turn.session_id;

                    onDone(true, text, parsed);
                });
        }

        /// <summary>
        /// Igual que SendSessionText pero fuerza alias "active".
        /// B exige client_turn_key (UUID) junto al texto.
        /// </summary>
        public IEnumerator SendActiveText(string message, Action<bool, string, SessionTextResponse> onDone)
        {
            if (!EmpathiaAuthState.HasToken)
            {
                onDone(false, "Primero haz login.", null);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                onDone(false, "Texto vacío.", null);
                yield break;
            }

            var url = EmpathiaAuthState.BaseUrl.TrimEnd('/')
                      + "/accompaniment/sessions/active/text";

            var turnKey = System.Guid.NewGuid().ToString();
            var body = new SessionTextRequest
            {
                text = message.Trim(),
                message = message.Trim(),
                client_turn_key = turnKey,
            };

            Debug.Log("[Empathia] POST " + url + " | key=" + turnKey + " | " + message.Trim());

            yield return SendJson(
                "POST",
                url,
                JsonUtility.ToJson(body),
                EmpathiaAuthState.Token,
                (ok, code, text) =>
                {
                    if (!ok)
                    {
                        onDone(false, MapError(code, text, "No se pudo enviar texto a B (/active/text)."), null);
                        return;
                    }

                    SessionTextResponse parsed = null;
                    try
                    {
                        parsed = JsonUtility.FromJson<SessionTextResponse>(text);
                    }
                    catch
                    {
                        // ignore
                    }

                    if (parsed != null && !string.IsNullOrEmpty(parsed.session_id))
                        EmpathiaAuthState.SessionId = parsed.session_id;
                    if (parsed != null && parsed.turn != null && !string.IsNullOrEmpty(parsed.turn.session_id))
                        EmpathiaAuthState.SessionId = parsed.turn.session_id;

                    onDone(true, text, parsed);
                });
        }

        /// <summary>
        /// Solo sube el WAV a B (POST /turns). No espera transcript ni TTS.
        /// B/C convierten a texto en su lado.
        /// </summary>
        public IEnumerator UploadTurnAudio(byte[] wavBytes, Action<string> onStatus, Action<bool, string> onDone)
        {
            if (!EmpathiaAuthState.HasToken || !EmpathiaAuthState.HasSession)
            {
                onDone(false, "Necesitas login y sesión activa.");
                yield break;
            }

            if (wavBytes == null || wavBytes.Length < 44)
            {
                onDone(false, "Audio vacío.");
                yield break;
            }

            var clientTurnKey = Guid.NewGuid().ToString();
            var turnUrl = EmpathiaAuthState.BaseUrl.TrimEnd('/')
                          + "/accompaniment/sessions/"
                          + EmpathiaAuthState.SessionId
                          + "/turns";

            onStatus?.Invoke("Enviando audio a B…");
            var uploadError = "";
            var turnId = "";

            yield return PostTurnMultipart(turnUrl, wavBytes, clientTurnKey, (ok, code, text) =>
            {
                if (!ok)
                {
                    uploadError = MapError(code, text, "No se pudo enviar el audio.");
                    return;
                }

                try
                {
                    var parsed = JsonUtility.FromJson<CreateTurnResponse>(text);
                    if (parsed != null && parsed.turn != null)
                        turnId = parsed.turn.id ?? "";
                }
                catch
                {
                    // ignore
                }
            });

            if (!string.IsNullOrEmpty(uploadError))
            {
                onDone(false, uploadError);
                yield break;
            }

            var msg = string.IsNullOrEmpty(turnId)
                ? "Audio enviado a B."
                : "Audio enviado a B (turn=" + turnId + ").";
            onStatus?.Invoke(msg);
            onDone(true, msg);
        }

        /// <summary>
        /// Sube WAV multipart, hace poll de events hasta turn.result / turn.error, descarga TTS.
        /// </summary>
        public IEnumerator RunTurn(
            byte[] wavBytes,
            Action<string> onStatus,
            Action<bool, TurnResultInfo, string> onDone)
        {
            if (!EmpathiaAuthState.HasToken || !EmpathiaAuthState.HasSession)
            {
                onDone(false, null, "Necesitas login y sesión activa antes del turno.");
                yield break;
            }

            if (wavBytes == null || wavBytes.Length < 44)
            {
                onDone(false, null, "Audio vacío. Genera WAV de prueba o graba micrófono.");
                yield break;
            }

            var clientTurnKey = Guid.NewGuid().ToString();
            var turnUrl = EmpathiaAuthState.BaseUrl.TrimEnd('/')
                          + "/accompaniment/sessions/"
                          + EmpathiaAuthState.SessionId
                          + "/turns";

            onStatus?.Invoke("Enviando audio para pasarlo a texto…");
            string turnId = null;
            string uploadError = null;

            yield return PostTurnMultipart(turnUrl, wavBytes, clientTurnKey, (ok, code, text) =>
            {
                if (!ok)
                {
                    uploadError = MapError(code, text, "No se pudo enviar el audio.");
                    return;
                }

                var parsed = JsonUtility.FromJson<CreateTurnResponse>(text);
                turnId = parsed != null && parsed.turn != null ? parsed.turn.id : null;
                if (string.IsNullOrEmpty(turnId))
                    uploadError = "B aceptó el turno pero no devolvió turn.id.";
            });

            if (!string.IsNullOrEmpty(uploadError))
            {
                onDone(false, null, uploadError);
                yield break;
            }

            onStatus?.Invoke("Audio enviado. Convirtiendo a texto…");

            long after = 0;
            var elapsed = 0f;
            TurnResultInfo result = null;

            while (elapsed < TurnTimeoutSeconds)
            {
                EventsResponse page = null;
                string pollError = null;

                yield return GetEvents(after, (ok, code, text) =>
                {
                    if (!ok)
                    {
                        pollError = MapError(code, text, "Error al consultar events.");
                        return;
                    }

                    page = JsonUtility.FromJson<EventsResponse>(text);
                });

                if (!string.IsNullOrEmpty(pollError))
                {
                    onDone(false, null, pollError);
                    yield break;
                }

                if (page != null)
                {
                    after = page.next_after;
                    if (page.events != null)
                    {
                        foreach (var ev in page.events)
                        {
                            if (ev == null || string.IsNullOrEmpty(ev.type))
                                continue;

                            onStatus?.Invoke("Evento: " + ev.type);

                            if (ev.type == "turn.error" && ev.payload != null
                                && (string.IsNullOrEmpty(turnId) || ev.payload.turn_id == turnId))
                            {
                                result = new TurnResultInfo
                                {
                                    TurnId = ev.payload.turn_id,
                                    IsError = true,
                                    ErrorCode = ev.payload.code,
                                    ErrorMessage = string.IsNullOrEmpty(ev.payload.message)
                                        ? "Error de turno"
                                        : ev.payload.message,
                                };
                                break;
                            }

                            if (ev.type == "turn.result" && ev.payload != null
                                && (string.IsNullOrEmpty(turnId) || ev.payload.turn_id == turnId))
                            {
                                result = new TurnResultInfo
                                {
                                    TurnId = ev.payload.turn_id,
                                    ReplyText = ev.payload.reply_text,
                                    Transcript = ev.payload.transcript,
                                    TtsUrl = BuildTtsUrl(ev.payload.turn_id, ev.payload.tts != null ? ev.payload.tts.url : null),
                                };
                                Debug.Log("[Empathia] turn.result transcript: " + (result.Transcript ?? "(vacío)"));
                                Debug.Log("[Empathia] turn.result reply_text: " + (result.ReplyText ?? "(vacío)"));
                                break;
                            }
                        }
                    }
                }

                if (result != null)
                    break;

                yield return new WaitForSeconds(PollIntervalSeconds);
                elapsed += PollIntervalSeconds;
            }

            if (result == null)
            {
                onDone(false, null, "Timeout esperando turn.result. Revisa events con B (after/poll).");
                yield break;
            }

            if (result.IsError)
            {
                onDone(false, result, MapTurnError(result.ErrorCode, result.ErrorMessage));
                yield break;
            }

            onDone(true, result, "turn.result OK");
        }

        public IEnumerator DownloadAndPlayTts(string ttsUrl, AudioSource audioSource, Action<bool, string> onDone)
        {
            if (string.IsNullOrEmpty(ttsUrl))
            {
                onDone(false, "Sin URL de TTS.");
                yield break;
            }

            if (!EmpathiaAuthState.HasToken)
            {
                onDone(false, "Sin token para descargar TTS.");
                yield break;
            }

            using (var req = UnityWebRequest.Get(ttsUrl))
            {
                req.SetRequestHeader("Authorization", "Bearer " + EmpathiaAuthState.Token);
                yield return req.SendWebRequest();

                var code = req.responseCode;
#if UNITY_2020_2_OR_NEWER
                var failed = req.result != UnityWebRequest.Result.Success;
#else
                var failed = req.isNetworkError || req.isHttpError;
#endif
                if (failed || code < 200 || code >= 300)
                {
                    onDone(false, MapError(code, req.downloadHandler != null ? req.downloadHandler.text : req.error, "No se pudo descargar TTS."));
                    yield break;
                }

                var bytes = req.downloadHandler.data;
                if (bytes == null || bytes.Length == 0)
                {
                    onDone(false, "TTS vacío.");
                    yield break;
                }

                var path = Path.Combine(Application.temporaryCachePath, "empathia-tts.wav");
                File.WriteAllBytes(path, bytes);
                var fileUrl = "file:///" + path.Replace("\\", "/");

                using (var clipReq = UnityWebRequestMultimedia.GetAudioClip(fileUrl, AudioType.WAV))
                {
                    yield return clipReq.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                    var clipFailed = clipReq.result != UnityWebRequest.Result.Success;
#else
                    var clipFailed = clipReq.isNetworkError || clipReq.isHttpError;
#endif
                    if (clipFailed)
                    {
                        onDone(false, "No se pudo decodificar el WAV de TTS.");
                        yield break;
                    }

                    var clip = DownloadHandlerAudioClip.GetContent(clipReq);
                    if (clip == null)
                    {
                        onDone(false, "AudioClip de TTS nulo.");
                        yield break;
                    }

                    if (audioSource == null)
                        audioSource = gameObject.AddComponent<AudioSource>();

                    audioSource.clip = clip;
                    audioSource.Play();
                    onDone(true, "Reproduciendo TTS (" + clip.length.ToString("0.0") + "s).");
                }
            }
        }

        IEnumerator PostTurnMultipart(
            string url,
            byte[] wavBytes,
            string clientTurnKey,
            Action<bool, long, string> onDone)
        {
            var form = new WWWForm();
            form.AddField("client_turn_key", clientTurnKey);
            form.AddBinaryData("audio", wavBytes, "turn.wav", "audio/wav");

            using (var req = UnityWebRequest.Post(url, form))
            {
                req.SetRequestHeader("Authorization", "Bearer " + EmpathiaAuthState.Token);
                req.SetRequestHeader("Accept", "application/json");
                yield return req.SendWebRequest();

                var code = req.responseCode;
                var text = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

                // 202 Accepted es éxito
                if (code >= 200 && code < 300)
                {
                    onDone(true, code, text);
                    yield break;
                }

                if (code == 0)
                {
                    onDone(false, 0, req.error ?? "connection");
                    yield break;
                }

                onDone(false, code, text);
            }
        }

        IEnumerator GetEvents(long after, Action<bool, long, string> onDone)
        {
            var url = EmpathiaAuthState.BaseUrl.TrimEnd('/')
                      + "/accompaniment/sessions/"
                      + EmpathiaAuthState.SessionId
                      + "/events?after="
                      + after;

            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Authorization", "Bearer " + EmpathiaAuthState.Token);
                req.SetRequestHeader("Accept", "application/json");
                yield return req.SendWebRequest();

                var code = req.responseCode;
                var text = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

                if (code == 0)
                {
                    onDone(false, 0, req.error ?? "connection");
                    yield break;
                }

                onDone(code >= 200 && code < 300, code, text);
            }
        }

        public static string BuildTtsUrl(string turnId, string serverUrl)
        {
            if (!string.IsNullOrEmpty(turnId))
            {
                return EmpathiaAuthState.BaseUrl.TrimEnd('/')
                       + "/accompaniment/turns/"
                       + turnId
                       + "/audio/tts";
            }

            return AlignUrlHost(serverUrl);
        }

        public static string AlignUrlHost(string absoluteUrl)
        {
            if (string.IsNullOrEmpty(absoluteUrl))
                return absoluteUrl;

            try
            {
                var baseUri = new Uri(EmpathiaAuthState.BaseUrl);
                var u = new Uri(absoluteUrl);
                var b = new UriBuilder(u)
                {
                    Scheme = baseUri.Scheme,
                    Host = baseUri.Host,
                    Port = baseUri.IsDefaultPort ? -1 : baseUri.Port,
                };
                return b.Uri.ToString();
            }
            catch
            {
                return absoluteUrl;
            }
        }

        public static string MapTurnError(string code, string message)
        {
            switch (code)
            {
                case "INTERNAL_ERROR":
                    return "Error interno en el turno (B/C). Reintentable. " + message;
                case "VALIDATION_ERROR":
                    return "Datos de turno inválidos. " + message;
            }

            if (!string.IsNullOrEmpty(message))
                return "Error de turno: " + message;
            return "Error de turno (" + code + ").";
        }

        IEnumerator SendJson(
            string method,
            string url,
            string json,
            string bearer,
            Action<bool, long, string> onDone)
        {
            using (var req = new UnityWebRequest(url, method))
            {
                if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    var payload = Encoding.UTF8.GetBytes(json ?? "{}");
                    req.uploadHandler = new UploadHandlerRaw(payload);
                    req.SetRequestHeader("Content-Type", "application/json");
                }

                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Accept", "application/json");
                if (!string.IsNullOrEmpty(bearer))
                    req.SetRequestHeader("Authorization", "Bearer " + bearer);

                yield return req.SendWebRequest();

                var code = req.responseCode;
                var text = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

#if UNITY_2020_2_OR_NEWER
                var failed = req.result != UnityWebRequest.Result.Success;
#else
                var failed = req.isNetworkError || req.isHttpError;
#endif
                if (failed && code >= 200 && code < 300)
                    failed = false;

                if (failed && code == 0)
                {
                    onDone(false, 0, req.error ?? "connection");
                    yield break;
                }

                onDone(code >= 200 && code < 300, code, text);
            }
        }

        public static string MapError(long httpCode, string bodyOrNetwork, string fallback)
        {
            if (httpCode == 0)
            {
                return "No se pudo conectar al servidor B en "
                       + EmpathiaAuthState.BaseUrl
                       + ". ¿Está encendido? (php artisan serve --host=127.0.0.1 --port=8000)";
            }

            var code = ExtractErrorCode(bodyOrNetwork);
            switch (code)
            {
                case "INVALID_CREDENTIALS":
                    return "Usuario o contraseña incorrectos.";
                case "SESSION_ALREADY_ACTIVE":
                    return "Ya hay una sesión activa. Ciérrala con el botón o pide a B que la cierre.";
                case "FORBIDDEN":
                    return "No tienes permiso para crear sesión (usa estudiante1).";
                case "UNAUTHENTICATED":
                case "UNAUTHORIZED":
                    return "No autorizado. Haz login de nuevo.";
            }

            if (httpCode == 401)
                return "No autorizado (401). Revisa usuario/clave o vuelve a hacer login.";
            if (httpCode == 409)
                return "Conflicto (409): probablemente ya hay una sesión activa.";

            if (!string.IsNullOrEmpty(ExtractErrorMessage(bodyOrNetwork)))
                return ExtractErrorMessage(bodyOrNetwork);

            return fallback + " (HTTP " + httpCode + ")";
        }

        static string ExtractErrorCode(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                var env = JsonUtility.FromJson<ApiErrorEnvelope>(json);
                return env != null && env.error != null ? env.error.code : null;
            }
            catch
            {
                return null;
            }
        }

        static string ExtractErrorMessage(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                var env = JsonUtility.FromJson<ApiErrorEnvelope>(json);
                return env != null && env.error != null ? env.error.message : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
