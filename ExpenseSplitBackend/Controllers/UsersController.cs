using ExpenseSplitBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpenseSplitBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Still requires a valid JWT
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            // UserManager can find the user directly from the HttpContext.User principal
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            // Map to DTO
            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                CreatedAt = user.LockoutEnd?.DateTime ?? DateTime.MinValue // Just an example, Identity stores different date props
                // Note: IdentityUser doesn't have a 'CreatedAt'. 
                // You could use 'LockoutEnd' or add a custom 'CreatedAt' prop to ApplicationUser
            };

            return Ok(userProfile);
        }
    }
}