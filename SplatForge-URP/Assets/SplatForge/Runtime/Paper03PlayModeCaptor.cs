// Paper03PlayModeCaptor.cs — Runtime MonoBehaviour for PlayMode capture.
//
// Editor 가 PlayMode 진입 후 N 프레임 워밍업하고 Camera.main 을 RenderTexture 로
// 캡처하여 PNG 작성, 완료 신호로 doneSentinel 텍스트 파일을 생성한다.

using System.IO;
using UnityEngine;

namespace SplatForge.Runtime
{
    public class Paper03PlayModeCaptor : MonoBehaviour
    {
        public int warmupFrames = 90;
        public string outputPng;
        public int width = 1280;
        public int height = 720;
        public string doneSentinel = "Assets/_paper03_done.txt";

        int frameCount;
        bool captured;

        void Update()
        {
            if (captured) return;
            frameCount++;
            if (frameCount < warmupFrames) return;

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[Paper03/Captor] Camera.main is null");
                WriteDone("err:no_camera");
                captured = true;
                return;
            }

            try
            {
                CaptureCamera(cam, outputPng, width, height);
                Debug.Log($"[Paper03/Captor] PNG written: {outputPng}");
                WriteDone("ok");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Paper03/Captor] capture failed: {e.Message}");
                WriteDone("err:" + e.Message);
            }
            finally
            {
                captured = true;
            }
        }

        void WriteDone(string status)
        {
            try
            {
                if (!string.IsNullOrEmpty(doneSentinel))
                {
                    var dir = Path.GetDirectoryName(doneSentinel);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(doneSentinel, status + "\n");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Paper03/Captor] sentinel write fail: {e.Message}");
            }
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
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }
    }
}
