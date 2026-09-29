# MockAssets — Credits & Licenses

Phase 2 (2026-04-29) Paper03 실험용으로 통합된 무료 가구 자산 목록.
모든 메시 자산은 **CC0 (퍼블릭 도메인)** 라이선스 또는 그에 준하는 무제약
라이선스이며, 출처를 명시할 의무는 없으나 재현성을 위해 기록한다.

## Polyhaven Models (CC0)

Source: https://polyhaven.com/models — License: CC0 1.0 Universal
Format: FBX 7.x (2k texture variant; 텍스처는 별도 미동봉 — 메시 지오메트리만 사용)

| MockAssets 슬러그 | Polyhaven 슬러그 | 시나리오 | FBX 크기 | 다운로드 URL |
|-------------------|-------------------|----------|----------|--------------|
| bed_01            | GothicBed_01                   | bedroom              | 920 KB   | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/GothicBed_01/GothicBed_01_2k.fbx |
| nightstand_01     | ClassicNightstand_01           | bedroom              | 100 KB   | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/ClassicNightstand_01/ClassicNightstand_01_2k.fbx |
| desk_01           | metal_office_desk              | bedroom, office      | 240 KB   | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/metal_office_desk/metal_office_desk_2k.fbx |
| chair_01          | mid_century_lounge_chair       | bedroom, office, living | 211 KB | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/mid_century_lounge_chair/mid_century_lounge_chair_2k.fbx |
| lamp_01           | desk_lamp_arm_01               | bedroom, living      | 2.3 MB   | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/desk_lamp_arm_01/desk_lamp_arm_01_2k.fbx |
| bookshelf_01      | Shelf_01                       | office               | 27 KB    | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/Shelf_01/Shelf_01_2k.fbx |
| plant_01          | potted_plant_01                | office, living       | 5.4 MB   | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/potted_plant_01/potted_plant_01_2k.fbx |
| sofa_01           | Sofa_01                        | living               | 191 KB   | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/Sofa_01/Sofa_01_2k.fbx |
| table_01          | coffee_table_round_01          | living               | 141 KB   | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/coffee_table_round_01/coffee_table_round_01_2k.fbx |
| cabinet_01        | modern_wooden_cabinet          | living (TV stand)    | 800 KB   | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/modern_wooden_cabinet/modern_wooden_cabinet_2k.fbx |
| wardrobe_01       | painted_wooden_cabinet_02      | bedroom              | 58 KB    | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/painted_wooden_cabinet_02/painted_wooden_cabinet_02_2k.fbx |
| tv_01             | Television_01                  | living               | 79 KB    | https://dl.polyhaven.org/file/ph-assets/Models/fbx/2k/Television_01/Television_01_2k.fbx |

**합계**: 10개 메시, 약 5.94 MB

## Kenney Furniture Kit (CC0)

Source: https://kenney.nl/assets/furniture-kit — License: CC0 (퍼블릭 도메인)
Pack version: Furniture Kit 2.0 (2018-10-20). FBX/OBJ/GLTF 동시 동봉.

| MockAssets 슬러그 | Kenney 원본 | 시나리오 | FBX 크기 |
|-------------------|-------------|----------|----------|
| monitor_01        | computerScreen.fbx | office | 21 KB |
| rug_01            | rugRectangle.fbx   | living | 14 KB |

다운로드 ZIP: https://kenney.nl/media/pages/assets/furniture-kit/e56d2a9828-1677580847/kenney_furniture-kit.zip

## 폴백 — 모두 해소됨 (Phase 2 통합 완료)

이전 폴백 4슬롯 (wardrobe / monitor / tv / rug) 은 모두 실제 메시로 대체됨.
- wardrobe_01 → Polyhaven painted_wooden_cabinet_02
- monitor_01  → Kenney computerScreen
- tv_01       → Polyhaven Television_01
- rug_01      → Kenney rugRectangle

프리미티브 큐브 폴백은 카탈로그 외 슬러그(향후 신규 시나리오) 전용으로 유지된다.

## Sketchfab 시도

`wall_lamp` by plaggy 등 Sketchfab CC0 자산은 헤드리스 다운로드 시 로그인
요구로 차단되어 이번 Phase 에서는 스킵. Polyhaven 의 `desk_lamp_arm_01` 으로
충분히 대체된다.

## KIRI 3DGS 자산 (보조 — 향후 하이브리드 데모용)

현재 Unity 프로젝트에서 발견되는 .asset 형식 GS 자산:
- `Assets/Samples/Statue/Statue.asset` — KIRI 마스터 라이선스
- `Assets/Samples/Bart/sample.asset` — KIRI 마스터 라이선스

Phase 3 paper integration 단계에서 living_room 시나리오의 `statue_01` 또는
`bart_01` 슬롯으로 hybrid mesh+3DGS 데모에 활용할 예정.

## Disk Footprint

`Assets/SplatForge/Resources/MockAssets/` — 약 5.94 MB (10× FBX, 텍스처 미동봉)
## Polyhaven PBR Textures (CC0) — 2026-05-07 추가

각 메시에 대해 `textures/` 하위에 diff(BaseColor) + nor_gl(Normal, OpenGL convention)
+ arm(AO/Roughness/Metallic packed) 3장의 2k jpg 동봉. URL 패턴:
`https://dl.polyhaven.org/file/ph-assets/Models/jpg/2k/<slug>/<slug>_<diff|nor_gl|arm>_2k.jpg`

- `textures/` 36 파일 (12 메시 × 3 종)
- 합계 약 50 MB

`.meta` 파일을 명시 생성하여 import settings 보장:
- diff: `textureType=Default, sRGBTexture=1`
- nor_gl: `textureType=NormalMap, sRGBTexture=0`
- arm:  `textureType=Default, sRGBTexture=0, isReadable=1` (런타임에 채널 재배치)

런타임 바인딩 — `Paper03Runner.ApplyHdrpPbr()`:
- diff → HDRP/Lit `_BaseColorMap`
- nor_gl → `_NormalMap`
- arm → `BuildMaskMapFromArm()` 으로 채널 재배치 후 `_MaskMap`
  - Polyhaven arm: R=AO, G=Rough, B=Metal
  - HDRP MaskMap: R=Metal, G=AO, B=detail, A=Smoothness=1-Roughness

