# Unity-SplatForge

![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?style=flat&logo=unity)
![Python](https://img.shields.io/badge/Python-3.10%2B-blue?style=flat&logo=python)
![Architecture](https://img.shields.io/badge/Architecture-Client%2FServer-orange)
![Tech](https://img.shields.io/badge/Tech-3DGS%20%7C%20LLM-purple)

> **A Neuro-Symbolic 3D Level Authoring Tool in Unity using Generative 3D Gaussian Splatting and LLM-based Spatial Layout.**
>
> 생성형 3D Gaussian Splatting(3DGS)과 거대 언어 모델(LLM)의 공간 추론을 결합한 유니티 기반의 하이브리드 공간 저작 파이프라인입니다.

---

## 📖 Overview (개요)

**Unity-SplatForge**는 파편화된 최신 3D 생성 AI 기술을 상용 게임 엔진(Unity) 워크플로우로 통합하는 연구 프로젝트입니다. 

기존의 텍스트-3D(Text-to-3D) 연구들이 단순한 시각적 생성(Viewer)에 머무르는 한계를 극복하기 위해, 본 프로젝트는 **'구조(Structure)는 명시적 규칙(Mesh)으로, 디테일(Detail)은 생성형 AI(Splat)로'** 처리하는 하이브리드 접근 방식을 제안합니다. 특히, LLM이 제안하는 의미론적 배치(Semantic Layout)를 유니티의 물리 엔진(Physics Raycast)으로 검증하여 "상호작용 가능한(Game-Ready)" 3D 공간을 자동으로 구성합니다.

## 🎯 Research Background (연구 배경)

1.  **3DGS의 진화:** 2025년 이후 3D Gaussian Splatting은 단순 렌더링 기술을 넘어 '공간 지능(Spatial Intelligence)'을 위한 데이터 표준으로 자리 잡고 있습니다.
2.  **기술의 파편화:** SOTA(State-of-the-Art) 생성 모델들은 대부분 Python/Research 환경에 머물러 있어, 실제 게임 개발 파이프라인(Unity/Unreal)에 즉시 적용하기 어렵습니다.
3.  **공간 환각(Spatial Hallucination):** LLM은 공간을 의미론적으로 이해하지만, 정확한 물리적 좌표 계산에는 취약하여 물체가 공중에 뜨거나 겹치는 현상이 발생합니다.

본 프로젝트는 이러한 문제를 해결하기 위해 **Neuro-Symbolic(신경망-기호 주의)** 접근법을 통해 AI의 창의성과 물리 엔진의 정합성을 결합합니다.

## ✨ Key Features (주요 기능)

* **Hybrid Geometry Pipeline:**
    * **Structure:** ProBuilder를 이용해 정확한 물리 충돌이 필요한 바닥/벽 구조 생성.
    * **Detail:** LGM(Large Geometry Model) 등을 이용해 텍스트 프롬프트로부터 고품질 3DGS 에셋 실시간 생성.
* **Neuro-Symbolic Layout:**
    * LLM(OpenAI o3/GPT-4o)을 활용하여 공간 맥락(Context)에 맞는 가구 및 오브젝트 배치 제안.
    * Unity Physics(Raycast, Collider Check)를 이용해 LLM의 좌표 오류를 실시간 보정 (Auto-Correction).
* **In-Editor AI Control:**
    * 외부 터미널 없이 Unity 에디터 내에서 Python AI 생성 서버를 제어하는 `ServerLauncher` 탑재.
* **Game-Ready Interaction:**
    * 생성된 Splat 객체에 자동으로 Proxy Collider 및 메타데이터를 부여하여 게임 로직(Navigation, Physics)과 즉시 연동.

## 🏗 System Architecture (시스템 구조)

```mermaid
graph LR
    A[User Input] -->|ProBuilder Layout + Prompt| B(Unity Client);
    B -->|Request Generation| C[Python AI Server];
    C -->|LGM / 3DGS Model| D[Generative 3D Asset (.ply)];
    B -->|Request Layout| E[LLM Agent];
    E -->|Semantic Coordinates| B;
    D --> B;
    B -->|Physics Verification| F{Raycast Correction};
    F -->|Verified Transform| G[Final Scene];