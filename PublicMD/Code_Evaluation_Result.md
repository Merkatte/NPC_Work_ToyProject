# Code Evaluation Result

## Purpose

This document records a code quality and architecture review of the current Unity worker/NPC
prototype after the Claude Code CLI bridge-code fixes. The review checks the code against:

- `PublicMD/CodeConvention.md`
- `PublicMD/ProjectStructure.md`
- the runtime ownership rule that `WorkerAI` stays execution-oriented and `WorkerActionSet`
  stays selector/action-construction-side.

Production code was not modified as part of this review.

## Review Snapshot

- Date: 2026-06-17
- Scope: `.cs` files under `Assets/Scripts` and `Assets/BehaviorGraph/CustomActionNode`, plus
  relevant `Assets/BehaviorGraph/FarmerGraph.asset`, `SampleScene.unity`, and `FarmerAI.prefab`
  references.
- Sources: `CodeConvention.md`, `ProjectStructure.md`, current C# scripts, Behavior Graph asset
  references, scene/prefab serialized references.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and
  0 errors.

## Executive Summary

The previous High-risk Behavior Graph findings have been addressed. `EnsureWorkerHasActionNode`
no longer scans the worker `GameObject` for a selector; it delegates to
`WorkerAI.TryEnsureCurrentAction()`. The broken `MoveToPlanDestinationNode` was removed, and the
current `FarmerGraph.asset` references only `EnsureWorkerHasActionNode` and
`RunWorkerCurrentActionNode`, so there is no stale graph reference to the deleted node.

The core worker architecture is now in better shape: `WorkerAI` remains the active plan lifecycle
owner, action cost/reward data lives in `WorkerActionResultStatData`, and `WorkerActionSet`
remains outside the worker prefab. No Critical or High issues were found in the current code.

The main remaining work is cleanup and documentation alignment: `ProjectStructure.md` still
documents the removed `MoveToPlanDestinationNode`, several older style issues remain in
`DestinationProvider`, `WorkerMover`, `WorkerStats`, and `TickManager`, and stale enum/interface
items should be intentionally removed or justified.

## Improvements Since Previous Review

- `EnsureWorkerHasActionNode` now depends on `WorkerAI.TryEnsureCurrentAction()` instead of
  directly searching for selectors.
- `MoveToPlanDestinationNode` was deleted, removing both prior problems: same-GameObject
  `WorkerActionSet` lookup and direct `WorkerActionContext.SetPlan()` bypass.
- `WorkerAI` now exposes `TryEnsureCurrentAction()` as a single shared entry point for the
  autonomous update path and the Behavior Graph ensure node.
- `FarmerGraph.asset` does not reference the deleted direct-move node.
- Build verification still passes after the bridge changes.

## Findings By Severity

### Critical

None found.

### High

None found.

### Medium

- `PublicMD/ProjectStructure.md`
  - Issue: The document still lists and describes `MoveToPlanDestinationNode`, but the script and
    `.meta` file have been deleted.
  - Evidence: `ProjectStructure.md` still includes `MoveToPlanDestinationNode.cs` in the file
    tree and has a Behavior Graph role section for it.
  - Risk: future agents may reintroduce or reason around a node that no longer exists.
  - Recommendation: update `ProjectStructure.md` to remove `MoveToPlanDestinationNode` or add a
    note that direct movement bridging was removed.

- `Assets/Scripts/Provider/DestinationProvider.cs`
  - Issue: `TryGetDestinationPosition()` iterates `_destinationInfos` with no null guard, and
    checks `info.Destination == null` on a Unity `GameObject`.
  - Risk: missing Inspector data can throw a null-reference exception; the Unity null-check style
    also violates `CodeConvention.md`.
  - Recommendation: add an `_destinationInfos == null` guard and use `!info.Destination`.

- `Assets/Scripts/Provider/DestinationProvider.cs`
  - Issue: `DestinationInfo` exposes `Destination` and `ActionType` as public fields.
  - Risk: this violates the `[SerializeField] private` Inspector field convention and makes the
    data holder less consistent with the rest of the project.
  - Recommendation: convert fields to `[SerializeField] private` with read-only public
    properties.

- `Assets/Scripts/Actors/Worker/Context/WorkerMover.cs`
  - Issue: Unity object null checks use `target == null` and `movementStats == null`.
  - Risk: violates the project's Unity null-check convention.
  - Recommendation: use `!target` and `!movementStats` for Unity object references.

- `Assets/Scripts/Actors/Worker/Context/WorkerMover.cs`
  - Issue: private readonly fields `target` and `movementStats` do not follow the currently
    stated `_camelCase` private field rule in `CodeConvention.md`.
  - Recommendation: rename to `_target` and `_movementStats`, or clarify the convention if
    constructor-injected readonly fields should use plain `camelCase`.

- `Assets/Scripts/Actors/Worker/Context/WorkerMover.cs`
  - Issue: `MoveTo(Vector3, Vector3)` has no callers in the current codebase.
  - Risk: dead movement code obscures the actual movement path (`StartMove`/`TickMove`).
  - Recommendation: remove it unless a near-term caller exists.

- `Assets/Scripts/Actors/Worker/Context/WorkerStats.cs`
  - Issue: stat bounds are declared as `private readonly float MIN_*_VAL`.
  - Risk: fixed values should be `const`, and the naming style conflicts with the C# convention
    used elsewhere in the project.
  - Recommendation: rename to `private const float MinHungerValue`, etc.

- `Assets/Scripts/Interface/IActionSet.cs`
  - Issue: `IActionSet<TKey>` exposes only `TryGetAction(TKey, out IAction)`, while current
    selectors depend on concrete `WorkerActionSet` for destination-injected renting, result stat
    lookup, and action return.
  - Risk: the interface does not reduce coupling and does not represent the actual role.
  - Recommendation: remove it until a real abstraction is needed, or replace it with a
    worker-specific interface that matches actual selector needs.

### Low

- `Assets/Scripts/Actors/Worker/WorkerAIManager.cs`
  - Issue: normal spawn flow still contains several `Debug.Log` calls, including raw object
    logging.
  - Recommendation: remove or gate behind a debug flag.

- `Assets/Scripts/Actors/Worker/Actions/MoveAction.cs`
  - Issue: `Debug.Log("Worker Moving Start")` runs on every movement start.
  - Recommendation: remove or gate behind a debug flag.

- `Assets/Scripts/Actors/Worker/Actions/DrinkAction.cs`
  - Issue: file starts with a stray leading blank/space before `using UnityEngine;`.
  - Recommendation: remove the stray whitespace.

- `Assets/Scripts/Actors/Worker/Context/WorkerActionContext.cs`
  - Issue: commented-out `Owner` code remains without explanation.
  - Recommendation: remove the dead comment, or replace it with a specific TODO if the owner
    reference is intentionally deferred.

- `Assets/Scripts/Manager/TickManager.cs`
  - Issue: serialized fields `slowtick`, `normaltick`, and `fasttick` do not use `_camelCase`.
  - Recommendation: rename to `_slowTick`, `_normalTick`, `_fastTick` with
    `[FormerlySerializedAs]` if serialized scene data must be preserved.

- `Assets/Scripts/Enum/ActionState.cs`, `Assets/Scripts/Enum/DestinationType.cs`,
  `Assets/Scripts/Interface/IAction.cs`, `Assets/Scripts/Interface/ITickable.cs`
  - Issue: unused `using UnityEngine;`.
  - Recommendation: remove unused usings.

- `Assets/Scripts/Enum/TickType.cs`
  - Issue: `TickType.Custom` is defined but not handled in `TickManager.GetTickers()` and has no
    current callers.
  - Recommendation: remove `Custom` or implement/document its behavior.

## Findings By File

### `Assets/Scripts/Actors/Worker/WorkerAI.cs`

- `TryEnsureCurrentAction()` is an acceptable consolidation of existing behavior. `WorkerAI`
  still asks the selector for a plan, but this was already part of its documented lifecycle and
  does not move selection policy into `WorkerAI`.
- The Behavior Graph ensure node now has a safe public entry point without reading selector or
  action-set internals.
- Minor style note: the newly added explanatory comments are useful, but production code should
  keep comments concise and encoding-stable.

### `Assets/BehaviorGraph/CustomActionNode/EnsureWorkerHasActionNode.cs`

- Previous High finding is resolved. The node now depends only on `WorkerAI` and delegates action
  selection/plan assignment through `TryEnsureCurrentAction()`.
- This aligns better with the bridge role: the node no longer scans for selectors or action sets.

### `Assets/BehaviorGraph/CustomActionNode/RunWorkerCurrentActionNode.cs`

- No issues found. It remains a thin bridge over `WorkerAI.TickCurrentAction()`.

### `Assets/BehaviorGraph/CustomActionNode/MoveToPlanDestinationNode.cs`

- The file was deleted. This removes the prior action-set lookup and direct context plan-setting
  defects.
- `FarmerGraph.asset` currently has no reference to this node, so the deletion appears safe from
  the checked serialized graph data.
- `ProjectStructure.md` still needs to be updated to match the deletion.

### `Assets/Scripts/Actors/Worker/WorkerAIManager.cs`

- Correctly does not require worker prefabs to own `WorkerActionSet`.
- Correctly resolves selector-side `WorkerActionSet` for selectors that implement
  `IWorkerActionSelectorSetup`.
- Cleanup remains: several debug logs should be removed or gated before production-like use.

### `Assets/Scripts/Actors/Worker/WorkerDefaultActionSelector.cs`

- Role remains clean: it decides intent and builds plans, while actions execute.
- `CriticalThreshold` and `PrepareThreshold` are named constants.
- Future predictive work-capacity logic should continue using `WorkerActionResultStatData`
  through `WorkerActionSet`, not concrete action classes.

### `Assets/Scripts/Actors/Worker/WorkerActionSet.cs`

- Pooling and data-backed action creation remain in the right place.
- Manual action registration remains a documented limitation.
- The implemented interface is still too narrow for actual selector usage.

### `Assets/Scripts/Actors/Worker/Actions/*.cs`

- Work/Eat/Drink/Rest still avoid hardcoded duration/stat effect values.
- `MoveAction` still contains a normal-flow debug log.
- Empty `Cancel()` bodies on timer actions are acceptable because those actions hold no external
  resources yet.

### `Assets/Scripts/Actors/Worker/Data/*.cs`

- Data responsibility remains clean: ScriptableObject lookup and serializable value objects only.
- Future improvement: add editor validation for duplicate `ActionType` entries or missing
  required entries.

### `Assets/Scripts/Actors/Worker/Context/*.cs`

- `WorkerStats.Apply(WorkerStatDelta)` remains a clean generic stat mutation point.
- `WorkerMover` still has null-check style, field naming, and dead method cleanup items.
- `WorkerActionContext` still has stale commented-out owner code.

### `Assets/Scripts/Provider/DestinationProvider.cs`

- Role is correct: destination lookup only.
- Needs null handling and field-style cleanup.

## Cross-Cutting Findings

- Behavior Graph High-risk ownership drift is currently resolved for checked assets because the
  broken direct movement bridge is gone and the ensure node routes through `WorkerAI`.
- Documentation drift now matters: `ProjectStructure.md` still describes the deleted direct move
  node.
- Namespace policy remains inconsistent. `ActionState` and `ActionType` use `WorkerEnum`,
  `DestinationType` uses `ProviderEnum`, `TickType` has no namespace, and most worker scripts
  have no namespace.
- `DestinationType` is still stale because destination lookup is `ActionType` based.
- `ActionType.Sleep` is still defined but no action is registered for it.
- `TickType.Custom` is still defined but not implemented as a distinct tick bucket.
- `FarmerAI.prefab` should not own `WorkerActionSet`; checked serialized references still place
  `WorkerActionSet` on the selector setup in `SampleScene`, matching current architecture.

## Positive Notes

- The latest bridge change reduced dependencies in `EnsureWorkerHasActionNode`.
- Removing `MoveToPlanDestinationNode` eliminated a concrete plan ownership violation.
- `WorkerAI` remains close to the documented execution/lifecycle role.
- `WorkerActionResultStatData` continues to provide a single source of truth for action duration
  and stat deltas.
- `WorkerActionPlan` remains simple and does not accumulate behavior logic.
- `WorkerMovementStats` follows serialized field naming and clamps exposed values with named
  constants.

## Recommended Next Actions

1. Update `PublicMD/ProjectStructure.md` to remove or mark the deleted `MoveToPlanDestinationNode`.
2. Clean up `DestinationProvider` null handling and field style.
3. Normalize `WorkerMover` null checks, field names, and remove the unused `MoveTo` overload.
4. Convert `WorkerStats` bounds to `private const float Min...` names.
5. Remove or gate normal-flow debug logs in `WorkerAIManager` and `MoveAction`.
6. Fix `TickManager` serialized field names with `[FormerlySerializedAs]` if needed.
7. Decide namespace policy for the worker domain and enum folder.
8. Verify or remove stale `DestinationType`, `ActionType.Sleep`, `TickType.Custom`, and the
   currently too-narrow `IActionSet<TKey>`.
