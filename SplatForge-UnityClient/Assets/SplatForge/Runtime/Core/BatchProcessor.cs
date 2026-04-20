using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SplatForge.Network;
using SplatForge.Geometry;

namespace SplatForge.Core
{
    /// <summary>
    /// Handles batch operations for multiple objects
    /// </summary>
    public class BatchProcessor
    {
        private readonly SplatForgeSession _session;

        public event Action<BatchOperationProgress> OnProgressChanged;
        public event Action<BatchOperationResult> OnOperationComplete;

        public bool IsProcessing { get; private set; }
        public float Progress { get; private set; }

        public BatchProcessor(SplatForgeSession session)
        {
            _session = session;
        }

        /// <summary>
        /// Generate multiple objects from prompts
        /// </summary>
        public async Task<BatchOperationResult> GenerateMultipleAsync(
            string[] prompts,
            GenerationQuality quality = GenerationQuality.Medium)
        {
            if (IsProcessing)
            {
                return new BatchOperationResult
                {
                    success = false,
                    errorMessage = "Another batch operation is in progress"
                };
            }

            if (_session?.Server == null || !_session.Server.IsConnected)
            {
                return new BatchOperationResult
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            IsProcessing = true;
            Progress = 0;

            var results = new List<GenerationResult>();
            var errors = new List<string>();

            try
            {
                for (int i = 0; i < prompts.Length; i++)
                {
                    ReportProgress(i, prompts.Length, $"Generating: {prompts[i]}");

                    var result = await _session.GenerateObjectAsync(prompts[i], quality);
                    results.Add(result);

                    if (!result.success)
                    {
                        errors.Add($"[{i}] {prompts[i]}: {result.errorMessage}");
                    }

                    Progress = (float)(i + 1) / prompts.Length;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Exception: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                Progress = 1;
            }

            var operationResult = new BatchOperationResult
            {
                success = errors.Count == 0,
                totalCount = prompts.Length,
                successCount = results.Count - errors.Count,
                failedCount = errors.Count,
                errors = errors.ToArray(),
                generationResults = results.ToArray()
            };

            OnOperationComplete?.Invoke(operationResult);
            return operationResult;
        }

        /// <summary>
        /// Apply layout suggestions to multiple objects
        /// </summary>
        public async Task<BatchOperationResult> ApplyLayoutAsync(
            HybridSceneObject[] objects,
            LayoutConstraints constraints = null)
        {
            if (IsProcessing)
            {
                return new BatchOperationResult
                {
                    success = false,
                    errorMessage = "Another batch operation is in progress"
                };
            }

            if (_session?.Server == null || !_session.Server.IsConnected)
            {
                return new BatchOperationResult
                {
                    success = false,
                    errorMessage = "Not connected to server"
                };
            }

            IsProcessing = true;
            Progress = 0;

            var errors = new List<string>();

            try
            {
                // Get object IDs
                var objectIds = new string[objects.Length];
                for (int i = 0; i < objects.Length; i++)
                {
                    objectIds[i] = objects[i].ObjectId;
                }

                ReportProgress(0, 2, "Requesting layout suggestions...");

                // Get layout suggestions
                var suggestion = await _session.GetLayoutSuggestionAsync(objectIds, constraints);
                Progress = 0.5f;

                if (!suggestion.success)
                {
                    return new BatchOperationResult
                    {
                        success = false,
                        errorMessage = suggestion.errorMessage
                    };
                }

                ReportProgress(1, 2, "Validating and applying placements...");

                // Validate and apply
                var validator = new LayoutValidator();
                var validationResults = validator.ValidateLayout(suggestion, _session.Registry);

                int applied = 0;
                foreach (var placement in suggestion.placements)
                {
                    var obj = _session.Registry.GetById(placement.objectId);
                    if (obj == null)
                    {
                        errors.Add($"Object not found: {placement.objectId}");
                        continue;
                    }

                    if (validationResults.TryGetValue(placement.objectId, out var validation))
                    {
                        if (!validation.isValid)
                        {
                            // Try to find valid position
                            var adjustedPlacement = validator.FindValidPosition(placement, obj);
                            obj.transform.position = adjustedPlacement.suggestedPosition;
                            obj.transform.rotation = adjustedPlacement.suggestedRotation;
                        }
                        else
                        {
                            obj.transform.position = placement.suggestedPosition;
                            obj.transform.rotation = placement.suggestedRotation;
                        }
                        applied++;
                    }
                }

                Progress = 1;

                return new BatchOperationResult
                {
                    success = errors.Count == 0,
                    totalCount = objects.Length,
                    successCount = applied,
                    failedCount = errors.Count,
                    errors = errors.ToArray(),
                    layoutSuggestion = suggestion
                };
            }
            catch (Exception ex)
            {
                return new BatchOperationResult
                {
                    success = false,
                    errorMessage = ex.Message
                };
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Process objects in sequence with custom action
        /// </summary>
        public async Task<BatchOperationResult> ProcessSequentialAsync<T>(
            T[] items,
            Func<T, int, Task<bool>> processor,
            string operationName = "Processing")
        {
            if (IsProcessing)
            {
                return new BatchOperationResult
                {
                    success = false,
                    errorMessage = "Another batch operation is in progress"
                };
            }

            IsProcessing = true;
            Progress = 0;

            var errors = new List<string>();
            int successCount = 0;

            try
            {
                for (int i = 0; i < items.Length; i++)
                {
                    ReportProgress(i, items.Length, $"{operationName} ({i + 1}/{items.Length})");

                    try
                    {
                        var processResult = await processor(items[i], i);
                        if (processResult)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"[{i}] Processing failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"[{i}] {ex.Message}");
                    }

                    Progress = (float)(i + 1) / items.Length;
                }
            }
            finally
            {
                IsProcessing = false;
                Progress = 1;
            }

            var result = new BatchOperationResult
            {
                success = errors.Count == 0,
                totalCount = items.Length,
                successCount = successCount,
                failedCount = errors.Count,
                errors = errors.ToArray()
            };

            OnOperationComplete?.Invoke(result);
            return result;
        }

        private void ReportProgress(int current, int total, string message)
        {
            OnProgressChanged?.Invoke(new BatchOperationProgress
            {
                currentIndex = current,
                totalCount = total,
                progressNormalized = total > 0 ? (float)current / total : 0,
                statusMessage = message
            });
        }
    }

    public struct BatchOperationProgress
    {
        public int currentIndex;
        public int totalCount;
        public float progressNormalized;
        public string statusMessage;
    }

    public class BatchOperationResult
    {
        public bool success;
        public string errorMessage;
        public int totalCount;
        public int successCount;
        public int failedCount;
        public string[] errors;

        // Optional specific results
        public GenerationResult[] generationResults;
        public LayoutSuggestion layoutSuggestion;
    }
}
