using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace SplatForge.Network
{
    /// <summary>
    /// Mock implementation of ISplatForgeServer for testing without a real server
    /// </summary>
    public class MockSplatForgeServer : ISplatForgeServer
    {
        private bool _isConnected;
        private string _endpoint;
        private int _generationCounter;

        public bool IsConnected => _isConnected;
        public string Endpoint => _endpoint;

        // Simulated delay settings
        public float MinConnectionDelaySeconds { get; set; } = 0.3f;
        public float MaxConnectionDelaySeconds { get; set; } = 0.8f;
        public float MinGenerationDelaySeconds { get; set; } = 1.0f;
        public float MaxGenerationDelaySeconds { get; set; } = 3.0f;
        public float MinLayoutDelaySeconds { get; set; } = 0.5f;
        public float MaxLayoutDelaySeconds { get; set; } = 1.5f;
        public float MinCompositionDelaySeconds { get; set; } = 1.5f;
        public float MaxCompositionDelaySeconds { get; set; } = 4.0f;

        // Failure simulation
        public float FailureRate { get; set; } = 0f;

        // Mock layout data (embedded for reliability)
        private static readonly Dictionary<string, MockLayoutData> _mockLayouts = new Dictionary<string, MockLayoutData>
        {
            { "bedroom", CreateBedroomLayout() },
            { "office", CreateOfficeLayout() },
            { "living", CreateLivingRoomLayout() }
        };

        public async Task<bool> ConnectAsync(string endpoint = null)
        {
            _endpoint = endpoint ?? "mock://localhost:8080";

            await SimulateDelay(MinConnectionDelaySeconds, MaxConnectionDelaySeconds);

            if (ShouldFail())
            {
                _isConnected = false;
                return false;
            }

            _isConnected = true;
            Debug.Log($"[Unity Mock] Connected to {_endpoint}");
            return true;
        }

        public void Disconnect()
        {
            _isConnected = false;
            Debug.Log("[Unity Mock] Disconnected");
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

            var startTime = Time.realtimeSinceStartup;
            await SimulateDelay(MinGenerationDelaySeconds, MaxGenerationDelaySeconds);
            var generationTime = Time.realtimeSinceStartup - startTime;

            if (ShouldFail())
            {
                return new GenerationResult
                {
                    success = false,
                    errorMessage = "Mock generation failed (simulated error)"
                };
            }

            _generationCounter++;
            var objectId = $"mock_obj_{_generationCounter:D4}";

            // Generate mock metadata based on prompt
            var metadata = GenerateMockMetadata(objectId, request.prompt);

            Debug.Log($"[Unity Mock] Generated object '{objectId}' for prompt: {request.prompt}");

            return new GenerationResult
            {
                success = true,
                objectId = objectId,
                plyData = null, // Mock doesn't provide actual PLY data
                metadata = metadata,
                generationTimeSeconds = generationTime
            };
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

            await SimulateDelay(MinLayoutDelaySeconds, MaxLayoutDelaySeconds);

            if (ShouldFail())
            {
                return new LayoutSuggestion
                {
                    success = false,
                    errorMessage = "Mock layout suggestion failed (simulated error)"
                };
            }

            var placements = GenerateMockPlacements(request);

            Debug.Log($"[Unity Mock] Generated {placements.Length} placement suggestions");

            return new LayoutSuggestion
            {
                success = true,
                placements = placements,
                reasoning = "Mock layout: Objects placed in a grid pattern with spacing based on constraints."
            };
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

            var startTime = Time.realtimeSinceStartup;
            await SimulateDelay(MinCompositionDelaySeconds, MaxCompositionDelaySeconds);
            var compositionTime = Time.realtimeSinceStartup - startTime;

            if (ShouldFail())
            {
                return new SceneCompositionResult
                {
                    success = false,
                    errorMessage = "Mock scene composition failed (simulated error)"
                };
            }

            Debug.Log("[Unity Mock] ComposeScene: using built-in layout data");

            // Select layout based on prompt keywords
            var layout = SelectLayoutForPrompt(request.prompt);
            var placements = GeneratePlacementsFromLayout(layout, request);

            Debug.Log($"[Unity Mock] Detected room type: '{layout.LayoutId}', returning {placements.Length} objects");

            return new SceneCompositionResult
            {
                success = true,
                placements = placements,
                reasoning = layout.Reasoning,
                compositionTimeSeconds = compositionTime
            };
        }

        private MockLayoutData SelectLayoutForPrompt(string prompt)
        {
            prompt = prompt.ToLower();

            if (prompt.Contains("bedroom") || prompt.Contains("bed") || prompt.Contains("sleep") || prompt.Contains("cozy"))
                return _mockLayouts["bedroom"];

            if (prompt.Contains("office") || prompt.Contains("work") || prompt.Contains("desk") || prompt.Contains("study"))
                return _mockLayouts["office"];

            if (prompt.Contains("living") || prompt.Contains("lounge") || prompt.Contains("sofa") || prompt.Contains("tv"))
                return _mockLayouts["living"];

            // Default to bedroom
            return _mockLayouts["bedroom"];
        }

        private SceneObjectPlacement[] GeneratePlacementsFromLayout(MockLayoutData layout, SceneCompositionRequest request)
        {
            var placements = new List<SceneObjectPlacement>();
            var floorCenter = request.floorStructure?.Center ?? Vector3.zero;
            var floorHeight = request.floorStructure?.floorHeight ?? 0f;
            var maxObjects = request.options?.maxObjects ?? 10;
            var includeDecorations = request.options?.includeDecorations ?? true;

            foreach (var item in layout.Items)
            {
                if (placements.Count >= maxObjects)
                    break;

                if (!includeDecorations && item.Category == "decoration")
                    continue;

                var placement = new SceneObjectPlacement
                {
                    objectId = $"{item.ObjectId}_{_generationCounter++:D4}",
                    assetPath = item.AssetPath,
                    category = item.Category,
                    objectName = item.ObjectName,
                    position = item.Position + floorCenter + Vector3.up * floorHeight,
                    rotation = item.Rotation,
                    scale = item.Scale,
                    metadata = new ObjectMetadataData
                    {
                        objectId = item.ObjectId,
                        objectName = item.ObjectName,
                        category = item.Category,
                        tags = new[] { item.Category, layout.LayoutId },
                        boundsMin = -item.Scale * 0.5f,
                        boundsMax = item.Scale * 0.5f,
                        sourcePrompt = request.prompt,
                        createdAt = DateTime.UtcNow
                    }
                };

                placements.Add(placement);
            }

            return placements.ToArray();
        }

        private ObjectMetadataData GenerateMockMetadata(string objectId, string prompt)
        {
            // Simple category detection from prompt
            var category = DetectCategory(prompt);
            var tags = ExtractTags(prompt);

            // Generate random bounds (typical small object size)
            var size = UnityEngine.Random.Range(0.3f, 1.5f);
            var boundsMin = new Vector3(-size * 0.5f, 0, -size * 0.5f);
            var boundsMax = new Vector3(size * 0.5f, size, size * 0.5f);

            return new ObjectMetadataData
            {
                objectId = objectId,
                objectName = GenerateObjectName(prompt),
                category = category,
                tags = tags,
                boundsMin = boundsMin,
                boundsMax = boundsMax,
                sourcePrompt = prompt,
                createdAt = DateTime.UtcNow
            };
        }

        private string DetectCategory(string prompt)
        {
            prompt = prompt.ToLower();

            if (prompt.Contains("chair") || prompt.Contains("table") || prompt.Contains("sofa") || prompt.Contains("desk"))
                return "furniture";
            if (prompt.Contains("tree") || prompt.Contains("plant") || prompt.Contains("flower") || prompt.Contains("grass"))
                return "vegetation";
            if (prompt.Contains("car") || prompt.Contains("vehicle") || prompt.Contains("bike"))
                return "vehicle";
            if (prompt.Contains("building") || prompt.Contains("house") || prompt.Contains("structure"))
                return "architecture";
            if (prompt.Contains("person") || prompt.Contains("human") || prompt.Contains("character"))
                return "character";

            return "misc";
        }

        private string[] ExtractTags(string prompt)
        {
            // Simple word extraction as tags
            var words = prompt.ToLower().Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var tags = new System.Collections.Generic.List<string>();

            foreach (var word in words)
            {
                if (word.Length > 3 && !IsCommonWord(word))
                {
                    tags.Add(word);
                    if (tags.Count >= 5) break;
                }
            }

            return tags.ToArray();
        }

        private bool IsCommonWord(string word)
        {
            var common = new[] { "the", "and", "with", "that", "this", "from", "have", "been" };
            return Array.IndexOf(common, word) >= 0;
        }

        private string GenerateObjectName(string prompt)
        {
            // Take first few meaningful words
            var words = prompt.Split(' ');
            var name = string.Join("_", words, 0, Math.Min(3, words.Length));
            return name.Length > 32 ? name.Substring(0, 32) : name;
        }

        private PlacementSuggestion[] GenerateMockPlacements(LayoutRequest request)
        {
            if (request.objectIdsToPlace == null || request.objectIdsToPlace.Length == 0)
                return Array.Empty<PlacementSuggestion>();

            var placements = new PlacementSuggestion[request.objectIdsToPlace.Length];
            var constraints = request.constraints ?? new LayoutConstraints();
            var context = request.sceneContext ?? new SceneContextData();

            // Simple grid placement
            var gridSize = Mathf.CeilToInt(Mathf.Sqrt(placements.Length));
            var spacing = constraints.minSpacing + 1f;
            var startX = -spacing * (gridSize - 1) * 0.5f;
            var startZ = -spacing * (gridSize - 1) * 0.5f;

            if (constraints.preferredArea.HasValue)
            {
                startX += constraints.preferredArea.Value.x;
                startZ += constraints.preferredArea.Value.z;
            }

            for (int i = 0; i < placements.Length; i++)
            {
                var gridX = i % gridSize;
                var gridZ = i / gridSize;

                var position = new Vector3(
                    startX + gridX * spacing,
                    constraints.groundObjects ? context.groundPlaneHeight : 0f,
                    startZ + gridZ * spacing
                );

                // Random rotation around Y axis
                var rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

                placements[i] = new PlacementSuggestion
                {
                    objectId = request.objectIdsToPlace[i],
                    suggestedPosition = position,
                    suggestedRotation = rotation,
                    confidence = UnityEngine.Random.Range(0.7f, 1f),
                    rationale = $"Grid position ({gridX}, {gridZ}) with {constraints.minSpacing}m minimum spacing"
                };
            }

            return placements;
        }

        private async Task SimulateDelay(float min, float max)
        {
            var delay = UnityEngine.Random.Range(min, max);
            await Task.Delay(TimeSpan.FromSeconds(delay));
        }

        private bool ShouldFail()
        {
            return FailureRate > 0 && UnityEngine.Random.value < FailureRate;
        }

        // ============================================
        // Mock Layout Data
        // ============================================

        private class MockLayoutData
        {
            public string LayoutId;
            public string Reasoning;
            public List<MockLayoutItem> Items = new List<MockLayoutItem>();
        }

        private class MockLayoutItem
        {
            public string ObjectId;
            public string AssetPath;
            public string Category;
            public string ObjectName;
            public Vector3 Position;
            public Vector3 Rotation;
            public Vector3 Scale = Vector3.one;
        }

        private static MockLayoutData CreateBedroomLayout()
        {
            return new MockLayoutData
            {
                LayoutId = "cozy_bedroom",
                Reasoning = "Placed bed against the back wall for a cozy feel. Nightstand beside the bed for convenience. Desk and chair in corner for workspace. Lamp on nightstand for ambient lighting.",
                Items = new List<MockLayoutItem>
                {
                    new MockLayoutItem { ObjectId = "bed_001", AssetPath = "MockAssets/bed_01", Category = "furniture", ObjectName = "Double Bed", Position = new Vector3(0, 0, 2), Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "nightstand_001", AssetPath = "MockAssets/nightstand_01", Category = "furniture", ObjectName = "Nightstand", Position = new Vector3(-1.5f, 0, 2), Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "desk_001", AssetPath = "MockAssets/desk_01", Category = "furniture", ObjectName = "Writing Desk", Position = new Vector3(3, 0, -1), Rotation = new Vector3(0, -90, 0), Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "chair_001", AssetPath = "MockAssets/chair_01", Category = "furniture", ObjectName = "Desk Chair", Position = new Vector3(2, 0, -1), Rotation = new Vector3(0, 90, 0), Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "lamp_001", AssetPath = "MockAssets/lamp_01", Category = "decoration", ObjectName = "Table Lamp", Position = new Vector3(-1.5f, 0.6f, 2), Scale = new Vector3(0.5f, 0.5f, 0.5f) }
                }
            };
        }

        private static MockLayoutData CreateOfficeLayout()
        {
            return new MockLayoutData
            {
                LayoutId = "modern_office",
                Reasoning = "Desk positioned facing entrance for commanding view. Chair for ergonomic access. Bookshelf along side wall. Plants in corners for ambiance.",
                Items = new List<MockLayoutItem>
                {
                    new MockLayoutItem { ObjectId = "desk_002", AssetPath = "MockAssets/desk_01", Category = "furniture", ObjectName = "Executive Desk", Position = new Vector3(0, 0, 2), Rotation = new Vector3(0, 180, 0), Scale = new Vector3(1.2f, 1, 1) },
                    new MockLayoutItem { ObjectId = "chair_002", AssetPath = "MockAssets/chair_01", Category = "furniture", ObjectName = "Office Chair", Position = new Vector3(0, 0, 1), Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "bookshelf_001", AssetPath = "MockAssets/bookshelf_01", Category = "furniture", ObjectName = "Bookshelf", Position = new Vector3(-3, 0, 0), Rotation = new Vector3(0, 90, 0), Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "plant_001", AssetPath = "MockAssets/plant_01", Category = "decoration", ObjectName = "Potted Plant", Position = new Vector3(3, 0, 2.5f), Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "plant_002", AssetPath = "MockAssets/plant_01", Category = "decoration", ObjectName = "Corner Plant", Position = new Vector3(-3, 0, 2.5f), Rotation = new Vector3(0, 45, 0), Scale = new Vector3(0.8f, 0.8f, 0.8f) }
                }
            };
        }

        private static MockLayoutData CreateLivingRoomLayout()
        {
            return new MockLayoutData
            {
                LayoutId = "living_room",
                Reasoning = "Sofa facing TV area for optimal viewing. Coffee table centered for accessibility. Armchair angled for conversation. Floor lamp provides ambient lighting. Area rug defines seating zone.",
                Items = new List<MockLayoutItem>
                {
                    new MockLayoutItem { ObjectId = "sofa_001", AssetPath = "MockAssets/sofa_01", Category = "furniture", ObjectName = "L-Shaped Sofa", Position = new Vector3(0, 0, -2), Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "coffee_table_001", AssetPath = "MockAssets/table_01", Category = "furniture", ObjectName = "Coffee Table", Position = Vector3.zero, Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "tv_stand_001", AssetPath = "MockAssets/cabinet_01", Category = "furniture", ObjectName = "TV Stand", Position = new Vector3(0, 0, 3), Rotation = new Vector3(0, 180, 0), Scale = new Vector3(1.5f, 1, 1) },
                    new MockLayoutItem { ObjectId = "armchair_001", AssetPath = "MockAssets/chair_01", Category = "furniture", ObjectName = "Armchair", Position = new Vector3(-2.5f, 0, 0), Rotation = new Vector3(0, 45, 0), Scale = Vector3.one },
                    new MockLayoutItem { ObjectId = "lamp_002", AssetPath = "MockAssets/lamp_01", Category = "decoration", ObjectName = "Floor Lamp", Position = new Vector3(-3, 0, -2), Scale = new Vector3(1, 1.5f, 1) },
                    new MockLayoutItem { ObjectId = "rug_001", AssetPath = "MockAssets/rug_01", Category = "decoration", ObjectName = "Area Rug", Position = new Vector3(0, 0.01f, -0.5f), Scale = new Vector3(2, 1, 1.5f) }
                }
            };
        }
    }
}
