using System.Text.Json.Serialization;

namespace Fitvalle_25.Models
{
    public class CoachCustomer
    {
        [JsonPropertyName("coachId")]
        public string CoachId { get; set; }

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; }

        [JsonPropertyName("assignedDate")]
        public string AssignedDate { get; set; }
    }
}
