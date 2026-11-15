# CP15B — EARTH-02 (OxygenExperiment)

- Status: `Done`
- StartedAt: 2026-11-15
- CompletedAt: 2026-11-15
- Scope:
  - Configured `EarthOrderKind.OxygenExperiment` (`EARTH-02`) in `EarthOrders.cs` (1 interior lab step, `60s`, `-0.15` stamina/s, `usesOxygenSplit: true`, `0` science).
  - Updated `Resources.cs` (`StepResources`) to compute unified `activeOrder`, `orderWorkRate`, and `orderEnergy`: during `UsesOxygenSplit` with `production > 0`, `10%` of actual `production` is diverted to the experiment (`oxygenRate = production * 0.90 - consumedOxygen`) without double-deducting from tank; when `production <= 0`, `orderWorkRate = 0` so progress halts.
  - Added EditMode test `Earth02OxygenExperimentDivertsTenPercentOfActualProductionAndHaltsAtZeroProduction` in `EarthOrderInfrastructureTests.cs`.
- Verification:
  - EditMode suite (`docs/validation/v2-cp15b-editmode.xml`): `68/68` Passed (`0` failed, `0` skipped).
