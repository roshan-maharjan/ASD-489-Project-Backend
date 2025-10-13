using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ExpenseSplitLambdaBackend.Services.AuthService.Dtos;
using System.Net;
using System.Text.Json;

namespace ExpenseSplitLambdaBackend.Services.AuthService
{
    public class RegistrationLambda
    {
        private const string TABLE_NAME = "UserAuthTable";
        private readonly AmazonDynamoDBClient _dynamoDbClient = new();
        private readonly Table _userTable;

        public RegistrationLambda()
        {
            _userTable = (Table?)Table.LoadTable(_dynamoDbClient, TABLE_NAME);
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public async Task<APIGatewayProxyResponse> RegisterHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                var input = JsonSerializer.Deserialize<UserInput>(request.Body);
                // Basic validation (more comprehensive validation should be used in production)
                if (input == null) return CreateResponse(HttpStatusCode.BadRequest, "Invalid request body.");

                // 1. Check if user already exists (prevent overwriting)
                var existingUser = await _userTable.GetItemAsync(input.Email);
                if (existingUser != null)
                {
                    return CreateResponse(HttpStatusCode.Conflict, "A user with this email already exists.");
                }

                // 2. Hash the password securely
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(input.Password);

                // 3. Create the DynamoDB document
                var userRecord = new UserRecord { Email = input.Email, PasswordHash = hashedPassword };
                var document = Document.FromJson(JsonSerializer.Serialize(userRecord));

                // 4. Save to DynamoDB
                await _userTable.PutItemAsync(document);

                // 5. Return success response
                return CreateResponse(HttpStatusCode.Created, new { Message = "Registration successful." });
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Registration failed: {ex.Message}");
                return CreateResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
            }
        }

        private APIGatewayProxyResponse CreateResponse(HttpStatusCode statusCode, object body)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Body = JsonSerializer.Serialize(body),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
