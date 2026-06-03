# Copilot Instructions for SURIMI-controller

## Project Overview

SURIMI-controller is a .NET 8 / C# 12 Aspire-hosted solution that orchestrates multi-simulation fisheries experiments. It acts as an intermediary between downstream scientific services (CMSY, Environment, OutputCreator, Poseidon, Ecopath, FisheriesAuthority, Market, ValueChain) and the experiment lifecycle.

### Projects

| Project | Description |
|---|---|
| `SURIMI-controller` | Core gRPC controller service, event orchestration, aggregation |
| `SURIMI-gui` | Blazor frontend |
| `SURIMI.AppHost` | .NET Aspire app host — always use as startup project |
| `SURIMI.Controller.Tests` | xUnit unit tests for controller services |

---

## Architecture

### Experiment Lifecycle

An experiment consists of N parallel simulations (runs). `ExperimentManager` wires `ISimulationManager` events and fans out downstream gRPC calls once all simulations have reported in for a given date.

Key classes:
- `ExperimentManager` — central orchestrator; subscribes to all simulation events
- `Experiment` (model) — per-experiment state, keyed by `ExperimentId` in a `ConcurrentDictionary`
- `ISimulationManager` — raises events: `BiomassUpdated`, `CatchDispositionUpdated`, `FishingActivityUpdated`, `SalesUpdated`, `SpeciesPriceUpdated`, `SimulateStep`, `SimulationFinalised`, `SimulationCancelled`

### Date-Bucketed Aggregation Pattern

All per-simulation summaries are tracked in `Dictionary<DateTime, Dictionary<string, TSummary?>>` keyed by `(date, simulationId)`. Entries are lazily initialized and removed after aggregation.

- **Payload summaries** (`BiomassSummary`, `CatchDispositionSummary`, `FishingActivitySummary`, `SalesSummary`, `SpeciesPriceSummary`) use `OnSummaryUpdated<TEventArgs, TSummary>` in `ExperimentManager`.
- **Flag events** (`SimulateStep`, `SimulationFinalised`, `SimulationCancelled`) use `OnFlagUpdated` in `ExperimentManager`, tracking `Dictionary<DateTime, Dictionary<string, bool?>>`.

When all simulations have checked in for a date, `onAllReceived` is called and the date bucket is removed.

### Adding a New Summary Event Handler

Follow this pattern:

1. **`Experiment.cs`** — Add `Dictionary<DateTime, Dictionary<string, TNewSummary?>> NewSummary { get; set; } = new();`
2. **`IAggregatorService.cs`** — Add `TNewStatisticsSummary AddAggregateNew(List<TNewSummary?> list);`
3. **`AggregatorService.cs`** — Implement aggregation. Use `AggregateGrids` for spatial (lat/lon) data; write a bespoke loop for fleet-segment or species-keyed data.
4. **`ExperimentManager.cs`** — Add a one-liner handler using `OnSummaryUpdated`, calling the appropriate downstream client method.

### Adding a New Flag Event Handler

1. **`Experiment.cs`** — Add `Dictionary<DateTime, Dictionary<string, bool?>> NewEventCalled { get; set; } = new();`
2. **`ExperimentManager.cs`** — Add a one-liner handler using `OnFlagUpdated`.

---

## Coding Conventions

- **C# 12 / .NET 8** — use collection expressions (`[]`), expression-bodied members, primary constructors where idiomatic.
- **Event handlers** are expression-bodied one-liners delegating to `OnSummaryUpdated` or `OnFlagUpdated`. Avoid duplicating the tracking/aggregation logic inline.
- **Aggregation helpers** in `AggregatorService` use `AggregateGrids` for spatial (latitude/longitude) grid data. Fleet-segment and species-keyed data use a direct `DistinctBy`/`Select` loop with `MathNet.Numerics.Statistics`.
- **DI** is constructor-injected. Do not use service locator or static state.
- **gRPC clients** are registered via `GrpcClientFactory` with named clients (e.g. `"Environment"`, `"Cmsy"`).
- **No `await` in fire-and-forget** downstream calls from event handlers — downstream calls are intentionally not awaited.
- **Logging** uses structured `ILogger<T>` with named placeholders (`{ExperimentId}`, `{SimulationId}`, `{Date}`).

---

## Key Service Interfaces

| Interface | Downstream service |
|---|---|
| `ICmsyServiceClient` | CMSY (biomass, catch disposition, experiment step/finalise/cancel) |
| `IOutputCreatorServiceClient` | OutputCreator (all statistics updates) |
| `IEnvironmentServiceClient` | Environment (env variables, experiment step/finalise/cancel) |
| `IPoseidonServiceClient` | Poseidon simulation service |
| `IEcopathServiceClient` | Ecopath simulation service |
| `IFisheriesAuthorityServiceClient` | FisheriesAuthority simulation service |
| `IMarketServiceClient` | Market simulation service |
| `IValueChainServiceClient` | ValueChain simulation service |
| `IAggregatorService` | Local aggregation; no gRPC — computes mean/P05/P95 statistics |

---

## Testing

- Tests live in `SURIMI.Controller.Tests` using **xUnit**.
- Test class naming: `<ServiceUnderTest>Tests` (e.g. `AggregatorServiceTests`).
- Tests are arranged with `// Arrange / Act / Assert` comments.
- Edge cases to cover: null list, empty list, all-null elements, single element, missing cells/species in some summaries (treat as 0), mixed nulls, large datasets for percentile verification.
- The `AggregatorService` is instantiated directly (no mocks needed). Other services should be tested with mocked dependencies via `Moq` or `NSubstitute`.

---

## Proto Files

All `.proto` definitions are maintained in the separate [SURIMI-protocol](https://github.com/Official-EwE/SURIMI-protocol) repository (remote alias `surimi-protocol`). Do not create or modify `.proto` files inside this repository. Generated C# types (namespace `Grpc.Surimi`) are consumed as a NuGet/project reference.

---

## Aspire

- Use `SURIMI.AppHost` as the startup project when running or debugging.
- The dashboard requires `ASPNETCORE_URLS`, `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL`, and related env vars — these are set automatically when launching via the AppHost.
