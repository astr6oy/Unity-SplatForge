# SplatForge 사용 가이드

Unity 에디터에서 3D Gaussian Splatting 오브젝트를 배치하고 관리하는 방법을 설명합니다.

---

## 목차

1. [초기 설정](#1-초기-설정)
2. [Control Panel 열기](#2-control-panel-열기)
3. [서버 연결하기](#3-서버-연결하기)
4. [Scene Composition (메인 기능)](#4-scene-composition-메인-기능)
5. [3DGS 오브젝트 준비하기](#5-3dgs-오브젝트-준비하기)
6. [테스트 기능](#6-테스트-기능)
7. [씬 설정 저장/불러오기](#7-씬-설정-저장불러오기)

---

## 1. 초기 설정

### 1.1 설정 파일 생성

처음 사용 시 설정 파일이 자동으로 생성됩니다. 수동으로 생성하려면:

1. **Edit > Project Settings** 메뉴 클릭
2. 왼쪽 목록에서 **SplatForge** 선택
3. 설정이 자동으로 생성됨

### 1.2 기본 설정 확인

| 설정 항목 | 설명 | 권장값 |
|----------|------|--------|
| Use Mock Server | 테스트용 가상 서버 사용 | ✅ 체크 (개발 중) |
| Server Endpoint | 실제 서버 주소 | Python 서버 구축 후 입력 |
| Auto Connect On Start | 플레이 시 자동 연결 | 필요에 따라 |

> **참고**: Mock 서버는 실제 3DGS 모델을 생성하지 않습니다. 내장된 레이아웃 데이터로 UI 테스트가 가능합니다.

---

## 2. Control Panel 열기

SplatForge의 모든 기능은 Control Panel에서 사용합니다.

**메뉴 경로**: `Tools > SplatForge > Control Panel`

Control Panel 구성:
- **Server Connection**: 서버 연결 상태 및 연결/해제
- **Scene Composition**: 공간 단위 씬 구성 (메인 기능)
- **Test: Individual Operations**: 개별 오브젝트 생성/레이아웃 (테스트용, 접힌 상태)

---

## 3. 서버 연결하기

### 3.1 연결 방법

1. Control Panel 상단의 **Server Connection** 섹션 확인
2. **Connect** 버튼 클릭
3. 상태 표시가 **Connected** (녹색)로 변경되면 성공

### 3.2 연결 상태 확인

| 상태 | 색상 | 의미 |
|------|------|------|
| Connected | 🟢 녹색 | 서버 연결됨, 사용 가능 |
| Disconnected | 🔴 빨간색 | 연결 안됨 |
| Connecting... | 🟡 노란색 | 연결 시도 중 |

### 3.3 설정 변경

서버 설정을 변경하려면:
1. Control Panel 헤더의 ⚙️ 버튼 클릭
2. 또는 `Edit > Project Settings > SplatForge`

---

## 4. Scene Composition (메인 기능)

프롬프트와 바닥 구조를 기반으로 전체 공간을 한 번에 구성합니다.

### 4.1 Floor Bounds 설정

씬 구성 전에 배치 영역을 지정해야 합니다.

**방법 1: 자동 감지**
1. Scene Composition 섹션의 **Auto-detect** 버튼 클릭
2. Ground 레이어의 오브젝트를 자동 감지하여 영역 설정
3. 감지된 Min/Max 좌표와 면적(m²) 확인

**방법 2: 기본값 사용**
- Auto-detect 없이 진행하면 기본 10x10m 영역 사용

**영역 초기화**
- **Reset** 버튼으로 감지된 영역 초기화

> **팁**: Ground 레이어에 바닥 오브젝트가 있어야 자동 감지가 정확합니다.

### 4.2 프롬프트 입력

두 가지 방법으로 씬 설명을 입력할 수 있습니다.

**방법 1: 프리셋 사용**
1. **Preset** 드롭다운에서 선택:
   - None (직접 입력)
   - Cozy Bedroom
   - Modern Office
   - Living Room
2. 선택 시 프롬프트가 자동 입력됨

**방법 2: 직접 입력**
1. Prompt 텍스트 영역에 원하는 씬 설명 입력
2. 예: "A cozy room with a bed, desk, and lamp"

### 4.3 옵션 설정

| 옵션 | 설명 | 기본값 |
|------|------|--------|
| Max Objects | 최대 오브젝트 수 | 10 |
| Include Decorations | 장식품 포함 여부 | ✅ 체크 |

### 4.4 씬 구성 실행

1. 프롬프트 입력 후 **Compose Scene** 버튼 클릭
2. 서버가 레이아웃 계산 (Mock 서버: 1.5~4초)
3. **Preview** 영역에 결과 표시:
   - 총 오브젝트 수
   - 처리 시간
   - 배치 이유 설명 (reasoning)
   - 오브젝트 목록 (이름, 카테고리)

### 4.5 결과 적용

1. Preview에서 결과 확인
2. **Apply to Scene** 버튼 클릭
3. 씬에 HybridSceneObject들이 생성됨
4. 생성된 컨테이너 오브젝트가 자동 선택됨

**결과 확인:**
- Hierarchy에 `ComposedScene_HHMMSS` 컨테이너 생성
- 각 오브젝트에 HybridSceneObject 컴포넌트 자동 추가
- Registry에 자동 등록됨

### 4.6 Preview 초기화

- **Clear** 버튼으로 Preview 결과 삭제
- 새로운 프롬프트로 다시 시도 가능

---

## 5. 3DGS 오브젝트 준비하기

Scene Composition으로 생성된 오브젝트 외에, 기존 GaussianSplat에도 SplatForge 기능을 추가할 수 있습니다.

### 5.1 HybridSceneObject 추가

1. Hierarchy에서 GaussianSplatRenderer가 있는 오브젝트 선택
2. Inspector에서 **Add Component** 클릭
3. **SplatForge > HybridSceneObject** 선택

### 5.2 HybridSceneObject 설정

Inspector에서 설정할 수 있는 항목:

**Metadata 섹션**
| 항목 | 설명 |
|------|------|
| Object ID | 자동 생성된 고유 ID (변경 불가) |
| Name | 오브젝트 이름 |
| Category | 분류 (furniture, vegetation 등) |
| Tags | 검색용 태그 (+ 버튼으로 추가) |

**Bounds 섹션**
| 항목 | 설명 |
|------|------|
| Sync from Asset | GaussianSplat 에셋에서 크기 자동 가져오기 |
| Bounds Min/Max | 오브젝트 경계 (충돌 감지에 사용) |

**Proxy Collider 섹션**
| 항목 | 설명 |
|------|------|
| Auto Generate | 충돌체 자동 생성 |
| Collider Type | Box / Sphere / Capsule / None |

### 5.3 오브젝트 등록하기

**방법 1**: Inspector에서 등록
1. HybridSceneObject가 있는 오브젝트 선택
2. Inspector 하단의 **Register to Session** 버튼 클릭

**방법 2**: Control Panel에서 일괄 등록
1. Test 섹션 > **Scene Registry** 펼치기
2. **Refresh** 버튼 클릭 (씬의 모든 HybridSceneObject 자동 등록)

---

## 6. 테스트 기능

개별 오브젝트 생성 및 레이아웃 제안 기능은 **Test: Individual Operations** 섹션에 있습니다.

### 6.1 Test 섹션 열기

Control Panel 하단의 **Test: Individual Operations** 헤더 클릭하여 펼치기

### 6.2 개별 오브젝트 생성

> **참고**: Mock 서버에서는 실제 3DGS 모델이 생성되지 않습니다.

1. **Object Generation** 펼치기
2. Prompt 입력 (예: "wooden chair with armrests")
3. Quality 선택 (Low / Medium / High)
4. **Generate** 버튼 클릭

### 6.3 레이아웃 제안

기존에 배치된 HybridSceneObject들의 위치를 재배치합니다.

1. Hierarchy에서 HybridSceneObject들 선택 (다중 선택 가능)
2. **Layout Suggestions** 펼치기
3. 제약 조건 설정:
   | 옵션 | 설명 |
   |------|------|
   | Avoid Overlap | 오브젝트 간 겹침 방지 |
   | Ground Objects | 바닥에 배치 |
   | Min Spacing | 최소 간격 (미터) |
4. **Get Layout** 버튼 클릭
5. **Apply Suggestions**로 적용

### 6.4 Scene Registry

등록된 오브젝트 목록 확인 및 관리

| 버튼 | 기능 |
|------|------|
| Refresh | 씬의 모든 HybridSceneObject 다시 스캔 |
| Select All | 모든 등록된 오브젝트 선택 |
| 오브젝트 이름 클릭 | 해당 오브젝트 선택 및 포커스 |

---

## 7. 씬 설정 저장/불러오기

작업한 오브젝트 배치를 저장하고 나중에 불러올 수 있습니다.

### 7.1 Scene Configuration 창 열기

**메뉴 경로**: `Tools > SplatForge > Scene Configuration`

### 7.2 현재 설정 저장

1. **Config Name** 입력 (예: "LivingRoom_v1")
2. **Description**에 설명 추가 (선택사항)
3. **Save As...** 버튼 클릭
4. 저장 위치 선택 (`.json` 파일로 저장됨)

### 7.3 설정 불러오기

1. **Load...** 버튼 클릭
2. 저장된 `.json` 파일 선택
3. 미리보기에서 내용 확인
4. **Apply to Scene** 버튼으로 적용

> **참고**: 불러오기는 동일한 Object ID를 가진 오브젝트에만 적용됩니다.

---

## 자주 묻는 질문

### Q: 서버에 연결되지 않아요
- Project Settings에서 **Use Mock Server**가 체크되어 있는지 확인
- 실제 서버 사용 시 서버가 실행 중인지 확인

### Q: Floor Auto-detect가 작동하지 않아요
- Ground 레이어에 바닥 오브젝트가 있는지 확인
- 또는 Default 레이어의 평평한 오브젝트가 있는지 확인

### Q: Scene Composition 결과가 항상 같아요
- Mock 서버는 프롬프트 키워드(bedroom, office, living)에 따라 고정된 레이아웃 반환
- 실제 서버 연결 시 다양한 결과 생성 가능

### Q: Apply to Scene 후 오브젝트가 보이지 않아요
- Mock 모드에서는 GaussianSplatAsset이 없어 렌더링되지 않음
- HybridSceneObject 컴포넌트와 메타데이터는 정상 생성됨
- Scene View의 Gizmo로 경계 확인 가능 (오브젝트 선택 시)

### Q: HybridSceneObject를 추가했는데 경계가 보이지 않아요
- Inspector에서 **Sync from Asset** 버튼 클릭
- GaussianSplatRenderer에 에셋이 할당되어 있는지 확인

---

## 단축키 / 메뉴 요약

| 기능 | 메뉴 경로 |
|------|----------|
| Control Panel | Tools > SplatForge > Control Panel |
| Scene Configuration | Tools > SplatForge > Scene Configuration |
| Project Settings | Edit > Project Settings > SplatForge |

---

## 워크플로우 요약

### 기본 워크플로우 (Scene Composition)

```
1. Control Panel 열기
2. Connect 클릭
3. (선택) Auto-detect로 Floor 감지
4. Preset 선택 또는 Prompt 입력
5. Compose Scene 클릭
6. Preview 확인
7. Apply to Scene 클릭
8. 완료!
```

### 고급 워크플로우 (수동 배치 + 레이아웃 조정)

```
1. 기존 GaussianSplat 오브젝트에 HybridSceneObject 추가
2. Metadata 설정 (카테고리, 태그)
3. Registry에 등록 (Refresh)
4. 오브젝트들 선택
5. Layout Suggestions로 자동 배치
6. Apply Suggestions
7. 필요시 수동 조정
```

---

## 다음 단계

- **Python 서버 연동**: `PythonServer/` 폴더의 서버 구현 후 실제 AI 레이아웃 생성
- **커스텀 프리셋**: `Create > SplatForge > Metadata Preset`으로 새 프리셋 생성
- **커스텀 레이아웃**: `Samples~/MockLayouts/`에 JSON 추가로 Mock 레이아웃 확장
