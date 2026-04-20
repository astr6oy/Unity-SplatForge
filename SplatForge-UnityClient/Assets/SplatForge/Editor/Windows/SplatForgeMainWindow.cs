using UnityEngine;
using UnityEditor;
using SplatForge.Core;
using SplatForge.Network;
using SplatForge.Geometry;

namespace SplatForge.Editor.Windows
{
    /// <summary>
    /// Main editor window for SplatForge
    /// Scene Composition 중심의 워크플로우
    /// </summary>
    public class SplatForgeMainWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        // Connection state
        private bool _isConnecting;

        // Scene Composition state (Main workflow)
        private string _compositionPrompt = "";
        private FloorStructure _floorStructure;
        private SceneCompositionOptions _compositionOptions;
        private bool _isComposing;
        private SceneCompositionResult _lastCompositionResult;
        private CompositionApplyResult _lastApplyResult;
        private SceneComposer _sceneComposer;
        private int _selectedPreset;
        private static readonly string[] _presetOptions = { "None", "Cozy Bedroom", "Modern Office", "Living Room" };
        private static readonly string[] _presetPrompts = { "", "A cozy bedroom with bed, desk and lamp", "A modern office with desk and plants", "A comfortable living room with sofa and TV" };

        // Legacy: Generation settings (Test section)
        private string _generationPrompt = "";
        private GenerationQuality _generationQuality = GenerationQuality.Medium;
        private bool _isGenerating;
        private GenerationResult _lastGenerationResult;

        // Legacy: Layout settings (Test section)
        private LayoutConstraints _layoutConstraints;
        private bool _isRequestingLayout;
        private LayoutSuggestion _lastLayoutSuggestion;

        // UI State
        private bool _showConnectionFoldout = true;
        private bool _showCompositionFoldout = true;
        private bool _showTestFoldout = false; // 기존 기능은 접힌 상태로 시작
        private bool _showGenerationFoldout = false;
        private bool _showLayoutFoldout = false;
        private bool _showRegistryFoldout = false;

        private ISession Session => SplatForgeSession.Current;
        private SplatForgeSettings Settings => SplatForgeSettings.Instance;

        [MenuItem("Tools/SplatForge/Control Panel")]
        public static void ShowWindow()
        {
            var window = GetWindow<SplatForgeMainWindow>();
            window.titleContent = new GUIContent("SplatForge", EditorGUIUtility.IconContent("d_SceneAsset Icon").image);
            window.minSize = new Vector2(320, 450);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            InitializeSettings();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                Repaint();
            }
        }

        private void InitializeSettings()
        {
            _layoutConstraints = new LayoutConstraints
            {
                minSpacing = Settings.DefaultMinSpacing,
                groundObjects = Settings.DefaultGroundObjects,
                avoidOverlap = Settings.DefaultAvoidOverlap
            };

            _compositionOptions = new SceneCompositionOptions
            {
                quality = GenerationQuality.Medium,
                maxObjects = 10,
                includeDecorations = true
            };

            _floorStructure = null;
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawConnectionSection();
            EditorGUILayout.Space(5);

            // Main workflow: Scene Composition
            DrawSceneCompositionSection();
            EditorGUILayout.Space(5);

            // Legacy test sections (collapsed by default)
            DrawTestSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("SplatForge Control Panel", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Status indicator
            var statusColor = GetConnectionStatusColor();
            var statusText = GetConnectionStatusText();
            var prevColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(statusText, EditorStyles.miniLabel);
            GUI.color = prevColor;

            // Settings button
            if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                SettingsService.OpenProjectSettings("Project/SplatForge");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawConnectionSection()
        {
            _showConnectionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showConnectionFoldout, "Server Connection");

            if (_showConnectionFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle("Use Mock Server", Settings.UseMockServer);
                if (!Settings.UseMockServer)
                {
                    EditorGUILayout.TextField("Endpoint", Settings.ServerEndpoint);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Settings", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    SettingsService.OpenProjectSettings("Project/SplatForge");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(_isConnecting);

                if (Session?.IsConnected == true)
                {
                    if (GUILayout.Button("Disconnect", GUILayout.Width(100)))
                    {
                        Session.Disconnect();
                        Repaint();
                    }
                }
                else
                {
                    if (GUILayout.Button(_isConnecting ? "Connecting..." : "Connect", GUILayout.Width(100)))
                    {
                        ConnectToServer();
                    }
                }

                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ============================================
        // Scene Composition (Main Workflow)
        // ============================================

        private void DrawSceneCompositionSection()
        {
            _showCompositionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showCompositionFoldout, "Scene Composition");

            if (_showCompositionFoldout)
            {
                EditorGUI.indentLevel++;

                var isConnected = Session?.IsConnected == true;
                EditorGUI.BeginDisabledGroup(!isConnected);

                // Floor Bounds
                EditorGUILayout.LabelField("Floor Bounds", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Auto-detect", GUILayout.Width(100)))
                {
                    _floorStructure = FloorStructure.DetectFromScene();
                    Repaint();
                }
                if (GUILayout.Button("Reset", GUILayout.Width(60)))
                {
                    _floorStructure = null;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();

                if (_floorStructure != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Vector3Field("Min", _floorStructure.BoundsMin);
                    EditorGUILayout.Vector3Field("Max", _floorStructure.BoundsMax);
                    EditorGUILayout.LabelField($"Area: {_floorStructure.Area:F1} m²");
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.HelpBox("Click 'Auto-detect' to scan Ground layer objects, or floor will use default 10x10m area.", MessageType.Info);
                }

                EditorGUILayout.Space(5);

                // Prompt / Preset
                EditorGUILayout.LabelField("Scene Description", EditorStyles.boldLabel);

                var newPreset = EditorGUILayout.Popup("Preset", _selectedPreset, _presetOptions);
                if (newPreset != _selectedPreset)
                {
                    _selectedPreset = newPreset;
                    if (_selectedPreset > 0)
                    {
                        _compositionPrompt = _presetPrompts[_selectedPreset];
                    }
                }

                EditorGUILayout.LabelField("Prompt:");
                _compositionPrompt = EditorGUILayout.TextArea(_compositionPrompt, GUILayout.Height(50));

                // Options
                EditorGUILayout.Space(3);
                _compositionOptions.maxObjects = EditorGUILayout.IntSlider("Max Objects", _compositionOptions.maxObjects, 1, 20);
                _compositionOptions.includeDecorations = EditorGUILayout.Toggle("Include Decorations", _compositionOptions.includeDecorations);

                EditorGUILayout.Space(5);

                // Compose button
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(_isComposing || string.IsNullOrWhiteSpace(_compositionPrompt));
                if (GUILayout.Button(_isComposing ? "Composing..." : "Compose Scene", GUILayout.Width(120)))
                {
                    ComposeScene();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();

                // Result preview
                if (_lastCompositionResult != null)
                {
                    EditorGUILayout.Space(5);
                    DrawCompositionResult();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawCompositionResult()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_lastCompositionResult.success)
            {
                EditorGUILayout.LabelField("Composition Preview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Objects: {_lastCompositionResult.placements?.Length ?? 0}");
                EditorGUILayout.LabelField($"Time: {_lastCompositionResult.compositionTimeSeconds:F2}s");

                if (!string.IsNullOrEmpty(_lastCompositionResult.reasoning))
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.HelpBox(_lastCompositionResult.reasoning, MessageType.Info);
                }

                // Object list
                if (_lastCompositionResult.placements != null && _lastCompositionResult.placements.Length > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Objects to place:", EditorStyles.miniBoldLabel);

                    foreach (var placement in _lastCompositionResult.placements)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  {placement.objectName}", GUILayout.Width(120));
                        EditorGUILayout.LabelField($"[{placement.category}]", EditorStyles.miniLabel, GUILayout.Width(80));
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Apply to Scene", GUILayout.Height(25)))
                {
                    ApplyComposition();
                }

                if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(25)))
                {
                    ClearCompositionPreview();
                }

                EditorGUILayout.EndHorizontal();

                // Apply result
                if (_lastApplyResult != null)
                {
                    EditorGUILayout.Space(3);
                    if (_lastApplyResult.Success)
                    {
                        EditorGUILayout.HelpBox($"Created {_lastApplyResult.TotalCreated} object(s) in scene.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(_lastApplyResult.ErrorMessage, MessageType.Error);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("Composition Failed", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_lastCompositionResult.errorMessage, MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private async void ComposeScene()
        {
            if (Session == null || string.IsNullOrWhiteSpace(_compositionPrompt)) return;

            _isComposing = true;
            _lastApplyResult = null;
            Repaint();

            var floor = _floorStructure ?? FloorStructure.CreateManual(Vector3.zero, new Vector2(10, 10));
            _lastCompositionResult = await Session.ComposeSceneAsync(_compositionPrompt, floor, _compositionOptions);

            _isComposing = false;
            Repaint();
        }

        private async void ApplyComposition()
        {
            if (_lastCompositionResult == null || !_lastCompositionResult.success) return;

            _sceneComposer ??= new SceneComposer(Session);
            _lastApplyResult = await _sceneComposer.ApplyCompositionAsync(_lastCompositionResult);

            if (_lastApplyResult.Success && _lastApplyResult.ContainerObject != null)
            {
                Selection.activeGameObject = _lastApplyResult.ContainerObject;
                EditorGUIUtility.PingObject(_lastApplyResult.ContainerObject);
            }

            Repaint();
        }

        private void ClearCompositionPreview()
        {
            _lastCompositionResult = null;
            _lastApplyResult = null;
            Repaint();
        }

        // ============================================
        // Test Section (Legacy Features)
        // ============================================

        private void DrawTestSection()
        {
            _showTestFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_showTestFoldout, "Test: Individual Operations");

            if (_showTestFoldout)
            {
                EditorGUI.indentLevel++;

                DrawGenerationSection();
                EditorGUILayout.Space(5);
                DrawLayoutSection();
                EditorGUILayout.Space(5);
                DrawRegistrySection();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawGenerationSection()
        {
            _showGenerationFoldout = EditorGUILayout.Foldout(_showGenerationFoldout, "Object Generation", true);

            if (_showGenerationFoldout)
            {
                EditorGUI.indentLevel++;

                var isConnected = Session?.IsConnected == true;
                EditorGUI.BeginDisabledGroup(!isConnected);

                EditorGUILayout.LabelField("Prompt", EditorStyles.boldLabel);
                _generationPrompt = EditorGUILayout.TextArea(_generationPrompt, GUILayout.Height(40));

                _generationQuality = (GenerationQuality)EditorGUILayout.EnumPopup("Quality", _generationQuality);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(_isGenerating || string.IsNullOrWhiteSpace(_generationPrompt));
                if (GUILayout.Button(_isGenerating ? "Generating..." : "Generate", GUILayout.Width(100)))
                {
                    GenerateObject();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();

                if (_lastGenerationResult != null)
                {
                    EditorGUILayout.Space(3);
                    DrawGenerationResult();
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawGenerationResult()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_lastGenerationResult.success)
            {
                EditorGUILayout.LabelField($"Generated: {_lastGenerationResult.objectId}");
                EditorGUILayout.LabelField($"Time: {_lastGenerationResult.generationTimeSeconds:F2}s");
            }
            else
            {
                EditorGUILayout.HelpBox(_lastGenerationResult.errorMessage, MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLayoutSection()
        {
            _showLayoutFoldout = EditorGUILayout.Foldout(_showLayoutFoldout, "Layout Suggestions", true);

            if (_showLayoutFoldout)
            {
                EditorGUI.indentLevel++;

                var isConnected = Session?.IsConnected == true;
                EditorGUI.BeginDisabledGroup(!isConnected);

                _layoutConstraints.avoidOverlap = EditorGUILayout.Toggle("Avoid Overlap", _layoutConstraints.avoidOverlap);
                _layoutConstraints.groundObjects = EditorGUILayout.Toggle("Ground Objects", _layoutConstraints.groundObjects);
                _layoutConstraints.minSpacing = EditorGUILayout.FloatField("Min Spacing", _layoutConstraints.minSpacing);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                var selectedCount = Selection.gameObjects?.Length ?? 0;
                var hasSelection = selectedCount > 0 && HasSelectedHybridObjects();

                EditorGUI.BeginDisabledGroup(_isRequestingLayout || !hasSelection);
                if (GUILayout.Button(_isRequestingLayout ? "Requesting..." : $"Get Layout ({selectedCount})", GUILayout.Width(120)))
                {
                    RequestLayoutSuggestion();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();

                if (_lastLayoutSuggestion != null)
                {
                    EditorGUILayout.Space(3);
                    DrawLayoutSuggestion();
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawLayoutSuggestion()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_lastLayoutSuggestion.success)
            {
                EditorGUILayout.LabelField($"Suggestions: {_lastLayoutSuggestion.placements?.Length ?? 0}");

                if (GUILayout.Button("Apply Suggestions"))
                {
                    ApplyLayoutSuggestions();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(_lastLayoutSuggestion.errorMessage, MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRegistrySection()
        {
            _showRegistryFoldout = EditorGUILayout.Foldout(_showRegistryFoldout, "Scene Registry", true);

            if (_showRegistryFoldout)
            {
                EditorGUI.indentLevel++;

                var registry = Session?.Registry;
                var count = registry?.Count ?? 0;

                EditorGUILayout.LabelField($"Registered Objects: {count}");

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                {
                    registry?.RefreshFromScene();
                    Repaint();
                }

                if (GUILayout.Button("Select All", GUILayout.Width(70)))
                {
                    SelectAllRegisteredObjects();
                }

                EditorGUILayout.EndHorizontal();

                if (registry != null && count > 0)
                {
                    EditorGUILayout.Space(3);

                    foreach (var obj in registry.AllObjects)
                    {
                        if (obj == null) continue;

                        EditorGUILayout.BeginHorizontal();

                        if (GUILayout.Button(obj.ObjectName, EditorStyles.linkLabel))
                        {
                            Selection.activeGameObject = obj.gameObject;
                            EditorGUIUtility.PingObject(obj.gameObject);
                        }

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(obj.Metadata?.Category ?? "-", GUILayout.Width(60));

                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        // ============================================
        // Helper Methods
        // ============================================

        private Color GetConnectionStatusColor()
        {
            if (Session == null) return Color.gray;
            if (_isConnecting) return Color.yellow;
            return Session.IsConnected ? Color.green : Color.red;
        }

        private string GetConnectionStatusText()
        {
            if (Session == null) return "No Session";
            if (_isConnecting) return "Connecting...";

            var serverType = Settings.UseMockServer ? "Mock" : "HTTP";
            return Session.IsConnected ? $"Connected ({serverType})" : $"Disconnected ({serverType})";
        }

        private bool HasSelectedHybridObjects()
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<HybridSceneObject>() != null)
                    return true;
            }
            return false;
        }

        private async void ConnectToServer()
        {
            if (Session == null) return;

            _isConnecting = true;
            Repaint();

            await Session.ConnectAsync();

            _isConnecting = false;
            Repaint();
        }

        private async void GenerateObject()
        {
            if (Session == null || string.IsNullOrWhiteSpace(_generationPrompt)) return;

            _isGenerating = true;
            Repaint();

            _lastGenerationResult = await Session.GenerateObjectAsync(_generationPrompt, _generationQuality);

            _isGenerating = false;
            Repaint();
        }

        private async void RequestLayoutSuggestion()
        {
            if (Session == null) return;

            var objectIds = new System.Collections.Generic.List<string>();
            foreach (var go in Selection.gameObjects)
            {
                var hybrid = go.GetComponent<HybridSceneObject>();
                if (hybrid != null)
                {
                    objectIds.Add(hybrid.ObjectId);
                }
            }

            if (objectIds.Count == 0) return;

            _isRequestingLayout = true;
            Repaint();

            _lastLayoutSuggestion = await Session.GetLayoutSuggestionAsync(objectIds.ToArray(), _layoutConstraints);

            _isRequestingLayout = false;
            Repaint();
        }

        private void ApplyLayoutSuggestions()
        {
            if (_lastLayoutSuggestion?.placements == null || Session?.Registry == null) return;

            Undo.RecordObjects(GetHybridObjectsForPlacements(), "Apply Layout Suggestions");

            foreach (var placement in _lastLayoutSuggestion.placements)
            {
                var obj = Session.Registry.GetById(placement.objectId);
                if (obj != null)
                {
                    obj.transform.position = placement.suggestedPosition;
                    obj.transform.rotation = placement.suggestedRotation;
                }
            }
        }

        private Object[] GetHybridObjectsForPlacements()
        {
            if (_lastLayoutSuggestion?.placements == null) return new Object[0];

            var objects = new System.Collections.Generic.List<Object>();
            foreach (var placement in _lastLayoutSuggestion.placements)
            {
                var obj = Session?.Registry?.GetById(placement.objectId);
                if (obj != null)
                {
                    objects.Add(obj.transform);
                }
            }
            return objects.ToArray();
        }

        private void SelectAllRegisteredObjects()
        {
            if (Session?.Registry == null) return;

            var objects = new System.Collections.Generic.List<GameObject>();
            foreach (var obj in Session.Registry.AllObjects)
            {
                if (obj != null)
                    objects.Add(obj.gameObject);
            }
            Selection.objects = objects.ToArray();
        }
    }
}
