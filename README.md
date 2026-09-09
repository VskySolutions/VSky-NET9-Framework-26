# VSkyBaseFramework (.NET 9)

A reusable **multi-tenant application base** for new VSky products. It ships the platform every product needs and no product-specific module:

- **Tenancy** — tenants with lifecycle (active / inactive / archived), tenant-scoped data via EF Core global query filters, tenant switching for multi-tenant users.
- **Identity and access** — platform-issued JWT (RS256) + API-key authentication, persons (CRM master records) promoted to users, system and custom roles, permission groups, permission-based RBAC, self-service password reset, session invalidation.
- **Tenant settings** — per-tenant SMTP accounts, email templates with platform defaults, tenant-configurable **option sets** (value lists with colours, icons and descriptions), user groups and departments.
- **Dashboards** — role-aware, per-user widget layouts (tenant admin and super admin tiers).
- **Universal Features** — a collaboration layer that attaches to **any** entity via a shared `(EntityType, EntityId)` key: conversations with @mentions, tags, attachments, activity timeline, reminders, notifications and preferences, pins, colour codes, checklists, sticky notes, deleted-records management, and a field-level modified log.
- **Background work** — Hangfire worker with SQL storage (email sending, reminder dispatch, sticky-note expiry).

Built on **.NET 9** following **Clean Architecture**, with a **Quasar 2 / Vue 3** admin SPA (`WEB/`).

> **Origin.** Forked on 2026-09-09 from `THF_EMSPortal` (branch `static-approval-policy`, commit `3320018`, including that working tree's uncommitted changes) with the REMS module removed. Git history was deliberately not carried over: this repository starts from a clean initial commit.

---

## Table of contents

- [Architecture](#architecture)
- [Technology stack](#technology-stack)
- [Solution structure](#solution-structure)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [Running the platform](#running-the-platform)
- [API surface](#api-surface)
- [Authentication & roles](#authentication--roles)
- [Database & migrations](#database--migrations)
- [Background jobs](#background-jobs)
- [Starting a new product on this base](#starting-a-new-product-on-this-base)
- [Further reading](#further-reading)

---

## Architecture

The platform is three deployable hosts sharing one SQL Server database:

| Host | Project | Responsibility |
|------|---------|----------------|
| **API** | `EmsPortal.Api` | HTTP entry point. Authenticates requests, serves every endpoint, owns EF Core migrations and startup seeding, hosts the Hangfire dashboard. Enqueues background jobs; never runs them. |
| **Worker** | `EmsPortal.Workers` | Runs the Hangfire server: email sending and the Universal Features recurring jobs. Without it, nothing queued (including email) is ever delivered. |
| **MCP Server** | `EmsPortal.McpServer` | Host placeholder for a Model Context Protocol surface (AI-agent access). |

Layering follows Clean Architecture — dependencies point inward:

```
Domain  ←  Application  ←  Infrastructure  ←  Api / Workers / McpServer
                                   ▲
                Shared  ───────────┘   (referenced by every project)
```

- **Domain** — entities and enums. No dependencies except `Shared`.
- **Application** — abstractions (interfaces), application services (email, option sets, universal features), default seed definitions. Depends only on `Domain` + `Shared`.
- **Infrastructure** — EF Core (`EmsPortalDbContext`), repositories, email, Hangfire, Serilog, security, Data Protection. Implements the Application abstractions.
- **Shared** — cross-cutting contracts: configuration option types, API response envelopes, the permission catalogue and role names.
- **Api / Workers / McpServer** — composition roots that wire everything via `AddApplication()` + `AddInfrastructure()`.

> The .NET projects and namespaces keep the `EmsPortal.*` names they were forked with. Renaming them is a mechanical, product-level decision; see [Starting a new product on this base](#starting-a-new-product-on-this-base).

---

## Technology stack

| Concern | Technology |
|---------|------------|
| Runtime | .NET 9 |
| Web API | ASP.NET Core 9 (controllers) |
| Persistence | Entity Framework Core 9, SQL Server |
| Background jobs | Hangfire (SQL Server storage) |
| Validation | FluentValidation (auto-validation) |
| Logging | Serilog → SQL Server sink + console |
| Auth | Platform-issued JWT (RS256) + API Key (PBKDF2); permission-based RBAC |
| Secrets at rest | .NET Data Protection (SQL-persisted key ring) |
| API docs | Native OpenAPI (`Microsoft.AspNetCore.OpenApi`) + Scalar UI |
| SPA | Quasar 2 / Vue 3 / Pinia / Vite (`WEB/`) |

---

## Solution structure

```
API/VSkyBaseFramework.sln
└── src/
    ├── EmsPortal.Domain          # entities, enums
    ├── EmsPortal.Shared          # contracts, config options, permission catalogue, role names
    ├── EmsPortal.Application     # abstractions, application services, default seeds (email templates, option sets)
    ├── EmsPortal.Infrastructure  # EF Core, repositories, email, Hangfire, security, logging
    ├── EmsPortal.Api             # ASP.NET Core host (controllers, middleware, auth, OpenAPI, seeding)
    ├── EmsPortal.Workers         # Hangfire worker
    └── EmsPortal.McpServer       # console host (MCP placeholder)
WEB/                              # Quasar SPA (see WEB/README.md)
```

---

## Getting started

### Prerequisites

- **.NET 9 SDK** (the repo pins the SDK via `API/global.json`)
- **SQL Server** (2019+ or SQL Express; Azure SQL works as a drop-in)
- **Node 18+** for the SPA

### Build

```bash
cd API
dotnet build VSkyBaseFramework.sln -c Release

cd ../WEB
npm install
npx cross-env QENV=dev quasar build
```

---

## Configuration

Configuration lives in each host's `appsettings.json` (not committed — copy `appsettings.json.example` and fill in local values) and is overridable via environment variables / secrets. Key sections (`EmsPortal.Api`):

| Section | Purpose |
|---------|---------|
| `ConnectionStrings:SqlServer` | Shared SQL Server connection string |
| `App:BaseUrl` | Public SPA URL used in emails (login and reset links) |
| `Authentication` | `Issuer`, `Audience`, `PrivateKeyPem`/`PublicKeyPem` (RS256), `AccessTokenMinutes`, `RefreshTokenDays`, `ApiKeyHeaderName` |
| `ApiKeys` | Registered machine-to-machine keys (PBKDF2 hashes) |
| `Hangfire` | `WorkerCount`, `ServerName`, `SchemaName`, `DashboardEnabled` |
| `Cors:AllowedOrigins` | Browser origins allowed to call the API (defaults to the local Quasar dev ports) |
| `Serilog` | `MinimumLevel` |
| `Bootstrap` | First-run Super Admin (`Email`, `Password`, `TenantIdentifier`, `TenantName`) |
| `ErrorHandling` | `IncludeExceptionDetails` (defaults to Development) |

The SPA reads its API and web base URLs from `WEB/config/env.<QENV>.cjs`; the `prod` and `test` files hold placeholders that must be set per product.

> **Security:** never commit real secrets. Use environment variables, user-secrets, or a secrets manager for the connection string, signing keys, and the bootstrap password before any non-local deployment.

---

## Running the platform

The **API** owns schema migrations and seeds the platform on startup (system roles, default email templates, default option sets, and a bootstrap Super Admin plus tenant on first run), so start it first.

```bash
# Terminal 1 — API (applies migrations + seeds, serves HTTP)
cd API
dotnet run --project src/EmsPortal.Api

# Terminal 2 — Worker (Hangfire server: executes jobs; required for email)
dotnet run --project src/EmsPortal.Workers

# Terminal 3 — SPA dev server
cd WEB
npx cross-env QENV=dev quasar dev
```

In **Development** the API (launch profile `http://localhost:5032`) serves:

- Scalar API reference → `/scalar/v1`
- OpenAPI document → `/openapi/v1.json`
- Hangfire dashboard → `/hangfire` (admin role)

### First login

The bootstrap seeder creates a Super Admin (defaults — change them):

```
email:    admin@integrationhub.local
password: ChangeMe123!
```

```bash
curl -X POST http://localhost:5032/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@integrationhub.local","password":"ChangeMe123!"}'
# → { "data": { "accessToken": "...", "refreshToken": "...", ... } }
```

Use the `accessToken` as `Authorization: Bearer <token>` on subsequent calls.

---

## API surface

All responses use the standard envelope (`ApiResponse<T>` / `ApiErrorResponse`). One controller per area under `API/src/EmsPortal.Api/Controllers`:

| Area | Controllers | Base routes |
|------|-------------|-------------|
| **Auth & profile** | `AuthController`, `ProfileController` | `/api/auth/*`, `/api/users/me/*` |
| **Tenants** | `TenantsController` | `/api/admin/tenants` |
| **People & users** | `PersonsController`, `UsersController`, `UserGroupsController` | `/api/admin/persons`, `/api/admin/users`, `/api/admin/user-groups` |
| **Access** | `RolesController`, `RoleMembersController`, `RoleGroupCompositionController`, `PermissionGroupsController`, `EffectivePermissionsController` | `/api/admin/roles`, `/api/admin/permission-groups`, `/api/admin/permissions` |
| **Tenant settings** | `SmtpAccountController`, `EmailTemplatesController`, `OptionSetsController` | `/api/admin/smtp-accounts`, `/api/admin/email-templates`, `/api/option-sets` |
| **Dashboard** | `DashboardController` | `/api/dashboard` |
| **Media** | `MediaController` | `/api/media` |
| **Universal Features** | `ConversationMessagesController`, `MentionsController`, `TagsController`, `AttachmentsController`, `ActivityController`, `RemindersController`, `NotificationsController`, `PersonalFeaturesController` (pins, colours, PDF), `ChecklistsController`, `StickyNotesController`, `DeletedRecordsController`, `ModifiedLogController` | `/api/uf/*`, `/api/notifications` |

The SPA's `WEB/src/services/api.js` mirrors these as one resource group per controller.

---

## Authentication & roles

- **Two schemes:** platform JWT (RS256, primary) and API Key (`X-Api-Key`, machine-to-machine). A composite `AnyOf` scheme tries JWT first.
- **Permission-based RBAC:** endpoints are gated by `[RequirePermission("area.action")]` against the permission keys carried on the caller's roles. The catalogue lives in `EmsPortal.Shared.Security.Permissions` (`tenants.*`, `persons.*`, `users.*`, `roles.*`, `groups.manage`, `email.manage`, `settings.manage`, `records.adminDelete`, `optionSets.*`) and is mirrored in `WEB/src/composables/usePermissions.js`. System-role permission sets are re-seeded on every startup, so catalogue changes apply without a data migration — but a live session's permissions come from its JWT, so users must sign in again to see them.
- **Seeded system roles:** `SuperAdmin` (every permission) and `TenantAdmin`. Everything else is a custom role composed of permission keys and/or permission groups.
- **Super-Admin-only actions:** deleting a `Person` and assigning roles across tenants are restricted to Super Admins regardless of granted permissions.
- **Tenant isolation:** the JWT carries `activeTenantId`; `TenantResolutionMiddleware` resolves and validates it, and all tenant-scoped queries filter by it automatically (EF global query filters). Background jobs carry the tenant id in their Hangfire payload.
- **Session invalidation:** a `tokenVersion` on the user is incremented on password change, deactivation, email change, and logout; the JWT handler rejects stale tokens.

---

## Database & migrations

- **Code-first EF Core**, applied automatically by the API on startup (`Database.Migrate()`).
- The base ships a single `InitialCreate` migration that creates the whole schema. Seed **data** (roles, email templates, option sets, bootstrap admin) is applied in code on startup, never in migrations, so a fresh database is fully usable after the first API start.
- Generate a new migration:

```bash
cd API
dotnet ef migrations add <Name> \
  --project src/EmsPortal.Infrastructure \
  --startup-project src/EmsPortal.Api \
  --output-dir Persistence/Migrations
```

- Tables: `Tenants`, `TenantRoles`, `Users`, `UserTenantRoles`, `RefreshTokens`, `PasswordResetTokens`, `Roles`, `PermissionGroups`, `PermissionGroupPermissions`, `RolePermissionGroups`, `PermissionGroupTemplates`, `Persons`, `Addresses`, `Media`, `UserGroups`, `UserGroupMembers`, `UserDepartments`, `SmtpAccounts`, `EmailTemplates`, `OptionSets`, `OptionSetItems`, `DashboardLayouts`, `AuditTrail`, the Universal Features tables (`ConversationMessages`, `ConversationMessageMentions`, `Tags`, `EntityTags`, `Attachments`, `ActivityEvents`, `Reminders`, `Notifications`, `NotificationPreferences`, `Pins`, `ColourCodes`, `Checklists`, `ChecklistItems`, `StickyNotes`, `StickyNoteDismissals`, `UserStickyNoteStates`, `DeletedRecordRetentionConfigs`, `FieldModifiedLogs`, `ModifiedLogFieldConfigs`), and `DataProtectionKeys`. Hangfire and Serilog manage their own schemas.

---

## Background jobs

- The **Worker** hosts the Hangfire server; the API only enqueues.
- `EmailSendJob` delivers queued transactional email through the tenant's active SMTP account. Only templates on the email allowlist (`EmailSendPolicy`) are ever emailed; everything else is in-app only.
- Recurring jobs registered by the worker on startup: `ReminderDispatchJob` (every minute) and `StickyNoteExpiryJob` (hourly).

---

## Starting a new product on this base

1. Copy or clone this repository and rename the database in `appsettings.json`.
2. Add the product's modules on top: an `EntityType` value per new record type (numbers are append-only; 6 and 14 are retired and must not be reused), entities and EF configurations, repositories registered in `Infrastructure/DependencyInjection.cs`, controllers, and SPA modules under `WEB/src/modules/`. `API/docs/DEVELOPMENT.md` lists every file a new module needs.
3. Add the module's permission keys to `Permissions.cs` and `usePermissions.js`, its default option sets to `DefaultOptionSets.cs`, its email templates to `DefaultEmailTemplates.cs` (and to `EmailSendPolicy` if they may be emailed), and its notification and activity-event types to their enums.
4. Wire its records into the Universal Features hooks that key on `EntityType`: `UniversalFeatureEntityAccess` (read permission), `UploadRecordKeyResolver` (upload folder name), `DeletedRecordsRepository` (restore / purge), `AggregateRootTouch` (if its list shows a parent worked on through child records), and `useEntityMeta.js` (label, icon, permalink route).
5. Optionally rename the `EmsPortal.*` projects and namespaces to the product's name.

---

## Further reading

- [`API/docs/RUN.md`](API/docs/RUN.md) — build, configure, run each host, verify end-to-end, and troubleshoot.
- [`API/docs/SCALAR.md`](API/docs/SCALAR.md) — run and use the Scalar API reference and the OpenAPI document.
- [`API/docs/DEVELOPMENT.md`](API/docs/DEVELOPMENT.md) — coding conventions, architecture rules, and how to extend the platform (add an entity, endpoint, migration, or SPA module).
- [`WEB/README.md`](WEB/README.md) — SPA conventions and shared building blocks.
