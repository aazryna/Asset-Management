using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManagementApi.Models
{
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }

        [Column("username")]
        public string Name { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Column("role")] // 
        public string Role { get; set; } = "Staff";

        [Column("status")] // 
        public string Status { get; set; } = "Active";
    }
}