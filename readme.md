# ExpenseSplitBackend

ExpenseSplitBackend is a .NET 9 Web API application designed to manage and split expenses among users, track debts, handle user accounts, and facilitate notifications and storage using AWS services.

## Features

-   **User Management**: Register, authenticate, and manage user accounts.
-   **Expense Tracking**: Create, update, and manage expenses shared among friends.
-   **Debt Management**: Track debts and settlements between users.
-   **Friends Management**: Add, remove, and manage friend relationships.
-   **Notifications**: Email and SMS notifications using AWS Simple Email Service (SES).
-   **File Storage**: Store and retrieve files using AWS S3.
-   **Secure Authentication**: JWT-based authentication and password hashing with BCrypt.
-   **API Documentation**: Integrated Swagger/OpenAPI documentation.

## Technologies Used

-   **.NET 9.0**
-   **ASP.NET Core Web API**
-   **Entity Framework Core (MySQL via Pomelo)**
-   **AWS SDKs (S3, SES)**
-   **BCrypt.Net**
-   **Swagger (Swashbuckle)**
-   **Docker Compose** (for containerized deployment)

## Getting Started

### Prerequisites

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   [Docker](https://www.docker.com/get-started) (optional, for containerization)
-   MySQL database
-   AWS account with access to S3, and SES

### Installation

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/roshan-maharjan/ASD-489-Project-Backend.git](https://github.com/roshan-maharjan/ASD-489-Project-Backend.git)
    cd ASD-489-Project-Backend/ExpenseSplitBackend
    ```

2.  **Configure application settings:**
    -   Update `appsettings.json` with your database connection string and AWS credentials.

3.  **Run database migrations:**
    ```bash
    dotnet ef database update
    ```

4.  **Run the application:**
    ```bash
    dotnet run
    ```

5.  **Access Swagger UI:**
    -   Navigate to `http://localhost:5000/swagger` (or the configured port) for API documentation.

### Docker Deployment

1.  **Build and run with Docker Compose:**
    ```bash
    docker-compose up --build
    ```

## Project Structure

-   `Controllers/` - API endpoints for users, friends, expenses, debts, etc.
-   `Services/` - Business logic and integrations (e.g., storage, email).
-   `Models/DTOs/` - Data transfer objects and models.
-   `Migrations/` - Entity Framework Core migrations.
-   `appsettings.json` - Application configuration.
-   `docker-compose.yml` - Docker Compose setup.

## Testing

-   Unit and integration tests are located in the `ExpenseSplitBackend.Tests` project.
-   Run tests with:
    ```bash
    dotnet test
    ```

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

## License

This project is licensed under the MIT License.

## Contact

For questions or support, please open an issue on [GitHub](https://github.com/roshan-maharjan/ASD-489-Project-Backend).