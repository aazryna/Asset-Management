namespace AssetManagementApi.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty; 
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; 
        public int? TicketId { get; set; }
        public int? TargetUserId { get; set; }
    }
}