"""FastAPI application entry point"""

import logging
import uvicorn
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from .config import get_settings
from .api import router

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(name)s] %(message)s",
    datefmt="%H:%M:%S",
)

app = FastAPI(
    title="SplatForge Server",
    description="LLM-based 3D scene composition server for Unity",
    version="0.1.0",
)

# CORS middleware for Unity WebGL and editor requests
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Allow all origins for development
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include API router
app.include_router(router)


@app.get("/")
async def root():
    """Root endpoint with server info"""
    settings = get_settings()
    return {
        "name": "SplatForge Server",
        "version": "0.1.0",
        "llm_provider": settings.llm_provider.value,
        "docs": "/docs",
    }


def run():
    """Run server with uvicorn"""
    settings = get_settings()
    uvicorn.run(
        "splatforge_server.main:app",
        host=settings.host,
        port=settings.port,
        reload=settings.debug,
    )


if __name__ == "__main__":
    run()
