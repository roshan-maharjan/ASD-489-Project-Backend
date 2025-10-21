# Stage 1: Build the application
# Use the .NET 8 SDK image as the base
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy the rest of the source code and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Create the final runtime image
# Use the .NET 8 ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Entry point for the container
ENTRYPOINT ["dotnet", "ExpenseSplitBackend.dll"]