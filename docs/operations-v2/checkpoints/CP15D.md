# CP15D — EARTH-04 (ThermalInsulationCheck)

- Status: `Done`
- StartedAt: 2026-11-15
- CompletedAt: 2026-11-15
- Scope:
  - Configured `EarthOrderKind.ThermalInsulationCheck` (`EARTH-04`) in `EarthOrders.cs` (Step 0: `Outside`, `60s`, `-0.25` stamina/s, `energyRate = 3.0 / 90.0`; Step 1: `Inside`, `30s`, `-0.15` stamina/s, `energyRate = 3.0 / 90.0`; `3.0` energy total, `0` science, does not repair `SystemWear`).
  - Added EditMode test `Earth04ThermalInsulationCheckUsesThreeEnergyUnitsAndDoesNotRepairSystemWear` in `EarthOrderInfrastructureTests.cs`.
- Verification:
  - EditMode suite (`docs/validation/v2-cp15d-editmode.xml`): `70/70` Passed (`0` failed, `0` skipped).
