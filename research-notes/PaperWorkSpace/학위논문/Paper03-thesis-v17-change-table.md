# Paper03 v16 → v17 변경표

기준: `data/thesis-output/v16-work/Paper03-thesis-v16.md` → `data/thesis-output/v17-work/Paper03-thesis-v17.md`
통합 잡: v17-integrate (Mary 월드모델 키스톤 + Rosalind 실사용 사례 반영)
검증: `diff v16 v17` = 아래 3건 외 변경 0. 신규 인용 [60]~[63] 본문↔참고문헌 1:1.

---

## 변경 요약

| # | 위치(v16) | 유형 | 출처 보고서 | 내용 |
| --- | --- | --- | --- | --- |
| 1 | §1.1 L183 (문단 내부) | 삽입(2문장) | Mary v17-m-worldmodel-flow.md B-2 | 월드모델→3DGS 채택 키스톤. DreamGaussian 문장 뒤·"다른 하나는…" 앞 |
| 2 | §1.1 L185 바로 앞 | 삽입(신규 단락) | Rosalind v17-r-usecases.md B(권장안) | 3DGS 실사용 확산 단락. 인용 [60][61][56][62][63] |
| 3 | 참고문헌 [59] 뒤 | 추가(4건) | Rosalind v17-r-usecases.md C | [60] Niantic, [61] ISPRS 가상박물관, [62] Framestore Superman, [63] NVIDIA NuRec |

그 외 본문·목차·각주·기존 참고문헌([1]~[59]) **무변경**.

---

## 상세

### 변경 1 — 월드모델 키스톤 (§1.1 L183 문단 내부)

**BEFORE** (해당 접합부):
> …DreamGaussian[7] 같은 후속 연구는 텍스트만으로 3DGS 에셋을 생성하는 단계까지 와 있다. 다른 하나는 앞서 언급한 LLM의 공간 추론 능력이다.…

**AFTER** (삽입분):
> …단계까지 와 있다. **주목할 점은, 앞서의 장면 생성형 월드 모델들이 그 산출을 담는 표현으로 바로 이 3DGS를 채택하고 있다는 것이다 — Lyra[21, 22]는 생성한 씬을 3DGS 표현으로 고정해 내놓고, HunyuanWorld[27]는 3DGS를 메시의 대안 표현으로 공식 지원한다. 즉 3DGS는 novel view synthesis의 표준을 넘어, 월드 모델이 공간을 내놓는 공통 출력 표현으로 자리잡는 중이다.** 다른 하나는…

- 인용: 전부 기존 등재 — [21]/[22] Lyra, [27] HunyuanWorld. 신규 인용 0.
- 보고서 B-2 문안과 문자 동일(AC3).

### 변경 2 — 실사용 사례 단락 (§1.1, 표준화 단락 앞 신규 단락)

삽입 단락(권장안, 약 400자):
> 이러한 기술적 부상은 이미 연구실을 벗어나 실사용 현장으로 확산되고 있다. Niantic은 온디바이스 3DGS 스캔을 지원하는 모바일 앱 Scaniverse를 대중에 배포하고 사용자가 만든 수만 개의 장면을 VR로 탐색하게 하였으며[60], 문화·유산 분야에서는 실제 전시 콘텐츠로 3DGS 가상 박물관을 구축해 기존 파노라마 방식보다 나은 몰입감을 보고한 연구가 나왔고[61], 국내에서도 조형물을 3DGS로 디지털화하는 시도가 이어졌다[56]. 영화 제작에서는 Framestore가 《Superman》(2025)의 특정 장면을 4D Gaussian Splatting으로 완성하였고[62], 산업 현장에서도 NVIDIA가 실센서 데이터를 3DGS로 재구성해 로보틱스·자율주행 시뮬레이션에 투입하는 파이프라인을 공개하는 등[63] 적용 범위가 빠르게 넓어지고 있다.

- 삽입 위치: 기존 L185 "3DGS의 이러한 부상은…산업 표준화의 국면으로…" 문단 바로 앞.
- 서사: L183(연구적 부상) → [신규 실사용] → L185(표준화).
- 게임 상용 채택 서술 없음("엔진 통합" 언급도 없음) — Rosalind 정직성 주석 준수.

### 변경 3 — 신규 참고문헌 [60]~[63] (참고문헌 [59] 뒤)

```
[60] Niantic, "What the Gaussian Splat? Niantic Scaniverse 4 invites everyone to share their world in 3D," Niantic Labs News, Aug. 26, 2024. …
[61] Kwon, O. and Yu, J., "Realistic and Interactive Virtual Museum Representation Using 3D Gaussian Splatting," ISPRS Annals …, vol. X-M-2-2025, pp. 185-192, 2025. …
[62] befores & afters, "Framestore showcases the 4D Gaussian Splatting used for 'Superman'," Feb. 13, 2026. …
[63] NVIDIA, "Omniverse NuRec — 3D Gaussian Splatting Libraries for Simulation," NVIDIA Developer, 2025. …
```

- 본문 인용 대응: [60]→Niantic, [61]→ISPRS 가상박물관, [62]→Framestore, [63]→NVIDIA. [56]은 기존 등재 문헌 재인용.
- 기존 번호 [1]~[59] 무변경.
