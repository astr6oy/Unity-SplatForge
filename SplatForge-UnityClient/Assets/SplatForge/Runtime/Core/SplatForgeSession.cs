using System;
using System.Threading.Tasks;
using UnityEngine;
using SplatForge.Network;
using SplatForge.Geometry;

namespace SplatForge.Core
{
    /// <summary>
    /// SplatForge 세션 관리자
    /// - 에디터 모드: EditorSession 자동 생성 (MonoBehaviour 불필요)
    /// - 런타임 모드: 자동 생성 또는 씬에 배치된 인스턴스 사용
    ///
    /// 사용법: SplatForgeSession.Current를 통해 접근 (자동 초기화)
    /// </summary>
    public class SplatForgeSession : MonoBehaviour, ISession
    {
        private static SplatForgeSession _runtimeInstance;
        private static EditorSession _editorSession;

        /// <summary>
        /// 현재 활성 세션 (자동 생성됨)
        /// - 에디터: EditorSession 반환
        /// - 런타임: SplatForgeSession 반환 (없으면 자동 생성)
        /// </summary>
        public static ISession Current
        {
            get
            {
                // 런타임 인스턴스 우선
                if (_runtimeInstance != null)
                    return _runtimeInstance;

                // 에디터에서는 EditorSession 사용
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (_editorSession == null)
                    {
                        _editorSession = new EditorSession();
                    }
                    return _editorSession;
                }
#endif
                // 런타임에서 기존 인스턴스 찾기
                _runtimeInstance = FindFirstObjectByType<SplatForgeSession>();

                // 없으면 자동 생성
                if (_runtimeInstance == null)
                {
                    var go = new GameObject("[SplatForge Session]");
                    _runtimeInstance = go.AddComponent<SplatForgeSession>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[SplatForgeSession] Auto-created runtime session");
                }

                return _runtimeInstance;
            }
        }

        /// <summary>
        /// 런타임 인스턴스 (MonoBehaviour)
        /// </summary>
        public static SplatForgeSession Instance => _runtimeInstance;

#if UNITY_EDITOR
        /// <summary>
        /// 에디터 세션 재초기화 (설정 변경 시 호출)
        /// </summary>
        public static void ResetEditorSession()
        {
            if (_editorSession != null)
            {
                _editorSession.Disconnect();
                _editorSession.ReinitializeServer();
                Debug.Log("[SplatForgeSession] Editor session reset due to settings change");
            }
        }
#endif

        [Header("Override Settings (Optional)")]
        [Tooltip("체크하면 SplatForgeSettings 대신 이 컴포넌트의 설정 사용")]
        [SerializeField] private bool _overrideGlobalSettings = false;

        [SerializeField] private string _serverEndpoint = "http://localhost:8080";
        [SerializeField] private bool _useMockServer = true;
        [SerializeField] private bool _autoConnectOnStart = false;

        private ISplatForgeServer _server;
        private SceneObjectRegistry _registry;

        // ISession 구현
        public ISplatForgeServer Server => _server;
        public SceneObjectRegistry Registry => _registry;
        public bool IsConnected => _server?.IsConnected ?? false;

        public string ServerEndpoint => _overrideGlobalSettings ? _serverEndpoint : SplatForgeSettings.Instance.ServerEndpoint;
        public bool UseMockServer => _overrideGlobalSettings ? _useMockServer : SplatForgeSettings.Instance.UseMockServer;

        public event Action<bool> OnConnectionChanged;
        public event Action<GenerationResult> OnObjectGenerated;
        public event Action<LayoutSuggestion> OnLayoutSuggestionReceived;
        public event Action<SceneCompositionResult> OnSceneComposed;

        private void Awake()
        {
            if (_runtimeInstance != null && _runtimeInstance != this)
            {
                Debug.LogWarning("[SplatForgeSession] Multiple instances detected. Destroying duplicate.");
                DestroyImmediate(gameObject);
                return;
            }
            _runtimeInstance = this;

            _registry = new SceneObjectRegistry();
            InitializeServer();
        }

        private void Start()
        {
            var autoConnect = _overrideGlobalSettings ? _autoConnectOnStart : SplatForgeSettings.Instance.AutoConnectOnStart;
            if (autoConnect)
            {
                _ = ConnectAsync();
            }
        }

        private void OnDestroy()
        {
            if (_runtimeInstance == this)
            {
                _runtimeInstance = null;
            }
            _server?.Disconnect();
        }

        private void InitializeServer()
        {
            var settings = SplatForgeSettings.Instance;

            if (UseMockServer)
            {
                var mockServer = new MockSplatForgeServer
                {
                    MinConnectionDelaySeconds = settings.MockMinConnectionDelay,
                    MaxConnectionDelaySeconds = settings.MockMaxConnectionDelay,
                    FailureRate = settings.MockFailureRate
                };
                _server = mockServer;
                Debug.Log("[SplatForgeSession] Using Mock Server");
            }
            else
            {
                _server = new HttpSplatForgeServer();
                Debug.Log($"[SplatForgeSession] Using HTTP Server: {ServerEndpoint}");
            }
        }

        public async Task<bool> ConnectAsync()
        {
            if (_server == null)
            {
                InitializeServer();
            }

            var result = await _server.ConnectAsync(ServerEndpoint);
            OnConnectionChanged?.Invoke(result);
            return result;
        }

        public void Disconnect()
        {
            _server?.Disconnect();
            OnConnectionChanged?.Invoke(false);
        }

        public async Task<GenerationResult> GenerateObjectAsync(string prompt, GenerationQuality quality = GenerationQuality.Medium)
        {
            if (_server == null || !_server.IsConnected)
            {
                return new GenerationResult
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            var request = new GenerationRequest(prompt) { quality = quality };
            var result = await _server.GenerateObjectAsync(request);
            OnObjectGenerated?.Invoke(result);
            return result;
        }

        public async Task<LayoutSuggestion> GetLayoutSuggestionAsync(string[] objectIds, LayoutConstraints constraints = null)
        {
            if (_server == null || !_server.IsConnected)
            {
                return new LayoutSuggestion
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            var sceneContext = _registry.GetSceneContext();
            var settings = SplatForgeSettings.Instance;

            var request = new LayoutRequest
            {
                sceneContext = sceneContext,
                objectIdsToPlace = objectIds,
                constraints = constraints ?? new LayoutConstraints
                {
                    minSpacing = settings.DefaultMinSpacing,
                    groundObjects = settings.DefaultGroundObjects,
                    avoidOverlap = settings.DefaultAvoidOverlap
                }
            };

            var result = await _server.GetLayoutSuggestionAsync(request);
            OnLayoutSuggestionReceived?.Invoke(result);
            return result;
        }

        public async Task<SceneCompositionResult> ComposeSceneAsync(string prompt, FloorStructure floorStructure, SceneCompositionOptions options = null)
        {
            if (_server == null || !_server.IsConnected)
            {
                return new SceneCompositionResult
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            var request = new SceneCompositionRequest(prompt)
            {
                floorStructure = floorStructure?.ToData(),
                options = options ?? new SceneCompositionOptions()
            };

            var result = await _server.ComposeSceneAsync(request);
            OnSceneComposed?.Invoke(result);
            return result;
        }

        public void RegisterObject(HybridSceneObject obj) => _registry?.Register(obj);
        public void UnregisterObject(HybridSceneObject obj) => _registry?.Unregister(obj);
    }

    /// <summary>
    /// 세션 공통 인터페이스
    /// </summary>
    public interface ISession
    {
        ISplatForgeServer Server { get; }
        SceneObjectRegistry Registry { get; }
        bool IsConnected { get; }
        string ServerEndpoint { get; }
        bool UseMockServer { get; }

        Task<bool> ConnectAsync();
        void Disconnect();
        Task<GenerationResult> GenerateObjectAsync(string prompt, GenerationQuality quality = GenerationQuality.Medium);
        Task<LayoutSuggestion> GetLayoutSuggestionAsync(string[] objectIds, LayoutConstraints constraints = null);
        Task<SceneCompositionResult> ComposeSceneAsync(string prompt, FloorStructure floorStructure, SceneCompositionOptions options = null);
    }

    /// <summary>
    /// 에디터 전용 세션 (MonoBehaviour 불필요)
    /// </summary>
    public class EditorSession : ISession
    {
        private ISplatForgeServer _server;
        private SceneObjectRegistry _registry;

        public ISplatForgeServer Server => _server;
        public SceneObjectRegistry Registry => _registry;
        public bool IsConnected => _server?.IsConnected ?? false;
        public string ServerEndpoint => SplatForgeSettings.Instance.ServerEndpoint;
        public bool UseMockServer => SplatForgeSettings.Instance.UseMockServer;

        public event Action<bool> OnConnectionChanged;
        public event Action<GenerationResult> OnObjectGenerated;
        public event Action<LayoutSuggestion> OnLayoutSuggestionReceived;
        public event Action<SceneCompositionResult> OnSceneComposed;

        public EditorSession()
        {
            _registry = new SceneObjectRegistry();
            InitializeServer();
        }

        private void InitializeServer()
        {
            var settings = SplatForgeSettings.Instance;

            if (settings.UseMockServer)
            {
                var mockServer = new MockSplatForgeServer
                {
                    MinConnectionDelaySeconds = settings.MockMinConnectionDelay,
                    MaxConnectionDelaySeconds = settings.MockMaxConnectionDelay,
                    FailureRate = settings.MockFailureRate
                };
                _server = mockServer;
                Debug.Log("[EditorSession] Using Mock Server");
            }
            else
            {
                _server = new HttpSplatForgeServer();
                Debug.Log($"[EditorSession] Using HTTP Server: {settings.ServerEndpoint}");
            }
        }

        public void ReinitializeServer()
        {
            _server?.Disconnect();
            InitializeServer();
        }

        public async Task<bool> ConnectAsync()
        {
            if (_server == null)
            {
                InitializeServer();
            }

            var result = await _server.ConnectAsync(ServerEndpoint);
            OnConnectionChanged?.Invoke(result);
            return result;
        }

        public void Disconnect()
        {
            _server?.Disconnect();
            OnConnectionChanged?.Invoke(false);
        }

        public async Task<GenerationResult> GenerateObjectAsync(string prompt, GenerationQuality quality = GenerationQuality.Medium)
        {
            if (_server == null || !_server.IsConnected)
            {
                return new GenerationResult
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            var request = new GenerationRequest(prompt) { quality = quality };
            var result = await _server.GenerateObjectAsync(request);
            OnObjectGenerated?.Invoke(result);
            return result;
        }

        public async Task<LayoutSuggestion> GetLayoutSuggestionAsync(string[] objectIds, LayoutConstraints constraints = null)
        {
            if (_server == null || !_server.IsConnected)
            {
                return new LayoutSuggestion
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            var sceneContext = _registry.GetSceneContext();
            var settings = SplatForgeSettings.Instance;

            var request = new LayoutRequest
            {
                sceneContext = sceneContext,
                objectIdsToPlace = objectIds,
                constraints = constraints ?? new LayoutConstraints
                {
                    minSpacing = settings.DefaultMinSpacing,
                    groundObjects = settings.DefaultGroundObjects,
                    avoidOverlap = settings.DefaultAvoidOverlap
                }
            };

            var result = await _server.GetLayoutSuggestionAsync(request);
            OnLayoutSuggestionReceived?.Invoke(result);
            return result;
        }

        public async Task<SceneCompositionResult> ComposeSceneAsync(string prompt, FloorStructure floorStructure, SceneCompositionOptions options = null)
        {
            if (_server == null || !_server.IsConnected)
            {
                return new SceneCompositionResult
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            var request = new SceneCompositionRequest(prompt)
            {
                floorStructure = floorStructure?.ToData(),
                options = options ?? new SceneCompositionOptions()
            };

            var result = await _server.ComposeSceneAsync(request);
            OnSceneComposed?.Invoke(result);
            return result;
        }
    }
}
