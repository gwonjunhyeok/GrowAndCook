# Unity 2D 탑다운 프로젝트 요약 문서 (현재 구현분 기준)

## 1. 프로젝트 개요
- Unity 기반 2D 탑다운 게임.
- 현재 구현 범위:
  - 플레이어 이동
  - 마우스 클릭 상호작용(전역 쿨타임)
  - 바위/광석 파괴(내구도)
  - 피드백(흔들림)
  - 인벤토리 슬롯 UI/드래그 이동(메인/서브)
  - 장비 슬롯 UI/드래그 이동
  - 가방 장착에 따른 메인 인벤토리 슬롯 확장/축소
  - 패널 드래그
  - 키 바인딩 데이터 분리 및 이동 입력 연동
  - 세이브 슬롯 기반 저장 데이터 구조 추가
  - UI 열림 상태에서 게임 월드 정지 + UI 조작 유지
  - ESC 설정창 오픈 시 나무판자 묶음 낙하/바운스 연출
- 향후 확장 예정:
  - 설정창 기반 키 변경
  - 키 바인딩 JSON 저장/로드
  - 숫자키 핫바 선택 + 마우스 클릭 기반 아이템 사용 시스템
  - 도구/소모품/설치형 아이템별 사용 타입 분리
  - 아이템 사용과 기본 상호작용의 우선순위 체계 정리
  - 특정 상황 아이템 지급
  - 도구(곡괭이/도끼) 조건
  - 드랍 테이블
  - 다양한 상호작용 오브젝트(NPC/문/상자/나무/작물 등)
  - 캐릭터 커스터마이징 저장 데이터 확장

---

## 2. 화면/조작(Flow) 흐름

### 2.1 플레이어 이동
1. `GameManager`가 현재 `KeyBindingSettings`를 보관
2. `PlayerMove.Update()`에서 현재 이동 키(`moveUp/moveDown/moveLeft/moveRight`)를 읽음
3. `Input.GetKey(...)`로 이동 방향 벡터 계산
4. 대각선 이동 시 정규화
5. `Rigidbody2D.MovePosition()`으로 이동
6. 좌/우 입력에 따라 `SpriteRenderer.flipX` 반전
7. UI가 열려 `GameManager.IsGameplayPaused == true`이면 이동 입력은 0으로 처리

### 2.2 월드 상호작용(마우스 클릭)
1. 마우스 좌클릭 입력
2. UI 열림 상태면 월드 상호작용 입력 차단
3. 전역 상호작용 쿨타임 체크(`Interact_Cooltime`)
4. `ScreenToWorldPoint`로 클릭 위치를 월드 좌표로 변환
5. `OverlapPoint` 또는 `OverlapCircle`로 상호작용 대상 콜라이더 탐색(레이어 마스크)
6. 대상의 `IInteractable` 획득
7. 플레이어-대상 거리 체크(`InteractRange`)
8. 조건 통과 시 전역 쿨타임 갱신 후 `target.Interact(playerTransform)` 호출

### 2.3 인벤토리/슬롯 드래그(현재 구현)
- 메인 슬롯(main)은 기본 27칸 구조.
- 메인 슬롯(main) / 서브 슬롯(sub) 두 패널 구조.
- 가방 장착 시 메인 슬롯이 9칸 추가되어 총 36칸까지 확장 가능.
- 슬롯을 일정 시간(0.3초) 누르면 드래그 시작(아이템이 있는 경우).
- UI 일시정지 상태에서도 드래그가 가능하도록 홀드 시간은 `Time.unscaledDeltaTime` 기준으로 누적.
- 드롭 시나리오:
  - 다른 슬롯 위에 드롭: 합치기(같은 아이템이면 스택), 아니면 스왑/이동
  - 장비 슬롯 위에 드롭: 장착 시도
  - 인벤 외부 드롭: 아이템 버리기 또는 원위치 복귀
  - 인벤 내부지만 슬롯이 아닌 곳: 원위치 복귀

### 2.4 장비 슬롯 드래그(현재 구현)
- 장비 슬롯(Head / Body / Leg / Feet / Bag) 구조.
- 장비 슬롯도 일정 시간(0.3초) 누르면 드래그 시작(아이템이 있는 경우).
- UI 일시정지 상태에서도 드래그가 가능하도록 홀드 시간은 `Time.unscaledDeltaTime` 기준으로 누적.
- 드롭 시나리오:
  - 빈 인벤 슬롯 위에 드롭: 인벤으로 이동
  - 다른 장비 슬롯 위에 드롭: 타입이 맞으면 이동/스왑
  - 이미 장착된 장비 슬롯 위에 다른 장비 아이템 드롭: 기존 장착 아이템과 스왑
  - Bag 슬롯의 가방을 인벤으로 옮길 때: 메인 인벤에 빈 슬롯이 9칸 이상이면 9칸 축소 후 이동, 부족하면 드래그 취소
  - 유효하지 않은 위치 드롭: 원래 장비 슬롯으로 복귀

### 2.5 키 바인딩 흐름(현재 구현)
1. `GameManager`가 `CurrentKeyBindings`를 보관
2. `KeyBindingSettings`는 이동/UI/마우스 키 데이터를 순수 데이터로 관리
3. 현재는 `PlayerMove`, `UIControlManager`가 이 키 바인딩을 사용
4. 설정창 변경 및 JSON 저장/로드는 아직 미구현

### 2.6 UI/게임 정지 흐름(현재 구현)
1. `UIControlManager`가 인벤토리/설정창 열기 입력을 처리
2. UI 하나라도 열리면 `GameManager.SetGameplayPaused(true)` 호출
3. `Time.timeScale = 0`으로 월드 진행 정지
4. `PlayerMove`, `CameraControl`, `PlayerInteractable`은 월드 입력 차단
5. UI 자체는 계속 활성 상태로 유지되어 드래그/클릭/버튼 조작 가능
6. 모든 UI가 닫히면 `GameManager.SetGameplayPaused(false)`로 게임 재개

### 2.6.1 ESC 설정창 오픈 연출(현재 구현)
1. 플레이어가 `settings` 키(`Esc`) 입력
2. `UIControlManager`가 `settingsPanel` 활성화
3. `GameManager.SetGameplayPaused(true)`로 월드 정지
4. `EscPanelAnimation.OnEnable()`이 즉시 실행
5. `Pivot`을 `StartAnchor` 위치로 리셋
6. `GraphicRaycaster`를 잠시 꺼서 연출 중 버튼 입력 차단
7. `Pivot`이 `StartAnchor -> EndAnchor 아래 오버슈트 위치 -> EndAnchor` 순서로 이동
8. 이동은 `Time.unscaledDeltaTime` 기준으로 처리되어 `timeScale = 0` 상태에서도 정상 재생
9. 연출 종료 후 버튼 입력을 다시 허용
10. `BorderViewport(RectMask2D)`가 메뉴 아래쪽부터만 판자 묶음이 보이도록 가림 처리

### 2.7 세이브 슬롯 흐름(현재 구현)
1. 외부 진입 코드에서 `GameSession.StartNewGame(slotIndex, sceneName, startPosition)` 또는 `GameSession.LoadGame(slotIndex)` 호출
2. `SaveFileService`가 `SaveSlot_1.json` ~ `SaveSlot_3.json` 파일을 생성/로드
3. `PlayerDataController`가 현재 세션 데이터를 플레이어 수치, 좌표, 인벤토리에 반영
4. 저장 시 `PlayerDataController.SaveCurrentSession()`이 현재 수치/위치/인벤토리 상태를 `GameSession.CurrentSaveData`에 반영 후 파일 저장

### 2.8 데이터 관리
- 게임 전역 데이터 관리 진입점: `GameManager`
- 런타임 세션 관리: `GameSession`
- 세이브 파일 입출력: `SaveFileService`
- 키 바인딩 데이터 관리: `KeyBindingSettings`
- 인벤토리/장비 데이터 관리: `Inven_System`
- `DataManage`는 현재 비어 있는 상태

---

## 3. 주요 데이터 모델(현재 기준)

### 3.1 ScriptableObject
- `ItemData`
  - `id`, `itemName`, `maxStack`, `icon`, `itemType`, `equipSlotType`
  - 인벤토리 UI, 스택 제한, 장착 가능 여부 판정에 사용

- `RockDataSO`
  - `displayName`, `interactRange`, `maxHp`, `useDrops`
  - 바위/광석의 기본 스펙 데이터
  - 파일명은 `RockDataOS.cs`지만 클래스명은 `RockDataSO`

### 3.2 Runtime 데이터
- `ItemStack`
  - `itemData + count`
  - `IsFull`, `IsEmpty` 등으로 스택 상태 판단
  - `Add`, `Remove`를 통해 수량 변경

- `DragData`
  - `fromMain`, `originIndex`, `draggedItem`, `isSplit`, `fromEquipment`, `originEquipmentSlot`
  - 인벤 슬롯/장비 슬롯 어느 쪽에서 드래그가 시작됐는지와 드래그 중인 아이템 정보를 저장

- `KeyBindingSettings`
  - `moveUp`, `moveDown`, `moveLeft`, `moveRight`
  - `inventory`, `settings`
  - `leftClick`, `rightClick`
  - 현재 키 바인딩 상태를 담는 순수 데이터 클래스

- `SaveGameData`
  - 세이브 슬롯 1개의 전체 저장 루트 데이터
  - `meta`, `player`, `inventory`, `wear`를 보관

- `PlayerSaveData`
  - `hp`, `stamina`, `level`, `experience`, `gold`
  - `sceneName`, `positionX`, `positionY`
  - 플레이어 수치와 시작 위치를 저장

- `InventorySaveData`
  - `mainSlots`, `subSlots`, `equipmentSlots`
  - 점유된 인벤토리/장착 슬롯만 저장

- `CharacterWearSaveData`
  - 아직 미구현 상태의 착용 외형 저장 placeholder 데이터
  - 현재는 기본값만 유지

---

## 4. 기능 목록 (현재까지 구현된 핵심 기능)

### 4.1 이동
- 2D 탑다운 이동(`Rigidbody2D` 기반)
- 좌/우 방향 전환(`flipX`)
- `GameManager.CurrentKeyBindings` 기반 이동 키 적용
- UI 열림 상태에서 월드 이동 입력 차단

### 4.2 상호작용
- 마우스 클릭 기반 대상 선택(`OverlapPoint`/`OverlapCircle`)
- 상호작용 가능 레이어 마스크 필터
- 거리 제한(`InteractRange`)
- 전역 상호작용 쿨타임(`Interact_Cooltime`)
- UI 열림 상태에서 월드 상호작용 입력 차단

### 4.3 바위/광석(테스트 단계)
- `RockDataSO` 기반 내구도(HP) 적용
- 상호작용 시 흔들림(`Shake`) + HP 감소
- HP 0 이하 시 `Destroy`

### 4.4 인벤토리(슬롯/드래그)
- 메인 인벤 기본 27칸(9칸 x 3줄) 구성
- 메인/서브 슬롯 분리 구성
- 아이템 추가 시: 기존 스택 우선 채우기 -> null 슬롯 재사용
- 슬롯 드래그(홀드 0.3초)
- Ctrl 누른 상태에서 절반 이동(분할 드래그)
- Bag 장착 시 메인 인벤 9칸 추가
- Bag 해제 시 메인 인벤 빈 슬롯 9칸 이상 필요, 충족 시 9칸 제거 / 미충족 시 취소
- 드롭 처리:
  - 같은 아이템이면 합치기(최대 스택 고려)
  - 아니면 스왑 또는 빈 슬롯 이동
  - 인벤 밖 드롭 시 버리기 또는 복귀
  - 인벤 안이지만 슬롯이 아닐 경우 복귀
- UI 정지 상태에서도 인벤토리 드래그 동작 유지
- 메인/서브/장비 슬롯 상태를 저장 데이터로 내보내기/복원 가능

### 4.5 장비 슬롯(드래그/스왑)
- 장비 슬롯 타입 검사
- 인벤 -> 장비 슬롯 장착
- 장비 슬롯 -> 인벤 이동
- 장비 슬롯 -> 장비 슬롯 이동/스왑
- 인벤 장비 아이템 -> 이미 장착된 장비 슬롯 드롭 시 스왑 처리
- Bag 슬롯 장비 해제 시 메인 인벤 여유 9칸 검사
- 장비 슬롯 출발 드래그 실패 시 원래 장비 슬롯으로 복귀
- 저장 데이터 로드 시 Bag 장비를 먼저 복원해 메인 슬롯 수를 맞춤

### 4.6 키 바인딩
- `KeyBindingSettings` 데이터 클래스 추가
- `GameManager`가 현재 키 바인딩을 보관
- `PlayerMove`와 `UIControlManager`가 현재 키 바인딩 사용
- 아직 설정창 변경 / JSON 저장 / 로드 미구현

### 4.7 세이브 슬롯 데이터 구조
- `SaveSlot_1.json` ~ `SaveSlot_3.json` 저장 구조
- `SaveFileService`가 슬롯별 생성 / 로드 / 저장 담당
- `GameSession`이 현재 활성 세션 데이터를 런타임에 보관
- `PlayerDataController`가 플레이어 수치 / 위치 / 인벤토리를 세션 데이터와 동기화

### 4.8 UI 기반 일시정지
- 인벤토리 또는 설정창이 열리면 게임 월드 정지
- `Time.timeScale = 0` 적용
- UI 클릭 / 버튼 / 드래그 / 스크롤은 계속 동작
- 모든 UI가 닫히면 게임 재개
- ESC 설정창은 오픈 시 판자 묶음이 즉시 낙하하고 한 번 바운스한 뒤 고정
- ESC 설정창 낙하 연출은 `unscaledDeltaTime` 기반으로 동작
- ESC 설정창 연출 중에는 버튼 입력을 잠시 막아 오동작을 방지
- `BorderViewport(RectMask2D)`로 메뉴 아래에서만 판자 묶음이 드러나도록 처리

---

## 5. 파일별 역할 요약 (현재 스크립트 기준)

### Player
- **PlayerMove.cs**
  - 현재 키 바인딩 설정을 읽어 플레이어 이동을 처리.
  - `Rigidbody2D.MovePosition`으로 이동하고, 좌/우 입력에 따라 `flipX` 반전.
  - UI 정지 상태에서는 월드 이동 입력을 차단.

### Interaction
- **PlayerInteractable.cs**
  - 마우스 좌클릭 기반 상호작용 진입점.
  - 전역 쿨타임, 레이어 필터, 거리 체크 후 `IInteractable.Interact` 호출.
  - UI 정지 상태에서는 월드 상호작용 입력을 차단.

- **IInteractable.cs**
  - 상호작용 가능한 오브젝트 인터페이스.
  - `InteractRange`, `Interact(Transform)` 규약 제공.

- **RockInteractable.cs**
  - 바위 상호작용 처리 구현체.
  - 상호작용 시 흔들림 실행 후 HP 감소, 0이면 오브젝트 파괴.

- **RockDataOS.cs**
  - `RockDataSO`를 선언하는 파일.
  - 바위/광석의 이름, 상호작용 거리, 내구도, 드랍 사용 여부를 정의.

- **Shake.cs**
  - 타격 피드백용 흔들림 컴포넌트.
  - 짧은 시간 동안 `localPosition`을 흔들고 원위치 복귀.

### Inventory
- **ItemData.cs**
  - 인벤토리 아이템 정의 `ScriptableObject`.
  - 스택 제한, 아이콘, 장비 타입 판정 등에 사용.

- **ItemStack.cs**
  - 런타임 스택 데이터.
  - `Add`, `Remove`, `IsFull`, `IsEmpty` 제공.

- **Inven_System.cs**
  - 인벤토리 전체 제어.
  - 메인/서브 슬롯, 아이템 추가, 드래그 시작/드롭 처리, 버리기/복귀를 담당.
  - Bag 장착 시 메인 슬롯 확장/축소 처리 포함.
  - 핫바 선택 및 테스트 입력(`P`, `H`, `O`)도 포함.
  - 인벤토리/장착 슬롯 상태를 세이브 데이터로 저장/복원하는 API 제공.

- **Inven_Slot.cs**
  - 인벤토리 슬롯 UI 컴포넌트.
  - 홀드 입력을 감지하고 실제 이동은 `Inven_System`으로 위임.
  - `Time.unscaledDeltaTime`으로 UI 정지 상태에서도 드래그 가능.

- **EquipmentSlotUI.cs**
  - 장비 슬롯 UI 컴포넌트.
  - 슬롯 타입 검사, 장착/이동 요청, Bag 해제 검증을 담당.
  - `Time.unscaledDeltaTime`으로 UI 정지 상태에서도 드래그 가능.

- **DragSlot.cs**
  - 드래그 중인 아이콘 표시 및 드래그 상태 관리.

- **DragData.cs**
  - 드래그 작업 상태 데이터 컨테이너.

- **TrashSlot.cs**
  - 드래그된 아이템 버리기 관련 UI 처리용 슬롯.

- **MovableHeaderUI.cs**
  - UI 패널 헤더 드래그 이동 처리.
  - `ExitPanel()`에서 지정 패널을 비활성화.

- **Singleton.cs**
  - 인벤토리/드래그 관련 싱글톤 기반 클래스.

### UI Utility
- **ScrollDragBlocker.cs**
  - 스크롤 영역과 드래그 입력 충돌을 막기 위한 보조 컴포넌트.

- **WheelOnlyScrollRect.cs**
  - 휠 입력 중심 스크롤 처리용 보조 컴포넌트.

### UI
- **UIControlManager.cs**
  - 인벤토리와 설정 UI 토글 입력을 처리.
  - UI 상태에 따라 게임 월드 정지/재개를 갱신.

- **ESCPanelAnimation.cs**
  - ESC 설정창이 열릴 때 판자 묶음 낙하 연출을 처리.
  - `StartAnchor`, `EndAnchor`, `Pivot` 기준으로 위치를 보간.
  - `Time.unscaledDeltaTime`을 사용해 게임 정지 상태에서도 연출이 재생되도록 처리.
  - 연출 중 `GraphicRaycaster`를 비활성화해 버튼 입력을 잠시 차단.

### Camera
- **CameraControl.cs**
  - 카메라 대상 추적 및 줌 처리.
  - `Space` 입력으로 추적 on/off 전환.
  - UI 정지 상태에서는 카메라 입력 차단.

### Data
- **GameManager.cs**
  - 게임 전역 데이터 접근점.
  - 현재 키 바인딩과 게임플레이 정지 상태를 보관하는 싱글톤 역할 수행.

- **KeyBindingSettings.cs**
  - 게임에서 사용하는 키 바인딩 데이터 클래스.
  - 이동, UI, 마우스 버튼 키를 보관.

- **SaveGameData.cs**
  - 세이브 슬롯 1개의 전체 저장 루트 데이터.
  - 메타, 플레이어, 인벤토리, 착용 외형 placeholder를 보관.

- **SaveFileService.cs**
  - 슬롯별 세이브 파일 생성, 로드, 저장 담당.

- **GameSession.cs**
  - 현재 활성 세이브 슬롯과 런타임 세이브 데이터를 보관.

- **PlayerDataController.cs**
  - 현재 세션 데이터를 플레이어 수치, 위치, 인벤토리에 반영하고 저장 시 다시 세션에 수집.

- **DataManage.cs**
  - 현재 내용이 비어 있는 플레이스홀더 스크립트.

---

## 6. 현재 동작 시나리오(핵심)

### 6.1 바위 부수기
1. 플레이어가 바위를 클릭
2. `PlayerInteractable`이 대상 탐색/거리 체크/쿨타임 체크
3. `RockInteractable.Interact` 호출
4. `Shake` 실행 + HP 감소
5. HP <= 0이면 `Destroy`

### 6.2 인벤 드래그 이동
1. 슬롯에서 마우스 좌클릭 홀드(0.3초)
2. `DragSlot.StartDrag` 실행(분할이면 절반, 아니면 전체)
3. 마우스 버튼 업
4. 인벤 슬롯 위면 `OnSlotDrop`
5. 장비 슬롯 위면 `OnEquipmentSlotDrop`
6. 유효하지 않은 위치면 `Return` 처리
7. UI 정지 상태에서도 홀드 시간은 계속 누적되어 드래그 가능

### 6.3 장비 드래그 이동
1. 장비 슬롯에서 마우스 좌클릭 홀드(0.3초)
2. `DragSlot.StartDrag(EquipmentSlotUI, ItemStack)` 실행
3. 원래 장비 슬롯은 비워짐
4. 마우스 버튼 업
5. 빈 인벤 슬롯이면 인벤으로 이동
6. Bag 슬롯의 가방을 인벤으로 옮길 때는 메인 인벤 빈 슬롯이 9칸 이상인지 먼저 검사
7. 조건 충족 시 메인 인벤을 압축한 뒤 9칸 제거하고 이동
8. 장비 슬롯이면 타입 검사 후 이동 또는 스왑
9. 유효하지 않거나 빈 슬롯이 부족한 위치면 원래 장비 슬롯으로 복귀
10. UI 정지 상태에서도 홀드 시간은 계속 누적되어 드래그 가능

### 6.4 UI 열림과 게임 정지
1. 플레이어가 인벤토리 또는 설정 UI를 엶
2. `UIControlManager`가 `GameManager.SetGameplayPaused(true)` 호출
3. `Time.timeScale = 0` 적용
4. 플레이어 이동, 카메라 조작, 월드 상호작용 입력 차단
5. UI 버튼, 드래그, 스크롤은 그대로 동작
6. 모든 UI를 닫으면 `GameManager.SetGameplayPaused(false)`로 게임 재개

### 6.4.1 ESC 설정창 낙하 연출
1. `Esc` 입력으로 설정창 활성화
2. `ESC Panel`의 `OnEnable`에서 `EscPanelAnimation` 실행
3. `Pivot`을 `StartAnchor`로 이동
4. `EndAnchor`보다 조금 더 아래까지 빠르게 낙하
5. 다시 `EndAnchor`로 올라오며 안착
6. 연출 중에는 설정창 버튼 입력 차단
7. 연출 종료 후 버튼 입력 허용

### 6.5 세이브 슬롯 시작/로드
1. 외부 진입 코드에서 새 게임 또는 로드 게임 흐름 결정
2. `GameSession`이 현재 슬롯과 세이브 데이터를 활성 세션으로 설정
3. 게임 씬 시작 시 `PlayerDataController`가 세션 데이터를 읽음
4. 플레이어 수치, 위치, 인벤토리 상태를 런타임에 반영
5. 저장 시 현재 수치, 위치, 인벤토리를 다시 세션 데이터에 수집 후 파일 기록

---

## 7. 의존성/주의 사항(현재 코드 기준)
- `PlayerMove`, `UIControlManager`, `CameraControl`, `PlayerInteractable`은 `GameManager.Instance`가 씬에 존재해야 pause/키 바인딩 상태를 정상 참조할 수 있다.
- 현재 키 바인딩은 런타임 메모리에만 있고 JSON 저장/로드는 아직 없다.
- `PlayerInteractable`은 아직 `KeyBindingSettings.leftClick`을 사용하지 않고 `Input.GetMouseButtonDown(0)`을 직접 사용한다.
- `CameraControl`도 아직 `Space`, `Mouse ScrollWheel`을 직접 사용한다.
- `Inven_System`은 `Resources.LoadAll<ItemData>("")` 기반이라 `Resources` 배치 정책에 영향을 받는다.
- 인벤 슬롯과 장비 슬롯은 입력만 담당하고, 실제 이동/스왑/복귀 로직은 `Inven_System`에 집중되어 있어야 구조가 유지된다.
- Bag 슬롯 확장/축소는 `mainSlotParent`, `mainSlotPrefab`, 메인 슬롯 인덱스 재정렬에 의존한다.
- 인벤토리 세이브 로드 시 Bag 장비를 먼저 복원해야 추가 슬롯 수가 맞는다.
- `GameManager`는 현재 `DontDestroyOnLoad`가 없으므로 씬 전환 시 유지되지 않는다.
- `GameSession`은 런타임 메모리 보관용이며, 실제 파일 저장은 `SaveFileService`가 담당한다.
- `CharacterWearSaveData`는 아직 placeholder 상태이므로 실제 착용 외형 반영 로직은 없다.
- `DataManage`는 아직 역할이 정해지지 않은 상태라 `GameManager`와 책임이 겹치지 않게 정리 필요.
- ESC 설정창 낙하 연출은 `ESC Panel`이 기본 비활성 상태에서 시작해야 `OnEnable` 기반 재생이 자연스럽다.
- `BorderViewport`, `Pivot`, `StartAnchor`, `EndAnchor`는 같은 로컬 좌표 기준을 유지해야 위치 보간이 어긋나지 않는다.
- `BorderViewport`에는 `RectMask2D`가 필요하고, 자체 `Image`는 보이지 않으며 `RaycastTarget`을 끄는 편이 안전하다.
- ESC 설정창 연출은 현재 `StartAnchor -> 오버슈트 -> EndAnchor` 고정 구조이며, 낙하 강도와 속도는 `ESCPanelAnimation` 직렬화 필드로 튜닝한다.

---

## 8. 다음 작업(우선순위 제안)

### 8.1 키 바인딩 시스템 완성
1. 설정창에서 키 변경 UI 추가
2. 변경된 키를 `GameManager.CurrentKeyBindings`에 반영
3. JSON 저장/로드 추가
4. 기본값 복원 기능 추가
5. 중복 키 검사 정책 정의

### 8.2 세이브 슬롯 실제 진입 흐름 연결
1. 타이틀 UI에서 슬롯 선택 처리
2. `New Game` -> `GameSession.StartNewGame(...)` 연결
3. `Load Game` -> `GameSession.LoadGame(...)` 연결
4. 저장 버튼 / 체크포인트 / 종료 저장 시 `PlayerDataController.SaveCurrentSession()` 연결

### 8.3 상호작용 입력도 키 바인딩과 통합
1. `PlayerInteractable`이 `leftClick/rightClick`을 사용하도록 변경
2. 추후 상호작용 키 전환 시에도 같은 데이터 구조 재사용

### 8.4 아이템 사용 입력 구조 설계
1. 숫자키(`1~9`)로 현재 핫바 슬롯 선택
2. 선택된 슬롯 아이템을 기준으로 마우스 클릭 입력 처리
3. 설계 방향 후보:
   - 좌클릭 기본 상호작용 / 우클릭 아이템 사용
   - 클릭 대상 우선순위 기반 처리
   - 아이템 타입 + 대상 타입 조합 기반 처리
4. 현재 추천 방향:
   - `UI > 문/NPC/강한 상호작용 대상 > 아이템-대상 조합 사용 > 자기 자신 사용(음식 섭취 등) > 무효 처리`
5. 도구 계열은 대상 타입에 따라 고유 상호작용 분기
   - 곡괭이 -> 돌/광석
   - 도끼 -> 나무
   - 괭이 -> 밭
   - 낫 -> 잡초/수확 대상
6. 음식 같은 사용 아이템은 빈 공간 클릭 시 자기 자신 사용, 문/NPC 클릭 시 대상 상호작용 우선 처리
7. 구현 시 입력 처리, 사용 판정, 대상 타입 판정을 분리해 구조를 유지하는 방향 권장

### 8.5 특정 상황 아이템 지급
1. 바위 파괴 시 돌 아이템 지급
2. 나무 파괴 시 목재 지급
3. 상자 오픈 시 특정 아이템 지급
4. 현재는 `Inven_System.AddItem(item)` 방식으로 지급 가능
5. 추후에는 DropTable SO 또는 드랍 정의 데이터로 확장

### 8.6 캐릭터 커스터마이징 데이터 확장
1. `CharacterWearSaveData`에 장착 중인 외형/코스튬 ID 추가
2. 저장/로드와 실제 외형 적용 로직 연결
3. 장비 외형과 기본 외형 우선순위 규칙 정의

---

## 9. 현재 구현 기준 핵심 정리
- 인벤 슬롯과 장비 슬롯은 입력 전용
- 실제 이동 정책은 `Inven_System` 담당
- 아이템 정의는 `ItemData`
- 런타임 수량은 `ItemStack`
- 메인 인벤은 기본 27칸이며, Bag 장착 시 9칸 확장된다
- Bag 해제는 메인 인벤 빈 슬롯 9칸 확보 시에만 가능하다
- `KeyBindingSettings`는 순수 키 데이터 클래스다
- `GameManager`가 현재 키 바인딩과 게임 정지 상태를 보관한다
- UI가 열리면 게임 월드는 멈추고 UI 조작은 계속 가능하다
- ESC 설정창은 열리는 순간 판자 묶음 낙하 연출이 재생된다
- ESC 설정창 낙하 연출은 `RectMask2D + StartAnchor/EndAnchor + unscaledDeltaTime` 조합으로 구성된다
- `SaveGameData` / `SaveFileService` / `GameSession` / `PlayerDataController`로 세이브 슬롯 구조가 추가되었다
- 플레이어 수치, 위치, 인벤토리 상태를 슬롯 파일에 저장할 수 있는 기반이 마련되었다
- 다음 핵심 작업은 타이틀 UI와 세이브 슬롯 진입 흐름 연결, 설정창 기반 키 변경, JSON 저장/로드 확장이다
