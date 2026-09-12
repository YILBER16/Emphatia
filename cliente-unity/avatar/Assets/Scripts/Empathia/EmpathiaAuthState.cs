using UnityEngine;

namespace Empathia
{
    /// <summary>
    /// Estado de auth en memoria. El último session.id se guarda para poder cerrarlo si B queda bloqueado.
    /// </summary>
    public static class EmpathiaAuthState
    {
        const string PrefSessionId = "Empathia.LastSessionId";

        // IP B por defecto (lab). Editable en la UI de login.
        public static string BaseUrl { get; set; } = "http://192.168.1.31:8000/api/v1";
        public static string Token { get; set; }
        /// <summary>Token del adulto (admin/counselor) antes del assume.</summary>
        public static string AdultToken { get; set; }
        public static string Username { get; set; }
        public static string Role { get; set; }
        public static string StudentUserId { get; set; }
        public static string StudentDisplayName { get; set; }

        static string _sessionId;
        public static string SessionId
        {
            get => _sessionId;
            set
            {
                _sessionId = value;
                // Solo persistimos ids reales; no borramos el guardado al limpiar memoria.
                if (!string.IsNullOrEmpty(value))
                {
                    PlayerPrefs.SetString(PrefSessionId, value);
                    PlayerPrefs.Save();
                }
            }
        }

        public static bool HasToken => !string.IsNullOrEmpty(Token);
        public static bool HasSession => !string.IsNullOrEmpty(SessionId);
        public static bool IsAdultStaff =>
            Role == "admin" || Role == "counselor";

        public static string TokenPreview
        {
            get
            {
                if (string.IsNullOrEmpty(Token))
                    return "(sin token)";
                return Token.Length <= 12 ? Token : Token.Substring(0, 8) + "…" + Token.Substring(Token.Length - 4);
            }
        }

        public static string SavedSessionId => PlayerPrefs.GetString(PrefSessionId, "");

        public static void ClearSessionMemory()
        {
            _sessionId = null;
        }

        public static void ClearSession()
        {
            ClearSessionMemory();
        }

        public static void ForgetSavedSession()
        {
            _sessionId = null;
            PlayerPrefs.DeleteKey(PrefSessionId);
            PlayerPrefs.Save();
        }

        public static void ClearAll()
        {
            Token = null;
            AdultToken = null;
            Username = null;
            Role = null;
            StudentUserId = null;
            StudentDisplayName = null;
            ClearSessionMemory();
        }
    }
}
