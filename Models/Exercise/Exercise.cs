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
        [Required(ErrorMessage ="El nombre del ejercicio es obligatorio")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [Required(ErrorMessage ="La descripcion es obligatoria")]
        public string? Description { get; set; }

        [JsonPropertyName("registerDate")]
        [Required]
        public DateTime RegisterDate { get; set; }

        [JsonPropertyName("typeID")]
        [Required(ErrorMessage ="El tipo de ejercicio es obligatorio")]
        public string? TypeID { get; set; }

        [JsonPropertyName("muscleID")]
        [Required(ErrorMessage ="El musculo objetivo es obligatorio")]
        public string? MuscleID { get; set; }

        [JsonPropertyName("imageUrl")]
        [Required(ErrorMessage ="La imagen del ejercicio es obligatoria")]
        public string? ImageUrl { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ExerciseType? Type { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public TargetMuscle? Muscle { get; set; }
    }
}
