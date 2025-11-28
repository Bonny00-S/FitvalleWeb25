namespace Fitvalle_25.Models
{
    public class TutoringRequest
    {
        public string Id { get; set; }
        public string CoachId { get; set; }
        public string CoachName { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string Message { get; set; }
        public string PreviousCoachId { get; set; }
        public string RequestDate { get; set; }
        public string ResponseDate { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }
}
