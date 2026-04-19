# External Integrations

**Analysis Date:** 2026-04-18

## APIs & External Services

**Payments:**
- Mercado Pago - creates checkout preferences and receives payment webhooks for confirmation workflow
  - SDK/Client: `mercadopago-sdk` via `MercadoPago.Client.Preference` in `backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoPreferenceGateway.cs`
  - Auth: `MercadoPago:AccessToken` and `MercadoPago:WebhookSecret` (bound in `backend/src/Infrastructure/DependencyInjection.cs` and options in `backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoOptions.cs`)

**Messaging/Notifications:**
- WhatsApp Cloud API (Meta Graph API) - sends operational template notifications
  - SDK/Client: typed `HttpClient` (`services.AddHttpClient<IWhatsAppNotificationService, WhatsAppNotificationService>()`) in `backend/src/Infrastructure/DependencyInjection.cs`, implementation in `backend/src/Infrastructure/Notifications/WhatsAppNotificationService.cs`
  - Auth: `WhatsApp:AccessToken` (options in `backend/src/Infrastructure/Notifications/WhatsAppOptions.cs`)

**Email Delivery:**
- SMTP server - sends identity verification and password-reset tokens
  - SDK/Client: `System.Net.Mail.SmtpClient` in `backend/src/Infrastructure/Identity/Services/SmtpClientTokenTransport.cs`
  - Auth: `IdentityTokenDelivery:Smtp:Username` and `IdentityTokenDelivery:Smtp:Password` (options in `backend/src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptions.cs`)

## Data Storage

**Databases:**
- PostgreSQL (primary transactional store for domain data and Hangfire storage)
  - Connection: `ConnectionStrings:DefaultConnection` (read in `backend/src/Infrastructure/DependencyInjection.cs` and `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`)
  - Client: EF Core + Npgsql (`backend/src/Infrastructure/DependencyInjection.cs`, `backend/src/Infrastructure/Persistence/AppDbContext.cs`)

**File Storage:**
- Not yet configured for production file streaming; download endpoint returns 501 placeholder behavior in `backend/src/API/Downloads/DownloadEndpoints.cs`

**Caching:**
- None detected (no Redis/cache provider wiring in `backend/src/API` or `backend/src/Infrastructure`)

## Authentication & Identity

**Auth Provider:**
- Custom JWT authentication
  - Implementation: ASP.NET Core JWT Bearer configuration in `backend/src/API/Program.cs` and token generation via `JwtTokenService` registration in `backend/src/Infrastructure/DependencyInjection.cs`

## Monitoring & Observability

**Error Tracking:**
- None detected for external SaaS error tracking (no Sentry/Rollbar/etc. integration files)

**Logs:**
- Serilog-based structured logging with ASP.NET integration and Hangfire log provider in `backend/src/API/API.csproj` and `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`

## CI/CD & Deployment

**Hosting:**
- Dockerized ASP.NET API runtime (`mcr.microsoft.com/dotnet/aspnet:10.0`) via `backend/Dockerfile`
- Local orchestration via Docker Compose services (`api`, `postgres`, `mailpit`) in `backend/docker-compose.yml`

**CI Pipeline:**
- None detected (`.github/workflows/` not present)

## Environment Configuration

**Required env vars:**
- `ConnectionStrings__DefaultConnection` (database + Hangfire storage) - `backend/src/Infrastructure/DependencyInjection.cs`, `backend/src/Infrastructure/Jobs/HangfireConfiguration.cs`
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` (JWT auth) - `backend/src/API/Program.cs`, `backend/src/Infrastructure/DependencyInjection.cs`
- `MercadoPago__AccessToken`, `MercadoPago__PublicKey`, `MercadoPago__WebhookSecret`, `MercadoPago__NotificationUrl`, `MercadoPago__SuccessUrl`, `MercadoPago__FailureUrl`, `MercadoPago__PendingUrl` (payment gateway + webhook validation + callback URLs) - `backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoOptions.cs`
- `WhatsApp__AccessToken`, `WhatsApp__PhoneNumberId`, `WhatsApp__WhatsAppBusinessId`, optional `WhatsApp__ApiVersion`, `WhatsApp__BaseUrl` (WhatsApp notifications) - `backend/src/Infrastructure/Notifications/WhatsAppOptions.cs`
- `IdentityTokenDelivery__Provider`, `IdentityTokenDelivery__Smtp__Host`, `IdentityTokenDelivery__Smtp__Port`, `IdentityTokenDelivery__Smtp__Username`, `IdentityTokenDelivery__Smtp__Password`, `IdentityTokenDelivery__Smtp__FromEmail`, `IdentityTokenDelivery__Smtp__UseTls` (token email delivery) - `backend/src/Infrastructure/Identity/Options/IdentityTokenDeliveryOptions.cs`
- `DownloadSigningKey` (signed download token validation) - `backend/src/API/Downloads/DownloadEndpoints.cs`

**Secrets location:**
- Configuration is sourced from ASP.NET configuration providers (`appsettings*.json` + environment variables) used in `backend/src/API` and `backend/src/Infrastructure`
- `.env` files: Not detected in repository root scan
- Local development example values are present in `backend/src/API/appsettings.json`, `backend/src/API/appsettings.Development.json`, and `backend/docker-compose.yml`; use external secret storage/host env injection for non-local environments

## Webhooks & Callbacks

**Incoming:**
- `POST /payments/mercadopago/webhook` for Mercado Pago notifications in `backend/src/API/Payments/PaymentWebhookEndpoints.cs`

**Outgoing:**
- Backend sends configured payment notification callback URL to Mercado Pago preference creation (`NotificationUrl` in payment preference request) in `backend/src/Application/Payments/Services/PaymentPreferenceService.cs` and `backend/src/Infrastructure/Payments/MercadoPago/MercadoPagoPreferenceGateway.cs`
- Backend sets Mercado Pago checkout redirect callbacks (`SuccessUrl`, `FailureUrl`, `PendingUrl`) in payment preferences via `backend/src/Application/Payments/Contracts/PaymentContracts.cs`
- Backend performs outbound HTTPS requests to WhatsApp Graph API in `backend/src/Infrastructure/Notifications/WhatsAppNotificationService.cs`

---

*Integration audit: 2026-04-18*
