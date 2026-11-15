# CP15C — EARTH-03 (RadiationSensorCalibration)

- Status: `Done`
- StartedAt: 2026-11-15
- CompletedAt: 2026-11-15
- Scope:
  - Configured `EarthOrderKind.RadiationSensorCalibration` (`EARTH-03`) in `EarthOrders.cs` (Step 0: `Outside`, `60s`, `-0.25` stamina/s; Step 1: `Inside`, `20s`, `-0.15` stamina/s; `0` science).
  - Added EditMode test `Earth03RadiationSensorCalibrationRunsOutsideThenInsideReportAndAppliesRadiationRisk` in `EarthOrderInfrastructureTests.cs`.
- Verification:
  - EditMode suite (`docs/validation/v2-cp15c-editmode.xml`): `69/69` Passed (`0` failed, `0` skipped).
