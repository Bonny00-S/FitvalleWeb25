using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace Fitvalle_25.Models.Exercise
{
    public class CompletedExercise
    {
        [JsonPropertyName("exerciseId")]
        public string ExerciseId { get; set; }

        [JsonPropertyName("exerciseName")]
        public string ExerciseName { get; set; }

        [JsonPropertyName("reps")]
        public int Reps { get; set; }

        [JsonPropertyName("sets")]
        public int Sets { get; set; }

        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("speed")]
        public double Speed { get; set; }
    }
}
