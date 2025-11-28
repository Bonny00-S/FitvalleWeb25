namespace Fitvalle_25.Models.Viewmodels
{
    public class UnifiedRequestVM
    {
        public string Id { get; set; }
        public string Type { get; set; }  // "general" o "tutoring"

        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public string State { get; set; } // "pending" / "accepted" / "rejected"
        public string Avatar { get; set; }
        public int? Age { get; set; }
        public string Height { get; set; }
        public string Weight { get; set; }
        public string GoalWeight { get; set; }
        public string RegisterDate { get; set; }
        public string Description { get; set; }
    }

}
