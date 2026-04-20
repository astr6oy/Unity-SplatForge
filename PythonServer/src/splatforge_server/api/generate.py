"""Object generation endpoint (stub)"""

from fastapi import APIRouter

from ..models import GenerationRequest, GenerationResult

router = APIRouter()


@router.post("/generate", response_model=GenerationResult)
async def generate_object(request: GenerationRequest) -> GenerationResult:
    """
    Generate a single 3DGS object from prompt.

    Note: This endpoint is a stub for future 3DGS generation integration.
    Currently returns a placeholder response.
    """
    return GenerationResult(
        success=False,
        errorMessage="3DGS generation is not yet implemented. Use /compose for scene layouts.",
        objectId="",
        plyData=None,
        metadata=None,
        generationTimeSeconds=0,
    )
