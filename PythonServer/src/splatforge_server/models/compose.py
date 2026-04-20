"""Scene Composition models matching Unity's ServerMessages.cs"""

from datetime import datetime
from typing import Optional
from pydantic import BaseModel, Field

from .common import Vector3


class WallSegment(BaseModel):
    """Wall segment definition"""

    start: Vector3
    end: Vector3
    height: float = 2.5


class FloorStructureData(BaseModel):
    """Floor structure data matching Unity's FloorStructureData"""

    bounds_min: Vector3 = Field(alias="boundsMin")
    bounds_max: Vector3 = Field(alias="boundsMax")
    floor_height: float = Field(default=0.0, alias="floorHeight")
    walls: list[WallSegment] = Field(default_factory=list)

    model_config = {"populate_by_name": True}


class SceneCompositionOptions(BaseModel):
    """Options for scene composition"""

    style: str = "default"
    quality: int = 1  # 0=Low, 1=Medium, 2=High
    seed: int = -1  # -1 = random
    max_objects: int = Field(default=10, alias="maxObjects")
    include_decorations: bool = Field(default=True, alias="includeDecorations")

    model_config = {"populate_by_name": True}


class SceneCompositionRequest(BaseModel):
    """Request for scene composition matching Unity's SceneCompositionRequest"""

    prompt: str
    floor_structure: Optional[FloorStructureData] = Field(default=None, alias="floorStructure")
    options: SceneCompositionOptions = Field(default_factory=SceneCompositionOptions)

    model_config = {"populate_by_name": True}


class ObjectMetadata(BaseModel):
    """Object metadata matching Unity's ObjectMetadataData"""

    object_id: str = Field(alias="objectId")
    object_name: str = Field(alias="objectName")
    category: str = ""
    tags: list[str] = Field(default_factory=list)
    bounds_min: Vector3 = Field(default_factory=Vector3.zero, alias="boundsMin")
    bounds_max: Vector3 = Field(default_factory=Vector3.zero, alias="boundsMax")
    source_prompt: str = Field(default="", alias="sourcePrompt")
    created_at: str = Field(default="", alias="createdAt")

    model_config = {"populate_by_name": True}


class SceneObjectPlacement(BaseModel):
    """Placement data for a single object matching Unity's SceneObjectPlacement"""

    object_id: str = Field(alias="objectId")
    asset_path: str = Field(default="", alias="assetPath")
    category: str = ""
    object_name: str = Field(alias="objectName")
    position: Vector3
    rotation: Vector3  # Euler angles in degrees
    scale: Vector3 = Field(default_factory=Vector3.one)
    metadata: Optional[ObjectMetadata] = None

    model_config = {"populate_by_name": True}


class SceneCompositionResult(BaseModel):
    """Result of scene composition matching Unity's SceneCompositionResult"""

    success: bool
    error_message: Optional[str] = Field(default=None, alias="errorMessage")
    placements: list[SceneObjectPlacement] = Field(default_factory=list)
    reasoning: str = ""
    composition_time_seconds: float = Field(default=0.0, alias="compositionTimeSeconds")

    model_config = {"populate_by_name": True}
