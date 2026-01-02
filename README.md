# 🎮 근무 중 이상 무 (Report: No Anomalies)

## 🎮 게임 정보
| 항목 | 내용 |
| :--- | :--- |
| **타이틀** | 근무 중 이상 무(Report: No Anomalies) |
| **장르** | 3D 1인칭 타임어택 관찰&진압 액션 |
| **플랫폼** | PC(Steam) |
| **플레이 타임** | 1회 플레이 기준 30~50분 |
| **배경** | 단체 폭동이 일어나기 직전의 교도소 |
| **목표** | 7일 동안 폭동 게이지가 100에 도달하지 않도록 죄수들을 관리하며 생존 |
| **기획 의도** | **관찰과 판단의 긴장감 극대화**<br>- 단순히 죄수들을 때려잡는 액션 게임이 아니라, '이 상황이 정상인가 아닌가?'를 판단해야 하는 심리적 압박<br>└ \<Paper, Please\>, \<8번 출구\>의 메커니즘을 액션에 접목<br><br>**불리한 상황에서 생존하는 스릴**<br>- 1명의 교도관 대 다수의 흉악범이라는 구도를 통해, 사냥꾼이 아닌 위태로운 관리자의 입장에서 오는 스릴 |
| **핵심 감정** | **편집증에 가까운 의심**<br>- 평범한 행동조차 수상하게 보이게 만들어, 긴장을 풀 수 없음<br><br>**쫓기는 듯한 압박감**<br>- 타임어택 요소와 폭동 게이지라는 실패의 가시화가 심리적 압박 제공<br><br>**정당화된 폭력의 카타르시스**<br>- '나의 정확한 판단으로 위기를 사전에 차단했다'는 안도감과 성취감이 동반된 타격 행위<br>- '무조건 패는 게 아니다. 팰 만 해서 팬다!' |

---

## 🕹️ 조작 키 (Control Keys)
| 기능 | 키 바인딩 | 기능 | 키 바인딩 |
| :--- | :--- | :--- | :--- |
| **이동** | `W`, `A`, `S`, `D` | **상호작용** | `E` |
| **달리기** | `SHIFT` | **특정 UI 닫기** | `E` |
| **앉기** | `CTRL` | **시점 조작** | 마우스 이동 |
| **점프** | `SPACE` | **공격** | 마우스 좌클릭 |
| **일시 정지** | `ESC` | **상세 보기 - 오브젝트 회전** | 마우스 우클릭 |
| **튜토리얼 스킵** | `Q` | **상세 보기 - 추가 상호작용** | 마우스 좌클릭 |

---

## 📝 플레이 개요
* **신입 교도관의 임무**
  * 플레이어는 집단 폭동과 집단 탈옥이 일어나기 직전의 교도소에 새로 근무하게 된 신입 교도관이다.
  * 플레이어의 임무는 7일 동안 교도소에서 3교대로 근무하며, 죄수들이 단체 행동(폭동/탈옥)을 일으키지 않도록 교도소의 분위기를 조절하는 것.
  * 플레이어는 교도소를 순찰하며 문제가 있는 죄수들은 진압하고, 멀쩡한 죄수들은 자극하지 않으며 교도소 내의 분위기(폭동 게이지)를 조절해야 한다.
* **게임 진행 방식**
  * 1회의 게임 플레이는 총 7일로 구성되어 있고, 플레이어 캐릭터는 각각의 하루마다 8시간씩 근무한다.
  * 게임 내 8시간은 현실 시간으로는 8분이며, 플레이어는 이 시간 동안 교도소 안을 순찰하며 시끄러운 감방들을 확인한다.

# 프로젝트 개발 상세 내역

## 👤 손준형

### Prisoner & Anomaly System (죄수 및 이상 현상)
게임의 핵심 긴장감을 담당하는 죄수 AI와 게임 플레이의 변수를 창출하는 이상 현상(Anomaly) 시스템을 구현했습니다. FSM 기반의 행동 제어와 데이터 주도형 설계를 통해 확장성 있고 유연한 시스템을 구축했습니다.

#### 1. 죄수 AI 시스템 (Prisoner AI)
유한 상태 머신(Finite State Machine, FSM) 패턴을 도입하여 죄수들의 복잡한 행동 패턴을 체계적으로 관리합니다.



* **FSM 아키텍처:**
    * PrisonerFSM을 통해 상태 간의 전이와 업데이트를 중앙에서 관리합니다.
    * 각 행동 로직을 독립적인 클래스(State)로 분리하여 유지보수성을 높였습니다.
* **주요 행동 상태 (States):**
    * **Idle:** 평상시 감방 내에서 대기하거나 자유 행동을 취하는 상태입니다.
    * **Inspection (점호):** 정해진 스케줄에 따라 기상(StandUp), 집합 장소로 이동(Moving), 점호 대기(WaitAtPoint)하는 일련의 시퀀스를 수행합니다.
    * **Combat:** 플레이어 또는 위협 대상에게 반응하여 공격을 수행하는 전투 상태입니다.
    * **Cower / Dead:** 위협에 처했을 때 움츠리거나 사망 처리되는 등 상황에 따른 리액션을 구현했습니다.
* **컨트롤러 (Prisoner Controller):**
    * NavMeshAgent를 활용한 길 찾기 및 이동 로직을 구현했습니다.
    * 애니메이션 시스템과 연동하여 상태 변화에 따른 자연스러운 모션을 처리합니다.

#### 2. 이상 현상 시스템 (Anomaly System)
플레이어에게 예측 불가능한 경험을 제공하기 위한 이상 현상 생성 및 관리 시스템입니다.

* **이상 현상 관리자 (Anomaly Distributor):**
    * 게임 내 발생하는 모든 이상 현상의 스폰과 생명 주기를 관리하는 매니저입니다.
    * 게임 진행 상황에 따라 적절한 타이밍과 위치에 이상 현상을 배치합니다.
* **데이터 주도형 설계 (Data-Driven Design):**
    * ScriptableObject(AnomalyDatabaseSO)를 활용하여 다양한 이상 현상의 데이터를 에셋 형태로 관리합니다. 코드 수정 없이 기획 데이터 변경만으로 다양한 변수를 제어할 수 있습니다.
* **스폰 및 상호작용:**
    * **Anomaly Spawn Slot:** 이상 현상이 발생할 수 있는 잠재적 위치를 정의하고 관리합니다.

---

## 👤 유원영

### -EventBus.cs
프로젝트 전반에서 시스템 간 결합도를 낮추기 위한 전역 이벤트 전달 메커니즘
(ex: Scene간 이동/UI 전환 / 게임 페이즈 변경 / 입력 상태)



* 이벤트 타입(Struct) 단위로 구독 리스트를 분리해서 책임 경계 유지.
* 어떤 구독자에서 예외가 발생해도 다른 구독자에게 이벤트가 전달이 중단안되도록 보호로직 포함.
* **\*WeakRefernce 기반 구독 구조**
    * EventBus가 콜백을 소유하지 않고 수신 객체가 사라졌을 경우 자동으로 정리
    * 씬 전환 시 Unsubscribe 누락으로 인한 메모리 누수 위험을 최소화 하기 위한 장치
* UIScene에 있는 UI들은 EventBus를 통해 EventStruct(이벤트 모음집)에 있는 이벤트를 받아 활성화/비활성화를 하고 있음.

### -InspectionManager.cs
플레이 중 특정 오브젝트를 상세보기 상태로 전환하고 관리하는 시스템
* 입력, 카메라, 상호작용 흐름을 플레이어의 조작과 분리시켜서 제어함.

### -AudioManager.cs
프로젝트의 사운드출력을 담당하는 매니저 클래스
* AudioMixer의 사운드의 조절 파라미터 종류에 따라 사운드를 맞게 출력함.

---

## 👤 임성규 (Im Sung-gyu)

### 1. 페이즈 관리 시스템 (GameManager)
게임의 흐름을 체계적으로 제어하기 위해 **페이즈(Phase) 전환 시스템**을 구축했습니다. 각 페이즈 진입 시 필요한 로직을 실행하고 `EventBus`를 통해 시스템 전반에 상태 변경을 전파합니다.

* **중앙 집중형 페이즈 제어 (`ChangePhase`)**
    * 새로운 페이즈 진입 시 기존 상태와 비교하여 중복 실행을 방지합니다.
    * `switch`문을 통해 각 페이즈(`Briefing`, `Patrol`, `Settlement`, `Ending` 등)에 최적화된 초기화 메서드를 호출합니다.
    * 페이즈 변경 정보를 `EventBus`로 발행하여 다른 클래스들이 독립적으로 반응할 수 있도록 설계했습니다.

* **주요 페이즈별 실행 로직**
    * **초기화 (`OnEnterNotStarted`)**: 새로운 루프 시작 시 날짜, 폭동 게이지, 플레이어 체력 등 기초 데이터를 초기화합니다.
    * **준비 및 브리핑 (`OnEnterStandby`, `OnEnterBriefing`)**: 날짜 카운트를 증가시키고 브리핑 시퀀스를 트리거합니다.
    * **순찰 (`OnEnterPatrol`)**: 순찰 제한 시간(480초) 설정, UI 타이머 초기화, 정산 보고용 기초 데이터를 캐싱하고 타이머 코루틴을 시작합니다.
    * **정산 (`OnEnterSettlement`)**: 진행 중인 타이머를 중지하고 정산 시스템 시작 이벤트를 발행합니다.
    * **엔딩 (`OnEnterEnding`)**: 최종 엔딩 종류를 판별하고, 세이브 데이터를 로드/업데이트하여 새로운 엔딩 해금 시 이를 메타 데이터에 기록합니다.

---

### 2. 씬 흐름 제어 (FlowController)
게임 전반의 씬 로드 및 언로드를 담당하며, 사용자에게 끊김 없는 경험을 제공하기 위한 **로딩 시퀀스**를 구현했습니다.

* **안정적인 씬 전환 (`ReloadPlaySceneRoutine`)**
    * 씬 교체 시 카메라나 오디오 리스너가 일시적으로 소멸되는 문제를 방지하기 위해 **로딩 씬 중첩 로드(Additive Load)** 방식을 채택했습니다.
    * `Resources.UnloadUnusedAssets()`와 `System.GC.Collect()`를 호출하여 씬 전환 시점의 메모리 누수를 최소화합니다.
    * 플레이 씬 로드가 완료되면 활성 씬을 설정하고 로딩 씬을 제거하여 자연스러운 화면 연결을 보장합니다.

* **시퀀스 관리 (`LoadPlaySceneSequence`)**
    * 튜토리얼 씬 로드와 인트로 씬 언로드 등 특정 상황에 맞는 씬 전환 순서를 코루틴으로 제어합니다.

---

### 3. 상호작용 시스템: 물건 옮기기 (Object Carrying)
물리 기반의 상호작용을 통해 오브젝트를 들고 옮기거나 던지는 시스템을 구현했습니다.

* **운반 로직 (`Interact` & `Drop`)**
    * **들기**: 오브젝트의 물리 엔진(`isKinematic`) 및 충돌체(`Collider`)를 일시 제어하고, 플레이어의 손(CarryParent) 위치에 부속시켜 위치와 회전을 동기화합니다.
    * **놓기/던지기**: 부모 관계를 해제하고 물리 효과를 재활성화합니다. 특히 `AddForce`를 이용해 플레이어의 정면 방향으로 물체를 날려 보내는 물리 반응을 구현했습니다.

* **상호작용 판단 (`TryInteract`)**
    * 레이캐스트(Raycast)를 활용하여 시야 중앙의 상호작용 대상을 탐색합니다.
    * **상태 기반 로직**: 플레이어가 이미 물건을 들고 있다면 '내려놓기'를 우선 실행하고, 비어 있다면 새로운 대상과 '상호작용'하도록 조건문을 설계했습니다.
 
---

## 👤  장현우

#### 본 프로젝트의 플레이어 시스템은 **FSM 기반 로직 설계 + Animator 표현 분리 + Ray 기반 상호작용 구조**를 중심으로 구현되었습니다.
---
## 1. Player Animation System
플레이어 애니메이션은 다음 구조로 구성되어 있습니다.
- **FSM (PlayerStateMachine)**  
  → 지금 무엇을 할 수 있는 상태인가를 판단
- **Animator**  
  → 현재 상태를 어떻게 보이게 할 것인가를 담당
- **Animation Event**  
  → 애니메이션 타이밍과 로직 상태를 정확히 동기화
---
### 1.1 Animator 전체 구조
Animator는 **2개의 주요 Layer**로 구성됩니다.
#### ▸ Base Layer
플레이어의 기본 동작을 담당합니다.
- Locomotion (Idle / Walk / Run)
- Jump / Fall / Land
- CrouchDown / CrouchLocomotion / StandUp
#### ▸ Attack Layer
플레이어 공격 전용 레이어입니다.
- Base Layer와 독립적으로 동작
- Any State → Player_Attack 구조
---
### 1.2 Locomotion 구조 (Base Layer)
**Blend Tree 구성**
Locomotion
└─ Blend Tree
├─ Player_Idle
├─ Walk2D
│ ├─ Forward
│ ├─ Backward
│ ├─ Left
│ └─ Right
└─ Run2D
├─ Forward
├─ Backward
├─ Left
└─ Right
markdown
코드 복사
**사용 파라미터**
- `Speed` (float) : 이동 강도
- `MoveX` (float) : 좌 / 우 입력
- `MoveY` (float) : 전 / 후 입력
> FSM(PlayerLocomotionState)에서 값만 세팅하며  
> Animator는 계산 없이 애니메이션 표현만 담당합니다.
---
### 1.3 Jump / Fall / Land 흐름
Locomotion
├─ Jump (Trigger)
│ ↓
├─ Fall (IsFalling = true)
│ ↓
└─ Land (Trigger)
↓
Locomotion
markdown
코드 복사
**사용 파라미터**
- `Jump` (Trigger)
- `IsFalling` (Bool)
- `Land` (Trigger)
**특징**
- 점프 / 낙하 판단은 FSM에서만 수행
- 공중 상태에서는 이동 및 앉기 입력 자동 차단
---
### 1.4 Crouch (앉기) 애니메이션
Locomotion
├─ CrouchDown
│ ↓
├─ CrouchLocomotion (Blend Tree)
│ ↓
└─ StandUp
↓
Locomotion
yaml
코드 복사
**CrouchLocomotion 구성**
- Player_CrouchIdle
- Player_CrouchingForward / Backward / Left / Right
**사용 파라미터**
- `CrouchDown` (Trigger)
- `StandUp` (Trigger)
- `IsCrouching` (Bool)
- `MoveX`, `MoveY`
---
### 1.5 Animation Event 연동
애니메이션 중간 프레임에서  
FSM 상태를 정확하게 전환하기 위해 Animation Event를 사용합니다.
| Event 함수 | 역할 |
|-----------|------|
| AE_BeginCrouchTransition | 앉기 / 서기 전환 시작 |
| AE_EndCrouchTransition | 전환 종료 |
| AE_EndCrouchDown | 앉기 상태 확정 |
| AE_EndStandUp | 서기 상태 확정 |
- 전환 중 이동 / 점프 / 공격 입력 차단
- FSM과 Animator 상태 불일치 방지
---
### 1.6 Animator Parameter 관리
- `PlayerAnimationData.cs`에서 Animator 파라미터 Hash 관리
- 매직 스트링(string 직접 사용) 방지
- 성능 및 유지보수 안정성 확보
---
## 2. Player Interaction System
#### 플레이어 상호작용은 **중앙 Ray(SphereCast) 기반 감지 + 입력(E 키) 기반 실행** 구조입니다.
---
### 2.1 핵심 구성 요소
- **PlayerInteractor** : 상호작용 감지 및 실행
- **IInteractable** : 상호작용 오브젝트 인터페이스
- **InteractableOutliner** : 시각적 강조(아웃라인)
- **EventBus** : UI(Hover / 안내 텍스트) 연동
---
### 2.2 동작 흐름
매 프레임
→ 카메라 중앙 SphereCast
→ IInteractable 탐색
→ Outliner ON / OFF
→ Hover 이벤트 발행
E 키 입력 시
→ 현재 대상이 있으면 Interact() 호출
yaml
코드 복사
- 감지는 항상 수행
- 실행은 입력이 있을 때만 수행
---
### 2.3 Carry(들기) 시스템
- `ICarryable` 인터페이스 기반
- 플레이어는 한 번에 하나의 물체만 소지 가능
- `IsCarrying == true` 상태에서 Interact 시 Drop 처리
---
## 3. Interactable Outliner System
상호작용 가능한 오브젝트에  
**아웃라인(Outline) 시각 효과**를 제공하는 시스템입니다.
### 동작 방식
Ray 감지
→ SetHighlight(true)
→ Outline 표시
Ray 해제
→ SetHighlight(false)
→ 알파 0 색상으로 완전 숨김
yaml
코드 복사
- MaterialPropertyBlock 사용
- 머티리얼 인스턴스 생성 없이 Renderer 단위 제어
- 얇은 메쉬 잔상 문제 방지
---
## 4. 구현 기능 (장현우)
### ⚔️ Combat & Physics System
**Ragdoll 피격 처리**
- Animator → Ragdoll 전환 구조
- 피격 방향(Vector3)과 힘(float)을 기반으로 AddForce 적용
- 단순 정지가 아닌, 타격 방향으로 날아가며 쓰러지는 물리 반응 구현
