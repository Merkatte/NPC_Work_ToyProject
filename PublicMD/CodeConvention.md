# Code Convention

## Scope
- This convention applies to scripts in this Unity project.
- When an existing file already has a clear local style, keep edits consistent with that file.

## Naming
- Use PascalCase for class names, enum names, method names, properties, and public fields.
- Use camelCase for private runtime fields.
- Use `_camelCase` for private serialized fields when the field is configured in the Inspector.
- Use clear domain names such as `WorkerActionPlan`, `DestinationProvider`, and `ActionType`.

## Structure
- Keep one primary class per `.cs` file unless the secondary type is a small serializable data holder used only by that file.
- Put AI actor code under `Assets/Scripts/Actors/AI`, split first into `Friendly` and `Enemy`.
- Put reusable friendly AI execution, movement, needs, context, and shared action data under `Assets/Scripts/Actors/AI/Friendly/Common`.
- Put job-specific behavior under its friendly role folder, such as `Friendly/Farmer` or `Friendly/Cook`.
- The top-level `Assets/Scripts/` folders are fixed by type: `Actors`, `Animation`, `Enum`, `Interface`, `Manager`, `Provider`, `Systems`. Do not add new top-level folders here for individual features.
- `Interface/`, `Enum/`, `Manager/`, `Provider/` hold only project-wide types used across two or more unrelated domains.
- New independent feature systems go under `Assets/Scripts/Systems/<FeatureName>/`. The subfolder is self-contained and may include interfaces, managers, data, enums, and implementations specific to that feature.
- Do not create a `Systems/` subfolder for a feature until it has enough scripts to justify separation. A single utility class does not need its own subfolder.
- Each script must have a clear primary responsibility.
- Do not split scripts only for the sake of splitting them. If two pieces of code serve the same practical responsibility, keep them together unless separating them removes a real dependency or clarifies ownership.
- Do split code when a responsibility is clearly different. A parser should parse, a manager should manage, a provider should provide, and an action should execute behavior.

## Responsibility Boundaries
- `WorkerAI` runs ticks and executes the current action plan. It should not decide worker behavior or implement action details.
- `IAction` implementations contain behavior execution logic.
- `WorkerActionContext` exposes worker information and capabilities needed by actions. It should not become the owner of all behavior logic.
- Selectors decide what the worker should do next and build the required action plan.
- Providers own lookup and management of external data such as destinations.
- Low-level movement details belong in movement components such as `WorkerMover`.
- A reader should usually understand or modify one concern by reading one matching script: decision logic in a selector, behavior logic in an action, movement logic in a mover, and destination lookup in a provider.

## Action Rules
- Every new behavior must be represented by an `IAction` implementation.
- Selectors choose or build an action plan; actions execute the plan.
- Selectors must be replaceable, so they should depend on interfaces or context data instead of concrete AI control flow.
- `WorkerActionContext` passes worker capabilities to actions. It must not become the object that performs every behavior directly.
- Adding a new worker behavior should normally mean adding a new `IAction` implementation.
- `IAction` implementations should be safe to run through `WorkerAI` and should not assume a specific selector implementation.
- Actions may call worker capabilities from context, but the action must still own the behavior flow, success/failure rules, and cancellation behavior.
- Actions registered in an action set may be reused. Any per-run state must be initialized in `Start()`.
- `Cancel()` must be safe to call even if the action has not fully started or has already been stopped.
- `Tick()` should not allocate avoidable objects and should not perform expensive lookups repeatedly.
- An action may read the current plan, but it should not create a new plan or decide the next high-level behavior.
- Action duration, cost, and reward tuning should live in worker data assets or data providers, not as hardcoded values inside `WorkerAI` or concrete action implementations.

## Plan Ownership
- Selectors build action plans.
- `WorkerAI` owns the active plan lifecycle.
- Actions execute the active plan and may read plan data required for execution.
- Context may expose the active plan, but context should not decide or replace plans on its own.
- A plan should contain execution parameters, not hidden behavior logic.

## Animation Rules
- `WorkerAI` and selectors must not resolve or play animations.
- Actions request playback through `WorkerActionContext.Animation` using the `IAnimPlayer` contract.
- `IAnim` implementations must remain stateless. They must not retain actor Transforms, facing, or active Tweens.
- `AnimSet` is a shared read-only registry of reusable `IAnim` definitions; do not add rent/return pooling for these definitions.
- Create one `ActorAnimationController` per actor. It owns that actor's active Tween and facing state.
- Tween visual properties only on a child visual root. Do not animate the gameplay root moved by `WorkerMover`.
- An action that starts an animation must stop its matching `AnimType` on completion, failure, and cancellation.
- Actions that determine direction, such as `MoveAction`, may set `FlipX`. Direction-neutral actions should preserve the controller's current facing.

## Data Assets
- A data asset (ScriptableObject or CSV) must have one clear purpose, the same way a script has one
  clear responsibility. Separate data by role, not by convenience.
- A base-stat asset for an entity must contain only that entity's own intrinsic stats. For example,
  a WorkerAI stat asset holds only true worker stats such as move speed or health.
- Data that is affected by a stat but is not itself that entity's stat must live in a separate
  ScriptableObject or CSV. For example, armor defense, equipment modifiers, or item bonuses belong
  in their own data asset, not inside the worker base-stat asset.
- Keep one source of truth per concept. Do not merge unrelated concepts (base stats, equipment
  tuning, drop tables, action tuning) into a single mixed asset.
- Match the storage format to the data shape: use ScriptableObject for small designer-tuned sets and
  enum-keyed lookups; use CSV when the data is large, tabular, or row-heavy and edited in bulk.
- Place a data asset under the domain folder that owns it, the same as scripts. Shared friendly AI
  action data belongs under `Assets/Scripts/Actors/AI/Friendly/Common/Data`; role-specific data belongs
  under that role folder. Do not put domain-specific data in a global
  folder.
- A consumer should read one concept from one data asset. Mixing concepts forces readers to load and
  understand unrelated data just to use one value.
- `WorkerActionResultStatData` is the existing precedent: it holds only per-`ActionType` action
  result tuning and nothing else. New data assets should follow the same single-purpose shape.

## Interfaces
- Use interfaces when multiple scripts share the same role and other scripts should depend on that shared role.
- Do not create a one-to-one interface for a single concrete script unless there is an immediate, concrete need.
- Prefer existing role interfaces such as `IAction`, `IActionSelector`, and `IActionSet` when consuming interchangeable behavior.

## Selectors
- Selectors are allowed to know more than ordinary scripts because they are the worker decision layer.
- Selector dependencies must be explicit through serialized fields, constructor/setup data, context, or clearly named lookup methods.
- Replacing a selector should not require modifying `WorkerAI`, actions, movement, or provider implementation code.
- Selector logic should decide intent and required plan data. It should not perform low-level movement, stat mutation, animation, UI, or direct behavior execution.
- Selectors may reference action sets, providers, enums, and context data when building a plan.
- Selector dependencies should be visible in fields or setup code. Avoid hidden global lookup from selector logic.

## Enums
- Enum access should be limited to the layers that actually make decisions with those enum values.
- Keep enum usage away from lower-level execution scripts when an interface or plan data can express the dependency more clearly.
- It is acceptable to use enums when avoiding them would make the code harder to read, but the allowed enum-reading layer should remain clear.
- `WorkerAI` should stay mostly enum-agnostic. Decision-level scripts such as selectors may use enums when building plans.

## Null Checks
- Prefer Unity-style truthiness for Unity object references: use `if (!component)` instead of `if (component == null)` when the type supports it.
- Use null-conditional calls such as `handler?.Invoke()` when skipping a missing reference is valid behavior.
- Use explicit null checks when the type does not support Unity-style truthiness or when the failure path must be handled clearly.
- Avoid casual `object != null` expressions when a clearer project style is available.
- Unity-style truthiness applies only to `UnityEngine.Object` types such as `MonoBehaviour`, `GameObject`, `Transform`, and assets.
- Plain C# objects, interfaces, and data classes may require `== null`, `!= null`, or `is null` checks because `!object` is not valid for them.

## Unity
- Prefer `[SerializeField] private` fields over public Inspector fields.
- Use `Try...` methods when failure is valid at runtime.
- Avoid LINQ in per-frame or gameplay decision code when a simple loop is enough.
- Avoid repeated `GetComponent`, `FindObjectOfType`, object allocation, and string formatting inside `Update()`, tick methods, selectors, and action `Tick()` methods.
- Resolve required Unity references in `Awake()`, `Start()`, explicit init methods, or serialized fields.
- Use `RequireComponent` when a component cannot operate without another component on the same GameObject.

## Namespaces
- Use namespaces consistently once a domain has enough scripts to benefit from them.
- Namespace names should follow domain ownership, not folder convenience alone.
- Avoid creating narrow namespaces only for one enum or one file.
- Keep namespace spelling stable. Renaming a namespace should be treated as a project-wide refactor.

## Field Style
- Use `_camelCase` for private fields, including private serialized fields.
- Use PascalCase for public properties.
- Use `readonly` when a field is assigned only during construction and never changes afterward.
- Use `const` or `static readonly` for fixed values instead of magic numbers repeated across methods.

## Comments
- Add comments only when the reason is not obvious from the code.
- Keep TODOs specific and actionable.
