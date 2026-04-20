using System;
using UnityEngine;

namespace SplatForge.Metadata
{
    /// <summary>
    /// Predefined categories for object classification
    /// </summary>
    public static class ObjectCategories
    {
        public const string Furniture = "furniture";
        public const string Vegetation = "vegetation";
        public const string Vehicle = "vehicle";
        public const string Architecture = "architecture";
        public const string Character = "character";
        public const string Prop = "prop";
        public const string Nature = "nature";
        public const string Misc = "misc";

        public static readonly string[] All = new[]
        {
            Furniture,
            Vegetation,
            Vehicle,
            Architecture,
            Character,
            Prop,
            Nature,
            Misc
        };

        public static bool IsValid(string category)
        {
            if (string.IsNullOrEmpty(category)) return false;
            foreach (var c in All)
            {
                if (string.Equals(c, category, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Common tags for object tagging
    /// </summary>
    public static class CommonTags
    {
        // Size
        public const string Small = "small";
        public const string Medium = "medium";
        public const string Large = "large";

        // Style
        public const string Modern = "modern";
        public const string Vintage = "vintage";
        public const string Futuristic = "futuristic";
        public const string Natural = "natural";

        // Material hints
        public const string Wooden = "wooden";
        public const string Metal = "metal";
        public const string Glass = "glass";
        public const string Stone = "stone";
        public const string Plastic = "plastic";

        // Function
        public const string Interactive = "interactive";
        public const string Static = "static";
        public const string Decorative = "decorative";

        public static readonly string[] All = new[]
        {
            Small, Medium, Large,
            Modern, Vintage, Futuristic, Natural,
            Wooden, Metal, Glass, Stone, Plastic,
            Interactive, Static, Decorative
        };
    }

    /// <summary>
    /// Metadata preset for quick object setup
    /// </summary>
    [CreateAssetMenu(fileName = "MetadataPreset", menuName = "SplatForge/Metadata Preset")]
    public class MetadataPreset : ScriptableObject
    {
        [Tooltip("Default category for this preset")]
        public string category = ObjectCategories.Misc;

        [Tooltip("Default tags to apply")]
        public string[] defaultTags = Array.Empty<string>();

        [Tooltip("Typical bounds min for this type of object")]
        public Vector3 typicalBoundsMin = new Vector3(-0.5f, 0f, -0.5f);

        [Tooltip("Typical bounds max for this type of object")]
        public Vector3 typicalBoundsMax = new Vector3(0.5f, 1f, 0.5f);

        [Tooltip("Description of this preset")]
        [TextArea(2, 4)]
        public string description;

        public ObjectMetadata CreateMetadata()
        {
            return new ObjectMetadata
            {
                Category = category,
                Tags = (string[])defaultTags.Clone(),
                LocalBoundsMin = typicalBoundsMin,
                LocalBoundsMax = typicalBoundsMax
            };
        }

        public void ApplyTo(ObjectMetadata metadata)
        {
            metadata.Category = category;
            foreach (var tag in defaultTags)
            {
                metadata.AddTag(tag);
            }
            // Only apply bounds if not already set
            if (metadata.LocalBoundsSize.sqrMagnitude < 0.001f)
            {
                metadata.LocalBoundsMin = typicalBoundsMin;
                metadata.LocalBoundsMax = typicalBoundsMax;
            }
        }
    }
}
