using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Models; 

namespace AssetManagementApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);

            // Trace create data
            _context.ActivityLogs.Add(new ActivityLog
            {
                Action = "CREATE",
                Description = $"Created user: {user.Name} ({user.Email})"
            });

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.Name = user.Name;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;
            existingUser.Status = user.Status;

            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                existingUser.Password = user.Password;
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                Action = "UPDATE",
                Description = $"Updated user ID: {id} ({existingUser.Name})"
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
            if (user == null)
        {
            return NotFound();
        }

        user.Status = "Inactive";

        var activeTickets = await _context.Tickets
            .Where(ticket =>
                (ticket.CreatedById == id || ticket.UserId == id ||
                    (ticket.Asset != null && ticket.Asset.UserId == id)) &&
                ticket.Status != "Resolved" &&
                ticket.Status != "Closed" &&
                ticket.Status != "Cancelled")
            .ToListAsync();

        foreach (var ticket in activeTickets)
        {
            ticket.Status = "Cancelled";
            ticket.Resolution = "Cancelled because the assigned user is no longer available.";
        }

        var assignedAssets = await _context.Assets
            .Where(asset => asset.UserId == id && !asset.IsDeleted)
            .ToListAsync();

        foreach (var asset in assignedAssets)
        {
            asset.UserId = null;
            if (asset.Status != "Decommissioned")
            {
                asset.Status = "Available";
            }
        }

        // Trace delete activity
        _context.ActivityLogs.Add(new ActivityLog
        {
            Action = "DELETE",
            Description = $"Deleted user: {user.Name} (Email: {user.Email})"
        });

        await _context.SaveChangesAsync();

        return NoContent();
    }
        }
    }