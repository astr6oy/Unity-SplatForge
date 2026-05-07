// Paper03Runner.cs — batchmode entry for Paper03 experiment
//
// 그림 7~12 정식 PBR 렌더 파이프라인 (2026-05-07 재작성):
//   (1) Polyhaven 동봉 PBR 텍스처(diff/nor_gl/arm)를 HDRP/Lit Material 에 직접 바인딩
//   (2) HDRP Volume(Exposure + Tonemapping ACES) + Directional Sun(Lux) 으로
//       카메라가 받는 노출을 결정. tint/contrast/gamma 후처리 없음.
//   (3) FBX root 회전 누적 정리 + 누운 객체 자동 보정 + per-asset override 로 회전 비정상 해결.
//   (4) 색조 hack 전면 제거.

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
            { "plant_01",       "nettle_plant" },
            { "sofa_01",        "Sofa_01" },
            { "table_01",       "coffee_table_round_01" },
            { "cabinet_01",     "modern_wooden_cabinet" },
            { "wardrobe_01",    "painted_wooden_cabinet_02" },
            { "tv_01",          "Television_01" },
        };

        // Per-asset 회전 보정 (Euler X,Y,Z degrees, world space).
        static readonly Dictionary<string, Vector3> AxisFix = new Dictionary<string, Vector3> {
        };

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

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(8f, 0.1f, 8f);
            floor.transform.position = new Vector3(0, -0.05f, 0);
            var floorMat = MakePlainHdrpLit(new Color(0.45f, 0.40f, 0.34f), 0.20f, 0.0f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

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

            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.85f, 0.90f, 1.0f);
            fillGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            var hdFill = fillGo.AddComponent<HDAdditionalLightData>();
            hdFill.intensity = 8000f;
            hdFill.lightUnit = LightUnit.Lux;
            hdFill.EnableShadows(false);

            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            // 코너에서 사선으로 잡되 약간 더 멀리 + 위에서 — 객체가 모두 화각에 들어오도록.
            camGo.transform.position = new Vector3(4.2f, 3.0f, -4.2f);
            camGo.transform.LookAt(new Vector3(0f, 0.6f, 0.4f));
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

                        var b = ComputeRendererBounds(go);
                        // chair/desk 같은 모델이 누워서 임포트되는 사례 보정 — 높이가 가로/세로
                        // 평균보다 절반 미만이면 누운 상태로 간주.
                        float avgXZ = (b.size.x + b.size.z) * 0.5f;
                        if (b.size.y < avgXZ * 0.50f)
                        {
                            go.transform.Rotate(-90f, 0f, 0f, Space.World);
                            diag += "auto_lay_fix:-90X;";
                            FitToBounds(go, size);
                        }

                        if (!string.IsNullOrEmpty(slug) && AxisFix.TryGetValue(slug, out var fix))
                        {
                            go.transform.Rotate(fix.x, fix.y, fix.z, Space.World);
                            diag += "override_axis;";
                            FitToBounds(go, size);
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

                float yPos = applyGroundSnap ? (p.position.y + size.y * 0.5f) : p.position.y;
                go.transform.position = new Vector3(p.position.x, yPos, p.position.z);
                go.transform.Rotate(0f, p.rotation.y, 0f, Space.World);

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
            float sx = targetSize.x / cur.x;
            float sy = targetSize.y / cur.y;
            float sz = targetSize.z / cur.z;
            float s  = Mathf.Min(sx, Mathf.Min(sy, sz));
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
