"""Main API router combining all endpoints"""

from fastapi import APIRouter

from .compose import router as compose_router
from .generate import router as generate_router
from .layout import router as layout_router
from .health import router as health_router

router = APIRouter(prefix="/api/v1")

router.include_router(health_router, tags=["health"])
router.include_router(compose_router, tags=["compose"])
router.include_router(generate_router, tags=["generate"])
router.include_router(layout_router, tags=["layout"])
