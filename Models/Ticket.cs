using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementApi.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Subject { get; set; } = string.Empty; 

        public string Description { get; set; } = string.Empty;

        // Status: Open, In Progress, Resolved, Closed
        public string Status { get; set; } = "Open";

        // Priority: Low, Medium, High, Urgent
        public string Priority { get; set; } = "Medium";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? AssetId { get; set; }
        [ForeignKey("AssetId")]
        public Asset? Asset { get; set; }

        public int? UserId { get; set; }
    }
}