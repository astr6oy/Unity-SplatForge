using System;
using System.Collections.Generic;
using UnityEngine;
using SplatForge.Geometry;
using SplatForge.Metadata;

namespace SplatForge.Core
{
    /// <summary>
    /// Serializable scene configuration for saving/loading SplatForge scenes
    /// </summary>
    [Serializable]
    public class SceneConfiguration
    {
        public string configVersion = "1.0";
        public string sceneName;
        public string description;
        public DateTime createdAt;
        public DateTime modifiedAt;

        public SceneSettings settings = new SceneSettings();
        public List<ObjectConfiguration> objects = new List<ObjectConfiguration>();

        public SceneConfiguration()
        {
            createdAt = DateTime.UtcNow;
            modifiedAt = DateTime.UtcNow;
        }

        public SceneConfiguration(string name)
        {
            sceneName = name;
            createdAt = DateTime.UtcNow;
            modifiedAt = DateTime.UtcNow;
        }
    }

    [Serializable]
    public class SceneSettings
    {
        public Vector3 sceneBoundsMin = new Vector3(-50, 0, -50);
        public Vector3 sceneBoundsMax = new Vector3(50, 20, 50);
        public float groundPlaneHeight = 0f;
        public Vector3 groundPlaneNormal = Vector3.up;

        public LayoutSettings defaultLayoutSettings = new LayoutSettings();
    }

    [Serializable]
    public class LayoutSettings
    {
        public bool avoidOverlap = true;
        public bool groundObjects = true;
        public float minSpacing = 0.5f;
    }

    [Serializable]
    public class ObjectConfiguration
    {
        public string objectId;
        public string objectName;
        public string assetPath;
        public string assetGuid;

        public Vector3 position;
        public Vector3 rotation; // Euler angles
        public Vector3 scale = Vector3.one;

        public string category;
        public string[] tags;
        public Vector3 boundsMin;
        public Vector3 boundsMax;

        public string notes;

        public ObjectConfiguration() { }

        public ObjectConfiguration(HybridSceneObject obj)
        {
            objectId = obj.ObjectId;
            objectName = obj.ObjectName;

            position = obj.transform.position;
            rotation = obj.transform.eulerAngles;
            scale = obj.transform.localScale;

            if (obj.Metadata != null)
            {
                category = obj.Metadata.Category;
                tags = obj.Metadata.Tags;
                boundsMin = obj.Metadata.LocalBoundsMin;
                boundsMax = obj.Metadata.LocalBoundsMax;
                notes = obj.Metadata.Notes;
            }

            // Try to get asset path
            if (obj.Renderer != null && obj.Renderer.asset != null)
            {
#if UNITY_EDITOR
                assetPath = UnityEditor.AssetDatabase.GetAssetPath(obj.Renderer.asset);
                assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
#endif
            }
        }

        public void ApplyTo(HybridSceneObject obj)
        {
            obj.transform.position = position;
            obj.transform.eulerAngles = rotation;
            obj.transform.localScale = scale;

            if (obj.Metadata != null)
            {
                obj.Metadata.Category = category;
                obj.Metadata.Tags = tags ?? new string[0];
                obj.Metadata.LocalBoundsMin = boundsMin;
                obj.Metadata.LocalBoundsMax = boundsMax;
                obj.Metadata.Notes = notes;
            }
        }
    }

    /// <summary>
    /// Handles serialization of scene configurations
    /// </summary>
    public static class SceneConfigurationSerializer
    {
        /// <summary>
        /// Create a configuration from the current registry
        /// </summary>
        public static SceneConfiguration CreateFromRegistry(SceneObjectRegistry registry, string sceneName = "Untitled")
        {
            var config = new SceneConfiguration(sceneName);

            if (registry != null)
            {
                foreach (var obj in registry.AllObjects)
                {
                    if (obj != null)
                    {
                        config.objects.Add(new ObjectConfiguration(obj));
                    }
                }
            }

            // Calculate scene bounds
            if (config.objects.Count > 0)
            {
                var min = Vector3.one * float.MaxValue;
                var max = Vector3.one * float.MinValue;

                foreach (var objConfig in config.objects)
                {
                    min = Vector3.Min(min, objConfig.position - objConfig.boundsMax);
                    max = Vector3.Max(max, objConfig.position + objConfig.boundsMax);
                }

                config.settings.sceneBoundsMin = min - Vector3.one * 5f;
                config.settings.sceneBoundsMax = max + Vector3.one * 5f;
            }

            return config;
        }

        /// <summary>
        /// Serialize configuration to JSON
        /// </summary>
        public static string ToJson(SceneConfiguration config, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(config, prettyPrint);
        }

        /// <summary>
        /// Deserialize configuration from JSON
        /// </summary>
        public static SceneConfiguration FromJson(string json)
        {
            return JsonUtility.FromJson<SceneConfiguration>(json);
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public static void SaveToFile(SceneConfiguration config, string filePath)
        {
            var json = ToJson(config);
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"[SceneConfiguration] Saved to {filePath}");
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        public static SceneConfiguration LoadFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogError($"[SceneConfiguration] File not found: {filePath}");
                return null;
            }

            var json = System.IO.File.ReadAllText(filePath);
            return FromJson(json);
        }
    }
}
