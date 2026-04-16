---
phase: 02
slug: catalog-product-governance
status: ready
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-16
---

# Phase 02 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 10) |
| **Config file** | `tests/UnitTests/UnitTests.csproj`, `tests/IntegrationTests/IntegrationTests.csproj` |
| **Quick run command** | `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~Catalog"` |
| **Full suite command** | `dotnet test backend.slnx -v minimal` |
| **Estimated runtime** | ~60-120 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~Catalog"`
- **After every plan wave:** Run `dotnet test tests/IntegrationTests/IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~Catalog"`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 02-01-01 | 01 | 1 | CAT-03, CAT-04 | T-02-01, T-02-02 | Slugs immutable, invalid updates rejected | unit | `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~CatalogDomainInvariantTests"` | ✅ | ⬜ pending |
| 02-01-02 | 01 | 1 | CAT-02, CAT-03 | T-02-03 | AND filter semantics + bounded pagination | unit | `dotnet test tests/UnitTests/UnitTests.csproj -v minimal --filter "FullyQualifiedName~CatalogServiceFilterAndPaginationTests"` | ✅ | ⬜ pending |
| 02-02-01 | 02 | 2 | CAT-03, CAT-04 | T-02-04 | Unique slug/category constraints enforced at DB boundary | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~CatalogPersistenceContractTests"` | ✅ | ⬜ pending |
| 02-02-02 | 02 | 2 | CAT-04 | T-02-05 | Schema changes are applied before endpoint verification | infra | `dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project src/API/API.csproj --context AppDbContext` | ✅ | ⬜ pending |
| 02-03-01 | 03 | 3 | CAT-01, CAT-02, CAT-03 | T-02-06, T-02-07 | Customer read endpoints honor filters/slugs without auth escalation | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~CatalogCustomerEndpointsTests"` | ✅ | ⬜ pending |
| 02-03-02 | 03 | 3 | CAT-04 | T-02-08 | Admin endpoints require AdminOnly and enforce immutable slugs | integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj -v minimal --filter "FullyQualifiedName~CatalogAdminEndpointsTests"` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

All phase behaviors have automated verification.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 120s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-04-16
