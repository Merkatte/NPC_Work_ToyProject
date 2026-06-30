# Project Structure

## Purpose
This document describes the current project structure for future sessions and external coding assistants. It should be read together with `CodeConvention.md` before making architectural changes.

The current project is a Unity 2D worker/NPC behavior prototype. The core design separates:

- decision making: selectors
- action execution: `IAction` implementations
- action lifecycle: `WorkerAI`
- queued action runtime flow: `WorkerActionPlan`
- worker capabilities: `WorkerActionContext`
- worker action result tuning: `WorkerActionResultStatData`
- worker carry state: `WorkerCarryStorage`
- shared animation definitions: `AnimSet` and stateless `IAnim` implementations
- per-worker animation playback state: `ActorAnimationController`
- scene destination lookup: providers, consumed by selectors
- low-level movement/stat logic: worker capability classes

## File Structure

```text
Assets/
  BehaviorGraph/
    FarmerGraph.asset
    CustomActionNode/
      EnsureWorkerHasActionNode.cs
      RunWorkerCurrentActionNode.cs

  Scripts/
    Actors/
      AI/
        Enemy/
          Enemy.cs

        Friendly/
          Common/
            WorkerAI.cs
            WorkerAIManager.cs
            WorkerActionPlan.cs
            WorkerActionSet.cs
            WorkerNeedsPolicy.cs
            WorkerSelectorType.cs
            Actions/
              AttackAction.cs
              DrinkAction.cs
              EatAction.cs
              MoveAction.cs
              PatrolAction.cs
              RestAction.cs
              SeekAction.cs
            Combat/
              CombatTargetHolder.cs
              EnemyScanner.cs
              PatrolParams.cs
            Context/
              WorkerActionContext.cs
              WorkerCarryStorage.cs
              WorkerMovementStats.cs
              WorkerMover.cs
              WorkerStats.cs
            Data/
              WorkerActionResultStatData.cs
              WorkerActionResultStatEntry.cs
              WorkerStatDelta.cs

          Cook/
            CookActionSelector.cs

          Farmer/
            FarmerActionSelector.cs
            Actions/
              DepositWheatAction.cs
              WorkAction.cs

          Guard/
            GuardActionSelector.cs

    Animation/
      ActorAnimationController.cs
      AnimContext.cs
      AnimSet.cs
      AnimType.cs
      IAnim.cs
      IAnimPlayer.cs
      IAnimSet.cs
      Anims/
        MoveAnim.cs
        WorkAnim.cs

    Enum/
      ActionState.cs
      ActionType.cs
      DestinationType.cs
      TickType.cs

    Interface/
      IAction.cs
      IActionSelector.cs
      IAttackPower.cs
      IDamageable.cs
      IInventory.cs
      ITickable.cs
      IWorkerActionSelectorSetup.cs

    Manager/
      TickManager.cs

    Provider/
      DestinationProvider.cs

    Systems/
      Combat/
        AttackPower.cs
        Health.cs
      Facility/
        WallPerimeter.cs
        WarehouseInventory.cs
      Recruitment/
        AlwaysAffordableCostPolicy.cs
        CandidateStatPreview.cs
        IRecruitmentCostPolicy.cs
        IResidentCandidateView.cs
        IResidentSpawner.cs
        RecruitmentManager.cs
        RecruitmentResult.cs
        ResidentCandidateDefinition.cs
        ResidentCandidateKind.cs
```

### Folder Intent

`Assets/Scripts/Actors/AI`
: Root for actor AI implementation. Separate player-aligned actors under `Friendly` from hostile actors under `Enemy`.

`Assets/Scripts/Actors/AI/Friendly/Common`
: Reusable friendly AI execution lifecycle, plan types, movement/recovery actions, runtime capabilities, and shared action-result data. Common code must not choose a job-specific production policy.

`Assets/Scripts/Actors/AI/Friendly/Common/Actions`
: Actions reusable across friendly roles, currently movement and basic need recovery.

`Assets/Scripts/Actors/AI/Friendly/Common/Context`
: Per-friendly-actor runtime state and capabilities exposed to selectors/actions.

`Assets/Scripts/Actors/AI/Friendly/Common/Data`
: Action result tuning/value types shared by friendly roles. `WheatDelta` remains a known Farmer-oriented field to generalize when the item production model is expanded.

`Assets/Scripts/Actors/AI/Friendly/Cook`
: Cook-worker decision code. The initial `CookActionSelector` implements the common worker selector contract but intentionally produces no plan until Cook actions and their action set are introduced.

`Assets/Scripts/Actors/AI/Friendly/Farmer`
: Farmer-specific decision code and production actions. `FarmerActionSelector` and its actions (`WorkAction`, `DepositWheatAction`) live here. Shared infrastructure (`WorkerAIManager`, `WorkerActionSet`) has been moved to Common.

`Assets/Scripts/Actors/AI/Friendly/Farmer/Actions`
: Farmer-only production and deposit action implementations. Put reusable need/movement actions in Common instead.

`Assets/Scripts/Actors/AI/Enemy`
: Reserved for hostile actor AI. Friendly Common code must not depend on Enemy implementations.

`Assets/Scripts/Animation`
: Shared animation contracts, lookup, and per-actor playback lifecycle. `IAnim` implementations are stateless definitions; `ActorAnimationController` owns runtime Tween state for one visual root.

`Assets/Scripts/Interface`
: Project-wide role contracts shared across multiple domains. Only interfaces used by two or more unrelated domains belong here. Domain-specific interfaces belong inside their `Systems/` subfolder.

`Assets/Scripts/Enum`
: Project-wide enums shared across multiple domains. Only enums used by two or more unrelated domains belong here. Domain-specific enums belong inside their `Systems/` subfolder.

`Assets/Scripts/Manager`
: Project-wide manager components. Only managers that own cross-domain concerns belong here. Domain-specific managers belong inside their `Systems/` subfolder.

`Assets/Scripts/Provider`
: Project-wide provider components. Currently contains destination lookup shared by worker selectors.

`Assets/Scripts/Systems`
: One subfolder per independent feature system. Each subfolder is self-contained and may include interfaces, managers, data, and other types specific to that feature. Adding a new feature system means adding a subfolder here — not a new top-level Scripts folder.

`Assets/Scripts/Systems/Combat`
: Health and attack power components used by scene actors.

`Assets/Scripts/Systems/Facility`
: Scene facility components such as wall perimeter and warehouse inventory.

`Assets/Scripts/Systems/Recruitment`
: Resident recruitment system. Contains its own interfaces, manager, data, and policy implementations.

`Assets/BehaviorGraph/CustomActionNode`
: Unity Behavior custom nodes that bridge Behavior Graph execution to the worker action system.

## Script Structure

### Core Worker Flow

The primary autonomous worker flow is:

```text
WorkerAI.Update()
  -> WorkerAI.TryEnsureCurrentAction()
       if no current action:
         actionSelector.TrySelectAction(context, out plan)
         WorkerAI.SetPlan(plan)
  -> WorkerAI.TickCurrentAction()
       plan.CurrentAction.Start(context) once
       plan.CurrentAction.Tick(context) every frame while running
       move to the next queued action when current action succeeds
       clear plan when the queue is complete or an action fails
```

`WorkerAI` does not decide what the worker should do. It asks the current selector for a `WorkerActionPlan`.

### Plan-Based Action Execution

Selectors no longer return one `IAction` directly. They return a `WorkerActionPlan` containing a queue of actions.

The plan contains:

- the current `IAction`
- queued next `IAction` items
- optional direct destination position

Actions read the active plan through `WorkerActionContext.Plan` when they need execution parameters.

Movement speed and stopping distance are not plan data. They belong to worker movement data and are read through `WorkerMover` / `WorkerMovementStats`.

### Action Set Reuse

`WorkerActionSet` owns one action instance per registered `ActionType` for a worker.

It also owns the reference to `WorkerActionResultStatData` and injects the matching `WorkerActionResultStatEntry` when creating stat-result actions.

Currently registered actions:

- `RestAction`
- `EatAction`
- `DrinkAction`
- `WorkAction`
- `MoveAction`
- `DepositWheatAction`

Because actions may be reused, every action with runtime state must reset that state in `Start()`. For example, timer-based actions reset their timers in `Start()`.

Stat-result actions should not hardcode duration, cost, or reward values. They read their injected result entry and apply its `WorkerStatDelta` to `WorkerStats` on completion. `WorkAction` also reads the injected wheat delta and adds it through `WorkerCarryStorage`.

### Destination-Based Movement

`MoveAction` does not receive a destination through its constructor. It reads destination data from the active `WorkerActionPlan`.

Movement destination can come from:

- a destination injected into `MoveAction` when it is rented from `WorkerActionSet`

When autonomous movement is planned as `MoveAction -> EatAction`, the selector asks `DestinationProvider` for the target position, rents `MoveAction` with that destination, then queues `MoveAction` before the target action.

`WorkerMover` performs the low-level movement using `Vector3.MoveTowards`.

## Script Roles

### CookActionSelector

File: `Assets/Scripts/Actors/AI/Friendly/Cook/CookActionSelector.cs`

Role:

- Provides the initial Cook-worker selector boundary.
- Implements `IActionSelector<WorkerActionContext, WorkerActionPlan>` so it can use the existing WorkerAI execution contract.
- Currently returns no plan because Cook actions, Cook state, and Cook action ownership are not defined yet.

Should not:

- Reuse `WorkerActionSet` merely to satisfy initialization before Cook actions are designed.
- Put cooking execution logic directly in the selector.

### WorkerAIManager

File: `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs`

Role:

- Instantiates `WorkerAI` prefabs.
- Resolves prefab-side worker dependencies such as `WorkerMovementStats`.
- Owns selector templates and chooses which selector type to inject.
- Creates a worker-specific selector instance from the selected template.
- Resolves the selector-side `WorkerActionSet` from the selector template instance and injects it into selectors that require setup.
- Creates `WorkerStats`, `WorkerCarryStorage`, `WorkerMover`, and `WorkerActionContext`.
- Creates one shared `AnimSet`, then creates one `ActorAnimationController` per spawned worker using the worker's child visual root.
- Injects the runtime context and selector into `WorkerAI`.
- Tracks spawned worker instances.

Should not:

- Decide worker behavior priorities.
- Execute actions or tick action plans.
- Mutate worker stats as behavior.
- Require worker prefabs to own `WorkerActionSet`.

### WorkerAI

File: `Assets/Scripts/Actors/AI/Friendly/Common/WorkerAI.cs`

Role:

- Owns the current `WorkerActionPlan`.
- Receives `WorkerActionContext` and the active selector through `Init`.
- Exposes `TryEnsureCurrentAction()`: asks the selector for a plan if no action is currently
  running. Called by `Update()` each frame and shared as the single entry point for Behavior
  Graph bridge nodes that need to guarantee a plan is active.
- Runs `Start`, `Tick`, and `Cancel` on the active action.
- Advances to the next queued action when the current action succeeds.
- Clears the active plan when the queue completes or an action fails.

Should not:

- Decide worker behavior priorities.
- Mutate stats directly as behavior.
- Implement movement, eating, resting, animation, or UI behavior.
- Hardcode destination selection logic.
- Instantiate itself or construct its own runtime context.
- Resolve selector dependencies.

### WorkerActionContext

File: `Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerActionContext.cs`

Role:

- Provides actions with access to worker capabilities and state.
- Contains `Transform`, `WorkerMover`, `WorkerStats`, `WorkerMovementStats`, `WorkerCarryStorage`, `IAnimPlayer`, and the current `WorkerActionPlan`.

Should not:

- Become a general behavior executor.
- Decide next action.
- Replace selector logic.

Important:

- `SetPlan` and `ClearPlan` exist so actions can read the active plan through context.
- Plan ownership conceptually belongs to `WorkerAI`, not to context.

### WorkerActionPlan

File: `Assets/Scripts/Actors/AI/Friendly/Common/WorkerActionPlan.cs`

Role:

- Runtime instruction object built by selectors or bridge nodes.
- Carries a queue of actions to execute in order.
- Tracks the actions rented from `WorkerActionSet` so they can be returned after execution.

Factory methods:

- `Create(params IAction[] actions)`: creates a queued action plan and tracks the rented actions.

Should not:

- Contain behavior logic.
- Perform provider lookup.
- Start or tick actions.
- Store worker stat-like values such as move speed or stopping distance.

### FarmerActionSelector

File: `Assets/Scripts/Actors/AI/Friendly/Farmer/FarmerActionSelector.cs`

Role:

- Default worker "brain".
- Reads `WorkerStats`.
- Uses `DestinationProvider` as an explicit selector dependency.
- Uses `WorkerActionSet` for both action rental and action result stat data lookup.
- Chooses the next intended behavior.
- Builds a `WorkerActionPlan`.
- Queues movement before the selected behavior if the worker is not already at the required destination.

Current priority:

1. If thirst, hunger, or fatigue is critical, queue the matching recovery action with movement if needed.
2. If carried wheat is full, queue move-to-warehouse then deposit wheat, or deposit immediately if already there.
3. If thirst, hunger, or fatigue is at the prepare threshold, queue the matching recovery action with movement if needed.
4. Otherwise, queue move-to-work-destination then work, or work immediately if already there.

Should not:

- Execute actions directly.
- Move transforms.
- Mutate stats.
- Read concrete action implementations for cost/reward values.
- Play animation or UI.

Replaceability:

- Any other selector can replace this class if it implements `IActionSelector<WorkerActionContext, WorkerActionPlan>`.
- Replacing the selector should not require changes to `WorkerAI` or actions.

### WorkerActionSet

File: `Assets/Scripts/Actors/AI/Friendly/Common/WorkerActionSet.cs`

Role:

- Registry from `ActionType` to `IAction`.
- Owns action instances for a worker.
- Owns the configured `WorkerActionResultStatData` reference.
- Provides action result stat entries to selectors through `TryGetResultStatEntry`.
- Injects result stat entries into timer-based stat actions when creating them.

Current limitation:

- Actions are registered manually in `Awake()`.
- If a new action is added, it must be registered here unless registration is later moved to data-driven setup.
- New stat-result actions need a matching `WorkerActionResultStatData` entry.

### IAction

File: `Assets/Scripts/Interface/IAction.cs`

Role:

- Common contract for every worker behavior.

Methods:

- `Start(WorkerActionContext context)`: initialize one run of the action.
- `Tick(WorkerActionContext context)`: advance the action and return `ActionState`.
- `Cancel(WorkerActionContext context)`: cleanup/interruption hook.

Important:

- New worker behavior should normally be a new `IAction`.
- Action implementations should not assume a specific selector.
- Action implementations may use context capabilities.

### MoveAction

File: `Assets/Scripts/Actors/AI/Friendly/Common/Actions/MoveAction.cs`

Role:

- Executes movement behavior from the active plan.
- Uses the destination injected when the action is rented from `WorkerActionSet`.
- Delegates low-level movement to `WorkerMover`.
- Requests `AnimType.Move` through `WorkerActionContext.Animation` and supplies horizontal facing from the destination direction.
- Stops its animation when movement completes, fails, or is cancelled.

Depends on:

- `WorkerActionContext.Plan`
- `WorkerActionContext.Mover`

Should not:

- Decide why the worker is moving.
- Choose destination type by itself.
- Create new plans.

### EatAction, DrinkAction, RestAction, WorkAction, DepositWheatAction

Files:

- `EatAction.cs`
- `DrinkAction.cs`
- `RestAction.cs`
- `WorkAction.cs`
- `DepositWheatAction.cs`

Role:

- Timer-based stat actions.
- Reset timer in `Start()`.
- Return `Running` while timer remains.
- Apply their injected `WorkerStatDelta` to `WorkerStats` on completion.
- Return `Success` after applying effect.
- `WorkAction` adds the injected wheat delta to `WorkerCarryStorage` on completion.
- `WorkAction` requests `AnimType.Work` through `WorkerActionContext.Animation` and stops it on completion or cancellation.
- `DepositWheatAction` clears carried wheat through `WorkerCarryStorage` on completion.

Current stat effects live in `WorkerActionResultStatData`:

- Eat reduces hunger.
- Drink reduces thirst.
- Rest reduces fatigue.
- Work increases hunger, thirst, and fatigue.
- Work adds wheat to carried wheat.
- DepositWheat clears carried wheat at the warehouse destination.

They should not:

- Hardcode duration, cost, or reward numbers.
- Reference selector logic or decide whether the worker should continue working.
- Ask `WorkerActionResultStatData` directly during `Tick()`.

### WorkerActionResultStatData

Files:

- `Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatData.cs`
- `Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatEntry.cs`
- `Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerStatDelta.cs`

Role:

- ScriptableObject database for worker action result tuning.
- Maps `ActionType` to duration, `WorkerStatDelta`, and wheat delta.
- Provides one source of truth for both action execution and selector prediction.

Should not:

- Execute actions.
- Mutate `WorkerStats`.
- Decide behavior priority.
- Depend on `WorkerAI`.

### WorkerStats

File: `Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerStats.cs`

Role:

- Owns worker stat values.
- Clamps hunger, thirst, and fatigue.
- Applies generic `WorkerStatDelta` values.
- Implements `ITickable`.

Should not:

- Decide what action to take.
- Know about selectors, actions, destinations, or movement.
- Know action-specific cost or reward numbers.

### WorkerCarryStorage

File: `Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerCarryStorage.cs`

Role:

- Owns the worker's carried wheat amount.
- Clamps carried wheat to the configured capacity.
- Provides `IsWheatFull` for selectors and add/deposit methods for actions.

Should not:

- Decide when to work or deposit.
- Know destination lookup or selector priority.
- Hardcode action reward values.

### WorkerMover

File: `Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerMover.cs`

Role:

- Performs low-level movement.
- Stores current target position and whether a move is active.
- Uses `WorkerMovementStats` for move speed and stopping distance.
- Returns `ActionState` from `TickMove()`.

Should not:

- Decide destination.
- Know about hunger, thirst, fatigue, or action priorities.
- Read selectors or action sets.

### WorkerMovementStats

File: `Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerMovementStats.cs`

Role:

- Unity component that owns movement-related worker values.
- Currently exposes Inspector-configurable move speed and stopping distance.
- Keeps movement-affecting values out of `WorkerActionPlan`.

Should not:

- Decide movement destination.
- Execute movement.
- Know about selectors or action queues.

### AnimSet And IAnim

Files:

- `Assets/Scripts/Animation/AnimSet.cs`
- `Assets/Scripts/Animation/IAnimSet.cs`
- `Assets/Scripts/Animation/IAnim.cs`
- `Assets/Scripts/Animation/Anims/MoveAnim.cs`
- `Assets/Scripts/Animation/Anims/WorkAnim.cs`

Role:

- `AnimSet` is the shared registry from `AnimType` to one stateless `IAnim` definition.
- `IAnim` creates a Tween from the supplied `AnimContext` but does not retain the target or active Tween.
- A single `AnimSet` may be shared by every worker managed by one `WorkerAIManager`.

Should not:

- Store per-worker Transform, facing, or active Tween state.
- Rent or pool animation definitions; the registered definitions are reusable objects.

### ActorAnimationController

File: `Assets/Scripts/Animation/ActorAnimationController.cs`

Role:

- Implements `IAnimPlayer` for one actor visual root.
- Resolves animation definitions through the shared `IAnimSet`.
- Owns that actor's active Tween, active `AnimType`, and horizontal facing.
- Stops the previous Tween before starting another animation.

Important:

- The target must be a child visual Transform, not the gameplay root moved by `WorkerMover`.
- Facing set by movement remains available to animations such as Work that do not choose a new direction.
- `WorkerAI` does not resolve or call this controller directly; actions access it through `WorkerActionContext.Animation`.

### DestinationProvider

File: `Assets/Scripts/Provider/DestinationProvider.cs`

Role:

- Inspector-configured destination lookup.
- Maps `ActionType` to a scene `GameObject`.
- Returns destination position through `TryGetDestinationPosition`.

Should not:

- Decide which destination is needed.
- Execute movement.
- Know worker stats or action priority.

### TickManager

File: `Assets/Scripts/Manager/TickManager.cs`

Role:

- Owns grouped ticking for objects implementing `ITickable`.
- Supports slow, normal, and fast tick groups.

Current status:

- `WorkerAI` has commented-out registration code, so worker stats currently tick directly in `WorkerAI.Update()`.

### Behavior Graph Nodes

`EnsureWorkerHasActionNode`
: Calls `WorkerAI.TryEnsureCurrentAction()`. Depends only on `WorkerAI` — does not search for
  selectors or `WorkerActionSet` directly.

`RunWorkerCurrentActionNode`
: Calls `WorkerAI.TickCurrentAction()` and converts `ActionState` to Behavior Graph `Status`.

## Dependency Structure

### High-Level Dependency Direction

Preferred dependency direction:

```text
WorkerAI
  -> IActionSelector<WorkerActionContext, WorkerActionPlan>

FarmerActionSelector
  -> WorkerActionSet
  -> WorkerActionResultStatData through WorkerActionSet lookup
  -> DestinationProvider

WorkerActionPlan
  -> IAction
  -> WorkerActionContext
  -> Worker capabilities
```

The intent is that higher-level orchestration depends on role contracts and context, while low-level systems do not depend back on the decision layer.

### Runtime Autonomous Flow

```text
WorkerAIManager
  instantiates WorkerAI prefab
  creates one shared AnimSet
  creates WorkerStats / WorkerCarryStorage / WorkerMover
  creates one ActorAnimationController for the worker child visual root
  creates WorkerActionContext containing IAnimPlayer
  creates a worker-specific IActionSelector from its selector entries
  injects the selector template's WorkerActionSet into selectors that require setup
  calls WorkerAI.Init(context, selector)

WorkerAI
  has WorkerActionContext
  has current WorkerActionPlan
  has IActionSelector
  asks selector for plan

FarmerActionSelector
  reads WorkerStats through context
  uses WorkerActionSet to get IAction
  may use WorkerActionSet to read action result data
  uses its DestinationProvider dependency to decide if movement is needed
  returns WorkerActionPlan containing one or more queued actions

WorkerAI
  stores plan
  exposes plan through context
  runs plan.CurrentAction
  advances to the next queued action on success

IAction implementation
  reads context
  executes behavior
  returns ActionState
```

### Movement Dependency Flow

```text
FarmerActionSelector
  decides intended action
  queues MoveAction before the intended action when destination movement is needed

MoveAction
  reads active WorkerActionPlan
  uses the Vector3 destination injected by WorkerActionSet
  calls WorkerMover.StartMove
  calls WorkerMover.TickMove

WorkerMover
  moves Transform
  reports Running, Success, or Failed
```

### Stat Action Dependency Flow

```text
EatAction / DrinkAction / RestAction / WorkAction / DepositWheatAction
  use injected WorkerActionResultStatEntry
  use WorkerActionContext.Stats
  call WorkerStats.Apply(entry.StatDelta)

WorkAction
  calls WorkerCarryStorage.AddWheat(entry.WheatDelta)

DepositWheatAction
  calls WorkerCarryStorage.DepositAllWheat()

WorkerActionResultStatData
  owns action duration and stat delta tuning

WorkerStats
  owns clamped hunger/thirst/fatigue values

WorkerCarryStorage
  owns clamped carried wheat values
```

### Animation Dependency Flow

```text
MoveAction / WorkAction
  request an AnimType through WorkerActionContext.Animation (IAnimPlayer)

ActorAnimationController (one per worker)
  owns active Tween and facing
  resolves a stateless IAnim through the shared IAnimSet

AnimSet (shared by workers)
  maps AnimType to IAnim

IAnim
  creates a Tween targeting that worker's child visual Transform
```

### Behavior Graph Dependency Flow

```text
Behavior Graph Node
  -> WorkerAI
  -> WorkerActionPlan / IAction
  -> WorkerActionContext
```

Behavior Graph nodes should remain bridge code. They should not become the main owner of worker behavior logic.

## Extension Guide

### Adding A New Worker Behavior

1. Add a new value to `ActionType` if the behavior needs selection through `WorkerActionSet`.
2. Create reusable movement/need behavior under `Assets/Scripts/Actors/AI/Friendly/Common/Actions`; create job-specific behavior under the matching role folder such as `Friendly/Farmer/Actions` or `Friendly/Cook/Actions`.
3. Register the action in `WorkerActionSet`.
4. Add a `WorkerActionResultStatData` entry if the behavior has duration, cost, or reward values.
5. Update the selector or create a new selector that can choose the action.
6. Queue multiple actions in `WorkerActionPlan` when the behavior requires a sequence.
7. Add required plan data to `WorkerActionPlan` only when the data belongs to the action sequence itself, not to worker stats or low-level capability settings.

Do not put new behavior directly in `WorkerAI`.

### Adding A New Animation

1. Add the identifier to `AnimType`.
2. Add a stateless `IAnim` implementation under `Assets/Scripts/Animation/Anims`.
3. Register one instance in `AnimSet`.
4. Request the animation from the owning action through `WorkerActionContext.Animation`.
5. Stop the matching animation on success, failure, and cancellation.

Do not add animation lookup or Tween ownership to `WorkerAI` or selectors.

### Adding A New Decision Policy

1. Create a class implementing `IActionSelector<WorkerActionContext, WorkerActionPlan>`.
2. Keep dependencies explicit through serialized fields, context, or setup methods.
3. Add a `WorkerSelectorType` value if the selector needs a new role/category.
4. Add the selector template to `WorkerAIManager`'s selector entries.

Do not modify `WorkerAI` just to change behavior priority.

### Adding A New Destination

1. Add or reuse an `ActionType`.
2. Add a matching `ActionType` entry to `DestinationProvider` in the Inspector.
3. Update the selector to queue `MoveAction` before that action when movement is required.

Do not hardcode scene positions inside actions.

### Changing Movement Logic

Change `WorkerMover` when the low-level movement algorithm changes.

Examples:

- switch from direct transform movement to Rigidbody2D movement
- add acceleration
- add pathfinding
- add obstacle avoidance

Do not spread movement math into selectors or unrelated actions.

## Known Design Notes

- Some current code still uses older style patterns, such as explicit `== null` checks and mixed private field naming. New edits should follow `CodeConvention.md`.
- `Assembly-CSharp.csproj` may be regenerated by Unity. If new scripts are added and command-line `dotnet build` cannot see them, Unity regeneration or project-file update may be needed.
- `DestinationProvider` currently stores `GameObject` references. If only position is needed, `Transform` may be cleaner later.
- `DestinationType.cs` still exists, but current destination lookup is based on `ActionType`.
