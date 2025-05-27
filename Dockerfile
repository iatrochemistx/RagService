# ---------- STAGE 1 : build ---------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy csproj files first for better layer caching
COPY src/RagService.Api/RagService.Api.csproj        ./RagService.Api/
COPY src/RagService.Application/RagService.Application.csproj  ./RagService.Application/
COPY src/RagService.Domain/RagService.Domain.csproj  ./RagService.Domain/
COPY src/RagService.Infrastructure/RagService.Infrastructure.csproj  ./RagService.Infrastructure/

# Restore only the API project (pulls in project references)
RUN dotnet restore RagService.Api/RagService.Api.csproj

# Copy *only* the source code under src/ into /src
COPY src/ ./

# Publish the API in Release mode
RUN dotnet publish RagService.Api/RagService.Api.csproj \
    -c Release -o /app/publish

# ---------- STAGE 2 : runtime -------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

# Have the app listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Force mock mode by default
ENV UseMocks=true

# Bring over the published output
COPY --from=build /app/publish .

# Run the API
ENTRYPOINT ["dotnet", "RagService.Api.dll"]
