# Dev Email Service Compose Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a local dev email capture service (Mailpit) to Docker Compose and wire API SMTP host to it so email flows are testable in development.

**Architecture:** Keep the change fully inside `docker-compose.yml` with no application code changes. Add one new infrastructure service (`mailpit`), connect API to it through Docker DNS (`mailpit` hostname), and keep current SMTP port/TLS/auth environment conventions.

**Tech Stack:** Docker Compose, Mailpit container image, existing ASP.NET Core environment variable configuration.

---

### Task 1: Assert desired compose state (failing first)

**Files:**
- Modify: `docker-compose.yml`
- Test: `docker-compose.yml` (assertion script reads this file)

- [ ] **Step 1: Write a failing state assertion (expected to fail before changes)**

```bash
python - <<'PY'
from pathlib import Path

compose = Path('docker-compose.yml').read_text()

assert 'mailpit:' in compose, 'mailpit service missing'
assert 'IdentityTokenDelivery__Smtp__Host: mailpit' in compose, 'API SMTP host not pointing to mailpit'
assert '- "8025:8025"' in compose, 'Mailpit UI port mapping missing'
print('All assertions passed')
PY
```

- [ ] **Step 2: Run assertion and verify failure**

Run: `python - <<'PY' ... PY` (same script from Step 1)
Expected: FAIL with at least one assertion error (`mailpit service missing` on current state)

- [ ] **Step 3: Commit checkpoint for failing-first evidence (optional)**

```bash
git status --short
```

Expected: no repository changes yet (assertion was read-only)

### Task 2: Add Mailpit service and wire API SMTP host

**Files:**
- Modify: `docker-compose.yml`
- Test: `docker-compose.yml` (same assertion script)

- [ ] **Step 1: Update compose service definitions**

Apply this exact shape in `docker-compose.yml`:

```yaml
services:
  api:
    environment:
      IdentityTokenDelivery__Smtp__Host: mailpit
    depends_on:
      postgres:
        condition: service_healthy
      mailpit:
        condition: service_started

  mailpit:
    image: axllent/mailpit:latest
    container_name: tibia-webstore-mailpit
    ports:
      - "1025:1025"
      - "8025:8025"
    restart: unless-stopped
```

Notes for exact edit:
- Replace only `IdentityTokenDelivery__Smtp__Host` value (`smtp.dev.local` -> `mailpit`).
- Keep existing SMTP port/user/password/from/tls env vars unchanged.
- Keep existing `postgres` dependency unchanged and append `mailpit` dependency.

- [ ] **Step 2: Re-run assertion and verify pass**

Run: `python - <<'PY' ... PY` (same script from Task 1)
Expected: PASS with `All assertions passed`

- [ ] **Step 3: Validate compose syntax**

Run: `docker compose config`
Expected: command succeeds and prints normalized compose config containing `mailpit` service and API SMTP host `mailpit`

- [ ] **Step 4: Start services and verify runtime wiring**

Run: `docker compose up -d`
Expected: `tibia-webstore-api`, `tibia-webstore-postgres`, and `tibia-webstore-mailpit` are running

- [ ] **Step 5: Manual delivery verification in Mailpit UI**

Run flow:
1. Trigger verification or password reset flow in the API.
2. Open `http://localhost:8025`.
3. Confirm a captured message appears.

Expected: email is visible in Mailpit inbox UI.

- [ ] **Step 6: Commit**

```bash
git add docker-compose.yml
git commit -m "chore(dev): add Mailpit to docker compose"
```

Expected: one commit with only compose-related change.
