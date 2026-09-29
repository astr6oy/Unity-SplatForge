using System;
using UnityEngine;
using GaussianSplatting.Runtime;
using SplatForge.Metadata;

namespace SplatForge.Geometry
{
    /// <summary>
    /// Wraps a GaussianSplatRenderer with metadata and proxy collider support
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(GaussianSplatRenderer))]
    public class HybridSceneObject : MonoBehaviour
    {
        [SerializeField] private ObjectMetadata _metadata = new ObjectMetadata();
        [SerializeField] private ProxyColliderType _proxyColliderType = ProxyColliderType.Box;
        [SerializeField] private bool _autoGenerateCollider = true;
        [SerializeField] private bool _syncBoundsFromAsset = true;

        [SerializeField, HideInInspector] private Collider _proxyCollider;

        private GaussianSplatRenderer _renderer;
        private GaussianSplatAsset _lastAsset;

        public ObjectMetadata Metadata => _metadata;
        public GaussianSplatRenderer Renderer => _renderer;
        public Collider ProxyCollider => _proxyCollider;

        public ProxyColliderType ColliderType
        {
            get => _proxyColliderType;
            set
            {
                if (_proxyColliderType != value)
                {
                    _proxyColliderType = value;
                    RegenerateProxyCollider();
                }
            }
        }

        public bool AutoGenerateCollider
        {
            get => _autoGenerateCollider;
            set => _autoGenerateCollider = value;
        }

        public string ObjectId => _metadata?.ObjectId;
        public string ObjectName => _metadata?.ObjectName ?? gameObject.name;

        private void OnEnable()
        {
            _renderer = GetComponent<GaussianSplatRenderer>();
            FindExistingProxyCollider();
            SyncBoundsIfNeeded();
            if (_autoGenerateCollider)
            {
                GenerateProxyColliderIfNeeded();
            }
        }

        private void FindExistingProxyCollider()
        {
            // 직렬화된 참조가 유효하면 사용
            if (_proxyCollider != null)
                return;

            // 기존 컬라이더 검색 (이미 생성된 것이 있을 수 있음)
            var existingCollider = _proxyColliderType switch
            {
                ProxyColliderType.Box => GetComponent<BoxCollider>() as Collider,
                ProxyColliderType.Sphere => GetComponent<SphereCollider>() as Collider,
                ProxyColliderType.Capsule => GetComponent<CapsuleCollider>() as Collider,
                _ => null
            };

            if (existingCollider != null && existingCollider.isTrigger)
            {
                _proxyCollider = existingCollider;
            }
        }

        private void OnDisable()
        {
            // Don't destroy collider on disable in edit mode
        }

        private void OnValidate()
        {
            if (_renderer == null)
                _renderer = GetComponent<GaussianSplatRenderer>();

            if (_syncBoundsFromAsset && _renderer != null && _renderer.asset != null)
            {
                SyncBoundsFromAsset();
            }
        }

        private void Update()
        {
            // Check if asset changed
            if (_renderer != null && _renderer.asset != _lastAsset)
            {
                _lastAsset = _renderer.asset;
                OnAssetChanged();
            }
        }

        private void OnAssetChanged()
        {
            if (_syncBoundsFromAsset)
            {
                SyncBoundsFromAsset();
            }
            if (_autoGenerateCollider)
            {
                RegenerateProxyCollider();
            }
        }

        /// <summary>
        /// Sync metadata bounds from the GaussianSplatAsset
        /// </summary>
        public void SyncBoundsFromAsset()
        {
            if (_renderer == null || _renderer.asset == null)
                return;

            var asset = _renderer.asset;
            _metadata.LocalBoundsMin = asset.boundsMin;
            _metadata.LocalBoundsMax = asset.boundsMax;
        }

        private void SyncBoundsIfNeeded()
        {
            if (_syncBoundsFromAsset && _metadata.LocalBoundsSize.sqrMagnitude < 0.001f)
            {
                SyncBoundsFromAsset();
            }
        }

        /// <summary>
        /// Generate or update the proxy collider based on current settings
        /// </summary>
        public void GenerateProxyColliderIfNeeded()
        {
            if (_proxyCollider == null)
            {
                RegenerateProxyCollider();
            }
        }

        /// <summary>
        /// Force regenerate the proxy collider
        /// </summary>
        public void RegenerateProxyCollider()
        {
            // Remove existing collider if type changed
            if (_proxyCollider != null)
            {
                if (Application.isPlaying)
                    Destroy(_proxyCollider);
                else
                    DestroyImmediate(_proxyCollider);
                _proxyCollider = null;
            }

            if (_proxyColliderType == ProxyColliderType.None)
                return;

            var bounds = _metadata.GetLocalBounds();
            if (bounds.size.sqrMagnitude < 0.001f)
            {
                // Fallback to asset bounds if metadata bounds not set
                SyncBoundsFromAsset();
                bounds = _metadata.GetLocalBounds();
            }

            if (bounds.size.sqrMagnitude < 0.001f)
            {
                Debug.LogWarning($"[HybridSceneObject] Cannot generate collider: bounds not set for {gameObject.name}");
                return;
            }

            _proxyCollider = ProxyColliderGenerator.GenerateCollider(gameObject, _proxyColliderType, bounds);
        }

        /// <summary>
        /// Get world-space bounds of this object
        /// </summary>
        public Bounds GetWorldBounds()
        {
            return _metadata.GetWorldBounds(transform);
        }

        /// <summary>
        /// Check if this object overlaps with another
        /// </summary>
        public bool OverlapsWith(HybridSceneObject other)
        {
            return GetWorldBounds().Intersects(other.GetWorldBounds());
        }

        /// <summary>
        /// Check if a world position is inside this object's bounds
        /// </summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            return GetWorldBounds().Contains(worldPoint);
        }

        /// <summary>
        /// Validate placement at a given position using physics raycasting
        /// </summary>
        public PlacementValidationResult ValidatePlacement(Vector3 worldPosition, LayerMask groundLayer, LayerMask obstacleLayer)
        {
            var result = new PlacementValidationResult { isValid = true };
            var bounds = _metadata.GetLocalBounds();
            var halfExtents = bounds.extents;

            // Check ground contact
            if (Physics.Raycast(worldPosition + Vector3.up * 10f, Vector3.down, out RaycastHit groundHit, 20f, groundLayer))
            {
                result.groundPosition = groundHit.point;
                result.groundNormal = groundHit.normal;
                result.hasGroundContact = true;
            }
            else
            {
                result.isValid = false;
                result.validationMessage = "No ground found at position";
                return result;
            }

            // Check for obstacles using box overlap
            var checkPosition = result.groundPosition + Vector3.up * halfExtents.y;
            var overlaps = Physics.OverlapBox(checkPosition, halfExtents * 0.9f, transform.rotation, obstacleLayer);

            if (overlaps.Length > 0)
            {
                result.isValid = false;
                result.validationMessage = $"Overlaps with {overlaps.Length} obstacle(s)";
                result.overlappingObjects = new GameObject[overlaps.Length];
                for (int i = 0; i < overlaps.Length; i++)
                {
                    result.overlappingObjects[i] = overlaps[i].gameObject;
                }
            }

            return result;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var bounds = _metadata.GetLocalBounds();
            if (bounds.size.sqrMagnitude < 0.001f)
                return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#endif
    }

    public enum ProxyColliderType
    {
        None,
        Box,
        Sphere,
        Capsule
    }

    public struct PlacementValidationResult
    {
        public bool isValid;
        public string validationMessage;
        public bool hasGroundContact;
        public Vector3 groundPosition;
        public Vector3 groundNormal;
        public GameObject[] overlappingObjects;
    }

    /// <summary>
    /// Utility for generating proxy colliders
    /// </summary>
    public static class ProxyColliderGenerator
    {
        public static Collider GenerateCollider(GameObject target, ProxyColliderType type, Bounds localBounds)
        {
            switch (type)
            {
                case ProxyColliderType.Box:
                    return GenerateBoxCollider(target, localBounds);

                case ProxyColliderType.Sphere:
                    return GenerateSphereCollider(target, localBounds);

                case ProxyColliderType.Capsule:
                    return GenerateCapsuleCollider(target, localBounds);

                default:
                    return null;
            }
        }

        private static BoxCollider GenerateBoxCollider(GameObject target, Bounds bounds)
        {
            var collider = target.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            collider.isTrigger = true; // Use trigger for overlap detection without physics simulation
            return collider;
        }

        private static SphereCollider GenerateSphereCollider(GameObject target, Bounds bounds)
        {
            var collider = target.AddComponent<SphereCollider>();
            collider.center = bounds.center;
            // Use the largest extent as radius
            collider.radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            collider.isTrigger = true;
            return collider;
        }

        private static CapsuleCollider GenerateCapsuleCollider(GameObject target, Bounds bounds)
        {
            var collider = target.AddComponent<CapsuleCollider>();
            collider.center = bounds.center;

            // Determine orientation based on largest dimension
            var size = bounds.size;
            if (size.y >= size.x && size.y >= size.z)
            {
                // Vertical capsule (Y-axis)
                collider.direction = 1;
                collider.height = size.y;
                collider.radius = Mathf.Max(size.x, size.z) * 0.5f;
            }
            else if (size.x >= size.y && size.x >= size.z)
            {
                // Horizontal capsule (X-axis)
                collider.direction = 0;
                collider.height = size.x;
                collider.radius = Mathf.Max(size.y, size.z) * 0.5f;
            }
            else
            {
                // Horizontal capsule (Z-axis)
                collider.direction = 2;
                collider.height = size.z;
                collider.radius = Mathf.Max(size.x, size.y) * 0.5f;
            }

            collider.isTrigger = true;
            return collider;
        }
    }
}
