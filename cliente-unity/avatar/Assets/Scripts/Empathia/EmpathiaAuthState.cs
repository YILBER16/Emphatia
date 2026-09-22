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
        public static StudentListItem SelectedStudent { get; set; }
        public static string PreferredName { get; set; }

        public static void SetPreferredName(string raw)
        {
            PreferredName = NormalizePreferredName(raw);
        }

        public static string NormalizePreferredName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var chars = new char[raw.Length];
            var n = 0;
            var prevSpace = false;
            foreach (var ch in raw.Trim())
            {
                var letter = char.IsLetter(ch) || ch == '\'' || ch == '-';
                var space = ch == ' ';
                if (!letter && !space)
                    continue;
                if (space)
                {
                    if (n == 0 || prevSpace)
                        continue;
                    prevSpace = true;
                    chars[n++] = ' ';
                    continue;
                }

                prevSpace = false;
                chars[n++] = ch;
            }

            if (n == 0)
                return null;

            var cleaned = new string(chars, 0, n).Trim();
            var parts = cleaned.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return null;

            var name = parts.Length >= 2 ? parts[0] + " " + parts[1] : parts[0];
            if (name.Length > 40)
                name = name.Substring(0, 40).Trim();
            return string.IsNullOrEmpty(name) ? null : name;
        }

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
            SelectedStudent = null;
            PreferredName = null;
            ClearSessionMemory();
        }
    }
}
