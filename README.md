# Tiklab-Challenge Backend
This is an ASP.NET Core Web API application using PostgreSQL, built and run entirely with Docker.
---

## 🚀 Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running on your system
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (optional, only if you want to run locally without Docker)
---

## 🛠️ How to Run

1. Open your terminal (PowerShell, CMD, or Bash)

2. Clone the project
   ```bash
   git clone https://github.com/fishperson113/ticklab-challenge.git
   ```
3. Navigate to the root folder of this project (where `docker-compose.yml` is located):
   ```bash
   cd ticklab-challenge/TiklabChallenge
   ```

3. Run the application using Docker Compose:

```bash
docker compose up
```

This will:

- Build the ASP.NET Core Web API app

- Start a PostgreSQL container

- Run the backend on http://localhost:8080

## 📂 Project Overview

- api (Dockerfile): .NET 8 + ASP.NET Core Web API

- db: PostgreSQL 15, running in Docker

- appsettings.json: Contains DB connection settings and application configs

## 🗃️ Database Info

The PostgreSQL service is preconfigured in appsettings.json:

```bash
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=tiklab;Username=postgres;Password=postgres"
}
```

**No local installation of PostgreSQL is required — Docker handles it for you.**

## 🧰 InMemory Database Flag (toggle)

File: appsettings.json

```bash
"UseInMemoryDatabase": true,
"ConnectionStrings": {
  "DefaultConnection": "Host=postgres;Port=5432;Database=ticklab-challenge;Username=postgres;Password=root"
}
```

How it works:
- UseInMemoryDatabase = true → API uses EF Core InMemory provider (no PostgreSQL required, data is lost when the app restarts).

- UseInMemoryDatabase = false → API uses PostgreSQL according to ConnectionStrings:DefaultConnection.

How to switch:
- For quick dev/test → set UseInMemoryDatabase = true and run docker compose up.

- For real database integration → set UseInMemoryDatabase = false and make sure you have created and applied migrations (see below).

## 📦 Migrations (when NOT using InMemory)

Create migration:

```bash
dotnet ef migrations add InitialCreate --project TiklabChallenge.Infrastructure --startup-project TiklabChallenge.API
```

Apply migration to DB:

```bash
dotnet ef database update --project TiklabChallenge.Infrastructure --startup-project TiklabChallenge.API
```

Notes:

- Ensure you have the Npgsql.EntityFrameworkCore.PostgreSQL package installed in the Infrastructure project.

- If EF Tools cannot find the provider (IMigrator error), add an IDesignTimeDbContextFactory for ApplicationContext
 or set the connection string environment variables before running the commands.

## 🛢️ Reset database with Docker (dev-only)

Remove container + volume to reset a fresh DB:

```bash
docker compose down -v
docker compose up --build
```

(Optional) debug endpoint is enabled in API:

```bash
POST http://localhost:8080/api/Debug/reset
```

## 🔐 Default dev account

If seeding is enabled:

- Email/UserName: admin

- Password: Admin@12345

Newly registered users are automatically assigned the Student role.

## ✅ Useful URLs

- **Backend:** http://localhost:8080
- **Swagger UI:** http://localhost:8080/swagger
