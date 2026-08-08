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

    [Serializable]
    public class CreateTurnResponse
    {
        public TurnDto turn;
    }

    [Serializable]
    public class TurnDto
    {
        public string id;
        public string session_id;
        public int sequence_no;
        public string status;
        public string client_turn_key;
    }

    [Serializable]
    public class EventsResponse
    {
        public EventEnvelope[] events;
        public long next_after;
    }

    [Serializable]
    public class EventEnvelope
    {
        public int v;
        public string type;
        public string ts;
        public string session_id;
        public long id;
        public EventPayload payload;
    }

    [Serializable]
    public class EventPayload
    {
        public string turn_id;
        public int sequence_no;
        public string client_turn_key;
        public string transcript;
        public string reply_text;
        public string message;
        public string code;
        public bool retryable;
        public string stage;
        public string state;
        public TtsInfo tts;
    }

    [Serializable]
    public class TtsInfo
    {
        public string format;
        public string url;
    }

    public class TurnResultInfo
    {
        public string TurnId;
        public string ReplyText;
        public string Transcript;
        public string TtsUrl;
        public string ErrorCode;
        public string ErrorMessage;
        public bool IsError;
    }
}
