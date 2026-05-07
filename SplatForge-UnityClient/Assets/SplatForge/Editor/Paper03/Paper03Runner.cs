// Paper03Runner.cs — batchmode entry for Paper03 experiment
//
// 레이아웃 spec.json 을 읽어 (1) 실제 메시 프리팹/리소스 또는 프리미티브 큐브를
// 바닥(20x20) 위에 배치하고, (2) 레이캐스트 기반 부착도/겹침 측정 + 시멘틱
// 근접도 점수를 계산한 뒤, (3) PNG 캡처 + metric JSON 을 기록한다.
//
// 호출:
//   Unity -batchmode -projectPath ... -executeMethod SplatForge.EditorPaper03.Paper03Runner.Run
//         -layoutSpec /abs/path/spec.json
//         -outputDir  /abs/path/results/<scenario>/<condition>
//         -trial      1
//         -condition  full|llm_only|random_physics
//         -logFile    /abs/path/log
//         -quit -nographics
//
// spec.json 스키마 (assetPath 추가):
//   { "scenario": "cozy_bedroom", "placements": [
//       { "objectId": "...", "objectName": "...", "assetPath": "MockAssets/bed_01",
//         "position": {"x":,"y":,"z":}, "rotation": {"x":,"y":,"z":},
//         "scale":   {"x":,"y":,"z":}, "boundsMin": {...}, "boundsMax": {...} },
//       ...
//   ] }

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
            public bool   asset_loaded;       // 실제 프리팹 로드 성공 여부
            public float  adhesion_dist;
            public bool   ground_contact;
            public int    overlap_count;
            public float  pos_x, pos_y, pos_z;
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
            public float semantic_proximity;
            public float wall_clock_ms;
            public string render_path;
            public string spec_path;
            public string ts_iso;
            public PerObject[] per_object;
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

            // condition 별 물리 토글:
            //   full           - LLM 위치 + 자동 ground-snap (size.y/2 만큼 들어올림)
            //   llm_only       - LLM 이 지정한 position.y 그대로 사용 (스냅 비활성)
            //   random_physics - 무작위 spec + ground-snap 적용
            bool applyGroundSnap = (condition != "llm_only");

            // 1) 빈 씬 + 바닥/조명/카메라 구성
            //    그림 7~12 렌더 품질 보정 (2026-04-29):
            //      - 카메라 가까이 + FOV 축소로 객체가 더 크게 보이도록
            //      - 키 라이트 + 필 라이트 + 앰비언트로 PBR 재질 입체감 강화
            //      - 바닥은 카펫 톤 PBR Standard 머터리얼
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.22f);
            RenderSettings.ambientIntensity = 1.0f;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(8f, 0.1f, 8f);
            floor.transform.position = new Vector3(0, -0.05f, 0);
            // 바닥 — 따뜻한 우드 톤 (rug 키워드를 피해 wood 카테고리로 매칭)
            ApplyPbrTint(floor, "table_floor_wood");
            // 키 라이트
            var lightGo = new GameObject("KeyLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.color = new Color(1.0f, 0.96f, 0.90f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
            // 필 라이트
            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.4f;
            fill.color = new Color(0.85f, 0.88f, 1.0f);
            fillGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            // 카메라 — 방의 한쪽 모서리에서 사선으로 내려다보는 구도
            // 객체가 ±2.5m 안쪽에 분포하므로 코너에서 isometric-ish 로 잡는다.
            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.05f;
            cam.allowHDR = false;
            camGo.transform.position = new Vector3(3.6f, 3.0f, -3.6f);
            camGo.transform.LookAt(new Vector3(0f, 0.4f, 0f));

            // 2) 배치 — Resources 프리팹 우선, 실패 시 프리미티브 폴백
            var spawned = new List<GameObject>();
            var assetLoadedFlags = new List<bool>();
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
                string resPath = NormalizeAssetPath(p.assetPath);
                if (!string.IsNullOrEmpty(resPath))
                {
                    var prefab = Resources.Load<GameObject>(resPath);
                    if (prefab != null)
                    {
                        go = UnityEngine.Object.Instantiate(prefab);
                        // 회전 비정상 방지 (2026-04-29):
                        //   FBX 임포트 후 root transform 의 잔존 회전을 제거. spec rotation 만 적용한다.
                        go.transform.rotation = Quaternion.identity;
                        // FBX 메시 크기를 spec bounds 에 맞춤
                        FitToBounds(go, size);
                        // 텍스처 표현 보정 (2026-04-29):
                        //   Polyhaven FBX 가 텍스처 없이 임포트되면 grey 로 보이므로
                        //   객체별 컬러 PBR Standard 머터리얼을 강제 부여한다.
                        ApplyPbrTint(go, p.objectName ?? p.objectId ?? p.assetPath);
                        loaded = true;
                        loadedCount++;
                    }
                }
                if (go == null)
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = size;
                    ApplyPbrTint(go, p.objectName ?? p.objectId ?? "x");
                }
                go.name = string.IsNullOrEmpty(p.objectName) ? p.objectId : p.objectName;

                // 위치 결정: full/random_physics 는 자동 스냅, llm_only 는 spec 그대로
                float yPos = applyGroundSnap ? (p.position.y + size.y * 0.5f) : p.position.y;
                go.transform.position = new Vector3(p.position.x, yPos, p.position.z);
                // 회전: spec.rotation.y 만 사용 (Y-yaw). x/z 는 0 으로 강제하여 뒤집힘 방지.
                go.transform.eulerAngles = new Vector3(0f, p.rotation.y, 0f);

                // Resources 프리팹은 콜라이더 없을 수 있어 박스콜라이더 강제 추가
                if (go.GetComponent<Collider>() == null)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.size = Vector3.one;
                }
                spawned.Add(go);
                assetLoadedFlags.Add(loaded);
            }

            Physics.SyncTransforms();

            // 3) 측정
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
                semantic_proximity = semantic,
                wall_clock_ms = (float)(DateTime.UtcNow - t0).TotalMilliseconds,
                render_path = pngPath,
                spec_path = layoutSpec,
                ts_iso = DateTime.UtcNow.ToString("o"),
                per_object = perList.ToArray(),
            };
            string outJson = Path.Combine(outputDir, $"trial_{trial}.json");
            File.WriteAllText(outJson, JsonUtility.ToJson(metrics, true));
            Debug.Log($"[Paper03] DONE scenario={spec.scenario} cond={condition} trial={trial} loaded={loadedCount}/{spec.placements.Length} adh%={pct:F1} sem={semantic:F2} png={pngPath}");

            EditorApplication.Exit(0);
        }

        // 서버 응답의 assetPath ("furniture/bed_01") 또는 정적 레이아웃의
        // "MockAssets/bed_01" 모두를 Resources 하위 경로로 정규화한다.
        // Resources.Load 는 확장자·앞 슬래시 없이 "MockAssets/bed_01" 형태를 요구.
        static string NormalizeAssetPath(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            string s = raw.Trim().Replace("\\", "/");
            if (s.StartsWith("/")) s = s.Substring(1);
            // 카테고리 prefix (furniture/, decorations/) → MockAssets/ 로 치환
            if (s.StartsWith("furniture/") || s.StartsWith("decorations/") || s.StartsWith("decoration/"))
            {
                int slash = s.IndexOf('/');
                s = "MockAssets/" + s.Substring(slash + 1);
            }
            // 그 외 (예: 단일 슬러그) 는 MockAssets/ 접두 추가
            if (!s.StartsWith("MockAssets/")) s = "MockAssets/" + s;
            // 확장자 제거
            int dot = s.LastIndexOf('.');
            int slashLast = s.LastIndexOf('/');
            if (dot > slashLast) s = s.Substring(0, dot);
            return s;
        }

        // 카테고리별 PBR 색조를 결정.
        // 그림 7~12 (2026-04-29): Polyhaven FBX 가 텍스처 없이 임포트되면 grey 로
        // 보이므로 객체 이름 카테고리에 맞춰 색을 부여한다.
        static void CategoryTint(string nameKey, out Color baseColor, out float smoothness, out float metallic)
        {
            string n = (nameKey ?? "").ToLowerInvariant();
            smoothness = 0.25f;
            metallic   = 0.0f;
            if (n.Contains("bed") || n.Contains("sofa") || n.Contains("armchair") || n.Contains("rug"))
            { baseColor = new Color(0.62f, 0.40f, 0.32f); smoothness = 0.18f; }
            else if (n.Contains("chair"))
            { baseColor = new Color(0.30f, 0.32f, 0.36f); smoothness = 0.40f; metallic = 0.20f; }
            else if (n.Contains("desk") || n.Contains("table") || n.Contains("nightstand") || n.Contains("bookshelf") || n.Contains("wardrobe") || n.Contains("cabinet"))
            { baseColor = new Color(0.50f, 0.34f, 0.22f); smoothness = 0.30f; }
            else if (n.Contains("monitor") || n.Contains("tv"))
            { baseColor = new Color(0.10f, 0.10f, 0.12f); smoothness = 0.65f; metallic = 0.30f; }
            else if (n.Contains("lamp"))
            { baseColor = new Color(0.92f, 0.85f, 0.62f); smoothness = 0.55f; metallic = 0.25f; }
            else if (n.Contains("plant"))
            { baseColor = new Color(0.22f, 0.50f, 0.28f); smoothness = 0.20f; }
            else
            { baseColor = new Color(0.55f, 0.55f, 0.58f); smoothness = 0.30f; }
            int h = (nameKey ?? "x").GetHashCode();
            float jitter = (((h >> 4) & 0xFF) / 255f - 0.5f) * 0.10f;
            baseColor.r = Mathf.Clamp01(baseColor.r + jitter);
            baseColor.g = Mathf.Clamp01(baseColor.g + jitter);
            baseColor.b = Mathf.Clamp01(baseColor.b + jitter);
            // HDRP 톤매핑이 적용되면 sRGB↔linear 변환이 과도하게 옅어짐.
            // 채도 부스트 — 평균에서 멀수록 더 강조.
            float avg = (baseColor.r + baseColor.g + baseColor.b) / 3f;
            baseColor.r = Mathf.Clamp01(avg + (baseColor.r - avg) * 1.6f);
            baseColor.g = Mathf.Clamp01(avg + (baseColor.g - avg) * 1.6f);
            baseColor.b = Mathf.Clamp01(avg + (baseColor.b - avg) * 1.6f);
        }

        // 임포트된 FBX 의 머터리얼을 그대로 두되 Color/Smoothness/Metallic 만 카테고리 톤으로 덮어쓴다.
        // HDRP 프로젝트에서 Shader.Find("Standard") 가 null 인 점을 회피.
        static void ApplyPbrTint(GameObject go, string nameKey)
        {
            CategoryTint(nameKey, out var baseColor, out var smoothness, out var metallic);
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0)
            {
                var single = go.GetComponent<Renderer>();
                if (single != null) TintRenderer(single, baseColor, smoothness, metallic);
                return;
            }
            foreach (var r in rends) TintRenderer(r, baseColor, smoothness, metallic);
        }

        static void TintRenderer(Renderer r, Color baseColor, float smoothness, float metallic)
        {
            // 인스턴스 머터리얼을 만들어 색만 덮어쓴다 — sharedMaterial 을 갈아끼우지 않아 shader 호환성 유지.
            var mat = r.sharedMaterial;
            if (mat == null)
            {
                // 폴백: HDRP/Lit, URP/Lit, Standard 순서로 시도
                Shader sh = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (sh == null) return;
                mat = new Material(sh);
                r.sharedMaterial = mat;
            }
            // 인스턴스화 — 다른 객체 영향 없음
            var inst = new Material(mat);
            // HDRP/Lit: _BaseColor, _Smoothness, _Metallic. Standard: _Color, _Glossiness, _Metallic. URP/Lit: _BaseColor, _Smoothness, _Metallic.
            if (inst.HasProperty("_BaseColor"))  inst.SetColor("_BaseColor", baseColor);
            if (inst.HasProperty("_Color"))      inst.SetColor("_Color", baseColor);
            inst.color = baseColor;
            if (inst.HasProperty("_Smoothness")) inst.SetFloat("_Smoothness", smoothness);
            if (inst.HasProperty("_Glossiness")) inst.SetFloat("_Glossiness", smoothness);
            if (inst.HasProperty("_Metallic"))   inst.SetFloat("_Metallic", metallic);
            r.sharedMaterial = inst;
        }

        // FBX 인스턴스의 월드 바운드를 spec size 에 맞도록 균등 스케일.
        static void FitToBounds(GameObject go, Vector3 targetSize)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            Vector3 cur = b.size;
            if (cur.x <= 0.001f || cur.y <= 0.001f || cur.z <= 0.001f) return;
            float sx = targetSize.x / cur.x;
            float sy = targetSize.y / cur.y;
            float sz = targetSize.z / cur.z;
            float s  = Mathf.Min(sx, Mathf.Min(sy, sz)); // 비율 유지
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
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;

            // 그림 7~12 톤 보정 (2026-04-29):
            //   HDRP 톤매핑 후 출력이 옅게 나오는 경향 — contrast +35%, gamma 1.15 (어둡게).
            var pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                // gamma >1 = 미드톤 어둡게 → 채도 회복
                c.r = Mathf.Pow(c.r, 1.15f);
                c.g = Mathf.Pow(c.g, 1.15f);
                c.b = Mathf.Pow(c.b, 1.15f);
                // contrast around 0.45
                c.r = Mathf.Clamp01((c.r - 0.45f) * 1.35f + 0.45f);
                c.g = Mathf.Clamp01((c.g - 0.45f) * 1.35f + 0.45f);
                c.b = Mathf.Clamp01((c.b - 0.45f) * 1.35f + 0.45f);
                pixels[i] = c;
            }
            tex.SetPixels(pixels);
            tex.Apply();

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