# Gameplay Tag System Benchmark Report

- Build/configuration summary: Debug test smoke scenario
- Runtime environment summary: .NET 10.0.0; Microsoft Windows 10.0.26200
- Registry summary: hash `16540112639998395124`, domains `8`, tags `62`, queries `5`
- Scenario summary table

| Scenario | Ticks | Operations | Mean ms | P95 ms | P99 ms | Max ms | Allocated bytes |
|---|---:|---:|---:|---:|---:|---:|---:|
| Stress_30Tps_BaselineGameplayFixture | 120 | 38400 | 0.0150 | 0.0158 | 0.0190 | 0.0199 | 0 |

- First slowdown point: not evaluated by smoke benchmark
- Allocation summary: measured tick allocation is reported above
- Notes for follow-up optimization: run the full pressure sweep outside CI when larger fixtures exist
