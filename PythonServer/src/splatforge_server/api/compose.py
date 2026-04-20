"""Scene composition endpoint"""

import logging
from fastapi import APIRouter, Depends

from ..models import SceneCompositionRequest, SceneCompositionResult
from ..services import SceneComposerService
from ..dependencies import get_scene_composer

logger = logging.getLogger("splatforge.compose")

router = APIRouter()


@router.post("/compose", response_model=SceneCompositionResult)
async def compose_scene(
    request: SceneCompositionRequest,
    composer: SceneComposerService = Depends(get_scene_composer),
) -> SceneCompositionResult:
    """
    Compose a 3D scene based on prompt and floor structure.

    This is the main workflow endpoint. Given a text description and floor bounds,
    it generates a complete scene layout with object placements.
    """
    prompt_preview = request.prompt[:50] + "..." if len(request.prompt) > 50 else request.prompt
    logger.info(f"[API] Compose request: '{prompt_preview}'")

    result = await composer.compose(request)

    logger.info(
        f"[API] Compose response: {len(result.placements)} objects "
        f"in {result.composition_time_seconds:.2f}s"
    )
    return result
