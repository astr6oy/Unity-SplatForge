"""Mock LLM provider with hardcoded layouts for testing"""

import asyncio
import logging
import random
from typing import Optional

from .base import LLMProvider, LayoutPlan, PlannedObject
from ..models import Vector3

logger = logging.getLogger("splatforge.llm.mock")


class MockLLMProvider(LLMProvider):
    """Mock LLM provider that returns predefined layouts based on keywords"""

    # Predefined layouts for common room types
    LAYOUTS = {
        "bedroom": {
            "objects": [
                {"type": "bed", "pos": [0, 0, 2], "rot": 0, "rationale": "Placed against back wall"},
                {"type": "nightstand", "pos": [-1.2, 0, 2], "rot": 0, "rationale": "Left of bed"},
                {"type": "nightstand", "pos": [1.2, 0, 2], "rot": 0, "rationale": "Right of bed"},
                {"type": "desk", "pos": [-2, 0, -1], "rot": 90, "rationale": "Against left wall"},
                {"type": "chair", "pos": [-1.5, 0, -1], "rot": -90, "rationale": "At desk"},
                {"type": "lamp", "pos": [-1.2, 0.5, 2], "rot": 0, "rationale": "On nightstand"},
                {"type": "wardrobe", "pos": [2.5, 0, 0], "rot": -90, "rationale": "Against right wall"},
            ],
            "reasoning": "Created a cozy bedroom layout with the bed as the focal point against the back wall. Nightstands flank the bed for symmetry. A desk workspace is set up against the left wall with proper lighting. The wardrobe is placed against the right wall for easy access.",
        },
        "office": {
            "objects": [
                {"type": "desk", "pos": [0, 0, 2], "rot": 180, "rationale": "Main workspace facing room"},
                {"type": "chair", "pos": [0, 0, 1.3], "rot": 0, "rationale": "At main desk"},
                {"type": "bookshelf", "pos": [-2.5, 0, 0], "rot": 90, "rationale": "Against left wall"},
                {"type": "filing_cabinet", "pos": [2, 0, 2.5], "rot": -90, "rationale": "Near desk"},
                {"type": "plant", "pos": [-2, 0, -2], "rot": 0, "rationale": "Corner decoration"},
                {"type": "lamp", "pos": [0.5, 0.75, 2], "rot": 0, "rationale": "Desk lamp"},
                {"type": "guest_chair", "pos": [-1.5, 0, -0.5], "rot": 45, "rationale": "For visitors"},
                {"type": "guest_chair", "pos": [1.5, 0, -0.5], "rot": -45, "rationale": "For visitors"},
            ],
            "reasoning": "Designed a professional office with the main desk positioned to face incoming visitors. Bookshelves and filing cabinets provide storage. Two guest chairs create a meeting area. A plant adds a touch of nature to the workspace.",
        },
        "living": {
            "objects": [
                {"type": "sofa", "pos": [0, 0, -1.5], "rot": 0, "rationale": "Main seating facing TV area"},
                {"type": "coffee_table", "pos": [0, 0, 0], "rot": 0, "rationale": "In front of sofa"},
                {"type": "tv_stand", "pos": [0, 0, 2.5], "rot": 180, "rationale": "Against far wall"},
                {"type": "armchair", "pos": [-2, 0, 0], "rot": 45, "rationale": "Additional seating"},
                {"type": "armchair", "pos": [2, 0, 0], "rot": -45, "rationale": "Additional seating"},
                {"type": "floor_lamp", "pos": [-2.5, 0, -1.5], "rot": 0, "rationale": "Corner lighting"},
                {"type": "plant", "pos": [2.5, 0, 2], "rot": 0, "rationale": "Corner decoration"},
                {"type": "rug", "pos": [0, 0.01, -0.5], "rot": 0, "rationale": "Under seating area"},
            ],
            "reasoning": "Created a comfortable living room centered around the TV viewing area. The sofa faces the TV stand with a coffee table in between. Two armchairs provide additional seating arranged at angles for conversation. A floor lamp and plant add ambiance.",
        },
    }

    def __init__(self, simulated_delay: float = 1.5):
        self.simulated_delay = simulated_delay

    def _detect_room_type(self, prompt: str) -> str:
        """Detect room type from prompt keywords"""
        prompt_lower = prompt.lower()

        if any(word in prompt_lower for word in ["bedroom", "bed", "sleep", "cozy"]):
            return "bedroom"
        elif any(word in prompt_lower for word in ["office", "work", "desk", "professional"]):
            return "office"
        elif any(word in prompt_lower for word in ["living", "lounge", "sofa", "tv"]):
            return "living"

        # Default to bedroom
        return "bedroom"

    def _scale_positions(
        self,
        objects: list[dict],
        floor_min: Vector3,
        floor_max: Vector3,
    ) -> list[dict]:
        """Scale object positions to fit within floor bounds"""
        # Calculate floor center and size
        center_x = (floor_min.x + floor_max.x) / 2
        center_z = (floor_min.z + floor_max.z) / 2
        size_x = floor_max.x - floor_min.x
        size_z = floor_max.z - floor_min.z

        # Default layout assumes 10x10 area centered at origin
        default_size = 10.0
        scale_x = size_x / default_size
        scale_z = size_z / default_size

        scaled = []
        for obj in objects:
            pos = obj["pos"]
            scaled.append({
                **obj,
                "pos": [
                    pos[0] * scale_x + center_x,
                    pos[1] + floor_min.y,
                    pos[2] * scale_z + center_z,
                ],
            })
        return scaled

    async def generate_layout(
        self,
        prompt: str,
        floor_bounds_min: Vector3,
        floor_bounds_max: Vector3,
        available_objects: list[str],
        max_objects: int = 10,
        include_decorations: bool = True,
    ) -> LayoutPlan:
        """Generate layout based on prompt keywords"""
        # Simulate processing time
        delay = self.simulated_delay + random.uniform(0, 1.0)
        await asyncio.sleep(delay)

        room_type = self._detect_room_type(prompt)
        logger.info(f"[Python Mock] Detected room type: '{room_type}'")
        layout_data = self.LAYOUTS.get(room_type, self.LAYOUTS["bedroom"])

        # Scale positions to floor bounds
        scaled_objects = self._scale_positions(
            layout_data["objects"],
            floor_bounds_min,
            floor_bounds_max,
        )

        # Filter by max_objects and decorations
        decoration_types = {"plant", "lamp", "floor_lamp", "rug"}
        filtered = []
        for obj in scaled_objects:
            if len(filtered) >= max_objects:
                break
            if not include_decorations and obj["type"] in decoration_types:
                continue
            filtered.append(obj)

        # Convert to PlannedObject instances
        planned = [
            PlannedObject(
                object_type=obj["type"],
                position=Vector3(x=obj["pos"][0], y=obj["pos"][1], z=obj["pos"][2]),
                rotation=Vector3(x=0, y=obj["rot"], z=0),
                rationale=obj.get("rationale", ""),
            )
            for obj in filtered
        ]

        logger.info(f"[Python Mock] Returning predefined layout with {len(planned)} objects")

        return LayoutPlan(
            objects=planned,
            reasoning=layout_data["reasoning"],
        )

    async def suggest_layout(
        self,
        object_types: list[str],
        floor_bounds_min: Vector3,
        floor_bounds_max: Vector3,
        existing_objects: list[dict],
        constraints: dict,
    ) -> LayoutPlan:
        """Suggest positions for objects avoiding existing ones"""
        await asyncio.sleep(self.simulated_delay)

        min_spacing = constraints.get("min_spacing", 0.5)

        # Calculate available positions (simple grid approach)
        center_x = (floor_bounds_min.x + floor_bounds_max.x) / 2
        center_z = (floor_bounds_min.z + floor_bounds_max.z) / 2

        planned = []
        for i, obj_type in enumerate(object_types):
            # Simple placement: spread objects around center
            angle = (i / len(object_types)) * 360
            radius = 2.0 + i * min_spacing
            x = center_x + radius * 0.5 * (1 if i % 2 == 0 else -1)
            z = center_z + radius * 0.3 * (1 if i % 3 == 0 else -1)

            planned.append(
                PlannedObject(
                    object_type=obj_type,
                    position=Vector3(x=x, y=floor_bounds_min.y, z=z),
                    rotation=Vector3(x=0, y=angle * 0.25, z=0),
                    rationale=f"Placed with {min_spacing}m spacing from other objects",
                )
            )

        return LayoutPlan(
            objects=planned,
            reasoning=f"Arranged {len(object_types)} objects with minimum {min_spacing}m spacing",
        )
