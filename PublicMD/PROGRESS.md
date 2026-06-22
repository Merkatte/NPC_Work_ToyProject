# PROGRESS

## Current Status
Friendly actor AI scripts are reorganized under `Assets/Scripts/Actors/AI/Friendly`. Reusable execution, movement, recovery, context, and action-result data now live in `Common`; Cook and Farmer keep role-specific selectors, composition, action ownership, and production actions. `Enemy` is reserved as the hostile-AI root. Script contents, type names, namespaces, and existing Unity asset GUIDs were preserved.

## Completed Tasks
| Task ID | Date | Summary | Evidence | Related REQs |
|---|---|---|---|---|
| IMP-001 | 2026-06-17 | Moved Work/Eat/Drink/Rest duration and stat deltas into WorkerActionResultStatData and injected entries through WorkerActionSet. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors. | WorkerAI must remain execution-only; action result stats must be data-owned. |
| IMP-002 | 2026-06-17 | Updated WorkerAIManager to inject selector-side WorkerActionSet and connected the SampleScene manager to the Default selector template. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors; serialized references verified by search. | WorkerAIManager must not require WorkerActionSet on WorkerAI prefabs. |
| IMP-004 | 2026-06-17 | Added wheat carry reward, warehouse destination, and DepositWheat action selected when carried wheat is full. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors; serialized references verified by search. | Work must produce wheat reward; WorkerAI must remain execution-only; deposit must be a separate action. |
| IMP-005 | 2026-06-19 | Added the initial IAnim contract, animation context/type, MoveAnim hop loop, and WorkAnim squash loop with horizontal flip support. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors. | DOTween animations must be isolated behind reusable animation implementations. |
| IMP-006 | 2026-06-19 | Added shared animation lookup, per-worker playback control, child visual-root ownership, and Move/Work action playback requests. | Command-line C# build and static prefab/reference checks passed. | WorkerAI must remain animation-agnostic; shared animation definitions must not share per-worker Tween state. |
| IMP-007 | 2026-06-20 | Added the initial CookActionSelector skeleton without coupling it to Farmer's WorkerActionSet. | Command-line C# build passed; selector contract implementation verified statically. | Cook requires a selector boundary before Cook-specific actions and action ownership are introduced. |
| IMP-008 | 2026-06-22 | Added warehouse inventory system: ItemType enum, IInventory contract, WarehouseInventory component, safe carry→warehouse transfer in DepositWheatAction, selector→action IInventory injection pattern. WorkerActionContext unchanged (Worker-only). | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors; scene wiring verified by YAML search (fileID 1903621705 consistent across WarehouseObject components, WarehouseInventory MonoBehaviour, and selector `_warehouse` reference). | REQ-F-022, REQ-F-023, REQ-NF-003. |
| IMP-009 | 2026-06-22 | Reorganized actor scripts into `AI/Friendly/Common`, `Cook`, and `Farmer`, with an `AI/Enemy` root reserved for hostile actors. | Command-line C# build passed with 0 warnings/errors; no duplicate GUIDs or stale old actor paths found; scene/prefab script GUID references remained unchanged. | Architecture responsibility and dependency placement; no gameplay requirement changed. |

## In Progress
| Task ID | Started | Current Step | Remaining Work |
|---|---|---|---|

## Files Changed
| Path | Change Summary | Reason |
|---|---|---|
| Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatData.cs | Added ScriptableObject result stat database. | Own action cost/reward data outside WorkerAI and action logic. |
| Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatEntry.cs | Added serializable action result entry type. | Expose action duration and stat delta as shared data. |
| Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerStatDelta.cs | Added serializable stat delta type. | Let actions apply stat changes without knowing action-specific values. |
| Assets/Scripts/Actors/AI/Friendly/Common/Data/DefaultWorkerActionResultStatData.asset | Added default Work/Eat/Drink/Rest durations and stat deltas matching previous behavior. | Preserve existing gameplay values while making them configurable. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerActionSet.cs | Added result stat data reference, lookup API, and data-backed action creation. | Centralize action construction and data injection. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/*.cs | Replaced hardcoded durations and stat values with injected result stat entries. | Keep actions responsible for execution flow only. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/DepositWheatAction.cs | Added timer-based action that deposits all carried wheat on completion. | Keep warehouse deposit behavior in an IAction implementation. |
| Assets/Scripts/Enum/ItemType.cs | Added ItemType enum (Wheat). | Identify item types without IItem interface overhead; same pattern as ActionType. |
| Assets/Scripts/Interface/IInventory.cs | Added IInventory contract (TryAdd/TryRemove/GetQuantity/TotalCount/Capacity). | Define a single inventory role contract shared by facility components and actions. |
| Assets/Scripts/Facility/WarehouseInventory.cs | Added WarehouseInventory MonoBehaviour implementing IInventory with total-capacity and per-ItemType quantity tracking. | Provide runtime inventory ownership for the warehouse facility as a composable component. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerCarryStorage.cs | Replaced DepositAllWheat with RemoveWheat(int); partial removal now supported. | Enable safe partial transfer where only the accepted quantity is removed from carry. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/DepositWheatAction.cs | Rewrote to use injected IInventory (SetTargetInventory/ClearTargetInventory); transfer removes only accepted quantity; returns Failed if accepted==0. | Fix resource loss bug; align with selector→action injection pattern identical to MoveAction destination. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerActionSet.cs | Added TryGetAction(ActionType, IInventory, out) and TryRentAction(ActionType, IInventory, out) overloads; ResetAction clears DepositWheatAction inventory. | Mirror MoveAction destination injection pattern for DepositWheatAction. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerDefaultActionSelector.cs | Added _warehouse serialized field; WarehouseHasSpace() guard; TryCreateDepositPlan() that injects IInventory into rented DepositWheatAction. | Keep IInventory out of WorkerActionContext; selector is the decision layer that owns external facility references. |
| Assets/Scenes/SampleScene.unity | Added WarehouseInventory component (fileID 1903621705) to WarehouseObject; wired _warehouse on selector template. | Connect scene facility to the selector so IInventory can be injected at plan time. |
| Assembly-CSharp.csproj | Added ItemType.cs, IInventory.cs, WarehouseInventory.cs compile entries. | Make new scripts visible to dotnet build before Unity regeneration. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerStats.cs | Added generic stat delta application and removed action-specific stat mutation methods. | Keep WorkerStats focused on stat ownership and clamping. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerCarryStorage.cs | Added carried wheat state, capacity clamp, add, and deposit operations. | Separate carried item state from hunger/thirst/fatigue stats. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerActionContext.cs | Exposes WorkerCarryStorage to actions and selectors. | Let actions mutate carried wheat without WorkerAI knowing the reward system. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerDefaultActionSelector.cs | Reads Work result data through WorkerActionSet before creating Work plans. | Let selector depend on shared data instead of action implementation. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerDefaultActionSelector.cs | Selects DepositWheat after critical needs and before prepare-threshold needs when carried wheat is full. | Prevent endless work while still honoring critical survival needs. |
| Assets/Scenes/SampleScene.unity | Connected the default result stat data asset to the existing WorkerActionSet. | Ensure scene action creation can resolve stat entries. |
| Assets/Scenes/SampleScene.unity | Added WarehousePoint/WarehouseObject and mapped ActionType.DepositWheat in DestinationProvider. | Give DepositWheat a scene destination. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerAIManager.cs | Removed worker-prefab WorkerActionSet lookup and resolved WorkerActionSet from selector instances. | Keep WorkerActionSet owned by selector/action construction, not WorkerAI. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerAIManager.cs | Added initial carry storage configuration for spawned workers. | Configure carried wheat capacity without WorkerAI owning item state. |
| Assets/Scenes/SampleScene.unity | Connected WorkerAIManager to the Default selector template. | Allow selector creation after WorkerActionSet resolution. |
| Assets/Scripts/Enum/ActionType.cs | Added DepositWheat action type. | Allow action set and destination provider to identify the deposit behavior. |
| Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatEntry.cs | Added wheat delta to action result data entries. | Keep work reward values in data instead of action code. |
| Assets/Scripts/Animation/AnimType.cs | Added Move and Work animation identifiers. | Support enum-keyed animation lookup without coupling to action implementations. |
| Assets/Scripts/Animation/AnimContext.cs | Added visual Transform and FlipX execution parameters. | Give animations only the runtime data required to create their tweens. |
| Assets/Scripts/Animation/IAnim.cs | Added the shared DOTween animation creation contract. | Allow animation implementations to be registered and invoked through one role. |
| Assets/Scripts/Animation/Anims/MoveAnim.cs | Added a looping local hop with stretch, squash, cleanup, and FlipX preservation. | Provide movement feedback without modifying gameplay movement logic. |
| Assets/Scripts/Animation/Anims/WorkAnim.cs | Added a looping squash pulse with cleanup and FlipX preservation. | Provide reusable visual feedback for work actions. |
| Assets/Scripts/Animation/IAnimSet.cs | Added the animation lookup contract. | Let playback depend on a shared registry role. |
| Assets/Scripts/Animation/AnimSet.cs | Registered one reusable Move and Work animation definition. | Share stateless definitions across all workers without pooling. |
| Assets/Scripts/Animation/IAnimPlayer.cs | Added per-actor playback, facing, and stop operations. | Expose animation capability to actions without exposing DOTween lifecycle details. |
| Assets/Scripts/Animation/ActorAnimationController.cs | Added per-worker active Tween and facing ownership. | Prevent workers from sharing mutable playback state. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerActionContext.cs | Exposed `IAnimPlayer` to actions. | Keep WorkerAI unaware of concrete animation lookup and playback. |
| Assets/Scripts/Actors/AI/Friendly/Common/Actions/MoveAction.cs | Starts Move animation with destination-derived facing and stops it with action lifecycle. | Keep movement animation owned by movement behavior. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/WorkAction.cs | Starts and stops Work animation with action lifecycle. | Preserve current facing while showing work feedback. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerAIManager.cs | Creates one shared AnimSet and one ActorAnimationController per spawned worker. | Establish correct shared-definition and per-actor-state ownership. |
| Assets/Prefab/FarmerAI.prefab | Moved SpriteRenderer to a child `VisualRoot`. | Isolate DOTween local visual changes from gameplay-root movement. |
| PublicMD/ProjectStructure.md | Documented animation roles, ownership, paths, and dependency flow. | Keep architecture guidance aligned with implementation. |
| PublicMD/CodeConvention.md | Added animation boundaries and corrected Farmer-domain paths. | Prevent animation lifecycle from drifting into WorkerAI or shared definitions. |
| Assets/Scripts/Actors/AI/Friendly/Cook/CookActionSelector.cs | Added a no-plan Cook selector implementing the existing worker selector contract. | Establish the Cook decision boundary without inventing Cook actions or reusing Farmer action ownership. |
| PublicMD/ProjectStructure.md | Documented the Cook folder and initial selector responsibility. | Keep actor-domain structure aligned with implementation. |
| Assets/Scripts/Actors/AI/Friendly/Common/** | Moved reusable AI lifecycle, plan, movement/recovery actions, context capabilities, and shared action-result data while preserving `.meta` files. | Prevent reusable friendly AI code from being owned by the Farmer role. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/** | Kept Farmer composition, production selector/action set, combat selector stub, and Work/Deposit actions under the Farmer role. | Keep job-specific production ownership out of Common. |
| Assets/Scripts/Actors/AI/Friendly/Cook/** | Moved the Cook selector into the friendly Cook role. | Align Cook with the Friendly actor hierarchy. |
| Assets/Scripts/Actors/AI/Enemy/ | Added the hostile AI root. | Separate future enemy behavior from friendly actor code. |
| Assembly-CSharp.csproj | Updated moved script compile paths. | Preserve command-line build verification before Unity regenerates the project file. |
| PublicMD/ProjectStructure.md, PublicMD/CodeConvention.md | Updated folder ownership and placement rules. | Keep future implementation aligned with the new hierarchy. |

## Implementation Notes
IMP-001: `WorkerAI` was intentionally left unchanged and does not reference `WorkerActionResultStatData`.

IMP-002: CSV/provider abstraction was not added yet; `WorkerActionResultStatData` is the current single source of truth and can be replaced later behind the same lookup role if needed.

IMP-003: `WorkerActionSet` remains selector-side. `WorkerAI` and worker prefabs should not own action pools.

IMP-004: `WorkerAI` remains unchanged for wheat and warehouse behavior. The selector only decides that full carried wheat should trigger `DepositWheat`; `WorkAction` and `DepositWheatAction` perform the state changes through `WorkerCarryStorage`.

IMP-005: `IAnim` implementations create and return DOTween tweens but do not own the active tween lifecycle. `AnimContext.Transform` must be a child visual Transform so MoveAnim local-position changes do not compete with WorkerMover on the actor root. FlipX is applied by preserving scale magnitude and changing only the local X scale sign.

IMP-006: `AnimSet` is shared and stores only stateless definitions. `ActorAnimationController` is created per worker and exclusively owns the mutable active Tween and facing. `MoveAction` and `WorkAction` request animation through `WorkerActionContext.Animation`; `WorkerAI` remains unchanged and animation-agnostic.

IMP-007: `CookActionSelector` intentionally does not implement `IWorkerActionSelectorSetup` because that setup contract injects Farmer's `WorkerActionSet`. It will remain a no-plan selector until Cook-specific actions and their ownership model are defined.

IMP-008: `WorkerActionContext` was intentionally left unchanged. `IInventory` is not a Worker capability — it is an external facility reference. The selector→action injection pattern (identical to `MoveAction` destination) was used instead. `WarehouseInventory` holds a total-capacity limit (not per-ItemType) as the initial choice (user-confirmed). Carry+warehouse both full results in `DepositWheatAction.Failed` with no resource loss; idle/wait policy for that state is out of scope for this slice. `Assembly-CSharp.csproj` was manually updated with new files because Unity has not yet reimported them; Unity regeneration will produce the authoritative csproj. Codex review agent launch was blocked by auto-mode sandbox; user must invoke it manually if desired.

IMP-009: Common contains reusable friendly-AI execution and capabilities, not global game services. Farmer retains `WorkerActionSet`, `WorkerAIManager`, Farmer selectors, `WorkAction`, and `DepositWheatAction` because those currently compose or execute Farmer production. `WorkerCombatActionSelector` remains under Farmer until its action-set dependency is generalized. Moving files preserved their existing `.meta` GUIDs, so serialized script references do not require rewiring.

## Blockers
| ID | Blocking Task | Problem | Required Decision |
|---|---|---|---|

## Verification Performed
| Task ID | Check | Result | Notes |
|---|---|---|---|
| IMP-001 | Command-line C# build | Passed | 0 warnings, 0 errors. |
| IMP-001 | Static reference check | Passed | Action hardcoded stat values were removed; WorkerAI has no result stat data reference. |
| IMP-002 | Command-line C# build | Passed | 0 warnings, 0 errors. |
| IMP-002 | Serialized reference search | Passed | SampleScene manager has Default selector entry; selector template has WorkerActionSet with result stat data. |
| IMP-004 | Command-line C# build | Passed | 0 warnings, 0 errors. |
| IMP-004 | Serialized reference search | Passed | DepositWheat action type, result data entry, destination mapping, and initial carry storage were found. |
| IMP-005 | Command-line C# build | Passed | DOTween reference resolved; 0 warnings and 0 errors. |
| IMP-005 | Static implementation check | Passed | MoveAnim and WorkAnim use local Transform properties, preserve FlipX through the X scale sign, and reset modified values when killed. |
| IMP-006 | Command-line C# build | Passed | New animation lookup/controller and action integration compile with DOTween references. |
| IMP-006 | Static prefab/reference check | Passed | FarmerAI SpriteRenderer is on child VisualRoot; WorkerAI has no animation dependency; actions use IAnimPlayer through context. |
| IMP-007 | Command-line C# build | Passed | CookActionSelector compiles against the existing generic selector contract. |
| IMP-007 | Static implementation check | Passed | Selector returns a null plan and does not depend on Farmer's WorkerActionSet. |
| IMP-008 | Command-line C# build | Passed | 0 warnings, 0 errors. |
| IMP-008 | Scene wiring search | Passed | `_warehouse` on selector template (fileID 568873992) references fileID 1903621705; fileID 1903621705 is in WarehouseObject component list and has WarehouseInventory script GUID a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6. |
| IMP-008 | Static logic review | Partial | Partial transfer preserves carry, but later Codex review found that carry-full + warehouse-full falls through to Work instead of entering a defined wait/blocked state. |
| IMP-009 | Command-line C# build | Passed | 0 warnings, 0 errors after updating moved compile paths. |
| IMP-009 | Structure and GUID audit | Passed | Old `Actors/Farmer` and `Actors/Cook` paths are absent; no duplicate asset GUIDs; existing scene/prefab script GUID references are unchanged. |

## Next Actions
1. Open the project in Unity Editor and allow it to reimport the moved scripts; confirm no missing-script components appear.
2. Confirm `WarehouseInventory` and selector `_warehouse` references survive reimport.
3. Implement the reviewed warehouse-full wait/blocked policy and destination-missing failure distinction.
4. Run the Farmer production/deposit and Move/Work animation loops in Play Mode.
5. Define Cook action ownership only after the M0 safe-failure checks pass.
