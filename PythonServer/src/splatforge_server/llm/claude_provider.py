"""Claude (Anthropic) LLM provider implementation"""

import json
from typing import Optional

from anthropic import AsyncAnthropic

from .base import LLMProvider, LayoutPlan, PlannedObject
from ..models import Vector3
from ..config import get_settings


SYSTEM_PROMPT = """You are a 3D scene layout planner. Given a room description and floor bounds, generate a JSON layout with object placements.

Rules:
1. Objects must not overlap - maintain at least 0.5m spacing
2. Objects must be within floor bounds
3. Furniture should be placed against walls when appropriate
4. Maintain walkable paths (min 0.8m width)
5. Group related objects (desk + chair, bed + nightstand)
6. Consider natural lighting and room flow

Output ONLY valid JSON in this exact format:
{
  "objects": [
    {"type": "bed", "position": [x, y, z], "rotation": [0, angle_y, 0], "rationale": "explanation"},
    ...
  ],
  "reasoning": "Overall layout explanation..."
}

Position is [x, y, z] where y is up (typically 0 for floor level).
Rotation is [rx, ry, rz] in degrees where ry is the main rotation around vertical axis."""


class ClaudeProvider(LLMProvider):
    """Claude-based LLM provider for layout generation"""

    def __init__(self, api_key: Optional[str] = None, model: Optional[str] = None):
        settings = get_settings()
        self.client = AsyncAnthropic(api_key=api_key or settings.anthropic_api_key)
        self.model = model or settings.claude_model

    def _build_user_prompt(
        self,
        prompt: str,
        floor_min: Vector3,
        floor_max: Vector3,
        available_objects: list[str],
        max_objects: int,
        include_decorations: bool,
    ) -> str:
        return f"""Create a layout for: {prompt}

Floor bounds: ({floor_min.x}, {floor_min.y}, {floor_min.z}) to ({floor_max.x}, {floor_max.y}, {floor_max.z})
Available object types: {', '.join(available_objects)}
Maximum objects: {max_objects}
Include decorations: {include_decorations}

Generate the layout JSON:"""

    def _parse_response(self, content: str) -> LayoutPlan:
        """Parse LLM response into LayoutPlan"""
        try:
            # Handle markdown code blocks
            if "```json" in content:
                content = content.split("```json")[1].split("```")[0]
            elif "```" in content:
                content = content.split("```")[1].split("```")[0]

            data = json.loads(content.strip())

            objects = []
            for obj in data.get("objects", []):
                pos = obj.get("position", [0, 0, 0])
                rot = obj.get("rotation", [0, 0, 0])
                objects.append(
                    PlannedObject(
                        object_type=obj.get("type", "unknown"),
                        position=Vector3(x=pos[0], y=pos[1], z=pos[2]),
                        rotation=Vector3(x=rot[0], y=rot[1], z=rot[2]),
                        rationale=obj.get("rationale", ""),
                    )
                )

            return LayoutPlan(
                objects=objects,
                reasoning=data.get("reasoning", ""),
            )
        except (json.JSONDecodeError, KeyError, IndexError) as e:
            return LayoutPlan(
                objects=[],
                reasoning=f"Failed to parse LLM response: {e}",
            )

    async def generate_layout(
        self,
        prompt: str,
        floor_bounds_min: Vector3,
        floor_bounds_max: Vector3,
        available_objects: list[str],
        max_objects: int = 10,
        include_decorations: bool = True,
    ) -> LayoutPlan:
        """Generate layout using Claude API"""
        user_prompt = self._build_user_prompt(
            prompt,
            floor_bounds_min,
            floor_bounds_max,
            available_objects,
            max_objects,
            include_decorations,
        )

        response = await self.client.messages.create(
            model=self.model,
            max_tokens=2000,
            system=SYSTEM_PROMPT,
            messages=[{"role": "user", "content": user_prompt}],
        )

        content = response.content[0].text if response.content else ""
        return self._parse_response(content)

    async def suggest_layout(
        self,
        object_types: list[str],
        floor_bounds_min: Vector3,
        floor_bounds_max: Vector3,
        existing_objects: list[dict],
        constraints: dict,
    ) -> LayoutPlan:
        """Suggest positions using Claude API"""
        existing_desc = "\n".join(
            f"- {obj.get('name', 'object')} at ({obj['position']['x']}, {obj['position']['y']}, {obj['position']['z']})"
            for obj in existing_objects
        )

        user_prompt = f"""Suggest positions for these objects: {', '.join(object_types)}

Floor bounds: ({floor_bounds_min.x}, {floor_bounds_min.y}, {floor_bounds_min.z}) to ({floor_bounds_max.x}, {floor_bounds_max.y}, {floor_bounds_max.z})

Existing objects in scene:
{existing_desc}

Constraints:
- Avoid overlap: {constraints.get('avoid_overlap', True)}
- Ground objects: {constraints.get('ground_objects', True)}
- Minimum spacing: {constraints.get('min_spacing', 0.5)}m

Generate positions JSON:"""

        response = await self.client.messages.create(
            model=self.model,
            max_tokens=2000,
            system=SYSTEM_PROMPT,
            messages=[{"role": "user", "content": user_prompt}],
        )

        content = response.content[0].text if response.content else ""
        return self._parse_response(content)
