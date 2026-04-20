"""Object generation models matching Unity's ServerMessages.cs"""

from enum import IntEnum
from typing import Optional
from pydantic import BaseModel, Field

from .compose import ObjectMetadata


class GenerationQuality(IntEnum):
    """Generation quality levels matching Unity's enum"""

    LOW = 0
    MEDIUM = 1
    HIGH = 2


class GenerationRequest(BaseModel):
    """Request for 3DGS object generation"""

    prompt: str
    negative_prompt: str = Field(default="", alias="negativePrompt")
    quality: int = GenerationQuality.MEDIUM
    seed: int = -1

    model_config = {"populate_by_name": True}


class GenerationResult(BaseModel):
    """Result of object generation"""

    success: bool
    error_message: Optional[str] = Field(default=None, alias="errorMessage")
    object_id: str = Field(default="", alias="objectId")
    ply_data: Optional[str] = Field(default=None, alias="plyData")  # Base64 encoded
    metadata: Optional[ObjectMetadata] = None
    generation_time_seconds: float = Field(default=0.0, alias="generationTimeSeconds")

    model_config = {"populate_by_name": True}
