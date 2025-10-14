using System.Text.Json.Serialization;
namespace Fitvalle_25.Models.Exercise
{
    public class TargetMuscle
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
