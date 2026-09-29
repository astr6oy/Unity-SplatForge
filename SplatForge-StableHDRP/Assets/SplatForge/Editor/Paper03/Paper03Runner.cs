// Paper03Runner.cs — batchmode entry for Paper03 experiment
//
// 그림 7~12 정식 PBR 렌더 파이프라인 (2026-05-07 v5 — 벽 통과 회귀 차단 삼중 안전망):
//   (1) Polyhaven 동봉 PBR 텍스처(diff/nor_gl/arm)를 HDRP/Lit Material 에 직접 바인딩
//   (2) HDRP Volume(Exposure + Tonemapping ACES) + Directional Sun(Lux)
//   (3) 회전 근본 보정: auto_lay_fix 휴리스틱 제거. spec.position.y → 0 클램프 +
//       ground-snap 이 실제 Renderer.bounds.size.y 사용.
//   (4) v4 시나리오별 룸 (T1): bedroom/office 3.0×3.0m, living_room 3.5×3.5m, 벽 H=2.5m.
//   (5) v5 §3.4 iterative push-out (T2): ground-snap 후 OverlapBox pair-wise 검사,
//       작은 객체를 큰 객체로부터 half-overlap 만큼 XZ 평면에서 밀어냄. MAX_ITER 20 회.
//       각 iter 후 가구↔벽 회수 단계 추가 — bounds.min/max 가 룸 한계 (floorHalf-0.05) 를
//       넘으면 안쪽으로 정확히 밀어낸다. resolved_overlaps 카운트를 metric JSON 에 기록.
//   (7) v5 §4.3 벽 통과 회귀 차단 (F1+F2+F3): F1 — per-object bounds-aware 클램프
//       (Renderer.bounds.extents 기반 5cm 마진). F2 — push-out 에 가구↔벽 회수 단계.
//       F3 — CLAMP 보수화 (3m 룸 1.4→1.2, 3.5m 룸 1.65→1.5).
//   (6) v4 카메라 (T3): pos (0, 3.5, -3.0), LookAt (0, 0.4, 0), FOV 55 — diorama
//       elevated angle 로 객체 배치를 위에서 비스듬히 내려다본다.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace SplatForge.EditorPaper03
{
    public static class Paper03Runner
    {
        [Serializable] public class V3 { public float x, y, z; }
        [Serializable] public class Placement
        {
            public string objectId;
            public string objectName;
            public string assetPath;
            public V3 position;
            public V3 rotation;
            public V3 scale;
            public V3 boundsMin;
            public V3 boundsMax;
        }
        [Serializable] public class Spec
        {
            public string scenario;
            public Placement[] placements;
        }

        [Serializable] public class PerObject
        {
            public string objectId;
            public string objectName;
            public string assetPath;
            public bool   asset_loaded;
            public float  adhesion_dist;
            public bool   ground_contact;
            public int    overlap_count;
            public float  pos_x, pos_y, pos_z;
            public string diag;
        }

        [Serializable] public class Metrics
        {
            public string scenario;
            public string condition;
            public int trial;
            public int placement_count;
            public int asset_loaded_count;
            public int ground_contact_count;
            public float floor_adhesion_pct;
            public int total_collisions;
            public int resolved_overlaps;
            public float semantic_proximity;
            public float wall_clock_ms;
            public string render_path;
            public string spec_path;
            public string ts_iso;
            public PerObject[] per_object;
        }

        // FBX 슬러그 → Polyhaven 슬러그 매핑 (텍스처 파일명 prefix).
        static readonly Dictionary<string,string> SlugToPoly = new Dictionary<string,string> {
            { "bed_01",         "GothicBed_01" },
            { "nightstand_01",  "ClassicNightstand_01" },
            { "desk_01",        "metal_office_desk" },
            { "chair_01",       "mid_century_lounge_chair" },
            { "lamp_01",        "desk_lamp_arm_01" },
            { "bookshelf_01",   "Shelf_01" },
            { "plant_01",       "potted_plant_01" },
            { "sofa_01",        "Sofa_01" },
            { "table_01",       "coffee_table_round_01" },
            { "cabinet_01",     "modern_wooden_cabinet" },
            { "wardrobe_01",    "painted_wooden_cabinet_02" },
            { "tv_01",          "Television_01" },
        };

        // Per-asset 회전 보정 (Euler X,Y,Z degrees, world space).
        // 2026-05-07 (R2 vision diagnostic 후): Alfred Opus 4.7 시각 검토 기반 갱신.
        //   bookshelf_01: 유지 (90,0,0) — 정상 세워짐.
        //   plant_01    : 제거 — potted_plant_01 (Y-up 자연, scale 자연 1.4m).
        //   cabinet_01  : (90,0,0) → (0,0,0) — 90° X 가 오히려 눕힘. 원본 그대로.
        //   nightstand_01: 유지 (90,0,0) — 시각 정상.
        //   table_01    : 유지 (90,0,0) — round table 정상.
        //   bed_01      : 신규 (90,0,0) — 벽처럼 수직 → 눕힘.
        //   chair_01    : R2 (90,0,0) — R1 -90X 가 거꾸로 뒤집음 → 180° 보정.
        //   sofa_01     : 신규 (90,0,0) — 등받이 위로 향함 → 세움.
        static readonly Dictionary<string, Vector3> AxisFix = new Dictionary<string, Vector3> {
            { "bookshelf_01",  new Vector3( 90f, 0f, 0f) },
            { "nightstand_01", new Vector3( 90f, 0f, 0f) },
            { "table_01",      new Vector3( 90f, 0f, 0f) },
            { "bed_01",        new Vector3( 90f, 0f, 0f) },
            { "chair_01",      new Vector3( 90f, 0f, 0f) },
            { "sofa_01",       new Vector3( 90f, 0f, 0f) },
        };

        // Per-asset 스케일 보정 (R2 진단 후 추가). instantiate 직후, ground-snap 전에 적용.
        // bounds 측정이 스케일된 결과 사용하도록 순서가 중요하다.
        //   plant_01 : 제거 — potted_plant_01 자연 높이 1.4m 사용.
        //   monitor_01: bounds 3.93m → ~0.7m 으로 축소 (0.18배).
        //   rug_01   : bounds 15.7m → ~3.5m 으로 축소 (0.22배).
        //   cabinet_01: R2 0.5배 — 너무 넓음 (~4m → ~2m).
        static readonly Dictionary<string, float> ASSET_SCALES = new Dictionary<string, float> {
            { "monitor_01", 0.18f },
            { "rug_01",     0.22f },
            { "cabinet_01", 0.5f  },
        };

        // ====================================================================
        // RunSingleAsset — 자산 격리 렌더 (디버깅용, 2026-04-29 추가)
        //   목적: 회전·시각 문제를 자산별 1매 PNG 로 진단.
        //   동일한 룸/조명/HDRP 설정을 유지하되 자산은 origin (0,0,0) 1개만 배치.
        //   카메라는 자산 영웅샷용으로 더 가까이 (1.5, 1.5, -2.5) FOV 35.
        // ====================================================================
        public static void RunSingleAsset()
        {
            string assetName = GetArg("-asset");
            string outputPath = GetArg("-output");
            if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("[Paper03/Iso] missing -asset or -output");
                EditorApplication.Exit(2);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupHdrpVolume();

            // 룸: 4×4 바닥 + 3 벽 (full mode 와 동일).
            const float ROOM_W = 4f, ROOM_D = 4f, WALL_H = 2.5f, WALL_T = 0.05f;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(ROOM_W, 0.05f, ROOM_D);
            floor.transform.position = new Vector3(0, -0.025f, 0);
            var floorMat = MakePlainHdrpLit(new Color(0.55f, 0.42f, 0.30f), 0.30f, 0.0f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            var wallMat = MakePlainHdrpLit(new Color(0.94f, 0.93f, 0.90f), 0.10f, 0.0f);
            var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.localScale = new Vector3(ROOM_W, WALL_H, WALL_T);
            backWall.transform.position = new Vector3(0, WALL_H * 0.5f, ROOM_D * 0.5f);
            backWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.localScale = new Vector3(WALL_T, WALL_H, ROOM_D);
            leftWall.transform.position = new Vector3(-ROOM_W * 0.5f, WALL_H * 0.5f, 0);
            leftWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.localScale = new Vector3(WALL_T, WALL_H, ROOM_D);
            rightWall.transform.position = new Vector3(ROOM_W * 0.5f, WALL_H * 0.5f, 0);
            rightWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // 조명 — full mode 와 동일 Sun + Fill.
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.97f, 0.93f);
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var hdSun = sunGo.AddComponent<HDAdditionalLightData>();
            hdSun.intensity = 30000f;
            hdSun.lightUnit = LightUnit.Lux;
            hdSun.SetShadowResolution(2048);
            hdSun.EnableShadows(true);
            // 그림자 완화 — 0.45 로 약 55% 투명도 적용 (마스터 요청, 2026-04-29).
            hdSun.shadowDimmer = 0.45f;

            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.85f, 0.90f, 1.0f);
            fillGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            var hdFill = fillGo.AddComponent<HDAdditionalLightData>();
            hdFill.intensity = 18000f; // 보조광 강화 (8000 → 18000 lux) — 그림자 영역 보강.
            hdFill.lightUnit = LightUnit.Lux;
            hdFill.EnableShadows(false);

            // 카메라 — 영웅샷 (자산 클로즈업).
            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.05f;
            camGo.transform.position = new Vector3(1.5f, 1.5f, -2.5f);
            camGo.transform.LookAt(new Vector3(0f, 0.5f, 0f));
            camGo.AddComponent<HDAdditionalCameraData>();

            // 자산 1개 인스턴스 — origin 배치, FitToBounds 생략 (원본 스케일 유지).
            string resPath = "MockAssets/" + assetName;
            var prefab = Resources.Load<GameObject>(resPath);
            string diag = "";
            GameObject go = null;
            if (prefab == null)
            {
                Debug.LogError($"[Paper03/Iso] asset not found: {resPath}");
                // 자산 없으면 fallback cube 로 진행 (PNG 는 생성).
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                var fm = MakePlainHdrpLit(new Color(0.8f, 0.2f, 0.2f), 0.25f, 0.0f);
                go.GetComponent<Renderer>().sharedMaterial = fm;
                diag = "asset_missing;";
            }
            else
            {
                go = UnityEngine.Object.Instantiate(prefab);
                go.transform.rotation = Quaternion.identity;

                // AxisFix 적용 (full mode 와 동일).
                if (AxisFix.TryGetValue(assetName, out var fix))
                {
                    go.transform.Rotate(fix.x, fix.y, fix.z, Space.World);
                    diag += "axisfix;";
                }

                // ASSET_SCALES 적용 — ground-snap 전에 수행해야 bounds 가 정확.
                if (ASSET_SCALES.TryGetValue(assetName, out var scl))
                {
                    go.transform.localScale *= scl;
                    diag += $"scale:{scl};";
                }

                ApplyHdrpPbr(go, assetName, ref diag);
            }
            go.name = assetName;

            // origin 배치 + ground-snap (Renderer.bounds.min.y = 0).
            go.transform.position = Vector3.zero;
            var rb = ComputeRendererBounds(go);
            float dy = -rb.min.y;
            go.transform.position += new Vector3(0f, dy, 0f);

            Physics.SyncTransforms();

            // 디버그 정보 — bounds 측정 후 Console 에 dump.
            var rbAfter = ComputeRendererBounds(go);
            Debug.Log($"[Paper03/Iso] asset={assetName} pos=({go.transform.position.x:F3},{go.transform.position.y:F3},{go.transform.position.z:F3}) bounds_size=({rbAfter.size.x:F3},{rbAfter.size.y:F3},{rbAfter.size.z:F3}) bounds_min=({rbAfter.min.x:F3},{rbAfter.min.y:F3},{rbAfter.min.z:F3}) diag={diag}");

            CaptureCamera(cam, outputPath, 1280, 720);
            Debug.Log($"[Paper03/Iso] DONE asset={assetName} png={outputPath}");
            EditorApplication.Exit(0);
        }

        public static void Run()
        {
            var t0 = DateTime.UtcNow;
            string layoutSpec = GetArg("-layoutSpec");
            string outputDir  = GetArg("-outputDir");
            string condition  = GetArg("-condition") ?? "full";
            int trial         = int.TryParse(GetArg("-trial"), out var tr) ? tr : 1;

            if (string.IsNullOrEmpty(layoutSpec) || string.IsNullOrEmpty(outputDir))
            {
                Debug.LogError("[Paper03] missing -layoutSpec or -outputDir");
                EditorApplication.Exit(2);
                return;
            }
            Directory.CreateDirectory(outputDir);

            Spec spec;
            try { spec = JsonUtility.FromJson<Spec>(File.ReadAllText(layoutSpec)); }
            catch (Exception e) { Debug.LogError($"[Paper03] spec parse fail: {e.Message}"); EditorApplication.Exit(3); return; }

            bool applyGroundSnap = (condition != "llm_only");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupHdrpVolume();

            // T1 (2026-05-07 v5) — 시나리오별 룸 크기 + 보수 CLAMP (벽 통과 회귀 차단):
            //   bedroom : 3.0×3.0m  → CLAMP ±1.2 (v4 1.4 → 보수 0.2m)
            //   office  : 3.0×3.0m  → CLAMP ±1.2
            //   living  : 3.5×3.5m  → CLAMP ±1.5 (v4 1.65 → 보수 0.15m)
            //   기타    : 4.0×4.0m fallback (기존 동작 유지) → CLAMP ±1.7
            // 벽 높이 2.5m 고정. 벽 두께 0.05m. CLAMP 는 객체 중심에 대한 한계.
            // F1 (per-object bounds-aware) + F2 (push-out wall awareness, MAX_ITER 20) +
            // F3 (보수 CLAMP) 삼중 안전망 — Renderer.bounds.extents 기반 per-object 마진 5cm.
            float ROOM_W, ROOM_D;
            float CLAMP_X, CLAMP_Z;
            string scn = (spec.scenario ?? "").ToLowerInvariant();
            if (scn == "cozy_bedroom" || scn == "modern_office")
            {
                ROOM_W = 3.0f; ROOM_D = 3.0f;
                CLAMP_X = 1.2f; CLAMP_Z = 1.2f;
            }
            else if (scn == "living_room")
            {
                ROOM_W = 3.5f; ROOM_D = 3.5f;
                CLAMP_X = 1.5f; CLAMP_Z = 1.5f;
            }
            else
            {
                ROOM_W = 4.0f; ROOM_D = 4.0f;
                CLAMP_X = 1.7f; CLAMP_Z = 1.7f;
            }
            const float WALL_H = 2.5f;
            const float WALL_T = 0.05f;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(ROOM_W, 0.05f, ROOM_D);
            floor.transform.position = new Vector3(0, -0.025f, 0);
            var floorMat = MakePlainHdrpLit(new Color(0.55f, 0.42f, 0.30f), 0.30f, 0.0f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            var wallMat = MakePlainHdrpLit(new Color(0.94f, 0.93f, 0.90f), 0.10f, 0.0f);

            var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.localScale = new Vector3(ROOM_W, WALL_H, WALL_T);
            backWall.transform.position = new Vector3(0, WALL_H * 0.5f, ROOM_D * 0.5f);
            backWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.localScale = new Vector3(WALL_T, WALL_H, ROOM_D);
            leftWall.transform.position = new Vector3(-ROOM_W * 0.5f, WALL_H * 0.5f, 0);
            leftWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.localScale = new Vector3(WALL_T, WALL_H, ROOM_D);
            rightWall.transform.position = new Vector3(ROOM_W * 0.5f, WALL_H * 0.5f, 0);
            rightWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // furniture 클램프 영역은 위 시나리오별 분기에서 결정됨.

            // Sun (HDRP intensity in Lux). 정오 햇빛 ~100,000 lux 는 너무 강하므로
            // 실내 창문 들어오는 일조량 ~30,000 lux 으로 설정.
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.97f, 0.93f);
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var hdSun = sunGo.AddComponent<HDAdditionalLightData>();
            hdSun.intensity = 30000f;
            hdSun.lightUnit = LightUnit.Lux;
            hdSun.SetShadowResolution(2048);
            hdSun.EnableShadows(true);
            // 그림자 완화 — 0.45 로 약 55% 투명도 적용 (마스터 요청, 2026-04-29).
            hdSun.shadowDimmer = 0.45f;

            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.85f, 0.90f, 1.0f);
            fillGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            var hdFill = fillGo.AddComponent<HDAdditionalLightData>();
            hdFill.intensity = 18000f; // 보조광 강화 (8000 → 18000 lux) — 그림자 영역 보강.
            hdFill.lightUnit = LightUnit.Lux;
            hdFill.EnableShadows(false);

            // T3 (v4) — elevated diorama. 위에서 비스듬히 내려다보는 시점으로
            //   객체 배치(특히 가구 사이 공간)을 더 명확히 보여준다.
            //   pos (0, 3.5, -3.0), LookAt (0, 0.4, 0), FOV 55.
            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.05f;
            camGo.transform.position = new Vector3(0f, 3.5f, -3.0f);
            camGo.transform.LookAt(new Vector3(0f, 0.4f, 0f));
            camGo.AddComponent<HDAdditionalCameraData>();

            var spawned = new List<GameObject>();
            var assetLoadedFlags = new List<bool>();
            var diagMessages = new List<string>();
            int loadedCount = 0;
            foreach (var p in spec.placements)
            {
                var size = new Vector3(
                    Mathf.Max(0.2f, p.boundsMax.x - p.boundsMin.x),
                    Mathf.Max(0.2f, p.boundsMax.y - p.boundsMin.y),
                    Mathf.Max(0.2f, p.boundsMax.z - p.boundsMin.z));
                if (size.x < 0.01f) size = new Vector3(0.6f, 0.6f, 0.6f);

                GameObject go = null;
                bool loaded = false;
                string diag = "";
                string resPath = NormalizeAssetPath(p.assetPath);
                string slug = ExtractSlug(resPath);
                if (!string.IsNullOrEmpty(resPath))
                {
                    var prefab = Resources.Load<GameObject>(resPath);
                    if (prefab != null)
                    {
                        go = UnityEngine.Object.Instantiate(prefab);
                        // 회전 정규화: root 만 reset. child transform 은 메시 hierarchy 의
                        // 일부이므로 건드리면 메시가 무너짐.
                        go.transform.rotation = Quaternion.identity;

                        FitToBounds(go, size);

                        // auto_lay_fix 휴리스틱 제거 (2026-05-07): rug/tv 처럼 본질적으로
                        // 평평한 객체를 -90X 회전시켜 세우는 부작용 발생. Polyhaven FBX 들은
                        // import time 에 이미 정상 방향이며 ground-snap 으로 대응.

                        if (!string.IsNullOrEmpty(slug) && AxisFix.TryGetValue(slug, out var fix))
                        {
                            go.transform.Rotate(fix.x, fix.y, fix.z, Space.World);
                            diag += "override_axis;";
                            FitToBounds(go, size);
                        }

                        // ASSET_SCALES — FitToBounds 후 적용 (FitToBounds 가 균일 스케일을
                        // 강제하므로 추가 보정으로 의도된 비율 부여). ground-snap 전에 수행.
                        if (!string.IsNullOrEmpty(slug) && ASSET_SCALES.TryGetValue(slug, out var scl))
                        {
                            go.transform.localScale *= scl;
                            diag += $"scale:{scl};";
                        }

                        ApplyHdrpPbr(go, slug, ref diag);

                        loaded = true;
                        loadedCount++;
                    }
                }
                if (go == null)
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = size;
                    var mat = MakePlainHdrpLit(CategoryFallbackColor(p.objectName ?? p.objectId ?? "x"), 0.25f, 0.0f);
                    go.GetComponent<Renderer>().sharedMaterial = mat;
                    diag += "fallback_cube;";
                }
                go.name = string.IsNullOrEmpty(p.objectName) ? p.objectId : p.objectName;

                // X/Z 클램프 — 룸 내부(±CLAMP)로 강제. LLM/random 이 벽 밖 좌표를 줘도 안전.
                float clampedX = Mathf.Clamp(p.position.x, -CLAMP_X, CLAMP_X);
                float clampedZ = Mathf.Clamp(p.position.z, -CLAMP_Z, CLAMP_Z);

                // Y 클램프 (H1 fix): LLM/mock 이 가끔 "데스크 위"를 의도해 y>0 을 넣지만
                // ground-snap 공식 y = position.y + size.y/2 와 충돌해 객체가 떠 버림.
                // 정책: spec.position.y 를 0 으로 클램프하고 ground-snap 이 단독으로
                // 바닥 안착을 처리한다 (실제 Renderer.bounds 사용해 pivot offset 까지 정확히).
                if (applyGroundSnap)
                {
                    // 임시 위치 (pivot 기준) 에 두고 실제 렌더러 bounds 를 측정.
                    go.transform.position = new Vector3(clampedX, 0f, clampedZ);
                    go.transform.Rotate(0f, p.rotation.y, 0f, Space.World);

                    var rb = ComputeRendererBounds(go);
                    // bounds.min.y 가 0 이 되도록 y 보정. (pivot != bottom 일 때 정확).
                    float dy = -rb.min.y;
                    go.transform.position += new Vector3(0f, dy, 0f);
                }
                else
                {
                    // llm_only — 보정 없음, spec 그대로.
                    go.transform.position = new Vector3(clampedX, p.position.y, clampedZ);
                    go.transform.Rotate(0f, p.rotation.y, 0f, Space.World);
                }

                if (go.GetComponent<Collider>() == null)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.size = Vector3.one;
                }
                spawned.Add(go);
                assetLoadedFlags.Add(loaded);
                diagMessages.Add(diag);
            }

            Physics.SyncTransforms();

            // F1 (2026-05-07 v5) — bounds-aware per-object 벽 통과 차단.
            // 객체 중심을 ±CLAMP 로 자르더라도 Renderer.bounds.extents 가 (CLAMP, floorHalf)
            // 사이 거리보다 크면 가장자리가 벽을 뚫는다. 객체별 bounds 측정 후 CLAMP 보다
            // 더 보수적인 마진으로 재클램프 (5cm 안전여유).
            float floorHalfX = ROOM_W * 0.5f;
            float floorHalfZ = ROOM_D * 0.5f;
            for (int i = 0; i < spawned.Count; i++)
            {
                var go = spawned[i];
                var rb = ComputeRendererBounds(go);
                float marginX = Mathf.Min(CLAMP_X, floorHalfX - rb.extents.x - 0.05f);
                float marginZ = Mathf.Min(CLAMP_Z, floorHalfZ - rb.extents.z - 0.05f);
                marginX = Mathf.Max(marginX, 0.0f);
                marginZ = Mathf.Max(marginZ, 0.0f);
                var pos = go.transform.position;
                pos.x = Mathf.Clamp(pos.x, -marginX, marginX);
                pos.z = Mathf.Clamp(pos.z, -marginZ, marginZ);
                go.transform.position = pos;
            }
            Physics.SyncTransforms();

            // T2 (§3.4 LayoutValidator iterative push-out) — pair-wise OverlapBox 기반
            // 충돌 해소. 작은 객체(Renderer.bounds.size 의 합이 작은 쪽)를 큰 객체로부터
            // half-overlap 거리만큼 XZ 평면 외측 방향으로 밀어내고, 룸 클램프 후 재측정.
            // 충돌이 사라지거나 최대 10회 반복까지 수행. llm_only 절제 조건 외 모두 적용.
            // T2 (v5) — pair-wise OverlapBox push-out + wall awareness. MAX_ITER 20.
            // (a) 가구↔가구 충돌은 작은 객체를 큰 객체로부터 half-overlap 외측으로 밀어낸다.
            //     룸 클램프는 per-object bounds 마진 (F1 과 동일 공식) 으로 대체 — CLAMP 직접
            //     사용 시 큰 객체가 벽을 뚫는 회귀 발생.
            // (b) 가구↔벽 충돌은 객체 bounds.min/max 가 floor 한계선을 넘는지 검사하고,
            //     초과분만큼 룸 안쪽으로 밀어낸다. F1 이 이미 한 번 처리했지만 push-out 결과로
            //     벽 밖으로 밀려난 경우 회수한다.
            int resolvedOverlaps = 0;
            if (condition != "llm_only")
            {
                const int MAX_ITER = 20;
                for (int it = 0; it < MAX_ITER; it++)
                {
                    bool anyResolved = false;

                    // (a) 가구↔가구 push-out
                    for (int i = 0; i < spawned.Count; i++)
                    {
                        for (int j = i + 1; j < spawned.Count; j++)
                        {
                            var ga = spawned[i];
                            var gb = spawned[j];
                            var ba = ComputeRendererBounds(ga);
                            var bb = ComputeRendererBounds(gb);
                            if (!ba.Intersects(bb)) continue;
                            float dx = bb.center.x - ba.center.x;
                            float dz = bb.center.z - ba.center.z;
                            float overlapX = (ba.extents.x + bb.extents.x) - Mathf.Abs(dx);
                            float overlapZ = (ba.extents.z + bb.extents.z) - Mathf.Abs(dz);
                            if (overlapX <= 0f && overlapZ <= 0f) continue;
                            Vector3 dir;
                            float push;
                            if (overlapX > 0f && (overlapZ <= 0f || overlapX < overlapZ))
                            {
                                dir = new Vector3(dx >= 0f ? 1f : -1f, 0f, 0f);
                                push = overlapX * 0.5f + 0.001f;
                            }
                            else
                            {
                                dir = new Vector3(0f, 0f, dz >= 0f ? 1f : -1f);
                                push = overlapZ * 0.5f + 0.001f;
                            }
                            float sizeA = ba.size.x + ba.size.z;
                            float sizeB = bb.size.x + bb.size.z;
                            GameObject mover; Vector3 dirMover;
                            if (sizeA < sizeB) { mover = ga; dirMover = -dir; }
                            else                { mover = gb; dirMover =  dir; }
                            var pp = mover.transform.position;
                            // per-object 마진 사용 (F1 공식과 동일).
                            var rbm = ComputeRendererBounds(mover);
                            float mvMarginX = Mathf.Max(0f, Mathf.Min(CLAMP_X, floorHalfX - rbm.extents.x - 0.05f));
                            float mvMarginZ = Mathf.Max(0f, Mathf.Min(CLAMP_Z, floorHalfZ - rbm.extents.z - 0.05f));
                            float nx = Mathf.Clamp(pp.x + dirMover.x * push, -mvMarginX, mvMarginX);
                            float nz = Mathf.Clamp(pp.z + dirMover.z * push, -mvMarginZ, mvMarginZ);
                            mover.transform.position = new Vector3(nx, pp.y, nz);
                            anyResolved = true;
                            resolvedOverlaps++;
                        }
                    }
                    Physics.SyncTransforms();

                    // (b) 가구↔벽 회수: bounds 가 룸 한계 ±(floorHalf - 0.05) 를 넘으면
                    //     안쪽으로 정확히 밀어낸다.
                    for (int i = 0; i < spawned.Count; i++)
                    {
                        var go = spawned[i];
                        var rbb = ComputeRendererBounds(go);
                        float wallLimitX = floorHalfX - 0.05f;
                        float wallLimitZ = floorHalfZ - 0.05f;
                        float dxPenL = -wallLimitX - rbb.min.x; // >0 if penetrating left wall
                        float dxPenR = rbb.max.x - wallLimitX;  // >0 if penetrating right wall
                        float dzPenB = -wallLimitZ - rbb.min.z; // back wall (+Z) check is max
                        float dzPenF = rbb.max.z - wallLimitZ;
                        var pp = go.transform.position;
                        float adjX = 0f, adjZ = 0f;
                        if (dxPenL > 0f) adjX += dxPenL;
                        if (dxPenR > 0f) adjX -= dxPenR;
                        if (dzPenB > 0f) adjZ += dzPenB;
                        if (dzPenF > 0f) adjZ -= dzPenF;
                        if (Mathf.Abs(adjX) > 1e-4f || Mathf.Abs(adjZ) > 1e-4f)
                        {
                            go.transform.position = new Vector3(pp.x + adjX, pp.y, pp.z + adjZ);
                            anyResolved = true;
                            resolvedOverlaps++;
                        }
                    }
                    Physics.SyncTransforms();

                    if (!anyResolved) break;
                }
            }

            var perList = new List<PerObject>();
            int groundCount = 0, totalOverlaps = 0;
            for (int i = 0; i < spec.placements.Length; i++)
            {
                var go = spawned[i];
                var p = spec.placements[i];
                var col = go.GetComponent<Collider>();
                var bounds = col.bounds;
                var rayOrigin = new Vector3(go.transform.position.x, bounds.min.y + 0.01f, go.transform.position.z);
                float adhesion = 999f;
                bool grounded = false;
                if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 5f))
                {
                    if (hit.collider.gameObject == floor)
                    {
                        adhesion = Mathf.Abs(rayOrigin.y - hit.point.y);
                        grounded = true;
                    }
                }
                if (grounded) groundCount++;

                int overlap = 0;
                var hits = Physics.OverlapBox(bounds.center, bounds.extents * 0.97f, go.transform.rotation);
                foreach (var h in hits)
                {
                    if (h.gameObject == go) continue;
                    if (h.gameObject == floor) continue;
                    if (h.gameObject == backWall || h.gameObject == leftWall || h.gameObject == rightWall) continue;
                    overlap++;
                }
                totalOverlaps += overlap;

                perList.Add(new PerObject {
                    objectId = p.objectId,
                    objectName = p.objectName,
                    assetPath = p.assetPath,
                    asset_loaded = assetLoadedFlags[i],
                    adhesion_dist = adhesion,
                    ground_contact = grounded,
                    overlap_count = overlap,
                    pos_x = go.transform.position.x,
                    pos_y = go.transform.position.y,
                    pos_z = go.transform.position.z,
                    diag = diagMessages[i],
                });
            }

            float semantic = ComputeSemanticProximity(spec.scenario, spawned);

            string pngPath = Path.Combine(outputDir, $"trial_{trial}.png");
            CaptureCamera(cam, pngPath, 1280, 720);

            float pct = spec.placements.Length == 0 ? 0f
                : 100f * CountAdhered(perList) / spec.placements.Length;
            var metrics = new Metrics {
                scenario = spec.scenario,
                condition = condition,
                trial = trial,
                placement_count = spec.placements.Length,
                asset_loaded_count = loadedCount,
                ground_contact_count = groundCount,
                floor_adhesion_pct = pct,
                total_collisions = totalOverlaps,
                resolved_overlaps = resolvedOverlaps,
                semantic_proximity = semantic,
                wall_clock_ms = (float)(DateTime.UtcNow - t0).TotalMilliseconds,
                render_path = pngPath,
                spec_path = layoutSpec,
                ts_iso = DateTime.UtcNow.ToString("o"),
                per_object = perList.ToArray(),
            };
            string outJson = Path.Combine(outputDir, $"trial_{trial}.json");
            File.WriteAllText(outJson, JsonUtility.ToJson(metrics, true));
            Debug.Log($"[Paper03] DONE scenario={spec.scenario} cond={condition} trial={trial} loaded={loadedCount}/{spec.placements.Length} adh%={pct:F1} sem={semantic:F2} resolved={resolvedOverlaps} png={pngPath}");

            EditorApplication.Exit(0);
        }

        // HDRP Volume — Auto-Exposure(Histogram) + ACES.
        // 빈 씬에서 Auto-Exposure 가 흔들리지 않도록 Histogram percent 를 좁게 잡고,
        // 노출 보정(compensation)으로 은은하게 밝힘.
        static void SetupHdrpVolume()
        {
            var volGo = new GameObject("GlobalVolume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 0;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = profile;

            var exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Automatic);
            exposure.meteringMode.Override(MeteringMode.CenterWeighted);
            exposure.compensation.Override(2.0f);
            exposure.limitMin.Override(-2f);
            exposure.limitMax.Override(14f);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            var ve = profile.Add<VisualEnvironment>(true);
            ve.skyType.Override((int)SkyType.PhysicallyBased);
            ve.skyAmbientMode.Override(SkyAmbientMode.Dynamic);

            var pbsky = profile.Add<PhysicallyBasedSky>(true);
            // 기본값 사용 — Earth-like atmosphere
        }

        static Material MakePlainHdrpLit(Color baseColor, float smoothness, float metallic)
        {
            var sh = Shader.Find("HDRP/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
            if (m.HasProperty("_Color"))     m.SetColor("_Color", baseColor);
            if (m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic"))  m.SetFloat("_Metallic", metallic);
            return m;
        }

        // PBR 텍스처 → HDRP/Lit 바인딩.
        // {slug}_diff_2k.jpg   → _BaseColorMap (sRGB)
        // {slug}_nor_gl_2k.jpg → _NormalMap (Linear)
        // {slug}_arm_2k.jpg    → _MaskMap (HDRP: R=Metal, G=AO, B=detail, A=Smoothness)
        //   Polyhaven arm: R=AO, G=Roughness, B=Metal → 채널 재배치.
        static void ApplyHdrpPbr(GameObject go, string slug, ref string diag)
        {
            string polySlug = null;
            if (!string.IsNullOrEmpty(slug)) SlugToPoly.TryGetValue(slug, out polySlug);

            Texture2D diff = null, nor = null, mask = null;
            if (!string.IsNullOrEmpty(polySlug))
            {
                diff = Resources.Load<Texture2D>($"MockAssets/textures/{polySlug}_diff_2k");
                nor  = Resources.Load<Texture2D>($"MockAssets/textures/{polySlug}_nor_gl_2k");
                var arm = Resources.Load<Texture2D>($"MockAssets/textures/{polySlug}_arm_2k");
                if (arm != null) mask = BuildMaskMapFromArm(arm);
                string flags = (diff!=null?"d":"") + (nor!=null?"n":"") + (arm!=null?"a":"");
                diag += "pbr:" + polySlug + "(" + flags + ");";
            }
            else
            {
                diag += "pbr_no_slug;";
            }

            var sh = Shader.Find("HDRP/Lit");
            if (sh == null) { diag += "no_hdrp_lit;"; return; }

            var rends = go.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                var m = new Material(sh);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
                if (diff != null && m.HasProperty("_BaseColorMap")) m.SetTexture("_BaseColorMap", diff);
                // Normal/Mask 는 import settings 가 sRGB/Linear 이슈로 색조 왜곡을 일으킬 수
                // 있어 일단 BaseColor 만 바인딩한다. 시각이 정상이면 단계적으로 normal·mask 추가.
                // (혹은 .meta 파일 강제 생성으로 textureType=NormalMap, sRGB=false 적용 후 활성화.)
                if (nor != null && m.HasProperty("_NormalMap")) m.SetTexture("_NormalMap", nor);
                if (mask != null && m.HasProperty("_MaskMap")) m.SetTexture("_MaskMap", mask);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.4f);
                if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", 0.0f);
                r.sharedMaterial = m;
            }
        }

        static Texture2D BuildMaskMapFromArm(Texture2D arm)
        {
            var rt = RenderTexture.GetTemporary(arm.width, arm.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(arm, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var src = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, false, true);
            src.ReadPixels(new Rect(0, 0, arm.width, arm.height), 0, 0);
            src.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            var px = src.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                float ao    = px[i].r;
                float rough = px[i].g;
                float metal = px[i].b;
                float smooth = 1f - rough;
                px[i] = new Color(metal, ao, 0f, smooth);
            }
            var dst = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, true, true);
            dst.SetPixels(px);
            dst.Apply(true);
            UnityEngine.Object.DestroyImmediate(src);
            return dst;
        }

        static string ExtractSlug(string resPath)
        {
            if (string.IsNullOrEmpty(resPath)) return null;
            int slash = resPath.LastIndexOf('/');
            return slash < 0 ? resPath : resPath.Substring(slash + 1);
        }

        static Bounds ComputeRendererBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.6f);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        static Color CategoryFallbackColor(string nameKey)
        {
            string n = (nameKey ?? "").ToLowerInvariant();
            if (n.Contains("bed") || n.Contains("sofa")) return new Color(0.45f, 0.30f, 0.24f);
            if (n.Contains("plant")) return new Color(0.22f, 0.45f, 0.25f);
            if (n.Contains("monitor") || n.Contains("tv")) return new Color(0.10f, 0.10f, 0.12f);
            return new Color(0.50f, 0.45f, 0.40f);
        }

        static string NormalizeAssetPath(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            string s = raw.Trim().Replace("\\", "/");
            if (s.StartsWith("/")) s = s.Substring(1);
            if (s.StartsWith("furniture/") || s.StartsWith("decorations/") || s.StartsWith("decoration/"))
            {
                int slash = s.IndexOf('/');
                s = "MockAssets/" + s.Substring(slash + 1);
            }
            if (!s.StartsWith("MockAssets/")) s = "MockAssets/" + s;
            int dot = s.LastIndexOf('.');
            int slashLast = s.LastIndexOf('/');
            if (dot > slashLast) s = s.Substring(0, dot);
            return s;
        }

        static void FitToBounds(GameObject go, Vector3 targetSize)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            Vector3 cur = b.size;
            if (cur.x <= 0.001f || cur.y <= 0.001f || cur.z <= 0.001f) return;
            // 2026-05-07: 균일 min 스케일 유지 — 메시 비례 보존이 우선.
            // 일부 가구가 target 보다 작아질 수 있지만 셰어링/뒤틀림 없음.
            float sx = targetSize.x / cur.x;
            float sy = targetSize.y / cur.y;
            float sz = targetSize.z / cur.z;
            float s = Mathf.Min(sx, Mathf.Min(sy, sz));
            go.transform.localScale = go.transform.localScale * s;
        }

        static int CountAdhered(List<PerObject> rows)
        {
            int n = 0;
            foreach (var r in rows)
                if (r.adhesion_dist < 0.05f) n++;
            return n;
        }

        static float ComputeSemanticProximity(string scenario, List<GameObject> objs)
        {
            (string a, string b, float target)[] pairs;
            switch ((scenario ?? "").ToLowerInvariant())
            {
                case "cozy_bedroom":
                    pairs = new (string, string, float)[] {
                        ("bed", "nightstand", 1.5f),
                        ("nightstand", "lamp", 0.6f),
                        ("desk", "chair", 0.8f),
                    }; break;
                case "modern_office":
                    pairs = new (string, string, float)[] {
                        ("desk", "chair", 0.8f),
                        ("desk", "monitor", 0.7f),
                        ("desk", "lamp", 0.7f),
                    }; break;
                case "living_room":
                    pairs = new (string, string, float)[] {
                        ("sofa", "coffee_table", 1.2f),
                        ("sofa", "tv", 3.0f),
                        ("armchair", "coffee_table", 1.5f),
                    }; break;
                default:
                    pairs = new (string, string, float)[0]; break;
            }
            if (pairs.Length == 0) return 0f;
            float sum = 0f; int n = 0;
            foreach (var pr in pairs)
            {
                var oa = FindByKeyword(objs, pr.a);
                var ob = FindByKeyword(objs, pr.b);
                if (oa == null || ob == null) continue;
                float d = Vector3.Distance(oa.transform.position, ob.transform.position);
                float score = Mathf.Exp(-Mathf.Abs(d - pr.target));
                sum += score; n++;
            }
            return n == 0 ? 0f : sum / n;
        }

        static GameObject FindByKeyword(List<GameObject> objs, string kw)
        {
            foreach (var go in objs)
                if (go.name != null && go.name.ToLowerInvariant().Contains(kw)) return go;
            return null;
        }

        static void CaptureCamera(Camera cam, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR);
            cam.targetTexture = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
        }

        static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == name && i + 1 < args.Length) return args[i + 1];
            return null;
        }
    }
}