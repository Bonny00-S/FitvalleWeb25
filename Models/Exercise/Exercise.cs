using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Fitvalle_25.Models.Exercise
{
    public class Exercise
    {
        [Key]
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        [Required]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [Required]
        public string? Description { get; set; }

        [JsonPropertyName("registerDate")]
        [Required]
        public DateTime RegisterDate { get; set; }

        [JsonPropertyName("typeID")]
        [Required]
        public string? TypeID { get; set; }

        [JsonPropertyName("muscleID")]
        [Required]
        public string? MuscleID { get; set; }

        [JsonPropertyName("imageUrl")]
        [Required]
        public string? ImageUrl { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ExerciseType? Type { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public TargetMuscle? Muscle { get; set; }
    }
}
