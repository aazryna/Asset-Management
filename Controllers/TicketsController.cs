using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Models;

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TicketsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tickets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTickets()
        {
            return await _context.Tickets
                .Include(t => t.Asset)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        // POST: api/tickets
        [HttpPost]
        public async Task<ActionResult<Ticket>> CreateTicket(Ticket ticket)
        {
            ticket.CreatedAt = DateTime.UtcNow;
            _context.Tickets.Add(ticket);

            // Tambah Activity Log
            var log = new ActivityLog
            {
                Action = "CREATE_TICKET",
                Description = $"New Ticket '{ticket.Subject}' has been opened.",
                Timestamp = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTickets), new { id = ticket.Id }, ticket);
        }

        // DELETE: api/tickets/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            _context.Tickets.Remove(ticket);

            // activity log delete ticket
            var log = new ActivityLog
            {
                Action = "DELETE_TICKET",
                Description = $"Tiket ID {id} ({ticket.Subject}) has been deleted.",
                Timestamp = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}