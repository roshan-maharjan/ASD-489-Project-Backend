using ExpenseSplitBackend.Data;
using ExpenseSplitBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseSplitBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Searches for users by name or email
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<UserProfile>());

            var users = await _context.Users
                .Where(u => u.Id != userId &&
                            (u.Email.Contains(query) ||
                             u.FirstName.Contains(query) ||
                             u.LastName.Contains(query)))
                .Select(u => new UserProfile(u.Id, u.FirstName, u.LastName, u.Email, null))
                .Take(10) // Limit results
                .ToListAsync();

            return Ok(users);
        }
    }
}