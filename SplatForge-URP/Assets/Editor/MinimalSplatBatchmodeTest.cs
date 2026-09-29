// Auto-generated 2026-05-08 — Paper03 fig 7~12 3DGS 부재 해결 검증
// Tests whether GaussianSplatRenderer outputs non-cleared color buffer in batchmode.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using GaussianSplatting.Runtime;

namespace SplatTest
{
    public static class MinimalSplatBatchmodeTest
    {
        const string ResultPath = "Assets/_test_result.txt";
        const string PngPath = "Assets/_test_render.png";

        public static void Run()
        {
            string result = "FAIL_UNKNOWN";
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var camGo = new GameObject("Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.transform.position = new Vector3(0f, 0.3f, -2.5f);
                cam.transform.LookAt(Vector3.zero);
                cam.backgroundColor = new Color(0.18f, 0.27f, 0.36f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.fieldOfView = 50f;

                var lightGo = new GameObject("Sun");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.0f;
                lightGo.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

                var guids = AssetDatabase.FindAssets("t:GaussianSplatAsset");
                if (guids.Length == 0)
                {
                    result = "FAIL_NO_SPLAT_ASSET";
                    WriteResult(result);
                    return;
                }
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var splatAsset = AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(assetPath);
                if (splatAsset == null)
                {
                    result = "FAIL_ASSET_LOAD_NULL:" + assetPath;
                    WriteResult(result);
                    return;
                }
                Debug.Log("[Test] Asset: " + assetPath + " splats=" + splatAsset.splatCount);

                var rendererGo = new GameObject("SplatRenderer");
                var rd = rendererGo.AddComponent<GaussianSplatRenderer>();
                rd.m_Asset = splatAsset;
                rendererGo.transform.position = Vector3.zero;
                rendererGo.transform.localScale = Vector3.one;

                if (!rd.HasValidAsset)
                {
                    result = "FAIL_INVALID_ASSET (splatCount=" + splatAsset.splatCount + ")";
                    WriteResult(result);
                    return;
                }

                int W = 640, H = 480;
                var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
                rt.Create();
                cam.targetTexture = rt;

                cam.Render();
                cam.Render(); // double-render to allow GS lazy-init

                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;

                var png = tex.EncodeToPNG();
                File.WriteAllBytes(PngPath, png);
                Debug.Log("[Test] PNG saved: " + PngPath + " bytes=" + png.Length);

                var pixels = tex.GetPixels32();
                var seen = new System.Collections.Generic.HashSet<uint>();
                int diversityProbe = 0;
                long sumR = 0, sumG = 0, sumB = 0;
                long varProbe = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    sumR += p.r; sumG += p.g; sumB += p.b;
                    uint c = ((uint)p.r << 24) | ((uint)p.g << 16) | ((uint)p.b << 8) | (uint)p.a;
                    if (seen.Count < 200 && seen.Add(c)) diversityProbe++;
                }
                double meanR = (double)sumR / pixels.Length;
                double meanG = (double)sumG / pixels.Length;
                double meanB = (double)sumB / pixels.Length;
                for (int i = 0; i < pixels.Length; i += 7)
                {
                    var p = pixels[i];
                    long dR = p.r - (long)meanR; long dG = p.g - (long)meanG; long dB = p.b - (long)meanB;
                    varProbe += dR * dR + dG * dG + dB * dB;
                }
                double normVar = (double)varProbe / (pixels.Length / 7.0);

                bool nonCleared = seen.Count > 30 && normVar > 50.0;
                Debug.Log(string.Format("[Test] colors={0} normVar={1:F2} mean=({2:F1},{3:F1},{4:F1})",
                    seen.Count, normVar, meanR, meanG, meanB));
                result = nonCleared ?
                    string.Format("PASS colors={0} var={1:F1}", seen.Count, normVar) :
                    string.Format("FAIL_CLEARED colors={0} var={1:F1}", seen.Count, normVar);
                WriteResult(result);
            }
            catch (System.Exception ex)
            {
                result = "FAIL_EXCEPTION:" + ex.GetType().Name + " " + ex.Message;
                Debug.LogError("[Test] " + result + "\n" + ex.StackTrace);
                WriteResult(result);
            }
            finally
            {
                Debug.Log("[Test] FINAL_RESULT=" + result);
            }
        }

        static void WriteResult(string s)
        {
            File.WriteAllText(ResultPath, s);
        }
    }
}
