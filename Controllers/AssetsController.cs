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
        [Authorize]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssets([FromQuery] string? search)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var query = _context.Assets
                .Include(a => a.User) 
                .AsQueryable();

            if (userRole != "Admin")
            {
                if (int.TryParse(userIdString, out int currentUserId))
            {
                query = query.Where(a => a.UserId == currentUserId);
            }
                else
            {
                return Unauthorized(new { message = "Invalid user token." });
            }
        }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(a => 
                    a.Name.ToLower().Contains(lowerSearch) || 
                    a.Category.ToLower().Contains(lowerSearch) || 
                    a.serialNumber.ToLower().Contains(lowerSearch) ||
                    (a.User != null && a.User.Name.ToLower().Contains(lowerSearch))
                );
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
            // A missing assignment must remain NULL; never infer the logged-in user.
            if (asset.UserId == 0)
            {
                asset.UserId = null;
            }
            asset.Status = asset.UserId != null ? "In Use" : "Available";

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            await _context.Entry(asset).Reference(a => a.User).LoadAsync();

            return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, asset); 
        }

        
            [HttpPut("{id}")]
            [Authorize]
            public async Task<IActionResult> UpdateAsset(int id, Asset assetDto)
            {
                if (id != assetDto.Id)
            {
            return BadRequest(new { message = "Asset ID mismatch"});
            }

            var existingAsset = await _context.Assets.FindAsync(id);
            if (existingAsset == null)
            {
                return NotFound(new { message = "Asset not found" });
            }

            existingAsset.Name = assetDto.Name;
            existingAsset.serialNumber = assetDto.serialNumber;
            existingAsset.Category = assetDto.Category;
    
            // Handle unassigned user
            if (assetDto.UserId == 0 || assetDto.UserId == null)
            {
                existingAsset.UserId = null;
            }
            else
            {
                existingAsset.UserId = assetDto.UserId;
            }

            // 3. Logic auto-status ikut arahan hang
            if (existingAsset.UserId != null)
            {
                existingAsset.Status = "In Use";
            }
            else
        {
            if (existingAsset.Status != "Maintenance")
            {
                existingAsset.Status = "Available";
            }
        }

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

            if (asset == null || asset.IsDeleted)
            {
                return NotFound(new { message = "Asset not found" });
            }

            asset.IsDeleted = true;
            asset.Status = "Decommissioned";
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AssetExists(int id)
        {
            return _context.Assets.Any(e => e.Id == id);
        }

    }
}
