using System.Text.Json.Serialization;

namespace Fitvalle_25.Models.Workout
{
    public class SessionExercise
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        [JsonPropertyName("exerciseId")]
        public string ExerciseId { get; set; }

        // extra data depending on exercise type
        [JsonPropertyName("sets")]
        public int? Sets { get; set; }

        [JsonPropertyName("reps")]
        public int? Reps { get; set; }

        [JsonPropertyName("weight")]
        public double? Weight { get; set; }

        [JsonPropertyName("speed")]
        public double? Speed { get; set; }

        [JsonPropertyName("duration")]
        public double? Duration { get; set; }
    }
}
