using System.Text.Json.Serialization;

namespace Fitvalle_25.Models.Exercise
{
    public class Routine
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; }

        [JsonPropertyName("coachId")]
        public string? CoachId { get; set; } // optional

        [JsonPropertyName("registerDate")]
        public DateTime RegisterDate { get; set; }
    }
}
