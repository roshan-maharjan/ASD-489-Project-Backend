using ExpenseSplitBackend.Models;
using ExpenseSplitBackend.Models.DTOs;
using ExpenseSplitBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpenseSplitBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService; // Assume you have a simple JWT token service
        private readonly IStorageService _storageService;

        public AccountController(UserManager<ApplicationUser> userManager, ITokenService tokenService, IStorageService storageService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _storageService = storageService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FirstName = model.FirstName, LastName = model.LastName };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { Message = "Invalid credentials" });

            var token = _tokenService.CreateToken(user); // Implement this service
            var userProfile = new UserProfile(user.Id, user.FirstName, user.LastName, user.Email, user.QRCodeS3Url);

            return Ok(new AuthResponse(token, userProfile));
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            string? presignedUrl = null;
            if (!string.IsNullOrEmpty(user.QRCodeS3Url))
            {
                // Extract file name from the stored URL if needed
                var fileName = Path.GetFileName(new Uri(user.QRCodeS3Url).AbsolutePath);
                var objectKey = $"qrcodes/{fileName}";
                presignedUrl = _storageService.GetPreSignedUrl(objectKey, TimeSpan.FromMinutes(15));
            }

            return Ok(new UserProfile(user.Id, user.FirstName, user.LastName, user.Email, presignedUrl));
        }

        [Authorize]
        [HttpPost("profile/qrcode")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadQrCode([FromForm] QrCodeUploadDTO model)
        {
            var file = model.File;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var fileName = $"{userId}-qrcode{Path.GetExtension(file.FileName)}";
            var fileUrl = await _storageService.UploadFileAsync(file, fileName);

            user.QRCodeS3Url = fileUrl;
            await _userManager.UpdateAsync(user);

            return Ok(new { QrCodeUrl = fileUrl });
        }
    }
}
// Note: ITokenService is not shown, but it's a standard service to generate a JWT.