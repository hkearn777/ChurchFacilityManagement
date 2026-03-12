namespace ChurchFacilityManagement.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }
        public DateTime ReportDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string RequestMethod { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Assigned { get; set; } = string.Empty;
        public string Trade { get; set; } = string.Empty;
        public string CorrectiveAction { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Attachments { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public int RowNumber { get; set; }
    }
}
