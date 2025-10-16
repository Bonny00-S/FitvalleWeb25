using System.Text.Json.Serialization;

namespace Fitvalle_25.Models
{
    public class Customer
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("birthdate")]
        public string Birthdate { get; set; }  // formato "dd/MM/yyyy"

        [JsonPropertyName("weight")]
        public string Weight { get; set; }

        [JsonPropertyName("height")]
        public string Height { get; set; }

        [JsonPropertyName("goalWeight")]
        public string GoalWeight { get; set; }

        [JsonPropertyName("registerDate")]
        public string RegisterDate { get; set; }
    }
}
