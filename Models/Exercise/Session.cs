using System.Text.Json.Serialization;

namespace Fitvalle_25.Models.Exercise
{
    public class Session
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("routineId")]
        public string RoutineId { get; set; }

        [JsonPropertyName("registerDate")]
        public DateTime RegisterDate { get; set; }
    }
}
