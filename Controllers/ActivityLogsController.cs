using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManagementApi.Data;
using AssetManagementApi.Models;

namespace AssetManagementApi.Controllers
{
    [Route("api/activity-logs")] 
    [ApiController]
    public class ActivityLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ActivityLogsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/activity-logs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActivityLog>>> GetActivityLogs()
        {
            return await _context.ActivityLogs
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}