# Codex Project Notes

이 파일은 Codex가 프로젝트 루트에서 작업을 시작할 때 먼저 참고할 프로젝트 안내입니다.

## PublicMD 문서 인덱스

프로젝트 루트의 `PublicMD` 폴더에는 장기 유지되는 작업 문서가 있습니다. 모든 작업에서 모든 문서를 무조건 읽을 필요는 없지만, 아래 조건에 해당하면 필요한 문서를 먼저 읽고 작업하세요.

### `PublicMD/ProjectStructure.md`

역할:

- Unity 2D worker/NPC 행동 프로토타입의 전체 구조를 설명합니다.
- `WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, `WorkerActionSet`, `WorkerDefaultActionSelector`, `WorkerMover`, `WorkerStats`, `DestinationProvider`, Behavior Graph 노드들의 책임과 의존 방향을 정리합니다.
- 새 worker 행동, 새 selector, 새 destination, 이동 로직 변경을 어디에 구현해야 하는지 안내합니다.

읽어야 하는 경우:

- worker/NPC 행동 흐름을 수정할 때
- `WorkerAI`, selector, action, action plan, context, movement, stat, provider 관련 코드를 변경할 때
- 새 `IAction` 구현, 새 `ActionType`, 새 destination, 새 decision policy를 추가할 때
- Behavior Graph custom node와 worker action system 사이의 연결을 수정할 때
- 구조상 어느 파일에 코드를 둬야 할지 판단이 필요할 때

### `PublicMD/CodeConvention.md`

역할:

- 이 Unity 프로젝트의 C# 코드 작성 규칙을 설명합니다.
- 네이밍, 폴더 배치, 책임 분리, action/plan ownership, interface 사용, selector 규칙, enum 사용, null check, Unity 컴포넌트 패턴, field style, comment 기준을 정리합니다.

읽어야 하는 경우:

- C# 스크립트를 새로 만들거나 수정할 때
- private field, serialized field, public property, enum, interface 이름을 정할 때
- action, selector, provider, manager, mover 등 책임 경계를 판단할 때
- Unity lifecycle, `SerializeField`, `RequireComponent`, `Try...` 메서드, null check 스타일을 맞춰야 할 때
- 기존 코드와 새 코드의 스타일 차이를 줄여야 할 때

## 작업 원칙

- `CLAUDE.md` 파일 및 `.claude` 폴더는 오로지 Claude를 위한 파일 및 폴더입니다. Codex는 해당 파일과 폴더를 절대 참조하거나 수정하지 않습니다.
- 구조나 책임 배치가 관련된 작업은 `ProjectStructure.md`를 먼저 읽으세요.
- C# 코드 변경 작업은 `CodeConvention.md`를 먼저 읽으세요.
- worker 행동 변경은 기본적으로 `WorkerAI`에 직접 넣지 말고 selector, action, plan, context, provider, mover 중 알맞은 책임 위치에 둡니다.
- 새 worker 행동은 보통 `Assets/Scripts/Actors/Worker/Actions` 아래의 새 `IAction` 구현으로 시작합니다.
- action은 실행 흐름과 성공/실패/취소 규칙을 담당하고, selector는 어떤 action plan을 실행할지 결정합니다.
- `WorkerAI`는 active plan lifecycle과 tick 실행을 담당하며 행동 우선순위나 세부 행동 구현을 직접 소유하지 않습니다.
- 기존 파일의 로컬 스타일이 명확하면 그 스타일을 우선 유지하되, 새 코드에는 `CodeConvention.md`의 규칙을 적용합니다.
