using System.Collections.Generic;
using UnityEngine;
using SplatForge.Geometry;
using SplatForge.Network;

namespace SplatForge.Core
{
    /// <summary>
    /// Validates and adjusts layout suggestions using physics
    /// </summary>
    public class LayoutValidator
    {
        public LayerMask GroundLayer { get; set; } = ~0;
        public LayerMask ObstacleLayer { get; set; } = ~0;
        public float MaxGroundCheckDistance { get; set; } = 50f;
        public float GroundCheckStartHeight { get; set; } = 20f;

        /// <summary>
        /// Validate a single placement suggestion
        /// </summary>
        public PlacementValidationResult ValidatePlacement(PlacementSuggestion suggestion, HybridSceneObject obj)
        {
            var result = new PlacementValidationResult
            {
                isValid = true
            };

            var bounds = obj.Metadata.GetLocalBounds();
            var halfExtents = bounds.extents;
            var position = suggestion.suggestedPosition;

            // Step 1: Ground check
            var groundCheckStart = position + Vector3.up * GroundCheckStartHeight;
            if (Physics.Raycast(groundCheckStart, Vector3.down, out RaycastHit groundHit, MaxGroundCheckDistance, GroundLayer))
            {
                result.hasGroundContact = true;
                result.groundPosition = groundHit.point;
                result.groundNormal = groundHit.normal;
            }
            else
            {
                result.isValid = false;
                result.validationMessage = "No ground surface found";
                return result;
            }

            // Step 2: Adjust position to ground
            var adjustedPosition = result.groundPosition + Vector3.up * halfExtents.y;

            // Step 3: Overlap check at adjusted position
            var overlaps = Physics.OverlapBox(
                adjustedPosition + bounds.center,
                halfExtents * 0.95f, // Slightly smaller to avoid edge cases
                suggestion.suggestedRotation,
                ObstacleLayer
            );

            // Filter out self and trigger colliders
            var validOverlaps = new List<Collider>();
            foreach (var collider in overlaps)
            {
                if (collider.gameObject == obj.gameObject)
                    continue;
                if (collider.isTrigger)
                    continue;
                validOverlaps.Add(collider);
            }

            if (validOverlaps.Count > 0)
            {
                result.isValid = false;
                result.validationMessage = $"Overlaps with {validOverlaps.Count} object(s)";
                result.overlappingObjects = new GameObject[validOverlaps.Count];
                for (int i = 0; i < validOverlaps.Count; i++)
                {
                    result.overlappingObjects[i] = validOverlaps[i].gameObject;
                }
            }

            return result;
        }

        /// <summary>
        /// Validate all placements in a layout suggestion
        /// </summary>
        public Dictionary<string, PlacementValidationResult> ValidateLayout(
            LayoutSuggestion suggestion,
            SceneObjectRegistry registry)
        {
            var results = new Dictionary<string, PlacementValidationResult>();

            if (suggestion?.placements == null || registry == null)
                return results;

            foreach (var placement in suggestion.placements)
            {
                var obj = registry.GetById(placement.objectId);
                if (obj == null)
                {
                    results[placement.objectId] = new PlacementValidationResult
                    {
                        isValid = false,
                        validationMessage = "Object not found in registry"
                    };
                    continue;
                }

                results[placement.objectId] = ValidatePlacement(placement, obj);
            }

            return results;
        }

        /// <summary>
        /// Adjust placement to ground level
        /// </summary>
        public PlacementSuggestion AdjustToGround(PlacementSuggestion original, HybridSceneObject obj)
        {
            var bounds = obj.Metadata.GetLocalBounds();
            var position = original.suggestedPosition;

            var groundCheckStart = position + Vector3.up * GroundCheckStartHeight;
            if (Physics.Raycast(groundCheckStart, Vector3.down, out RaycastHit hit, MaxGroundCheckDistance, GroundLayer))
            {
                return new PlacementSuggestion
                {
                    objectId = original.objectId,
                    suggestedPosition = hit.point + Vector3.up * bounds.extents.y,
                    suggestedRotation = original.suggestedRotation,
                    confidence = original.confidence,
                    rationale = original.rationale + " (adjusted to ground)"
                };
            }

            return original;
        }

        /// <summary>
        /// Find a valid position near the suggested one
        /// </summary>
        public PlacementSuggestion FindValidPosition(
            PlacementSuggestion original,
            HybridSceneObject obj,
            float searchRadius = 3f,
            int maxAttempts = 16)
        {
            var bounds = obj.Metadata.GetLocalBounds();
            var halfExtents = bounds.extents;

            // First try the original position
            var validation = ValidatePlacement(original, obj);
            if (validation.isValid)
            {
                return AdjustToGround(original, obj);
            }

            // Try positions in a spiral pattern
            for (int i = 0; i < maxAttempts; i++)
            {
                var angle = i * Mathf.PI * 2f / maxAttempts;
                var radius = searchRadius * (0.3f + 0.7f * (float)i / maxAttempts);
                var offset = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );

                var testSuggestion = new PlacementSuggestion
                {
                    objectId = original.objectId,
                    suggestedPosition = original.suggestedPosition + offset,
                    suggestedRotation = original.suggestedRotation,
                    confidence = original.confidence * 0.9f,
                    rationale = original.rationale + $" (offset by {offset.magnitude:F1}m)"
                };

                validation = ValidatePlacement(testSuggestion, obj);
                if (validation.isValid)
                {
                    return AdjustToGround(testSuggestion, obj);
                }
            }

            // Return original if no valid position found
            return original;
        }
    }
}
