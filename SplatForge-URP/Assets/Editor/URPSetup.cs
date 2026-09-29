// Auto-generated 2026-05-08 — URP RenderPipelineAsset bootstrap for batchmode test
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GaussianSplatting.Runtime;

namespace SplatTest
{
    public static class URPSetup
    {
        public static void Run()
        {
            try
            {
                Debug.Log("[URPSetup] starting");

                var rendererPath = "Assets/Settings/SplatTest_URPRenderer.asset";
                var pipelinePath = "Assets/Settings/SplatTest_URPPipeline.asset";
                Directory.CreateDirectory("Assets/Settings");
                AssetDatabase.Refresh();

                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, rendererPath);
                }

                bool hasFeature = false;
                if (renderer.rendererFeatures != null)
                {
                    foreach (var f in renderer.rendererFeatures)
                    {
                        if (f != null && f.GetType().Name == "GaussianSplatURPFeature")
                        {
                            hasFeature = true;
                            break;
                        }
                    }
                }
                if (!hasFeature)
                {
                    var featType = System.Type.GetType("GaussianSplatting.Runtime.GaussianSplatURPFeature, GaussianSplatting.Runtime");
                    if (featType == null)
                    {
                        // search all loaded assemblies
                        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            var t = asm.GetType("GaussianSplatting.Runtime.GaussianSplatURPFeature");
                            if (t != null) { featType = t; break; }
                        }
                    }
                    if (featType == null)
                    {
                        Debug.LogError("[URPSetup] GaussianSplatURPFeature type not found");
                        File.WriteAllText("Assets/_setup_result.txt", "FAIL_FEATURE_TYPE_MISSING");
                        return;
                    }
                    var feature = ScriptableObject.CreateInstance(featType) as ScriptableRendererFeature;
                    feature.name = "GaussianSplatURPFeature";
                    AssetDatabase.AddObjectToAsset(feature, renderer);
                    if (renderer.rendererFeatures == null)
                    {
                        var listField = typeof(UniversalRendererData).GetField("m_RendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
                        listField?.SetValue(renderer, new List<ScriptableRendererFeature>());
                    }
                    renderer.rendererFeatures.Add(feature);
                    EditorUtility.SetDirty(feature);
                    EditorUtility.SetDirty(renderer);
                }

                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(pipeline, pipelinePath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
                AssetDatabase.SaveAssets();

                Debug.Log("[URPSetup] done — pipeline=" + pipelinePath + " renderer=" + rendererPath);
                File.WriteAllText("Assets/_setup_result.txt", "OK pipeline=" + pipelinePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[URPSetup] EXCEPTION: " + ex);
                File.WriteAllText("Assets/_setup_result.txt", "FAIL_EXCEPTION:" + ex.GetType().Name + " " + ex.Message);
            }
        }
    }
}
