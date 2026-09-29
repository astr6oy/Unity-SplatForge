// Auto-generated 2026-05-08 — Enter PlayMode in batchmode, wait for captor, exit
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GaussianSplatting.Runtime;

namespace SplatTest
{
    public static class PlayModeBatchTest
    {
        const string DoneFile = "Assets/_test_done.txt";
        const string ResultFile = "Assets/_test_result.txt";
        const string ScenePath = "Assets/_TestScene.unity";
        const int MaxWaitSeconds = 60;

        public static void Run()
        {
            try
            {
                // Build scene
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.transform.position = new Vector3(0f, 0.3f, -2.5f);
                cam.transform.LookAt(Vector3.zero);
                cam.backgroundColor = new Color(0.18f, 0.27f, 0.36f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.fieldOfView = 50f;

                var lightGo = new GameObject("Sun");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lightGo.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

                var guids = AssetDatabase.FindAssets("t:GaussianSplatAsset");
                if (guids.Length == 0)
                {
                    File.WriteAllText(ResultFile, "FAIL_NO_SPLAT_ASSET");
                    EditorApplication.Exit(2);
                    return;
                }
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var splatAsset = AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(assetPath);

                var rendererGo = new GameObject("SplatRenderer");
                var rd = rendererGo.AddComponent<GaussianSplatRenderer>();
                rd.m_Asset = splatAsset;

                var captorGo = new GameObject("Captor");
                captorGo.AddComponent<SplatTestCaptor>();

                if (File.Exists(DoneFile)) File.Delete(DoneFile);
                if (File.Exists(ResultFile)) File.Delete(ResultFile);

                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[PlayModeBatchTest] scene saved, asset=" + assetPath + " splats=" + splatAsset.splatCount);

                // Hook playmode state changed and enter
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                _startTime = DateTime.UtcNow;
                EditorApplication.update += Tick;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception ex)
            {
                File.WriteAllText(ResultFile, "FAIL_SETUP:" + ex.Message);
                Debug.LogError("[PlayModeBatchTest] setup failed: " + ex);
                EditorApplication.Exit(3);
            }
        }

        static DateTime _startTime;

        static void Tick()
        {
            if (File.Exists(DoneFile))
            {
                Debug.Log("[PlayModeBatchTest] done file detected, exiting playmode");
                EditorApplication.update -= Tick;
                EditorApplication.ExitPlaymode();
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
                return;
            }
            if ((DateTime.UtcNow - _startTime).TotalSeconds > MaxWaitSeconds)
            {
                Debug.LogError("[PlayModeBatchTest] timeout waiting for capture");
                if (!File.Exists(ResultFile)) File.WriteAllText(ResultFile, "FAIL_TIMEOUT");
                EditorApplication.update -= Tick;
                EditorApplication.ExitPlaymode();
                EditorApplication.delayCall += () => EditorApplication.Exit(4);
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Debug.Log("[PlayModeBatchTest] playmode state: " + state);
        }
    }
}
