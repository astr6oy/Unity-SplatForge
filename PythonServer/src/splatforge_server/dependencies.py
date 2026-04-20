"""FastAPI dependency injection"""

from functools import lru_cache

from .config import get_settings, LLMProvider as LLMProviderEnum
from .llm import LLMProvider, MockLLMProvider, OpenAIProvider, ClaudeProvider
from .services import SceneComposerService, AssetManager


@lru_cache
def get_asset_manager() -> AssetManager:
    """Get cached asset manager instance"""
    return AssetManager()


@lru_cache
def get_llm_provider() -> LLMProvider:
    """Get LLM provider based on configuration"""
    settings = get_settings()

    if settings.llm_provider == LLMProviderEnum.OPENAI:
        return OpenAIProvider()
    elif settings.llm_provider == LLMProviderEnum.CLAUDE:
        return ClaudeProvider()
    else:
        return MockLLMProvider()


@lru_cache
def get_scene_composer() -> SceneComposerService:
    """Get cached scene composer service"""
    return SceneComposerService(
        llm_provider=get_llm_provider(),
        asset_manager=get_asset_manager(),
    )
