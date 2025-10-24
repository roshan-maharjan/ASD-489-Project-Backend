using ExpenseSplitBackend.Controllers;
using ExpenseSplitBackend.Data;
using ExpenseSplitBackend.Models;
using ExpenseSplitBackend.Models.DTOs;
using ExpenseSplitBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace ExpenseSplitBackend.Tests.Controllers
{
    public class ExpenseControllerTests
    {
        private Mock<ApplicationDbContext> _dbContextMock;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<DebtEmailService> _debtEmailServiceMock;

        private ExpensesController CreateControllerWithUser(string userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            var controller = new ExpensesController(
                _dbContextMock.Object,
                _userManagerMock.Object,
                _debtEmailServiceMock.Object
            );
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            return controller;
        }

        public ExpenseControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ExpenseSplitTestDb")
                .Options;
            // FIX: Use actual ApplicationDbContext, not a mock, for in-memory DB
            _dbContextMock = new Mock<ApplicationDbContext>(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            _debtEmailServiceMock = new Mock<DebtEmailService>(null, null); // Pass nulls or mocks as needed for dependencies
        }

        [Fact]
        public async Task CreateExpense_ExactSplit_InvalidAmounts_ReturnsBadRequest()
        {
            // Arrange
            var userId = "user1";
            var payer = new ApplicationUser { Id = userId };
            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(payer);

            var model = new CreateExpenseModel(
                "Trip",
                100,
                System.DateTime.UtcNow,
                SplitMethod.Exact,
                new List<SplitParticipant>
                {
                    new SplitParticipant { UserId = userId, Amount = 50 },
                    new SplitParticipant { UserId = "user2", Amount = 30 },
                    new SplitParticipant { UserId = "user3", Amount = 10 }
                }
            );

            var controller = CreateControllerWithUser(userId);

            // Act
            var result = await controller.CreateExpense(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            // FIX: BadRequest value is likely a string, not an object with Message property
            var message = badRequest.Value?.ToString();
            Assert.Contains("Sum of exact amounts does not equal total amount", message);
        }

        [Fact]
        public async Task CreateExpense_PercentageSplit_InvalidPercentages_ReturnsBadRequest()
        {
            // Arrange
            var userId = "user1";
            var payer = new ApplicationUser { Id = userId };
            _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(payer);

            var model = new CreateExpenseModel(
                "Gift",
                100,
                System.DateTime.UtcNow,
                SplitMethod.Percentage,
                new List<SplitParticipant>
                {
                    new SplitParticipant { UserId = userId, Percentage = 40 },
                    new SplitParticipant { UserId = "user2", Percentage = 30 },
                    new SplitParticipant { UserId = "user3", Percentage = 20 }
                }
            );

            var controller = CreateControllerWithUser(userId);

            // Act
            var result = await controller.CreateExpense(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            // FIX: BadRequest value is likely a string, not an object with Message property
            var message = badRequest.Value?.ToString();
            Assert.Contains("Sum of percentages does not equal 100", message);
        }
    }
}