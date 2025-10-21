using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Fitvalle_25.Models.Exercise
{
    public class SessionExercise
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        [JsonPropertyName("exerciseId")]
        public string ExerciseId { get; set; }

        // extra data depending on exercise type
        [JsonPropertyName("sets")]
        [Range(1, 10, ErrorMessage = "⚠️ Las series deben estar entre 1 y 10.")]
        public int? Sets { get; set; }

        [JsonPropertyName("reps")]
        [Range(1, 50, ErrorMessage = "⚠️ Las repeticiones deben estar entre 1 y 10.")]
        public int? Reps { get; set; }

        [JsonPropertyName("weight")]
        [Range(1, 500, ErrorMessage = "⚠️ El peso debe estar entre 1 y 500.")]
        public double? Weight { get; set; }

        [JsonPropertyName("speed")]
        [Range(1,20,ErrorMessage = "⚠️ La velocidad debe estar entre 1 y 20 km/h")]
        public double? Speed { get; set; }

        [JsonPropertyName("duration")]
        [Range(0,30,ErrorMessage = "⚠️ La duracion debe estar entre 0 y 30 minutos")]
        public double? Duration { get; set; }
    }
}
