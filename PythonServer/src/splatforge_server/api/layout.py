"""Layout suggestion endpoint"""

from fastapi import APIRouter, Depends

from ..models import (
    LayoutRequest,
    LayoutSuggestion,
    PositionSuggestion,
    Vector3,
)
from ..llm import LLMProvider
from ..dependencies import get_llm_provider

router = APIRouter()


@router.post("/layout", response_model=LayoutSuggestion)
async def get_layout_suggestion(
    request: LayoutRequest,
    llm: LLMProvider = Depends(get_llm_provider),
) -> LayoutSuggestion:
    """
    Get layout suggestions for specific objects.

    Given existing scene context and objects to place, suggests optimal positions.
    """
    try:
        # Convert existing objects to dict format for LLM
        existing = [
            {
                "name": obj.object_name,
                "position": {"x": obj.position.x, "y": obj.position.y, "z": obj.position.z},
            }
            for obj in request.scene_context.existing_objects
        ]

        # Get layout from LLM
        layout = await llm.suggest_layout(
            object_types=request.object_ids_to_place,
            floor_bounds_min=request.scene_context.scene_bounds_min,
            floor_bounds_max=request.scene_context.scene_bounds_max,
            existing_objects=existing,
            constraints={
                "avoid_overlap": request.constraints.avoid_overlap,
                "ground_objects": request.constraints.ground_objects,
                "min_spacing": request.constraints.min_spacing,
            },
        )

        # Convert to suggestions
        suggestions = [
            PositionSuggestion(
                objectId=obj.object_type,
                suggestedPosition=obj.position,
                suggestedRotation=obj.rotation,
                confidence=0.9,
                reasoning=obj.rationale,
            )
            for obj in layout.objects
        ]

        return LayoutSuggestion(
            success=True,
            suggestions=suggestions,
            overallReasoning=layout.reasoning,
        )

    except Exception as e:
        return LayoutSuggestion(
            success=False,
            errorMessage=str(e),
            suggestions=[],
            overallReasoning="",
        )
