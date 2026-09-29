using UnityEngine;
using UnityEditor;
using System.IO;
using SplatForge.Core;

namespace SplatForge.Editor.Windows
{
    /// <summary>
    /// Window for managing scene configurations
    /// </summary>
    public class SceneConfigurationWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private SplatForgeSession _session;

        private string _configName = "MyScene";
        private string _description = "";
        private string _lastSavePath;
        private SceneConfiguration _loadedConfig;

        private bool _showSaveFoldout = true;
        private bool _showLoadFoldout = true;
        private bool _showPreviewFoldout = false;

        [MenuItem("Tools/SplatForge/Scene Configuration")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneConfigurationWindow>();
            window.titleContent = new GUIContent("Scene Config");
            window.minSize = new Vector2(300, 400);
        }

        private void OnEnable()
        {
            RefreshSession();
        }

        private void RefreshSession()
        {
            _session = SplatForgeSession.Instance;
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawSaveSection();
            EditorGUILayout.Space(5);

            DrawLoadSection();
            EditorGUILayout.Space(5);

            if (_loadedConfig != null)
            {
                DrawPreviewSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Scene Configuration Manager", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            var objectCount = _session?.Registry?.Count ?? 0;
            EditorGUILayout.LabelField($"Objects: {objectCount}", EditorStyles.miniLabel, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSaveSection()
        {
            _showSaveFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showSaveFoldout, "Save Configuration");

            if (_showSaveFoldout)
            {
                EditorGUI.indentLevel++;

                _configName = EditorGUILayout.TextField("Config Name", _configName);

                EditorGUILayout.LabelField("Description");
                _description = EditorGUILayout.TextArea(_description, GUILayout.Height(60));

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                var hasObjects = _session?.Registry?.Count > 0;
                EditorGUI.BeginDisabledGroup(!hasObjects);

                if (GUILayout.Button("Save As...", GUILayout.Width(100)))
                {
                    SaveConfiguration();
                }

                if (!string.IsNullOrEmpty(_lastSavePath))
                {
                    if (GUILayout.Button("Quick Save", GUILayout.Width(100)))
                    {
                        QuickSave();
                    }
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                if (!hasObjects)
                {
                    EditorGUILayout.HelpBox("No objects registered in the scene. Add HybridSceneObjects and register them first.", MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawLoadSection()
        {
            _showLoadFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showLoadFoldout, "Load Configuration");

            if (_showLoadFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Load...", GUILayout.Width(100)))
                {
                    LoadConfiguration();
                }

                EditorGUILayout.EndHorizontal();

                if (_loadedConfig != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox($"Loaded: {_loadedConfig.sceneName}\nObjects: {_loadedConfig.objects.Count}", MessageType.Info);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Apply to Scene", GUILayout.Width(120)))
                    {
                        ApplyConfigurationToScene();
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    {
                        _loadedConfig = null;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawPreviewSection()
        {
            _showPreviewFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showPreviewFoldout, "Configuration Preview");

            if (_showPreviewFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Scene Name", _loadedConfig.sceneName);
                EditorGUILayout.LabelField("Version", _loadedConfig.configVersion);
                EditorGUILayout.LabelField("Created", _loadedConfig.createdAt.ToString("yyyy-MM-dd HH:mm"));

                if (!string.IsNullOrEmpty(_loadedConfig.description))
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Description");
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextArea(_loadedConfig.description, GUILayout.Height(40));
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);

                foreach (var objConfig in _loadedConfig.objects)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(objConfig.objectName ?? objConfig.objectId, GUILayout.Width(150));
                    EditorGUILayout.LabelField(objConfig.category ?? "-", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"({objConfig.position.x:F1}, {objConfig.position.y:F1}, {objConfig.position.z:F1})");
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void SaveConfiguration()
        {
            var path = EditorUtility.SaveFilePanel(
                "Save Scene Configuration",
                Application.dataPath,
                _configName + ".json",
                "json"
            );

            if (string.IsNullOrEmpty(path))
                return;

            var config = SceneConfigurationSerializer.CreateFromRegistry(_session.Registry, _configName);
            config.description = _description;

            SceneConfigurationSerializer.SaveToFile(config, path);
            _lastSavePath = path;

            EditorUtility.DisplayDialog("Save Complete", $"Configuration saved to:\n{path}", "OK");
        }

        private void QuickSave()
        {
            if (string.IsNullOrEmpty(_lastSavePath))
                return;

            var config = SceneConfigurationSerializer.CreateFromRegistry(_session.Registry, _configName);
            config.description = _description;
            config.modifiedAt = System.DateTime.UtcNow;

            SceneConfigurationSerializer.SaveToFile(config, _lastSavePath);
            Debug.Log($"[SceneConfiguration] Quick saved to {_lastSavePath}");
        }

        private void LoadConfiguration()
        {
            var path = EditorUtility.OpenFilePanel(
                "Load Scene Configuration",
                Application.dataPath,
                "json"
            );

            if (string.IsNullOrEmpty(path))
                return;

            _loadedConfig = SceneConfigurationSerializer.LoadFromFile(path);

            if (_loadedConfig != null)
            {
                _configName = _loadedConfig.sceneName;
                _description = _loadedConfig.description;
                _showPreviewFoldout = true;
            }
        }

        private void ApplyConfigurationToScene()
        {
            if (_loadedConfig == null || _session?.Registry == null)
                return;

            int applied = 0;
            int notFound = 0;

            foreach (var objConfig in _loadedConfig.objects)
            {
                var obj = _session.Registry.GetById(objConfig.objectId);
                if (obj != null)
                {
                    Undo.RecordObject(obj.transform, "Apply Configuration");
                    objConfig.ApplyTo(obj);
                    applied++;
                }
                else
                {
                    notFound++;
                    Debug.LogWarning($"[SceneConfiguration] Object not found in scene: {objConfig.objectId}");
                }
            }

            var message = $"Applied configuration to {applied} objects.";
            if (notFound > 0)
            {
                message += $"\n{notFound} objects were not found in the scene.";
            }

            EditorUtility.DisplayDialog("Apply Configuration", message, "OK");
        }
    }
}
