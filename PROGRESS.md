# PROGRESS

## Current Status
The initial DOTween animation domain is implemented. MoveAnim and WorkAnim create reusable looping tweens from an AnimContext containing a visual Transform and horizontal flip request.

## Completed Tasks
| Task ID | Date | Summary | Evidence | Related REQs |
|---|---|---|---|---|
| IMP-001 | 2026-06-17 | Moved Work/Eat/Drink/Rest duration and stat deltas into WorkerActionResultStatData and injected entries through WorkerActionSet. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors. | WorkerAI must remain execution-only; action result stats must be data-owned. |
| IMP-002 | 2026-06-17 | Updated WorkerAIManager to inject selector-side WorkerActionSet and connected the SampleScene manager to the Default selector template. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors; serialized references verified by search. | WorkerAIManager must not require WorkerActionSet on WorkerAI prefabs. |
| IMP-004 | 2026-06-17 | Added wheat carry reward, warehouse destination, and DepositWheat action selected when carried wheat is full. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors; serialized references verified by search. | Work must produce wheat reward; WorkerAI must remain execution-only; deposit must be a separate action. |
| IMP-005 | 2026-06-19 | Added the initial IAnim contract, animation context/type, MoveAnim hop loop, and WorkAnim squash loop with horizontal flip support. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors. | DOTween animations must be isolated behind reusable animation implementations. |

## In Progress
| Task ID | Started | Current Step | Remaining Work |
|---|---|---|---|

## Files Changed
| Path | Change Summary | Reason |
|---|---|---|
| Assets/Scripts/Actors/Worker/Data/WorkerActionResultStatData.cs | Added ScriptableObject result stat database. | Own action cost/reward data outside WorkerAI and action logic. |
| Assets/Scripts/Actors/Worker/Data/WorkerActionResultStatEntry.cs | Added serializable action result entry type. | Expose action duration and stat delta as shared data. |
| Assets/Scripts/Actors/Worker/Data/WorkerStatDelta.cs | Added serializable stat delta type. | Let actions apply stat changes without knowing action-specific values. |
| Assets/Scripts/Actors/Worker/Data/DefaultWorkerActionResultStatData.asset | Added default Work/Eat/Drink/Rest durations and stat deltas matching previous behavior. | Preserve existing gameplay values while making them configurable. |
| Assets/Scripts/Actors/Worker/WorkerActionSet.cs | Added result stat data reference, lookup API, and data-backed action creation. | Centralize action construction and data injection. |
| Assets/Scripts/Actors/Worker/Actions/*.cs | Replaced hardcoded durations and stat values with injected result stat entries. | Keep actions responsible for execution flow only. |
| Assets/Scripts/Actors/Worker/Actions/DepositWheatAction.cs | Added timer-based action that deposits all carried wheat on completion. | Keep warehouse deposit behavior in an IAction implementation. |
| Assets/Scripts/Actors/Worker/Context/WorkerStats.cs | Added generic stat delta application and removed action-specific stat mutation methods. | Keep WorkerStats focused on stat ownership and clamping. |
| Assets/Scripts/Actors/Worker/Context/WorkerCarryStorage.cs | Added carried wheat state, capacity clamp, add, and deposit operations. | Separate carried item state from hunger/thirst/fatigue stats. |
| Assets/Scripts/Actors/Worker/Context/WorkerActionContext.cs | Exposes WorkerCarryStorage to actions and selectors. | Let actions mutate carried wheat without WorkerAI knowing the reward system. |
| Assets/Scripts/Actors/Worker/WorkerDefaultActionSelector.cs | Reads Work result data through WorkerActionSet before creating Work plans. | Let selector depend on shared data instead of action implementation. |
| Assets/Scripts/Actors/Worker/WorkerDefaultActionSelector.cs | Selects DepositWheat after critical needs and before prepare-threshold needs when carried wheat is full. | Prevent endless work while still honoring critical survival needs. |
| Assets/Scenes/SampleScene.unity | Connected the default result stat data asset to the existing WorkerActionSet. | Ensure scene action creation can resolve stat entries. |
| Assets/Scenes/SampleScene.unity | Added WarehousePoint/WarehouseObject and mapped ActionType.DepositWheat in DestinationProvider. | Give DepositWheat a scene destination. |
| Assets/Scripts/Actors/Worker/WorkerAIManager.cs | Removed worker-prefab WorkerActionSet lookup and resolved WorkerActionSet from selector instances. | Keep WorkerActionSet owned by selector/action construction, not WorkerAI. |
| Assets/Scripts/Actors/Worker/WorkerAIManager.cs | Added initial carry storage configuration for spawned workers. | Configure carried wheat capacity without WorkerAI owning item state. |
| Assets/Scenes/SampleScene.unity | Connected WorkerAIManager to the Default selector template. | Allow selector creation after WorkerActionSet resolution. |
| Assets/Scripts/Enum/ActionType.cs | Added DepositWheat action type. | Allow action set and destination provider to identify the deposit behavior. |
| Assets/Scripts/Actors/Worker/Data/WorkerActionResultStatEntry.cs | Added wheat delta to action result data entries. | Keep work reward values in data instead of action code. |
| Assets/Scripts/Animation/AnimType.cs | Added Move and Work animation identifiers. | Support enum-keyed animation lookup without coupling to action implementations. |
| Assets/Scripts/Animation/AnimContext.cs | Added visual Transform and FlipX execution parameters. | Give animations only the runtime data required to create their tweens. |
| Assets/Scripts/Animation/IAnim.cs | Added the shared DOTween animation creation contract. | Allow animation implementations to be registered and invoked through one role. |
| Assets/Scripts/Animation/MoveAnim.cs | Added a looping local hop with stretch, squash, cleanup, and FlipX preservation. | Provide movement feedback without modifying gameplay movement logic. |
| Assets/Scripts/Animation/WorkAnim.cs | Added a looping squash pulse with cleanup and FlipX preservation. | Provide reusable visual feedback for work actions. |

## Implementation Notes
IMP-001: `WorkerAI` was intentionally left unchanged and does not reference `WorkerActionResultStatData`.

IMP-002: CSV/provider abstraction was not added yet; `WorkerActionResultStatData` is the current single source of truth and can be replaced later behind the same lookup role if needed.

IMP-003: `WorkerActionSet` remains selector-side. `WorkerAI` and worker prefabs should not own action pools.

IMP-004: `WorkerAI` remains unchanged for wheat and warehouse behavior. The selector only decides that full carried wheat should trigger `DepositWheat`; `WorkAction` and `DepositWheatAction` perform the state changes through `WorkerCarryStorage`.

IMP-005: `IAnim` implementations create and return DOTween tweens but do not own the active tween lifecycle. `AnimContext.Transform` must be a child visual Transform so MoveAnim local-position changes do not compete with WorkerMover on the actor root. FlipX is applied by preserving scale magnitude and changing only the local X scale sign.

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

## Next Actions
1. Add an animation controller/set that registers IAnim implementations by AnimType and owns the active Tween lifecycle.
2. Connect action start/completion/cancellation to animation requests after the controller ownership is approved.
