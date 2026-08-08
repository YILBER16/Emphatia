using System;
using System.Collections;
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
                    EmpathiaAuthState.ClearSession();
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
            yield return SendJson(
                "POST",
                EmpathiaAuthState.BaseUrl.TrimEnd('/') + "/accompaniment/sessions",
                JsonUtility.ToJson(body),
                EmpathiaAuthState.Token,
                (ok, code, text) =>
                {
                    if (!ok)
                    {
                        onDone(false, MapError(code, text, "No se pudo crear la sesión."));
                        return;
                    }

                    var parsed = JsonUtility.FromJson<CreateSessionResponse>(text);
                    if (parsed == null || parsed.session == null || string.IsNullOrEmpty(parsed.session.id))
                    {
                        onDone(false, "Respuesta de sesión sin id.");
                        return;
                    }

                    EmpathiaAuthState.SessionId = parsed.session.id;
                    onDone(true, "Sesión creada. Id: " + EmpathiaAuthState.SessionId);
                });
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

        IEnumerator SendJson(
            string method,
            string url,
            string json,
            string bearer,
            Action<bool, long, string> onDone)
        {
            using (var req = new UnityWebRequest(url, method))
            {
                var payload = Encoding.UTF8.GetBytes(json ?? "{}");
                req.uploadHandler = new UploadHandlerRaw(payload);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
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
