"""Configuration management using pydantic-settings"""

from enum import Enum
from pathlib import Path
from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class LLMProvider(str, Enum):
    MOCK = "mock"
    OPENAI = "openai"
    CLAUDE = "claude"


class Settings(BaseSettings):
    """Application settings loaded from environment variables"""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
    )

    # Server
    host: str = "0.0.0.0"
    port: int = 8080
    debug: bool = True

    # LLM Provider
    llm_provider: LLMProvider = LLMProvider.MOCK

    # OpenAI
    openai_api_key: str = ""
    openai_model: str = "gpt-4-turbo-preview"

    # Claude
    anthropic_api_key: str = ""
    claude_model: str = "claude-3-opus-20240229"

    # Assets
    assets_dir: Path = Path("./assets")

    @property
    def assets_path(self) -> Path:
        """Resolve assets directory to absolute path"""
        return self.assets_dir.resolve()


@lru_cache
def get_settings() -> Settings:
    """Get cached settings instance"""
    return Settings()
