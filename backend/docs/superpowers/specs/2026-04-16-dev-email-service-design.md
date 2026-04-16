## Dev Email Service in Docker Compose (Design)

### Objective

Add a development-only email capture service to local Docker Compose so identity emails (verification and password reset) can be inspected during development without external SMTP dependencies.

### Selected Approach

Approach A (recommended and approved):

- Add a `mailpit` service to `docker-compose.yml`.
- Set API SMTP host to Docker service name `mailpit`.
- Keep SMTP port `1025`, TLS disabled, and existing SMTP credential environment variables unchanged.
- Expose Mailpit UI on host `8025` for local inbox inspection.

### Scope

In scope:

- Compose service addition for Mailpit.
- API environment update for SMTP host.
- Compose startup dependency from API to Mailpit.

Out of scope:

- Any production deployment/runtime changes.
- Application code/domain logic changes.
- Broader mail provider abstraction or credential model changes.

### Compose Changes

1. Add new service:

- Service name: `mailpit`
- Image: official Mailpit image
- Port mappings:
  - `1025:1025` (SMTP)
  - `8025:8025` (Web UI)
- Restart policy aligned with existing dev services (`unless-stopped`)

2. Update API SMTP host:

- Change `IdentityTokenDelivery__Smtp__Host` from `smtp.dev.local` to `mailpit`.

3. API startup dependency:

- Add `mailpit` under `api.depends_on` with `condition: service_healthy`.
4. Mailpit image/version stability:

- Pin Mailpit image to a specific version tag to avoid silent behavior drift from `latest`.
5. Mailpit readiness check:

- Add a Mailpit healthcheck that probes the web endpoint on port `8025`.

### Data/Flow Impact

- No persistence model changes.
- Existing email flows remain unchanged; SMTP target changes to local in-cluster Mailpit.
- Emails sent by the API are captured and viewable via `http://localhost:8025`.

### Error Handling and Reliability

- If Mailpit is unavailable, SMTP sends fail as they do today for unreachable SMTP hosts.
- `depends_on` with `service_healthy` improves startup ordering and readiness confidence, reducing transient failures during local boot.
- No retry strategy changes in this task.

### Security and Environment Considerations

- Dev-only local capture mailbox; not intended for production.
- TLS remains disabled for local SMTP (`UseTls=false`), unchanged from current dev behavior.
- Existing credentials remain in place to avoid widening task scope; Mailpit tolerates dev capture usage.
- Version pinning reduces unplanned local environment drift.

### Validation Plan

1. Run `docker compose up -d`.
2. Trigger an email flow (e.g., verification or password reset).
3. Open `http://localhost:8025` and verify message appears.
4. Confirm API can resolve SMTP host (`mailpit`) through Compose network.

### Risks and Mitigations

- Risk: Service name mismatch causes host resolution failures.
  - Mitigation: Keep host exactly equal to Compose service name (`mailpit`).
- Risk: Port conflict on host `8025`.
  - Mitigation: Developer can remap host port if needed without changing container-side defaults.

### Alternatives Considered

- MailHog: widely used, but Mailpit is modern and actively favored as successor.
- smtp4dev: richer feature set, but heavier than needed for current local workflows.

### Success Criteria

- `docker compose up` starts API, Postgres, and Mailpit successfully.
- Email flows from API are captured and visible in Mailpit UI.
- No application code changes required outside Compose environment configuration.
