// Paper03PlayModeEntry.cs — Editor → PlayMode 진입 후 캡처 (URP, v2 — 3DGS 주입).
//
// v2 변경: BuildSceneOnly 후 GaussianSplatRenderer 1개 주입 (fig 7~12 3DGS 부재 해결).
// Asset 검색: AssetDatabase.FindAssets("t:GaussianSplatAsset") 첫 항목 사용.
// 위치: 룸 시나리오에 무관하게 (0.8, 0.0, -0.4) 카메라 시점에 보이는 위치.

using System.IO;
using UnityEditor;
using UnityEngine;
using SplatForge.Runtime;
using GaussianSplatting.Runtime;

namespace SplatForge.EditorPaper03
{
    public static class Paper03PlayModeEntry
    {
        const string DONE_SENTINEL = "Assets/_paper03_done.txt";
        const int POLL_MS = 200;

        public static void RunSingleAssetPlayMode()
        {
            string assetName = GetArg("-asset");
            string outputPath = GetArg("-output");
            if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("[Paper03/PM/Iso] missing -asset or -output");
                EditorApplication.Exit(2);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var cam = Paper03Runner.BuildSingleAssetScene(assetName);

            // 3DGS 주입 (선택 — -no-splat 옵션으로 비활성).
            if (string.IsNullOrEmpty(GetArg("-no-splat")))
                InjectSplatAsset(new Vector3(0.8f, 0.0f, -0.4f), Vector3.one * 0.6f);

            AttachCaptor(cam, outputPath, 1280, 720);
            EnterAndWait();
        }

        public static void RunPlayMode()
        {
            string layoutSpec = GetArg("-layoutSpec");
            string outputDir  = GetArg("-outputDir");
            string condition  = GetArg("-condition") ?? "full";
            int trial         = int.TryParse(GetArg("-trial"), out var tr) ? tr : 1;

            if (string.IsNullOrEmpty(layoutSpec) || string.IsNullOrEmpty(outputDir))
            {
                Debug.LogError("[Paper03/PM] missing -layoutSpec or -outputDir");
                EditorApplication.Exit(2);
                return;
            }
            Directory.CreateDirectory(outputDir);

            Paper03Runner.Spec spec;
            try { spec = JsonUtility.FromJson<Paper03Runner.Spec>(File.ReadAllText(layoutSpec)); }
            catch (System.Exception e)
            {
                Debug.LogError($"[Paper03/PM] spec parse fail: {e.Message}");
                EditorApplication.Exit(3);
                return;
            }

            Paper03Runner.BuildSceneOnly(spec);

            // 3DGS 주입 (시나리오별 위치 약간 다르게 — 카메라 시야 내 mesh 와 겹치지 않게).
            if (string.IsNullOrEmpty(GetArg("-no-splat")))
            {
                Vector3 pos;
                string scn = (spec.scenario ?? "").ToLowerInvariant();
                if (scn == "cozy_bedroom")        pos = new Vector3(1.0f, 0.0f, -0.6f);
                else if (scn == "modern_office")  pos = new Vector3(-1.0f, 0.0f, 0.6f);
                else if (scn == "living_room")    pos = new Vector3(1.2f, 0.0f, -1.0f);
                else                              pos = new Vector3(1.0f, 0.0f, -0.5f);
                InjectSplatAsset(pos, Vector3.one * 0.8f);
            }

            var camGo = GameObject.Find("Cam");
            if (camGo == null)
            {
                Debug.LogError("[Paper03/PM] Cam not found after BuildSceneOnly");
                EditorApplication.Exit(4);
                return;
            }
            var cam = camGo.GetComponent<Camera>();
            string pngPath = Path.Combine(outputDir, $"trial_{trial}.png");

            AttachCaptor(cam, pngPath, 1280, 720);
            EnterAndWait();
        }

        static void InjectSplatAsset(Vector3 position, Vector3 scale)
        {
            var guids = AssetDatabase.FindAssets("t:GaussianSplatAsset");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[Paper03/PM] no GaussianSplatAsset found — skipping splat injection");
                return;
            }
            var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var splatAsset = AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(assetPath);
            if (splatAsset == null)
            {
                Debug.LogError($"[Paper03/PM] splat asset load null: {assetPath}");
                return;
            }
            var go = new GameObject("Paper03_Splat_Injected");
            var rd = go.AddComponent<GaussianSplatRenderer>();
            rd.m_Asset = splatAsset;
            go.transform.position = position;
            go.transform.localScale = scale;
            Debug.Log($"[Paper03/PM] injected splat: {assetPath} at {position} scale {scale}");
        }

        static void AttachCaptor(Camera cam, string outputPath, int w, int h)
        {
            cam.tag = "MainCamera";
            var captorGo = new GameObject("Paper03Captor");
            var captor = captorGo.AddComponent<Paper03PlayModeCaptor>();
            captor.warmupFrames = 90;
            captor.outputPng = outputPath;
            captor.width = w;
            captor.height = h;
            captor.doneSentinel = DONE_SENTINEL;
        }

        static void EnterAndWait()
        {
            try { if (File.Exists(DONE_SENTINEL)) File.Delete(DONE_SENTINEL); } catch { }
            EditorApplication.update += PollDone;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayMode(PlayModeStateChange s)
        {
            Debug.Log($"[Paper03/PM] playmode state: {s}");
        }

        static void PollDone()
        {
            if (!File.Exists(DONE_SENTINEL)) return;
            EditorApplication.update -= PollDone;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            try { File.Delete(DONE_SENTINEL); } catch { }
            Debug.Log("[Paper03/PM] done sentinel detected — exiting");
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        static string GetArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == name && i + 1 < args.Length) return args[i + 1];
            return null;
        }
    }
}
