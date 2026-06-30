# Code Evaluation Result

## Purpose
Read-only lead-programmer review of IMP-018 against the NPC/worker architecture, responsibility boundaries, lifecycle safety, Unity serialized-reference safety, and code convention rules. Production files were not modified.

## Review Snapshot
- Date: 2026-06-30
- Scope: Changed files only, plus directly affected dependency surfaces:
  - `Assets/Scenes/SampleScene.unity`
  - `Assets/Scripts/Provider/DestinationProvider.cs`
  - `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs`
  - `Assets/Scripts/Actors/AI/Friendly/Common/Actions/MoveAction.cs`
  - `Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerStats.cs`
  - Direct surfaces: `GuardActionSelector`, `FarmerActionSelector`, `WorkerActionSet`, `WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, Behavior Graph bridge nodes, `FarmerAI.prefab`, action result data.
- Sources:
  - `.codex/skills/reviewing-npc-work-code/SKILL.md`
  - `.codex/skills/reviewing-npc-work-code/references/review-workflow.md`
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - Previous `PublicMD/Code_Evaluation_Result.md`
  - Current `git status`, `git diff`, targeted source reads, and targeted `rg` checks.
- Verification:
  - Ran `git status --short`.
  - Ran targeted `git diff` and `git diff --check` for the changed files.
  - Ran targeted searches for selector scene references, destination provider usage, remaining debug logs, stat bound constants, prefab capability wiring, and Behavior Graph bridge ownership.
  - Did not rerun `dotnet build Assembly-CSharp.csproj --no-restore` because this review sandbox is read-only and build output may write `bin/obj` or generated files. The implementation summary reports build result as 0 warnings and 0 errors.

## Executive Summary
IMP-018 correctly resolves the four targeted cleanup items in the changed files.

- Fix A is confirmed: the Guard selector entry in `SampleScene.unity` now points to `GuardActionSelector` fileID `937587168` instead of the Farmer selector fileID `568873992`.
- Fix B is confirmed: `DestinationProvider._destinationInfos` now defaults to `Array.Empty<DestinationInfo>()`.
- Fix C is confirmed for the changed worker files: the normal-path `Debug.Log` calls were removed from `WorkerAIManager` and `MoveAction`, and missing configuration paths now use `Debug.LogWarning`.
- Fix D is confirmed: `WorkerStats` stat bounds were consolidated into two `const` values with conventional naming.

One high-severity serialized composition issue remains: after Fix A, selecting Guard will instantiate the correct selector, but the Guard selector template is still not composition-ready for guard combat because its destination provider and enemy mask are unset, and the spawned worker prefab still lacks an `AttackPower` component.

## Improvements Since Previous Review
- Guard selector entry wiring is improved: `WorkerSelectorType.Guard` now references the `GuardActionSelector` component in `SampleScene.unity:949-950`.
- `DestinationProvider` no longer has a null serialized-array default in code.
- Worker spawn and movement normal paths no longer emit noisy `Debug.Log` output.
- Worker missing-configuration diagnostics now use warnings in the reviewed manager paths.
- `WorkerStats` no longer stores fixed stat bounds as six readonly instance fields.

## Findings By Severity

### Critical
None found.

### High

#### Finding 1: Guard selector entry is now corrected, but the Guard template remains incomplete
- Severity: High
- Category: Unity scene/prefab serialized-reference safety; correctness; role composition
- Location:
  - `Assets/Scenes/SampleScene.unity:755-761`
  - `Assets/Scenes/SampleScene.unity:949-950`
  - `Assets/Scripts/Actors/AI/Friendly/Guard/GuardActionSelector.cs:38-44`
  - `Assets/Scripts/Actors/AI/Friendly/Guard/GuardActionSelector.cs:56-59`
  - `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs:143-152`
  - `Assets/Prefab/FarmerAI.prefab:139`
  - `Assets/Prefab/FarmerAI.prefab:151`
- Evidence:
  - The manager entry for Guard now correctly uses `_selectorSource: {fileID: 937587168}` at `SampleScene.unity:950`, and fileID `937587168` is `Assembly-CSharp::GuardActionSelector` at `SampleScene.unity:755`.
  - The Guard selector template still has `_destinationProvider: {fileID: 0}` at `SampleScene.unity:757`.
  - The Guard selector template still has `_enemyMask.m_Bits: 0` at `SampleScene.unity:759-761`.
  - `GuardActionSelector.Awake()` only falls back to `TryGetComponent(out _destinationProvider)` on the same selector GameObject at `GuardActionSelector.cs:38-39`; the scene template object contains `WorkerActionSet` and `GuardActionSelector`, not `DestinationProvider`.
  - `GuardActionSelector` has `Init(DestinationProvider destinationProvider)` at `GuardActionSelector.cs:56-59`, but `WorkerAIManager` only calls `IWorkerActionSelectorSetup.Init(actionSet)` at `WorkerAIManager.cs:143-152`; it does not inject a destination provider.
  - `GuardActionSelector` resolves attack power with `GetComponentInParent<IAttackPower>()` at `GuardActionSelector.cs:42`. The configured worker prefab serializes `WorkerAI` at `FarmerAI.prefab:139` and `WorkerMovementStats` at `FarmerAI.prefab:151`; no `AttackPower` component is present in the prefab evidence.
- Description:
  Fix A corrected the selector type/source mismatch, but enabling Guard selection still will not produce a functional guard role. With an empty enemy mask, patrol scanning cannot acquire enemies. Without an `AttackPower` component on the spawned worker hierarchy, attack plan creation fails. Without a destination provider, Guard need-recovery planning cannot move to configured recovery destinations and falls back to running those actions in place.
- Recommended fix:
  Wire the Guard selector template with the shared `DestinationProvider`, set a non-empty enemy layer mask, and provide guard-capable workers with an `AttackPower` component or explicit injected attack capability. If Guard uses a different prefab than Farmer, make that prefab distinction explicit at the resident composition/manager boundary.
- Impact if unfixed:
  Switching `_initialSelectorType` to Guard can instantiate the correct selector but still produce a guard that patrols without detecting enemies, cannot attack when a target is available, and does not use facility destinations for need recovery. This leaves the scene in a misleading “wired but not runnable” state for Guard/combat validation.

### Medium
None found.

### Low
None found.

## Priority Review Results
- Priority 1, architecture and responsibility placement: No issue found in the IMP-018 changed C# files. `WorkerAIManager` remains composition/initialization focused, `WorkerAI` remains lifecycle/tick focused, `DestinationProvider` remains lookup-only, `MoveAction` remains execution-only, and `WorkerStats` remains state/clamp-only.
- Priority 2, correctness, lifecycle, cancellation, and regression risks: No new lifecycle or cancellation regression found in the changed C# files. `MoveAction.Cancel()` still stops movement, stops the move animation, and clears the rented destination. `WorkerAI` still returns rented actions on success, failure, disable, and death paths.
- Priority 3, Unity scene, prefab, component, and serialized-reference safety: One high issue remains. Guard now references the correct selector component, but the Guard selector template and worker prefab are not fully wired for combat behavior.
- Priority 4, code convention, maintainability, dead code, and magic values: No issue found in the changed files. The fixed stat bounds now follow the project preference for fixed values as constants, and the removed logs reduce normal-path console noise.

## Findings By File
- `Assets/Scenes/SampleScene.unity`: Fix A is correct at `SampleScene.unity:949-950`. Remaining issue: the Guard selector template at `SampleScene.unity:756-761` still has missing/empty role dependencies.
- `Assets/Scripts/Provider/DestinationProvider.cs`: No issue found. `_destinationInfos` now defaults to `Array.Empty<DestinationInfo>()`, and both `Try...` methods still fail cleanly by returning `false`.
- `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs`: No issue found in the changed lines. Normal-path logs were removed, missing configuration logs are warnings, and selector setup responsibility remains in the manager.
- `Assets/Scripts/Actors/AI/Friendly/Common/Actions/MoveAction.cs`: No issue found. Removing the start log does not change movement, failure, animation stop, or cancellation behavior.
- `Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerStats.cs`: No issue found. The two constant stat bounds preserve the previous clamp behavior for hunger, thirst, and fatigue.
- `Assets/Scripts/Actors/AI/Friendly/Guard/GuardActionSelector.cs`: Directly affected by the scene selector fix. The class is in the correct selector layer, but the scene/prefab composition for its dependencies is incomplete.
- `Assets/Prefab/FarmerAI.prefab`: Directly affected by Guard composition. The prefab is suitable for current farmer movement/stat flow but is not guard-combat capable because it lacks serialized `AttackPower` evidence.

## Cross-Cutting Findings
- The scene now points the Guard role to the right selector type, which is a meaningful improvement. The remaining risk is composition completeness: selector entry correctness, selector dependencies, target layer masks, and worker capabilities need to be validated together.
- No architecture drift was introduced by IMP-018. The changes do not move behavior decisions into actions, movement into selectors, or plan lifecycle into providers/context.
- The reviewed debug logging cleanup is appropriately scoped. Remaining `Debug.LogWarning` calls in worker/action paths represent missing injected/configured dependencies rather than normal gameplay spam.

## Positive Notes
- `WorkerAIManager.TryCreateSelector` still keeps selector instantiation and action-set setup out of `WorkerAI`.
- `DestinationProvider` remains a simple provider and does not absorb selector priority or movement behavior.
- `MoveAction` still owns only movement execution from injected plan/action data and does not choose destinations.
- `WorkerStats` now uses compact fixed bounds without altering the public stat behavior.
- `git diff --check` reported no whitespace errors for the reviewed changed files.

## Recommended Next Actions
1. Complete Guard scene/prefab composition: wire `_destinationProvider`, set `_enemyMask`, and add/inject `AttackPower` for guard-capable workers.
2. Add a validation path for selector templates that checks role-specific serialized dependencies before runtime selection.
3. Rerun `dotnet build Assembly-CSharp.csproj --no-restore` outside the read-only review sandbox if the launcher needs independent build verification attached to this review.

## Final Verdict
Conditionally approved for the four IMP-018 fixes. The changed C# files are architecturally clean and do not introduce lifecycle or convention regressions.

Not approved for Guard/combat runtime validation yet. The Guard selector reference is fixed, but the Guard template and worker prefab still lack required serialized/capability wiring for functional combat behavior.