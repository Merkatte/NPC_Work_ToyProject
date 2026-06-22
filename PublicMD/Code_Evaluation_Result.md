# Code Evaluation Result

## Purpose

This document records a lead-programmer review of the NPC_Work_2D worker/NPC implementation after the warehouse inventory slice (`IMP-008`). The review checks `PublicMD/CodeConvention.md`, `PublicMD/ProjectStructure.md`, the review workflow, all relevant C# scripts, and Unity scene/prefab wiring. Production code was not modified.

## Review Snapshot

- Date: 2026-06-22
- Scope: all `.cs` files under `Assets/Scripts` and `Assets/BehaviorGraph/CustomActionNode`, `Assets/Scenes/SampleScene.unity`, `Assets/Prefab/FarmerAI.prefab`, and the warehouse-related working-tree changes
- Sources: current code convention, project structure, architecture, prior evaluation report, source code, asset metadata, scene/prefab YAML
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and 0 errors; warehouse script GUID and scene references passed static checks
- Limit: Unity Editor import, component deserialization, and Play Mode behavior were not executed

## Executive Summary

The previous High issue—deposited wheat disappearing because no warehouse owned it—is substantially resolved. `WarehouseInventory` owns runtime quantities, `DepositWheatAction` removes only the quantity accepted by the destination, and `WorkerDefaultActionSelector` injects the external inventory without polluting `WorkerActionContext`. `WorkerAI` remains execution-only. Static scene wiring is internally consistent and the project builds cleanly.

One High behavior issue remains in the completed slice: when both worker carry and warehouse are full, the selector skips deposit and falls through to another `WorkAction`. The worker therefore continues working with a full carry, discards the zero accepted production result, and never enters the required identifiable wait or production-stop state. This contradicts `REQ-F-023` and the architecture's warehouse-rejection rule.

The existing missing-destination ambiguity also remains. In the new deposit path it can allow a deposit action to run without moving when the warehouse destination mapping is absent. Documentation is now stale: `ProjectStructure.md` and parts of `ARCHITECTURE.md` still describe the removed `DepositAllWheat()` flow and claim that no warehouse inventory owner exists.

No Critical issues found.

## Improvements Since Previous Review

- Added `WarehouseInventory` as the runtime owner of warehouse quantities and capacity.
- Added `IInventory` with add, remove, quantity, total-count, and capacity contracts.
- Added `ItemType.Wheat` and connected the initial warehouse to the shared item-keyed inventory API.
- Changed `DepositWheatAction` to add to the destination first and remove only the accepted quantity from carry.
- Kept facility inventory out of `WorkerActionContext`; the selector injects it into the rented deposit action.
- Added the `WarehouseInventory` component to `WarehouseObject` and wired `_warehouse` on the default selector template.
- Preserved `WorkerAI`'s execution-only responsibility.
- The previous warehouse-persistence High finding is resolved for the normal non-full path.

## Findings By Severity

### Critical

None found.

### High

- **Warehouse-full selection falls back to work instead of wait/stop**
  - Location: `Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerDefaultActionSelector.cs:47-56`; `Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/WorkAction.cs:31`
  - Evidence: deposit is selected only when `IsWheatFull && WarehouseHasSpace()`. If carry is full and the warehouse has no space, the branch is skipped and the selector eventually creates another Work plan. `WorkAction` ignores the accepted amount returned by `AddWheat`.
  - Description: a full worker repeatedly performs work while its carry cannot accept more wheat. The newly produced amount is not represented, and there is no identifiable waiting or production-stopped state.
  - Impact: violates `REQ-F-023`, `ARCHITECTURE.md:919`, and the M0 safe-failure exit criteria; the production loop appears active while producing no stored output.
  - Recommended fix: make full carry a decision boundary regardless of warehouse space. If the warehouse cannot accept items, return an explicit wait/blocked plan or a throttled no-plan result with an observable reason. Do not select Work until carry space is available.

### Medium

- **Missing destination is still treated as already at destination**
  - Location: `Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerDefaultActionSelector.cs:101-113, 132-144, 148-162`
  - Evidence: `TryGetMoveDestination` returns `false` for both missing provider/mapping and being within stopping distance. Callers create the target action directly whenever it returns `false`.
  - Description: work, recovery, and deposit actions may execute in place when scene destination data is absent.
  - Impact: configuration errors remain silent; the warehouse action can deposit remotely instead of failing plan construction.
  - Recommended fix: return a three-state result such as Missing, AlreadyThere, or MoveRequired, and return rented actions when the required mapping is missing.

- **Inventory change records/notifications are absent**
  - Location: `Assets/Scripts/Facility/WarehouseInventory.cs`; `Assets/Scripts/Interface/IInventory.cs`
  - Evidence: mutations update the dictionary and total count but produce no change record or notification.
  - Description: the target flow in `ARCHITECTURE.md:138, 455, 982` requires inventory changes to be observable for UI/debug.
  - Impact: the next UI/debug slice has no supported observation boundary and may be forced to poll concrete inventory state.
  - Recommended fix: add a focused inventory change result/event containing item, accepted/removed quantity, and reason. Keep UI out of the inventory implementation.

- **Project structure and architecture documentation are stale after IMP-008**
  - Location: `PublicMD/ProjectStructure.md:406, 415, 677`; `PublicMD/ARCHITECTURE.md:141, 475`
  - Evidence: the documents still say `DepositWheatAction` calls `DepositAllWheat()`, clears all carry, and that no warehouse runtime owner exists.
  - Description: current ownership and transfer semantics are not reflected in the long-lived architecture guidance.
  - Impact: subsequent Cook, merchant, and inventory work may be designed against obsolete behavior.
  - Recommended fix: document `IInventory`, `WarehouseInventory`, partial acceptance, selector-to-action injection, and the current warehouse-full limitation.

- **Shared item identity is placed in the worker namespace**
  - Location: `Assets/Scripts/Enum/ItemType.cs`; consumers in `WarehouseInventory` and `IInventory`
  - Evidence: `ItemType` is declared in `WorkerEnum`, while the architecture defines items as shared by facilities, actions, economy, merchant, and UI.
  - Description: a settlement-wide inventory key is owned by the worker namespace.
  - Impact: facility and economy domains must depend on a worker-specific namespace, creating dependency-direction drift as the item catalog grows.
  - Recommended fix: move item identity to a neutral inventory/item domain namespace and location before Cook and merchant systems depend on it.

- **`WorkerActionResultStatData` does not validate duplicate or missing entries**
  - Location: `Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatData.cs:11-31`
  - Evidence: the first duplicate silently wins and missing required entries only cause runtime action creation to fail.
  - Impact: Inspector data errors can disable work or deposit behavior without an editor-time diagnosis.
  - Recommended fix: add `OnValidate()` checks for duplicate keys and required registered action entries.

- **Combat selector still has no in-range action or idle policy**
  - Location: `Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerCombatActionSelector.cs:30-49`
  - Evidence: missing target and in-range target both return no plan; `WorkerAI.Update()` requests a plan again every frame.
  - Impact: a combat worker can remain stuck while being polled each frame.
  - Recommended fix: add an attack/idle plan or an explicit event/throttled selection policy before enabling this selector.

- **`IActionSet<TKey>` still represents only a fraction of the actual action-set role**
  - Location: `Assets/Scripts/Interface/IActionSet.cs`; `Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerActionSet.cs`
  - Evidence: consumers use concrete overloads for destinations, inventory injection, result lookup, and action return.
  - Impact: the interface suggests replaceability that does not exist.
  - Recommended fix: remove it until needed or define a contract covering the real rental/return responsibilities.

### Low

- **Warehouse capacity has no editor/runtime validation**
  - Location: `Assets/Scripts/Facility/WarehouseInventory.cs:7, 23-24`
  - Evidence: `_capacity` can be negative; no `Min`, `OnValidate`, or runtime clamp is applied.
  - Recommended fix: clamp capacity to a non-negative value and validate invariants.

- **Destroyed Unity inventory references are not safely detected through the interface field**
  - Location: `Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/DepositWheatAction.cs:33, 51`
  - Evidence: `targetInventory` is typed as `IInventory`; `targetInventory == null` does not provide Unity fake-null semantics if the underlying `WarehouseInventory` is destroyed after plan creation.
  - Recommended fix: define an inventory availability contract or perform an explicit Unity-object validity check for Unity-backed inventories.

- **New and existing private fields do not follow the project field convention**
  - Location: `DepositWheatAction` (`resultStatEntry`, `targetInventory`, `timer`, `failed`), `WorkerAI`, `WorkerAIManager`, `WorkerCarryStorage`, `WorkerMover`, `MoveAction`, `MoveAnim`, `WorkAnim`
  - Evidence: private fields use unprefixed camelCase while `CodeConvention.md` requires `_camelCase` for private fields.
  - Recommended fix: apply a focused naming cleanup; use `[FormerlySerializedAs]` for serialized fields.

- **Normal-flow logs and stale comments remain**
  - Location: `WorkerAIManager`, `MoveAction`, `WorkerAI`, `WorkerActionContext`
  - Evidence: spawn/move logs run during normal behavior; commented-out `SetAction` and owner code remain.
  - Recommended fix: remove or gate routine logs and delete obsolete commented code.

- **Destination provider validation/style issues remain**
  - Location: `Assets/Scripts/Provider/DestinationProvider.cs`
  - Evidence: `_destinationInfos.Length` has no null guard; `DestinationInfo` exposes public mutable fields; Unity references use `== null`.
  - Recommended fix: guard the array, use private serialized fields with read-only properties, and add duplicate/missing mapping validation.

- **Stale enums/usings and unsupported values remain**
  - Location: `DestinationType`, `ActionType.Sleep`, `TickType.Custom`, unused `UnityEngine` imports
  - Recommended fix: remove or document unused values and reject unsupported tick types explicitly.

- **`WorkerActionResultStatData` naming no longer matches its contents**
  - Location: `WorkerActionResultStatData`, `WorkerActionResultStatEntry`
  - Evidence: entries contain `_wheatDelta`, which is an item reward rather than only stat data.
  - Recommended fix: rename to `WorkerActionResultData`/`WorkerActionResultEntry` before more item outputs are added.

## Findings By File

### `Assets/Scripts/Facility/WarehouseInventory.cs`

- Correctly owns facility inventory quantities and total capacity.
- `TryAdd` supports partial acceptance and maintains the total-count invariant for current single-threaded use.
- Needs capacity validation and an observation/change-record boundary.
- Zero-count dictionary entries after full removal are harmless but may be cleaned up if item enumeration is later exposed.

### `Assets/Scripts/Interface/IInventory.cs`

- The interface has a real multi-domain role and matches the architecture's add/remove/read boundary.
- It currently exposes no mutation reason or change observation needed by UI/debug.

### `Assets/Scripts/Enum/ItemType.cs`

- The enum is minimal and suitable for the first slice.
- `WorkerEnum` is the wrong long-term ownership namespace for a facility/economy-wide item identity.

### `Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/DepositWheatAction.cs`

- Correctly performs destination-first partial transfer and removes only accepted carry.
- Correctly fails when accepted quantity is zero, preserving carry.
- Its external inventory is cleared when the action is returned to the pool.
- Interface-backed Unity object validity and private-field naming need cleanup.

### `Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerActionSet.cs`

- Inventory injection mirrors the existing destination-injection pattern and returns mismatched rented actions safely.
- `ResetAction` clears mutable per-run deposit state before pooling.
- Concrete action casts are pragmatic but show why `IActionSet<TKey>` does not represent the actual contract.

### `Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerDefaultActionSelector.cs`

- Correctly keeps warehouse availability and deposit intent in the decision layer.
- Correctly avoids putting the facility inventory in worker context.
- Warehouse-full behavior falls through to work and must be corrected before M0 is considered complete.
- Missing destination and already-arrived states remain conflated.

### `Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/WorkAction.cs`

- Execution responsibility remains clean and data-driven.
- Ignoring the accepted quantity from `AddWheat` becomes observable when a selector incorrectly permits work with full carry.

### `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAI.cs`

- Remains execution-only and has no inventory/facility dependency.
- Existing naming and stale-comment cleanup remains.

### `Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerAIManager.cs`

- Selector composition and per-worker context construction remain structurally sound.
- Existing normal-flow logging and field naming remain cleanup items.

### `Assets/Scripts/Actors/AI/Friendly/Cook/CookActionSelector.cs`

- Correctly remains a no-plan boundary rather than inventing cooking ownership prematurely.
- Do not extend it until shared item/inventory ownership and current M0 failures are resolved.

### `Assets/Scripts/Animation/*`

- Shared animation definitions remain stateless; active Tween state remains per actor.
- VisualRoot prefab wiring remains correct by static YAML inspection.
- Private readonly animation parameter names still violate the current convention.

### `Assets/Scripts/Provider/DestinationProvider.cs`

- Role is appropriately limited to lookup.
- Missing array/mapping validation and public mutable nested fields remain.

### `Assets/BehaviorGraph/CustomActionNode/*`

- Nodes remain focused bridges through `WorkerAI` and do not acquire selector/action-set dependencies.

### `Assets/Scenes/SampleScene.unity`

- `_warehouse` on `WorkerDefaultActionSelector` references fileID `1903621705`.
- fileID `1903621705` is attached to `WarehouseObject` and references the `WarehouseInventory` script GUID declared in its `.meta` file.
- DestinationProvider still maps deposit movement to `WarehousePoint` by the existing scene data.
- Unity Editor deserialization and Play Mode remain unverified.

### `Assets/Prefab/FarmerAI.prefab`

- Worker root contains `WorkerAI` and `WorkerMovementStats`; the child `VisualRoot` owns the `SpriteRenderer`.
- No selector-side action set or facility inventory leaked onto the worker prefab.

## Cross-Cutting Findings

- The new inventory boundary is directionally correct, but warehouse rejection lacks a defined selector state.
- Static scene wiring is consistent, but hand-edited YAML cannot replace Unity Editor import and Play Mode verification.
- Long-lived structure documents are behind the implementation and currently contradict the new transfer semantics.
- Shared inventory/item concepts need a neutral domain before Cook, merchant, and UI systems depend on them.
- Field naming drift remains widespread but is lower priority than runtime safe-failure behavior.

## Positive Notes

- `WorkerAI` remains free of warehouse, item, and action-set details.
- The destination inventory is mutated before carry is reduced, preventing loss on partial or rejected transfer.
- `WorkerActionSet` clears injected external references when pooled actions are returned.
- `WarehouseInventory` has one clear state-ownership responsibility.
- Selector, action, inventory, and carry responsibilities remain distinct.
- Scene and prefab references are internally consistent in YAML.
- No duplicate asset GUIDs were found.
- Command-line build passes with 0 warnings and 0 errors.

## Recommended Next Actions

1. **High:** implement an explicit warehouse-full wait/blocked policy so a full-carry worker cannot select Work.
2. **Medium:** separate destination Missing, AlreadyThere, and MoveRequired results; fail plan construction on missing mappings.
3. **Verification:** open the project in Unity, confirm the warehouse component/reference survives import, and run the full Move → Work → Carry → Deposit loop including partial/full warehouse cases.
4. **Medium:** update `ProjectStructure.md` and stale architecture current-state notes for `IInventory`, `WarehouseInventory`, and partial transfer.
5. **Medium:** add inventory change observation for UI/debug and move `ItemType` to a neutral inventory/item domain.
6. **Medium:** add validation for warehouse capacity, destination mappings, and action-result entries.
7. **Medium:** complete or keep the combat selector disabled until an in-range policy exists.
8. **Low:** clean field naming, routine logs, stale comments, unused enums/usings, and misleading action-result data names.
