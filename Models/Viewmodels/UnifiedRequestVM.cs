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
    }

}
