using ExpenseSplitBackend.Data;
using ExpenseSplitBackend.Models;
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
    public class FriendsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FriendsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Gets the user's current friends
        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var friendIds = await _context.Friendships
                .Where(f => f.UserId == userId)
                .Select(f => f.FriendId)
                .ToListAsync();

            var friends = await _context.Users
                .Where(u => friendIds.Contains(u.Id))
                .Select(u => new UserProfile(u.Id, u.FirstName, u.LastName, u.Email, null)) // Don't send QR code in list
                .ToListAsync();

            return Ok(friends);
        }

        // NEW: Searches for users who are NOT already friends
        [HttpGet("search-non-friends")]
        public async Task<IActionResult> SearchNonFriends([FromQuery] string query)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new List<UserProfile>());
            }

            var lowerQuery = query.ToLower();

            // 1. Get the list of IDs of people who are already friends
            var friendIds = await _context.Friendships
                .Where(f => f.UserId == userId)
                .Select(f => f.FriendId)
                .ToHashSetAsync(); // Use HashSet for efficient lookups

            // 2. Search for users who match the query, are NOT the user, and are NOT in the friends list
            var users = await _context.Users
                .Where(u => (u.FirstName.ToLower().Contains(lowerQuery) ||
                             u.LastName.ToLower().Contains(lowerQuery) ||
                             u.Email.ToLower().Contains(lowerQuery)) &&
                            u.Id != userId && // Don't include the user themselves
                            !friendIds.Contains(u.Id)) // Don't include existing friends
                .Select(u => new UserProfile(u.Id, u.FirstName, u.LastName, u.Email, null))
                .ToListAsync();

            return Ok(users);
        }

        // Adds a new friend
        [HttpPost]
        public async Task<IActionResult> AddFriend([FromQuery] string friendId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == friendId)
                return BadRequest("You cannot add yourself as a friend.");

            var friendExists = await _context.Users.AnyAsync(u => u.Id == friendId);
            if (!friendExists)
                return NotFound("User not found.");

            var alreadyFriends = await _context.Friendships
                .AnyAsync(f => f.UserId == userId && f.FriendId == friendId);

            if (alreadyFriends)
                return BadRequest("You are already friends with this user.");

            // Add friendship both ways for easy lookup
            var friendship1 = new Friendship { UserId = userId, FriendId = friendId };
            var friendship2 = new Friendship { UserId = friendId, FriendId = userId };

            _context.Friendships.AddRange(friendship1, friendship2);
            await _context.SaveChangesAsync();

            return Ok("Friend added successfully.");
        }

        // NEW: Deletes a friend
        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find both friendship entries
            var friendship1 = await _context.Friendships
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendId == friendId);

            var friendship2 = await _context.Friendships
                .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendId == userId);

            if (friendship1 == null || friendship2 == null)
            {
                // This could mean they aren't friends or data is inconsistent
                return NotFound("Friendship not found.");
            }

            _context.Friendships.Remove(friendship1);
            _context.Friendships.Remove(friendship2);
            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content is a standard successful response for DELETE
        }
    }
}