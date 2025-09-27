using System.Text.Json.Serialization;

namespace Fitvalle_25.Models
{
    public class FirebaseErrorResponse
    {
        [JsonPropertyName("error")]
        public FirebaseError Error { get; set; }
    }

    public class FirebaseError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
