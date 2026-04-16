## Deferred Items (Phase 01 Plan 05)

- **Date:** 2026-04-14
- **Context:** While running full-plan verification command `dotnet test backend.slnx -v minimal`.
- **Out-of-scope issue:** `tests/UnitTests/Identity/VerificationAndPasswordResetRoundTripTests` has two pre-existing failures (`Sequence contains no elements`) unrelated to JWT bearer validation wiring and admin authorization checks introduced in plan 01-05.
- **Action taken:** Left untouched per scope boundary; focused identity/security regression filter for this plan passed.
