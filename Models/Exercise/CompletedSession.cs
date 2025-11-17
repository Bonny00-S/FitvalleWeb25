using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fitvalle_25.Models.Exercise
{
    public class CompletedSession
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; }

        [JsonPropertyName("routineId")]
        public string RoutineId { get; set; }

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; }

        [JsonPropertyName("dateFinished")]
        public DateTime DateFinished { get; set; }

        [JsonPropertyName("exercisesDone")]
        public List<CompletedExercise> ExercisesDone { get; set; } = new();
    }
}
