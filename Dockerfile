
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files
COPY ["OrderManagement.Api/OrderManagement.Api.csproj", "OrderManagement.Api/"]
COPY ["OrderManagement.Application/OrderManagement.Application.csproj", "OrderManagement.Application/"]
COPY ["OrderManagement.Domain/OrderManagement.Domain.csproj", "OrderManagement.Domain/"]
COPY ["OrderManagement.Infrastructure/OrderManagement.Infrastructure.csproj", "OrderManagement.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "OrderManagement.Api/OrderManagement.Api.csproj"

# Copy source code
COPY . .

# Publish API
RUN dotnet publish "OrderManagement.Api/OrderManagement.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderManagement.Api.dll"]
