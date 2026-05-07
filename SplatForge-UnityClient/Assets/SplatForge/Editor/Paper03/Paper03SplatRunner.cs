// Paper03SplatRunner.cs — 3DGS 격리 렌더 (Phase B verification, 2026-05-07)
//
// 목적: HDRP custom pass 가 batchmode 에서 GaussianSplat 을 렌더하는지 검증.
// RunSingleAsset 과 동일한 룸/조명/카메라 설정 + GaussianSplatRenderer + CustomPassVolume.

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using GaussianSplatting.Runtime; // GaussianSplatAsset, GaussianSplatRenderer (HDRPPass 는 internal)

namespace SplatForge.EditorPaper03
{
    public static class Paper03SplatRunner
    {
        public static void RunSingleSplat()
        {
            string assetPath = GetArg("-asset");      // ex) Assets/Samples/Statue/Statue.asset
            string outputPath = GetArg("-output");
            float splatScaleArg = 1.0f;
            var ssRaw = GetArg("-splatScale");
            if (!string.IsNullOrEmpty(ssRaw)) float.TryParse(ssRaw, out splatScaleArg);
            float worldScaleArg = 1.0f;
            var wsRaw = GetArg("-worldScale");
            if (!string.IsNullOrEmpty(wsRaw)) float.TryParse(wsRaw, out worldScaleArg);

            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("[Paper03/Splat] missing -asset or -output");
                EditorApplication.Exit(2);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupHdrpVolumeLocal();

            // 룸: 4×4 바닥 + 3 벽.
            const float ROOM_W = 4f, ROOM_D = 4f, WALL_H = 2.5f, WALL_T = 0.05f;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(ROOM_W, 0.05f, ROOM_D);
            floor.transform.position = new Vector3(0, -0.025f, 0);
            floor.GetComponent<Renderer>().sharedMaterial = MakeHdrpLitLocal(new Color(0.55f, 0.42f, 0.30f), 0.30f);

            var wallMat = MakeHdrpLitLocal(new Color(0.94f, 0.93f, 0.90f), 0.10f);

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

            // 조명.
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
            hdSun.shadowDimmer = 0.45f;

            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.85f, 0.90f, 1.0f);
            fillGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            var hdFill = fillGo.AddComponent<HDAdditionalLightData>();
            hdFill.intensity = 18000f;
            hdFill.lightUnit = LightUnit.Lux;
            hdFill.EnableShadows(false);

            // 카메라 — 영웅샷.
            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.05f;
            camGo.transform.position = new Vector3(1.5f, 1.5f, -2.5f);
            camGo.transform.LookAt(new Vector3(0f, 0.5f, 0f));
            camGo.AddComponent<HDAdditionalCameraData>();

            // 3DGS 자산 로드.
            var splatAsset = AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(assetPath);
            if (splatAsset == null)
            {
                Debug.LogError($"[Paper03/Splat] failed to load GaussianSplatAsset: {assetPath}");
                EditorApplication.Exit(3);
                return;
            }
            Debug.Log($"[Paper03/Splat] loaded asset splatCount={splatAsset.splatCount} bounds=[{splatAsset.boundsMin}..{splatAsset.boundsMax}]");

            var splatGo = new GameObject("Splat");
            var renderer = splatGo.AddComponent<GaussianSplatRenderer>();
            renderer.m_Asset = splatAsset;
            renderer.m_SplatScale = splatScaleArg;

            // 셰이더 바인딩 — AddComponent 로 만든 인스턴스는 inspector 기본값이 비어 있다.
            renderer.m_ShaderSplats        = Shader.Find("Gaussian Splatting/Render Splats");
            renderer.m_ShaderComposite     = Shader.Find("Hidden/Gaussian Splatting/Composite");
            renderer.m_ShaderDebugPoints   = Shader.Find("Gaussian Splatting/Debug/Render Points");
            renderer.m_ShaderDebugBoxes    = Shader.Find("Gaussian Splatting/Debug/Render Boxes");
            renderer.m_CSSplatUtilities    = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                "Assets/Plugins/UnityGaussianSplatting/Shaders/SplatUtilities.compute");
            Debug.Log($"[Paper03/Splat] shaders splats={(renderer.m_ShaderSplats!=null)} composite={(renderer.m_ShaderComposite!=null)} dbgPts={(renderer.m_ShaderDebugPoints!=null)} dbgBox={(renderer.m_ShaderDebugBoxes!=null)} cs={(renderer.m_CSSplatUtilities!=null)}");

            // OnEnable 가 첫 AddComponent 시 셰이더 null 로 early-return 되므로, 셰이더 할당 후 재호출.
            renderer.enabled = false;
            renderer.enabled = true;

            // bounds 기반 자동 스케일 — 자산이 매우 클 때 (Statue: 100m) 룸에 맞게 축소.
            // 목표 bounds size 1.0m 기준. -worldScale 인자로 override 가능.
            var bMin = splatAsset.boundsMin;
            var bMax = splatAsset.boundsMax;
            float maxExtent = Mathf.Max(bMax.x - bMin.x, Mathf.Max(bMax.y - bMin.y, bMax.z - bMin.z));
            float autoScale = (maxExtent > 0.001f) ? (1.0f / maxExtent) : 1.0f;
            float finalScale = autoScale * worldScaleArg;
            splatGo.transform.localScale = Vector3.one * finalScale;

            // bounds 기반 ground-snap — bounds.min.y 가 0 이 되도록 위로 올림.
            float worldMinY = bMin.y * finalScale;
            splatGo.transform.position = new Vector3(0f, -worldMinY, 0f);

            Debug.Log($"[Paper03/Splat] placement scale={finalScale:F4} pos={splatGo.transform.position}");

            // CustomPassVolume + GaussianSplatHDRPPass 등록.
            var cpvGo = new GameObject("GaussianSplatsHDRPPass");
            var cpv = cpvGo.AddComponent<CustomPassVolume>();
            cpv.isGlobal = true;
            cpv.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
            // GaussianSplatHDRPPass 는 internal 이므로 Type.GetType 으로 동적 로드 후 AddPassOfType(Type) 사용.
            var passType = System.Type.GetType("GaussianSplatting.Runtime.GaussianSplatHDRPPass, GaussianSplatting");
            if (passType == null)
            {
                Debug.LogError("[Paper03/Splat] cannot resolve GaussianSplatHDRPPass type");
                EditorApplication.Exit(4);
                return;
            }
            cpv.AddPassOfType(passType);
            Debug.Log($"[Paper03/Splat] CustomPassVolume registered: passes={cpv.customPasses.Count} type={passType.FullName}");

            // EnsureSorterAndRegister 강제 호출 — 첫 cam.Render() 전에 등록 보장.
            renderer.EnsureSorterAndRegister();

            CaptureCamera(cam, outputPath, 1280, 720);
            Debug.Log($"[Paper03/Splat] DONE asset={assetPath} png={outputPath}");
            EditorApplication.Exit(0);
        }

        // 로컬 헬퍼 (Paper03Runner private 접근 회피).
        static void SetupHdrpVolumeLocal()
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
        }

        static Material MakeHdrpLitLocal(Color baseColor, float smoothness)
        {
            var sh = Shader.Find("HDRP/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
            if (m.HasProperty("_Color"))     m.SetColor("_Color", baseColor);
            if (m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic"))  m.SetFloat("_Metallic", 0.0f);
            return m;
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