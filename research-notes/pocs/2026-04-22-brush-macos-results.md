---
type: poc-results
status: in-progress
related:
  - "[[2026-04-22-brush-macos-single-pipeline]]"
created: 2026-04-21
---

# Brush macOS 단일 파이프라인 PoC 결과

## 목적

본 PoC의 성패는 **Unity-SplatForge 아키텍처 결정의 전제**입니다. Brush(Rust/WGPU)와 aras-p Gaussian Splatting(Unity) 조합이 맥북 단일 머신에서 학습→PLY export→Unity 렌더까지 성립하면 Python 서버 분리 아키텍처는 불필요해지고, 실패하면 Python 서버 경로로 되돌아갑니다. 이 문서는 실행 중 즉시 체크 가능한 결과 기록지입니다.

관련 계획서: [[2026-04-22-brush-macos-single-pipeline]]

---

## 1. 환경 사전점검

> Alfred가 2026-04-20 기준 alfred-bridge로 원격 점검한 결과. 실행 전 마스터가 재확인 필수.

- [x] **사용자 계정**: USER=`oz6oy`, HOME=`/Users/oz6oy`
- [x] **Rust 툴체인**: **설치 완료 (2026-04-20)**
  - 설치 명령: `curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh`
  - 설치 후 `source "$HOME/.cargo/env"` 적용
  - 측정값 — 설치된 rustc 버전: **1.95.0 stable** (profile=minimal, target=aarch64-apple-darwin)
- [x] **디스크 여유 공간**: 앱 정리 후 **68GB** 확보 (빌드·학습 여유 확보 완료)
  - 권장: 10GB+ (Brush 빌드 3~5GB + 학습 데이터셋 + PLY 산출물)
  - 빌드·학습 전 공간 확보 권장 (Downloads, 이전 빌드 캐시 정리 등)
  - 측정값 — 확보 후 여유 공간: **68** GB
- [x] **Xcode CLT**: 설치 확인됨 (cargo build 성공으로 간접 검증 — clang/linker 정상 작동)
  - 학위논문 작업 중인 맥북이므로 기설치 가능성 높음
  - 측정값 — CLT 경로: (build 성공으로 간접 확인, 추후 `xcode-select -p` 직접 값 기록 가능)
- [x] **Brush clone 위치**: **`/Users/oz6oy/repos/3DGS/brush`** (117MB 소스)
  - 후보 A: `~/3DGS/brush`
  - 후보 B: `~/workspace/brush` (새로 생성)
  - 최종 선택: **`/Users/oz6oy/repos/3DGS/brush`** (기존 3DGS 작업 경로에 통합)
  - clone 명령: `git clone --depth 1 https://github.com/ArthurBrussee/brush.git`
- [x] **Metal/WGPU 지원**: macOS 14+ 기본 제공 — 별도 설치 불필요
  - 측정값 — macOS 버전: (M1 Max 맥북, macOS 정상 작동 확인 — 세부 버전 추후 기록)

---

## 2. AC1-AC7 체크리스트

> 상세 기준은 [[2026-04-22-brush-macos-single-pipeline]] §2 참조. 각 항목 PASS/FAIL/N/A + 측정값.

### AC1 — Brush 빌드 성공

- [x] **PASS** (2026-04-20)  - [ ] FAIL  - [ ] N/A
- 빌드 명령: `cargo build --release --bin brush`
  - 참고: 초기 `--bin brush_app` 오타로 실패했고, `--bin brush`로 수정 후 재실행하여 성공
- 빌드 시간: **7분 6초** (`Finished release profile [optimized] target(s) in 7m 06s`)
- 결과 바이너리: `/Users/oz6oy/repos/3DGS/brush/target/release/brush` (155,090,160 바이트 = 약 **148MB**)
- `brush --help` 정상 출력 확인: "Brush - universal splats" 배너 + train 옵션(`--total-train-iters`, `--lr-mean` 등) 표시
- 에러 로그 (FAIL 시): N/A

### AC2 — 샘플 데이터셋 학습 시작

- [ ] PASS  - [ ] FAIL  - [ ] N/A
- 사용 데이터셋: _(빈칸)_ (예: nerf_synthetic/lego, COLMAP 출력 등)
- 이미지 수: _(빈칸)_
- 학습 시작 시각: _(빈칸)_

### AC3 — Metal/WGPU 백엔드 정상 작동

- [ ] PASS  - [ ] FAIL  - [ ] N/A
- GPU 사용 확인 (Activity Monitor / `powermetrics`): _(빈칸)_
- WGPU 백엔드 로그 문자열: _(빈칸)_ (예: `Metal`, `MoltenVK` 등)

### AC4 — 학습 수렴 / aras-p import 성공 (Forge task-001 반영)

- [x] **PASS (단서부 PASS)** (2026-04-22, Forge task-001 partial)  - [ ] FAIL  - [ ] N/A
- **aras-p 포맷 호환 실증**:
  - 원본 PLY: `Statue.ply` (88,052,691 B, 355,045 splats) — aras-p 요건 완전 충족 (binary_little_endian 1.0, f_dc_0~2, f_rest_0~44, opacity, scale_0~2, rot_0~3)
  - aras-p 산출 asset: `Statue.asset` (1,139 B) + 데이터 파일 4종 합계 **83,877,072 B**
    - `3DGS_pos.bytes`: 4,260,544 B
    - `3DGS_oth.bytes`: 5,680,720 B
    - `3DGS_col.bytes`: 5,767,168 B
    - `3DGS_shs.bytes`: 68,168,640 B
  - format: **VeryHigh (Float32 all)**, format_version: **20231020**
  - splat_count: **355,045**
- **단서**: Statue.asset은 2025-09-14 GUI 세션 산출물(기존 자산). Forge task-001의 `-batchmode` 실행에 의한 신규 생성이 아님. 다만 aras-p 파이프라인이 이 프로젝트/데이터에서 정상 작동한 증거로서는 충분.
- 총 iteration: _(빈칸 — Brush 학습 미진입)_
- final loss: _(빈칸)_
- 학습 시간: _(빈칸)_
- splat 개수: 355,045 (Statue 데이터 기준)

### AC5 — PLY export 성공

- [ ] PASS  - [ ] FAIL  - [ ] N/A
- 출력 경로: _(빈칸)_
- 파일 크기: _(빈칸)_ MB
- export 명령: _(빈칸)_

### AC6 — Unity aras-p 로드 성공

- [ ] PASS  - [ ] FAIL  - [ ] N/A
- Unity 버전: _(빈칸)_
- aras-p GaussianSplatting 버전/커밋: _(빈칸)_
- 로드 에러 (FAIL 시): _(빈칸)_

### AC7 — Unity 렌더 FPS

- [ ] PASS  - [ ] FAIL  - [ ] N/A
- 해상도: _(빈칸)_
- FPS: _(빈칸)_
- 타겟: 30 FPS+ (계획서 §2 참조)

---

## 3. PLY 포맷 호환 명세 (핵심)

> Alfred가 `PLYFileReader.cs` + `GaussianFileReader.cs` 소스 직접 분석으로 확정한 사실.
> **이것은 INRIA 3DGS 표준 PLY 포맷**. Brush가 INRIA 표준 준수 시 호환 가능성 높음.

### 3.1 헤더 요구사항

- [ ] **magic**: `ply`로 시작
- [ ] **format 줄**: `format binary_little_endian 1.0` **강제**
  - 근거: `PLYFileReader.cs` L43-64 — ASCII PLY는 거부됨
  - big_endian도 거부됨
- [ ] **element vertex 선언**: splat 개수

### 3.2 필수 property 목록 (GaussianFileReader.cs L73 기준)

위치 (3):
- [ ] `x`, `y`, `z`

DC (Spherical Harmonics degree 0, RGB 기본색) (3):
- [ ] `f_dc_0`, `f_dc_1`, `f_dc_2`

불투명도 (1):
- [ ] `opacity`

스케일 (3):
- [ ] `scale_0`, `scale_1`, `scale_2`

회전 (쿼터니언) (4):
- [ ] `rot_0`, `rot_1`, `rot_2`, `rot_3`

### 3.3 SH 계수 (선택적이지만 표준)

- [ ] `f_rest_0` ~ `f_rest_44` (**총 45개, SH degree 3**)
  - 근거: `GaussianFileReader.cs` L99-127 주변
  - SH degree 0만 사용 시 f_rest 전체 생략 가능성 있으나 표준 출력은 degree 3

### 3.4 지원 데이터 타입

- [ ] `float` (4 byte)
- [ ] `double` (8 byte)
- [ ] `uchar` (1 byte) — 주로 color 양자화 시

### 3.5 필수 검증 절차

Brush가 PLY export한 직후 **반드시** 실행:

```bash
# 헤더 덤프 — property 목록 육안 비교
head -c 500 output.ply

# 엔디안/포맷 확인
file output.ply   # "little endian" 문자열 기대
```

- [ ] 헤더에 `binary_little_endian` 포함 확인
- [ ] 위 §3.2 필수 14개 property 모두 존재 확인
- [ ] `f_rest_0` ~ `f_rest_44` 존재 여부 기록: _(빈칸)_

---

## 4. Brush export 검증 명령 (실행 기록)

```bash
head -c 500 output.ply
```

실행 결과 (헤더 텍스트):
```
_(빈칸 — 실제 헤더 붙여넣기)_
```

```bash
file output.ply
```

실행 결과:
```
_(빈칸)_
```

property 누락/추가 항목 요약: _(빈칸)_

---

## 5. 수치 기록

| 항목 | 측정값 | 비고 |
|------|--------|------|
| Brush 빌드 시간 | **7분 6초** | `cargo build --release --bin brush`, 2026-04-20 |
| 학습 데이터셋 | _(빈칸)_ | 이름/이미지 수 |
| 학습 iteration | _(빈칸)_ | |
| 학습 시간 | _(빈칸)_ | 분 |
| final loss | _(빈칸)_ | |
| splat 개수 | **355,045** | Forge task-001 Statue 데이터 (aras-p m_SplatCount) |
| PLY 파일 크기 | **88,052,691 B** | `Statue.ply` 원본 (약 88MB) |
| aras-p asset 크기 | **1,139 B** | `Statue.asset` 포인터 |
| aras-p 데이터 합계 | **83,877,072 B** | pos 4,260,544 + oth 5,680,720 + col 5,767,168 + shs 68,168,640 |
| aras-p format | **VeryHigh (Float32 all)** | format_version=20231020 |
| Unity 렌더 FPS | _(빈칸 — AC5 대기)_ | 해상도 포함 |
| 맥북 모델 / 칩 | **M1 Max** | HOME=/Users/oz6oy |
| macOS 버전 | _(빈칸)_ | 추후 직접 기록 |
| PLY 바이너리 크기 | 148MB | `brush` 릴리즈 바이너리 (155,090,160 B) |

---

## 5.5 Risks / Notes

> 2026-04-20 Alfred 사전 환경 조사에서 드러난 사실. AC6(Unity aras-p 로드) 진입 전 반드시 해소해야 하는 항목 포함.

### 5.5.1 Unity 버전 호환성 (⚠️ 검증 필요)

- **맥북 설치 Unity 버전**: 2017.4.40f1, **6000.2.10f1**, **6000.3.6f1** (Unity 6 존재)
- **aras-p 공식 권장 버전**: 2022.3 LTS
- **위험**: Unity 6 (6000.x) 계열에서 aras-p GaussianSplatting 패키지의 API/렌더 파이프라인 호환성이 미검증
  - URP/HDRP 버전 차이, ScriptableRenderPass 시그니처 변화 등이 빌드 실패로 이어질 수 있음
- **조치안**:
  - (우선) AC6 진입 전 2022.3 LTS를 추가 설치하여 baseline 확보
  - (병행) Unity 6000.3.6f1에서 aras-p 로드 시도하고 실패 시 에러 로그 기록 → AC6 체크리스트에 별도 기록
- [ ] 호환성 검증 완료

### 5.5.2 맥북 기존 자산 경로

- **맥북 Unity-SplatForge 원본**: `/Users/oz6oy/repos/3DGS/projects/unity-splatforge/` (맥미니 초기 push 소스)
- **기존 학습 3DGS PLY 8개**: `/Users/oz6oy/repos/3DGS/unity-lab/Assets/GaussianAssets/KIRI/` 하위
  - 예: `Statue.ply` **88MB** (맥미니 샘플과 동일 크기)
  - AC5 Brush PLY export 시 포맷/크기/헤더를 이 기존 PLY와 교차 비교하면 호환성 1차 판단 가능

### 5.5.3 디스크 여유

- 앱 정리 후 **68GB** 확보 → Brush 빌드 + 학습 데이터셋 + PLY 산출물에 충분

### 5.5.4 Unity 6 HDRP batchmode 초기화 정지 이슈 (Forge task-001, 2026-04-22)

**증상 요약**: Unity 6000.3.6f1 + HDRP 프로젝트에서 `-batchmode` 실행 시 `Library Redirect Path: Library/` 로그 출력 이후 초기화가 무한 대기. 프로세스 CPU **0.3%**, RSS **172MB**로 정체 — asset DB refresh 또는 shader compile 단계 정지 추정. Library/ArtifactDB(84MB)·SourceAssetDB(14MB)가 열려 있으나 변경 없음.

**3회 시도 내역** (timeout 600s, 모두 SIGTERM 종료):
1. 1차: `-batchmode -nographics` — EXIT_CODE=124 (timeout)
2. 2차: `-batchmode -nographics` 재시도 — EXIT_CODE=143 (SIGTERM)
3. 3차: `-batchmode` (nographics 제외) — EXIT_CODE=143 (SIGTERM)

**Forge 제안 후속 조치 (원문)**:
- 맥미니(Alfred)에서 Unity GUI 모드로 프로젝트를 한 번 열어 Library 캐시를 완전히 초기화한 뒤 batchmode 재시도.
- 또는 Unity 6000.3.6f1의 batchmode HDRP 호환성 이슈 조사.

**Alfred 추가 권고**:
- (a) 빈 **URP 빈 프로젝트**(또는 Built-in RP) 재시도 — HDRP 파이프라인 특정 원인 배제/확증 목적. batchmode가 URP에서 정상 작동하면 HDRP 초기화 단계 이슈로 확정 가능.
- (b) **GUI 기반 PlayMode 캡처로 우회** — Unity GUI에서 직접 GaussianSplatAssetCreator 호출 또는 PlayMode 실행 후 스크린샷/프로파일러 기록으로 AC5(렌더 검증)에 진입.

**배치 래퍼 상태**: Forge가 `Assets/Editor/BatchSplatAssetCreator.cs`(리플렉션 래퍼, `GaussianSplatAssetCreator.CreateAsset()` 호출)를 신규 작성. Unity 초기화 이후 스크립트 자체는 정상 컴파일 확인 (`Library/ScriptAssemblies/Assembly-CSharp-Editor.dll` 존재).

### 5.5.5 Forge task-001 산출물 경로 (맥북)

원본 결과 JSON: `/Users/alfredsteinberg/Repos/3DGS/Unity-SplatForge/research-notes/pocs/forge-result-001-statue-aras-p.json`

맥북 aras-p 산출 자산 (`/Users/oz6oy/repos/3DGS/unity-lab/Assets/GaussianAssets/KIRI/Statue/`):
- `Statue.asset` (1,139 B) — aras-p 포인터 asset
- `3DGS_pos.bytes` (4,260,544 B)
- `3DGS_oth.bytes` (5,680,720 B)
- `3DGS_col.bytes` (5,767,168 B)
- `3DGS_shs.bytes` (68,168,640 B)

### 5.5.6 rev2 failed 재현 — HDRP 가설 반증 (Forge task-001-rev2, 2026-04-22)

**rev2 개요**: task-001 실패 원인 진단을 위해 Unity Test Framework 기반 EditMode 테스트로 접근. `-runTests`를 통해 PLY 헤더 검증 + asset 로드 테스트 2건을 실행하도록 설계. 결과 요약: **failed** (duration 1200s, exit_code 124 — Unity timeout 정확히 도달, SIGKILL).

**원본 결과 JSON**: `/Users/alfredsteinberg/Repos/3DGS/Unity-SplatForge/research-notes/pocs/forge-result-001-rev2.json`

**AC 판정 (6건)**:

| AC | 판정 | 비고 |
|----|------|------|
| AC-REQUIRED-READING | **PASS** | `unity-test-framework-kit.md`, `unity-test-operations.md` 모두 Read 확인 |
| AC-TEST-DISCOVERED | **FAIL** | Unity Test Runner가 asset import/컴파일 단계 미도달 |
| AC-PLY-HEADER-TEST-PASS | **FAIL** | 테스트 미실행 |
| AC-ASSET-LOAD-TEST-PASS | **FAIL** | 테스트 미실행 |
| AC-NUNIT-XML | **FAIL** | `editmode.xml` 미생성 |
| AC-NO-BATCHMODE-HANG | **FAIL** | task-001 동일 증상 재현 — Unity 로그 102줄에서 정지 ('Library Redirect Path' + FMOD 초기화 후) |

**Forge 핵심 발견 (원문)**:

> "-executeMethod든 -runTests든 Unity batchmode 진입점과 무관하게 동일한 위치에서 정체. 테스트 프레임워크 문제가 아닌 Unity Editor 자체의 프로젝트 로딩 문제."

**마스터 지적 수용 + 자체 반성**:
- 마스터 지적: 가우시안 렌더링은 URP에서 문제가 있어 **HDRP를 선택한 역사적 배경**이 있고, **sweet-slides가 같은 Unity 환경에서 정상 동작 중**이다. 따라서 HDRP를 원인으로 단정한 Alfred의 가설은 근거가 부족하다.
- Alfred 반성: §5.5.4 Alfred 권고 (a)에서 "URP 빈 프로젝트 재시도"를 HDRP 배제 확증 수단으로 제시했으나, sweet-slides 반례를 간과했다. HDRP 파이프라인 자체보다 해당 프로젝트 특유의 Library 상태·asset 구성·Unity 6000.3.6f1 버그 가능성을 먼저 의심했어야 한다.

**가설 순위 재배치 (HDRP를 last로)**:

1. **Library cache I/O** — ArtifactDB(84MB)·SourceAssetDB(14MB) 열림 상태에서 진행 정체. GUI 모드 1회 진입으로 Library 재생성 후 재시도 필요성 최상위.
2. **Asset import deadlock** — 대형 PLY(88MB) 또는 기존 asset 그래프가 AssetDatabase refresh 단계에서 교착.
3. **Unity 6000.3.6f1 특이 버그** — 6000.3.x 계열의 batchmode 초기화 회귀. → **반증됨 — rev3에서 재현** (6000.3.13f1 동일 증상). §5.5.8 참조.
4. **Resource carry-over** — 이전 GUI 세션 잔존 자원(FMOD·shader compile 캐시)이 batchmode와 충돌.
5. **Lock 잔존** — `.lock`·`.UnityLockfile` 잔존이 batchmode 진입 차단.
6. **HDRP 파이프라인** (최하위, 근거 부족) — sweet-slides가 동일 Unity 환경에서 정상 작동 중이므로 원인 가능성 낮음.

**rev3 진행 상황 (2026-04-22 방금 시작)**:
- Unity **6000.3.13f1** (동일 3.x 브랜치 최신 패치) Unity Hub headless CLI로 설치 시작.
- 설치 완료 후 **rev3** 실행 예정 — sweet-slides 킷 + rev2가 생성한 테스트 자산 5건 재사용.

### 5.5.7 Forge 자산 재사용 목록 (rev2 → rev3)

rev2가 생성한 테스트 코드 자산. **rev3에서 삭제하지 말고 재활용**한다:

- `Assets/Tests/EditMode/Helpers/TestFixtureLoader.cs`
- `Assets/Tests/EditMode/Helpers/TestStageBuilder.cs`
- `Assets/Tests/EditMode/Helpers/SelectorAliasRegistry.cs`
- `Assets/Tests/EditMode/SplatForge.Testing.EditMode.asmdef`
- `Assets/Tests/EditMode/StatueImportTests.cs`

### 5.5.8 rev3 failed — Unity 6000.3.6f1 특이 버그 가설 반증 (Forge task-001-rev3, 2026-04-22)

**rev3 개요**: rev2에서 남은 "Unity 6000.3.6f1 특이 버그" 가설을 검증하기 위해 **Unity 6000.3.13f1** (동일 3.x 브랜치 최신 패치) 신규 설치본으로 동일 절차를 재실행. 결과: **failed** — rev2와 동일 지점에서 정체. 오히려 **더 일찍** 멈춤 (FMOD 단계 미도달).

**원본 결과 JSON**: `/Users/alfredsteinberg/Repos/3DGS/Unity-SplatForge/research-notes/pocs/forge-result-001-rev3.json`

**환경 정보 (rev3 로그 상단에서 발견)**:
- macOS **26.0.1**, Darwin **25.0.0**, **arm64**, Memory **64GB**
- Unity **6000.3.13f1** (신규 설치본, Library 최초 빌드)

**2회 시도 내역** (모두 Library Redirect Path 단계에서 hang):

| 시도 | 조건 | duration | 로그 줄수 | CPU | 프로세스 상태 | 결과 |
|------|------|----------|-----------|-----|--------------|------|
| 1차 | Unity 6000.3.13f1 최초 실행 | 560s | 100줄 | 0.4% | SLEEPING | Library Redirect Path hang |
| 2차 | `LibraryInitializing` 삭제 후 재실행 | 510s | 100줄 | 0.4% | SLEEPING | 동일 패턴 |

**rev2와의 비교 특이점**:
- **rev3는 FMOD 단계에조차 미도달** (로그 100줄에서 정지)
- rev2는 102줄로 **FMOD 초기화까지는 진행**했음 → rev3가 오히려 더 이른 단계에서 멈춤

**Forge 핵심 결론 (원문)**:

> "Unity 6000.3.6f1 특이 버그 가설 반증. 6000.3.x 계열 공통 또는 이 프로젝트 고유 문제."

**AC 판정 (6건)**:

| AC | 판정 | 비고 |
|----|------|------|
| AC-REQUIRED-READING | **PASS** | 요구 문서 Read 완료 |
| AC-TEST-DISCOVERED | **FAIL** | Unity 초기화 미완 |
| AC-PLY-HEADER-TEST-PASS | **FAIL** | 미실행 |
| AC-ASSET-LOAD-TEST-PASS | **FAIL** | 미실행 |
| AC-NUNIT-XML | **FAIL** | 미생성 |
| AC-NO-BATCHMODE-HANG | **FAIL** | 2회 모두 hang 재현 |

**created_files**: `[]` — rev2 자산 재사용, 신규 생성 없음.

**Forge 추가 가설 5가지 (원문 인용)**:

1. **Library 캐시 호환성** — Library가 6000.3.6f1에서 빌드됨. 6000.3.13f1이 이를 업그레이드하려다 hang 가능성. Library 삭제 후 클린 빌드 시도 제안.
2. **HDRP/URP 파이프라인** — 프로젝트가 HDRP 사용 시 batchmode -nographics와 충돌 가능. 이번 실행은 -nographics 미사용.
3. **PackageManager lock** — UPM 소켓 (/tmp/Unity-Upm-*.sock) 관련 교착 가능성.
4. **macOS 26.0.1 호환성** — Darwin 25.0.0 + Unity 6000.3.x ARM64 조합 문제 가능성.
5. **-batchmode 없이 GUI 모드로** 프로젝트 열어 Library 업그레이드 완료 후 batchmode 재시도.

**가설 순위 재재배치 (Unity 6 특이 버그 배제)**:

rev3에서 6000.3.13f1이 동일(오히려 더 이른) 증상을 보이면서 "Unity 6000.3.6f1 특이 버그" 가설이 반증됨. 새 최상위 후보:

- **A. Library cache 호환성** (Forge 가설 #1) — 6000.3.6f1로 빌드된 Library를 6000.3.13f1이 업그레이드 시도하며 교착. Library 완전 삭제 후 클린 빌드 시 돌파 가능성 최상위.
- **B. macOS 26 + Unity 호환성** (Forge 가설 #4) — Darwin 25.0.0 (macOS 26.0.1) + Unity 6000.3.x ARM64 조합. OS·Unity·Apple Silicon 스택 회귀 가능성.
- **C. 프로젝트 고유 설정** (HDRP/asmdef/package 구성) — sweet-slides는 정상이므로 프로젝트 자체의 파이프라인 설정·asmdef·Package 의존성이 Unity 초기화를 막을 가능성.

---

## 6. 최종 판정

> **현재 상태: 보류 (in-progress)** — Forge task-001 partial → **rev2 failed** (HDRP 가설 반증) → **rev3 failed** (Unity 6000.3.6f1 특이 버그 가설 반증). 근본 원인 미확정. 다음 실험안 3개 후보 중 택1 필요.

**다음 실험안 후보**:

- **(a) Library 완전 삭제 + 클린 빌드 (rev4)** — Forge 가설 #1 검증. 6000.3.13f1 단독으로 Library를 처음부터 빌드. 캐시 업그레이드 교착이 원인이면 돌파.
- **(b) sweet-slides 프로젝트에서 동일 Unity `-runTests` 명령 컨트롤 테스트** — 환경(macOS 26 + Unity 6000.3.13f1) vs 프로젝트(Unity-SplatForge 고유 설정) 분리. sweet-slides가 성공하면 B 배제, C 확증. 실패하면 B 최상위 확정.
- **(c) Unity 2022.3.62f3 LTS 설치 후 재시도** — aras-p 공식 지원 버전으로 회귀. 성공 시 Unity 6 계열 전체 회피 경로 확보 (단, 프로젝트 현재 Unity 6 기반이므로 재구성 비용 수반).

- [ ] **PASS** — 전 AC 통과, Brush + aras-p 단일 파이프라인 성립
  - 아키텍처 결정 권고: Python 서버 경로 폐기, 맥북 단일 파이프라인 채택
- [ ] **PARTIAL** — 일부 AC 실패 (보완 가능)
  - 실패 항목: _(빈칸)_
  - 보완 방안: _(빈칸)_
  - 아키텍처 결정 권고: _(빈칸)_
- [ ] **FAIL** — 치명적 AC 실패, 경로 변경 필요
  - 실패 원인 요약: _(빈칸)_
  - 아키텍처 결정 권고: Fallback 경로 §7 채택

**아키텍처 결정 권고 상세**:

_(빈칸 — 마스터 최종 판단 기재. 현재는 AC5 렌더 검증 후 확정)_

---

## 7. Fallback 경로 (실패 시)

> 상세는 [[2026-04-22-brush-macos-single-pipeline]] §5 참조.

### 7.1 차선 A — splat-apple (MLX 기반)

- Apple Silicon MLX 프레임워크 기반 3DGS 학습 구현체
- macOS 네이티브, Rust 의존성 없음
- 검증 포인트: PLY export 포맷이 INRIA 표준과 호환되는지 (§3 동일 기준 적용)
- [ ] Brush 실패 시 검토 대상

### 7.2 차선 B — Python 서버 분리 (원래 계획)

- 서버: nerfstudio/gsplat on Linux GPU
- 맥북: Unity 렌더 전용 클라이언트
- PLY를 네트워크 전송 또는 공유 볼륨으로 이관
- [ ] Brush + splat-apple 모두 실패 시 복귀

### 7.3 FAIL 시 다음 액션

- [ ] 실패 원인을 본 문서 §6에 명확히 기록
- [ ] [[2026-04-22-brush-macos-single-pipeline]]에 결과 링크 추가
- [ ] 마스터와 아키텍처 결정 재회의 (al-chat으로 트리거)

---

## 부록 — 실행 중 발견한 이슈/메모

_(빈칸 — 자유 기술. Brush 버그, Unity 로드 오류, 성능 bottleneck 등)_
