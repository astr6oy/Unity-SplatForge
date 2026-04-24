## 2.4.5 macOS 생태계의 CUDA-free 3DGS 도구

3DGS 관련 학습·렌더 도구 대부분은 원 3DGS (Kerbl et al., 2023) 구현이 CUDA·C++ 래스터라이저를 전제로 한다는 계보적 이유로 NVIDIA GPU 환경을 가정한다.
본 연구는 개발·검증 환경이 macOS Apple Silicon이라는 제약에서 출발하므로, CUDA 의존 없이 **학습 → PLY → Unity 임포트 → Metal 런타임** 전 구간을 완결할 수 있는 도구 조합을 조사한다.

조사 범위는 2026-04 기준 활발히 유지되는 공개 프로젝트 5건이다.
splat-apple (Ghif, 2026; MLX/MPS 이중 경로), Brush (Brussee, 2026; Rust+wgpu 크로스플랫폼), OpenSplat (Tofy, 2025; libtorch MPS), gsplat-mps (Iffyloop, 2024; nerfstudio/gsplat 0.1.3 포크), 그리고 상류 nerfstudio/gsplat (Nerfstudio, 2026; CUDA 전용)이 해당한다.
표 2는 각 도구의 라이선스·유지 상태·Apple Silicon 성능 수치를 정리한다.

**표 2. macOS CUDA-free 3DGS 학습 도구 비교 (2026-04 기준)**

| 도구 | 백엔드 | 라이선스 | 최근 업데이트 | Stars | Apple Silicon 성능 |
|------|-------|---------|--------------|-------|------------------|
| Brush (Brussee, 2026) | Rust+wgpu (Burn) | Apache-2.0 | 2026-04-19 | 3961 | 공식 벤치 부재 (본 연구 PoC에서 실측) |
| splat-apple (Ghif, 2026) | MLX C++ Metal / PyTorch MPS | 라이선스 부재 | 2026-02-19 | 10 | M4 Fern MLX 38.5 it/s, PyTorch GCD 10.6 it/s |
| OpenSplat (Tofy, 2025) | libtorch MPS (C++) | AGPL-3.0 | 2025-12-26 | 1949 | cmake `-DGPU_RUNTIME=MPS` 공식 지원 |
| gsplat-mps (Iffyloop, 2024) | gsplat 0.1.3 포크 + MPS | AGPL-3.0 | 2024-07-06 | 37 | 저자 "not thoroughly tested" 명시 |
| nerfstudio/gsplat (Nerfstudio, 2026) | CUDA 전용 | Apache-2.0 | 2026-04-09 | 4879 | MPS 미지원 (Issue #163 업스트림 제안만 존재) |

본 연구는 학습 백엔드로 **Brush**를, Unity 임포트·렌더 단계로 aras-p (Pranckevičius, 2025)의 UnityGaussianSplatting을 각각 채택한다.
Brush 선정의 근거는 세 가지이다.

첫째, **라이선스 적합성**이다.
Apache-2.0으로 연구·상용 배포에 가장 관용적이며, OpenSplat의 AGPL-3.0 copyleft나 splat-apple의 라이선스 부재 상태를 회피한다.
논문 부록 공개나 후속 상용화 경로 모두에서 법적 불확실성이 최소이다.

둘째, **유지 활발성과 커뮤니티 규모**이다.
2026-04-19 커밋과 stars 3961은 splat-apple(10), gsplat-mps(2024-07 이후 정체)과 대비된다.
wgpu 기반 크로스플랫폼 설계는 향후 윈도우·리눅스 서버 경로로 회귀해야 하는 상황에서도 동일 코드베이스 유지가 가능하다.

셋째, **PLY 호환 경로**이다.
Brush는 원 3DGS 논문 규격의 PLY 포맷(x/y/z, scale_0-2, opacity, rot_0-3, f_dc_0-2, f_rest 속성)을 로드·저장한다.
aras-p 플러그인의 `Tools → Gaussian Splats → Create GaussianSplatAsset` 메뉴가 동일 스키마를 전제하므로 중간 변환 없이 직결된다.

2026-04-22 수행한 E2E PoC에서 Brush 300 iter 학습 → PLY(118 splat, binary_little_endian) → aras-p asset 변환 → Unity PlayMode 렌더까지 6개 AC 전체를 PASS한 바 있다 (Unity-SplatForge 연구팀, 2026).
aras-p Metal 경로의 공개 수치는 M1 Max 6.1M splats에서 21.5ms/46FPS를 기록하여 (Pranckevičius, 2025), 런타임 성능은 이미 실용 수준임이 확인된다.
반면 Brush 측 Apple Silicon 학습 시간 수치는 README에 부재하며, 본 연구의 측정치가 독자적 기여로 남을 여지가 있다.

본 연구의 차별점은 **Unity 생태계와 macOS 네이티브 학습 도구의 연결 경로를 실측으로 확증**한 점에 있다.
기존 Paper01·Paper02가 상정한 Python+Windows+CUDA 2-tier 아키텍처는 2026-04의 도구 성숙도에 따라 macOS 단일 기기 경로로 축약 가능해졌으며, 본 논문은 이 축약의 타당성을 PoC로 입증한다.
