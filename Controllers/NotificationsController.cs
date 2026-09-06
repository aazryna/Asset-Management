using System.Security.Claims;
using AssetManagementApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManagementApi.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            if (userRole == "Admin")
            {
                var newTickets = await (
                    from log in _context.ActivityLogs
                    join ticket in _context.Tickets on log.TicketId equals ticket.Id
                    join actor in _context.Users on ticket.CreatedById equals actor.Id
                    where log.Action == "CREATE_TICKET" && actor.Role != "Admin"
                    orderby log.Timestamp descending
                    select new
                    {
                        log.Id,
                        log.Action,
                        log.Description,
                        log.Timestamp,
                        log.TicketId
                    }).Take(20).ToListAsync();

                return Ok(newTickets);
            }

            var ticketUpdates = await _context.ActivityLogs
                .Where(log => log.Action == "UPDATE_TICKET" && log.TargetUserId == userId)
                .OrderByDescending(log => log.Timestamp)
                .Take(20)
                .Select(log => new
                {
                    log.Id,
                    log.Action,
                    log.Description,
                    log.Timestamp,
                    log.TicketId
                })
                .ToListAsync();

            return Ok(ticketUpdates);
        }
    }
}
