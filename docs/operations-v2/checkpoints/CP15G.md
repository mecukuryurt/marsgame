# CP15G — EARTH-07 (EmergencyStockAudit)

- Status: `Done`
- StartedAt: 2026-11-15
- CompletedAt: 2026-11-15
- Scope:
  - Configured `EarthOrderKind.EmergencyStockAudit` (`EARTH-07`) in `EarthOrders.cs` (Step 0: `Inside`, `60s`, `-0.15` stamina/s; Step 1: `Inside`, `30s`, `-0.15` stamina/s; does not add/consume `FoodRationsRemaining`, `0` science).
  - Added EditMode test `Earth07EmergencyStockAuditRunsTwoIndoorStepsWithoutChangingRationsOrScienceAndCompletesBag` in `EarthOrderInfrastructureTests.cs` (also verifying all 7 distinct Earth order kinds are drawn in the first 7 orders).
- Verification:
  - EditMode suite (`docs/validation/v2-cp15g-editmode.xml`): `73/73` Passed (`0` failed, `0` skipped).
