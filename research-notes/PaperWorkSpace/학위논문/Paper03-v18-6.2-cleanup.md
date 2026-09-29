# 6.2절 정리 (2026-07-07) — 남은 작업은 딱 2개입니다

> 배경: 패치 블록 문서의 P-34(d)는 기존 batchmode 문단에 "(L4)" 접두만 붙이는 패치였는데, 편집 과정에서 **그 문단 자체가 삭제**되어 접두를 붙일 대상이 사라진 상태입니다(문단의 후속 문단인 'HDRP·URP 양 경로 검증'은 살아 있음). 그래서 패치 문서에서 L4 삽입 블록을 찾을 수 없었던 것이 맞습니다 — 문서 구성의 한계였습니다. 아래로 완결됩니다.

## 현재 6.2절 상태 (07-07 12:49본 기준 — 이미 정상인 것)
✔ "일곱 가지 한계가 남아 있다. 이하 L1~L7로 지시한다." ✔ (L1) ✔ 새 (L2)(교정 재실측…) ✔ (L3) ✔ HDRP·URP 양 경로 검증 문단 ✔ (L5) ✔ (L6) ✔ (L7) ✔ 표 10 ✔ 마무리 2문단
✘ 문제 2개: ① 옛 (L2) 문단이 새 (L2) 앞에 중복 잔존 ② (L4) 본문단 소실

---

## 작업 ① — 옛 (L2) 문단 삭제
한글 Ctrl+F로 아래를 검색해 그 문단 **전체를 삭제**합니다(첫째·둘째·셋째 요인 + "…해소할 예정이다."까지). 바로 뒤의 새 (L2) 문단("본 측정에서 living_room 시나리오는 교정 재실측 기준…")은 그대로 둡니다.
```
(L2) 본 측정에서 living_room 시나리오의 full pipeline 모드는 Floor Adhesion 0%
```

## 작업 ② — (L4) 문단 삽입
한글 Ctrl+F로 아래를 검색해 그 문단(HDRP·URP 양 경로 검증) **바로 앞**에,
```
HDRP·URP 양 경로 검증 (2026-05)
```
아래 문단을 통째로 붙여넣습니다(별도 문단으로):
```
(L4) 본 측정 자동화 파이프라인이 HDRP CustomPassVolume + GaussianSplatHDRPPass 경로를 batchmode 환경에서 종결시키지 못한 통합 한계도 별도로 기록한다. Unity 6000.3.6f1 + UnityGaussianSplatting 1.1.1 + HDRP 17.x 조합에서 GS_ENABLE_HDRP scripting define 활성화, GaussianSplatRenderer 셰이더·ComputeShader 인스펙터 기본값 코드 바인딩, GaussianSplatHDRPPass 등록(internal 클래스를 Type.GetType 으로 동적 로드), EnsureSorterAndRegister() 강제 호출까지 모든 사전 조건을 충족시킨 상태에서도 단일 Camera.Render() 호출로 캡처한 결과가 cleared 색상 버퍼(전 픽셀 (0,0,0))로 반환되었다. 동일 파이프라인은 mesh 자산만 포함했을 때는 정상 PBR 렌더링을 산출하므로, 본 한계는 batchmode 단일 프레임 렌더 경로가 HDRP custom pass 의 RT 합성을 종결하지 못하는 데서 비롯된 것으로 분석된다(GameView 다중 프레임 렌더 루프에서는 동일 코드가 정상 동작함을 3.3 인용 사례가 시사한다). 후속 작업에서는 (1) manual GameView 캡처(LaunchAgent 기반 비-batchmode 자동화), (2) URP 경로(GaussianSplatURPFeature)로의 이행, (3) batchmode EditorApplication.Step() 다중 프레임 priming 의 세 갈래로 해소를 시도할 예정이다.
```

---

## 완료 후 6.2절 최종 순서 (눈 확인용 체크리스트)
1. 일곱 가지 한계가 남아 있다. 이하 L1~L7로 지시한다.
2. (L1) LLM 공간 추론 — L자형 방…
3. (L2) 본 측정에서 living_room 시나리오는 **교정 재실측 기준**… ← 하나만 있어야 함
4. (L3) 3DGS 객체의 정적 특성…
5. (L4) 본 측정 자동화 파이프라인이 HDRP… ← 이번에 삽입
6. HDRP·URP 양 경로 검증 (2026-05)… (L4의 보충 문단, 라벨 없음)
7. (L5) 확장성 문제…
8. (L6) 3DGS 학습은 여전히 CUDA…
9. (L7) COLMAP no-CUDA 단계 단축…
10. 표 10 (변경 없음 — 그대로)
11. "추가 대안 경로는 세 갈래로…" / "다만 이들 대안은…" (변경 없음 — 그대로)

※ 이 문서가 어제 보정 문서(fixups)의 F-2·F-3을 **대체**합니다. F-1(그림목차 그림9 캡션)·F-4(3.1절 구절 복원)·F-5(3.3절 Brush 괄호)·F-6(ABSTRACT 'Kong 외')은 여전히 유효합니다.