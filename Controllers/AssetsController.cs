using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AssetManagementApi.Data;
using AssetManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims; 

namespace AssetManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AssetsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Assets (List all assets & basic search feature)[cite: 1]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssets([FromQuery] string? search)
        {
            var query = _context.Assets
                .Include(a => a.User) 
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.Name.Contains(search) || 
                a.Category.Contains(search) || a.serialNumber.Contains(search));
            }

            return await query.OrderByDescending(a => a.Id).ToListAsync(); 
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Asset>> GetAsset(int id)
        {
            var asset = await _context.Assets
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asset == null)
            {
                return NotFound(new {message = "Asset not found" });
            }

            return asset;
        }

        // POST: api/Assets (Add new Asset)[cite: 1]
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Asset>> CreateAsset(Asset asset)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdString, out int userId))
            {
                asset.UserId = userId; 
            }
            else
            {
                return Unauthorized(new { message = "Invalid token or user ID not found." });
            }

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            await _context.Entry(asset).Reference(a => a.User).LoadAsync();

            return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, asset); 
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateAsset(int id, Asset asset)
        {
            if (id != asset.Id)
            {
                return BadRequest(new { message = "Asset ID mismatch"});
            }

            _context.Entry(asset).State = EntityState.Modified;

            try 
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssetExists(id))
                {
                    return NotFound(new { message = "Asset not found" });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        //DELETE: api/Assets5 (Delete asset)[cite: 1]
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAsset(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound(new { message = "Asset not found" });
            }

            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AssetExists(int id)
        {
            return _context.Assets.Any(e => e.Id == id);
        }

    }
}
