using System;
using UnityEngine;

namespace SplatForge.Network
{
    /// <summary>
    /// Request for generating a 3DGS object
    /// </summary>
    [Serializable]
    public class GenerationRequest
    {
        public string prompt;
        public string negativePrompt;
        public GenerationQuality quality = GenerationQuality.Medium;
        public int seed = -1; // -1 means random

        public GenerationRequest(string prompt)
        {
            this.prompt = prompt;
        }
    }

    public enum GenerationQuality
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Result from object generation
    /// </summary>
    [Serializable]
    public class GenerationResult
    {
        public bool success;
        public string errorMessage;
        public string objectId;
        public byte[] plyData;
        public ObjectMetadataData metadata;
        public float generationTimeSeconds;
    }

    /// <summary>
    /// Serializable metadata for transport
    /// </summary>
    [Serializable]
    public class ObjectMetadataData
    {
        public string objectId;
        public string objectName;
        public string category;
        public string[] tags;
        public Vector3 boundsMin;
        public Vector3 boundsMax;
        public string sourcePrompt;
        public DateTime createdAt;
    }

    /// <summary>
    /// Request for layout suggestions
    /// </summary>
    [Serializable]
    public class LayoutRequest
    {
        public SceneContextData sceneContext;
        public string[] objectIdsToPlace;
        public LayoutConstraints constraints;
    }

    [Serializable]
    public class SceneContextData
    {
        public SceneObjectInfo[] existingObjects;
        public Vector3 sceneBoundsMin;
        public Vector3 sceneBoundsMax;
        public Vector3 groundPlaneNormal = Vector3.up;
        public float groundPlaneHeight = 0f;
    }

    [Serializable]
    public class SceneObjectInfo
    {
        public string objectId;
        public string category;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 boundsSize;
    }

    [Serializable]
    public class LayoutConstraints
    {
        public bool avoidOverlap = true;
        public bool groundObjects = true;
        public float minSpacing = 0.5f;
        public Vector3? preferredArea;
        public float preferredAreaRadius = 5f;
    }

    /// <summary>
    /// Layout suggestion from the server
    /// </summary>
    [Serializable]
    public class LayoutSuggestion
    {
        public bool success;
        public string errorMessage;
        public PlacementSuggestion[] placements;
        public string reasoning;
    }

    [Serializable]
    public class PlacementSuggestion
    {
        public string objectId;
        public Vector3 suggestedPosition;
        public Quaternion suggestedRotation;
        public float confidence;
        public string rationale;
    }

    // ============================================
    // Scene Composition Types (Main Workflow)
    // ============================================

    /// <summary>
    /// Request for composing a complete scene from a prompt
    /// </summary>
    [Serializable]
    public class SceneCompositionRequest
    {
        public string prompt;
        public FloorStructureData floorStructure;
        public SceneCompositionOptions options;

        public SceneCompositionRequest(string prompt)
        {
            this.prompt = prompt;
            this.options = new SceneCompositionOptions();
        }
    }

    /// <summary>
    /// Floor and wall structure definition for scene composition
    /// </summary>
    [Serializable]
    public class FloorStructureData
    {
        public Vector3 boundsMin;
        public Vector3 boundsMax;
        public float floorHeight;
        public WallSegment[] walls;

        public Vector3 Center => (boundsMin + boundsMax) * 0.5f;
        public Vector3 Size => boundsMax - boundsMin;
    }

    /// <summary>
    /// Wall segment definition
    /// </summary>
    [Serializable]
    public class WallSegment
    {
        public Vector3 start;
        public Vector3 end;
        public float height;
        public bool hasWindow;
        public bool hasDoor;
    }

    /// <summary>
    /// Options for scene composition
    /// </summary>
    [Serializable]
    public class SceneCompositionOptions
    {
        public string style = "default";
        public GenerationQuality quality = GenerationQuality.Medium;
        public int seed = -1;
        public int maxObjects = 10;
        public bool includeDecorations = true;
    }

    /// <summary>
    /// Result from scene composition
    /// </summary>
    [Serializable]
    public class SceneCompositionResult
    {
        public bool success;
        public string errorMessage;
        public SceneObjectPlacement[] placements;
        public string reasoning;
        public float compositionTimeSeconds;
    }

    /// <summary>
    /// Placement information for a single object in the composed scene
    /// </summary>
    [Serializable]
    public class SceneObjectPlacement
    {
        public string objectId;
        public string assetPath;
        public string category;
        public string objectName;
        public Vector3 position;
        public Vector3 rotation; // Euler angles
        public Vector3 scale;
        public ObjectMetadataData metadata;

        public Quaternion GetRotation() => Quaternion.Euler(rotation);
    }
}
