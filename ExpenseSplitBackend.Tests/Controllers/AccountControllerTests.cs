using Moq;
using ExpenseSplitBackend.Controllers;
using ExpenseSplitBackend.Models;
using ExpenseSplitBackend.Models.DTOs;
using ExpenseSplitBackend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ExpenseSplitBackend.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        _tokenServiceMock = new Mock<ITokenService>();
        _storageServiceMock = new Mock<IStorageService>();
        _controller = new AccountController(_userManagerMock.Object, _tokenServiceMock.Object, _storageServiceMock.Object);
    }

    [Fact]
    public async Task Register_ReturnsOk_WhenUserCreated()
    {
        var model = new RegisterModel("Test", "User", "test@example.com", "Password123!");
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.Register(model);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenUserCreationFails()
    {
        var model = new RegisterModel("Test", "User", "test@example.com", "Password123!");
        var errors = new[] { new IdentityError { Description = "Error" } };
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
            .ReturnsAsync(IdentityResult.Failed(errors));

        var result = await _controller.Register(model);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(errors, badRequest.Value);
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        var model = new LoginModel("test@example.com", "Password123!");
        var user = new ApplicationUser { Id = "1", Email = model.Email, FirstName = "Test", LastName = "User" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, model.Password)).ReturnsAsync(true);
        _tokenServiceMock.Setup(x => x.CreateToken(user)).Returns("token");

        var result = await _controller.Login(model);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var authResponse = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("token", authResponse.Token);
        Assert.Equal(user.Email, authResponse.User.Email);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
    {
        var model = new LoginModel("test@example.com", "Password123!");
        _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.Login(model);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid credentials", unauthorized.Value.GetType().GetProperty("Message")?.GetValue(unauthorized.Value));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        var model = new LoginModel("test@example.com", "Password123!");
        var user = new ApplicationUser { Id = "1", Email = model.Email };
        _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, model.Password)).ReturnsAsync(false);

        var result = await _controller.Login(model);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid credentials", unauthorized.Value.GetType().GetProperty("Message")?.GetValue(unauthorized.Value));
    }

    [Fact]
    public async Task GetProfile_ReturnsOk_WhenUserFound()
    {
        var user = new ApplicationUser { Id = "1", FirstName = "Test", LastName = "User", Email = "test@example.com" };
        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);

        var result = await _controller.GetProfile();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfile>(okResult.Value);
        Assert.Equal(user.Email, profile.Email);
    }

    [Fact]
    public async Task GetProfile_ReturnsNotFound_WhenUserIsNull()
    {
        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.GetProfile();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetProfile_ReturnsPresignedUrl_WhenQrCodeExists()
    {
        var user = new ApplicationUser
        {
            Id = "1",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            QRCodeS3Url = "https://bucket/qrcodes/1-qrcode.png"
        };
        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
        _storageServiceMock.Setup(x => x.GetPreSignedUrl(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns("presigned-url");

        var result = await _controller.GetProfile();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<UserProfile>(okResult.Value);
        Assert.Equal("presigned-url", profile.QRCodeS3Url);
    }

    [Fact]
    public async Task UploadQrCode_ReturnsOk_WhenFileUploaded()
    {
        var userId = "1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1);
        fileMock.Setup(f => f.FileName).Returns("qrcode.png");
        var dto = new QrCodeUploadDTO { File = fileMock.Object };

        var claims = new List<System.Security.Claims.Claim> { new(System.Security.Claims.ClaimTypes.NameIdentifier, userId) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _storageServiceMock.Setup(x => x.UploadFileAsync(fileMock.Object, It.IsAny<string>())).ReturnsAsync("file-url");
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

        var result = await _controller.UploadQrCode(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("file-url", okResult.Value.GetType().GetProperty("QrCodeUrl")?.GetValue(okResult.Value));
    }

    [Fact]
    public async Task UploadQrCode_ReturnsUnauthorized_WhenUserNotFound()
    {
        var userId = "1";
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1);
        fileMock.Setup(f => f.FileName).Returns("qrcode.png");
        var dto = new QrCodeUploadDTO { File = fileMock.Object };

        var claims = new List<System.Security.Claims.Claim> { new(System.Security.Claims.ClaimTypes.NameIdentifier, userId) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

        var result = await _controller.UploadQrCode(dto);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UploadQrCode_ReturnsBadRequest_WhenFileIsNullOrEmpty()
    {
        var userId = "1";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };
        var dto = new QrCodeUploadDTO { File = null };

        var claims = new List<System.Security.Claims.Claim> { new(System.Security.Claims.ClaimTypes.NameIdentifier, userId) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };

        var result = await _controller.UploadQrCode(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file uploaded.", badRequest.Value);
    }
}