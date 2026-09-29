# SplatForge Unity Client Architecture

## Overview

Unity 에디터에서 3D Gaussian Splatting(3DGS) 에셋을 생성/배치하고, **Scene Composition** 기반으로 공간 단위 레이아웃을 구성하는 연구용 프로토타입 시스템.

### 핵심 워크플로우

```
User Prompt + Floor Structure → Server → Layout JSON + Assets → Physics Validation → Scene
```

## Tech Stack

| Layer | Technology |
|-------|------------|
| Engine | Unity 2022.3+ (LTS) |
| Rendering | UnityGaussianSplatting Plugin |
| Language | C# 9.0 |
| Async | Task-based Async Pattern (TAP) |
| Serialization | Unity JsonUtility |
| Editor UI | IMGUI (EditorGUILayout) |

## Folder Structure

```
Assets/SplatForge/
├── Runtime/                          # 런타임 코드 (빌드에 포함)
│   ├── Core/                         # 핵심 시스템
│   │   ├── SplatForgeSettings.cs     # 전역 설정 (ScriptableObject)
│   │   ├── SplatForgeSession.cs      # 세션 관리자 + ISession 인터페이스
│   │   ├── SceneObjectRegistry.cs    # 씬 오브젝트 레지스트리
│   │   ├── FloorStructure.cs         # 바닥/벽 구조 정의 + 자동 감지
│   │   ├── SceneComposer.cs          # 씬 구성 결과 적용 로직
│   │   ├── LayoutValidator.cs        # Physics 기반 배치 검증
│   │   ├── SceneConfiguration.cs     # 씬 설정 직렬화
│   │   └── BatchProcessor.cs         # 배치 작업 처리
│   │
│   ├── Network/                      # 서버 통신
│   │   ├── ISplatForgeServer.cs      # 서버 인터페이스
│   │   ├── MockSplatForgeServer.cs   # Mock 서버 구현
│   │   └── ServerMessages.cs         # 요청/응답 DTO
│   │
│   ├── Geometry/                     # 지오메트리 관련
│   │   └── HybridSceneObject.cs      # GaussianSplatRenderer 래퍼
│   │
│   ├── Metadata/                     # 메타데이터 시스템
│   │   ├── ObjectMetadata.cs         # 오브젝트 메타데이터
│   │   └── MetadataPresets.cs        # 카테고리/태그 프리셋
│   │
│   └── SplatForge.Runtime.asmdef     # Runtime Assembly Definition
│
├── Editor/                           # 에디터 전용 코드
│   ├── Windows/                      # 에디터 윈도우
│   │   ├── SplatForgeMainWindow.cs   # 메인 컨트롤 패널 (Scene Composition UI)
│   │   ├── LayoutVisualizationOverlay.cs  # Scene View 오버레이
│   │   └── SceneConfigurationWindow.cs    # 씬 설정 관리
│   │
│   ├── Inspectors/                   # Custom Inspector
│   │   ├── HybridSceneObjectEditor.cs    # HybridSceneObject Inspector
│   │   └── MetadataPresetEditor.cs       # MetadataPreset Inspector
│   │
│   ├── SplatForgeSettingsProvider.cs # Project Settings 통합
│   └── SplatForge.Editor.asmdef      # Editor Assembly Definition
│
├── Resources/                        # 자동 로드 에셋
│   └── SplatForgeSettings.asset      # 전역 설정 파일
│
└── Samples~/                         # 샘플 데이터
    ├── MockLayouts/                  # Mock 레이아웃 JSON
    │   ├── cozy_bedroom.json
    │   ├── modern_office.json
    │   └── living_room.json
    └── MockAssets/                   # Mock GaussianSplatAsset
```

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Unity Editor                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                      SplatForgeSettings                              │    │
│  │                  (ScriptableObject - 전역 설정)                       │    │
│  │  - Server Endpoint, Mock 설정                                        │    │
│  │  - Layout 기본값                                                     │    │
│  └──────────────────────────────┬──────────────────────────────────────┘    │
│                                 │ 설정 참조                                  │
│                                 ▼                                            │
│  ┌──────────────────┐  ┌────────────────────────────────────────────────┐   │
│  │ SplatForgeMain   │  │              ISession (인터페이스)              │   │
│  │ Window           │──▶│  ┌──────────────────────────────────────────┐  │   │
│  │                  │  │  │ EditorSession / SplatForgeSession        │  │   │
│  │ - Scene Comp UI  │  │  │ - ComposeSceneAsync()                    │  │   │
│  │ - Floor Detect   │  │  │ - GenerateObjectAsync()                  │  │   │
│  │ - Preview/Apply  │  │  │ - GetLayoutSuggestionAsync()             │  │   │
│  └──────────────────┘  │  └──────────────────────────────────────────┘  │   │
│                        └──────────────────┬─────────────────────────────┘   │
│                                           │                                  │
│        ┌──────────────────────────────────┼──────────────────────────┐      │
│        ▼                                  ▼                          ▼      │
│  ┌─────────────────────┐  ┌──────────────────────┐  ┌──────────────────┐   │
│  │  ISplatForgeServer  │  │  SceneComposer       │  │ FloorStructure   │   │
│  │  ┌───────────────┐  │  │  - ApplyComposition  │  │ - Auto-detect    │   │
│  │  │MockSplatForge │  │  │  - ValidatePlacement │  │ - Manual input   │   │
│  │  │Server         │  │  │  - InstantiateObject │  │ - Ground layer   │   │
│  │  └───────────────┘  │  └──────────┬───────────┘  └──────────────────┘   │
│  │  ┌───────────────┐  │             │                                      │
│  │  │(Future)       │  │             ▼                                      │
│  │  │PythonServer   │  │  ┌──────────────────────────────────────────┐     │
│  │  └───────────────┘  │  │          HybridSceneObject               │     │
│  └─────────────────────┘  │  ┌────────────────────────────────────┐  │     │
│                           │  │ GaussianSplatRenderer              │  │     │
│                           │  └────────────────────────────────────┘  │     │
│                           │  ┌────────────────────────────────────┐  │     │
│                           │  │ ObjectMetadata                     │  │     │
│                           │  └────────────────────────────────────┘  │     │
│                           │  ┌────────────────────────────────────┐  │     │
│                           │  │ Proxy Collider (직렬화)            │  │     │
│                           │  └────────────────────────────────────┘  │     │
│                           └──────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Core Components

> **사용법은 [USAGE_GUIDE.md](./USAGE_GUIDE.md) 참조**

### 1. SplatForgeSettings

ScriptableObject 기반 전역 설정. Resources 폴더에서 자동 로드.

- 위치: `Assets/SplatForge/Resources/SplatForgeSettings.asset`
- 편집: `Edit > Project Settings > SplatForge`

### 2. ISession / SplatForgeSession

세션 인터페이스 및 구현체.

| 구현체 | 사용 시점 | 생성 방식 |
|--------|----------|----------|
| `EditorSession` | Edit Mode | 자동 |
| `SplatForgeSession` | Play Mode | 자동 (DontDestroyOnLoad) |

```csharp
ISession session = SplatForgeSession.Current; // 항상 유효한 세션 반환

// Scene Composition (메인 워크플로우)
var result = await session.ComposeSceneAsync(prompt, floorStructure, options);

// 개별 오브젝트 생성 (테스트용)
var result = await session.GenerateObjectAsync(prompt, quality);
```

### 3. ISplatForgeServer

서버 통신 추상화 인터페이스.

```csharp
public interface ISplatForgeServer
{
    Task<bool> ConnectAsync(string endpoint = null);
    void Disconnect();
    bool IsConnected { get; }

    // 메인 워크플로우: Scene Composition
    Task<SceneCompositionResult> ComposeSceneAsync(SceneCompositionRequest request);

    // 개별 기능 (테스트/확장용)
    Task<GenerationResult> GenerateObjectAsync(GenerationRequest request);
    Task<LayoutSuggestion> GetLayoutSuggestionAsync(LayoutRequest request);
}
```

**구현체:**
- `MockSplatForgeServer`: 테스트용 Mock 구현 (내장 레이아웃 데이터 사용)
- (Future) `HttpSplatForgeServer`: Python 서버 연동

### 4. FloorStructure

바닥/벽 구조 정의 및 자동 감지.

```csharp
// Ground 레이어에서 자동 감지
var floor = FloorStructure.DetectFromScene();

// 수동 생성
var floor = FloorStructure.CreateManual(center, size, floorHeight);

// 속성
floor.BoundsMin;    // 바닥 영역 최소점
floor.BoundsMax;    // 바닥 영역 최대점
floor.FloorHeight;  // 바닥 높이
floor.Area;         // 면적 (m²)
```

### 5. SceneComposer

Scene Composition 결과를 실제 씬에 적용.

```csharp
var composer = new SceneComposer(session);

// 결과 적용
var applyResult = await composer.ApplyCompositionAsync(compositionResult, parent);

// 결과 확인
applyResult.Success;
applyResult.CreatedObjects;  // 생성된 HybridSceneObject 목록
applyResult.ContainerObject; // 부모 GameObject
```

**기능:**
- SceneCompositionResult에서 HybridSceneObject 인스턴스화
- Physics 검증 및 높이 보정
- 메타데이터 자동 설정
- Registry 자동 등록

### 6. HybridSceneObject

GaussianSplatRenderer를 래핑하여 메타데이터와 물리 충돌체를 추가.

```csharp
[RequireComponent(typeof(GaussianSplatRenderer))]
public class HybridSceneObject : MonoBehaviour
{
    ObjectMetadata Metadata { get; }
    GaussianSplatRenderer Renderer { get; }
    Collider ProxyCollider { get; }

    Bounds GetWorldBounds();
    PlacementValidationResult ValidatePlacement(...);
}
```

### 7. SceneObjectRegistry

씬 내 모든 HybridSceneObject를 추적하고 쿼리.

```csharp
registry.Register(obj);
registry.Unregister(obj);

var obj = registry.GetById("obj_001");
var furniture = registry.FindByCategory("furniture");
var nearby = registry.FindInRadius(position, 5f);
```

## Data Flow

### Scene Composition Flow (메인 워크플로우)

```
User Input
├─ Prompt: "A cozy bedroom with bed and desk"
├─ Floor: Auto-detect / Manual
└─ Options: maxObjects, includeDecorations
       │
       ▼
┌──────────────────────┐
│ SceneCompositionRequest │
│ - prompt             │
│ - floorStructure     │
│ - options            │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│  ISplatForgeServer   │
│  (Mock/Real)         │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ SceneCompositionResult │
│ - placements[]       │
│   ├─ objectId        │
│   ├─ assetPath       │
│   ├─ position        │
│   ├─ rotation        │
│   └─ scale           │
│ - reasoning          │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│    SceneComposer     │
│ - Physics validation │
│ - Height correction  │
│ - Instantiation      │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│  HybridSceneObject[] │
│  (Scene에 배치됨)     │
└──────────────────────┘
```

### Object Generation Flow (테스트용)

```
User Input (Prompt)
       │
       ▼
┌──────────────────┐     ┌─────────────────┐
│ GenerationRequest │────▶│ ISplatForgeServer│
└──────────────────┘     └────────┬────────┘
                                  │
                                  ▼
                         ┌─────────────────┐
                         │ GenerationResult │
                         │ - objectId       │
                         │ - plyData        │
                         │ - metadata       │
                         └─────────────────┘
```

## Message Types

### Scene Composition (메인)

| Type | Purpose |
|------|---------|
| `SceneCompositionRequest` | 씬 구성 요청 (prompt, floorStructure, options) |
| `SceneCompositionResult` | 씬 구성 결과 (placements[], reasoning) |
| `SceneObjectPlacement` | 개별 배치 정보 (objectId, assetPath, position, rotation, scale) |
| `FloorStructureData` | 바닥 구조 (boundsMin/Max, floorHeight, walls) |
| `SceneCompositionOptions` | 옵션 (style, quality, maxObjects, includeDecorations) |

### Individual Operations (테스트용)

| Type | Purpose |
|------|---------|
| `GenerationRequest` | 오브젝트 생성 요청 (prompt, quality, seed) |
| `GenerationResult` | 생성 결과 (objectId, plyData, metadata) |
| `LayoutRequest` | 레이아웃 제안 요청 (context, objectIds, constraints) |
| `LayoutSuggestion` | 레이아웃 제안 (placements[], reasoning) |

### Metadata Types

| Type | Purpose |
|------|---------|
| `ObjectMetadata` | 오브젝트 메타데이터 (id, name, category, tags, bounds) |
| `ObjectMetadataData` | 직렬화용 DTO |

## Assembly Dependencies

```
┌─────────────────────────┐
│   SplatForge.Editor     │
│   (Editor Only)         │
└───────────┬─────────────┘
            │ references
            ▼
┌─────────────────────────┐
│   SplatForge.Runtime    │
└───────────┬─────────────┘
            │ references
            ▼
┌─────────────────────────┐
│   GaussianSplatting     │
│   (Plugin)              │
└─────────────────────────┘
```

## Editor Integration

### Menu Items

| Menu Path | Window |
|-----------|--------|
| Tools > SplatForge > Control Panel | SplatForgeMainWindow |
| Tools > SplatForge > Scene Configuration | SceneConfigurationWindow |

### Control Panel 구조

```
┌─────────────────────────────────────┐
│ SplatForge Control Panel    [⚙️]    │
├─────────────────────────────────────┤
│ ▼ Server Connection                 │
│   [Use Mock Server: ✓]              │
│   [Connect] / [Disconnect]          │
├─────────────────────────────────────┤
│ ▼ Scene Composition (메인)          │
│   Floor Bounds: [Auto-detect][Reset]│
│   Preset: [None ▼]                  │
│   Prompt: [___________________]     │
│   Max Objects: [10]                 │
│   Include Decorations: [✓]          │
│   [Compose Scene]                   │
│   ┌─ Preview ─────────────────────┐ │
│   │ Objects: 5                    │ │
│   │ - Double Bed [furniture]      │ │
│   │ - Desk Chair [furniture]      │ │
│   │ ...                           │ │
│   │ [Apply to Scene] [Clear]      │ │
│   └───────────────────────────────┘ │
├─────────────────────────────────────┤
│ ▶ Test: Individual Operations       │
│   (접힌 상태: 개별 생성/레이아웃)    │
└─────────────────────────────────────┘
```

## Extension Points

### 1. Python 서버 구현 추가

```csharp
public class HttpSplatForgeServer : ISplatForgeServer
{
    public async Task<SceneCompositionResult> ComposeSceneAsync(SceneCompositionRequest request)
    {
        // HTTP POST to Python server
        var json = JsonUtility.ToJson(request);
        var response = await httpClient.PostAsync(endpoint + "/compose", json);
        return JsonUtility.FromJson<SceneCompositionResult>(response);
    }
}
```

### 2. 커스텀 Floor Detection

`FloorStructure.DetectFromScene()` 확장하여 다른 레이어 또는 감지 로직 추가

### 3. 새 레이아웃 프리셋 추가

`Samples~/MockLayouts/`에 JSON 파일 추가

## Server API Contract

Unity 클라이언트가 기대하는 서버 API:

### POST /compose
```json
// Request
{
  "prompt": "A cozy bedroom with bed and desk",
  "floorStructure": {
    "boundsMin": {"x": -5, "y": 0, "z": -5},
    "boundsMax": {"x": 5, "y": 0, "z": 5},
    "floorHeight": 0
  },
  "options": {
    "maxObjects": 10,
    "includeDecorations": true
  }
}

// Response
{
  "success": true,
  "placements": [
    {
      "objectId": "bed_001",
      "assetPath": "assets/bed_01",
      "category": "furniture",
      "objectName": "Double Bed",
      "position": {"x": 0, "y": 0, "z": 2},
      "rotation": {"x": 0, "y": 0, "z": 0},
      "scale": {"x": 1, "y": 1, "z": 1}
    }
  ],
  "reasoning": "Placed bed against back wall..."
}
```

## Future Considerations

1. **Real Server Integration**: Python 서버와 HTTP/WebSocket 연동
2. **Asset Import Pipeline**: PLY 파일 자동 임포트 및 변환
3. **Real-time Preview**: 서버 응답 전 예상 배치 미리보기
4. **Undo/Redo Support**: 에디터 작업 취소/재실행
5. **Multi-User Collaboration**: 협업 지원
