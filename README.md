# LinguaSpace

**LinguaSpace** is a language-exchange social platform where learners connect, practise together, and grow. Features include a social feed, real-time voice/video rooms, direct messaging, gamification, and push notifications.

> Backend only. Built with .NET 10 Clean Architecture + CQRS.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| API style | Minimal API + Scalar interactive docs |
| Database | PostgreSQL (via EF Core 10) |
| Cache / SignalR backplane | Redis |
| Real-time | SignalR |
| Voice / Video | LiveKit |
| Auth | ASP.NET Identity · JWT + Refresh Token · Google OAuth |
| Push notifications | Firebase Cloud Messaging (FCM) |
| Observability | OpenTelemetry (OTLP) |
| Orchestration (dev) | .NET Aspire 13 |

---

## Architecture

Clean Architecture — dependency flow: **Domain → Application → Infrastructure / Web**

```
src/
├── Domain/          # Entities, value objects, domain events, enums (no external deps)
├── Application/     # MediatR commands/queries, validators, interfaces, AutoMapper profiles
├── Infrastructure/  # EF Core, ASP.NET Identity, Redis, LiveKit, Firebase, JWT
├── Web/             # Minimal API endpoints, OpenAPI/Scalar, middleware
├── AppHost/         # .NET Aspire orchestration (PostgreSQL + Redis containers + Web)
└── Shared/          # Service name constants shared between AppHost and other projects
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL + Redis via Aspire)
- [Bruno](https://www.usebruno.com/) (optional, for API testing)

---

## Getting Started

### 1. Build

```bash
dotnet build
```

### 2. Run

```bash
dotnet run --project src\AppHost
```

.NET Aspire starts automatically:
- **PostgreSQL** and **Redis** containers are created via Docker
- **Aspire dashboard** opens at `https://localhost:17251` (logs, traces, metrics)
- **Web API** is available at `https://localhost:7001`
- **Scalar API Reference** at `https://localhost:7001/scalar`

### 3. Configuration

Edit `src/Web/appsettings.json` for local overrides:

```jsonc
{
  "ConnectionStrings": {
    "LinguaSpaceDb": "Server=...;Database=LinguaSpaceDb;Username=...;Password=...;",
    "cache": "localhost:6379,abortConnect=false"
  },
  "Jwt": {
    "Key": "<min 32-char secret>",
    "Issuer": "LinguaSpace",
    "Audience": "LinguaSpaceApi"
  },
  "Google": {
    "ClientId": ""          // leave empty to skip audience validation in dev
  },
  "LiveKit": {
    "ApiKey": "devkey",
    "ApiSecret": "devsecret",
    "Host": "http://localhost:7880"
  },
  "FeedSettings": {
    "FanOutThreshold": 500  // users above this threshold use pull-based feed
  }
}
```

> When running via `AppHost`, Aspire injects `ConnectionStrings__LinguaSpaceDb` and `ConnectionStrings__cache` automatically — no manual DB setup needed.

---

## API Modules

All endpoints are documented at `/scalar` (interactive) and `/openapi/v1.json`.

| Module | Prefix | Highlights |
|---|---|---|
| **Auth** | `/api/Auth` | Register · Login · Refresh · Logout · Me · Verify email · Forgot/Reset password · FCM device token · Google OAuth |
| **Users** | `/api/Users` | Get profile · Update profile · Search users · Add language · Friend requests |
| **Feed** | `/api/Feed` | Get feed (following/explore tabs) · Create post · Add comment |
| **Messages** | `/api/Messages` | Send DM · Get conversation history |
| **Rooms** | `/api/Rooms` | List rooms · Create room · Join/Leave room · LiveKit token |
| **Gamification** | `/api/Gamification` | XP · Badges · Leaderboard |
| **Notifications** | `/api/Notifications` | List notifications · Mark as read |
| **Moderation** | `/api/Moderation` | Report content · Review reports (admin) |
| **Media** | `/api/Media` | Upload media for posts/rooms |
| **Webhooks** | `/api/Webhooks` | LiveKit room event webhooks |

---

## Auth Flow

```
Register → Login → [access token in body] + [refresh_token HttpOnly cookie]
                         ↓
                  Authenticated requests (Bearer token)
                         ↓
               Token expired → POST /api/Auth/refresh (cookie auto-sent)
```

**Google OAuth:** client obtains a Google `id_token` via Google Sign-In SDK, then `POST /api/Auth/oauth/google` with `{ "idToken": "..." }`. The backend validates, finds-or-creates the account, and returns the same token pair.

---

## Tests

```bash
# All tests
dotnet test

# Individual test projects
dotnet test tests\Domain.UnitTests
dotnet test tests\Application.UnitTests
dotnet test tests\Application.FunctionalTests
dotnet test tests\Infrastructure.IntegrationTests
```

Functional tests dispatch MediatR commands directly against a real PostgreSQL database (Respawn resets state between tests).

---

## API Testing with Bruno

A [Bruno](https://www.usebruno.com/) collection is included in `bruno/`.

1. Open Bruno → **Open Collection** → select the `bruno/` folder
2. Select the **Local** environment
3. Run requests in order — `01_Register` sets `accessToken` automatically via post-response scripts

Collections:
- `bruno/Auth/` — Register, Login, Refresh, Logout, Me, Google OAuth
- `bruno/Users/` — Profile, Update, Search, Add Language, Friend Request
- `bruno/Feed/` — Get Feed, Create Post, Explore, Add Comment
- `bruno/Messages/` — Send DM, Get History
- `bruno/Rooms/` — List, Create, Join

---

## Code Scaffolding

Scaffold new use cases from `src\Application\`:

```bash
# Command
dotnet new ca-usecase --name CreatePost --feature-name Feed --usecase-type command --return-type int

# Query
dotnet new ca-usecase -n GetFeed -fn Feed -ut query -rt FeedVm
```

Install the template if needed:

```bash
dotnet new install Clean.Architecture.Solution.Template::10.8.0
```

Each scaffolded folder contains `{Name}.cs` (command/query + handler) and an optional `{Name}Validator.cs`.

---

## Code Style

- `.editorconfig` enforces conventions: no `var`, `_camelCase` private fields, `s_camelCase` private static fields, file-scoped namespaces
- `TreatWarningsAsErrors = true` — all warnings are build errors
- NuGet versions managed centrally in `Directory.Packages.props`