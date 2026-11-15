# CP15E — EARTH-05 (WaterQualityAnalysis)

- Status: `Done`
- StartedAt: 2026-11-15
- CompletedAt: 2026-11-15
- Scope:
  - Configured `EarthOrderKind.WaterQualityAnalysis` (`EARTH-05`) in `EarthOrders.cs` (1 interior lab step, `60s`, `-0.15` stamina/s, `energyRate = 2.0 / 60.0`, `upfrontWaterCost = 2.0`, `0` science).
  - Updated `MissionSimulation.AssignEarthOrder` to deduct `2.0` water once (`!order.UpfrontWaterPaid`) and return clear rejection if `Water < 2.0`.
  - Added EditMode test `Earth05WaterQualityAnalysisRequiresTwoWaterAndTwoEnergyAndDoesNotHealWaterFault` in `EarthOrderInfrastructureTests.cs`.
- Verification:
  - EditMode suite (`docs/validation/v2-cp15e-editmode.xml`): `71/71` Passed (`0` failed, `0` skipped).
