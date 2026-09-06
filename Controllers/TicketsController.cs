using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims;
using System.Text.Json;
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
        private readonly IWebHostEnvironment _environment;

        public TicketsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/tickets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ticket>>> GetTickets()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            IQueryable<Ticket> query = _context.Tickets
                .Include(t => t.Asset)
                    .ThenInclude(a => a.User) 
                .Include(t => t.User)
                .Include(t => t.CreatedBy)
                .Include(t => t.ResolutionHistory.OrderByDescending(history => history.CreatedAt));

            if (userRole != "Admin")
        {
                if (int.TryParse(userIdClaim, out int parsedUserId))
            {
                query = query.Where(t => t.CreatedById == parsedUserId || t.UserId == parsedUserId || (t.Asset != null && t.Asset.UserId == parsedUserId));
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
            Timestamp = DateTime.UtcNow,
            TicketId = ticketDto.Id
        };
        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(ticketDto);
}

        [HttpPost("with-attachments")]
        [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<ActionResult<Ticket>> CreateTicketWithAttachments(
            [FromForm] string subject,
            [FromForm] string description,
            [FromForm] string priority,
            [FromForm] int? assetId,
            [FromForm] List<IFormFile>? attachments)
        {
            if (attachments?.Count > 3)
            {
                return BadRequest("You can attach up to 3 images.");
            }

            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (attachments?.Any(file => !allowedContentTypes.Contains(file.ContentType.ToLowerInvariant())) == true)
            {
                return BadRequest("Only JPG, PNG, and WEBP images are allowed.");
            }

            if (attachments?.Any(file => file.Length > 5 * 1024 * 1024) == true)
            {
                return BadRequest("Each image must be 5 MB or smaller.");
            }

            var ticket = new Ticket
            {
                Subject = subject,
                Description = description,
                Priority = priority,
                AssetId = assetId
            };

            var createResult = await CreateTicket(ticket);
            if (createResult.Result is not OkObjectResult)
            {
                return createResult;
            }

            if (attachments is { Count: > 0 })
            {
                var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadDirectory = Path.Combine(webRootPath, "uploads", "tickets");
                Directory.CreateDirectory(uploadDirectory);

                var attachmentUrls = new List<string>();
                foreach (var file in attachments)
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid():N}{extension}";
                    var filePath = Path.Combine(uploadDirectory, fileName);

                    await using var stream = System.IO.File.Create(filePath);
                    await file.CopyToAsync(stream);
                    attachmentUrls.Add($"/uploads/tickets/{fileName}");
                }

                ticket.AttachmentUrls = JsonSerializer.Serialize(attachmentUrls);
                await _context.SaveChangesAsync();
            }

            return Ok(ticket);
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

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(existingTicket.Priority, ticket.Priority, StringComparison.OrdinalIgnoreCase) && userRole != "Admin")
            {
                return Forbid();
            }

            existingTicket.Status = ticket.Status;
            existingTicket.Priority = ticket.Priority;
            existingTicket.Resolution = ticket.Resolution;

            if (ticket.Status == "Resolved" && !string.IsNullOrWhiteSpace(ticket.Resolution))
            {
                _context.ResolutionHistories.Add(new ResolutionHistory
                {
                    TicketId = existingTicket.Id,
                    Feedback = ticket.Resolution,
                    CreatedAt = DateTime.UtcNow
                });
            }

            try
            {
                // activity log update ticket
                var log = new ActivityLog
                {
                    Action = "UPDATE_TICKET",
                    Description = $"Ticket ID {id} ({existingTicket.Subject}) status updated to {existingTicket.Status}.",
                    Timestamp = DateTime.UtcNow,
                    TicketId = existingTicket.Id,
                    TargetUserId = existingTicket.CreatedById ?? existingTicket.UserId
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