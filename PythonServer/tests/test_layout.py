"""Tests for layout suggestion endpoint"""

import pytest
from fastapi.testclient import TestClient

from splatforge_server.main import app


@pytest.fixture
def client():
    return TestClient(app)


def test_layout_suggestion(client):
    """Test layout suggestion endpoint"""
    request = {
        "sceneContext": {
            "existingObjects": [
                {
                    "objectId": "desk_001",
                    "objectName": "Office Desk",
                    "category": "furniture",
                    "position": {"x": 0, "y": 0, "z": 2},
                    "rotation": {"x": 0, "y": 0, "z": 0},
                    "boundsMin": {"x": -0.7, "y": 0, "z": -0.35},
                    "boundsMax": {"x": 0.7, "y": 0.75, "z": 0.35},
                }
            ],
            "sceneBoundsMin": {"x": -5, "y": 0, "z": -5},
            "sceneBoundsMax": {"x": 5, "y": 3, "z": 5},
            "groundPlaneNormal": {"x": 0, "y": 1, "z": 0},
            "groundPlaneHeight": 0,
        },
        "objectIdsToPlace": ["chair", "lamp"],
        "constraints": {
            "avoidOverlap": True,
            "groundObjects": True,
            "minSpacing": 0.5,
        },
    }

    response = client.post("/api/v1/layout", json=request)
    assert response.status_code == 200

    data = response.json()
    assert data["success"] is True
    assert len(data["suggestions"]) == 2
    assert "overallReasoning" in data

    # Check suggestion structure
    suggestion = data["suggestions"][0]
    assert "objectId" in suggestion
    assert "suggestedPosition" in suggestion
    assert "suggestedRotation" in suggestion


def test_layout_empty_scene(client):
    """Test layout with no existing objects"""
    request = {
        "sceneContext": {
            "existingObjects": [],
            "sceneBoundsMin": {"x": -3, "y": 0, "z": -3},
            "sceneBoundsMax": {"x": 3, "y": 2.5, "z": 3},
        },
        "objectIdsToPlace": ["sofa"],
        "constraints": {
            "avoidOverlap": True,
            "groundObjects": True,
            "minSpacing": 0.3,
        },
    }

    response = client.post("/api/v1/layout", json=request)
    assert response.status_code == 200

    data = response.json()
    assert data["success"] is True
    assert len(data["suggestions"]) == 1
