using UnityEngine;
using UnityEditor;
using SplatForge.Geometry;
using SplatForge.Metadata;
using SplatForge.Core;

namespace SplatForge.Editor.Inspectors
{
    /// <summary>
    /// Custom inspector for HybridSceneObject
    /// </summary>
    [CustomEditor(typeof(HybridSceneObject))]
    [CanEditMultipleObjects]
    public class HybridSceneObjectEditor : UnityEditor.Editor
    {
        private HybridSceneObject _target;
        private SerializedProperty _metadataProp;
        private SerializedProperty _proxyColliderTypeProp;
        private SerializedProperty _autoGenerateColliderProp;
        private SerializedProperty _syncBoundsFromAssetProp;

        // Metadata sub-properties
        private SerializedProperty _objectIdProp;
        private SerializedProperty _objectNameProp;
        private SerializedProperty _categoryProp;
        private SerializedProperty _tagsProp;
        private SerializedProperty _sourcePromptProp;
        private SerializedProperty _localBoundsMinProp;
        private SerializedProperty _localBoundsMaxProp;
        private SerializedProperty _notesProp;

        // UI State
        private bool _showMetadataFoldout = true;
        private bool _showBoundsFoldout = true;
        private bool _showColliderFoldout = true;
        private bool _showDebugFoldout = false;

        private string _newTag = "";

        private void OnEnable()
        {
            _target = (HybridSceneObject)target;

            _metadataProp = serializedObject.FindProperty("_metadata");
            _proxyColliderTypeProp = serializedObject.FindProperty("_proxyColliderType");
            _autoGenerateColliderProp = serializedObject.FindProperty("_autoGenerateCollider");
            _syncBoundsFromAssetProp = serializedObject.FindProperty("_syncBoundsFromAsset");

            // Metadata sub-properties
            _objectIdProp = _metadataProp.FindPropertyRelative("_objectId");
            _objectNameProp = _metadataProp.FindPropertyRelative("_objectName");
            _categoryProp = _metadataProp.FindPropertyRelative("_category");
            _tagsProp = _metadataProp.FindPropertyRelative("_tags");
            _sourcePromptProp = _metadataProp.FindPropertyRelative("_sourcePrompt");
            _localBoundsMinProp = _metadataProp.FindPropertyRelative("_localBoundsMin");
            _localBoundsMaxProp = _metadataProp.FindPropertyRelative("_localBoundsMax");
            _notesProp = _metadataProp.FindPropertyRelative("_notes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawMetadataSection();
            EditorGUILayout.Space(5);

            DrawBoundsSection();
            EditorGUILayout.Space(5);

            DrawColliderSection();
            EditorGUILayout.Space(5);

            DrawActionsSection();
            EditorGUILayout.Space(5);

            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMetadataSection()
        {
            _showMetadataFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showMetadataFoldout, "Metadata");

            if (_showMetadataFoldout)
            {
                EditorGUI.indentLevel++;

                // Object ID (read-only)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(_objectIdProp, new GUIContent("Object ID"));
                EditorGUI.EndDisabledGroup();

                // Object Name
                EditorGUILayout.PropertyField(_objectNameProp, new GUIContent("Name"));

                // Category dropdown
                DrawCategoryDropdown();

                // Tags
                DrawTagsEditor();

                // Source Prompt
                EditorGUILayout.PropertyField(_sourcePromptProp, new GUIContent("Source Prompt"));

                // Notes
                EditorGUILayout.LabelField("Notes");
                _notesProp.stringValue = EditorGUILayout.TextArea(_notesProp.stringValue, GUILayout.Height(40));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawCategoryDropdown()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Category");

            var currentCategory = _categoryProp.stringValue;
            var categoryIndex = System.Array.IndexOf(ObjectCategories.All, currentCategory);
            if (categoryIndex < 0) categoryIndex = ObjectCategories.All.Length - 1; // Default to misc

            var newIndex = EditorGUILayout.Popup(categoryIndex, ObjectCategories.All);
            if (newIndex != categoryIndex)
            {
                _categoryProp.stringValue = ObjectCategories.All[newIndex];
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTagsEditor()
        {
            EditorGUILayout.LabelField("Tags");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Existing tags
            for (int i = 0; i < _tagsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var tagProp = _tagsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.LabelField(tagProp.stringValue, EditorStyles.miniLabel);

                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    _tagsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Add new tag
            EditorGUILayout.BeginHorizontal();

            _newTag = EditorGUILayout.TextField(_newTag, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("+", GUILayout.Width(30)) && !string.IsNullOrWhiteSpace(_newTag))
            {
                _tagsProp.InsertArrayElementAtIndex(_tagsProp.arraySize);
                _tagsProp.GetArrayElementAtIndex(_tagsProp.arraySize - 1).stringValue = _newTag.Trim();
                _newTag = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();

            // Common tags quick-add
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quick Add:", GUILayout.Width(70));

            foreach (var commonTag in new[] { "small", "medium", "large", "decorative" })
            {
                if (GUILayout.Button(commonTag, EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    if (!HasTag(commonTag))
                    {
                        _tagsProp.InsertArrayElementAtIndex(_tagsProp.arraySize);
                        _tagsProp.GetArrayElementAtIndex(_tagsProp.arraySize - 1).stringValue = commonTag;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private bool HasTag(string tag)
        {
            for (int i = 0; i < _tagsProp.arraySize; i++)
            {
                if (_tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    return true;
            }
            return false;
        }

        private void DrawBoundsSection()
        {
            _showBoundsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showBoundsFoldout, "Bounds");

            if (_showBoundsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_syncBoundsFromAssetProp, new GUIContent("Sync from Asset"));

                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(_localBoundsMinProp, new GUIContent("Bounds Min"));
                EditorGUILayout.PropertyField(_localBoundsMaxProp, new GUIContent("Bounds Max"));

                // Calculated size (read-only)
                var size = _localBoundsMaxProp.vector3Value - _localBoundsMinProp.vector3Value;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Vector3Field("Size", size);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Sync from Asset", GUILayout.Width(120)))
                {
                    Undo.RecordObject(target, "Sync Bounds");
                    _target.SyncBoundsFromAsset();
                    serializedObject.Update();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawColliderSection()
        {
            _showColliderFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showColliderFoldout, "Proxy Collider");

            if (_showColliderFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_autoGenerateColliderProp, new GUIContent("Auto Generate"));
                EditorGUILayout.PropertyField(_proxyColliderTypeProp, new GUIContent("Collider Type"));

                // Show current collider status
                var collider = _target.ProxyCollider;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Current Collider", collider, typeof(Collider), true);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Regenerate", GUILayout.Width(100)))
                {
                    Undo.RecordObject(_target.gameObject, "Regenerate Collider");
                    _target.RegenerateProxyCollider();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Register to Session"))
            {
                var session = SplatForgeSession.Instance;
                if (session != null)
                {
                    session.RegisterObject(_target);
                    Debug.Log($"[HybridSceneObject] Registered '{_target.ObjectName}' to session");
                }
                else
                {
                    EditorUtility.DisplayDialog("SplatForge",
                        "No SplatForgeSession found in scene.",
                        "OK");
                }
            }

            if (GUILayout.Button("Validate Placement"))
            {
                ValidatePlacement();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDebugSection()
        {
            _showDebugFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showDebugFoldout, "Debug Info");

            if (_showDebugFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginDisabledGroup(true);

                // Renderer info
                var renderer = _target.Renderer;
                EditorGUILayout.ObjectField("Renderer", renderer, typeof(GaussianSplatting.Runtime.GaussianSplatRenderer), true);

                if (renderer != null && renderer.asset != null)
                {
                    EditorGUILayout.LabelField($"Splat Count: {renderer.splatCount:N0}");
                    EditorGUILayout.Vector3Field("Asset Bounds Min", renderer.asset.boundsMin);
                    EditorGUILayout.Vector3Field("Asset Bounds Max", renderer.asset.boundsMax);
                }

                // World bounds
                var worldBounds = _target.GetWorldBounds();
                EditorGUILayout.Vector3Field("World Center", worldBounds.center);
                EditorGUILayout.Vector3Field("World Size", worldBounds.size);

                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void ValidatePlacement()
        {
            // Simple raycast validation
            var position = _target.transform.position;
            var groundLayer = LayerMask.GetMask("Default");

            if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            {
                EditorUtility.DisplayDialog("Placement Validation",
                    $"Ground found at Y={hit.point.y:F2}\nNormal: {hit.normal}",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Placement Validation",
                    "No ground found below object.",
                    "OK");
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmos(HybridSceneObject obj, GizmoType gizmoType)
        {
            if (obj.Metadata == null) return;

            var bounds = obj.Metadata.GetLocalBounds();
            if (bounds.size.sqrMagnitude < 0.001f) return;

            // Draw local bounds
            Gizmos.matrix = obj.transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            // Draw world bounds
            Gizmos.matrix = Matrix4x4.identity;
            var worldBounds = obj.GetWorldBounds();
            Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
    }
}
