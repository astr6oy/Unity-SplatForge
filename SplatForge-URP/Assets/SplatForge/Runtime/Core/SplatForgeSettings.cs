using UnityEngine;

namespace SplatForge.Core
{
    /// <summary>
    /// 프로젝트 전역 SplatForge 설정 (ScriptableObject)
    /// 에디터와 런타임 모두 동일한 설정을 공유
    /// </summary>
    [CreateAssetMenu(fileName = "SplatForgeSettings", menuName = "SplatForge/Settings")]
    public class SplatForgeSettings : ScriptableObject
    {
        private const string DefaultSettingsPath = "SplatForgeSettings";

        private static SplatForgeSettings _instance;

        [Header("Server Configuration")]
        [Tooltip("서버 엔드포인트 URL")]
        [SerializeField] private string _serverEndpoint = "http://localhost:8080";

        [Tooltip("Mock 서버 사용 여부 (테스트용)")]
        [SerializeField] private bool _useMockServer = true;

        [Tooltip("시작 시 자동으로 서버에 연결")]
        [SerializeField] private bool _autoConnectOnStart = false;

        [Header("Mock Server Settings")]
        [Tooltip("Mock 서버 최소 연결 지연 (초)")]
        [SerializeField] private float _mockMinConnectionDelay = 0.3f;

        [Tooltip("Mock 서버 최대 연결 지연 (초)")]
        [SerializeField] private float _mockMaxConnectionDelay = 0.8f;

        [Tooltip("Mock 서버 실패 확률 (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _mockFailureRate = 0f;

        [Header("Layout Defaults")]
        [Tooltip("기본 최소 오브젝트 간격")]
        [SerializeField] private float _defaultMinSpacing = 0.5f;

        [Tooltip("기본적으로 오브젝트를 바닥에 배치")]
        [SerializeField] private bool _defaultGroundObjects = true;

        [Tooltip("기본적으로 오브젝트 겹침 방지")]
        [SerializeField] private bool _defaultAvoidOverlap = true;

        // Properties
        public string ServerEndpoint
        {
            get => _serverEndpoint;
            set => _serverEndpoint = value;
        }

        public bool UseMockServer
        {
            get => _useMockServer;
            set => _useMockServer = value;
        }

        public bool AutoConnectOnStart
        {
            get => _autoConnectOnStart;
            set => _autoConnectOnStart = value;
        }

        public float MockMinConnectionDelay => _mockMinConnectionDelay;
        public float MockMaxConnectionDelay => _mockMaxConnectionDelay;
        public float MockFailureRate => _mockFailureRate;

        public float DefaultMinSpacing => _defaultMinSpacing;
        public bool DefaultGroundObjects => _defaultGroundObjects;
        public bool DefaultAvoidOverlap => _defaultAvoidOverlap;

        /// <summary>
        /// 전역 설정 인스턴스 (Resources 폴더에서 자동 로드)
        /// </summary>
        public static SplatForgeSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<SplatForgeSettings>(DefaultSettingsPath);

                    if (_instance == null)
                    {
                        Debug.LogWarning($"[SplatForgeSettings] Settings not found at Resources/{DefaultSettingsPath}. Using default settings.");
                        _instance = CreateInstance<SplatForgeSettings>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 설정 강제 리로드
        /// </summary>
        public static void ReloadSettings()
        {
            _instance = null;
            var _ = Instance; // Trigger reload
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 설정 에셋 생성 또는 선택
        /// </summary>
        public static SplatForgeSettings GetOrCreateSettings()
        {
            var settings = Resources.Load<SplatForgeSettings>(DefaultSettingsPath);

            if (settings == null)
            {
                // Resources 폴더 확인/생성
                var resourcesPath = "Assets/SplatForge/Resources";
                if (!System.IO.Directory.Exists(resourcesPath))
                {
                    System.IO.Directory.CreateDirectory(resourcesPath);
                    UnityEditor.AssetDatabase.Refresh();
                }

                // 설정 에셋 생성
                settings = CreateInstance<SplatForgeSettings>();
                UnityEditor.AssetDatabase.CreateAsset(settings, $"{resourcesPath}/{DefaultSettingsPath}.asset");
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"[SplatForgeSettings] Created settings asset at {resourcesPath}/{DefaultSettingsPath}.asset");
            }

            return settings;
        }

        /// <summary>
        /// Project Settings에서 사용할 SerializedObject 반환
        /// </summary>
        public static UnityEditor.SerializedObject GetSerializedSettings()
        {
            return new UnityEditor.SerializedObject(GetOrCreateSettings());
        }
#endif
    }
}
