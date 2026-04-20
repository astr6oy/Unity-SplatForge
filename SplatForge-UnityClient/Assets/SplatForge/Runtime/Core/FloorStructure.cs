using System.Collections.Generic;
using UnityEngine;
using SplatForge.Network;

namespace SplatForge.Core
{
    /// <summary>
    /// Floor structure definition and auto-detection for scene composition
    /// </summary>
    public class FloorStructure
    {
        public Vector3 BoundsMin { get; set; }
        public Vector3 BoundsMax { get; set; }
        public float FloorHeight { get; set; }
        public List<WallInfo> Walls { get; set; } = new List<WallInfo>();

        public Vector3 Center => (BoundsMin + BoundsMax) * 0.5f;
        public Vector3 Size => BoundsMax - BoundsMin;
        public float Area => Size.x * Size.z;

        /// <summary>
        /// Convert to serializable data for network transport
        /// </summary>
        public FloorStructureData ToData()
        {
            var walls = new WallSegment[Walls.Count];
            for (int i = 0; i < Walls.Count; i++)
            {
                walls[i] = Walls[i].ToSegment();
            }

            return new FloorStructureData
            {
                boundsMin = BoundsMin,
                boundsMax = BoundsMax,
                floorHeight = FloorHeight,
                walls = walls
            };
        }

        /// <summary>
        /// Auto-detect floor structure from scene Ground layer objects
        /// </summary>
        public static FloorStructure DetectFromScene(int groundLayerMask = -1)
        {
            if (groundLayerMask == -1)
            {
                groundLayerMask = LayerMask.GetMask("Ground");
                if (groundLayerMask == 0)
                {
                    groundLayerMask = LayerMask.GetMask("Default");
                }
            }

            var structure = new FloorStructure();
            var foundBounds = false;
            var combinedBounds = new Bounds();

            // Find all ground objects
            var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            foreach (var renderer in allRenderers)
            {
                if (!IsInLayerMask(renderer.gameObject.layer, groundLayerMask))
                    continue;

                // Check if it's a floor-like object (mostly horizontal)
                var bounds = renderer.bounds;
                var size = bounds.size;

                // Floor objects are typically wider than tall
                if (size.x > size.y * 0.5f || size.z > size.y * 0.5f)
                {
                    if (!foundBounds)
                    {
                        combinedBounds = bounds;
                        foundBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(bounds);
                    }
                }
            }

            if (foundBounds)
            {
                structure.BoundsMin = new Vector3(
                    combinedBounds.min.x,
                    combinedBounds.max.y,
                    combinedBounds.min.z
                );
                structure.BoundsMax = new Vector3(
                    combinedBounds.max.x,
                    combinedBounds.max.y,
                    combinedBounds.max.z
                );
                structure.FloorHeight = combinedBounds.max.y;
            }
            else
            {
                // Default fallback: 10x10 area at origin
                structure.BoundsMin = new Vector3(-5f, 0f, -5f);
                structure.BoundsMax = new Vector3(5f, 0f, 5f);
                structure.FloorHeight = 0f;
            }

            return structure;
        }

        /// <summary>
        /// Create a manual floor structure with specified bounds
        /// </summary>
        public static FloorStructure CreateManual(Vector3 center, Vector2 size, float floorHeight = 0f)
        {
            var halfSize = new Vector3(size.x * 0.5f, 0f, size.y * 0.5f);
            return new FloorStructure
            {
                BoundsMin = center - halfSize + Vector3.up * floorHeight,
                BoundsMax = center + halfSize + Vector3.up * floorHeight,
                FloorHeight = floorHeight
            };
        }

        /// <summary>
        /// Check if a point is within the floor bounds
        /// </summary>
        public bool ContainsPoint(Vector3 point)
        {
            return point.x >= BoundsMin.x && point.x <= BoundsMax.x &&
                   point.z >= BoundsMin.z && point.z <= BoundsMax.z;
        }

        /// <summary>
        /// Get a random position within the floor bounds
        /// </summary>
        public Vector3 GetRandomPosition()
        {
            return new Vector3(
                Random.Range(BoundsMin.x, BoundsMax.x),
                FloorHeight,
                Random.Range(BoundsMin.z, BoundsMax.z)
            );
        }

        private static bool IsInLayerMask(int layer, int layerMask)
        {
            return (layerMask & (1 << layer)) != 0;
        }
    }

    /// <summary>
    /// Wall information for scene composition
    /// </summary>
    public class WallInfo
    {
        public Vector3 Start { get; set; }
        public Vector3 End { get; set; }
        public float Height { get; set; } = 2.5f;
        public bool HasWindow { get; set; }
        public bool HasDoor { get; set; }

        public Vector3 Direction => (End - Start).normalized;
        public float Length => Vector3.Distance(Start, End);
        public Vector3 Normal => Vector3.Cross(Direction, Vector3.up).normalized;

        public WallSegment ToSegment()
        {
            return new WallSegment
            {
                start = Start,
                end = End,
                height = Height,
                hasWindow = HasWindow,
                hasDoor = HasDoor
            };
        }
    }
}
