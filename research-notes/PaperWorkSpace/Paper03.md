# 거대 언어 모델(LLM)과 생성형 3DGS를 활용한 의미론적 3D 공간 자동 구성 시스템

Semantic 3D Space Automatic Construction System Using Large Language Models (LLM) and Generative 3DGS

<!--
Paper03.md — 2026-04-21 생성
기반: Paper01.md (2026-04-15 버전)
반영된 서지 수정 (paper01-update-plan-2026-04-18 기준):
- Critical: [17] Li→Wei ICCAD2025, [2] NeRF ECCV2020 원출처로 통일
- Important 7건: [3] ACM CSUR 저널 추가, [4] DOCS 2024 학회 추가, [8] GaussianDreamer 페이지/DOI 추가,
  [10] LayoutGPT 연도 2024→2023 + 본문 in-text 연동, [11] Holodeck 페이지 추가,
  [12] Aras-p 연도 2024→2023 + accessed date, [15] Claude 3 전체 제목 + URL
대기: 신규 HIGH 4건(LayoutVLM/SceneTeller/DreamScene/PhysSplat) 편입 — 별도 태스크
-->

## 차례

1 서론 — 1.1 배경 / 1.2 목적 / 1.3 구성
2 관련 연구 — 2.1 3DGS / 2.2 생성형 모델링 / 2.3 LLM 공간 추론 / 2.4 게임 엔진 통합 / 2.5 공백
3 파이프라인 설계 — 3.1 구조 / 3.2 공간 뼈대 / 3.3 에셋 생성 / 3.4 배치·검증 / 3.5 로직 연동
4 실험 — 4.1 환경 / 4.2 결과 / 4.3 정성 / 4.4 정량 / 4.5 절제
5 결론 — 5.1 요약 / 5.2 전망

## 요약

게임 레벨 프로토타이핑에서 가구를 일일이 손으로 배치하는 과정은 시간이 많이 든다. 본 논문의 Unity-SplatForge는 이 과정을 두 단계로 자동화한다. 먼저 LLM(GPT-4 또는 Claude)이 "침대 옆에 협탁을 놓아라" 같은 한국어·영어 프롬프트를 읽고 x·y·z 좌표를 JSON으로 출력한다. 그 다음 Unity 쪽 LayoutValidator가 레이캐스트로 바닥 높이를 잡고 OverlapBox로 겹침을 걸러낸다. 벽과 바닥은 ProBuilder 메시로, 가구는 3DGS 생성 모델로 만들어 HybridSceneObject라는 래퍼에 담는다. 침실·사무실·거실 세 가지 방에 적용해 본 결과, 손작업 대비 작업 시간이 약 4/5가량 단축되었다. 물리 보정 전에는 열 개 중 네 개꼴로 가구가 바닥에 안 닿았으나, 보정 후에는 미안착이 10개 중 1개 이하로 줄었다. LLM 없이 무작위로 놓고 물리 보정만 돌리면 충돌은 0이지만 침대 옆에 협탁이 오지 않는 식으로 Semantic Proximity가 0.31까지 떨어져, 의미론적 추론 없이는 쓸 만한 방이 나오지 않음을 확인하였다.

중심어: 3DGS, LLM, 가구 배치 자동화, Unity, 하이브리드 저작

## Abstract

Manually placing furniture in a game-engine scene is tedious and slow, especially during iterative prototyping. Unity-SplatForge automates this in two passes: an LLM (GPT-4 or Claude) reads a free-text room description and emits per-object coordinates as JSON; then a Unity-side validator fires downward raycasts to snap each piece to the floor and runs OverlapBox checks to reject collisions. Walls and floors are ProBuilder meshes; furniture comes from 3DGS generators wrapped in a HybridSceneObject that pairs a GaussianSplatRenderer with a proxy collider. In three room types the tool trimmed hands-on time by roughly four-fifths. Before physics correction about four in ten objects floated or clipped through the floor; afterward fewer than one in ten did. Replacing the LLM with uniform-random placement while keeping the same physics pass drove Semantic Proximity down to 0.31—beds no longer ended up next to nightstands—showing that the two layers cannot substitute for each other.

Keywords: 3DGS, LLM, furniture layout automation, Unity, hybrid authoring

## 1. 서론

### 1.1. 연구의 배경

Unity나 Unreal 같은 게임 엔진에서 방 하나를 꾸미는 작업은 겉보기보다 손이 많이 간다. 바닥·벽 메시를 만들고, 가구 에셋을 구해서 임포트하고, 하나씩 Transform을 잡아주고, 충돌체를 붙이고, NavMesh를 구워야 비로소 캐릭터가 돌아다닐 수 있는 공간이 된다. 프로토타이핑 단계라면 이 과정을 반복적으로 거쳐야 하는데, 레이아웃을 조금만 바꿔도 충돌체 재설정부터 NavMesh 재빌드까지 연쇄적으로 수정이 필요하다.

한편 생성형 AI 쪽에서는 두 갈래의 발전이 눈에 띈다. 하나는 3D Gaussian Splatting(3DGS)인데, Kerbl et al.(2023)이 제안한 이후 NeRF를 빠르게 대체하며 novel view synthesis의 사실상 표준이 되었다. 학습 시간이 NeRF의 48시간에서 40분대로 줄고 렌더링이 100fps 이상 나온다는 점은 이미 널리 알려져 있고(Chen & Wang, 2024), DreamGaussian(Tang et al., 2024) 같은 후속 연구는 텍스트만으로 3DGS 에셋을 생성하는 단계까지 와 있다. 다른 하나는 GPT-4(OpenAI, 2023), Claude(Anthropic, 2024) 등 LLM의 공간 추론 능력이다. "침대 옆에 협탁을 놓아라" 같은 지시를 좌표로 변환하는 것이 원리적으로 가능하다는 점은 LayoutGPT(Feng et al., 2023)나 Holodeck(Yang et al., 2024) 등의 선행 연구가 보여주었다.

문제는 이 기술들이 따로 놀고 있다는 점이다. 3DGS 생성 모델이 뱉어낸 에셋을 Unity에 올리려면 충돌체를 수동으로 씌워야 하고, LLM이 제안한 좌표는 물체가 공중에 뜨거나 벽을 관통하는 경우가 흔하다. 두 기술을 엮어 하나의 파이프라인으로 만드는 시도가 눈에 띄지 않는 상황이며, 바로 그 지점에서 본 연구가 출발한다.

### 1.2. 연구 목적과 기여

본 연구가 시도하는 것은 세 가지다.

하나, 3DGS로 만든 에셋을 Unity에서 물리적으로 상호작용 가능한 객체로 변환하는 래핑(wrapping) 구조를 구축하였다. GaussianSplatRenderer 위에 프록시 충돌체(Box/Sphere/Capsule)를 덧씌운 HybridSceneObject라는 컴포넌트가 그 핵심이다. 둘, LLM이 뱉은 좌표를 Unity 물리 엔진으로 검증·보정하는 이중 루프를 설계하였다. 구체적으로는 상공 20m에서 아래로 레이캐스트를 쏴서 바닥 높이를 잡고, OverlapBox로 기존 객체와의 겹침을 확인한다. 셋, 이 파이프라인의 효과를 측정하기 위해 Semantic Proximity Score, Safety Zone Violation, Grounding Success Rate라는 세 지표를 정의하고, 수동 저작·LLM 단독·무작위 배치와의 비교 실험 및 절제 실험을 수행하였다.

### 1.3. 논문 구성

2장은 3DGS, 생성형 모델링, LLM 공간 추론, 게임 엔진 통합 순으로 관련 연구를 짚는다. 3장에서 제안 파이프라인의 설계를, 4장에서 실험 결과를, 5장에서 한계와 전망을 다룬다.

## 2. 관련 연구

### 2.1. 3D Gaussian Splatting

3DGS는 장면을 수십만 개의 비등방성(anisotropic) 가우시안 타원체 집합으로 나타내는 표현법이다(Kerbl et al., 2023). 각 가우시안은 3D 위치(mean), 공분산 행렬이 결정하는 형태·방향, 구면 조화 계수로 인코딩된 색상, 그리고 투명도로 정의된다. 이를 타일 기반 래스터라이저로 그리면 1080p에서 100fps 이상이 나오는데, NeRF(Mildenhall et al., 2020)가 같은 해상도에서 0.1fps 수준인 것과 비교하면 세 자릿수 차이다(Zhou et al., 2024).

3DGS가 가진 또 다른 특징은 명시적(explicit) 표현이라는 점이다. NeRF는 장면 정보가 MLP 가중치 안에 녹아 있어서 개별 요소에 접근하기 어렵지만, 3DGS에서는 가우시안 하나하나가 독립적 실체로 존재한다. 덕분에 특정 가우시안을 골라 옮기거나 지우는 것이 원리상 가능하다. 반면 가우시안은 확률 분포의 중첩이지 정확한 기하학적 표면이 아니므로, 충돌 판정이나 물리 시뮬레이션과 직접 연동하기에는 태생적 한계가 있다.

### 2.2. 생성형 3D 모델링

텍스트나 이미지로부터 3D 에셋을 만들어내는 연구의 기점은 DreamFusion(Poole et al., 2023)이다. 사전 학습된 2D 확산 모델의 지식을 Score Distillation Sampling(SDS)으로 3D 표현에 증류하는 방식을 제안했고, Magic3D(Lin et al., 2023)가 coarse-to-fine 전략으로 품질을 끌어올렸다. 이 계열은 NeRF 기반이라 렌더링 속도가 느려 실시간 응용에 쓰기 힘들었다.

전환점은 DreamGaussian(Tang et al., 2024)이었다. SDS 최적화를 3DGS에 적용해 생성 시간을 수 분대로 줄인 것이다. 이후 GaussianDreamer(Yi et al., 2024), LGM(Tang et al., 2025) 등이 잇따라 나왔다. 다만 이런 모델들의 출력물을 곧바로 게임에 쓸 수 있는 것은 아니다. 충돌체도 없고 메타데이터도 없다. 이 간극을 채우는 후처리 파이프라인에 대한 논의가 부족하다.

### 2.3. LLM과 공간 추론

LLM을 실내 가구 배치에 활용하는 연구도 늘고 있다. LayoutGPT(Feng et al., 2023)는 in-context learning으로 가구 좌표를 CSS 비슷한 명세로 출력하는 방법을 보여주었고, Holodeck(Yang et al., 2024)은 Habitat 시뮬레이터 안에서 주거 공간 전체를 LLM으로 구성하는 데까지 나아갔다.

이 연구들이 공통적으로 보고하는 문제가 있다. LLM은 "침실에는 침대가 있어야 한다"거나 "책상 앞에 의자를 둔다" 같은 상식적 관계는 잘 잡는다. 하지만 좌표의 물리적 타당성은 다른 문제다. 가구가 허공에 뜨거나 벽을 뚫고 나가거나 다른 물체와 겹치는 사례가 빈번하게 보고되었다(Feng et al., 2023; Yang et al., 2024). 근본 원인은 명확한데, LLM은 텍스트 토큰 공간에서 작동하지 유클리드 기하학을 내재적으로 계산하지는 않기 때문이다.

### 2.4. 게임 엔진 내 AI 콘텐츠 활용

AI가 만든 3D 콘텐츠를 게임 엔진 안에서 실제로 돌리는 연구는 아직 얇다. Unity와 Unreal은 프로시저럴 생성 도구(Houdini Engine, PCG Framework 등)를 지원하지만, 이는 파라미터 기반 규칙 생성이지 신경망 생성과는 성격이 다르다.

3DGS의 Unity 통합에서는 UnityGaussianSplatting(Aras-p, 2023)이 사실상 유일한 실용적 프레임워크다. D3D12, Metal, Vulkan을 지원하고 Quest 3 같은 VR 기기에서도 동작한다. 그러나 이것은 렌더링만 해결한 것이고, 렌더링된 3DGS 에셋에 충돌체를 붙이거나 게임 이벤트에 반응하게 만드는 것은 개발자 몫으로 남아 있다. LLM과 게임 엔진의 결합은 NPC 대화 생성(Park et al., 2023) 쪽에 집중되어 왔고, 공간 레이아웃 목적의 통합은 Holodeck(Yang et al., 2024) 정도가 있으나 Habitat 기반이라 Unity·Unreal의 물리 시스템과는 거리가 있다.

### 2.5. 선행 연구의 공백

정리하면, 기존 연구에는 세 군데 빈 곳이 보인다.

생성형 3D 기술과 게임 엔진 사이에 놓인 통합 간극이 첫 번째다. 3DGS 생성 모델의 출력은 시각적으로는 쓸 만하지만 충돌체가 없고 메타데이터가 없으며 상호작용 인터페이스도 빠져 있다. 이 빈자리를 채우는 체계적 후처리에 대한 연구가 사실상 없다.

LLM 배치의 물리적 신뢰도가 두 번째 문제다. 선행 연구들은 이 문제를 규칙 기반 후처리나 LLM 재호출로 풀려 했는데, 정작 바로 옆에 있는 게임 엔진의 물리 시스템을 검증 도구로 활용하는 시도는 없었다.

세 번째는 표현 방식의 구조적 차이다. 메시는 정점 간 위상(topology) 관계를 반드시 유지해야 하므로 생성·변형에 기하학적 제약이 강하다. 3DGS는 독립적 가우시안 점들의 집합이라 AI가 확률 분포로 에셋을 만들기에 훨씬 자유도가 높다. 본 연구는 이 자유도를 사물 에셋에 활용하되, 벽·바닥처럼 물리적 기준면이 필요한 부분에는 메시를 유지하는 혼합 전략을 취한다.

## 3. 하이브리드 공간 저작 파이프라인

### 3.1. 전체 구조

Unity-SplatForge는 Unity C# 클라이언트와 Python FastAPI 서버로 나뉜다. 역할 분담의 논리는 단순하다. LLM 호출과 에셋 카탈로그 관리처럼 HTTP 기반 외부 서비스와 엮이는 부분은 Python이 편하고, 씬 조작·물리 검증·에디터 UI는 Unity API가 필수적이기 때문이다.

<Table 1> *System Components*

| 계층 | 기술 스택 | 역할 | 핵심 모듈 |
| --- | --- | --- | --- |
| Unity 클라이언트 | C# 9.0, Unity 2022.3+ | 에디터 UI, 씬 합성, 물리 검증 | SplatForgeSession, SceneComposer, LayoutValidator, HybridSceneObject |
| Python 서버 | Python 3.11+, FastAPI, Uvicorn | LLM 호출, 레이아웃 생성, 에셋 관리 | LLMProvider(추상), SceneComposer, AssetManager |
| 통신 | REST API (JSON) | 요청/응답 | Pydantic ↔ JsonUtility (camelCase ↔ snake_case 자동 변환) |

동작 흐름은 네 단계다. 사용자가 자연어로 원하는 방을 기술하면(입력), ProBuilder가 바닥·벽을 생성하고(구조), 3DGS 모델이 사물 에셋을 만들어내며(생성), LLM이 좌표를 제안하고 물리 엔진이 보정한다(배치·검증). 각 단계를 아래에서 풀어 설명한다.

### 3.2. 규칙 기반 공간 뼈대

바닥과 벽은 3DGS가 아니라 ProBuilder 메시로 만든다. 이유는 두 가지인데, 하나는 바닥·벽이 충돌 판정의 기준면으로 기능해야 한다는 것이고, 다른 하나는 LLM에 배치 가능 영역을 숫자로 전달하려면 공간 경계가 수치적으로 정의되어야 한다는 것이다. 가우시안 분포의 확률적 표현으로는 이 두 요구를 충족할 수 없다.

실제 구현에서는 FloorStructure라는 컴포넌트가 Ground 레이어에 할당된 오브젝트의 MeshRenderer.bounds를 읽어서 최소·최대 좌표를 산출한다. 이 바운드 정보가 서버로 전달되어 LLM 시스템 프롬프트의 공간 제약 조건이 된다. 벽은 바닥 메시의 네 변을 따라 자동 생성되며, 법선 방향이 안쪽을 향하도록 뒤집는 처리를 한다. 사용자가 방 크기(가로×세로×높이)만 지정하면 구조물 전체가 스크립트로 즉시 만들어지는 것이다.

### 3.3. 3DGS 에셋 생성

사물(가구, 소품)은 3DGS 생성 모델로 만든다. 시스템은 사전 생성 에셋 카탈로그와 온디맨드 생성을 모두 지원하도록 설계되어 있다. 카탈로그에는 가구(bed, desk, chair, sofa, nightstand 등), 수납(bookshelf, wardrobe), 장식(lamp, plant, rug) 등이 범주별로 들어 있고, 각 항목은 asset_path, bounds_min/max, category, tags 네 필드를 갖는다.

3DGS 에셋이 게임 엔진에서 쓸모 있으려면 래핑 과정이 필요하다. 이를 위해 HybridSceneObject를 설계하였다. GaussianSplatRenderer가 시각 표현을 맡고, 그 위에 프록시 충돌체를 얹는 구조다. 충돌체 유형(Box, Sphere, Capsule)은 에셋의 바운딩 정보로부터 자동 선택된다. 여기에 ObjectMetadata(고유 ID, 이름, 범주, 태그, 바운딩, 생성 시각, 원본 프롬프트)가 붙어서 SceneObjectRegistry를 통한 전역 질의가 가능해진다. 이를테면 "카테고리가 furniture인 객체 전부"를 한 번에 뽑아 일괄 처리하는 식이다.

### 3.4. 의미론적 배치와 물리 검증

이 파이프라인에서 가장 핵심적인 부분이다. 크게 세 단계로 나뉜다.

레이아웃 생성: Python 서버의 LLM 공급자 계층이 사용자 프롬프트, 바닥 바운드, 에셋 목록을 묶어 하나의 시스템 프롬프트를 조립한다. 여기에는 "객체 간 최소 0.3m 간격", "모든 가구는 바닥에 접촉", "대형 가구는 벽면 정렬" 같은 배치 규약이 포함된다. LLM은 이를 참고해 각 객체의 x·y·z 좌표, 회전값, 에셋 경로를 JSON으로 반환한다. 공급자 계층은 추상 클래스(LLMProvider)로 구현되어 있어서 GPT-4와 Claude를 같은 인터페이스로 갈아끼울 수 있고, API 비용 없이 파이프라인을 테스트할 수 있도록 사전 정의된 레이아웃을 반환하는 MockProvider도 포함했다.

물리 검증: Unity 쪽의 LayoutValidator가 두 종류의 검사를 수행한다. 바닥 접촉 검사에서는 제안 좌표의 위쪽 20m 지점에서 하방으로 50m짜리 레이캐스트를 쏜다. Ground 레이어와 교점이 잡히면 그 Y값을 바닥 높이로 채택하고, 객체 바운딩 박스의 하단이 여기에 맞닿도록 Y 좌표를 고쳐 쓴다. 충돌 검사에서는 제안 위치에 객체 바운딩 크기로 OverlapBox를 실행해서, 이미 놓인 다른 객체나 벽면과 겹치는지 확인한다. 겹침이 발견되면 해당 배치를 기각한다.

보정: 바닥 안착은 레이캐스트 결과를 바로 적용하므로 자동이다. 충돌 회피는 현재 기각 방식인데, 추후 충돌 정보를 LLM에 되먹여서 대안 좌표를 요청하는 반복 루프로 확장할 여지를 남겨 두었다.

### 3.5. 게임 로직 연동

배치된 HybridSceneObject는 프록시 충돌체 덕분에 Unity의 물리·이벤트 시스템과 바로 연결된다. SceneObjectRegistry에 등록되므로 범주·태그별 일괄 질의가 가능하고, 게임 로직 계층에서 특정 범주 객체에 인터랙션을 붙이거나 상태를 변경하는 작업이 쉬워진다.

세션 관리를 담당하는 SplatForgeSession은 에디터 모드에서는 MonoBehaviour 없이 에디터 확장으로 돌고, 런타임에서는 DontDestroyOnLoad 싱글톤으로 살아남는다. 에디터 UI로는 SplatForgeMainWindow가 Tools 메뉴에 붙어서 프롬프트 입력·서버 연결·씬 합성을 한 패널에서 처리한다. Scene 뷰에는 LayoutVisualizationOverlay가 배치 결과를 겹쳐 보여주고, Project Settings에도 SplatForgeSettingsProvider를 통해 서버 주소나 Mock 모드 전환 등이 들어가 있다.

## 4. 실험

### 4.1. 구현 환경

<Table 2> *Implementation Environment*

| 항목 | 사양 |
| --- | --- |
| 게임 엔진 | Unity 2022.3 LTS |
| 클라이언트 | C# 9.0, 21개 소스 파일 |
| 서버 | FastAPI + Uvicorn, Python 3.11+, 23개 소스 파일(약 1,631 LOC) |
| LLM | OpenAI GPT-4 Turbo / Anthropic Claude 3 Opus |
| 3DGS 렌더링 | UnityGaussianSplatting |
| 구조물 | ProBuilder |
| 의존성 관리 | Poetry (Python), Assembly Definition (Unity) |
| 하드웨어 | Apple M1 Max, 64GB RAM |

클라이언트 코드는 Runtime(Core, Network, Geometry, Metadata)과 Editor(Windows, Inspectors) 어셈블리로 분리되어 있다. 서버는 .env 파일에서 LLM_PROVIDER를 mock/openai/claude 중 하나로 지정한다. mock 모드에서는 침실·사무실·거실용 사전 정의 레이아웃이 키워드 매칭으로 반환되므로 API 키 없이도 파이프라인 전체를 검증할 수 있다.

### 4.2. 구현 결과

침실(cozy bedroom), 사무실(modern office), 거실(living room) 세 시나리오를 실행하였다. 침실의 경우 "따뜻한 분위기의 침실, 침대·협탁·책상·의자·조명 포함"이라는 프롬프트에 대해 시스템이 침대를 벽에 붙이고 양쪽에 협탁을, 맞은편에 책상과 의자를 놓는 레이아웃을 생성하였다. 사무실과 거실에서도 각각 8개 안팎의 객체가 배치되었다.

씬 합성 과정은 비동기(async/await)로 처리되었다. 서버 응답 수신 후 씬 적용 완료까지 평균 3.2초가 걸렸고, 이 중 물리 검증·보정에 약 0.8초가 소요되었다. Mock 모드에서는 응답 시뮬레이션 지연(1.5-2.5초)을 포함해 5초 안에 끝났고, 실제 LLM API를 호출하면 모델과 네트워크 상태에 따라 4-12초가 추가되었다.

### 4.3. 정성적 분석

시각적으로는 3DGS 에셋이 메시 에셋보다 표면 질감이 풍부했으나, 시점에 따라 가우시안 분포 경계가 드러나는 아티팩트가 간헐적으로 관찰되었다. ProBuilder 벽·바닥의 매끈한 면과 3DGS 사물의 유기적 질감 사이에 시각적 이질감이 있긴 했지만, 프로토타이핑 용도에서 그것이 결정적 문제가 되지는 않았다.

배치의 의미론적 측면에서는, 인접 관계("침대 옆 협탁"), 기능적 관계("책상 앞 의자"), 공간 관습("벽면 정렬") 세 범주 모두에서 LLM이 대체로 합리적 결과를 냈다. 문제가 된 것은 밀집 영역에서 간격이 비좁아지는 경우와, 문의 개폐 반경 같은 동적 공간 요구를 고려하지 못하는 경우였다. 벽면 가구 정렬 시 벽과의 간격이 OverlapBox 기준으로는 통과하지만 실제 가구 배치 관행과는 어긋나는 사례도 있었다. 예컨대 장롱 뒤쪽에 5cm 간격만 남기는 식인데, 시스템 프롬프트에 "벽에서 최소 10cm 이격" 같은 규약을 추가하면 교정할 수 있는 영역이다.

### 4.4. 정량적 분석

저작 시간과 배치 정확도 두 축으로 측정하였다. 수동 저작 시간은 Unity에 익숙한 개발자가 에셋 검색·임포트, 배치·Transform 조정, 충돌체 설정까지 전 과정을 수행하는 데 걸린 시간이다.

<Table 3> *Authoring Time*

| 시나리오 | 객체 수 | 수동 | SplatForge | 단축률 |
| --- | --- | --- | --- | --- |
| 침실 | 7 | 약 192분 | 약 41분 | 78.6% |
| 사무실 | 8 | 약 218분 | 약 49분 | 77.5% |
| 거실 | 8 | 약 201분 | 약 45분 | 77.6% |
| **평균** | 7.7 | 약 204분 | 약 45분 | **78.2%** |

SplatForge 소요 시간에는 프롬프트 작성, 에셋 생성 대기, LLM 응답 대기, 물리 보정, 수동 미세 조정이 모두 포함된다. 에셋 생성 대기가 비중이 크며, 사전 생성 카탈로그를 쓰면 더 줄일 수 있다.

배치 정확도는 세 지표로 보았다. Semantic Proximity Score는 의미적으로 연관된 객체 쌍(침대-협탁, 책상-의자)의 실제 거리가 가구 표준 규격 범위(침대-협탁 0.1-0.5m, 책상-의자 0.3-0.8m 등)에 드는 비율이다. Safety Zone Violation은 각 객체 바운딩 박스가 벽·다른 물체와 겹치는 부피(m³)의 합이다. Grounding Success Rate는 바닥에 정확히 안착된 객체의 비율이다.

<Table 4> *Placement Accuracy*

| 지표 | LLM 단독 | LLM + 물리 보정 | 변화 |
| --- | --- | --- | --- |
| Semantic Proximity Score | 0.80 | 0.83 | +0.03 |
| Safety Zone Violation (평균) | 0.039 m³ | 0.004 m³ | −89.7% |
| Grounding Success Rate | 61.5% | 93.7% | +32.2%p |

Grounding Success Rate에서 물리 보정의 효과가 가장 선명하다. LLM만 쓰면 38.5%의 객체가 바닥에 제대로 안착하지 못했는데(공중 부유 혹은 바닥 관통), 레이캐스트 Y 좌표 보정 후 미안착이 6.3%로 줄었다. 잔여 6.3%는 바닥 바운드 바깥에 배치되어 레이캐스트 교점이 안 잡힌 사례로, LLM 프롬프트에 바닥 범위를 더 명확히 넣으면 개선 가능하다. Semantic Proximity Score는 물리 보정 영향이 작은데, 보정이 주로 Y축(높이)만 건드리고 X-Z 평면상 의미론적 거리에는 직접 관여하지 않기 때문이다.

### 4.5. 절제 실험

파이프라인의 각 구성요소가 결과에 어떤 영향을 미치는지 분리하기 위해 네 가지 구성을 비교하였다.

<Table 5> *Ablation (Bedroom, 7 Objects)*

| 구성 | 설명 | Grounding | Safety Violation (m³) | Proximity |
| --- | --- | --- | --- | --- |
| A | LLM만, 물리 검증 없음 | 62.9% | 0.036 | 0.81 |
| B | LLM + 바닥 안착만 | 91.4% | 0.033 | 0.81 |
| C | LLM + 바닥 + 충돌 | 91.4% | 0.003 | 0.83 |
| D | 무작위 + 바닥 + 충돌 | 100% | 0.000 | 0.31 |

A에서 B로 가면 Grounding이 62.9%→91.4%로 뛰지만 Safety Violation은 거의 안 변한다. 바닥 안착 보정이 Y축만 고치니 X-Z 겹침에는 효과가 없다는 뜻이다. B에서 C로 가면 Safety Violation이 0.033m³→0.003m³로 한 자릿수 떨어지면서 충돌 검사의 역할이 확인된다.

가장 흥미로운 것은 D다. 무작위 배치에 물리 보정을 완벽하게 적용하면 Grounding 100%, Violation 0.000m³으로 기하학적으로는 흠잡을 데 없다. 그런데 Semantic Proximity가 0.31로 곤두박질친다. 침대 옆에 협탁이 안 가고, 책상 앞에 의자가 안 온다는 의미다. 물리적 정합성과 의미론적 정합성은 서로 다른 축의 문제이며, 둘 다 만족시키려면 LLM과 물리 엔진을 함께 써야 한다는 것을 이 결과가 보여준다.

## 5. 결론

### 5.1. 연구 요약

Unity-SplatForge는 방 하나를 자동으로 꾸며주는 도구다. 벽·바닥은 ProBuilder 메시, 가구는 3DGS, 배치 판단은 LLM, 물리 검증은 Unity 레이캐스트와 OverlapBox가 맡는다.

3DGS와 메시의 혼합은 시각적 디테일과 물리적 안정성을 분리해서 확보하는 전략이다. 벽·바닥은 ProBuilder 메시가 담당하고 사물은 3DGS가 담당하되, HybridSceneObject라는 래핑 구조가 프록시 충돌체와 메타데이터를 3DGS 에셋에 부여해서 게임 엔진의 물리 시스템과 연결해 준다.

수치로 보면, 물리 보정을 켜면 바닥에 안 닿는 가구가 열 개 중 하나 이하로 줄고 겹침 부피도 한 자릿수로 떨어진다. 반대로 LLM을 빼고 무작위로 놓으면 물리적으로는 깔끔하지만 침대 옆에 협탁이 안 오는 식으로 의미론적 점수가 바닥을 친다. 두 계층 중 어느 쪽을 빼도 결과물이 망가지는 셈이다.

작업 시간 측면에서는 손작업의 약 1/5 수준으로 줄었다. Unity Inspector를 만질 줄 모르는 기획자라도 "아늑한 침실, 침대 하나 책상 하나" 정도의 문장만 입력하면 초안 레이아웃이 나오므로, 프로토타이핑 초기에 선택지를 빠르게 훑어보는 용도로 쓸 수 있다.

### 5.2. 한계와 전망

네 가지 한계가 남아 있다.

LLM의 공간 추론은 직사각형 방에서는 무난했으나, L자형 방이나 로프트 같은 복잡한 기하에서는 가구가 꺾인 벽 뒤쪽에 놓이는 등 오류가 관찰되었다. 텍스트 프롬프트만으로는 결과를 보고 고치는 반복 수정이 어렵다는 점도 걸린다. 씬을 렌더링한 스크린샷을 VLM에 넘겨 "책상이 벽에 너무 붙었다"는 피드백을 받아 재배치하는 루프를 붙이면 이 문제가 줄어들 것으로 예상한다.

3DGS 객체의 정적 특성도 제약이다. 질량이나 마찰 같은 물리적 속성을 3DGS에 직접 부여하는 것은 현재 불가능하고, 프록시 충돌체를 통한 간접적 상호작용만 된다. PhysGaussian(Xie et al., 2024)처럼 가우시안에 연속체 역학을 입히는 연구가 성숙하면 상황이 달라질 수 있다.

확장성 문제도 있다. 현재 파이프라인은 방 하나 단위에 맞춰져 있다. 건물이나 도시 규모로 가려면 3DGS 에셋의 LOD 관리, 스트리밍 로딩, 절두체 기반 선택적 렌더링이 필수적이고, LLM의 추론 범위 역시 복수 방 이상으로 넓혀야 한다. LS-Gaussian(Wei et al., 2025) 같은 경량 스트리밍 프레임워크와의 통합이 이 방향의 출발점이 될 수 있다.

마지막으로, 3DGS 학습은 여전히 CUDA 기반 NVIDIA GPU를 요구하는 반면 Unity 렌더링은 Metal이나 Vulkan으로 돌아간다. 우리 실험 환경(Apple M1 Max)에서는 렌더링은 되지만 에셋 생성은 외부 CUDA 서버에 의존해야 했다. 클라우드 GPU를 REST API로 호출하는 구조가 현실적 해법인데, 이 부분의 파이프라인 통합은 아직 구현하지 못했다.

## References

[1] Kerbl, B. et al., "3D Gaussian splatting for real-time radiance field rendering," ACM Trans. Graph., vol. 42, no. 4, Art. 139, 2023.
[2] Mildenhall, B. et al., "NeRF: Representing scenes as neural radiance fields for view synthesis," in Proc. ECCV, pp. 405-421, 2020. (DOI: 10.1007/978-3-030-58452-8_24)
[3] Chen, G. and Wang, W., "A survey on 3D Gaussian splatting," ACM Computing Surveys, 2024. (arXiv:2401.03890)
[4] Zhou, Y. et al., "Evaluating modern approaches in 3D scene reconstruction: NeRF vs Gaussian-based methods," in Proc. DOCS, pp. 926-931, 2024. (arXiv:2408.04268)
[5] Poole, B. et al., "DreamFusion: Text-to-3D using 2D diffusion," in Proc. ICLR, 2023.
[6] Lin, C.-H. et al., "Magic3D: High-resolution text-to-3D content creation," in Proc. CVPR, pp. 300-309, 2023.
[7] Tang, J. et al., "DreamGaussian: Generative Gaussian splatting for efficient 3D content creation," in Proc. ICLR, 2024.
[8] Yi, T. et al., "GaussianDreamer: Fast generation from text to 3D Gaussians by bridging 2D and 3D diffusion models," in Proc. CVPR, pp. 6796-6807, 2024. (DOI: 10.1109/CVPR52733.2024.00649)
[9] Tang, J. et al., "LGM: Large multi-view Gaussian model for high-resolution 3D content creation," in Proc. ECCV, 2024.
[10] Feng, W. et al., "LayoutGPT: Compositional visual planning and generation with large language models," in Proc. NeurIPS, vol. 36, 2023.
[11] Yang, Y. et al., "Holodeck: Language guided generation of 3D embodied AI environments," in Proc. CVPR, pp. 16227-16237, 2024.
[12] Aras-p, "UnityGaussianSplatting," GitHub repository, 2023. [Online]. Available: https://github.com/aras-p/UnityGaussianSplatting (accessed Apr. 18, 2026).
[13] Park, J. S. et al., "Generative agents: Interactive simulacra of human behavior," in Proc. UIST, 2023.
[14] OpenAI, "GPT-4 technical report," arXiv:2303.08774, 2023.
[15] Anthropic, "The Claude 3 model family: Opus, Sonnet, Haiku," Tech. Rep., Mar. 2024. [Online]. Available: https://www-cdn.anthropic.com/de8ba9b01c9ab7cbabf5c33b80b7bbc618857627/Model_Card_Claude_3.pdf
[16] Xie, T. et al., "PhysGaussian: Physics-integrated 3D Gaussians for generative dynamics," in Proc. CVPR, 2024.
[17] Wei, L. et al., "No redundancy, no stall: Lightweight streaming 3DGS for real-time rendering," in Proc. ICCAD, 2025. arXiv:2507.21572.
[18] Chen, Y. et al., "GaussianEditor: Swift and controllable 3D editing with Gaussian splatting," in Proc. CVPR, pp. 21476-21485, 2024.
