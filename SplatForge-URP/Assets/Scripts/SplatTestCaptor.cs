// Auto-generated 2026-05-08 — PlayMode captor (matches unity-lab Paper03RuntimeCaptor pattern)
using System;
using System.IO;
using UnityEngine;

namespace SplatTest
{
    public class SplatTestCaptor : MonoBehaviour
    {
        public int warmupFrames = 90;
        public string outputPng = "Assets/_test_render.png";
        public string resultFile = "Assets/_test_result.txt";
        public string doneFile = "Assets/_test_done.txt";
        public int width = 640;
        public int height = 480;

        int frame;
        bool fired;

        void Update()
        {
            if (fired) return;
            frame++;
            if (frame >= warmupFrames)
            {
                fired = true;
                Capture();
                File.WriteAllText(doneFile, DateTime.UtcNow.ToString("o"));
            }
        }

        void Capture()
        {
            try
            {
                var cam = Camera.main;
                if (cam == null)
                {
                    var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                    if (cams.Length > 0) cam = cams[0];
                }
                if (cam == null)
                {
                    File.WriteAllText(resultFile, "FAIL_NO_CAMERA");
                    return;
                }
                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                rt.Create();
                var prevTarget = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.Render();
                var prevActive = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = prevActive;
                cam.targetTexture = prevTarget;

                var png = tex.EncodeToPNG();
                File.WriteAllBytes(outputPng, png);

                var pixels = tex.GetPixels32();
                var seen = new System.Collections.Generic.HashSet<uint>();
                long sumR = 0, sumG = 0, sumB = 0;
                for (int i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    sumR += p.r; sumG += p.g; sumB += p.b;
                    if (seen.Count < 300) seen.Add(((uint)p.r << 24) | ((uint)p.g << 16) | ((uint)p.b << 8));
                }
                double meanR = (double)sumR / pixels.Length;
                double meanG = (double)sumG / pixels.Length;
                double meanB = (double)sumB / pixels.Length;
                long varProbe = 0;
                for (int i = 0; i < pixels.Length; i += 7)
                {
                    var p = pixels[i];
                    long dR = p.r - (long)meanR; long dG = p.g - (long)meanG; long dB = p.b - (long)meanB;
                    varProbe += dR * dR + dG * dG + dB * dB;
                }
                double normVar = (double)varProbe / (pixels.Length / 7.0);
                bool nonCleared = seen.Count > 30 && normVar > 50.0;
                string r = string.Format("{0} colors={1} var={2:F1} mean=({3:F0},{4:F0},{5:F0})",
                    nonCleared ? "PASS" : "FAIL_CLEARED", seen.Count, normVar, meanR, meanG, meanB);
                File.WriteAllText(resultFile, r);
                Debug.Log("[Captor] " + r);
            }
            catch (Exception ex)
            {
                File.WriteAllText(resultFile, "FAIL_EXCEPTION:" + ex.GetType().Name + " " + ex.Message);
                Debug.LogError("[Captor] " + ex);
            }
        }
    }
}
