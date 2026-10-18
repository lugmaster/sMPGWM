# Gameplay Tag System Benchmark Report

- Build/configuration summary: Debug test smoke scenario
- Runtime environment summary: .NET 10.0.0; Microsoft Windows 10.0.26200
- Registry summary: hash `2066811068655212625`, domains `8`, tags `65`, queries `5`
- Scenario summary table

| Scenario | Ticks | Operations | Mean ms | P95 ms | P99 ms | Max ms | Allocated bytes |
|---|---:|---:|---:|---:|---:|---:|---:|
| Stress_30Tps_BaselineGameplayFixture | 120 | 38400 | 0.0177 | 0.0254 | 0.0430 | 0.0656 | 0 |

- First slowdown point: not evaluated by smoke benchmark
- Allocation summary: measured tick allocation is reported above
- Notes for follow-up optimization: run the full pressure sweep outside CI when larger fixtures exist
