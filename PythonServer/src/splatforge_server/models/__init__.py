"""Pydantic models for API request/response schemas"""

from .common import Vector3, Quaternion
from .compose import (
    FloorStructureData,
    WallSegment,
    SceneCompositionOptions,
    SceneCompositionRequest,
    ObjectMetadata,
    SceneObjectPlacement,
    SceneCompositionResult,
)
from .generate import GenerationQuality, GenerationRequest, GenerationResult
from .layout import (
    ObjectInfo,
    SceneContext,
    LayoutConstraints,
    LayoutRequest,
    PositionSuggestion,
    LayoutSuggestion,
)

__all__ = [
    # Common
    "Vector3",
    "Quaternion",
    # Compose
    "FloorStructureData",
    "WallSegment",
    "SceneCompositionOptions",
    "SceneCompositionRequest",
    "ObjectMetadata",
    "SceneObjectPlacement",
    "SceneCompositionResult",
    # Generate
    "GenerationQuality",
    "GenerationRequest",
    "GenerationResult",
    # Layout
    "ObjectInfo",
    "SceneContext",
    "LayoutConstraints",
    "LayoutRequest",
    "PositionSuggestion",
    "LayoutSuggestion",
]
