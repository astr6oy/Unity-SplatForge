"""Pytest fixtures for SplatForge Server tests"""

import pytest
from fastapi.testclient import TestClient

from splatforge_server.main import app
from splatforge_server.llm import MockLLMProvider
from splatforge_server.services import AssetManager, SceneComposerService


@pytest.fixture
def client():
    """Create test client"""
    return TestClient(app)


@pytest.fixture
def mock_llm():
    """Create mock LLM provider with no delay"""
    return MockLLMProvider(simulated_delay=0)


@pytest.fixture
def asset_manager():
    """Create asset manager"""
    return AssetManager()


@pytest.fixture
def composer(mock_llm, asset_manager):
    """Create scene composer with mock LLM"""
    return SceneComposerService(
        llm_provider=mock_llm,
        asset_manager=asset_manager,
    )
