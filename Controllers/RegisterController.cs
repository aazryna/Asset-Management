using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Models;

namespace AssetManagementApi.Controllers
{
    [Route("api/register")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RegisterController(AppDbContext context)
        {
            _context = context;
        }

        public class RegisterModel
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty; 
            public string Role { get; set; } = "Normal User";
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // Check if the email has been registered
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email is already registered." });
            }

            // Create a new user object
            var newUser = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password, 
                Role = model.Role
            };

            _context.Users.Add(newUser);
            
            //Log registration activity
            _context.ActivityLogs.Add(new ActivityLog
            {
                Action = "USER_REGISTER",
                Description = $"New user registered: {model.Email}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration successful.", user = newUser });
        }
    }
}