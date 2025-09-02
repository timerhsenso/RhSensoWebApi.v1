# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file
COPY RhSensoWebApi.sln ./

# Copy project files
COPY src/API/RhSensoWebApi.API.csproj ./src/API/
COPY src/Core/RhSensoWebApi.Core.csproj ./src/Core/
COPY src/Infrastructure/RhSensoWebApi.Infrastructure.csproj ./src/Infrastructure/
COPY tests/RhSensoWebApi.Tests/RhSensoWebApi.Tests.csproj ./tests/RhSensoWebApi.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish src/API/RhSensoWebApi.API.csproj -c Release -o /app/publish --no-restore

# Use the official .NET 8 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create logs directory
RUN mkdir -p /app/logs

# Copy published application
COPY --from=build /app/publish .

# Expose port
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Run the application
ENTRYPOINT ["dotnet", "RhSensoWebApi.API.dll"]

