using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims;
using AssetManagementApi.Data;
using AssetManagementApi.Models;

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Ticket> query = _context.Tickets
                .Include(t => t.Asset)
                    .ThenInclude(a => a.User) 
                .Include(t => t.User)
                .Include(t => t.CreatedBy);

            if (userRole != "Admin")
        {
                if (int.TryParse(userIdClaim, out int parsedUserId))
            {
                query = query.Where(t => t.CreatedById == parsedUserId || t.UserId == parsedUserId || t.Asset.UserId == parsedUserId);
            }
                else
            {
                return Unauthorized();
            }
        }

            var tickets = await query.ToListAsync();
            return Ok(tickets);
        }

        // POST: api/tickets
        [HttpPost]
        public async Task<ActionResult<Ticket>> CreateTicket(Ticket ticketDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(userIdClaim, out int parsedUserId))
        {
            return Unauthorized();
        }

        ticketDto.CreatedById = parsedUserId;

        if (ticketDto.AssetId.HasValue)
        {
            var asset = await _context.Assets.FindAsync(ticketDto.AssetId.Value);
            if (asset != null)
            {
                if (userRole == "Admin" && asset.UserId.HasValue)
                {
                    ticketDto.UserId = asset.UserId.Value;
                }
                else
                {
                    ticketDto.UserId = parsedUserId;
                }

                asset.Status = "Maintenance";
                _context.Entry(asset).State = EntityState.Modified;
            }
            else
            {
                ticketDto.UserId = parsedUserId;
            }
        }
        else
        {
            ticketDto.UserId = parsedUserId;
        }

        ticketDto.Status = "Open";
        ticketDto.CreatedAt = DateTime.UtcNow;

        _context.Tickets.Add(ticketDto);
        await _context.SaveChangesAsync();

        // Activity log
        var log = new ActivityLog
        {
            Action = "CREATE_TICKET",
            Description = $"Ticket ID {ticketDto.Id} ({ticketDto.Subject}) created for User ID {ticketDto.UserId}.",
            Timestamp = DateTime.UtcNow
        };
        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(ticketDto);
}

        // PUT: api/tickets/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTicket(int id, Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return BadRequest();
            }

            var existingTicket = await _context.Tickets.FindAsync(id);
            if (existingTicket == null)
            {
                return NotFound();
            }

            existingTicket.Status = ticket.Status;
            existingTicket.Resolution = ticket.Resolution;

            try
            {
                // activity log update ticket
                var log = new ActivityLog
                {
                    Action = "UPDATE_TICKET",
                    Description = $"Ticket ID {id} ({existingTicket.Subject}) status updated to {existingTicket.Status}.",
                    Timestamp = DateTime.UtcNow
                };
                _context.ActivityLogs.Add(log);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tickets.Any(e => e.Id == id))
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