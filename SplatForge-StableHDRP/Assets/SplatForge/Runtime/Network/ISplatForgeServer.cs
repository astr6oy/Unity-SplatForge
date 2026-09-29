using System.Threading.Tasks;

namespace SplatForge.Network
{
    /// <summary>
    /// Interface for SplatForge server communication
    /// </summary>
    public interface ISplatForgeServer
    {
        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <param name="endpoint">Server endpoint URL. If null, uses default.</param>
        /// <returns>True if connection successful</returns>
        Task<bool> ConnectAsync(string endpoint = null);

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Whether currently connected to the server
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Server endpoint URL
        /// </summary>
        string Endpoint { get; }

        /// <summary>
        /// Generate a 3DGS object from a text prompt
        /// </summary>
        Task<GenerationResult> GenerateObjectAsync(GenerationRequest request);

        /// <summary>
        /// Get layout suggestions for placing objects in the scene
        /// </summary>
        Task<LayoutSuggestion> GetLayoutSuggestionAsync(LayoutRequest request);

        /// <summary>
        /// Compose a complete scene from a prompt with floor structure
        /// Returns layout with asset references for instantiation
        /// </summary>
        Task<SceneCompositionResult> ComposeSceneAsync(SceneCompositionRequest request);
    }
}
