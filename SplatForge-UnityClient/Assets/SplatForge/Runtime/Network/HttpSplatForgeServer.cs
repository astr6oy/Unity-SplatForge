using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SplatForge.Network
{
    /// <summary>
    /// HTTP-based server implementation for connecting to Python backend
    /// </summary>
    public class HttpSplatForgeServer : ISplatForgeServer
    {
        private string _endpoint;
        private bool _isConnected;

        public bool IsConnected => _isConnected;
        public string Endpoint => _endpoint;

        public async Task<bool> ConnectAsync(string endpoint)
        {
            _endpoint = endpoint.TrimEnd('/');

            try
            {
                // Test connection with health check
                using var request = UnityWebRequest.Get($"{_endpoint}/api/v1/health");
                request.timeout = 5;

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _isConnected = true;
                    Debug.Log($"[HttpSplatForgeServer] Connected to {_endpoint}");
                    return true;
                }
                else
                {
                    Debug.LogError($"[HttpSplatForgeServer] Connection failed: {request.error}");
                    _isConnected = false;
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[HttpSplatForgeServer] Connection error: {e.Message}");
                _isConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            Debug.Log("[HttpSplatForgeServer] Disconnected");
        }

        public async Task<GenerationResult> GenerateObjectAsync(GenerationRequest request)
        {
            if (!_isConnected)
            {
                return new GenerationResult
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            try
            {
                var json = JsonUtility.ToJson(request);
                var result = await PostJsonAsync<GenerationResult>("/api/v1/generate", json);
                return result;
            }
            catch (Exception e)
            {
                return new GenerationResult
                {
                    success = false,
                    errorMessage = e.Message
                };
            }
        }

        public async Task<LayoutSuggestion> GetLayoutSuggestionAsync(LayoutRequest request)
        {
            if (!_isConnected)
            {
                return new LayoutSuggestion
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            try
            {
                var json = JsonUtility.ToJson(request);
                var result = await PostJsonAsync<LayoutSuggestion>("/api/v1/layout", json);
                return result;
            }
            catch (Exception e)
            {
                return new LayoutSuggestion
                {
                    success = false,
                    errorMessage = e.Message
                };
            }
        }

        public async Task<SceneCompositionResult> ComposeSceneAsync(SceneCompositionRequest request)
        {
            if (!_isConnected)
            {
                return new SceneCompositionResult
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            try
            {
                var json = JsonUtility.ToJson(request);
                var result = await PostJsonAsync<SceneCompositionResult>("/api/v1/compose", json);
                Debug.Log($"[HTTP] Received {result.placements?.Length ?? 0} placements from Python server");
                return result;
            }
            catch (Exception e)
            {
                return new SceneCompositionResult
                {
                    success = false,
                    errorMessage = e.Message
                };
            }
        }

        private async Task<T> PostJsonAsync<T>(string path, string jsonBody) where T : class
        {
            var url = $"{_endpoint}{path}";

            Debug.Log($"[HTTP→Server] POST {path}");
            Debug.Log($"[HTTP→Server] Request: {TruncateJson(jsonBody, 300)}");

            var startTime = Time.realtimeSinceStartup;

            using var request = new UnityWebRequest(url, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 30;

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            var elapsedMs = (int)((Time.realtimeSinceStartup - startTime) * 1000);

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"HTTP Error: {request.error} - {request.downloadHandler?.text}");
            }

            var responseJson = request.downloadHandler.text;
            Debug.Log($"[HTTP←Server] Response ({elapsedMs}ms): {TruncateJson(responseJson, 300)}");
            return JsonUtility.FromJson<T>(responseJson);
        }

        private static string TruncateJson(string json, int maxLength)
        {
            if (string.IsNullOrEmpty(json) || json.Length <= maxLength)
                return json;
            return json.Substring(0, maxLength) + "...";
        }
    }
}
