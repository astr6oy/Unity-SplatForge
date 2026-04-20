"""Scene composition service"""

import logging
import time
import uuid
from datetime import datetime, timezone

from ..llm import LLMProvider, LayoutPlan
from ..models import (
    Vector3,
    SceneCompositionRequest,
    SceneCompositionResult,
    SceneObjectPlacement,
    ObjectMetadata,
)
from .asset_manager import AssetManager

logger = logging.getLogger("splatforge.composer")


class SceneComposerService:
    """Service for composing 3D scenes using LLM-based layout generation"""

    def __init__(self, llm_provider: LLMProvider, asset_manager: AssetManager):
        self.llm = llm_provider
        self.assets = asset_manager

    async def compose(self, request: SceneCompositionRequest) -> SceneCompositionResult:
        """
        Compose a scene based on the request.

        1. Parse floor structure
        2. Get available objects from asset manager
        3. Generate layout using LLM
        4. Map to asset paths and create placements
        """
        start_time = time.time()

        try:
            # Extract floor bounds
            if request.floor_structure:
                floor_min = request.floor_structure.bounds_min
                floor_max = request.floor_structure.bounds_max
            else:
                # Default 10x10 area
                floor_min = Vector3(x=-5, y=0, z=-5)
                floor_max = Vector3(x=5, y=0, z=5)

            logger.info(f"[Composer] Floor: ({floor_min.x},{floor_min.y},{floor_min.z}) to ({floor_max.x},{floor_max.y},{floor_max.z})")

            # Get available objects
            available_objects = self.assets.get_available_objects()

            # Generate layout using LLM
            logger.info("[Composer] Calling LLM provider...")
            layout = await self.llm.generate_layout(
                prompt=request.prompt,
                floor_bounds_min=floor_min,
                floor_bounds_max=floor_max,
                available_objects=available_objects,
                max_objects=request.options.max_objects,
                include_decorations=request.options.include_decorations,
            )
            logger.info(f"[Composer] LLM returned {len(layout.objects)} objects")

            # Convert layout to placements
            placements = self._create_placements(layout, request.prompt)

            elapsed = time.time() - start_time

            return SceneCompositionResult(
                success=True,
                placements=placements,
                reasoning=layout.reasoning,
                compositionTimeSeconds=elapsed,
            )

        except Exception as e:
            elapsed = time.time() - start_time
            return SceneCompositionResult(
                success=False,
                errorMessage=str(e),
                placements=[],
                reasoning="",
                compositionTimeSeconds=elapsed,
            )

    def _create_placements(
        self, layout: LayoutPlan, source_prompt: str
    ) -> list[SceneObjectPlacement]:
        """Convert LLM layout plan to scene object placements"""
        placements = []

        for obj in layout.objects:
            # Get asset info
            asset_info = self.assets.get_asset_info(obj.object_type)

            # Generate unique ID
            object_id = f"{obj.object_type}_{uuid.uuid4().hex[:8]}"

            # Get bounds from asset or use defaults
            if asset_info:
                bounds_min = asset_info.bounds_min
                bounds_max = asset_info.bounds_max
                display_name = asset_info.display_name
                asset_path = asset_info.asset_path
                category = asset_info.category
                tags = asset_info.tags
            else:
                bounds_min = Vector3(x=-0.5, y=0, z=-0.5)
                bounds_max = Vector3(x=0.5, y=1.0, z=0.5)
                display_name = obj.object_type.replace("_", " ").title()
                asset_path = f"unknown/{obj.object_type}"
                category = "furniture"
                tags = []

            # Create metadata
            metadata = ObjectMetadata(
                objectId=object_id,
                objectName=display_name,
                category=category,
                tags=tags,
                boundsMin=bounds_min,
                boundsMax=bounds_max,
                sourcePrompt=source_prompt,
                createdAt=datetime.now(timezone.utc).isoformat(),
            )

            # Create placement
            placement = SceneObjectPlacement(
                objectId=object_id,
                assetPath=asset_path,
                category=category,
                objectName=display_name,
                position=obj.position,
                rotation=obj.rotation,
                scale=Vector3.one(),
                metadata=metadata,
            )

            placements.append(placement)

        return placements
