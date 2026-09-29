using System;
using System.Collections.Generic;
using UnityEngine;
using SplatForge.Geometry;
using SplatForge.Network;

namespace SplatForge.Core
{
    /// <summary>
    /// Registry for tracking all HybridSceneObjects in the scene
    /// </summary>
    public class SceneObjectRegistry
    {
        private readonly Dictionary<string, HybridSceneObject> _objectsById = new Dictionary<string, HybridSceneObject>();
        private readonly List<HybridSceneObject> _allObjects = new List<HybridSceneObject>();

        public int Count => _allObjects.Count;
        public IReadOnlyList<HybridSceneObject> AllObjects => _allObjects;

        public event Action<HybridSceneObject> OnObjectRegistered;
        public event Action<HybridSceneObject> OnObjectUnregistered;

        /// <summary>
        /// Register an object with the registry
        /// </summary>
        public void Register(HybridSceneObject obj)
        {
            if (obj == null) return;

            var id = obj.ObjectId;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[SceneObjectRegistry] Cannot register object with empty ID: {obj.gameObject.name}");
                return;
            }

            if (_objectsById.ContainsKey(id))
            {
                Debug.LogWarning($"[SceneObjectRegistry] Object with ID '{id}' already registered");
                return;
            }

            _objectsById[id] = obj;
            _allObjects.Add(obj);
            OnObjectRegistered?.Invoke(obj);
        }

        /// <summary>
        /// Unregister an object from the registry
        /// </summary>
        public void Unregister(HybridSceneObject obj)
        {
            if (obj == null) return;

            var id = obj.ObjectId;
            if (string.IsNullOrEmpty(id)) return;

            if (_objectsById.Remove(id))
            {
                _allObjects.Remove(obj);
                OnObjectUnregistered?.Invoke(obj);
            }
        }

        /// <summary>
        /// Get an object by its ID
        /// </summary>
        public HybridSceneObject GetById(string id)
        {
            return _objectsById.TryGetValue(id, out var obj) ? obj : null;
        }

        /// <summary>
        /// Find objects by category
        /// </summary>
        public List<HybridSceneObject> FindByCategory(string category)
        {
            var results = new List<HybridSceneObject>();
            foreach (var obj in _allObjects)
            {
                if (obj.Metadata != null &&
                    string.Equals(obj.Metadata.Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(obj);
                }
            }
            return results;
        }

        /// <summary>
        /// Find objects by tag
        /// </summary>
        public List<HybridSceneObject> FindByTag(string tag)
        {
            var results = new List<HybridSceneObject>();
            foreach (var obj in _allObjects)
            {
                if (obj.Metadata != null && obj.Metadata.HasTag(tag))
                {
                    results.Add(obj);
                }
            }
            return results;
        }

        /// <summary>
        /// Find objects within a sphere
        /// </summary>
        public List<HybridSceneObject> FindInRadius(Vector3 center, float radius)
        {
            var results = new List<HybridSceneObject>();
            var radiusSqr = radius * radius;

            foreach (var obj in _allObjects)
            {
                if ((obj.transform.position - center).sqrMagnitude <= radiusSqr)
                {
                    results.Add(obj);
                }
            }
            return results;
        }

        /// <summary>
        /// Find objects within bounds
        /// </summary>
        public List<HybridSceneObject> FindInBounds(Bounds bounds)
        {
            var results = new List<HybridSceneObject>();

            foreach (var obj in _allObjects)
            {
                if (bounds.Contains(obj.transform.position) ||
                    bounds.Intersects(obj.GetWorldBounds()))
                {
                    results.Add(obj);
                }
            }
            return results;
        }

        /// <summary>
        /// Get scene context for layout requests
        /// </summary>
        public SceneContextData GetSceneContext()
        {
            var context = new SceneContextData
            {
                existingObjects = new SceneObjectInfo[_allObjects.Count]
            };

            var sceneBounds = new Bounds();
            bool boundsInitialized = false;

            for (int i = 0; i < _allObjects.Count; i++)
            {
                var obj = _allObjects[i];
                var worldBounds = obj.GetWorldBounds();

                context.existingObjects[i] = new SceneObjectInfo
                {
                    objectId = obj.ObjectId,
                    category = obj.Metadata?.Category ?? "unknown",
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    boundsSize = worldBounds.size
                };

                if (!boundsInitialized)
                {
                    sceneBounds = worldBounds;
                    boundsInitialized = true;
                }
                else
                {
                    sceneBounds.Encapsulate(worldBounds);
                }
            }

            if (boundsInitialized)
            {
                context.sceneBoundsMin = sceneBounds.min;
                context.sceneBoundsMax = sceneBounds.max;
            }

            return context;
        }

        /// <summary>
        /// Clear all registered objects
        /// </summary>
        public void Clear()
        {
            var objects = new List<HybridSceneObject>(_allObjects);
            foreach (var obj in objects)
            {
                Unregister(obj);
            }
        }

        /// <summary>
        /// Refresh registry from scene (finds all HybridSceneObjects)
        /// </summary>
        public void RefreshFromScene()
        {
            Clear();

            var sceneObjects = UnityEngine.Object.FindObjectsByType<HybridSceneObject>(FindObjectsSortMode.None);
            foreach (var obj in sceneObjects)
            {
                Register(obj);
            }
        }
    }
}
