"""Tests for scene composition endpoint"""

import pytest
from fastapi.testclient import TestClient

from splatforge_server.main import app
from splatforge_server.models import (
    SceneCompositionRequest,
    FloorStructureData,
    SceneCompositionOptions,
    Vector3,
)


@pytest.fixture
def client():
    return TestClient(app)


def test_health_check(client):
    """Test health endpoint"""
    response = client.get("/api/v1/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "healthy"
    assert "llm_provider" in data


def test_compose_bedroom(client):
    """Test composing a bedroom scene"""
    request = {
        "prompt": "A cozy bedroom with a bed and desk",
        "floorStructure": {
            "boundsMin": {"x": -5, "y": 0, "z": -5},
            "boundsMax": {"x": 5, "y": 0, "z": 5},
            "floorHeight": 0,
            "walls": [],
        },
        "options": {
            "maxObjects": 10,
            "includeDecorations": True,
        },
    }

    response = client.post("/api/v1/compose", json=request)
    assert response.status_code == 200

    data = response.json()
    assert data["success"] is True
    assert len(data["placements"]) > 0
    assert "reasoning" in data
    assert data["compositionTimeSeconds"] > 0

    # Check placement structure
    placement = data["placements"][0]
    assert "objectId" in placement
    assert "objectName" in placement
    assert "position" in placement
    assert "rotation" in placement


def test_compose_office(client):
    """Test composing an office scene"""
    request = {
        "prompt": "A professional office with desk and chairs",
        "options": {
            "maxObjects": 8,
            "includeDecorations": False,
        },
    }

    response = client.post("/api/v1/compose", json=request)
    assert response.status_code == 200

    data = response.json()
    assert data["success"] is True

    # Should not include decorations
    for placement in data["placements"]:
        assert placement["category"] != "decoration"


def test_compose_without_floor_structure(client):
    """Test composing with default floor bounds"""
    request = {
        "prompt": "A living room with sofa",
    }

    response = client.post("/api/v1/compose", json=request)
    assert response.status_code == 200

    data = response.json()
    assert data["success"] is True


def test_compose_max_objects(client):
    """Test max objects limit"""
    request = {
        "prompt": "A bedroom",
        "options": {
            "maxObjects": 3,
        },
    }

    response = client.post("/api/v1/compose", json=request)
    assert response.status_code == 200

    data = response.json()
    assert len(data["placements"]) <= 3
