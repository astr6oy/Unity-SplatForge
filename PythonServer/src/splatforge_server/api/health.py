"""Health check endpoint"""

from fastapi import APIRouter
from pydantic import BaseModel

from ..config import get_settings, LLMProvider

router = APIRouter()


class HealthResponse(BaseModel):
    """Health check response"""

    status: str
    version: str
    llm_provider: str


@router.get("/health")
async def health_check() -> HealthResponse:
    """Check server health and configuration"""
    settings = get_settings()

    return HealthResponse(
        status="healthy",
        version="0.1.0",
        llm_provider=settings.llm_provider.value,
    )
