"""Abstract base class for LLM providers"""

from abc import ABC, abstractmethod
from dataclasses import dataclass
from typing import Optional

from ..models import Vector3


@dataclass
class PlannedObject:
    """A single object in the layout plan"""

    object_type: str  # e.g., "bed", "desk", "chair"
    position: Vector3
    rotation: Vector3  # Euler angles in degrees
    rationale: str = ""


@dataclass
class LayoutPlan:
    """Complete layout plan from LLM"""

    objects: list[PlannedObject]
    reasoning: str


class LLMProvider(ABC):
    """Abstract base class for LLM providers"""

    @abstractmethod
    async def generate_layout(
        self,
        prompt: str,
        floor_bounds_min: Vector3,
        floor_bounds_max: Vector3,
        available_objects: list[str],
        max_objects: int = 10,
        include_decorations: bool = True,
    ) -> LayoutPlan:
        """
        Generate a layout plan based on the prompt and constraints.

        Args:
            prompt: User's description of the desired scene
            floor_bounds_min: Minimum corner of the floor area
            floor_bounds_max: Maximum corner of the floor area
            available_objects: List of available object types
            max_objects: Maximum number of objects to place
            include_decorations: Whether to include decorative items

        Returns:
            LayoutPlan with object placements and reasoning
        """
        pass

    @abstractmethod
    async def suggest_layout(
        self,
        object_types: list[str],
        floor_bounds_min: Vector3,
        floor_bounds_max: Vector3,
        existing_objects: list[dict],
        constraints: dict,
    ) -> LayoutPlan:
        """
        Suggest positions for specific objects given existing scene context.

        Args:
            object_types: Types of objects to place
            floor_bounds_min: Minimum corner of the floor area
            floor_bounds_max: Maximum corner of the floor area
            existing_objects: Already placed objects in the scene
            constraints: Layout constraints (spacing, grounding, etc.)

        Returns:
            LayoutPlan with suggested positions
        """
        pass
