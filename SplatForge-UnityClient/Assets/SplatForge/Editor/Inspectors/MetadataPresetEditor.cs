using UnityEngine;
using UnityEditor;
using SplatForge.Metadata;

namespace SplatForge.Editor.Inspectors
{
    /// <summary>
    /// Custom inspector for MetadataPreset
    /// </summary>
    [CustomEditor(typeof(MetadataPreset))]
    public class MetadataPresetEditor : UnityEditor.Editor
    {
        private SerializedProperty _categoryProp;
        private SerializedProperty _defaultTagsProp;
        private SerializedProperty _typicalBoundsMinProp;
        private SerializedProperty _typicalBoundsMaxProp;
        private SerializedProperty _descriptionProp;

        private string _newTag = "";

        private void OnEnable()
        {
            _categoryProp = serializedObject.FindProperty("category");
            _defaultTagsProp = serializedObject.FindProperty("defaultTags");
            _typicalBoundsMinProp = serializedObject.FindProperty("typicalBoundsMin");
            _typicalBoundsMaxProp = serializedObject.FindProperty("typicalBoundsMax");
            _descriptionProp = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Metadata Preset", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Category dropdown
            DrawCategoryDropdown();

            EditorGUILayout.Space(5);

            // Default tags
            DrawTagsEditor();

            EditorGUILayout.Space(5);

            // Typical bounds
            EditorGUILayout.LabelField("Typical Bounds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_typicalBoundsMinProp, new GUIContent("Min"));
            EditorGUILayout.PropertyField(_typicalBoundsMaxProp, new GUIContent("Max"));

            // Preview size
            var size = _typicalBoundsMaxProp.vector3Value - _typicalBoundsMinProp.vector3Value;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector3Field("Size", size);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            // Description
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_descriptionProp, GUIContent.none);

            EditorGUILayout.Space(10);

            // Quick preset buttons
            DrawQuickPresetButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCategoryDropdown()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Category");

            var currentCategory = _categoryProp.stringValue;
            var categoryIndex = System.Array.IndexOf(ObjectCategories.All, currentCategory);
            if (categoryIndex < 0) categoryIndex = ObjectCategories.All.Length - 1;

            var newIndex = EditorGUILayout.Popup(categoryIndex, ObjectCategories.All);
            if (newIndex != categoryIndex)
            {
                _categoryProp.stringValue = ObjectCategories.All[newIndex];
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTagsEditor()
        {
            EditorGUILayout.LabelField("Default Tags", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Existing tags
            for (int i = 0; i < _defaultTagsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var tagProp = _defaultTagsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.LabelField(tagProp.stringValue, EditorStyles.miniLabel);

                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    _defaultTagsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            // Add new tag
            EditorGUILayout.BeginHorizontal();

            _newTag = EditorGUILayout.TextField(_newTag, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("+", GUILayout.Width(30)) && !string.IsNullOrWhiteSpace(_newTag))
            {
                _defaultTagsProp.InsertArrayElementAtIndex(_defaultTagsProp.arraySize);
                _defaultTagsProp.GetArrayElementAtIndex(_defaultTagsProp.arraySize - 1).stringValue = _newTag.Trim();
                _newTag = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();

            // Common tags
            EditorGUILayout.BeginHorizontal();
            foreach (var tag in CommonTags.All.Take(8))
            {
                if (GUILayout.Button(tag, EditorStyles.miniButton))
                {
                    if (!HasTag(tag))
                    {
                        _defaultTagsProp.InsertArrayElementAtIndex(_defaultTagsProp.arraySize);
                        _defaultTagsProp.GetArrayElementAtIndex(_defaultTagsProp.arraySize - 1).stringValue = tag;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private bool HasTag(string tag)
        {
            for (int i = 0; i < _defaultTagsProp.arraySize; i++)
            {
                if (_defaultTagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    return true;
            }
            return false;
        }

        private void DrawQuickPresetButtons()
        {
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Chair"))
            {
                ApplyQuickPreset(ObjectCategories.Furniture, new[] { "medium", "wooden" },
                    new Vector3(-0.25f, 0, -0.25f), new Vector3(0.25f, 0.9f, 0.25f));
            }

            if (GUILayout.Button("Table"))
            {
                ApplyQuickPreset(ObjectCategories.Furniture, new[] { "medium", "wooden" },
                    new Vector3(-0.6f, 0, -0.4f), new Vector3(0.6f, 0.75f, 0.4f));
            }

            if (GUILayout.Button("Tree"))
            {
                ApplyQuickPreset(ObjectCategories.Vegetation, new[] { "large", "natural" },
                    new Vector3(-1f, 0, -1f), new Vector3(1f, 4f, 1f));
            }

            if (GUILayout.Button("Small Prop"))
            {
                ApplyQuickPreset(ObjectCategories.Prop, new[] { "small", "decorative" },
                    new Vector3(-0.15f, 0, -0.15f), new Vector3(0.15f, 0.3f, 0.15f));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ApplyQuickPreset(string category, string[] tags, Vector3 boundsMin, Vector3 boundsMax)
        {
            _categoryProp.stringValue = category;

            _defaultTagsProp.ClearArray();
            foreach (var tag in tags)
            {
                _defaultTagsProp.InsertArrayElementAtIndex(_defaultTagsProp.arraySize);
                _defaultTagsProp.GetArrayElementAtIndex(_defaultTagsProp.arraySize - 1).stringValue = tag;
            }

            _typicalBoundsMinProp.vector3Value = boundsMin;
            _typicalBoundsMaxProp.vector3Value = boundsMax;
        }
    }

    // Extension method for Take (since we can't use System.Linq in some Unity versions)
    internal static class ArrayExtensions
    {
        public static T[] Take<T>(this T[] array, int count)
        {
            if (array == null || count <= 0) return new T[0];
            count = Mathf.Min(count, array.Length);
            var result = new T[count];
            System.Array.Copy(array, result, count);
            return result;
        }
    }
}
