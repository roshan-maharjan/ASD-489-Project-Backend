using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ExpenseSplitLambdaBackend.Services.AuthService.Dtos;
using System.Net;
using System.Text.Json;

namespace ExpenseSplitLambdaBackend.Services.AuthService
{
    public class LoginLambda
    {
        private const string TABLE_NAME = "UserAuthTable";
        private readonly AmazonDynamoDBClient _dynamoDbClient = new();
        private readonly Table _userTable;

        public LoginLambda()
        {
            _userTable = (Table?)Table.LoadTable(_dynamoDbClient, TABLE_NAME);
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<APIGatewayProxyResponse> LoginHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                var input = JsonSerializer.Deserialize<UserInput>(request.Body);
                if (input == null) return CreateResponse(HttpStatusCode.BadRequest, "Invalid request body.");

                // 1. Retrieve user from DynamoDB by Email
                var document = await _userTable.GetItemAsync(input.Email);

                if (document == null)
                {
                    // Return a generic error to prevent user enumeration
                    return CreateResponse(HttpStatusCode.Unauthorized, "Invalid credentials.");
                }

                var userRecord = JsonSerializer.Deserialize<UserRecord>(document.ToJson());

                // 2. Verify the provided password against the stored hash
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(input.Password, userRecord.PasswordHash);

                if (!isPasswordValid)
                {
                    return CreateResponse(HttpStatusCode.Unauthorized, "Invalid credentials.");
                }

                // 3. Generate JWT Token
                string jwtToken = JwtHelper.GenerateToken(userRecord.Email, userRecord.UserId);

                // 4. Return success response with the JWT
                return CreateResponse(HttpStatusCode.OK,
                    new
                    {
                        IsSuccessful = true,
                        Token = jwtToken
                    });
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Login failed: {ex.Message}");
                return CreateResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred during login.");
            }
        }

        private APIGatewayProxyResponse CreateResponse(HttpStatusCode statusCode, object body)
        {
            // Same helper method as above
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Body = JsonSerializer.Serialize(body),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
