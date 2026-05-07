"""OpenAI LLM provider implementation"""

import json
import os
import time
from typing import Optional

from openai import AsyncOpenAI

from .base import LLMProvider, LayoutPlan, PlannedObject
from ..models import Vector3
from ..config import get_settings


SYSTEM_PROMPT = """You are a 3D scene layout planner. Given a room description and floor bounds, generate a JSON layout with object placements.

Rules:
1. Objects must not overlap - maintain at least 0.4m spacing (use the FULL floor area; rooms are typically small ~3-4m square)
2. Objects must be within floor bounds
3. Furniture should be placed against walls when appropriate
4. Maintain walkable paths (min 0.6m width)
5. Group related objects close together (desk + chair within 0.7m, bed + nightstand within 0.4m, sofa + coffee_table within 1.0m)
6. Consider natural lighting and room flow
7. Do NOT spread objects far apart — keep the layout dense like a real small room

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


# === Paper03 Phase 2 — usage logging ===
_USAGE_LOG_PATH = os.environ.get("OPENAI_USAGE_LOG", "/Users/oz6oy/sf-test-agent/paper03-experiment/logs/openai_usage.jsonl")


def _log_usage(model: str, usage_obj, latency_s: float):
    """Append per-call usage record. NEVER logs the API key or prompt content."""
    try:
        rec = {
            "ts": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            "model": model,
            "prompt_tokens": getattr(usage_obj, "prompt_tokens", None),
            "completion_tokens": getattr(usage_obj, "completion_tokens", None),
            "total_tokens": getattr(usage_obj, "total_tokens", None),
            "latency_s": round(latency_s, 3),
        }
        with open(_USAGE_LOG_PATH, "a") as f:
            f.write(json.dumps(rec) + "\n")
    except Exception:
        pass


class OpenAIProvider(LLMProvider):
    """OpenAI-based LLM provider for layout generation"""

    def __init__(self, api_key: Optional[str] = None, model: Optional[str] = None):
        settings = get_settings()
        self.client = AsyncOpenAI(api_key=api_key or settings.openai_api_key)
        self.model = model or settings.openai_model

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
        """Generate layout using OpenAI API"""
        user_prompt = self._build_user_prompt(
            prompt,
            floor_bounds_min,
            floor_bounds_max,
            available_objects,
            max_objects,
            include_decorations,
        )

        t0 = time.time()
        response = await self.client.chat.completions.create(
            model=self.model,
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": user_prompt},
            ],
            temperature=0.7,
            max_tokens=2000,
        )
        latency = time.time() - t0
        _log_usage(self.model, response.usage, latency)

        content = response.choices[0].message.content or ""
        return self._parse_response(content)

    async def suggest_layout(
        self,
        object_types: list[str],
        floor_bounds_min: Vector3,
        floor_bounds_max: Vector3,
        existing_objects: list[dict],
        constraints: dict,
    ) -> LayoutPlan:
        """Suggest positions using OpenAI API"""
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

        t0 = time.time()
        response = await self.client.chat.completions.create(
            model=self.model,
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": user_prompt},
            ],
            temperature=0.7,
            max_tokens=2000,
        )
        latency = time.time() - t0
        _log_usage(self.model, response.usage, latency)

        content = response.choices[0].message.content or ""
        return self._parse_response(content)