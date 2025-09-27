using System.Text.Json.Serialization;

namespace Fitvalle_25.Models
{
    public class Request
    {
        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }
    }
}
