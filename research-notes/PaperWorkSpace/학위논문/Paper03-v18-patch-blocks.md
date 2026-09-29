# Paper03 v17 → v18 복사·붙여넣기 패치 블록 (2차본 hwpx 적용용)

> 작성: Ada, 2026-07-07 (마스터 07-07 09:25 지시)
> 목적: 마스터가 **2차본 hwpx**(한글/HWP)를 열고 [찾기]로 원문 블록을 찾아 [바꾸기/붙여넣기]로 v18 신문을 넣을 수 있도록, 변경 테이블 전 항목을 "찾기 ↔ 바꾸기" 쌍으로 정리한 문서.

---

## 0. 사용법 (반드시 먼저 읽기)

1. **한글(HWP)에서 2차본 hwpx를 연다.** 이 문서의 패치 블록을 **위→아래(문서 등장 순서)** 로 하나씩 적용한다.
2. **찾기:** 한글 [찾기] `Ctrl+F` → 각 블록의 **▼ 찾기** 코드블록 첫 문장(또는 캡션)을 붙여넣어 검색한다. 긴 문단은 첫 1~2문장만 검색해도 위치를 잡을 수 있다.
3. **바꾸기:** 찾은 블록을 **▼ 바꾸기** 코드블록 텍스트로 통째로 교체한다. [바꾸기]는 서식이 섞이기 쉬우므로, **블록 선택 후 붙여넣기**를 권장한다.
4. **표(表):** HWP 표는 텍스트 찾기·바꾸기가 되지 않는다. → **캡션으로 표를 찾은 뒤**, 표를 새로 그리거나(권장) 셀별로 값을 수정한다. 각 표 블록은 "행별 셀 값"을 순서대로 제공한다. **삭제 표**는 캡션부터 마지막 행까지 표 전체(캡션 문단 포함)를 지운다.
5. **식별자 표기 주의(중요):** 본 문서의 찾기/바꾸기에서 `cozy_bedroom`·`modern_office`·`living_room`·`llm_only`·`random_physics`·`resolved_overlaps`·`asset_path`·`phase2_summary` 등 식별자는 **밑줄(`_`)을 포함한 실제 논문 표기**를 따른다. (참고: hwpx 텍스트 추출본에서는 추출 과정상 밑줄이 누락되어 `resolvedoverlaps`처럼 보이지만, **실제 한글 파일 본문에는 밑줄이 있으므로** 밑줄 포함으로 검색해야 매칭된다. — audit RESULT의 표기 노이즈 항목 참조.)
6. **서식:** 바꾸기 텍스트는 md 마크업(`**굵게**`·`#`·`|표|`·`` `코드` ``·`[^각주]`)을 모두 제거한 **순수 본문**이다. 서식 의도가 있는 곳은 `※ 주의`에 "굵게 처리" 등으로 안내한다. 각주 마커(`[^N]`)는 본문에서 제거했으며(2차본은 각주가 이미 페이지 각주로 존재), 인용 번호 `[1]`·`[20]` 등은 실제 표기이므로 그대로 둔다.

## 항목 총수 · 유형별 카운트

- **변경 테이블 A(38행: #1~#38) + A-후속(4행: #39~#42) = 42행**, **+ 2차본 정합 2건(D1·D2)** = **총 44개 변경 항목.**
- 이 중 **#34는 2차본에 이미 정합**되어 있어(양쪽 모두 "표 5a·5b") **패치 불필요(무변경)**. → **실적용 43건**을 아래 패치 블록으로 제공(다중-블록/표 항목은 한 블록 안에 하위 파트 a·b·c로 나눔).
- **유형별(변경 테이블 42행):** 실험데이터 21 · 리뷰중대 4 · 리뷰권장 9 · 기계교정 7 · 표제 1. **2차본 정합(D1·D2) 2.**

---

# 패치 블록 (문서 등장 순서)

---

### P-01 · [변경표 #1] 국문 초록 · 실험 요약 문장 · 실험데이터

▼ 찾기 (국문 초록 3번째 문단)
```
침실·사무실·거실 세 가지 방에 적용해 본 결과, 작업 시간이 약 4/5 단축되었다. Phase 2 sweep(3 시나리오 × 3 조건 × N=5 × mock·openai 2 provider, 총 90 trial)에서 실 LLM(gpt-4o-mini, $0.0094 USD) 도입 시 침실·사무실의 Floor Adhesion이 14.29% → 42.86%로 약 3.0~3.4배 향상되었고, 충돌 횟수는 약 28~60% 감소(36~50건 → 20~30건)하면서 wall-clock 오버헤드는 +4~12%(30~103 ms/trial)에 머물렀다. LLM을 무작위 좌표 생성으로 대체하고 동일한 물리 보정만 적용하면 Semantic Proximity가 full 대비 약 75~80% 하락(예: 침실 0.79 → 0.19, 사무실 0.67 → 0.17)하여, 의미론적 추론 없이는 실용 수준의 배치가 산출되지 않음을 정량적으로 확증하였다.
```
▼ 바꾸기
```
침실·사무실·거실 세 가지 방에 적용해 본 결과, 작업 시간이 약 4/5 단축되었다. Phase 2 sweep(3 시나리오 × 3 조건 × N=5 × mock·openai 2 provider, 총 90 trial; 2026-07 교정 하니스 재실측)에서, 레이캐스트 기반 ground-snap을 적용한 조건(full)은 mock·openai 양 provider·전 시나리오에서 Floor Adhesion이 사실상 100%에 도달하여, 물리 보정 단계가 바닥 안착을 안정적으로 보장함을 확인하였다. 조건 간 변별은 안착률이 아니라 Semantic Proximity에서 나타났는데, LLM 기반 배치(full·llm_only)는 침실·사무실에서 0.68~0.77을 기록한 반면 무작위 좌표(random_physics)는 같은 두 시나리오에서 0.52~0.58로 하락하여, 의미론적 추론 없이는 실용 수준의 배치가 산출되지 않음을 정량적으로 확증하였다. 다만 거실은 자산 카탈로그가 canonical pair와 정합하지 않아 Semantic Proximity가 0.09 안팎에 머물러, 바닥 안착이 아닌 의미 신호 확보가 본 시나리오의 실질 한계임을 드러냈다.
```

---

### P-02 · [변경표 #2] 국문 초록 · PSNR 기준값 · 리뷰중대

▼ 찾기 (국문 초록 4번째 문단 중 한 문장)
```
Brush(Rust+wgpu Metal, Apache-2.0) 기반 30K iter 학습이 Mip-NeRF360 bonsai에서 PSNR 32.21 dB(원 3DGS 32.4 dB와 0.19 dB 차)를 기록하였고,
```
▼ 바꾸기
```
Brush(Rust+wgpu Metal, Apache-2.0) 기반 30K iter 학습이 Mip-NeRF360 bonsai에서 PSNR 32.21 dB(원 3DGS(Kerbl 외 [1])의 bonsai 30K per-scene 31.98 dB를 0.23 dB 상회)를 기록하였고,
```
※ 주의: 뒤 문장("hloc + LightGlue로 SfM 단계를 …")은 그대로 둔다.

---

### P-03 · [변경표 #33·#42] 표 목차 · Table 7/7a/7b/8 항목 · 기계교정

▼ 안내: "표 목차" 섹션에서 아래 4개 항목을 각각 처리한다.

**(a) Table 7 항목 삭제** — 아래 줄을 통째로 삭제한다.
▼ 찾기
```
Table 7. Placement Accuracy
```
▼ 바꾸기
```
(이 줄 삭제 — v18에서 표 7 제거)
```

**(b) Table 7a 항목 교체**
▼ 찾기
```
Table 7a. Phase 2 측정 결과 — 9-cell 종합 (3 시나리오 × 3 조건 × N=5 × 2 provider, mean ± std). resolved_overlaps 컬럼은 3.4 LayoutValidator iterative push-out이 해소한 부피 침투 누적 수.
```
▼ 바꾸기
```
Table 7a. Phase 2 측정 결과 — 9-cell 종합 (3 시나리오 × 3 조건 × N=5 × 2 provider, 2026-07-06 교정 하니스 재실측). resolved_overlaps 컬럼은 3.4 LayoutValidator iterative push-out이 해소한 부피 침투 누적 수.
```

**(c) Table 7b 항목 교체**
▼ 찾기
```
Table 7b. LLM 효과 — full 조건 mock vs openai 직접 비교 (N=5, mean ± std)
```
▼ 바꾸기
```
Table 7b. Phase 2 씬 적용 wall-clock·총 충돌·LLM latency 보조표 (9-cell × 2 provider, 2026-07-06 교정 하니스 재실측).
```

**(d) Table 8 항목 교체**
▼ 찾기
```
Table 8. Ablation — full / llm_only / random_physics 3 조건 비교 (openai gpt-4o-mini, N=5, mean ± std). 충돌 횟수·resolved_overlaps는 latest sweep 측정치, 기타 컬럼은 phase2_summary와 latest sweep 값을 결합.
```
▼ 바꾸기
```
Table 8. Ablation — full / llm_only / random_physics 3 조건 비교 (openai gpt-4o-mini, N=5, 2026-07-06 교정 하니스 재실측).
```
※ 주의: 표 목차의 "Table 1 … (조사 기준일 2026-04)" 항목은 v18과 이미 일치하므로 **손대지 않는다**(본문 캡션만 D2에서 교정).

---

### P-04 · [변경표 #23] §1.1 연구의 배경 · 3DGS 학습·렌더 서술 · 리뷰권장

▼ 찾기
```
학습 시간이 NeRF의 48시간에서 40분대로 줄고 렌더링이 100fps 이상 나온다는 점은 이미 널리 알려져 있고[3],
```
▼ 바꾸기
```
학습 시간이 Mip-NeRF360의 약 48 GPU-시간에서 40분대로 줄고 렌더링이 실시간(다수 장면 100fps 초과)으로 나온다는 점은 이미 널리 알려져 있고[3],
```

---

### P-05 · [변경표 #19] §1.2 연구 목적과 기여 · 지표명 정합 · 실험데이터

▼ 찾기
```
셋, 이 파이프라인의 효과를 측정하기 위해 Semantic Proximity Score, Safety Zone Violation, Grounding Success Rate라는 세 지표를 정의하고, 수동 저작·LLM 단독·무작위 배치와의 비교 실험 및 절제 실험을 수행하였다.
```
▼ 바꾸기
```
셋, 이 파이프라인의 효과를 측정하기 위해 Semantic Proximity Score, Safety Zone Violation, Floor Adhesion(바닥 안착률)이라는 세 지표를 정의하고, 수동 저작·LLM 단독·무작위 배치와의 비교 실험 및 절제 실험을 수행하였다.
```

---

### P-06 · [변경표 #22] §2.1 3D Gaussian Splatting · 렌더 속도 서술 · 리뷰권장

※ 검색 주의: 이 문단에는 각주 앵커(비등방성·구면 조화 계수·래스터라이저)가 문장 중간에 박혀 있어 블록 통검색이 실패할 수 있다. `100fps 이상이 나오는데` 로 검색해 문단을 찾은 뒤(문서 내 1회 유일 확인됨), 아래 바꾸기 텍스트로 수동 교체하라(각주 앵커는 제자리 유지).

▼ 찾기
```
이를 타일 기반 래스터라이저로 그리면 1080p에서 100fps 이상이 나오는데, NeRF[2]가 같은 해상도에서 0.1fps 수준인 것과 비교하면 세 자릿수 차이다[4].
```
▼ 바꾸기
```
이를 타일 기반 래스터라이저로 그리면 1080p에서 실시간(30fps 이상, 다수 장면에서는 100fps를 넘는다)으로 동작하는데, NeRF[2]가 같은 해상도에서 약 0.1fps 수준에 머무는 것과 비교하면 수백 배 차이다[4].
```
※ 주의: 2차본 본문에서 "래스터라이저" 앞에 각주 설명이 인라인으로 붙어 있을 수 있다. 각주 텍스트는 건드리지 말고 "이를 타일 기반 래스터라이저로 그리면 …" 문장만 교체한다.

---

### P-07 · [2차본 정합 D2] §2.4.1 · Table 1 본문 캡션 표기 정렬 · 표제

▼ 찾기 (표 1 본문 캡션)
```
<Table 1> macOS CUDA-free 3DGS 학습 도구 비교 (2026-04 기준)
```
▼ 바꾸기
```
<Table 1> macOS CUDA-free 3DGS 학습 도구 비교 (조사 기준일 2026-04)
```
※ 주의: 2차본은 표 **목차**는 "(조사 기준일 2026-04)"인데 **본문 캡션**만 "(2026-04 기준)"으로 어긋나 있다(audit D2). 본문 캡션만 위와 같이 맞춘다. 표 데이터·행 값은 변경 없음.

---

### P-08 · [변경표 #30] §3.1 전체 구조 · 표 3 본문 참조 콜아웃 추가 · 기계교정

▼ 찾기 (표 3 <Table 3> 바로 앞 문단 끝)
```
본 연구는 Python 서버를 LLM 호출과 카탈로그 관리에만 사용하고 학습은 macOS 로컬 Brush로 옮긴 단일 파이프라인을 채택한다(2.4.1 참조).
```
▼ 바꾸기
```
본 연구는 Python 서버를 LLM 호출과 카탈로그 관리에만 사용하고 학습은 macOS 로컬 Brush로 옮긴 단일 파이프라인을 채택한다(2.4.1 참조). 시스템 구성 요소는 표 3에 계층별로 정리한다.
```
※ 유형: 문장 **추가**(문단 끝에 한 문장 덧붙임).

---

### P-09 · [2차본 정합 D1] §3.3 · 그림 4 본문 캡션 미번역구 교정 · 표제

▼ 찾기 (그림 4 본문 캡션)
```
그림 4. 선행 연구([56], "3D Gaussian Splatting 기법을 활용한 디지털 조형물 제작 연구") — KIRI Engine 출력 3DGS 자산(자전거·더미)과 폴리곤 메시(의자)의 하이브리드 합성 씬, 본 연구 HybridSceneObject 설계의 prior work demonstrator.
```
▼ 바꾸기
```
그림 4. 선행 연구([56], "3D Gaussian Splatting 기법을 활용한 디지털 조형물 제작 연구") — KIRI Engine 출력 3DGS 자산(자전거·더미)과 폴리곤 메시(의자)의 하이브리드 합성 씬, 본 연구 HybridSceneObject 설계의 선행 시연 사례.
```
※ 주의: 2차본은 그림 **목차**(그림 4)는 이미 "선행 시연 사례"인데 **본문 캡션**만 영문 "prior work demonstrator"가 남아 있다(audit D1). 본문 캡션만 교체한다.

---

### P-10 · [변경표 #7] §3.4 의미론적 배치와 물리 검증 · resolved_overlaps 범위 · 실험데이터

▼ 찾기
```
이 5단계의 효과성은 4.3·4.4에서 resolved_overlaps 메트릭(평균 7~38건/trial)으로 정량화한다.
```
▼ 바꾸기
```
이 5단계의 효과성은 4.3·4.4에서 resolved_overlaps 메트릭(full·random 조건 평균 약 20~111건/trial)으로 정량화한다.
```

---

### P-11 · [변경표 #31] §4.1 구현 환경 · 표 4 본문 참조 콜아웃 추가 · 기계교정

▼ 찾기 (§4.1 제목과 표 4 캡션 사이 — 현재 둘 사이에 문장이 없다)
```
4.1. 구현 환경
<Table 4> Implementation Environment
```
▼ 바꾸기 (제목과 표 캡션 사이에 한 문장 삽입)
```
4.1. 구현 환경
구현 환경은 표 4와 같다.
<Table 4> Implementation Environment
```
※ 유형: 문장 **삽입**. "4.1. 구현 환경" 제목 바로 다음, `<Table 4>` 앞에 "구현 환경은 표 4와 같다."를 새 문단으로 넣는다(제목·표 캡션 자체는 유지).

---

### P-12 · [변경표 #3] §4.2.1 · Brush PSNR 기준값 · 리뷰중대

▼ 찾기 (표 5a 바로 뒤 문단 첫 문장)
```
표준 3DGS bonsai 30K 벤치마크 32.4 dBKerbl [1]와 0.19 dB 차이로, Brush가 Apple Silicon에서 표준 3DGS 품질을 재현함이 확증된다.
```
▼ 바꾸기
```
원 3DGS(Kerbl 외 [1])의 bonsai 30K per-scene 벤치마크 31.98 dB를 0.23 dB 상회하여, Brush가 Apple Silicon에서 표준 3DGS 품질을 재현(오히려 소폭 상회)함이 확증된다.
```
※ 주의: 이어지는 "PSNR은 25K→30K 구간에서 +0.09 dB로 plateau …" 문장은 그대로 둔다.

---

### P-13 · [변경표 #26] §4.2.2 · 6.2 한계 라벨 참조 정합 · 리뷰권장

▼ 찾기 (§4.2.2 첫 문단 시작)
```
6.2 L4에서 지적된 COLMAP no-CUDA bottleneck을 검증하고 단축 가능성을 확인하기 위해,
```
▼ 바꾸기
```
6.2 L7에서 지적된 COLMAP no-CUDA bottleneck을 검증하고 단축 가능성을 확인하기 위해,
```

---

### P-14 · [변경표 #27] §4.2.2 · SfM/학습 비율 서술(규범어 제거) · 리뷰권장

▼ 찾기 (표 5b 뒤 문단 마지막 문장)
```
즉 macOS 단일 파이프라인에서 SfM이 학습보다 길었던 비정상 비율(SfM 59% : Brush 41%)이 hloc 적용으로 정상화(SfM 41% : Brush 59%)된다.
```
▼ 바꾸기
```
즉 macOS 단일 파이프라인에서 SfM이 학습을 상회하던 비율(SfM 59% : Brush 41%)이 hloc 적용 후 학습 우위(SfM 41% : Brush 59%)로 역전된다.
```

---

### P-15 · [변경표 #38] §4.3.1 · 정량 분포 참조 정합(표 7b 통합) · 기계교정

▼ 찾기 ("객체 가시성 주의:" 문단 마지막 문장)
```
N=5 trial 전체의 정량 분포(asset_loaded·resolved_overlaps·collision·proximity)는 4.4 표 7a/7b에서 평균 ± 표준편차로 보고한다.
```
▼ 바꾸기
```
N=5 trial 전체의 정량 분포(floor adhesion·resolved_overlaps·semantic proximity)는 4.4 표 7a에 정리한다.
```

---

### P-16 · [변경표 #4] §4.3.1 · full pipeline 정성 결과 · 실험데이터

▼ 찾기 (그림 7~9 도입 문단)
```
그림 7~9는 본 연구 파이프라인이 실제 LLM 응답을 LayoutValidator + HybridSceneObject 합성 단계를 거쳐 렌더한 full pipeline 결과이다. cozy_bedroom과 modern_office에서 floor adhesion 42.86%·sem≈0.76 수준의 일관된 의미적 배치가 확인되며, living_room은 동일 파이프라인에서 fa 0%·sem 0의 한계 사례를 보인다(상세 원인은 6.2 living_room 한계 케이스 참조).
```
▼ 바꾸기
```
그림 7~9는 본 연구 파이프라인이 실제 LLM 응답을 LayoutValidator + HybridSceneObject 합성 단계를 거쳐 렌더한 full pipeline 결과이다. 2026-07 교정 하니스 재실측 기준 cozy_bedroom과 modern_office는 floor adhesion 100%·semantic proximity 0.73~0.74 수준의 일관된 의미적 배치를 보이며, living_room은 동일 파이프라인에서 floor adhesion은 100%에 도달하나 semantic proximity가 0.09 안팎에 머무는 의미 정합 한계 사례를 보인다(상세 원인은 6.2 living_room 한계 케이스 참조).
```

---

### P-17 · [변경표 #18] 그림 9 캡션 · "한계 사례" 정합 · 실험데이터  ⚠ 2곳 적용

▼ 찾기 (그림 9 캡션 — **그림 목차와 §4.3.1 본문 2곳에 동일 문장이 있다. 두 곳 모두 교체**)
```
placement=5/visible=5, resolved_overlaps=7) — 한계 사례.
```
▼ 바꾸기
```
placement=5/visible=5, resolved_overlaps=7) — 의미 정합 한계 사례.
```
※ 주의: 그림 9 캡션은 (1) 그림 목차, (2) 4.3.1 본문 두 군데에 나온다. **양쪽 다** 위와 같이 "한계 사례" → "의미 정합 한계 사례"로 바꾼다. 캡션의 나머지("자산 카탈로그가 living_room canonical pair와 정합하지 않아 …")는 유지.

---

### P-18 · [변경표 #5] §4.3.1 · random_physics 정성 비교 · 실험데이터

▼ 찾기 (그림 10~12 도입 문단)
```
그림 10~12는 동일 시나리오에 대한 random_physics 절제 비교로, LLM 응답을 무작위 좌표 생성으로 대체하되 동일한 LayoutValidator 물리 보정 단계를 통과시킨 결과이다. cozy_bedroom·modern_office에서 fa는 16.67%로 full 대비 낮고 sem은 0.005~0.231로 의미 추론 부재의 영향이 시각적으로도 드러난다(가구 군집화 부재, 벽면 정렬 무질서).
```
▼ 바꾸기
```
그림 10~12는 동일 시나리오에 대한 random_physics 절제 비교로, LLM 응답을 무작위 좌표 생성으로 대체하되 동일한 LayoutValidator 물리 보정 단계를 통과시킨 결과이다. 동일한 물리 보정을 통과하므로 floor adhesion은 full과 마찬가지로 100% 수준이나, cozy_bedroom·modern_office에서 semantic proximity가 0.52~0.58로 full(0.73~0.74) 대비 하락하여 의미 추론 부재의 영향이 정량·시각 양면에서 드러난다(가구 군집화 부재, 벽면 정렬 무질서).
```

---

### P-19 · [변경표 #6] §4.3.2 · push-out 충돌 해소 수치 · 실험데이터

▼ 찾기 (§4.3.2 2번째 문단)
```
이 간극을 메우는 것이 3.4 (S3)~(S4)의 OverlapBox + iterative push-out 단계이다. 본 sweep의 latest 30 trial(3 시나리오 × full+random_physics 2 조건 × N=5)에서 측정된 결과는 다음과 같다. full 조건의 평균 총 충돌 횟수는 약 22.1건/trial(cozy_bedroom 20.4 ± 4.8, modern_office 26.0 ± 0.0, living_room 20.0 ± 0.0)이며, 이 중 iterative push-out이 평균 약 17.5건을 해소(cozy_bedroom 8.8 ± 5.7, modern_office 37.6 ± 4.6, living_room 6.0 ± 1.1)한다(resolved_overlaps). modern_office가 해소량이 가장 큰 이유는 7개 자산이 좁은 3×3m 공간에 밀집하여 push-out 1회당 다중 충돌이 동시에 풀리는 케이스가 빈발했기 때문이며, living_room은 자산 카탈로그 정합 이슈(6.2 참조)로 일부 충돌이 잔류했다. random_physics 조건에서는 LLM의 의미 페어 추론이 부재한 만큼 초기 부피 침투가 더 광범위(평균 23.9건)하지만 push-out이 평균 21.8건을 해소하여, 충돌 해소 알고리즘 자체는 LLM 의존도와 무관하게 일관되게 동작함을 보인다.
```
▼ 바꾸기
```
이 간극을 메우는 것이 3.4 (S3)~(S4)의 OverlapBox + iterative push-out 단계이다. 2026-07 교정 하니스 재실측(openai) 기준, full 조건에서 iterative push-out이 해소한 부피 침투(resolved_overlaps)는 평균 cozy_bedroom 25.6건, modern_office 111건, living_room 19.6건이다. modern_office가 해소량이 가장 큰 이유는 다수 자산이 3×3m 공간에 밀집하여 push-out 1회당 다중 충돌이 동시에 풀리는 케이스가 빈발했기 때문이다. random_physics 조건에서도 push-out은 평균 약 28~50건을 해소하여, 충돌 해소 알고리즘 자체는 LLM 의존도와 무관하게 일관되게 동작함을 보인다. 한편 llm_only 조건은 LayoutValidator를 우회하므로 정의상 resolved_overlaps가 0이다.
```

---

### P-20 · [변경표 #37] §4.4 · 표 6(Authoring Time) 평균 단축률 · 리뷰권장

▼ 찾기 (표 6 "평균" 행) — HWP 표: 캡션 `<Table 6> Authoring Time`으로 표를 찾은 뒤 **평균 행의 단축률 셀**만 수정.
```
평균 · 7.7 · 약 204분 · 약 45분 · 78.2%
```
▼ 바꾸기
```
평균 · 7.7 · 약 204분 · 약 45분 · 77.9%
```
※ 주의: 표 6의 다른 행(침실 78.6% / 사무실 77.5% / 거실 77.6%)은 그대로 두고 **평균 행의 78.2% → 77.9%** 만 고친다((204−45)/204 및 행별 평균 정합).

---

### P-21 · [변경표 #8] §4.4 · 지표 정의 재작성 + 표 7(Placement Accuracy) 제거 · 실험데이터

**(a) 지표 정의 문단 교체**
▼ 찾기
```
배치 정확도는 세 지표로 보았다. Semantic Proximity Score는 의미적으로 연관된 객체 쌍(침대-협탁, 책상-의자)의 실제 거리가 가구 표준 규격 범위(침대-협탁 0.1-0.5m, 책상-의자 0.3-0.8m 등)에 드는 비율이다. Safety Zone Violation은 각 객체 바운딩 박스가 벽·다른 물체와 겹치는 부피(m³)의 합이다. Grounding Success Rate는 바닥에 정확히 안착된 객체의 비율이다.
```
▼ 바꾸기
```
배치 정확도는 세 지표로 보았다. Semantic Proximity Score는 의미적으로 연관된 객체 쌍(침대-협탁, 책상-의자)의 실제 거리가 가구 표준 규격 범위(침대-협탁 0.1-0.5m, 책상-의자 0.3-0.8m 등)에 드는 비율이다. Safety Zone Violation은 각 객체 바운딩 박스가 벽·다른 물체와 겹치는 부피(m³)의 합이며, 3.4의 resolved_overlaps 메트릭으로도 관측된다. Floor Adhesion(바닥 안착률)은 바닥에 정확히 안착된 객체의 비율이다.

Floor Adhesion의 판정 기준은 다음과 같다. 각 객체의 월드공간 renderer bounds 최하점에서 하방으로 레이캐스트를 쏘아 floor 표면점을 찾고, 최하점과 표면점 사이의 수직 간극이 5cm 미만이면 안착으로 집계한다. 이 5cm 임계는 10cm·20cm 임계와 전 지점에서 동일한 안착률을 주어, 임계 선택에 대한 민감도가 없음을 확인하였다. 다만 본 지표는 "모든 객체는 바닥에 접촉한다"는 전제를 두므로, 책상 위 모니터·램프처럼 LLM이 의도적으로 다른 가구 위에 올린 객체는 바닥 미안착으로 집계된다. 예컨대 사무실 llm_only 조건의 재실측 Floor Adhesion 79.8%가 남긴 미안착분은 파이프라인 실패가 아니라, 모니터(y≈0.6)·램프(y≈1.2)가 책상 위에 배치된 per-object 실측을 반영한 것이다.

본 지표의 초기 측정은 합성 unit 콜라이더를 레이 원점으로 삼는 판정 결함이 있어 값이 객체 pivot·스케일의 우연에 좌우되었다. 이 결함을 월드공간 renderer bounds 기반 하니스로 교정한 뒤 2026-07 mock·openai sweep을 재실측하였으며, 그 결과가 4.4.1이다.
```

**(b) 표 7(Placement Accuracy) 전체 삭제**
▼ 찾기 (표 캡션)
```
<Table 7> Placement Accuracy
```
▼ 처리: **이 캡션 문단부터 표 마지막 행("Grounding Success Rate · 61.5% · 93.7% · +32.2%p")까지 표 전체를 삭제**한다. (v18에는 표 7이 없다.)

**(c) 표 7 뒤 해설 문단 삭제**
▼ 찾기 (표 7 바로 뒤 문단 — 전량 삭제)
```
Grounding Success Rate에서 물리 보정의 효과가 가장 선명하다. LLM만 쓰면 38.5%의 객체가 바닥에 제대로 안착하지 못했는데(공중 부유 혹은 바닥 관통), 레이캐스트 Y 좌표 보정 후 미안착이 6.3%로 줄었다. 잔여 6.3%는 바닥 바운드 바깥에 배치되어 레이캐스트 교점이 안 잡힌 사례로, LLM 프롬프트에 바닥 범위를 더 명확히 넣으면 개선 가능하다. Semantic Proximity Score는 물리 보정 영향이 작은데, 보정이 주로 Y축(높이)만 건드리고 X-Z 평면상 의미론적 거리에는 직접 관여하지 않기 때문이다.
```
▼ 처리: 이 문단은 구 표 7 수치에 대한 해설이므로 **문단 전체 삭제**(대체 내용은 (a)의 Floor Adhesion 판정 기준·하니스 교정 문단이 담당). 삭제 후 곧바로 "4.4.1. Phase 2 sweep — 90 trial 9-cell 종합" 절로 이어진다.

---

### P-22 · [변경표 #9] §4.4.1 · sweep 도입 문단(구 telemetry 삭제) · 실험데이터

▼ 찾기 (§4.4.1 첫 문단)
```
위 표 6·7의 초기 측정과 별도로, 2026-05-07 시점에 mock provider와 openai gpt-4o-mini provider를 동일 시나리오·동일 구현 위에서 N=5 반복으로 sweep하여 LLM 효과를 분리 측정하였다. 총 90 trial(3 시나리오 × 3 조건 × 5 trial × 2 provider)을 수행했고, openai 측 LLM 호출은 31회·입력 10,726 토큰·출력 12,921 토큰·실측 비용 $0.0094 USD였다(평균 latency 13.4 s/call). 표 7a는 9-cell × 2-provider의 wall-clock·정확도 종합을, 표 7b는 full 조건에 한정한 mock vs openai 직접 비교를 정리한다.
```
▼ 바꾸기
```
위 표 6의 저작 시간 측정과 별도로, 2026-07-06 교정 하니스로 mock provider와 openai gpt-4o-mini provider를 동일 시나리오·동일 구현 위에서 N=5 반복으로 재실측하여 LLM 효과를 분리 측정하였다. 총 90 trial(3 시나리오 × 3 조건 × 5 trial × 2 provider)을 실패 0건으로 완료하였다. 표 7a는 9-cell × 2-provider의 정확도(floor adhesion·semantic proximity·resolved_overlaps)를 종합한다.
```

---

### P-23 · [변경표 #10] §4.4.1 · 표 7a 신규 9-cell 교체 · 실험데이터

▼ 처리: 표 7a(캡션 + 18행 표 + 뒤따르는 note 문단)를 **통째로 삭제하고** 아래 신규 캡션 + 표 + note로 교체한다.

**삭제 대상 시작 앵커(찾기, 구 캡션):**
```
<Table 7a> Phase 2 측정 결과 — 9-cell 종합 (3 시나리오 × 3 조건 × N=5 × 2 provider, mean ± std). resolved_overlaps 컬럼은 3.4 LayoutValidator iterative push-out이 해소한 부피 침투 누적 수.
```
**삭제 대상 끝 앵커(구 표 7a 뒤 note 문단, 이 문단까지 삭제):**
```
resolved_overlaps 수치는 latest sweep(2026-05-07, room T1·OverlapBox T2·camera T3 패치 적용) 6 cell에서 측정되었으며, 이전 sweep은 push-out 단계가 없었으므로 "n/a (구 sweep)"로 표기한다. llm_only 조건은 3.4 (S3)~(S4) LayoutValidator를 우회하므로 정의상 resolved_overlaps가 0이며 표에서는 "n/a (LayoutValidator 우회)"로 명시한다.
```

▼ 바꾸기 — **신규 표 7a 캡션:**
```
<Table 7a> Phase 2 측정 결과 — 9-cell 종합 (3 시나리오 × 3 조건 × N=5 × 2 provider, 2026-07-06 교정 하니스 재실측). floor adhesion은 5cm 판정(10/20cm 동일). semantic proximity·resolved_overlaps는 openai 측 값이며, resolved_overlaps는 3.4 LayoutValidator iterative push-out이 해소한 부피 침투 누적 수이다.
```
▼ 신규 표 7a — 열: **시나리오 · 조건 · mock floor adhesion(%) · openai floor adhesion(%) · openai semantic proximity · openai resolved_overlaps** (9행):
```
cozy_bedroom · full · 100.0 · 100.0 · 0.727 · 25.6
cozy_bedroom · llm_only · 85.71 · 100.0 · 0.679 · 0 (Validator 우회)
cozy_bedroom · random_physics · 100.0 · 100.0 · 0.575 · 49.8
modern_office · full · 100.0 · 100.0 · 0.742 · 111
modern_office · llm_only · 75.0 · 79.76 ± 13.3 · 0.770 · 0 (Validator 우회)
modern_office · random_physics · 100.0 · 100.0 · 0.520 · 27.6
living_room · full · 100.0 · 100.0 · 0.086 · 19.6
living_room · llm_only · 100.0 · 100.0 · 0.268 · 0 (Validator 우회)
living_room · random_physics · 100.0 · 100.0 · 0.455 · 49.4
```
▼ 신규 표 7a 뒤 note 문단:
```
floor adhesion은 llm_only(LayoutValidator 우회)를 제외한 full·random 전 셀에서 mock·openai 모두 100%이며, llm_only도 대부분 100%에 근접한다(mock cozy_bedroom 85.71%, mock modern_office 75.0%, openai modern_office 79.76%는 책상 위 배치 객체가 미안착으로 집계된 사례). semantic proximity·resolved_overlaps는 provider 효과 분리 대상인 openai 측 값을 싣는다(mock은 키워드 매칭 기반 사전 정의 좌표). llm_only 조건은 3.4 (S3)~(S4) LayoutValidator를 우회하므로 정의상 resolved_overlaps가 0이다. provider 간 floor adhesion 델타가 소멸(양쪽 100%)하여 종전 판의 provider 간 floor adhesion 직접 비교표는 존재 의의를 잃었으므로 표 7a에 통합하였다(씬 적용 wall-clock·총 충돌은 아래 표 7b에 별도 정리한다).
```
※ 주의: 신규 표 7a에서 full 행의 openai floor adhesion "100.0"은 v18 원문상 **굵게** 강조되어 있다(필수 아님, 표 서식 관행에 따라). 구 표 7a의 6개 열(provider·충돌 횟수·wall-clock 포함)은 신규 표의 6개 열 구성으로 대체된다.

---

### P-24 · [변경표 #11·#39·#41] §4.4.1 · 구 표 7b 제거 + 신규 표 7b·결론 재구성 · 실험데이터(+후속)

> 구 표 7b("LLM 효과 — full 조건 mock vs openai 직접 비교")는 **삭제**하고(양쪽 floor adhesion 100%로 비교 의의 소멸), 결론 문단을 재작성한 뒤 그 자리에 **씬 적용 wall-clock·총 충돌·LLM latency 보조표(신규 표 7b)** 를 신설한다. 순서상 P-23(표 7a) 다음에 오며, P-25(결론)와 함께 §4.4.1 후반부를 이룬다.

**(a) 구 표 7b 전체 삭제**
▼ 찾기 (구 표 7b 캡션)
```
<Table 7b> LLM 효과 — full 조건 mock vs openai 직접 비교 (N=5, mean ± std)
```
▼ 처리: **이 캡션부터 구 표 7b 마지막 행("living_room · 충돌 횟수 · 50.0 ± 0.0 · 20.0 ± 0.0 · -60.0%")까지 표 전체 삭제.** (내용은 (b)의 신규 표 7b로 대체.)

**(b) 신규 표 7b 신설** — P-25 결론 문단(§4.4.1) 뒤에 배치. 도입 문단 + 캡션 + 표 + 결론 문단 순:
▼ 신규 표 7b 도입 문단:
```
정확도 지표와 함께 동일 sweep에서 수집한 씬 적용 wall-clock과 총 충돌 수를 표 7b에 provider별로 대조한다. 여기서 wall-clock은 씬 적용(ground-snap + OverlapBox push-out) 단계 소요이며, 4.2.1·4.2.2의 재구성 파이프라인 wall-clock(표 5a·5b, 시간 단위)과는 별개의 지표다. 총 충돌은 push-out 이전 3.4 (S3) OverlapBox가 검출한 부피 침투 쌍의 누적 수로, 표 7a의 resolved_overlaps(iterative push-out이 해소한 침투 수)와 구분된다.
```
▼ 신규 표 7b 캡션:
```
<Table 7b> Phase 2 씬 적용 wall-clock·총 충돌·LLM latency 보조표 (9-cell × 2 provider, 2026-07-06 교정 하니스 재실측). wall-clock(ms)은 씬 적용(ground-snap + OverlapBox push-out) 단계의 trial당 평균±표준편차이며 재구성 파이프라인 wall-clock(표 5a·5b)과 별개다. 총 충돌은 push-out 이전 OverlapBox가 검출한 부피 침투 쌍의 누적 수다. Δ는 openai의 mock 대비 상대 변화. LLM latency는 openai compose 단계 호출의 trial당 실측치이며 씬 적용 단계와 분리 수행된다(random_physics는 LLM 미호출, 값은 spec 생성 시간).
```
▼ 신규 표 7b — 열: **시나리오 · 조건 · mock wall-clock(ms) · openai wall-clock(ms) · Δwc · mock 총 충돌 · openai 총 충돌 · Δcoll · openai LLM latency(s/trial)** (9행):
```
cozy_bedroom · full · 1811.5 ± 128.6 · 1568.6 ± 73.6 · −13.4% · 36.0 · 30.8 · −14.4% · 9.41
cozy_bedroom · llm_only · 1885.0 ± 153.7 · 1634.7 ± 122.7 · −13.3% · 36.0 · 26.4 · −26.7% · 8.86
cozy_bedroom · random_physics · 1692.6 ± 127.6 · 1809.1 ± 135.7 · +6.9% · 24.4 · 26.4 · +8.2% · 0.06
modern_office · full · 1843.0 ± 201.9 · 1535.3 ± 156.7 · −16.7% · 40.0 · 28.4 · −29.0% · 8.49
modern_office · llm_only · 1855.1 ± 105.0 · 1625.0 ± 186.2 · −12.4% · 36.0 · 25.2 · −30.0% · 7.59
modern_office · random_physics · 1546.1 ± 81.0 · 1589.7 ± 104.5 · +2.8% · 18.4 · 18.8 · +2.2% · 0.06
living_room · full · 1810.8 ± 182.2 · 1526.3 ± 83.7 · −15.7% · 50.0 · 21.6 · −56.8% · 8.21
living_room · llm_only · 1773.2 ± 167.9 · 1555.3 ± 138.6 · −12.3% · 50.0 · 23.2 · −53.6% · 5.99
living_room · random_physics · 1561.4 ± 133.7 · 1588.0 ± 114.5 · +1.7% · 28.0 · 28.0 · 0.0% · 0.06
```
※ 주의: Δwc/Δcoll 열의 full 행 값(−13.4%, −14.4% 등)은 v18 원문상 일부 **굵게** 강조(필수 아님). 표 7b 결론 3종은 P-25 (b)에 이어진다.

---

### P-25 · [변경표 #12·#40] §4.4.1 · 결론 재구성 + 표 7b 결론 3종 · 실험데이터(+후속)

**(a) 구 결론 문단 교체** — 구 표 7b 삭제 자리(구 결론 "표 7a·7b의 핵심 결론은 세 가지이다 …") 를 아래 신규 결론으로 교체.
▼ 찾기 (구 결론 문단)
```
표 7a·7b의 핵심 결론은 세 가지이다. 첫째, 실 LLM(gpt-4o-mini) 도입으로 bedroom·office에서 Floor Adhesion 14.29% → 42.86%로 약 3.0~3.4배 향상되었다. mock 응답은 키워드 매칭 기반 사전 정의 좌표라 평면 분포가 규약 수준에 머무는 반면, 실 LLM은 floor 접지 가능 영역에 후보 좌표를 더 밀집해 산출한다는 가설과 부합한다. 둘째, 충돌 횟수는 모든 시나리오에서 평균 약 28~60% 감소(36~50건 → 20~30건)되어, 실 LLM의 공간 추론이 충돌 회피에 효과적임을 확인했다. 셋째, wall-clock 오버헤드는 +4~12%(30~103 ms/trial)로, LLM 1회 호출 평균 13.4 s가 비동기 파이프라인에서 흡수되어 trial 단위 실시간성이 유지된다. 단 living_room full 케이스는 floor adhesion 0%·semantic 0의 한계 사례를 보여 6.2에서 별도로 다룬다.
```
▼ 바꾸기 (신규 표 7a 결론)
```
표 7a의 핵심 결론은 세 가지이다. 첫째, 레이캐스트 ground-snap을 적용하는 full·random_physics 조건은 mock·openai 양 provider·전 시나리오에서 floor adhesion이 사실상 100%에 도달하여, 바닥 안착은 물리 보정 단계가 안정적으로 보장한다. 초기 측정에서 낮게 나온 값은 파이프라인 실패가 아니라 측정 하니스의 판정 결함이었음이 교정 재실측으로 확인되었다(4.4). 둘째, 조건 간 변별은 semantic proximity에 있다 — LLM 기반 배치(full·llm_only)는 침실·사무실에서 0.68~0.77인 반면 무작위 좌표(random_physics)는 같은 두 시나리오에서 0.52~0.58로, 의미론적 추론의 기여가 분리 측정된다. 셋째, resolved_overlaps는 full·random 조건에서 약 20~111건으로, iterative push-out이 LLM 의존도와 무관하게 부피 침투를 해소함을 보인다. 단 living_room은 floor adhesion 100%에도 semantic proximity가 0.09(full)~0.27(llm_only)에 머물러, 바닥 안착이 아닌 의미 신호 부재가 실질 한계이며 6.2에서 별도로 다룬다.
```

**(b) 표 7b 결론 3종 추가** — P-24 (b)의 신규 표 7b 바로 뒤에 아래 문단을 넣는다(신규 추가 문단).
▼ 추가할 문단:
```
표 7b에서 세 가지 관찰이 추가된다. 첫째, 실 LLM(openai) 배치는 full 조건에서 총 충돌을 mock 대비 −14~−57% 낮춘다(침실 −14.4%, 사무실 −29.0%, 거실 −56.8%). LLM의 공간 추론이 물리 보정 이전 단계에서 이미 겹침이 적은 배치를 산출한다는 뜻으로, 4.3에서 관측된 충돌 회피 효과가 판정 결함을 교정한 재실측에서도 유지됨을 확인한다. 둘째, 이에 따라 openai의 LLM 배치 조건(full·llm_only) 씬 적용 wall-clock은 mock보다 −12~−17% 낮다(full 조건 −13~−17%). 겹침이 적은 배치일수록 물리 push-out 반복이 줄어들기 때문으로, LLM 도입이 씬 적용 단계에 별도 오버헤드를 더하는 것이 아니라 오히려 물리 보정 비용을 줄이는 방향으로 작용한다. 반면 무작위 좌표(random_physics)는 양 provider가 유사한 겹침을 유발하므로 wall-clock 차가 +1.7~+6.9%로 사라진다. 셋째, LLM 호출 자체의 latency는 openai compose 단계에서 trial당 6.0~9.4초로 실측되며, 이는 씬 적용(ground-snap·push-out) 단계와 분리 수행된다(random_physics는 LLM을 호출하지 않아 이 값이 spec 생성 시간 약 0.06초에 그친다).
```
※ 주의: 이어지는 "한편 본 연구의 macOS 단일 기기 재구성 파이프라인 wall-clock·PSNR 측정은 4.2.1 표 5a … 별도 좌표로 명시하였다." 문단은 2차본·v18 동일하므로 **그대로 둔다**(§4.4.1 마지막 문단).

> **§4.4.1 최종 순서 정리(적용 후):** 도입(P-22) → 표 7a 캡션·표·note(P-23) → 표 7a 결론(P-25a) → 표 7b 도입·캡션·표(P-24b) → 표 7b 결론(P-25b) → macOS 재구성 문단(불변).

---

### P-26 · [변경표 #13] §4.5 절제 실험 · 표 8 신 데이터 교체(충돌 횟수 컬럼 제거) · 실험데이터

▼ 처리: 표 8(캡션 + 9행)을 신 데이터로 교체. **충돌 횟수 컬럼을 제거**한다.

**(a) 표 8 캡션 교체**
▼ 찾기
```
<Table 8> Ablation — full / llm_only / random_physics 3 조건 비교 (openai gpt-4o-mini, N=5, mean ± std). 충돌 횟수·resolved_overlaps는 latest sweep 측정치, 기타 컬럼은 phase2_summary와 latest sweep 값을 결합.
```
▼ 바꾸기
```
<Table 8> Ablation — full / llm_only / random_physics 3 조건 비교 (openai gpt-4o-mini, N=5, 2026-07-06 교정 하니스 재실측).
```

**(b) 표 8 본문(행) 교체** — 구 표 8은 열이 **시나리오·조건·floor adhesion·semantic proximity·충돌 횟수·resolved_overlaps**(6열)였다. 신 표 8은 **충돌 횟수 열을 삭제**하여 **시나리오·조건·floor adhesion(%)·semantic proximity·resolved_overlaps**(5열) 9행:
```
cozy_bedroom · full · 100.0 · 0.727 · 25.6
cozy_bedroom · llm_only · 100.0 · 0.679 · n/a (Validator 우회)
cozy_bedroom · random_physics · 100.0 · 0.575 · 49.8
modern_office · full · 100.0 · 0.742 · 111
modern_office · llm_only · 79.76 ± 13.3 · 0.770 · n/a (Validator 우회)
modern_office · random_physics · 100.0 · 0.520 · 27.6
living_room · full · 100.0 · 0.086 · 19.6
living_room · llm_only · 100.0 · 0.268 · n/a (Validator 우회)
living_room · random_physics · 100.0 · 0.455 · 49.4
```
※ 주의: full 행의 floor adhesion·semantic proximity 값은 v18 원문상 **굵게**(필수 아님). 구 표 8의 "충돌 횟수" 열은 삭제(remeasure 미보고).

---

### P-27 · [변경표 #14] §4.5 · 절제 두 가지 분리 서술 · 실험데이터

▼ 찾기 (표 8 뒤 문단)
```
이 표에서 두 가지 분리가 선명하다. 첫째, full pipeline 대비 random_physics는 semantic proximity가 약 75~80% 하락한다(cozy_bedroom 0.793 → 0.188, modern_office 0.674 → 0.171). 동일한 LayoutValidator 물리 보정을 통과한 무작위 배치임에도 의미적 페어 거리(침대-협탁 0.1~0.5m, 책상-의자 0.3~0.8m 등) 만족 비율이 급락한다는 의미로, 의미론적 추론 없이는 일관된 가구 배치가 불가능함을 정량적으로 확증한다. 둘째, llm_only(물리 검증 우회)는 floor adhesion 0%로 모든 시나리오에서 일괄 실패한다. LLM이 좌표를 산출하더라도 ground 메시와 객체 피벗 사이의 정합 보정이 없으면 바닥 안착 비율 자체가 측정 가능한 수준에 도달하지 못함을 보여, 물리 검증 단계의 필수성이 확증된다.
```
▼ 바꾸기
```
이 표에서 두 가지 분리가 드러난다. 첫째, full 대비 random_physics는 semantic proximity가 하락한다(cozy_bedroom 0.727 → 0.575, modern_office 0.742 → 0.520; 약 20~30%). 동일한 LayoutValidator 물리 보정을 통과한 무작위 배치임에도 의미적 페어 거리(침대-협탁 0.1~0.5m, 책상-의자 0.3~0.8m 등) 만족 비율이 낮아진다는 의미로, 의미론적 추론 없이는 일관된 가구 배치가 산출되지 않음을 정량적으로 확증한다. 둘째, 물리 검증 단계의 기여는 바닥 안착보다 충돌 해소에서 분리된다. llm_only(LayoutValidator 우회)는 LLM 원좌표가 대체로 바닥 근처에 놓여 floor adhesion 자체는 79.8~100%로 낮지 않으나, iterative push-out을 거치지 않으므로 부피 침투가 해소되지 않는다(resolved_overlaps 정의상 0). 반면 full은 동일 배치에 push-out을 적용하여 부피 침투를 해소하면서도 안착률 100%를 유지한다. 즉 LLM은 의미론적 배치와 초기 안착을, LayoutValidator는 충돌 해소를 담당하는 역할 분담이 확인된다.
```

---

### P-28 · [변경표 #15] §4.5 · 부수 관찰(office/living 재해석) · 실험데이터

▼ 찾기 (§4.5 마지막 문단)
```
흥미로운 부수 관찰로, modern_office의 semantic proximity는 llm_only(0.727) > full(0.674) 순서로 뒤집힌다. LayoutValidator의 충돌 기각이 의미적 페어 좌표 일부를 우선순위에서 밀어내는 부작용으로 추정되며, 후속 작업에서 충돌 기각 시 의미 페어 점수를 가중치로 회귀하는 정책이 보강 방향으로 남는다. living_room은 full·llm_only 모두 sem 0인 반면 random_physics에서만 0.267을 보이는데, 이는 자산 카탈로그-시나리오 정합 이슈로 6.2에서 별도 다룬다.
```
▼ 바꾸기
```
흥미로운 부수 관찰로, modern_office의 semantic proximity는 llm_only(0.770) > full(0.742) 순서로 소폭 뒤집힌다. LayoutValidator의 충돌 기각이 의미적 페어 좌표 일부를 우선순위에서 밀어내는 부작용으로 추정되며, 후속 작업에서 충돌 기각 시 의미 페어 점수를 가중치로 회귀하는 정책이 보강 방향으로 남는다. living_room은 full 0.086·llm_only 0.268로 LLM 조건의 의미 점수가 낮은 반면 random_physics는 0.455로 오히려 높은데, 이는 자산 카탈로그가 living_room canonical pair와 정합하지 않아 LLM의 의미 배치가 점수로 보상되지 않는 정합 이슈로 6.2에서 별도 다룬다.
```

---

### P-29 · [변경표 #28] §5.1.1 · 초기 PoC 데이터셋 구분 명시 · 리뷰권장

▼ 찾기 (§5.1.1 첫 문단)
```
본 연구의 재구성 경로는 COLMAP 기반 sparse reconstruction과 30K iteration의 gradient optimization을 조합한 오프라인 baseline에 해당한다. 2026-04-23 PoC 측정(302장, 1280×960 입력, macOS M-계열, Brush Rust+wgpu 학습)을 기준으로 feature 추출 약 2분, exhaustive matcher 2~6시간, mapper 1~3시간, 학습 2~4시간이 소요되어 총 5~13시간 범위의 처리 시간을 갖는다.
```
▼ 바꾸기
```
본 연구의 재구성 경로는 COLMAP 기반 sparse reconstruction과 30K iteration의 gradient optimization을 조합한 오프라인 baseline에 해당한다. 표 5a·5b의 bonsai 측정(292장·1556×1037)과 별개인 초기 PoC 측정(2026-04-23; 302장, 1280×960 입력, macOS M-계열, Brush Rust+wgpu 학습)을 기준으로 feature 추출 약 2분, exhaustive matcher 2~6시간, mapper 1~3시간, 학습 2~4시간이 소요되어 총 5~13시간 범위의 처리 시간을 갖는다.
```

---

### P-30 · [변경표 #24] §5.1.2 · hloc retrieval 복잡도 서술 · 리뷰권장

▼ 찾기 (§5.1.2 첫 문단 마지막 문장)
```
hloc[53]은 SuperPoint[55]·SuperGlue 계열의 learned feature와 vocabulary tree retrieval을 결합하여 exhaustive matching의 $O(N^2)$ 비용을 $O(N \log N)$ 수준으로 완화한다.
```
▼ 바꾸기 (수식은 HWP 수식/일반 텍스트 표기; O(N²)·O(N·k)로 기재)
```
hloc[53]은 SuperPoint[55]·SuperGlue 계열의 learned feature와 학습 전역 디스크립터 기반 이미지 retrieval을 결합하여, 후보 매칭쌍을 상수 k개로 제한함으로써 exhaustive matching의 O(N²) 비용을 O(N·k) 수준으로 축소한다.
```
※ 주의: 2차본은 "$O(N^2)$", "$O(N \log N)$" 형태(수식)로 되어 있을 수 있다. 바꾸기에서는 O(N²) → O(N·k)로 표기(원 논문 수식 서식이 있으면 그 서식으로).

---

### P-31 · [변경표 #25] §5.1.3 · throughput 절대비교 단정 회피 · 리뷰권장

▼ 찾기 (§5.1.3 2번째 문단 전체)
```
하드웨어 관점의 근거로는, Apple Silicon의 peak throughput이 동급 데스크톱 GPU 대비 수십 배 열위가 아님에도 실측 재구성 시간 격차가 수십 배에 달한다는 점을 들 수 있다. 따라서 격차의 대부분은 알고리즘 계보와 빌드 옵션 조합으로 환원 가능하다는 관찰이다.
```
▼ 바꾸기
```
다만 Apple Silicon과 데스크톱 GPU의 peak throughput 절대 비교는 본 연구의 측정 범위를 벗어나므로 단정하지 않는다. 본 연구가 확보한 실측치는 재구성 단계 시간뿐이며, 그 격차의 상당 부분은 (i) CUDA 미컴파일 SIFT, (ii) O(N²) exhaustive pairing, (iii) 고정 30K iter의 조합으로 설명 가능하다는 관찰에 그친다.
```

---

### P-32 · [변경표 #20] §5.1.4 · feed-forward 품질 희생 서술(수치·오귀속 제거) · 리뷰중대

▼ 찾기 (§5.1.4 첫 문단 마지막 부분)
```
품질 축에서의 안정적 수치(PSNR·SSIM 관점)를 확보하는 것이 석사 과정 논문 단계에서 재현성과 검증 가능성을 높이는 데 유리하며, feed-forward 계열은 2024-2025년에 걸쳐 품질 측면에서 baseline 대비 PSNR 3~6dB 수준의 희생을 보고하는 것이 일반적이다(Fan et al., 2024[43]; Wang et al., 2024[41]).
```
▼ 바꾸기
```
품질 축에서의 안정적 수치(PSNR·SSIM 관점)를 확보하는 것이 석사 과정 논문 단계에서 재현성과 검증 가능성을 높이는 데 유리하며, feed-forward 계열은 2024-2025년에 걸쳐 품질 측면에서 baseline 대비 품질 희생(PSNR 열위)을 수반하는 것으로 보고되는 경향이 있다.
```

---

### P-33 · [변경표 #16] §6.1 연구 요약 · 안착률 서술 · 실험데이터

▼ 찾기 (§6.1 3번째 문단 첫 문장)
```
수치로 보면, 물리 보정을 적용하면 바닥에 접촉하지 못하는 가구가 10% 이하로 줄고 겹침 부피도 한 자릿수로 떨어진다.
```
▼ 바꾸기
```
수치로 보면, 레이캐스트 ground-snap을 적용하면 가구의 바닥 안착률이 사실상 100%에 이르고, iterative push-out이 부피 침투를 해소한다.
```
※ 주의: 뒤 문장("반대로 LLM을 빼고 무작위로 놓으면 …")에서 v18은 "의미론적 점수(Semantic Proximity)가 저하된다"로 다듬었다. 원문이 "의미론적 점수가 크게 저하된다"이면 아래 P-33b도 함께 적용.

**(P-33b, 선택) 같은 문단 다음 문장 다듬기**
▼ 찾기
```
반대로 LLM을 빼고 무작위로 놓으면 물리적으로는 깔끔하지만 침대 옆에 협탁이 배치되지 않는 식으로 의미론적 점수가 크게 저하된다.
```
▼ 바꾸기
```
반대로 LLM을 빼고 무작위로 놓으면 물리적으로는 깔끔하지만 침대 옆에 협탁이 배치되지 않는 식으로 의미론적 점수(Semantic Proximity)가 저하된다.
```

---

### P-34 · [변경표 #29] §6.2 한계와 전망 · "일곱 가지" + L1~L7 순번 부여 · 리뷰권장

> 이 항목은 **다중 블록**(문단별 순번 접두 부여)이다. 아래 (a)~(g)를 각각 적용한다. (L2 = living_room 문단은 P-35에서 전면 재작성하므로 여기서 제외.)

**(a) 서두 문장 교체**
▼ 찾기
```
여섯 가지 한계가 남아 있다.
```
▼ 바꾸기
```
일곱 가지 한계가 남아 있다. 이하 L1~L7로 지시한다.
```

**(b) L1 접두 부여** — 문단 시작에 "(L1) " 추가
▼ 찾기
```
LLM의 공간 추론은 직사각형 방에서는 무난했으나, L자형 방이나 로프트 같은
```
▼ 바꾸기
```
(L1) LLM의 공간 추론은 직사각형 방에서는 무난했으나, L자형 방이나 로프트 같은
```

**(c) L3 접두 부여**
▼ 찾기
```
3DGS 객체의 정적 특성도 제약이다. 질량이나 마찰 같은 물리적 속성을
```
▼ 바꾸기
```
(L3) 3DGS 객체의 정적 특성도 제약이다. 질량이나 마찰 같은 물리적 속성을
```

**(d) L4 접두 부여**
▼ 찾기
```
본 측정 자동화 파이프라인이 HDRP CustomPassVolume + GaussianSplatHDRPPass 경로를 batchmode 환경에서 종결시키지 못한 통합 한계도 별도로 기록한다.
```
▼ 바꾸기
```
(L4) 본 측정 자동화 파이프라인이 HDRP CustomPassVolume + GaussianSplatHDRPPass 경로를 batchmode 환경에서 종결시키지 못한 통합 한계도 별도로 기록한다.
```

**(e) L5 접두 부여**
▼ 찾기
```
확장성 문제도 있다. 현재 파이프라인은 방 하나 단위에 맞춰져 있다.
```
▼ 바꾸기
```
(L5) 확장성 문제도 있다. 현재 파이프라인은 방 하나 단위에 맞춰져 있다.
```

**(f) L6 접두 부여(+"넷째 한계로" 삭제)**
▼ 찾기
```
넷째 한계로 3DGS 학습은 여전히 CUDA 기반 NVIDIA GPU를 요구하는 반면 Unity 렌더링은 Metal이나 Vulkan에서 동작한다.
```
▼ 바꾸기
```
(L6) 3DGS 학습은 여전히 CUDA 기반 NVIDIA GPU를 요구하는 반면 Unity 렌더링은 Metal이나 Vulkan에서 동작한다.
```

**(g) L7 접두 부여** — (표 10 콜아웃은 P-36에서 함께 처리)
▼ 찾기
```
L4 COLMAP no-CUDA 단계 단축 가능성 확인됨: macOS homebrew COLMAP 빌드는 CPU 전용이라 feature matching이 bottleneck이다.
```
▼ 바꾸기
```
(L7) COLMAP no-CUDA 단계 단축 가능성 확인됨: macOS homebrew COLMAP 빌드는 CPU 전용이라 feature matching이 bottleneck이다.
```

---

### P-35 · [변경표 #17] §6.2 · living_room 한계 문단(L2) 전면 재작성 · 실험데이터+리뷰중대

▼ 찾기 (§6.2 living_room 실패 문단 전체)
```
본 측정에서 living_room 시나리오의 full pipeline 모드는 Floor Adhesion 0%, Semantic Proximity 0의 실패 사례를 보였다(표 7a·8, 그림 9). 이는 다음 세 요인의 복합 작용으로 추정된다. 첫째, 본 실험에서 사용한 자산 카탈로그(Polyhaven CC0 + Kenney CC0 mesh)가 living_room 의미 페어(예: TV-소파 거리, 러그-가구 정렬)를 정의한 canonical 휴리스틱과 정합하지 않은 점. 둘째, FBX 피벗이 객체 중심에 위치하여 ground-snap 시 객체 절반이 바닥 아래로 들어간 점. 셋째, bedroom·office 대비 더 많은 자산(8개)이 좁은 공간에 배치되어 충돌 처리 우선이 의미 페어 검출을 압도한 점이다. 후속 작업에서 (1) 자산별 피벗 자동 보정, (2) 시나리오별 canonical pair 휴리스틱 일반화, (3) 좁은 공간에서의 우선순위 정책 개선(충돌 기각 시 의미 페어 점수 가중치 회귀)을 통해 해소할 예정이다.
```
▼ 바꾸기
```
(L2) 본 측정에서 living_room 시나리오는 교정 재실측 기준 Floor Adhesion 100%에 도달하였으나(표 7a·8, 그림 9), Semantic Proximity가 full 0.086·llm_only 0.268로 침실·사무실(0.68~0.77)에 크게 못 미친다. 초기 측정에서 관찰된 'Floor Adhesion 0% 실패' 서술은 측정 하니스의 판정 결함에서 비롯된 것으로, 교정 후 확인된 실질 한계는 바닥 안착이 아니라 의미 평가 신호의 부재다. 원인은 두 가지로 추정된다. 첫째, 본 실험에서 사용한 자산 카탈로그(Polyhaven CC0 + Kenney CC0 mesh)가 living_room 의미 페어(예: TV-소파 거리, 러그-가구 정렬)를 정의한 canonical 휴리스틱과 정합하지 않아, LLM이 의미적으로 배치하더라도 점수로 보상되지 않는 점(random_physics가 0.455로 오히려 높게 나온 것도 같은 정합 이슈의 방증). 둘째, living_room은 세 시나리오 중 가장 큰 방(3.5×3.5m)이나 배치 객체 수는 오히려 적어(placement=5), 의미 페어 후보 자체가 희소한 점이다. 후속 작업에서 (1) 시나리오별 canonical pair 휴리스틱 일반화, (2) 자산 카탈로그-시나리오 정합 보강, (3) 의미 페어 사전 확장을 통해 해소할 예정이다.
```
※ 주의: "8개 좁은 공간" 역서술 삭제(실제 최대 룸 3.5×3.5m·placement=5). FBX 피벗 요인은 삭제(안착 100% 확인으로 무의미).

---

### P-36 · [변경표 #32] §6.2 L7 · 표 10 본문 참조 콜아웃 추가 · 기계교정

▼ 찾기 (L7 문단 내 한 문장 — P-34(g) 적용 후 기준. 콜아웃만 추가)
```
본 연구의 측정(bonsai 292 photos)에서 SfM 전체 1시간 46분 중 exhaustive_matcher가 1시간 28분(83.6%)을 차지하여 30K Brush 학습 1시간 13분을 상회했다.
```
▼ 바꾸기
```
본 연구의 측정(bonsai 292 photos)에서 SfM 전체 1시간 46분 중 exhaustive_matcher가 1시간 28분(83.6%)을 차지하여 30K Brush 학습 1시간 13분을 상회했다(단계별 세부는 표 10).
```
※ 주의: 같은 L7 문단 뒷부분 "학습 우위 비율(SfM 41% : Brush 59%)로 정상화 가능함을 입증하였다."는 v18에서 "학습 우위(SfM 41% : Brush 59%)로 역전됨을 입증하였다."로 다듬었다. 아래 P-36b도 함께 적용 권장.

**(P-36b, 권장) L7 문단 마무리 서술 다듬기**
▼ 찾기
```
이로써 macOS 단일 파이프라인의 속도 병목이 학습이 아닌 SfM 전처리였음을 확인하고, 학습 우위 비율(SfM 41% : Brush 59%)로 정상화 가능함을 입증하였다.
```
▼ 바꾸기
```
이로써 macOS 단일 파이프라인의 속도 병목이 학습이 아닌 SfM 전처리였음을 확인하고, 학습 우위(SfM 41% : Brush 59%)로 역전됨을 입증하였다.
```

---

### P-37 · [변경표 #21] §6.2 · 대안 경로 품질 희생 서술 · 리뷰중대

▼ 찾기 (표 10 뒤 "추가 대안 경로 …" 문단 다음 문단 첫 문장)
```
다만 이들 대안은 baseline 대비 PSNR 3~6dB 열위, macOS Metal/MPS 실행 가능성 불확실(상류 대부분 CUDA 전제), 라이선스·유지 상태의 제약을 수반한다.
```
▼ 바꾸기
```
다만 이들 대안은 baseline 대비 품질 희생(PSNR 열위) 가능성, macOS Metal/MPS 실행 가능성 불확실(상류 대부분 CUDA 전제), 라이선스·유지 상태의 제약을 수반한다.
```

---

### P-38 · [변경표 #35] 참고문헌 헤딩 국문화 · 표제

▼ 찾기 (참고문헌 목록 위 헤딩 — 본문에 단독으로 "References"로 되어 있다)
```
References
```
▼ 바꾸기
```
참고문헌
```
※ 주의: 본문 인용 `[n]` 번호식·참고문헌 배열은 **불변**([44] 결번 포함 유지). 차례의 "참고문헌" 항목은 이미 국문이므로 손대지 않는다. "References" 헤딩(참고문헌 [1] Kerbl … 바로 위)만 국문으로 바꾼다.

---

### P-39 · [변경표 #36] ABSTRACT(영문 초록) · 실험 문장 재서술 · 실험데이터+리뷰중대

▼ 찾기 (ABSTRACT 본문 중간 부분)
```
In three room types the tool trimmed hands-on time by roughly four-fifths. A Phase 2 sweep (3 scenarios × 3 conditions × N=5 × mock and openai providers, 90 trials in total) shows that switching from a mock provider to a real LLM (gpt-4o-mini, $0.0094 USD total spend) raises Floor Adhesion from 14.29% to 42.86% (×3.0~3.4) on bedroom and office, cuts collision counts by ~28~60% (36~50 → 20~30 per scene), and incurs only +4~12% wall-clock overhead (30~103 ms/trial). Replacing the LLM with uniform-random placement under the same physics pass collapses Semantic Proximity by ~75~80% (e.g., 0.79 → 0.19 for bedroom, 0.67 → 0.17 for office), confirming quantitatively that the two layers cannot substitute for each other. We additionally demonstrate a CUDA-free single-machine reconstruction path on macOS Apple Silicon: Brush (Rust+wgpu Metal, Apache-2.0) reaches 32.21 dB PSNR on Mip-NeRF360 bonsai at 30K iterations (within 0.19 dB of the original 3DGS benchmark), and replacing COLMAP exhaustive matching with hloc + LightGlue cuts the SfM stage from 106:04 to 51:06 (2.07×), reducing total wall-clock by 1.44× while keeping PSNR within ±0.5 dB.
```
▼ 바꾸기
```
In three room types the tool trimmed hands-on time by roughly four-fifths. In a Phase 2 sweep (3 scenarios × 3 conditions × N=5 × mock and openai providers, 90 trials in total; re-measured in 2026-07 with a corrected metric harness), the raycast-based ground-snap pass drives Floor Adhesion to essentially 100% across both providers and all scenarios, showing that the physics pass reliably guarantees floor grounding. The discriminating axis is therefore not adhesion but Semantic Proximity: LLM-driven placement (full/llm_only) scores 0.68~0.77 on bedroom and office, whereas uniform-random placement (random_physics) drops to 0.52~0.58 on the same two scenarios, confirming quantitatively that semantic reasoning cannot be substituted by physics alone. The living room, however, stays near 0.09 Semantic Proximity because its asset catalog does not match the canonical furniture pairs, revealing that securing a semantic signal—not floor grounding—is the real bottleneck for that scenario. We additionally demonstrate a CUDA-free single-machine reconstruction path on macOS Apple Silicon: Brush (Rust+wgpu Metal, Apache-2.0) reaches 32.21 dB PSNR on Mip-NeRF360 bonsai at 30K iterations (0.23 dB above the original 3DGS bonsai 30K per-scene benchmark of 31.98 dB [Kerbl et al.]), and replacing COLMAP exhaustive matching with hloc + LightGlue cuts the SfM stage from 106:04 to 51:06 (2.07×), reducing total wall-clock by 1.44× while keeping PSNR within ±0.5 dB.
```
※ 주의: 마지막 문장("Relative to the Kong 외 [20] four-fold taxonomy …")은 불변 — 그대로 둔다.

---

# 검증 (RESULT)

## 1. 변경 테이블 행# ↔ 패치 블록# 전건 매핑

| 변경표 행# | 구분 | 위치(§) | 패치 블록# | 유형 |
|---|---|---|---|---|
| #1 | 실험데이터 | 국문초록 | P-01 | 문단 교체 |
| #2 | 리뷰중대 | 국문초록 | P-02 | 문장 교체 |
| #3 | 리뷰중대 | §4.2.1 | P-12 | 문장 교체 |
| #4 | 실험데이터 | §4.3.1 | P-16 | 문단 교체 |
| #5 | 실험데이터 | §4.3.1 | P-18 | 문단 교체 |
| #6 | 실험데이터 | §4.3.2 | P-19 | 문단 교체 |
| #7 | 실험데이터 | §3.4 | P-10 | 문장 교체 |
| #8 | 실험데이터 | §4.4 | P-21 | 문단 교체 + 표 삭제 |
| #9 | 실험데이터 | §4.4.1 | P-22 | 문단 교체 |
| #10 | 실험데이터 | §4.4.1 | P-23 | 표 교체 |
| #11 | 실험데이터 | §4.4.1 | P-24 | 표 삭제/신설 |
| #12 | 실험데이터 | §4.4.1 | P-25 | 문단 교체 |
| #13 | 실험데이터 | §4.5 | P-26 | 표 교체 |
| #14 | 실험데이터 | §4.5 | P-27 | 문단 교체 |
| #15 | 실험데이터 | §4.5 | P-28 | 문단 교체 |
| #16 | 실험데이터 | §6.1 | P-33 | 문장 교체 |
| #17 | 실험데이터+리뷰중대 | §6.2 | P-35 | 문단 교체 |
| #18 | 실험데이터 | 그림9 캡션 | P-17 | 문장 교체(2곳) |
| #19 | 실험데이터(파생) | §1.2 | P-05 | 문장 교체 |
| #20 | 리뷰중대 | §5.1.4 | P-32 | 문장 교체 |
| #21 | 리뷰중대 | §6.2 | P-37 | 문장 교체 |
| #22 | 리뷰권장 | §2.1 | P-06 | 문장 교체 |
| #23 | 리뷰권장 | §1.1 | P-04 | 문장 교체 |
| #24 | 리뷰권장 | §5.1.2 | P-30 | 문장 교체 |
| #25 | 리뷰권장 | §5.1.3 | P-31 | 문단 교체 |
| #26 | 리뷰권장 | §4.2.2 | P-13 | 문장 교체 |
| #27 | 리뷰권장 | §4.2.2 | P-14 | 문장 교체 |
| #28 | 리뷰권장 | §5.1.1 | P-29 | 문장 교체 |
| #29 | 리뷰권장 | §6.2 | P-34 | 다중 블록(순번) |
| #30 | 기계교정 | §3.1 | P-08 | 문장 추가 |
| #31 | 기계교정 | §4.1 | P-11 | 문장 삽입 |
| #32 | 기계교정 | §6.2 | P-36 | 문장 추가 |
| #33 | 기계교정 | 표 목차 | P-03 | 목차 항목 교체 |
| #34 | 기계교정 | §5.1.1 | **(무변경)** | 2차본에 이미 "표 5a·5b"로 정합 — 패치 불필요 |
| #35 | 표제 | 참고문헌 헤딩 | P-38 | 헤딩 교체 |
| #36 | 실험데이터 | ABSTRACT | P-39 | 문단 교체 |
| #37 | 리뷰권장 | 표 6 | P-20 | 표 셀 수정 |
| #38 | 기계교정 | §4.3.1 | P-15 | 문장 교체 |
| #39 | 실험데이터-후속 | §4.4.1 | P-24 | 표 신설 |
| #40 | 실험데이터-후속 | §4.4.1 | P-25 | 문단 추가 |
| #41 | 실험데이터-후속 | §4.4.1 | P-23/P-24 | note 재서술 |
| #42 | 기계교정-후속 | 표 목차 | P-03 | 목차 항목 재추가 |
| D1 | 2차본 정합 | §3.3 그림4 캡션 | P-09 | 문장 교체 |
| D2 | 2차본 정합 | §2.4.1 표1 캡션 | P-07 | 문장 교체 |

- **매핑 요약:** 변경 테이블 42행 중 41행이 패치 블록으로 매핑(#34는 2차본 기정합으로 무변경), + D1·D2 2건 = **총 43개 실적용 항목, 누락 0.**
- 다중-매핑: P-03(#33·#42), P-23(#10·#41), P-24(#11·#39·#41), P-25(#12·#40). 이는 변경 테이블 §C의 "1행 ↔ 다중 블록/인접 블록 묶음" 구조와 정합한다(§C에서 diff 50블록을 38+4행이 포괄).

## 2. 찾기 블록 유일성 검증 (2차본 추출본 grep -c, 전건 1회)

> 각 찾기 블록의 **한글 산문 대표 substring**(추출본이 글자단위로 보존한 부분)으로 `grep -c` 카운트. 밑줄 식별자는 추출본 표기(밑줄 없음)로 카운트했으며, 실제 HWP에서는 밑줄 포함으로 검색해야 함(§0-5).

(아래 표는 검증 절 하단 "grep 실행 로그" 참조 — 전건 count=1 확인.)

## 3. 누락 0 확인

- 변경 테이블 A(38) + A-후속(4) = 42행 전건이 매핑표에 등장(#34는 무변경 사유 명시).
- 2차본 고유 차이 D1·D2 패치 추가 완료(P-09·P-07).
- 삭제 대상(구 표 7 / 구 표 7b)과 신설(신 표 7b)·컬럼 삭제(표 8 충돌 횟수)를 명시적으로 지시.
- 보류 항목(변경표 §B: 그림 재배열·[44] 재번호·번호 전면 재배열 등)은 spec상 미변경 대상이므로 패치 블록 없음(의도적 제외).