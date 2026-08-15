using System;

namespace Empathia
{
    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class LoginResponse
    {
        public string token;
        public string token_type;
        public string expires_at;
        public LoginUser user;
    }

    [Serializable]
    public class LoginUser
    {
        public string id;
        public string display_name;
        public string role;
        public string username;
    }

    [Serializable]
    public class CreateSessionRequest
    {
        public string locale = "es";
        public string client = "unity";
    }

    [Serializable]
    public class CreateSessionResponse
    {
        public SessionDto session;
    }

    [Serializable]
    public class CloseSessionResponse
    {
        public bool ok;
        public SessionDto session;
    }

    [Serializable]
    public class SessionDto
    {
        public string id;
        public string student_user_id;
        public string status;
        public string locale;
        public string client;
        public string started_at;
        public string ended_at;
        public string ws_url;
        public string ws_ticket;
    }

    [Serializable]
    public class ApiErrorEnvelope
    {
        public ApiError error;
    }

    [Serializable]
    public class ApiError
    {
        public string code;
        public string message;
    }
}
