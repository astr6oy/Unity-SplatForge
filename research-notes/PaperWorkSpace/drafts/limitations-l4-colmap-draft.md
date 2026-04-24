<!-- Insert: Paper03 §5.2 한계와 전망 (기존 한계 블록 말미에 L4 문단 추가) -->
<!-- Anchor: 기존 §5.2 "L3 Brush 벤치마크 공개 자료 부족…" 뒤 -->
<!-- Source: synthesis-2026-04-23 §4 L4, findings/3dgs-reconstruction-speed-survey-2026-04-23 -->

### L4. COLMAP no-CUDA 빌드의 Sparse Reconstruction 병목

본 연구의 재구성 파이프라인 중 sparse reconstruction 단계는 COLMAP 4.0.3 homebrew 빌드를 사용한다.
해당 빌드는 `Commit Unknown on Unknown without CUDA`로 표기되어 있으며, SIFT feature 추출과 exhaustive matching 전 구간이 CPU에서 실행된다.
결과적으로 macOS M-계열 하드웨어의 Metal GPU와 ANE는 재구성 단계에서 유휴 상태로 남는다.

이 구성은 소규모 입력에서는 수용 가능하나, 본 PoC의 302장 입력과 같은 **대규모 데이터셋에서는 exhaustive matching이 주요 bottleneck**이 된다.
302장의 exhaustive pairing은 $\binom{302}{2} = 45{,}451$ 페어에 달하며, 측정된 블록당 97초를 근거로 이론 하한만 79분, 실제 완료 시간은 2~6시간 범위로 관측된다.

대안 경로는 세 갈래로 구분된다.
첫째, **hloc** (Sarlin et al., 2019) 기반의 learned feature + vocabulary tree retrieval로 exhaustive pairing의 $O(N^2)$ 비용을 $O(N \log N)$ 수준으로 완화하는 접근이다.
둘째, **DUSt3R** (Wang et al., 2024)와 **MASt3R** (Leroy et al., 2024)의 matching-free dense regression으로 sparse reconstruction 자체를 우회하는 접근이다.
셋째, **InstantSplat** (Fan et al., 2024)과 같이 DUSt3R 초기화와 저 iter 학습을 결합하여 전 구간을 feed-forward 축으로 이동시키는 접근이다.

다만 이들 대안은 baseline 대비 PSNR 3~6dB 열위, macOS Metal/MPS 실행 가능성 불확실(상류 대부분 CUDA 전제), 라이선스·유지 상태의 제약을 수반한다.
현 시점의 실측 검증과 본 연구 파이프라인과의 정합 평가는 §3.1의 속도-품질 trade-off 지형과 일관되게 **본 연구의 범위 밖**으로 두며, hloc·DUSt3R·MASt3R 경로의 macOS 실행 가능성과 302장 기준 동일 데이터셋 실측을 **후속 과제**로 명시한다.
본 연구의 차별점은 해당 bottleneck을 은폐하지 않고 **baseline 축의 정직한 좌표**로 기록하여 feed-forward 축과의 비교 기준점을 제공한 데에 있다.

---

## 신규 레퍼런스 후보 ([NEW:Z] 플레이스홀더)

- `[NEW:L1]` Sarlin et al. *hloc*. CVPR 2019. arXiv:1812.03506. (§3.1 [NEW:D4]와 동일)
- `[NEW:L2]` Wang et al. *DUSt3R*. CVPR 2024. arXiv:2312.14132. (§3.1 [NEW:D1]과 동일)
- `[NEW:L3]` Leroy et al. *MASt3R*. ECCV 2024. arXiv:2406.09756. (§3.1 [NEW:D2]와 동일)
- `[NEW:L4]` Fan et al. *InstantSplat*. arXiv:2403.20309 (2024). (§3.1 [NEW:D3]과 동일)
