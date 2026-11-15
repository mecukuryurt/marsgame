# CP15F — EARTH-06 (AntennaAlignment)

- Status: `Done`
- StartedAt: 2026-11-15
- CompletedAt: 2026-11-15
- Scope:
  - Configured `EarthOrderKind.AntennaAlignment` (`EARTH-06`) in `EarthOrders.cs` (Step 0: `Outside`, `60s`, `-0.25` stamina/s, `energyRate = 5.0 / 60.0`; Step 1: `Inside`, `30s`, `-0.15` stamina/s; `0` science; does not alter `CommsDelaySeconds` or Earth order arrival schedule).
  - Added EditMode test `Earth06AntennaAlignmentDrawsFiveEnergyOutsideAndRunsLinkTestInsideWithoutChangingCommsOrSchedule` in `EarthOrderInfrastructureTests.cs`.
- Verification:
  - EditMode suite (`docs/validation/v2-cp15f-editmode.xml`): `72/72` Passed (`0` failed, `0` skipped).
