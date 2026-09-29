using UnityEngine;
using UnityEditor;
using SplatForge.Core;

namespace SplatForge.Editor
{
    /// <summary>
    /// Project Settings에 SplatForge 설정 패널 추가
    /// </summary>
    public static class SplatForgeSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSplatForgeSettingsProvider()
        {
            var provider = new SettingsProvider("Project/SplatForge", SettingsScope.Project)
            {
                label = "SplatForge",
                guiHandler = (searchContext) =>
                {
                    var settings = SplatForgeSettings.GetSerializedSettings();
                    settings.Update();

                    EditorGUILayout.Space(10);

                    // Server Configuration
                    EditorGUILayout.LabelField("Server Configuration", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(settings.FindProperty("_serverEndpoint"), new GUIContent("Server Endpoint"));
                    EditorGUILayout.PropertyField(settings.FindProperty("_useMockServer"), new GUIContent("Use Mock Server"));
                    EditorGUILayout.PropertyField(settings.FindProperty("_autoConnectOnStart"), new GUIContent("Auto Connect On Start"));
                    EditorGUI.indentLevel--;

                    EditorGUILayout.Space(10);

                    // Mock Server Settings
                    EditorGUILayout.LabelField("Mock Server Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(!settings.FindProperty("_useMockServer").boolValue);
                    EditorGUILayout.PropertyField(settings.FindProperty("_mockMinConnectionDelay"), new GUIContent("Min Connection Delay"));
                    EditorGUILayout.PropertyField(settings.FindProperty("_mockMaxConnectionDelay"), new GUIContent("Max Connection Delay"));
                    EditorGUILayout.PropertyField(settings.FindProperty("_mockFailureRate"), new GUIContent("Failure Rate"));
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;

                    EditorGUILayout.Space(10);

                    // Layout Defaults
                    EditorGUILayout.LabelField("Layout Defaults", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(settings.FindProperty("_defaultMinSpacing"), new GUIContent("Default Min Spacing"));
                    EditorGUILayout.PropertyField(settings.FindProperty("_defaultGroundObjects"), new GUIContent("Ground Objects"));
                    EditorGUILayout.PropertyField(settings.FindProperty("_defaultAvoidOverlap"), new GUIContent("Avoid Overlap"));
                    EditorGUI.indentLevel--;

                    EditorGUILayout.Space(20);

                    // Actions
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select Settings Asset", GUILayout.Width(150)))
                    {
                        Selection.activeObject = SplatForgeSettings.GetOrCreateSettings();
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    }

                    if (GUILayout.Button("Open Control Panel", GUILayout.Width(150)))
                    {
                        Windows.SplatForgeMainWindow.ShowWindow();
                    }

                    EditorGUILayout.EndHorizontal();

                    if (settings.hasModifiedProperties)
                    {
                        settings.ApplyModifiedProperties();
                        SplatForgeSettings.ReloadSettings();
                        SplatForgeSession.ResetEditorSession();
                    }
                },

                keywords = new[] { "SplatForge", "3DGS", "Gaussian", "Splatting", "Server", "Mock" }
            };

            return provider;
        }
    }
}
