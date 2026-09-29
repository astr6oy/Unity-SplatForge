using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SplatForge.Network;
using SplatForge.Geometry;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SplatForge.Core
{
    /// <summary>
    /// Applies scene composition results to the Unity scene
    /// Handles asset loading, instantiation, and physics validation
    /// </summary>
    public class SceneComposer
    {
        private readonly ISession _session;

        public LayerMask GroundLayer { get; set; } = -1;
        public LayerMask ObstacleLayer { get; set; } = -1;
        public bool ValidatePlacement { get; set; } = true;
        public bool AutoCorrectHeight { get; set; } = true;

        public SceneComposer(ISession session)
        {
            _session = session;
        }

        /// <summary>
        /// Apply composition result to the scene
        /// </summary>
        public async Task<CompositionApplyResult> ApplyCompositionAsync(
            SceneCompositionResult result,
            Transform parent = null)
        {
            var applyResult = new CompositionApplyResult();

            if (!result.success || result.placements == null)
            {
                applyResult.Success = false;
                applyResult.ErrorMessage = result.errorMessage ?? "No placements in result";
                return applyResult;
            }

            // Create parent container if not specified
            if (parent == null)
            {
                var container = new GameObject($"ComposedScene_{DateTime.Now:HHmmss}");
                parent = container.transform;
                applyResult.ContainerObject = container;
            }

            foreach (var placement in result.placements)
            {
                try
                {
                    var obj = await InstantiateObjectAsync(placement, parent);

                    if (obj != null)
                    {
                        // Validate and correct placement
                        if (ValidatePlacement)
                        {
                            var validationResult = ValidateAndCorrectPlacement(obj, placement);
                            if (!validationResult.isValid && !AutoCorrectHeight)
                            {
                                Debug.LogWarning($"[SceneComposer] Placement validation failed for {placement.objectName}: {validationResult.validationMessage}");
                            }
                        }

                        // Register with session
                        _session?.Registry?.Register(obj);
                        applyResult.CreatedObjects.Add(obj);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SceneComposer] Failed to instantiate {placement.objectName}: {e.Message}");
                    applyResult.FailedPlacements.Add(placement);
                }
            }

            applyResult.Success = applyResult.CreatedObjects.Count > 0;
            return applyResult;
        }

        /// <summary>
        /// Instantiate a single object from placement data
        /// </summary>
        private async Task<HybridSceneObject> InstantiateObjectAsync(
            SceneObjectPlacement placement,
            Transform parent)
        {
            // Create GameObject
            var go = new GameObject(placement.objectName);
            go.transform.SetParent(parent);
            go.transform.position = placement.position;
            go.transform.rotation = placement.GetRotation();
            go.transform.localScale = placement.scale;

            // Add HybridSceneObject component
            var hybrid = go.AddComponent<HybridSceneObject>();

            // Setup metadata
            if (placement.metadata != null)
            {
                hybrid.Metadata.ObjectId = placement.objectId;
                hybrid.Metadata.ObjectName = placement.objectName;
                hybrid.Metadata.Category = placement.category;
                hybrid.Metadata.Tags = placement.metadata.tags ?? Array.Empty<string>();
                hybrid.Metadata.SourcePrompt = placement.metadata.sourcePrompt;
                hybrid.Metadata.LocalBoundsMin = placement.metadata.boundsMin;
                hybrid.Metadata.LocalBoundsMax = placement.metadata.boundsMax;
            }

            // Try to load and assign GaussianSplatAsset
            // In mock mode, asset might not exist - that's OK
            await TryLoadAssetAsync(hybrid, placement.assetPath);

            return hybrid;
        }

        /// <summary>
        /// Try to load GaussianSplatAsset for the object
        /// In mock mode, assets don't exist - this is a placeholder for real implementation
        /// </summary>
        private async Task TryLoadAssetAsync(HybridSceneObject hybrid, string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

#if UNITY_EDITOR
            // In editor, try to load from AssetDatabase
            var fullPath = $"Assets/SplatForge/Samples~/{assetPath}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<GaussianSplatting.Runtime.GaussianSplatAsset>(fullPath);

            if (asset != null && hybrid.Renderer != null)
            {
                // GaussianSplatRenderer.asset is read-only property
                // Use SerializedObject to set the backing field in editor
                var so = new SerializedObject(hybrid.Renderer);
                var assetProp = so.FindProperty("m_Asset");
                if (assetProp != null)
                {
                    assetProp.objectReferenceValue = asset;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[SceneComposer] Loaded asset: {assetPath}");
                }
            }
#endif
            // In runtime, would use Addressables or Resources
            await Task.CompletedTask;
        }

        /// <summary>
        /// Validate placement and correct if needed
        /// </summary>
        private PlacementValidationResult ValidateAndCorrectPlacement(
            HybridSceneObject obj,
            SceneObjectPlacement placement)
        {
            var result = new PlacementValidationResult { isValid = true };

            if (GroundLayer == 0)
                return result;

            // Raycast to find ground
            var rayOrigin = obj.transform.position + Vector3.up * 10f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, GroundLayer))
            {
                result.hasGroundContact = true;
                result.groundPosition = hit.point;
                result.groundNormal = hit.normal;

                if (AutoCorrectHeight)
                {
                    // Adjust position to ground level
                    var bounds = obj.Metadata.GetLocalBounds();
                    var correctedPos = hit.point + Vector3.up * bounds.extents.y;
                    obj.transform.position = correctedPos;
                }
            }
            else
            {
                result.isValid = false;
                result.validationMessage = "No ground found at placement position";
            }

            // Check for overlaps
            if (ObstacleLayer != 0)
            {
                var bounds = obj.Metadata.GetLocalBounds();
                var overlaps = Physics.OverlapBox(
                    obj.transform.position,
                    bounds.extents * 0.9f,
                    obj.transform.rotation,
                    ObstacleLayer
                );

                // Filter out self
                var actualOverlaps = new List<GameObject>();
                foreach (var overlap in overlaps)
                {
                    if (overlap.gameObject != obj.gameObject)
                    {
                        actualOverlaps.Add(overlap.gameObject);
                    }
                }

                if (actualOverlaps.Count > 0)
                {
                    result.isValid = false;
                    result.validationMessage = $"Overlaps with {actualOverlaps.Count} object(s)";
                    result.overlappingObjects = actualOverlaps.ToArray();
                }
            }

            return result;
        }

        /// <summary>
        /// Clear all objects from a composed scene container
        /// </summary>
        public void ClearComposition(GameObject container)
        {
            if (container == null)
                return;

            var hybrids = container.GetComponentsInChildren<HybridSceneObject>();
            foreach (var hybrid in hybrids)
            {
                _session?.Registry?.Unregister(hybrid);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
            else
#endif
            {
                UnityEngine.Object.Destroy(container);
            }
        }
    }

    /// <summary>
    /// Result of applying a scene composition
    /// </summary>
    public class CompositionApplyResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public GameObject ContainerObject { get; set; }
        public List<HybridSceneObject> CreatedObjects { get; set; } = new List<HybridSceneObject>();
        public List<SceneObjectPlacement> FailedPlacements { get; set; } = new List<SceneObjectPlacement>();

        public int TotalCreated => CreatedObjects.Count;
        public int TotalFailed => FailedPlacements.Count;
    }
}
