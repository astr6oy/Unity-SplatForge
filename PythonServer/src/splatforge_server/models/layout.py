"""Layout suggestion models matching Unity's ServerMessages.cs"""

from typing import Optional
from pydantic import BaseModel, Field

from .common import Vector3


class ObjectInfo(BaseModel):
    """Information about an object in the scene"""

    object_id: str = Field(alias="objectId")
    object_name: str = Field(alias="objectName")
    category: str = ""
    position: Vector3
    rotation: Vector3
    bounds_min: Vector3 = Field(alias="boundsMin")
    bounds_max: Vector3 = Field(alias="boundsMax")

    model_config = {"populate_by_name": True}


class SceneContext(BaseModel):
    """Current scene state for layout requests"""

    existing_objects: list[ObjectInfo] = Field(default_factory=list, alias="existingObjects")
    scene_bounds_min: Vector3 = Field(alias="sceneBoundsMin")
    scene_bounds_max: Vector3 = Field(alias="sceneBoundsMax")
    ground_plane_normal: Vector3 = Field(
        default_factory=Vector3.up, alias="groundPlaneNormal"
    )
    ground_plane_height: float = Field(default=0.0, alias="groundPlaneHeight")

    model_config = {"populate_by_name": True}


class LayoutConstraints(BaseModel):
    """Constraints for layout generation"""

    avoid_overlap: bool = Field(default=True, alias="avoidOverlap")
    ground_objects: bool = Field(default=True, alias="groundObjects")
    min_spacing: float = Field(default=0.5, alias="minSpacing")

    model_config = {"populate_by_name": True}


class LayoutRequest(BaseModel):
    """Request for layout suggestions"""

    scene_context: SceneContext = Field(alias="sceneContext")
    object_ids_to_place: list[str] = Field(alias="objectIdsToPlace")
    constraints: LayoutConstraints = Field(default_factory=LayoutConstraints)

    model_config = {"populate_by_name": True}


class PositionSuggestion(BaseModel):
    """Suggested position for an object"""

    object_id: str = Field(alias="objectId")
    suggested_position: Vector3 = Field(alias="suggestedPosition")
    suggested_rotation: Vector3 = Field(alias="suggestedRotation")
    confidence: float = 1.0
    reasoning: str = ""

    model_config = {"populate_by_name": True}


class LayoutSuggestion(BaseModel):
    """Result of layout suggestion request"""

    success: bool
    error_message: Optional[str] = Field(default=None, alias="errorMessage")
    suggestions: list[PositionSuggestion] = Field(default_factory=list)
    overall_reasoning: str = Field(default="", alias="overallReasoning")

    model_config = {"populate_by_name": True}
