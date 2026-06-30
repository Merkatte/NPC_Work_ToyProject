# Code Evaluation Result

## Purpose
Read-only lead-programmer review of the Unity worker/NPC code against `PublicMD/CodeConvention.md`, `PublicMD/ProjectStructure.md`, and the `reviewing-npc-work-code` workflow. Production code was not modified.

## Review Snapshot
- Date: 2026-06-30
- Scope: All `.cs` files under `Assets/Scripts`, Behavior Graph bridge nodes under `Assets/BehaviorGraph/CustomActionNode`, `Assets/Scenes/SampleScene.unity`, `Assets/Prefab/FarmerAI.prefab`, `Assets/BehaviorGraph/FarmerGraph.asset`, and `Assets/Scripts/Actors/AI/Friendly/Common/Data/DefaultWorkerActionResultStatData.asset`.
- Sources: `PublicMD/CodeConvention.md`, `PublicMD/ProjectStructure.md`, previous `PublicMD/Code_Evaluation_Result.md`, `.codex/skills/reviewing-npc-work-code/SKILL.md`, `.codex/skills/reviewing-npc-work-code/references/review-workflow.md`, code, scene, prefab, and data asset files listed above.
- Verification: Ran `dotnet build Assembly-CSharp.csproj --no-restore` successfully with 0 warnings and 0 errors. Ran targeted `rg` checks for stale enums, destination types, repeated component lookup, debug logs, TODOs, selector/action ownership, serialized scene references, prefab component wiring, and project-file compile includes.
- Workspace note: The working tree already contains unrelated modified and untracked files, including skill folders and project/document files. This report only updates `PublicMD/Code_Evaluation_Result.md`.

## Executive Summary
The core worker architecture remains directionally sound. `WorkerAI` still owns plan lifecycle and ticking, selectors still build `WorkerActionPlan` instances, actions execute behavior through `WorkerActionContext`, and animation playback stays behind `IAnimPlayer`.

The main current risk is serialized scene wiring for the Guard role. `WorkerAIManager` maps both Default and Guard selector entries to the Farmer selector template, so selecting Guard would instantiate `FarmerActionSelector` instead of `GuardActionSelector`. The actual Guard template also has no destination provider, an empty enemy mask, and the worker prefab does not include `AttackPower`, so guard combat behavior is not currently runnable without additional wiring.

The previous recruitment build-coverage finding is resolved: recruitment scripts now live under `Assets/Scripts/Systems/Recruitment`, are included in `Assembly-CSharp.csproj`, and the project builds. The recruitment payment/spawn transaction issue remains.

## Improvements Since Previous Review
- Recruitment files are now under the convention-approved `Assets/Scripts/Systems/Recruitment` folder instead of the old `Assets/Scripts/Recruitment` path used in the previous report.
- `Assembly-CSharp.csproj` includes the recruitment scripts and Guard/Behavior Graph additions, and `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and 0 errors.
- Worker behavior additions for guard/combat are placed in selectors, actions, context-side helper data, and combat/facility systems rather than being pushed into `WorkerAI`.

## Findings By Severity

### Critical
None found.

### High
#### Finding 1: Guard selector scene wiring points to the Farmer selector template
- Severity: High
- File: `Assets/Scenes/SampleScene.unity:947`
- Evidence: `WorkerAIManager` has `_selectorType: 2` (Guard) at `Assets/Scenes/SampleScene.unity:949`, but `_selectorSource` points to `{fileID: 568873992}` at line 950. That fileID is the `FarmerActionSelector` component at lines 456-460. The actual `GuardActionSelector` component exists separately at lines 753-755.
- Why it matters: `WorkerAIManager.TryCreateSelector` instantiates the serialized selector source and only checks that it implements `IActionSelector<WorkerActionContext, WorkerActionPlan>` (`Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs:149-170`). If `_initialSelectorType` is changed to Guard, the manager will create a Farmer selector and run farmer work/deposit behavior instead of guard patrol/combat behavior.
- Recommendation: Change the Guard selector entry to reference the `GuardActionSelector` component (`fileID: 937587168`) or a dedicated Guard selector prefab/template. Add a scene validation pass or editor assertion that selector entry type and component type match.

#### Finding 2: Guard combat template is incomplete even if selected
- Severity: High
- Files: `Assets/Scenes/SampleScene.unity:756`, `Assets/Scenes/SampleScene.unity:757`, `Assets/Scenes/SampleScene.unity:759`, `Assets/Prefab/FarmerAI.prefab:139`
- Evidence: The Guard template has `_actionSet: {fileID: 0}` and `_destinationProvider: {fileID: 0}` at scene lines 756-757, and `_enemyMask.m_Bits: 0` at lines 759-761. `GuardActionSelector.Awake()` can recover a same-object `WorkerActionSet` but only tries to find a same-object `DestinationProvider` (`GuardActionSelector.cs:35-39`), and `IWorkerActionSelectorSetup.Init` injects only the action set (`WorkerAIManager.cs:156-164`). `GuardActionSelector` resolves `_attackPower` with `GetComponentInParent<IAttackPower>()` (`GuardActionSelector.cs:41-44`), but `FarmerAI.prefab` contains only `WorkerAI` and `WorkerMovementStats` components at lines 139 and 151; no `AttackPower` or `Health` component is serialized on the worker prefab.
- Why it matters: Once the Guard entry is corrected, needs recovery destinations will be unavailable, enemy scanning will never match anything with a zero mask, and attack plan creation will fail because `_attackPower` is null. The role will degrade to failed or inert plans instead of patrol/seek/attack behavior.
- Recommendation: Wire the Guard template to the shared `DestinationProvider`, set a non-empty enemy layer mask, and add or inject an `AttackPower` component for guard-capable workers. If guards need different prefabs than farmers, represent that explicitly in `WorkerAIManager` or the recruitment/spawn composition boundary.

### Medium
#### Finding 3: Recruitment payment is not transaction-safe when spawn fails
- Severity: Medium
- File: `Assets/Scripts/Systems/Recruitment/RecruitmentManager.cs:67`
- Evidence: `TryRecruit` calls `_costPolicy.TryPay(candidate.RecruitCost)` at lines 67-68 before checking `_residentSpawner == null` at line 70 and before `_residentSpawner.TrySpawnResident(candidate)` at line 80. The comment at lines 50-54 also acknowledges the missing refund path.
- Why it matters: The current `AlwaysAffordableCostPolicy` is non-mutating, but a real wallet-backed implementation can deduct gold and then fail to spawn. The candidate is retained, but payment state would already be mutated.
- Recommendation: Redesign the cost policy before adding a wallet implementation. Use reserve/commit/rollback, a refund token, or a command-level transaction that validates spawn preconditions before committing payment.

#### Finding 4: Combat scanning performs component lookup in action ticks
- Severity: Medium
- Files: `Assets/Scripts/Actors/AI/Friendly/Common/Actions/SeekAction.cs:50`, `Assets/Scripts/Actors/AI/Friendly/Common/Actions/PatrolAction.cs:65`, `Assets/Scripts/Actors/AI/Friendly/Common/Combat/EnemyScanner.cs:41`
- Evidence: `SeekAction.Tick()` and `PatrolAction.Tick()` call `EnemyScanner.TryFindNearest`, and `EnemyScanner` calls `col.GetComponent<IDamageable>()` for each collider returned by `Physics2D.OverlapCircle`.
- Why it matters: `CodeConvention.md` says to avoid repeated `GetComponent` in tick methods. This is currently bounded by a 10-collider buffer, but patrol scans every action tick and can become a hot path as enemy counts grow.
- Recommendation: Cache damageable targets on an enemy component, maintain a target registry, or introduce a small `DamageableCollider` bridge so scanning can avoid repeated interface component lookup in the action tick path.

### Low
#### Finding 5: `DestinationProvider` lacks null-array safety
- Severity: Low
- File: `Assets/Scripts/Provider/DestinationProvider.cs:7`
- Evidence: `_destinationInfos` is serialized but not initialized. Both lookup methods iterate `_destinationInfos.Length` at lines 11 and 28. A provider component with the field left unset can throw before returning a clean `false`.
- Why it matters: Providers are scene-wired dependencies used by selectors. Their `Try...` methods should treat missing data as a valid runtime failure path, not an exception.
- Recommendation: Initialize `_destinationInfos` to `Array.Empty<DestinationInfo>()` or guard `if (_destinationInfos == null)` in both lookup methods.

#### Finding 6: Worker debug logging is noisy for normal gameplay paths
- Severity: Low
- Files: `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs:40`, `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs:45`, `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs:67`, `Assets/Scripts/Actors/AI/Friendly/Common/Actions/MoveAction.cs:28`
- Evidence: Worker spawn and movement paths emit plain `Debug.Log` messages during normal operation.
- Why it matters: These are expected runtime paths and can flood the console once multiple workers spawn or move, making real warnings harder to see.
- Recommendation: Remove normal-path logs, downgrade to editor-only diagnostics, or gate them behind a serialized debug flag. Keep `Debug.LogWarning` for missing required configuration.

#### Finding 7: Stale enum/API surface remains
- Severity: Low
- Files: `Assets/Scripts/Enum/ActionType.cs:9`, `Assets/Scripts/Enum/DestinationType.cs:5`, `Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerMover.cs:56`
- Evidence: `ActionType.Sleep` remains even though current need logic uses `Rest`; `DestinationType` exists while current destination lookup uses `ActionType`; `WorkerMover.MoveTo` is present but no current caller was found in `Assets/Scripts`.
- Why it matters: Stale API surface makes selector/action ownership harder to infer and can mislead future implementation work.
- Recommendation: Remove unused enum values/types and unused mover methods once no scene serialization or pending branch depends on them.

#### Finding 8: Older style constants and comments remain in core worker files
- Severity: Low
- Files: `Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerStats.cs:5`, `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAI.cs:18`, `Assets/Scripts/Actors/AI/Friendly/Common/Actions/SeekAction.cs:36`
- Evidence: `WorkerStats` uses readonly instance fields named `MIN_HUNGER_VAL` through `MAX_FATIGUE_VAL` instead of `const`/`static readonly` project style. Several comments in worker/combat files render as corrupted mojibake in the current checkout, while project docs render Korean correctly.
- Why it matters: This is not a runtime bug, but it reduces maintainability and violates the convention preference for clear comments and fixed-value constants.
- Recommendation: Convert fixed stat bounds to `const` or `static readonly` with conventional names, and either restore comments with a consistent UTF-8 encoding or remove comments that no longer add readable context.

## Findings By File
- `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAI.cs`: No ownership drift found. It still owns plan lifecycle and action ticking. Low cleanup: stale commented-out `SetAction` and corrupted comments.
- `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs`: Selector instantiation role is appropriate. Scene data currently misuses its selector entry contract for Guard. Low cleanup: normal-path `Debug.Log` noise.
- `Assets/Scripts/Actors/AI/Friendly/Common/WorkerActionPlan.cs`: No issue found. It remains a small queued runtime instruction object.
- `Assets/Scripts/Actors/AI/Friendly/Common/WorkerActionSet.cs`: No issue found in ownership. It owns pools/action creation and keeps actions out of `WorkerAI`.
- `Assets/Scripts/Actors/AI/Friendly/Common/Actions/*.cs`: Behavior remains in actions. Medium issue in scan-dependent actions due repeated component lookup through `EnemyScanner`. Low cleanup in `MoveAction` logging.
- `Assets/Scripts/Actors/AI/Friendly/Farmer/FarmerActionSelector.cs`: No current issue found. It builds plans and does not execute action behavior.
- `Assets/Scripts/Actors/AI/Friendly/Guard/GuardActionSelector.cs`: Code is structurally in the correct selector layer, but scene/prefab wiring currently prevents the role from working.
- `Assets/Scripts/Actors/AI/Friendly/Cook/CookActionSelector.cs`: No issue found for a placeholder boundary.
- `Assets/Scripts/Actors/AI/Friendly/Common/Context/*.cs`: Responsibilities are mostly clean. Low cleanup in `WorkerStats` constants and unused `WorkerMover.MoveTo`.
- `Assets/Scripts/Actors/AI/Friendly/Common/Data/*.cs`: No issue found. Data asset shape remains single-purpose for action result tuning.
- `Assets/Scripts/Actors/AI/Friendly/Common/Combat/*.cs`: `CombatTargetHolder` and `PatrolParams` are simple selector/action handoff types. `EnemyScanner` has the repeated component lookup issue.
- `Assets/Scripts/Animation/**/*.cs`: No issue found. `IAnim` definitions are stateless and `ActorAnimationController` owns per-worker tween state.
- `Assets/Scripts/Provider/DestinationProvider.cs`: Low null-array safety issue. Current scene references are populated for farmer needs/work/deposit and seek, but the component should fail cleanly when not configured.
- `Assets/Scripts/Systems/Combat/*.cs`: `Health` and `AttackPower` are placed in a domain system folder. Scene/prefab wiring does not yet attach `AttackPower` to guard workers.
- `Assets/Scripts/Systems/Facility/*.cs`: No direct issue found. `WarehouseInventory` is a clear facility inventory component.
- `Assets/Scripts/Systems/Recruitment/*.cs`: Medium transaction issue remains in `RecruitmentManager.TryRecruit`. Folder placement and compile coverage are now correct.
- `Assets/Scripts/Interface/*.cs` and `Assets/Scripts/Enum/*.cs`: Low stale enum/API issue. Interfaces otherwise match current shared-domain contracts.
- `Assets/Scripts/Manager/TickManager.cs`: No runtime issue found in isolation. It is currently not integrated with worker stats, consistent with `ProjectStructure.md` known notes.
- `Assets/BehaviorGraph/CustomActionNode/*.cs`: No ownership issue found. Nodes remain thin bridges to `WorkerAI`.
- `Assets/Scenes/SampleScene.unity`: High Guard selector wiring issue and incomplete Guard template wiring.
- `Assets/Prefab/FarmerAI.prefab`: Suitable for farmer movement/action execution, but not currently guard-combat capable because it lacks an `AttackPower` component.

## Cross-Cutting Findings
- Scene/prefab wiring needs the same review standard as C# for selector roles. The code can compile and still instantiate the wrong selector behavior because selector entries are plain `MonoBehaviour` references.
- Guard combat is partially implemented in code but not composition-ready. Role selection, attack capability, target layer mask, and need destinations must be validated together.
- Several older comments are unreadable in the current checkout. Since comments are supposed to explain non-obvious behavior, unreadable comments should be treated as stale code-adjacent maintenance debt.

## Positive Notes
- `WorkerAI` remains execution-focused and has not absorbed selector priorities, movement math, animation lookup, or action implementation details.
- `WorkerActionSet` centralizes action construction and return/reset handling, including parameterized Move/Deposit/Seek/Patrol/Attack action rental.
- Farmer deposit now routes through `IInventory`, so wheat deposit is no longer just clearing carry state without facility acceptance.
- Animation ownership is clean: actions request animation through context, and only `ActorAnimationController` owns active tweens.
- Recruitment is now correctly scoped under `Systems/Recruitment` and compiles in the project.

## Recommended Next Actions
1. Fix `SampleScene.unity` selector entries so Guard references `GuardActionSelector`, then wire Guard `_destinationProvider`, `_enemyMask`, and worker attack capability.
2. Add an editor/runtime validation path for `WorkerAIManager` selector entries: expected role type, non-null selector source, required `WorkerActionSet`, and role-specific dependencies.
3. Redesign recruitment payment around reserve/commit/rollback before connecting a real wallet.
4. Remove or gate normal-path worker debug logs.
5. Clean stale enum/API/comment debt after confirming no serialized references depend on it.

## Final Verdict
Not approved for Guard/combat role usage yet because scene/prefab wiring currently instantiates the wrong selector and leaves the actual Guard template incomplete. The farmer worker flow and core action architecture are in acceptable shape for the current prototype, and the project compiles successfully.
