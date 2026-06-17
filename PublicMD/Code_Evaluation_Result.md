# Code Evaluation Result

## Purpose

This document records a lead-programmer style review of the current Unity worker/NPC prototype after
the wheat reward and warehouse deposit workflow was added.

The review checks the code against:

- `PublicMD/CodeConvention.md`
- `PublicMD/ProjectStructure.md`
- `reviewing-npc-work-code/references/review-workflow.md`
- the ownership rule that `WorkerAI` remains execution-only and worker behavior lives in selectors,
  actions, context capabilities, data, and providers.

Production code was not modified as part of this review.

## Review Snapshot

- Date: 2026-06-18
- Scope: `.cs` files under `Assets/Scripts` and `Assets/BehaviorGraph/CustomActionNode`, plus
  relevant `SampleScene.unity`, `FarmerAI.prefab`, and `FarmerGraph.asset` serialized references.
- Sources: current `CodeConvention.md`, `ProjectStructure.md`, previous
  `Code_Evaluation_Result.md`, C# scripts, action result data asset, scene/prefab/graph wiring.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and
  0 errors.
- Working tree note: `Assets/Prefab/FarmerAI.prefab` and `PublicMD/CodeConvention.md` were already
  modified before this report update. They were reviewed as current workspace state but not changed
  by this review.

## Executive Summary

The architecture still keeps the main boundary intact: `WorkerAI` is not deciding wheat behavior,
does not know `WorkerActionSet`, and does not read action result data. The wheat workflow was added
mostly in the correct layers: `WorkAction` adds carried wheat from data, `WorkerCarryStorage` owns
the carried amount, `WorkerDefaultActionSelector` decides when full workers should deposit, and
`DepositWheatAction` executes deposit timing.

The main functional gap is that deposited wheat is not stored anywhere. `DepositWheatAction` calls
`WorkerCarryStorage.DepositAllWheat()` and discards the returned amount. `WarehouseObject` exists in
the scene only as a destination; there is no `WarehouseStorage`, provider, or context capability that
receives and persists deposited wheat. This means the visible carry loop works, but the economic
reward is effectively lost on deposit.

No Critical issues were found. One High issue was found: missing warehouse inventory persistence.
Most remaining issues are Medium/Low cleanup: destination lookup hides missing scene wiring, action
result data lacks validation, several stale enums/interfaces remain, and older style/debug/comment
issues are still present.

## Improvements Since Previous Review

- `ProjectStructure.md` no longer documents the deleted `MoveToPlanDestinationNode`; the previous
  documentation drift was resolved.
- `ActionType.DepositWheat`, `DepositWheatAction`, `WorkerCarryStorage`, wheat result data, and a
  warehouse destination were added without putting behavior logic into `WorkerAI`.
- `SampleScene.unity` now serializes `_initialCarryStorage` and maps `ActionType: 6` to
  `WarehouseObject` in `DestinationProvider`.
- `FarmerAI.prefab` still only owns `WorkerAI` and `WorkerMovementStats`; it does not own
  `WorkerActionSet`, which matches the current selector-side action-set design.
- Build verification still passes after the wheat workflow.

## Findings By Severity

### Critical

None found.

### High

- `Assets/Scripts/Actors/Worker/Actions/DepositWheatAction.cs`
  - Issue: deposited wheat is discarded instead of being persisted to a warehouse or storage owner.
  - Evidence: `Tick()` calls `context.CarryStorage.DepositAllWheat();` and ignores the returned
    amount. Search found `WarehouseObject` only in `SampleScene.unity`; no `WarehouseStorage`,
    `StoredWheat`, or equivalent script exists.
  - Risk: the worker appears to deposit wheat, but the reward is lost. This violates the gameplay
    requirement that work produces wheat that is stored in the warehouse.
  - Recommendation: add a dedicated warehouse inventory owner such as `WarehouseStorage` with
    `StoredWheat` and `AddWheat(int amount)`. Inject or provide it through an explicit provider or
    context capability, then have `DepositWheatAction` transfer the returned deposited amount into
    that storage.

### Medium

- `Assets/Scripts/Actors/Worker/WorkerDefaultActionSelector.cs`
  - Issue: missing destination data is treated the same as "movement is not needed".
  - Evidence: `TryGetMoveDestination()` returns `false` when `_destinationProvider` is missing or
    `TryGetDestinationPosition()` fails, and `TryCreatePlan()` then creates the target action
    without movement.
  - Risk: scene wiring mistakes can make `Work`, `Eat`, `Drink`, `Rest`, or `DepositWheat` execute
    in place. This is especially risky for `DepositWheat` because it can clear carried wheat without
    reaching a warehouse.
  - Recommendation: separate "already at destination" from "destination lookup failed". For actions
    that require a destination, fail plan creation when the provider or mapping is missing.

- `Assets/Scripts/Provider/DestinationProvider.cs`
  - Issue: `_destinationInfos` has no null guard, and `DestinationInfo` uses public mutable fields.
  - Evidence: `TryGetDestinationPosition()` immediately iterates `_destinationInfos.Length`;
    `DestinationInfo` exposes `public GameObject Destination;` and `public ActionType ActionType;`.
  - Risk: unassigned arrays can throw at runtime, and the data holder violates the serialized
    field convention.
  - Recommendation: guard `_destinationInfos == null`, use Unity truthiness for missing
    `GameObject`, and convert `DestinationInfo` to `[SerializeField] private` fields with read-only
    properties.

- `Assets/Scripts/Actors/Worker/Data/WorkerActionResultStatData.cs`
  - Issue: action result data has no validation for duplicate or missing `ActionType` entries.
  - Evidence: `TryGetEntry()` returns the first matching entry and silently returns `false` when an
    entry is missing. `WorkerActionSet` cannot create timer actions whose entries are absent.
  - Risk: designer data mistakes can quietly disable action creation or make duplicate entries hard
    to diagnose.
  - Recommendation: add editor-time validation, such as `OnValidate()` duplicate checks and explicit
    warnings for required action types used by the active `WorkerActionSet`.

- `Assets/Scripts/Actors/Worker/Data/WorkerActionResultStatData.cs`
  - Issue: the class name says `StatData`, but entries now include `_wheatDelta`, a carried-item
    reward.
  - Evidence: `WorkerActionResultStatEntry` contains `WorkerStatDelta` and `int WheatDelta`.
  - Risk: the runtime responsibility is still "action result tuning", but the name can mislead
    future edits into treating all rewards as stats or into adding unrelated reward data here.
  - Recommendation: either rename toward `WorkerActionResultData` / `WorkerActionResultEntry`, or
    document clearly that this asset owns per-action result tuning, including item deltas.

- `Assets/Scripts/Actors/Worker/Context/WorkerMover.cs`
  - Issue: field naming and Unity null checks do not match current convention, and
    `MoveTo(Vector3, Vector3)` is unused.
  - Evidence: private readonly fields are `target` and `movementStats`; null checks use
    `target == null` / `movementStats == null`; `MoveTo(...)` has no current callers.
  - Risk: stale movement APIs obscure the supported `StartMove` / `TickMove` path and style drift
    makes future changes less consistent.
  - Recommendation: rename fields to `_target` / `_movementStats`, use Unity truthiness, and remove
    the unused `MoveTo(...)` unless a near-term caller exists.

- `Assets/Scripts/Interface/IActionSet.cs`
  - Issue: `IActionSet<TKey>` does not represent actual selector needs.
  - Evidence: it exposes only `TryGetAction(TKey, out IAction)`, while selectors depend on concrete
    `WorkerActionSet` for destination-injected move rental, result data lookup, and returning
    actions.
  - Risk: the interface does not reduce coupling and can give a false sense of replaceability.
  - Recommendation: remove it until a real abstraction is needed, or replace it with a
    worker-specific interface that includes the actual action-set role.

### Low

- `Assets/Scripts/Actors/Worker/WorkerAI.cs` and
  `Assets/BehaviorGraph/CustomActionNode/EnsureWorkerHasActionNode.cs`
  - Issue: comments are encoding-corrupted and no longer readable.
  - Evidence: comments near `TryEnsureCurrentAction()` and `EnsureWorkerHasActionNode.OnStart()`
    render as broken text.
  - Recommendation: remove the comments or rewrite them as short ASCII comments.

- `Assets/Scripts/Actors/Worker/WorkerAI.cs`
  - Issue: stale commented-out `SetAction()` code remains.
  - Recommendation: remove the commented block; `SetPlan()` and `TryEnsureCurrentAction()` now
    express the intended API.

- `Assets/Scripts/Actors/Worker/Context/WorkerActionContext.cs`
  - Issue: stale commented-out owner code remains.
  - Evidence: `//public WorkerAI Owner { get; }` and `//Owner = owner;`.
  - Recommendation: remove the dead comments.

- `Assets/Scripts/Actors/Worker/WorkerAIManager.cs` and
  `Assets/Scripts/Actors/Worker/Actions/MoveAction.cs`
  - Issue: normal runtime flow logs through `Debug.Log`.
  - Evidence: spawn flow logs raw worker objects, spawn counts, and `MoveAction.Start()` logs
    `"Worker Moving Start"`.
  - Recommendation: remove these logs or gate them behind an explicit debug flag.

- `Assets/Scripts/Actors/Worker/Context/WorkerStats.cs`
  - Issue: fixed stat bounds are instance `readonly` fields with all-caps names.
  - Recommendation: convert to `private const float MinHungerValue`, etc.

- `Assets/Scripts/Actors/Worker/WorkerAIManager.cs`
  - Issue: `WorkerInitialCarryStorage._maxWheat = 5` is a serialized default inside manager code.
  - Risk: acceptable for the prototype, but if more carry limits or worker archetypes are added,
    manager defaults will become tuning data.
  - Recommendation: keep this as a short-term serialized default, or move worker starting/capacity
    values into a dedicated worker initial-state data asset when more worker types appear.

- `Assets/Scripts/Enum/ActionType.cs`, `Assets/Scripts/Enum/DestinationType.cs`,
  `Assets/Scripts/Enum/TickType.cs`
  - Issue: stale enum values remain.
  - Evidence: `ActionType.Sleep` has no registered action; `DestinationType` is unused by current
    `DestinationProvider`; `TickType.Custom` falls back to the normal ticker bucket.
  - Recommendation: remove or explicitly document these values.

- `Assets/Scripts/Interface/IAction.cs`, `Assets/Scripts/Interface/ITickable.cs`,
  `Assets/Scripts/Enum/ActionState.cs`, `Assets/Scripts/Enum/DestinationType.cs`
  - Issue: unused `using UnityEngine;` remains.
  - Recommendation: remove unused usings during cleanup.

- `Assets/Scripts/Manager/TickManager.cs`
  - Issue: serialized fields `slowtick`, `normaltick`, and `fasttick` do not follow `_camelCase`.
  - Recommendation: rename to `_slowTick`, `_normalTick`, and `_fastTick` with
    `[FormerlySerializedAs]` if scene data must be preserved.

## Findings By File

### `Assets/Scripts/Actors/Worker/WorkerAI.cs`

- Role is still correct: it owns active plan lifecycle, starts/ticks/cancels actions, and asks the
  injected selector for a plan.
- It does not know about wheat, warehouse storage, action result data, or `WorkerActionSet`.
- Cleanup needed: encoding-corrupted comments and stale commented-out `SetAction()`.

### `Assets/Scripts/Actors/Worker/WorkerAIManager.cs`

- Correctly creates `WorkerStats`, `WorkerCarryStorage`, `WorkerMover`, and
  `WorkerActionContext`, then injects context and selector into `WorkerAI`.
- Correctly resolves selector-side `WorkerActionSet` from selector instances instead of requiring
  the worker prefab to own it.
- Cleanup needed: normal-flow debug logs and possible future extraction of initial worker/carry
  tuning into a data asset if worker archetypes grow.

### `Assets/Scripts/Actors/Worker/WorkerDefaultActionSelector.cs`

- Correct layer for the new deposit decision. It checks critical needs, then full carry storage,
  then prepare-level needs, then work.
- It does not mutate stats or carried wheat.
- Main issue: failed destination lookup becomes "no movement needed", allowing in-place execution.

### `Assets/Scripts/Actors/Worker/WorkerActionSet.cs`

- Correctly owns action pooling and data-backed construction.
- `DepositWheatAction` registration is in the correct place.
- Remaining limitation: manual registration and dependency on concrete `WorkerActionSet` are
  documented but still a maintenance cost.

### `Assets/Scripts/Actors/Worker/Actions/WorkAction.cs`

- Correctly applies stat delta and wheat delta from injected data.
- Does not know selector logic or data asset references.

### `Assets/Scripts/Actors/Worker/Actions/DepositWheatAction.cs`

- Correctly models deposit as an `IAction` and uses injected duration/stat delta data.
- High issue: clears carried wheat but does not transfer it to any warehouse storage.

### `Assets/Scripts/Actors/Worker/Actions/MoveAction.cs`

- Correctly delegates movement to `WorkerMover`.
- Cleanup needed: normal-flow debug log and minor spacing/style consistency.

### `Assets/Scripts/Actors/Worker/Context/WorkerCarryStorage.cs`

- Responsibility is clean for carried wheat: current amount, capacity clamp, add, and deposit.
- It should not become the warehouse inventory owner. Keep persistent stored wheat in a separate
  warehouse component or storage service.

### `Assets/Scripts/Actors/Worker/Context/WorkerActionContext.cs`

- Exposes `WorkerCarryStorage` as a worker capability, which is acceptable for actions/selectors.
- Cleanup needed: stale commented-out owner code.

### `Assets/Scripts/Actors/Worker/Data/*.cs`

- `WorkerActionResultStatEntry` now carries `_wheatDelta`; this keeps work reward values out of
  action code.
- Naming is now slightly misleading because the data is no longer stat-only.
- Add validation for duplicate/missing action entries.

### `Assets/Scripts/Provider/DestinationProvider.cs`

- Role remains correct: lookup only.
- Needs null handling and serialized field style cleanup.

### `Assets/BehaviorGraph/CustomActionNode/*.cs`

- Behavior Graph nodes remain bridge code.
- `EnsureWorkerHasActionNode` depends on `WorkerAI.TryEnsureCurrentAction()` and does not search
  for selectors or action sets.
- Cleanup needed: encoding-corrupted comment in `EnsureWorkerHasActionNode`.

### `Assets/Scenes/SampleScene.unity`

- `WorkerAIManager` has `_initialCarryStorage` serialized with `_maxWheat: 5`.
- `DestinationProvider` maps `ActionType: 6` to `WarehouseObject`.
- `WarehouseObject` is only a destination object; it has no warehouse inventory component.

### `Assets/Prefab/FarmerAI.prefab`

- Contains `WorkerAI` and `WorkerMovementStats`.
- No `WorkerActionSet` is attached to the worker prefab, which matches current ownership rules.

## Cross-Cutting Findings

- The added wheat workflow respects the main dependency direction, but the economy loop is
  incomplete because deposited wheat has no storage owner.
- Destination lookup currently collapses three states into one `false`: no provider, no mapping,
  and already at destination. That makes runtime wiring mistakes hard to detect.
- `WorkerActionResultStatData` is becoming "action result data" rather than stat-only data. That is
  acceptable if kept scoped to per-action result tuning, but the name should be corrected before
  more non-stat rewards are added.
- Namespace policy remains inconsistent: `ActionType` / `ActionState` use `WorkerEnum`,
  `DestinationType` uses `ProviderEnum`, `TickType` and most worker scripts have no namespace.
- Several stale enum/interface items remain: `DestinationType`, `ActionType.Sleep`,
  `TickType.Custom`, and the too-narrow `IActionSet<TKey>`.

## Positive Notes

- `WorkerAI` remains free of work reward, warehouse, action-set, and data-asset responsibilities.
- New wheat behavior was represented as actions and context capability rather than being pushed
  into `WorkerAI`.
- `WorkerCarryStorage` has a clear carried-item responsibility and does not decide behavior.
- `WorkerDefaultActionSelector` owns the deposit decision without executing the deposit.
- `SampleScene.unity` has the selector-side `WorkerActionSet` and warehouse destination connected.
- `dotnet build Assembly-CSharp.csproj --no-restore` passes with 0 warnings and 0 errors.

## Recommended Next Actions

1. Add warehouse inventory persistence: `WarehouseStorage` or equivalent, plus explicit injection or
   provider access for `DepositWheatAction`.
2. Change destination planning so missing provider/mapping fails required destination actions
   instead of executing them in place.
3. Add validation to `WorkerActionResultStatData` for duplicate and missing action entries.
4. Rename `WorkerActionResultStatData` / `WorkerActionResultStatEntry` if the asset will continue
   owning non-stat rewards such as wheat.
5. Clean up `WorkerAI`, `EnsureWorkerHasActionNode`, and `WorkerActionContext` stale/broken
   comments.
6. Remove normal-flow debug logs in `WorkerAIManager` and `MoveAction`.
7. Normalize `DestinationProvider`, `WorkerMover`, `WorkerStats`, and `TickManager` style issues.
8. Remove or document stale `DestinationType`, `ActionType.Sleep`, `TickType.Custom`, and
   `IActionSet<TKey>`.
