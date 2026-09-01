namespace AssetManagementApi.Models
{
    public class Asset
    {
        public int Id {get; set;}
        public string Name {get; set;} = string.Empty;
        public string serialNumber {get; set;} = string.Empty;
        public string Category {get; set;} = string.Empty; //Laptop, Monitor, etc
        public string Status {get; set;} = "Available"; //Available, Assigned, Maintenance

        public int? UserId {get; set;}
        public User? User {get; set;}
    }
}