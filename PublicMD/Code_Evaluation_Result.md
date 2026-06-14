# Code Evaluation Result

## Purpose

This document records a convention/architecture compliance review of the current C# scripts
against `PublicMD/CodeConvention.md` and `PublicMD/ProjectStructure.md`.

This is a **read-only analysis snapshot**. No code was modified as part of this review.
Items listed here may already be fixed by the time this is read — verify against the current
code before acting on any item.

Scope reviewed: all `.cs` files under `Assets/Scripts` and `Assets/BehaviorGraph/CustomActionNode`
(26 files total), checked against:

- `PublicMD/CodeConvention.md` (naming, field style, null-check style, structure, namespaces,
  responsibility boundaries, action/plan ownership rules)
- `PublicMD/ProjectStructure.md` (per-file "Role" / "Should not" rules, Known Design Notes)

---

## Findings By File

### `Assets/Scripts/Actors/Worker/WorkerAI.cs`

- `[SerializeField]` fields `actionSelectorSource`, `movementStats`, `tickManager`,
  `statsTickType` do not use the `_camelCase` prefix required for private serialized fields
  (CodeConvention "Field Style"). Suggested: `_actionSelectorSource`, `_movementStats`,
  `_tickManager`, `_statsTickType`.
- `//private void OnEnable() => tickManager?.RegisterTick(statsTickType, stats);` is commented
  out with no explanation. Compare with the `SetAction(...)` commented block just below it,
  which has an explanatory comment justifying why it is disabled (CodeConvention "Comments").
  This line should either get a similar explanation or be removed.
- `Awake()` directly constructs `WorkerStats`, `WorkerMover`, and `WorkerActionContext`, and
  `statsTickType` is currently unused, and `TickManager` registration is commented out. These
  are **already documented** in ProjectStructure.md "Known Design Notes" / "Current note" —
  not new issues, listed here only for completeness.

### `Assets/Scripts/Actors/Worker/WorkerDefaultActionSelector.cs`

- `[SerializeField]` fields `actionSet`, `destinationProvider` do not use the `_camelCase`
  prefix (CodeConvention "Field Style"). Suggested: `_actionSet`, `_destinationProvider`.
- The threshold value `70f` is repeated 3 times in `TrySelectAction` (thirst, hunger, fatigue
  checks). CodeConvention "Field Style" recommends `const`/`static readonly` for fixed values
  repeated across methods. Suggested: `private const float NeedThreshold = 70f;`.

### `Assets/Scripts/Actors/Worker/Context/WorkerStats.cs`

- `MIN_HUNGER_VAL`, `MAX_HUNGER_VAL`, `MIN_THIRST_VAL`, `MAX_THIRST_VAL`, `MIN_FATIGUE_VAL`,
  `MAX_FATIGUE_VAL` are declared `private readonly float ... = <literal>`. These never change
  after being set, so CodeConvention "Field Style" suggests `const` instead of `readonly`, and
  PascalCase naming (e.g. `MinHungerVal`) instead of `ALL_CAPS`.
- `Work(float amount)` (single-parameter overload, `Fatigue += amount`) does not appear to be
  called anywhere; `WorkAction` calls the 3-parameter `Work(hunger, thirst, fatigue)` overload
  instead. Verify usage and remove if dead.

### `Assets/Scripts/Actors/Worker/Context/WorkerMover.cs`

- `TickMove()`: `if (target == null || movementStats == null || ...)` — `target` is a
  `Transform` and `movementStats` is a `WorkerMovementStats : MonoBehaviour`, both
  `UnityEngine.Object` types. CodeConvention "Null Checks" recommends Unity-style truthiness:
  `if (!target || !movementStats || ...)`.
- `MoveTo(Vector3, Vector3)`: `if (target == null) return;` — same issue, should be
  `if (!target) return;`.
- `MoveTo(Vector3, Vector3)` does not appear to be called from `MoveAction` or any reviewed
  Behavior Graph node (only `StartMove`/`TickMove`/`Stop` are used). Verify usage and remove if
  dead.

### `Assets/Scripts/Actors/Worker/Context/WorkerActionContext.cs`

- `//public WorkerAI Owner { get; }` and `//Owner = owner;` are commented-out code with no
  explanation (CodeConvention "Comments": comments should explain non-obvious reasons; dead
  code without a reason should be removed or annotated with a TODO).

### `Assets/Scripts/Actors/Worker/Context/WorkerMovementStats.cs`

- No issues found. `_moveSpeed` / `_stoppingDistance` (`_camelCase` serialized fields) and
  `const` usage for `MinMoveSpeed` / `MinStoppingDistance` match CodeConvention.

### `Assets/Scripts/Actors/Worker/WorkerActionPlan.cs`

- No issues found. Matches the "Plan Ownership" rules in CodeConvention and the
  `WorkerActionPlan` role description in ProjectStructure.md.

### `Assets/Scripts/Actors/Worker/WorkerActionSet.cs`

- No issues found. Manual registration in `Awake()` is a documented limitation in
  ProjectStructure.md, not a new issue.

### `Assets/Scripts/Actors/Worker/Actions/EatAction.cs`

- `private float _timer = 2f;` — this is a private runtime field (not `[SerializeField]`), and
  uses the `_camelCase` style, while `DrinkAction.timer` and `WorkAction.timer` (also private
  runtime fields) use plain `camelCase` without the underscore. Naming should be consistent
  across the four action files (see "Cross-Cutting Findings" below for the underlying
  ambiguity).
- The literal `2f` appears twice (field initializer and inside `Start()`), where the field
  initializer is redundant since `Start()` always resets it. Consider a single
  `private const float Duration = 2f;`.

### `Assets/Scripts/Actors/Worker/Actions/RestAction.cs`

- `private float _timer;` — same naming inconsistency as `EatAction` (compare
  `DrinkAction.timer` / `WorkAction.timer`, which use no underscore).

### `Assets/Scripts/Actors/Worker/Actions/DrinkAction.cs`

- Line 1 has a stray leading space before `using UnityEngine;`. Minor formatting issue.

### `Assets/Scripts/Actors/Worker/Actions/MoveAction.cs`

- No issues found. Null checks on `context`/`context.Mover` (plain C# types) correctly use
  `== null` / `?.`; `Start`/`Tick`/`Cancel` and per-run state reset match
  CodeConvention "Action Rules".

### `Assets/Scripts/Actors/Worker/Actions/WorkAction.cs`

- `context.Stats.Work(10f, 10f, 20f)` uses three magic numbers, but only in one place — lower
  priority than the repeated values noted elsewhere. Consider named constants for readability
  if these values are tuned later.

### `Assets/Scripts/Provider/DestinationProvider.cs`

- `TryGetDestinationPosition`: `if (info.ActionType != actionType || info.Destination == null)`
  — `info.Destination` is a `GameObject` (`UnityEngine.Object`). CodeConvention "Null Checks"
  recommends `!info.Destination` instead of `info.Destination == null`.
- `DestinationInfo.Destination` / `DestinationInfo.ActionType` are public fields on a
  `[Serializable]` data class. CodeConvention "Unity" prefers `[SerializeField] private` for
  Inspector fields, though public fields on plain serializable data holders (not
  `MonoBehaviour`) are common Unity practice — flagged as a minor consistency note only.

### `Assets/Scripts/Manager/TickManager.cs`

- `[SerializeField]` fields `slowtick`, `normaltick`, `fasttick` do not use the `_camelCase`
  prefix (CodeConvention "Field Style"). Suggested: `_slowTick`, `_normalTick`, `_fastTick`.

### `Assets/Scripts/Interface/ITickable.cs`

- `using UnityEngine;` is present but no UnityEngine type is referenced in this file. Unused
  using directive.

### `Assets/Scripts/Enum/ActionState.cs`

- `using UnityEngine;` is present but no UnityEngine type is referenced. Unused using
  directive.

### `Assets/Scripts/Enum/ActionType.cs`

- No issues found.

### `Assets/Scripts/Enum/DestinationType.cs`

- `using UnityEngine;` is present but no UnityEngine type is referenced. Unused using
  directive.
- This enum is already noted in ProjectStructure.md "Known Design Notes" (line ~545) as stale:
  "current destination lookup is based on `ActionType`" rather than `DestinationType`. This
  review confirms `DestinationType` is not referenced by `DestinationProvider` or any
  reviewed selector/action. Candidate for removal, pending confirmation it is truly unused
  project-wide.
- Declared in namespace `ProviderEnum`, while sibling files `ActionState.cs` / `ActionType.cs`
  in the same `Assets/Scripts/Enum` folder use namespace `WorkerEnum`. See "Cross-Cutting
  Findings" below.

### `Assets/Scripts/Enum/TickType.cs`

- No namespace declared, while sibling enums in the same folder use `WorkerEnum` or
  `ProviderEnum`. See "Cross-Cutting Findings" below.

### `Assets/Scripts/Interface/IAction.cs`

- `public ActionType ActionType { get; }` — the explicit `public` modifier on an interface
  member is redundant (interface members are implicitly public). Minor style note, not a
  functional issue.

### `Assets/Scripts/Interface/IActionSelector.cs`

- No issues found. Genuinely shared role contract (implemented by
  `WorkerDefaultActionSelector`, referenced by `WorkerAI` and Behavior Graph nodes).

### `Assets/Scripts/Interface/IActionSet.cs`

- `IActionSet<TKey>` currently has only one implementer (`WorkerActionSet`). CodeConvention
  "Interfaces" advises against one-to-one interfaces for a single concrete class unless there
  is a concrete need (e.g. testing/mocking). Not necessarily wrong — flagged for awareness only.

### `Assets/BehaviorGraph/CustomActionNode/EnsureWorkerHasActionNode.cs`

- `GetActionSelector()` duplicates the same `MonoBehaviour[]` component-scan logic as
  `WorkerAI.FindActionSelector()`. Not a strict CodeConvention violation, but a duplication
  worth considering for a shared helper if more call sites appear.

### `Assets/BehaviorGraph/CustomActionNode/MoveToPlanDestinationNode.cs`

- `OnUpdate()`: `if (workerAI == null && !GameObject.TryGetComponent(out workerAI))` —
  `workerAI` is a `WorkerAI : MonoBehaviour` (`UnityEngine.Object`). CodeConvention
  "Null Checks" recommends `if (!workerAI && !GameObject.TryGetComponent(out workerAI))`.
- `ReturnPlanActions()`: `if (!actionSet || plan == null)` correctly mixes Unity-style
  truthiness (`!actionSet`, a `MonoBehaviour`) with an explicit null check (`plan == null`, a
  plain `WorkerActionPlan`) — listed here as a **correct example**, not an issue.
- This node creates and runs a `WorkerActionPlan` directly rather than going through
  `WorkerAI.SetPlan` / `TickCurrentAction` (unlike `EnsureWorkerHasActionNode` /
  `RunWorkerCurrentActionNode`). This asymmetry is explicitly described in
  ProjectStructure.md (lines ~410-411) as the intended bridging behavior for this node — not
  flagged as a violation.

### `Assets/BehaviorGraph/CustomActionNode/RunWorkerCurrentActionNode.cs`

- No issues found. Thin bridge matching its ProjectStructure.md role description exactly.

---

## Cross-Cutting Findings

### 1. Folder structure vs. ProjectStructure.md

ProjectStructure.md's "File Structure" section lists `WorkerActionContext.cs`,
`WorkerMover.cs`, `WorkerStats.cs`, and `WorkerMovementStats.cs` directly under
`Assets/Scripts/Actors/Worker/`. In the actual codebase, these four files live under
`Assets/Scripts/Actors/Worker/Context/`. Either the document should be updated to reflect the
`Context/` subfolder, or the files should be moved to match the document — whichever the team
intends as the source of truth.

### 2. Namespace usage is inconsistent

- `Assets/Scripts/Enum/ActionState.cs` and `ActionType.cs` use namespace `WorkerEnum`.
- `Assets/Scripts/Enum/DestinationType.cs` (same folder) uses namespace `ProviderEnum`.
- `Assets/Scripts/Enum/TickType.cs` (same folder) has **no namespace**.
- All Worker-domain classes (`WorkerAI`, `WorkerActionContext`, `WorkerActionPlan`,
  `WorkerActionSet`, `WorkerDefaultActionSelector`, `WorkerStats`, `WorkerMover`,
  `WorkerMovementStats`, and all five `IAction` implementations), all four interfaces, and
  `TickManager` / `DestinationProvider` have **no namespace**.

CodeConvention "Namespaces" says to use namespaces consistently once a domain has enough
scripts to benefit from them. The Worker domain has well over a dozen files with no namespace
at all, while three small enum files in the same folder use two different namespaces between
them. This is worth a deliberate decision (e.g., introduce a `Worker` namespace for the
domain, and/or unify the enum namespaces) rather than leaving it as incidental drift.

### 3. `_camelCase` naming for `[SerializeField]` private fields

CodeConvention "Field Style" specifies `_camelCase` for private fields, including private
serialized fields. The following serialized fields do not follow this:

- `WorkerAI.cs`: `actionSelectorSource`, `movementStats`, `tickManager`, `statsTickType`
- `WorkerDefaultActionSelector.cs`: `actionSet`, `destinationProvider`
- `TickManager.cs`: `slowtick`, `normaltick`, `fasttick`

Correctly-named examples for comparison: `WorkerMovementStats._moveSpeed` /
`_stoppingDistance`, `WorkerActionSet._initialPoolSize`, `DestinationProvider._destinationInfos`.

### 4. Private runtime field naming: `_timer` vs `timer`

- `EatAction.cs` and `RestAction.cs` use `_timer` (with underscore).
- `DrinkAction.cs` and `WorkAction.cs` use `timer` (no underscore).

Both are private runtime fields (not `[SerializeField]`). CodeConvention has two relevant
lines that read slightly differently — the "Naming" section (line ~9) describes private
runtime fields as plain `camelCase` and reserves `_camelCase` for Inspector-configured
serialized fields, while the "Field Style" section (line ~94) describes `_camelCase` as
covering "private fields, including private serialized fields" without explicitly excluding
non-serialized runtime fields. Because the existing code itself is split 2-vs-2 on this point,
this is reported as an inconsistency to resolve with a single explicit rule, rather than as a
violation of one specific line.

### 5. Repeated magic numbers

- `70f` — repeated 3 times in `WorkerDefaultActionSelector.TrySelectAction` (thirst, hunger,
  fatigue thresholds).
- `50f` — used once each in `EatAction.Tick` (`Stats.Eat(50f)`), `DrinkAction.Tick`
  (`Stats.Drink(50f)`), and `RestAction.Tick` (`Stats.Rest(50f)`) — same value repeated across
  three files/methods.
- `2f` — used twice within `EatAction.cs` (field initializer and `Start()`).

CodeConvention "Field Style" recommends `const` / `static readonly` for such fixed, repeated
values.

### 6. Unused `using UnityEngine;`

`ITickable.cs`, `ActionState.cs`, and `DestinationType.cs` each have `using UnityEngine;` but
do not reference any UnityEngine type.

### 7. Possible dead code (verify before removing)

- `WorkerMover.MoveTo(Vector3, Vector3)` — not called by `MoveAction` or any reviewed
  Behavior Graph node.
- `WorkerStats.Work(float amount)` (single-parameter overload) — not called by `WorkAction`,
  which uses the 3-parameter overload instead.
- `Assets/Scripts/Enum/DestinationType.cs` — already flagged as stale in
  ProjectStructure.md; this review found no reference to it in `DestinationProvider` or any
  reviewed selector/action.
- `ActionType.Sleep` — defined in the enum but not handled in
  `WorkerActionSet.CreateAction` / `RegisterPool` (no corresponding action is registered for
  it).

---

## Items Already Documented in ProjectStructure.md (not new findings)

Listed here only so this document is self-contained; no action implied beyond what
ProjectStructure.md already states:

- `WorkerAI.Awake()` directly constructs `WorkerStats`, `WorkerMover`, and
  `WorkerActionContext` — marked as temporary, pending a factory/manager.
- `WorkerAI.statsTickType` is currently unused except for planned `TickManager` integration.
- `TickManager` registration from `WorkerAI` is commented out.
- `Assets/Scripts/Enum/DestinationType.cs` still exists despite destination lookup being
  `ActionType`-based.
- `DestinationProvider` stores `GameObject` references; `Transform` may be cleaner if only
  position is needed.

---

## Summary

- **Naming convention fixes**: 9 `[SerializeField]` fields missing `_camelCase` prefix across
  `WorkerAI.cs`, `WorkerDefaultActionSelector.cs`, `TickManager.cs`; `_timer`/`timer`
  inconsistency between `EatAction`/`RestAction` and `DrinkAction`/`WorkAction`;
  `WorkerStats` `MIN_*`/`MAX_*` constants should be `const` + PascalCase.
- **Null-check style fixes**: 4 locations using `== null` on `UnityEngine.Object` types that
  should use `!x` (`WorkerMover.TickMove`, `WorkerMover.MoveTo`,
  `DestinationProvider.TryGetDestinationPosition`, `MoveToPlanDestinationNode.OnUpdate`).
- **Cleanup candidates**: unused `using UnityEngine;` in 3 files; unexplained commented-out
  code in `WorkerActionContext.cs` and `WorkerAI.cs`; possibly-dead `WorkerMover.MoveTo`,
  `WorkerStats.Work(float)`, `DestinationType.cs`, `ActionType.Sleep`.
- **Repeated magic numbers**: `70f` (x3), `50f` (x3 across files), `2f` (x2 in one file) —
  candidates for `const`.
- **Documentation drift**: `Context/` subfolder not reflected in ProjectStructure.md's file
  tree; namespace usage (`WorkerEnum` / `ProviderEnum` / no namespace) is inconsistent across
  the Enum folder and the Worker domain.
