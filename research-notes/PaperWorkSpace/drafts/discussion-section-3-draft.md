# 3. 논의 (Discussion)

본 절은 본 연구 파이프라인이 놓인 기술적 좌표를 세 축에서 정리한다.
첫째, 재구성 속도와 품질 사이의 trade-off를 오프라인 baseline과 feed-forward 계열 대비로 명시한다.
둘째, 2024년 이후 부상한 월드 모델과 본 연구의 접근을 대조한다.
셋째, 메시와 3DGS를 혼용하는 하이브리드 표현의 설계적 정당화를 최근 3DGS 물리 통합 연구와 엮어 보강한다.

## 3.1 재구성 속도-품질 trade-off

### 3.1.1 본 파이프라인의 오프라인 특성

본 연구의 재구성 경로는 COLMAP 기반 sparse reconstruction과 30K iteration의 gradient optimization을 조합한 **오프라인 baseline**에 해당한다.
2026-04-23 PoC 측정(302장, 1280×960 입력, macOS M-계열, Brush Rust+wgpu 학습)을 기준으로 feature 추출 약 2분, exhaustive matcher 2~6시간, mapper 1~3시간, 학습 2~4시간이 소요되어 **총 5~13시간 범위**의 처리 시간을 갖는다.
이는 Kerbl et al. (2023)이 제시한 원 3DGS 학습 프로토콜을 충실히 따를 때 나타나는 전형적 특성이다.

302장 입력의 exhaustive pairing은 $\binom{302}{2} = 45{,}451$ 페어에 달하며, 블록당 97초의 실측치를 기반으로 한 이론 하한만도 79분에 이른다.
본 연구에서 사용한 COLMAP 4.0.3 homebrew 빌드는 `Commit Unknown on Unknown without CUDA`로 SIFT 추출과 matching 전 구간을 CPU에서 실행하므로, 맥북 M-계열의 Metal GPU와 ANE는 재구성 단계에서 유휴 상태로 남는다.

### 3.1.2 Feed-forward 계열의 시간 단축

2024년 이후의 **feed-forward 계열**은 해당 시간 축을 수초~수분 단위로 단축한다.
DUSt3R (Wang et al., 2024)는 feature matching 단계를 생략하고 이미지 쌍에서 dense point cloud를 직접 회귀하며, MASt3R (Leroy et al., 2024)는 correspondence 품질을 개선한 후속 모델이다.
InstantSplat (Fan et al., 2024)은 DUSt3R 초기화를 바탕으로 저 iter 학습을 결합하여 수 분 내 3DGS 산출을 보고한다.
hloc (Sarlin et al., 2019)은 SuperPoint·SuperGlue 계열의 learned feature와 vocabulary tree retrieval을 결합하여 exhaustive matching의 $O(N^2)$ 비용을 $O(N \log N)$ 수준으로 완화한다.

상용 제품군에서는 KIRI Engine이 50~150장 입력 기준 10~15분 내외의 end-to-end 처리를 공개 제품 지표로 제시하며, 본 연구 baseline 대비 약 20~50배의 단축이 관찰된다.
다만 KIRI Engine의 내부 알고리즘·하드웨어는 공개되지 않아, 해당 수치의 해석에는 가설적 요소가 포함된다.

### 3.1.3 격차의 원인 — 알고리즘·빌드 조합

이 격차의 주 원인은 하드웨어 절대 성능이 아니라 **알고리즘과 빌드 조합**으로 판단된다.
feed-forward 계열은 learned matcher로 $O(N^2)$ pair 수를 우회하고 초기화 품질을 확보하여 학습 iter 자체를 1/10 이하로 낮춘다.
즉 속도 축의 격차는 (i) CUDA 미컴파일 SIFT, (ii) $O(N^2)$ exhaustive pairing, (iii) 고정 30K iter의 누적 효과로 해석된다.

하드웨어 관점의 근거로는, Apple Silicon의 peak throughput이 동급 데스크톱 GPU 대비 수십 배 열위가 아님에도 실측 재구성 시간 격차가 수십 배에 달한다는 점을 들 수 있다.
따라서 격차의 대부분은 알고리즘 계보와 빌드 옵션 조합으로 환원 가능하다는 관찰이다.

### 3.1.4 품질 희생과 시나리오별 권고

그럼에도 본 연구는 의도적으로 baseline 축에 파이프라인을 위치시킨다.
품질 축에서의 안정적 수치(PSNR·SSIM 관점)를 확보하는 것이 석사 과정 논문 단계에서 재현성과 검증 가능성을 높이는 데 유리하며, feed-forward 계열은 2024-2025년에 걸쳐 품질 측면에서 baseline 대비 **PSNR 3~6dB 수준의 희생**을 보고하는 것이 일반적이다 (Fan et al., 2024; Wang et al., 2024).

응용 시나리오별 권고는 세 갈래로 요약된다.
첫째, 전시·아카이브·정적 에셋 생산 목적은 baseline 축이 정합한다.
둘째, 모바일 스캔이나 대화형 프로토타이핑과 같이 사용자 대기 시간이 중요한 경우 feed-forward 축이 정합한다.
셋째, 본 연구의 Unity-SplatForge 시스템은 **생성 단계 산출물의 품질 일관성과 검수 가능성**이 배치·검증 단계의 신뢰성과 직결되므로 현 단계에서는 baseline 축을 채택한다.
Feed-forward 경로로의 확장 가능성은 §5 한계와 전망에서 후속 과제로 명시한다.

본 연구의 차별점은 이 trade-off 지형에서 **"baseline 품질을 확보하되 macOS 단일 기기에서 재현 가능한 경로"**를 구현한 데에 있다.
KIRI Engine과 같은 상용 서비스는 품질 축에서 feed-forward로 치우친 선택을 하고, 대부분의 학술 계열 baseline은 CUDA GPU 환경을 전제한다.
본 연구는 그 교차 영역, 즉 **"CUDA-free baseline 품질"** 좌표를 점유한다는 점이 실무·교육 재현성의 관점에서 고유한 기여로 위치된다.

## 3.2 월드 모델 대비 본 연구의 입지

### 3.2.1 월드 모델 흐름의 개요

2024년 말부터 2026년 상반기에 걸쳐 **월드 모델(World Model)** 담론이 재부상하였다.
원류는 Ha & Schmidhuber (2018)가 제시한 V-M-C 구조의 환경 압축 표현이다.
2024-2026 기류의 직접적 동인은 Sora·Veo·Runway Gen-4로 대표되는 생성형 비디오의 시공간 일관성 강화와 Fei-Fei Li가 제기한 공간 지능(Spatial Intelligence) 담론, 그리고 NVIDIA Cosmos로 대표되는 physical AI 데이터 병목 해소 수요이다.
Kong et al. (2025)의 서베이는 해당 흐름을 Video-based, 3D-scene-based, Interactive/Playable, Foundation-for-Physical-AI의 네 축으로 분류하며, 본 절의 비교 축도 이 분류를 따른다.

### 3.2.2 3D-scene 축과 본 연구의 기술 계보 공유

본 연구와 직접 인접한 축은 **3D-scene-based** 계열로, 출력 표현이 3DGS로 수렴한다는 점에서 기술 계보를 공유한다.
NVIDIA Lyra (Wang et al., 2025; Lyra 2.0, 2026)는 비디오 확산 모델의 암묵적 3D 지식을 3DGS로 self-distillation하여 텍스트·단일 이미지에서 실시간 렌더링 가능한 장면을 합성한다.
Lyra 2.0은 surface mesh 공출력을 지원하여 실시간 엔진·물리 시뮬레이터 로드를 명시한다.
Tencent HunyuanWorld 1.0 (Tencent Hunyuan, 2025)과 HY-World 2.0 (2026)은 3DGS와 mesh를 함께 export하는 오픈소스 SOTA를 지향하며, World Labs의 Marble (World Labs, 2025)은 최초의 상용 generative world model로 Vision Pro·Quest 3 즉시 호환을 표방한다.
Interactive/Playable 축의 대표인 DeepMind Genie 3 (DeepMind, 2025)는 720p 24fps 실시간 응답과 수 분 단위의 일관성을 하드코딩 물리 엔진 없이 autoregressive 학습으로 달성한다.

### 3.2.3 생성 단위의 차이 — 씬 E2E vs 의미론적 조립

본 연구는 이 흐름 안에서 **"의미론적 배치 기반 조립"**이라는 별개의 좌표를 차지한다.
월드 모델 대부분이 **씬 전체를 통째로 생성**하는 E2E 접근을 채택하는 반면, 본 연구는 기존·생성형 3DGS 에셋을 LLM의 배치 규칙으로 **의미론적으로 조합**하고 Unity 물리엔진으로 검증한다.
표 1은 주요 축에서의 대비를 정리한다.

**표 1. 월드 모델 계열과 Unity-SplatForge의 대비**

| 대비 축 | 월드 모델 (Lyra/Marble/Genie 3) | Unity-SplatForge (본 연구) |
|--------|-------------------------------|--------------------------|
| 생성 단위 | 씬 전체 (E2E 신경망 산출) | 개별 3DGS 에셋 + LLM 배치 |
| 품질 일관성 | 모델 파라미터 의존, 불투명 | 기존 에셋 재사용·검수 가능 |
| 자유도 | 텍스트·이미지 조건에서 광범위 | 에셋 풀 범위 내 제한, 대신 예측 가능 |
| 물리 정합성 | 학습된 prior (Genie 3) 또는 후처리 (Marble) | Unity 물리엔진 명시적 적용 |
| 편집성 | 신경망 산출의 국부 수정 난이도 높음 | aras-p 툴로 splat 단위 편집 가능 |
| 인프라 요구 | 대규모 GPU 클러스터 학습 필요 | 단일 워크스테이션 + macOS 로컬 경로 |
| 런타임 통합 | 독자 뷰어 또는 신규 엔진 로더 | Unity 네이티브 워크플로 |

### 3.2.4 본 연구의 차별점 세 가지

이 대비에서 본 연구의 차별점은 세 가지로 요약된다.

첫째, **엔진 네이티브 통합**이다.
월드 모델은 대체로 독립 모델이거나 자체 뷰어를 제공하지만, 본 연구는 Unity 런타임에서 `HybridSceneObject`·`LayoutValidator`·`SceneComposer` 등 기존 컴포넌트를 변경 없이 활용한다.

둘째, **의미론적 배치 특화**이다.
Marble·HY-World가 "방 전체를 한 번에" 생성하는 것과 달리, 본 연구는 LLM이 산출한 scene graph에 따라 개별 객체를 배치·검증하므로 에셋 재사용과 수정이 단위별로 가능하다.

셋째, **저자원 재현성**이다.
Genie 3나 Cosmos가 대규모 인프라를 전제하는 반면, 본 연구는 macOS 단일 기기에서 Brush 학습과 aras-p 임포트까지 완결되는 경로를 확보한다.
이는 학부·석사 단계의 재현 가능성과 직결된다.

### 3.2.5 제약과 확장 방향

한편 본 연구의 제약도 명확하다.
월드 모델 대비 **생성 자유도**는 에셋 풀 범위로 한정되며, 비정형 공간이나 비일상 객체의 즉석 생성은 월드 모델 계열이 우세하다.
따라서 후속 연구에서 Lyra·HY-World의 씬 생성 결과를 본 파이프라인의 에셋 입력으로 편입하는 **하이브리드 경로**가 자연스러운 확장 방향으로 남는다.

## 3.3 하이브리드 표현의 설계적 정당화

### 3.3.1 메시+3DGS 분리의 설계 원칙

본 연구는 바닥·벽과 같은 **구조 기하**를 Unity ProBuilder 기반 메시로, 가구·소품과 같은 **객체 기하**를 3DGS 스플랫으로 분리하여 취급하는 하이브리드 표현을 채택한다.
이 설계 선택은 Paper01·Paper02에서도 유지된 바 있으며, 본 논문에서는 월드 모델 흐름과 3DGS 물리 통합의 최근 연구에 비추어 정당화를 보강한다.

### 3.3.2 Collision 근사의 정확성

첫째 논점은 **collision 근사의 정확성**이다.
3DGS는 뷰 종속적 색상을 갖는 이방성 가우시안의 볼륨 집합으로 표현되며, 명시적 표면이 존재하지 않는다.
씬 전체를 단일 3DGS로 구성하는 월드 모델 계열 접근에서는 레이캐스트의 기준면이 부재하여 물리 엔진이 요구하는 정확한 collision 근사가 곤란하다.

본 연구는 바닥·벽을 메시로 고정하여 레이캐스트와 네비게이션 기준면을 확보한다.
3DGS 객체는 프록시 충돌체(AABB 또는 convex hull)로 감싸 Unity PhysX 계열 물리와 정합시킨다.
이 구성은 **구조 기하의 정확성**과 **객체 외관의 실사성**을 동시에 확보하는 실용적 타협점이다.

### 3.3.3 3DGS 네이티브 물리 연구 대비의 위치 선정

둘째 논점은 **3DGS 네이티브 물리 연구 대비의 위치 선정**이다.
PhysGaussian (Xie et al., 2024)은 3DGS를 물질점법(MPM)과 결합하여 splat 자체가 변형·충돌하는 파이프라인을 제안하였다.
PhysSplat (Zhao et al., 2024)과 GASP (Borycki et al., 2025)는 이 방향을 확장한다.

이들은 3DGS 표현에서 직접 물리량을 풀어내는 **네이티브 경로**를 추구하며, 학술적으로는 표현의 일관성과 장기적 확장성 면에서 우위를 갖는다.
그러나 이 경로는 상용 게임 엔진의 네이티브 물리와 직접 호환되지 않으며, 대규모 씬에서의 실시간 성능은 여전히 연구 단계에 있다.
본 연구는 Unity 물리 엔진의 성숙한 런타임 성능과 디자이너 워크플로를 활용하는 **프록시 경로**를 선택하여, 실용적 배포 가능성을 우선한다.
표현 일관성은 PhysSplat·GASP 계열에 양보하되, 생태계 통합과 즉시 활용성을 본 연구의 contribution으로 명시한다.

### 3.3.4 월드 모델의 full-3DGS 출력과의 경계

셋째 논점은 **월드 모델의 full-3DGS 출력과의 경계**이다.
Lyra 2.0이 surface mesh를 공출력하도록 확장된 것은 "3DGS만으로 물리 정합성을 완결하기 어렵다"는 경험적 인식의 반영으로 해석 가능하다.
본 연구의 메시+3DGS 하이브리드는 이 인식과 방향을 공유한다.
다만 메시 생성을 씬 단위 신경망이 아니라 **ProBuilder 도구 + LLM 조건부 구조 정의**에 위임하여 구조 기하의 명확성과 편집성을 확보한다.

결과적으로 본 연구의 하이브리드 표현은 세 제약을 동시에 해소하는 **중간 경로**로 정당화된다.
(i) 월드 모델 계열의 collision 불투명성 우회.
(ii) 3DGS 네이티브 물리 연구의 엔진 통합 지연 우회.
(iii) 메시 기반 파이프라인의 실사성 한계 보완.
