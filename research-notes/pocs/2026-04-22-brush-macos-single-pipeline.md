---
type: poc-plan
status: ready-to-execute
domain: 3dgs-macos
priority: P0
related:
  - "[[../../README]]"
  - "[[../../01-access-paths]] (agent-workspace 경로)"
  - "[[../../findings/macos-3dgs-tools-2026-04-21]] (선행 R1 survey)"
created: 2026-04-21
target_start: 2026-04-22
---

# PoC: Brush + aras-p Unity Single Pipeline (macOS)

> **PoC 목표**: macOS(Apple Silicon M1 Max)에서 CUDA 없이 3DGS 학습 → PLY export → Unity 플러그인 import → 런타임 렌더까지 **end-to-end 단일 파이프라인**이 성립하는지 실증.
>
> **결정 산출물**: 본 PoC 성패가 논문의 아키텍처(Python 서버 유지 vs macOS 단일 파이프라인) 최종 결정의 근거가 된다. 마스터 명시 결정(2026-04-21).

## 0. 컨텍스트 요약

- 현재 Unity-SplatForge는 **Python 서버(CUDA·Windows) + Unity 클라이언트(macOS)** 로 OS가 분리되어 있음
- 최종 지향점: **macOS 단일 파이프라인**. Python 서버 분리는 임시 구조
- 근거: `findings/macos-3dgs-tools-2026-04-21.md` — Brush(Apache-2.0, Rust+wgpu, stars 3961)가 최우선 PoC 후보로 선정됨
- 차선: splat-apple(MLX, Apple Silicon 특화, 라이선스 부재 → 연구용 한정)

## 1. 후보 및 선정 근거

### 최우선: **Brush (ArthurBrussee/brush)**

- 레포: https://github.com/ArthurBrussee/brush
- 라이선스: **Apache-2.0** (상용·학술 배포 자유)
- 최근 커밋: 2026-04-19
- 백엔드: Rust + wgpu (Burn 프레임워크)
- 크로스플랫폼: macOS / Windows / Linux / 브라우저 모두 지원
- 입출력: COLMAP 입력 지원, **PLY 출력 지원 (확인됨)**
- 벤치마크: README에 Apple Silicon 전용 수치는 없음 — PoC에서 실측

### Unity 측: **aras-p/UnityGaussianSplatting** (이미 논문에 사용 중)

- 레포: https://github.com/aras-p/UnityGaussianSplatting
- 메뉴: `Tools → Gaussian Splats → Create GaussianSplatAsset`
- M1 Max 벤치: 6.1M splat @ 21.5ms/46FPS (공식 README)

## 2. PoC 성공 기준 (Acceptance Criteria)

| # | 기준 | 측정 방법 |
|---|------|-----------|
| AC1 | Brush가 macOS(M1 Max)에서 **빌드·실행 성공** | `brush --help` 정상 출력 |
| AC2 | 소규모 COLMAP 데이터셋 **학습 완료** (≤ 30분) | terminal log에 final iter 기록 |
| AC3 | **PLY export 성공** | 출력 파일 크기 > 0, PLY magic bytes 정상 |
| AC4 | **aras-p 플러그인으로 import 성공** | `Create GaussianSplatAsset` 메뉴에서 에러 없음 |
| AC5 | Unity Editor에서 **런타임 렌더 확인** | Scene 뷰에 splat 가시화 |
| AC6 | PhysX 프록시 충돌체와 **결합 가능 확인** | HybridSceneObject 래핑 후 충돌 판정 정상 |
| AC7 | 학습 시간·Splat 수·품질 **수치 기록** | 논문 4.1 구현 환경 갱신용 |

**PASS**: AC1-AC5 모두 통과 → macOS 단일 파이프라인 채택
**PARTIAL**: AC1-AC3까지만 (PLY 포맷 호환 이슈 등) → 중간 변환 레이어 검토
**FAIL**: AC1/AC2 실패 → Python 서버 구조 유지 결정

## 3. 사전 준비 체크리스트

### 3.1 맥북 환경
- [ ] Rust 툴체인 설치 (`rustup` 또는 homebrew `rustup-init`)
- [ ] Xcode Command Line Tools (보통 기설치)
- [ ] Brush 레포 clone: `git clone https://github.com/ArthurBrussee/brush.git`
- [ ] Disk 공간: 학습·출력용 10GB+ 여유 확인

### 3.2 데이터셋
- [ ] COLMAP 포맷 소규모 장면 1개 확보
  - 옵션 A: 기존 논문/프로젝트에서 재사용 (`SplatForge-UnityClient/Assets/Samples/`에서 제거된 Statue.ply 원본 COLMAP?)
  - 옵션 B: 퍼블릭 데이터셋 (Tanks & Temples 소규모 scene, Mip-NeRF 360 garden 일부 등)
  - 옵션 C: 아이폰 촬영 20-50장 → COLMAP 추정 (~30분 전처리)
- [ ] 권장: 옵션 B — 재현성·시간 절약

### 3.3 Unity 프로젝트
- [ ] 기존 `SplatForge-UnityClient/` 그대로 활용
- [ ] aras-p 플러그인 이미 포함됨 (manifest.json 확인)
- [ ] 테스트용 빈 씬 하나 생성 권장

## 4. 실행 절차 (단계별)

### Step 1: Brush 설치·빌드 (예상 15-30분)

```bash
# 맥북에서
cd ~/workspace  # 또는 적절한 위치
git clone https://github.com/ArthurBrussee/brush.git
cd brush
cargo build --release
./target/release/brush --help
```

**주의**: Rust 첫 빌드는 의존성 컴파일로 시간 걸림. `cargo install`로 바이너리 전역 설치도 가능.

### Step 2: 데이터셋 준비 (옵션에 따라 5분~1시간)

```bash
# COLMAP 데이터 예시 구조
dataset/
  images/
    frame_0001.jpg
    ...
  sparse/0/
    cameras.bin
    images.bin
    points3D.bin
```

### Step 3: 학습 실행 (예상 10-30분)

```bash
./target/release/brush train \
  --source dataset/ \
  --output dataset/output.ply \
  --iterations 7000  # 기본값 30000은 PoC에 과할 수 있음
```

**벤치마크 기록**:
- 시작·종료 시각
- GPU 사용률 (Activity Monitor)
- 최종 iter 수 / loss
- 출력 PLY 파일 크기 + Splat 수 추정

### Step 4: Unity import (예상 5분)

1. `SplatForge-UnityClient/` 열기 (Unity 2022.3 LTS)
2. `Assets/GaussianSplats/` 폴더에 `output.ply` 복사
3. 메뉴: `Tools → Gaussian Splats → Create GaussianSplatAsset`
4. 생성된 `.asset` 파일을 빈 GameObject의 `GaussianSplatRenderer`에 할당
5. Scene 뷰에서 렌더 확인

**실패 시나리오**:
- PLY 포맷 불일치 (Brush는 INRIA 포맷 준수하나 세부 필드 차이 가능)
- 해결: aras-p 플러그인의 예상 포맷 재확인 (`spherical harmonics` 계수, rotation quaternion 순서 등)

### Step 5: HybridSceneObject 결합 (예상 10분)

1. 렌더된 splat GameObject에 `HybridSceneObject` 컴포넌트 추가
2. 프록시 BoxCollider 자동 생성 확인
3. 테스트 큐브를 씬에 던져 충돌 판정 확인

### Step 6: 결과 기록

`research-notes/pocs/2026-04-22-brush-macos-results.md` 신규 생성:
- AC1-AC7 각 항목 PASS/FAIL + 증거(로그, 스크린샷, 수치)
- 학습 시간 Apple Silicon 실측
- PLY 호환 이슈 및 해결책
- 최종 판정: PASS / PARTIAL / FAIL
- 아키텍처 결정 권고

## 5. 실패 대응 플랜

### 5.1 Brush 빌드 실패
- Rust 버전 이슈 → `rustup update` 후 재시도
- 특정 크레이트 의존성 충돌 → GitHub Issue 검색
- 그래도 실패 → **차선 후보 splat-apple(MLX)** 시도 (라이선스 제한 감수, 학위논문용 한정)

### 5.2 학습 실패 / 메모리 부족
- `--iterations` 감소
- 입력 이미지 해상도 다운샘플
- 배치 크기 조정 (Brush 설정 확인)

### 5.3 PLY 포맷 비호환
- Brush export 포맷 덤프 → INRIA 표준 대비 검토
- aras-p 플러그인 소스 내 import parser 확인
- 변환 스크립트 작성 or aras-p측 patch 검토 (Apache-2.0이므로 가능)

### 5.4 전면 실패
- **Python 서버 + CUDA(Windows) 구조 유지** 결정
- 논문 한계 섹션에 "Apple Silicon 네이티브 학습 파이프라인의 공백" 명시
- Future work: Brush 업스트림 Apple 벤치 실측 기여 방향

## 6. 시간 예산 (총 예상 4-6시간, 1-2일 분산 가능)

| Step | 낙관 | 비관 |
|------|------|------|
| 1. Brush 설치 | 15분 | 60분 (빌드 실패 대응) |
| 2. 데이터셋 | 5분 | 60분 (아이폰 촬영 + COLMAP) |
| 3. 학습 | 10분 | 60분 (iter 조정 + 재시도) |
| 4. Unity import | 5분 | 30분 (PLY 호환 이슈) |
| 5. HybridSceneObject | 10분 | 20분 |
| 6. 결과 기록 | 20분 | 30분 |
| **합계** | **~1시간** | **~4.5시간** |

## 7. Alfred 보조 범위

Alfred는 맥미니에서 다음을 지원:
- **원격 조회**: `bash scripts/remote/macbook.sh ls/read`로 Brush 레포 상태 확인
- **결과 문서화**: 마스터가 PoC 수행 시 수치·로그를 al-chat으로 공유하면 Alfred가 `pocs/2026-04-22-brush-macos-results.md`에 구조화
- **PLY 호환 분석**: aras-p import 코드(C#) 읽기 + Brush export 포맷 교차 검토
- **실패 시 차선 검토**: splat-apple 또는 OpenSplat 대안 Plan 업데이트

## 8. 시작 직후 Alfred 자동 수행 (다음 세션 startup 후)

1. 본 문서(pocs/2026-04-22-brush-macos-single-pipeline.md) 읽기
2. `bash scripts/remote/macbook.sh status` — 맥북 idle 확인
3. `bash scripts/remote/macbook.sh exec "which cargo rustc"` — Rust 설치 여부
4. `bash scripts/remote/macbook.sh exec "ls ~/workspace"` 또는 적절한 작업 공간 확인
5. 마스터에게 al-chat으로 준비 상태 보고 + Step 1 착수 여부 확인

---

관련: [[../README]] · [[../../../findings/macos-3dgs-tools-2026-04-21]]
