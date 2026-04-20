using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using SplatForge.Network;
using SplatForge.Core;
using SplatForge.Geometry;

namespace SplatForge.Editor.Windows
{
    /// <summary>
    /// Visualizes layout suggestions in the Scene View
    /// </summary>
    [InitializeOnLoad]
    public static class LayoutVisualizationOverlay
    {
        private static LayoutSuggestion _currentSuggestion;
        private static Dictionary<string, PlacementValidationResult> _validationResults;
        private static SceneObjectRegistry _registry;
        private static bool _isVisible;
        private static bool _showConnections = true;
        private static bool _showLabels = true;

        static LayoutVisualizationOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static void ShowSuggestion(LayoutSuggestion suggestion, SceneObjectRegistry registry)
        {
            _currentSuggestion = suggestion;
            _registry = registry;
            _isVisible = suggestion != null && suggestion.success && suggestion.placements != null;
            _validationResults = null;

            SceneView.RepaintAll();
        }

        public static void ValidateAndShow(LayoutSuggestion suggestion, SceneObjectRegistry registry)
        {
            _currentSuggestion = suggestion;
            _registry = registry;
            _isVisible = suggestion != null && suggestion.success && suggestion.placements != null;

            if (_isVisible)
            {
                var validator = new LayoutValidator();
                _validationResults = validator.ValidateLayout(suggestion, registry);
            }

            SceneView.RepaintAll();
        }

        public static void Hide()
        {
            _isVisible = false;
            _currentSuggestion = null;
            _validationResults = null;
            SceneView.RepaintAll();
        }

        public static void SetOptions(bool showConnections, bool showLabels)
        {
            _showConnections = showConnections;
            _showLabels = showLabels;
            SceneView.RepaintAll();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!_isVisible || _currentSuggestion?.placements == null)
                return;

            Handles.BeginGUI();
            DrawOverlayControls(sceneView);
            Handles.EndGUI();

            // Draw placement visualizations
            foreach (var placement in _currentSuggestion.placements)
            {
                DrawPlacementVisualization(placement);
            }

            // Draw connections between placements
            if (_showConnections && _currentSuggestion.placements.Length > 1)
            {
                DrawConnections();
            }
        }

        private static void DrawOverlayControls(SceneView sceneView)
        {
            var rect = new Rect(10, 10, 200, 100);
            GUI.Window(12345, rect, (id) =>
            {
                GUILayout.Label("Layout Preview", EditorStyles.boldLabel);

                _showConnections = GUILayout.Toggle(_showConnections, "Show Connections");
                _showLabels = GUILayout.Toggle(_showLabels, "Show Labels");

                GUILayout.Space(5);

                if (GUILayout.Button("Apply All"))
                {
                    ApplyAllPlacements();
                }

                if (GUILayout.Button("Clear Preview"))
                {
                    Hide();
                }

            }, "Layout Suggestions");
        }

        private static void DrawPlacementVisualization(PlacementSuggestion placement)
        {
            var position = placement.suggestedPosition;
            var rotation = placement.suggestedRotation;

            // Get object for bounds
            HybridSceneObject obj = null;
            Vector3 size = Vector3.one;

            if (_registry != null)
            {
                obj = _registry.GetById(placement.objectId);
                if (obj != null)
                {
                    size = obj.Metadata.LocalBoundsSize;
                }
            }

            // Determine color based on validation
            Color boxColor = new Color(0.2f, 0.7f, 1f, 0.3f);
            Color wireColor = new Color(0.2f, 0.7f, 1f, 1f);

            if (_validationResults != null && _validationResults.TryGetValue(placement.objectId, out var validation))
            {
                if (!validation.isValid)
                {
                    boxColor = new Color(1f, 0.3f, 0.3f, 0.3f);
                    wireColor = new Color(1f, 0.3f, 0.3f, 1f);
                }
                else
                {
                    boxColor = new Color(0.3f, 1f, 0.3f, 0.3f);
                    wireColor = new Color(0.3f, 1f, 0.3f, 1f);
                }
            }

            // Draw box at suggested position
            var matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            using (new Handles.DrawingScope(matrix))
            {
                // Solid box
                Handles.color = boxColor;
                var bounds = new Bounds(Vector3.up * size.y * 0.5f, size);
                DrawSolidBox(bounds);

                // Wire box
                Handles.color = wireColor;
                DrawWireBox(bounds);
            }

            // Draw arrow from current position to suggested position
            if (obj != null)
            {
                var currentPos = obj.transform.position;
                var distance = Vector3.Distance(currentPos, position);

                if (distance > 0.1f)
                {
                    Handles.color = new Color(1f, 1f, 0f, 0.5f);
                    Handles.DrawDottedLine(currentPos, position, 4f);

                    // Arrow head
                    var direction = (position - currentPos).normalized;
                    var arrowPos = position - direction * 0.3f;
                    Handles.ConeHandleCap(0, arrowPos, Quaternion.LookRotation(direction), 0.2f, EventType.Repaint);
                }
            }

            // Draw label
            if (_showLabels)
            {
                var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = wireColor }
                };

                var labelPos = position + Vector3.up * (size.y + 0.3f);
                var confidence = Mathf.RoundToInt(placement.confidence * 100);
                Handles.Label(labelPos, $"{placement.objectId}\n({confidence}%)", labelStyle);
            }

            // Draw ground position indicator
            if (_validationResults != null && _validationResults.TryGetValue(placement.objectId, out validation))
            {
                if (validation.hasGroundContact)
                {
                    Handles.color = new Color(0.5f, 1f, 0.5f, 0.5f);
                    Handles.DrawWireDisc(validation.groundPosition, validation.groundNormal, 0.3f);

                    // Ground normal
                    Handles.color = Color.green;
                    Handles.DrawLine(validation.groundPosition, validation.groundPosition + validation.groundNormal * 0.5f);
                }
            }
        }

        private static void DrawConnections()
        {
            Handles.color = new Color(0.5f, 0.5f, 1f, 0.3f);

            for (int i = 0; i < _currentSuggestion.placements.Length - 1; i++)
            {
                var pos1 = _currentSuggestion.placements[i].suggestedPosition;
                var pos2 = _currentSuggestion.placements[i + 1].suggestedPosition;

                Handles.DrawDottedLine(pos1, pos2, 2f);
            }
        }

        private static void DrawSolidBox(Bounds bounds)
        {
            var vertices = new Vector3[8];
            var min = bounds.min;
            var max = bounds.max;

            vertices[0] = new Vector3(min.x, min.y, min.z);
            vertices[1] = new Vector3(max.x, min.y, min.z);
            vertices[2] = new Vector3(max.x, min.y, max.z);
            vertices[3] = new Vector3(min.x, min.y, max.z);
            vertices[4] = new Vector3(min.x, max.y, min.z);
            vertices[5] = new Vector3(max.x, max.y, min.z);
            vertices[6] = new Vector3(max.x, max.y, max.z);
            vertices[7] = new Vector3(min.x, max.y, max.z);

            // Draw faces
            DrawQuad(vertices[0], vertices[1], vertices[2], vertices[3]); // Bottom
            DrawQuad(vertices[4], vertices[7], vertices[6], vertices[5]); // Top
            DrawQuad(vertices[0], vertices[3], vertices[7], vertices[4]); // Left
            DrawQuad(vertices[1], vertices[5], vertices[6], vertices[2]); // Right
            DrawQuad(vertices[0], vertices[4], vertices[5], vertices[1]); // Front
            DrawQuad(vertices[3], vertices[2], vertices[6], vertices[7]); // Back
        }

        private static void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Handles.DrawAAConvexPolygon(a, b, c, d);
        }

        private static void DrawWireBox(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;

            // Bottom
            Handles.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z));
            Handles.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z));
            Handles.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z));
            Handles.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, min.y, min.z));

            // Top
            Handles.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z));
            Handles.DrawLine(new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z));
            Handles.DrawLine(new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z));
            Handles.DrawLine(new Vector3(min.x, max.y, max.z), new Vector3(min.x, max.y, min.z));

            // Verticals
            Handles.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z));
            Handles.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z));
            Handles.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z));
            Handles.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, max.z));
        }

        private static void ApplyAllPlacements()
        {
            if (_currentSuggestion?.placements == null || _registry == null)
                return;

            var objects = new List<Object>();
            foreach (var placement in _currentSuggestion.placements)
            {
                var obj = _registry.GetById(placement.objectId);
                if (obj != null)
                    objects.Add(obj.transform);
            }

            Undo.RecordObjects(objects.ToArray(), "Apply Layout Suggestions");

            foreach (var placement in _currentSuggestion.placements)
            {
                var obj = _registry.GetById(placement.objectId);
                if (obj != null)
                {
                    obj.transform.position = placement.suggestedPosition;
                    obj.transform.rotation = placement.suggestedRotation;
                }
            }

            Hide();
        }
    }
}
