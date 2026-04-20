# 신규 관련 논문 후보 (2026-04-18)

## 요약 라인
- 조사 주제 7개 (A~G)
- 후보 총 10건 (HIGH: 4 / MEDIUM: 4 / LOW: 2)

---

## 주제 A — LLM 기반 실내 레이아웃

### A-1: Sun et al. 2025 — LayoutVLM: Differentiable Optimization of 3D Layout via Vision-Language Models
- **서지**: F.-Y. Sun, W. Liu, and J. Wu, "LayoutVLM: Differentiable Optimization of 3D Layout via Vision-Language Models," in *Proc. IEEE/CVF Conf. Computer Vision and Pattern Recognition (CVPR)*, 2025, pp. 29469–29478.
- **arXiv/DOI**: arXiv:2412.02193
- **요약**: VLM을 활용하여 초기 3D 레이아웃을 생성하고, 물리적 타당성(충돌·접지)과 공간 관계를 동시에 미분 가능 최적화로 정제하는 프레임워크. LLM 단독 배치의 물리적 부정합 문제를 VLM+최적화 루프로 해결한 점이 핵심.
- **Paper01 관련성**: Unity-SplatForge의 LLM 배치 → Raycast/OverlapBox 물리 검증 파이프라인과 직접 대응. LayoutVLM의 미분 가능 최적화 접근은 Section 2 관련연구에서 LLM 기반 배치의 한계와 대안으로 논의 가능. Safety Zone Violation 지표와의 비교 관점 제공.
- **배치 권장**: Section 2.X (관련연구 — LLM/VLM 기반 레이아웃)
- **우선순위**: HIGH
- **근거 URL**: https://arxiv.org/abs/2412.02193 / https://github.com/sunfanyunn/LayoutVLM

### A-2: Yang & Lu 2024 — LLplace: The 3D Indoor Scene Layout Generation and Editing via Large Language Model
- **서지**: Y. Yang and H. Lu, "LLplace: The 3D Indoor Scene Layout Generation and Editing via Large Language Model," arXiv preprint, 2024.
- **arXiv/DOI**: arXiv:2406.03866
- **요약**: Llama3를 파인튜닝하여 공간 관계 사전지식이나 in-context 예시 없이 대화형으로 실내 레이아웃 생성·편집. 3D-Front 기반 대화 데이터셋 구축으로 오브젝트 추가/제거 지원.
- **Paper01 관련성**: Unity-SplatForge가 GPT-4/Claude를 사용하는 것과 달리 오픈소스 LLM 파인튜닝 접근. Section 2 관련연구에서 LLM 배치 방법론 계보(LayoutGPT→Holodeck→LLplace) 정리에 활용. 대화형 편집은 향후 과제 논의 가능.
- **배치 권장**: Section 2.X (관련연구)
- **우선순위**: MEDIUM
- **근거 URL**: https://arxiv.org/abs/2406.03866

---

## 주제 B — Text → 3D Scene (씬 단위 자동 생성)

### B-1: Öcal et al. 2024 — SceneTeller: Language-to-3D Scene Generation
- **서지**: B. M. Öcal, M. Tatarchenko, S. Karaoğlu, and T. Gevers, "SceneTeller: Language-to-3D Scene Generation," in *Proc. European Conf. Computer Vision (ECCV)*, 2024.
- **arXiv/DOI**: arXiv:2407.20727
- **요약**: 자연어 프롬프트로 오브젝트 배치를 지정하고, in-context learning + CAD 모델 검색 + 3DGS 기반 스타일화를 결합한 턴키 파이프라인. 3DGS를 씬 단위 생성에 직접 활용한 점이 특징.
- **Paper01 관련성**: Unity-SplatForge와 마찬가지로 LLM(텍스트) → 3DGS 씬 구성 파이프라인. SceneTeller는 CAD+3DGS 스타일화, SplatForge는 ProBuilder 메시+3DGS 가구로 접근이 다름. Section 2 관련연구에서 직접 비교 대상.
- **배치 권장**: Section 2.X (관련연구 — Text-to-3D Scene)
- **우선순위**: HIGH
- **근거 URL**: https://arxiv.org/abs/2407.20727 / https://sceneteller.github.io/

### B-2: Li et al. 2024 — DreamScene: 3D Gaussian-based Text-to-3D Scene Generation via Formation Pattern Sampling
- **서지**: H. Li et al., "DreamScene: 3D Gaussian-based Text-to-3D Scene Generation via Formation Pattern Sampling," in *Proc. European Conf. Computer Vision (ECCV)*, 2024.
- **arXiv/DOI**: arXiv:2404.03575
- **요약**: GPT-4 에이전트가 오브젝트 시맨틱/공간 제약을 추론하여 하이브리드 그래프 구성 → 그래프 기반 배치 알고리즘으로 충돌 없는 레이아웃 생성 → Formation Pattern Sampling으로 3DGS 오브젝트 합성. 오브젝트 재배치·외형 수정·4D 모션 편집 지원.
- **Paper01 관련성**: GPT-4 기반 씬 플래닝 + 충돌 방지 레이아웃이 SplatForge의 LLM 배치 + Raycast 검증과 구조적으로 유사. Section 2에서 "LLM 기반 씬 플래닝 → 3DGS 렌더링" 계보의 핵심 참조. Grounding Success Rate 지표와 비교 논의 가능.
- **배치 권장**: Section 2.X (관련연구 — Text-to-3D Scene)
- **우선순위**: HIGH
- **근거 URL**: https://arxiv.org/abs/2404.03575 / https://dreamscene-project.github.io/

---

## 주제 C — 3DGS + 게임 엔진 통합

### C-1: Baltsavias et al. 2025 — Beyond Digital Twins: 3D Gaussian Splatting, Game Engines and Crossmedia Cultural Heritage Representations
- **서지**: E. Baltsavias et al., "Beyond Digital Twins: 3D Gaussian Splatting, Game Engines and Crossmedia Cultural Heritage Representations," in *Proc. ACM SIGGRAPH Talks*, 2025.
- **arXiv/DOI**: DOI:10.1145/3721239.3734094
- **요약**: 문화유산 디지털화에서 3DGS를 게임 엔진(Unity/Unreal)과 통합하는 실무 사례. 3DGS 환경 배경과 전통 메시 캐릭터를 결합하는 하이브리드 렌더링 접근 논의.
- **Paper01 관련성**: SplatForge의 "ProBuilder 메시(벽/바닥) + 3DGS(가구)" 하이브리드 방식과 개념적으로 유사한 게임 엔진 통합 사례. Section 2 관련연구의 게임 엔진+3DGS 통합 서브섹션에 배치 가능. 다만 실내 씬 자동 구성이 아닌 문화유산 도메인이라 직접 관련성은 중간.
- **배치 권장**: Section 2.X (관련연구 — 게임 엔진 통합)
- **우선순위**: MEDIUM
- **근거 URL**: https://dl.acm.org/doi/10.1145/3721239.3734094

---

## 주제 D — 3DGS 물리 상호작용

### D-1: Zhao et al. 2024 — PhysSplat: Efficient Physics Simulation for 3D Scenes via MLLM-Guided Gaussian Splatting
- **서지**: Z. Zhao et al., "PhysSplat: Efficient Physics Simulation for 3D Scenes via MLLM-Guided Gaussian Splatting," in *Proc. IEEE/CVF Int. Conf. Computer Vision (ICCV)*, 2025.
- **arXiv/DOI**: arXiv:2411.12789
- **요약**: MLLM(다중모달 LLM)이 장면 이미지를 보고 물리 속성(질량, 마찰 등)을 제로샷 추론 → MPM 기반 시뮬레이션. 단일 GPU에서 2분 내 사실적 물리 시뮬레이션 달성. 기존 PhysGaussian 대비 자동화된 물리 속성 추정이 핵심 차별점.
- **Paper01 관련성**: SplatForge가 Raycast/OverlapBox로 물리 검증하는 것과 보완적. PhysSplat의 MLLM 기반 물리 속성 추론은 LLM이 배치뿐 아니라 물리 시뮬레이션까지 확장 가능함을 시사. Section 5.2 향후 과제에서 "3DGS 오브젝트에 물리 속성 부여" 확장 방향으로 인용 적합.
- **배치 권장**: Section 5.2 (향후 과제)
- **우선순위**: HIGH
- **근거 URL**: https://arxiv.org/abs/2411.12789

### D-2: Borycki et al. 2024 — GASP: Gaussian Splatting for Physics-Based Simulations
- **서지**: P. Borycki, W. Smolak, J. Waczyńska, M. Mazur, S. Tadeja, and P. Spurek, "GASP: Gaussian Splatting for Physics-Based Simulations," *Computer Vision and Image Understanding*, 2025.
- **arXiv/DOI**: arXiv:2409.05819
- **요약**: 평면 가우시안 분포를 삼각형으로 파라미터화하여 물리 엔진과 3DGS를 직접 연결. 별도 메싱 없이 3D 포인트 클라우드 처리로 물리 시뮬레이션 수행. 범용 물리 엔진 호환성이 장점.
- **Paper01 관련성**: Unity 물리 엔진과 3DGS의 직접 통합이라는 점에서 SplatForge의 Raycast 기반 검증의 확장 가능성 시사. Section 5.2 향후 과제에서 "3DGS 오브젝트의 런타임 물리 반응" 논의 시 참조.
- **배치 권장**: Section 5.2 (향후 과제)
- **우선순위**: MEDIUM
- **근거 URL**: https://arxiv.org/abs/2409.05819 / https://waczjoan.github.io/GASP/

---

## 주제 E — 3DGS 편집

### E-1: Vachha & Haque 2024 — Instruct-GS2GS: Editing 3D Gaussian Splatting Scenes with Instructions
- **서지**: C. Vachha and A. Haque, "Instruct-GS2GS: Editing 3D Gaussian Splatting Scenes with Instructions," 2024.
- **arXiv/DOI**: (프로젝트 페이지: instruct-gs2gs.github.io)
- **요약**: InstructPix2Pix 디퓨전 모델로 학습 뷰 이미지를 반복 편집하면서 3DGS 씬을 최적화. GaussianEditor 대비 텍스트 인스트럭션만으로 글로벌 스타일 편집 가능, Instruct-NeRF2NeRF 대비 4배 빠른 학습(13분).
- **Paper01 관련성**: SplatForge에서 생성된 3DGS 가구의 사후 스타일 편집 가능성. Section 5.2 향후 과제에서 "사용자 텍스트 지시로 가구 외형 수정" 확장 논의에 적합.
- **배치 권장**: Section 5.2 (향후 과제)
- **우선순위**: LOW
- **근거 URL**: https://instruct-gs2gs.github.io/ / https://github.com/cvachha/instruct-gs2gs

---

## 주제 F — VLM 기반 레이아웃 피드백 루프

주제 A-1의 LayoutVLM (Sun et al. 2025)이 이 주제의 핵심 논문을 겸함. VLM이 렌더링된 이미지를 평가하고 미분 가능 최적화로 레이아웃을 반복 정제하는 구조가 피드백 루프 그 자체.

추가 독립 후보: 해당 주제 적합 후보 미발견 (LayoutVLM 외에 VLM 피드백 루프를 실내 씬에 명시적으로 적용한 2024-10 이후 논문은 검색 범위 내 확인 불가).

---

## 주제 G — Furniture Placement / Indoor Scene 평가 벤치마크

### G-1: Zhang et al. 2024 — FurniScene: A Large-scale 3D Room Dataset with Intricate Furnishing Scenes
- **서지**: Z. Zhang et al., "FurniScene: A Large-scale 3D Room Dataset with Intricate Furnishing Scenes," arXiv preprint, 2024.
- **arXiv/DOI**: arXiv:2401.03470
- **요약**: 11,698개 방, 39,691개 고유 가구 CAD 모델(89종), 소형 장식품까지 포함한 대규모 실내 씬 데이터셋. Two-Stage Diffusion Scene Model(TSDSM) 제안 및 실내 씬 생성 벤치마크 수립. 기존 3D-Front 대비 세밀한 가구 배치 포함.
- **Paper01 관련성**: SplatForge 평가 시 벤치마크 데이터셋으로 활용 가능성. Semantic Proximity 지표 검증에 FurniScene의 전문가 배치 데이터를 ground truth로 참조할 수 있음. Section 2 관련연구에서 기존 벤치마크/데이터셋 서브섹션에 배치.
- **배치 권장**: Section 2.X (관련연구 — 데이터셋/벤치마크)
- **우선순위**: MEDIUM
- **근거 URL**: https://arxiv.org/abs/2401.03470

---

## 검색 한계 및 참고사항
- FurniScene(2024-01)은 기준 기간(2024-10~)보다 약간 이르나, 벤치마크 참조 가치가 높아 포함.
- Instruct-GS2GS(2024)도 정확한 학회 게재 날짜가 불명확하나 2024년 공개로 기간 내 포함.
- OptiScene(arXiv:2506.07570)과 IL3D(arXiv:2510.12095)는 2025년 중후반 논문으로 출판 확정 여부 불확실하여 제외. 추후 재확인 권장.
- DreamScene은 ECCV 2024(2024-10 학회) 발표로 기간 경계에 해당하나 포함.
