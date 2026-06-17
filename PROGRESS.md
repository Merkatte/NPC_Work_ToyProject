# PROGRESS

## Current Status
Worker action result stat values were separated from action code into ScriptableObject data. WorkerAIManager now resolves WorkerActionSet from the selector template instance instead of requiring worker prefabs to own it.

## Completed Tasks
| Task ID | Date | Summary | Evidence | Related REQs |
|---|---|---|---|---|
| IMP-001 | 2026-06-17 | Moved Work/Eat/Drink/Rest duration and stat deltas into WorkerActionResultStatData and injected entries through WorkerActionSet. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors. | WorkerAI must remain execution-only; action result stats must be data-owned. |
| IMP-002 | 2026-06-17 | Updated WorkerAIManager to inject selector-side WorkerActionSet and connected the SampleScene manager to the Default selector template. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors; serialized references verified by search. | WorkerAIManager must not require WorkerActionSet on WorkerAI prefabs. |

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
| Assets/Scripts/Actors/Worker/Context/WorkerStats.cs | Added generic stat delta application and removed action-specific stat mutation methods. | Keep WorkerStats focused on stat ownership and clamping. |
| Assets/Scripts/Actors/Worker/WorkerDefaultActionSelector.cs | Reads Work result data through WorkerActionSet before creating Work plans. | Let selector depend on shared data instead of action implementation. |
| Assets/Scenes/SampleScene.unity | Connected the default result stat data asset to the existing WorkerActionSet. | Ensure scene action creation can resolve stat entries. |
| Assets/Scripts/Actors/Worker/WorkerAIManager.cs | Removed worker-prefab WorkerActionSet lookup and resolved WorkerActionSet from selector instances. | Keep WorkerActionSet owned by selector/action construction, not WorkerAI. |
| Assets/Scenes/SampleScene.unity | Connected WorkerAIManager to the Default selector template. | Allow selector creation after WorkerActionSet resolution. |

## Implementation Notes
IMP-001: `WorkerAI` was intentionally left unchanged and does not reference `WorkerActionResultStatData`.

IMP-002: CSV/provider abstraction was not added yet; `WorkerActionResultStatData` is the current single source of truth and can be replaced later behind the same lookup role if needed.

IMP-003: `WorkerActionSet` remains selector-side. `WorkerAI` and worker prefabs should not own action pools.

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

## Next Actions
1. Add predictive selector logic that calculates expected work capacity from `WorkerActionResultStatData`.
