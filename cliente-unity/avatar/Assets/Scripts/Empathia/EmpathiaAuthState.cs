namespace Empathia
{
    /// <summary>
    /// Estado de auth en memoria de juego (Sprint 1). Solo habla con B (:8000).
    /// </summary>
    public static class EmpathiaAuthState
    {
        public static string BaseUrl { get; set; } = "http://127.0.0.1:8000/api/v1";
        public static string Token { get; set; }
        public static string Username { get; set; }
        public static string SessionId { get; set; }

        public static bool HasToken => !string.IsNullOrEmpty(Token);
        public static bool HasSession => !string.IsNullOrEmpty(SessionId);

        public static string TokenPreview
        {
            get
            {
                if (string.IsNullOrEmpty(Token))
                    return "(sin token)";
                return Token.Length <= 12 ? Token : Token.Substring(0, 8) + "…" + Token.Substring(Token.Length - 4);
            }
        }

        public static void ClearSession()
        {
            SessionId = null;
        }

        public static void ClearAll()
        {
            Token = null;
            Username = null;
            SessionId = null;
        }
    }
}
