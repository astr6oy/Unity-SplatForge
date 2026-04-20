"""Asset management service"""

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from ..models import Vector3
from ..config import get_settings


@dataclass
class AssetInfo:
    """Information about an available asset"""

    asset_id: str
    asset_path: str
    display_name: str
    category: str
    tags: list[str]
    bounds_min: Vector3
    bounds_max: Vector3


class AssetManager:
    """Manages available 3DGS assets and their metadata"""

    # Default asset catalog (used when no metadata.json exists)
    DEFAULT_ASSETS = {
        "bed": AssetInfo(
            asset_id="bed_001",
            asset_path="furniture/bed_01",
            display_name="Double Bed",
            category="furniture",
            tags=["bedroom", "furniture", "sleeping"],
            bounds_min=Vector3(x=-1.0, y=0, z=-1.1),
            bounds_max=Vector3(x=1.0, y=0.6, z=1.1),
        ),
        "nightstand": AssetInfo(
            asset_id="nightstand_001",
            asset_path="furniture/nightstand_01",
            display_name="Nightstand",
            category="furniture",
            tags=["bedroom", "furniture", "storage"],
            bounds_min=Vector3(x=-0.25, y=0, z=-0.25),
            bounds_max=Vector3(x=0.25, y=0.5, z=0.25),
        ),
        "desk": AssetInfo(
            asset_id="desk_001",
            asset_path="furniture/desk_01",
            display_name="Office Desk",
            category="furniture",
            tags=["office", "furniture", "workspace"],
            bounds_min=Vector3(x=-0.7, y=0, z=-0.35),
            bounds_max=Vector3(x=0.7, y=0.75, z=0.35),
        ),
        "chair": AssetInfo(
            asset_id="chair_001",
            asset_path="furniture/chair_01",
            display_name="Office Chair",
            category="furniture",
            tags=["office", "furniture", "seating"],
            bounds_min=Vector3(x=-0.3, y=0, z=-0.3),
            bounds_max=Vector3(x=0.3, y=1.0, z=0.3),
        ),
        "sofa": AssetInfo(
            asset_id="sofa_001",
            asset_path="furniture/sofa_01",
            display_name="3-Seat Sofa",
            category="furniture",
            tags=["living", "furniture", "seating"],
            bounds_min=Vector3(x=-1.0, y=0, z=-0.45),
            bounds_max=Vector3(x=1.0, y=0.85, z=0.45),
        ),
        "armchair": AssetInfo(
            asset_id="armchair_001",
            asset_path="furniture/armchair_01",
            display_name="Armchair",
            category="furniture",
            tags=["living", "furniture", "seating"],
            bounds_min=Vector3(x=-0.4, y=0, z=-0.4),
            bounds_max=Vector3(x=0.4, y=0.9, z=0.4),
        ),
        "coffee_table": AssetInfo(
            asset_id="coffee_table_001",
            asset_path="furniture/coffee_table_01",
            display_name="Coffee Table",
            category="furniture",
            tags=["living", "furniture", "table"],
            bounds_min=Vector3(x=-0.6, y=0, z=-0.4),
            bounds_max=Vector3(x=0.6, y=0.45, z=0.4),
        ),
        "bookshelf": AssetInfo(
            asset_id="bookshelf_001",
            asset_path="furniture/bookshelf_01",
            display_name="Bookshelf",
            category="furniture",
            tags=["office", "furniture", "storage"],
            bounds_min=Vector3(x=-0.4, y=0, z=-0.2),
            bounds_max=Vector3(x=0.4, y=1.8, z=0.2),
        ),
        "wardrobe": AssetInfo(
            asset_id="wardrobe_001",
            asset_path="furniture/wardrobe_01",
            display_name="Wardrobe",
            category="furniture",
            tags=["bedroom", "furniture", "storage"],
            bounds_min=Vector3(x=-0.6, y=0, z=-0.3),
            bounds_max=Vector3(x=0.6, y=2.0, z=0.3),
        ),
        "tv_stand": AssetInfo(
            asset_id="tv_stand_001",
            asset_path="furniture/tv_stand_01",
            display_name="TV Stand",
            category="furniture",
            tags=["living", "furniture", "entertainment"],
            bounds_min=Vector3(x=-0.8, y=0, z=-0.25),
            bounds_max=Vector3(x=0.8, y=0.5, z=0.25),
        ),
        "filing_cabinet": AssetInfo(
            asset_id="filing_cabinet_001",
            asset_path="furniture/filing_cabinet_01",
            display_name="Filing Cabinet",
            category="furniture",
            tags=["office", "furniture", "storage"],
            bounds_min=Vector3(x=-0.25, y=0, z=-0.3),
            bounds_max=Vector3(x=0.25, y=1.0, z=0.3),
        ),
        "guest_chair": AssetInfo(
            asset_id="guest_chair_001",
            asset_path="furniture/guest_chair_01",
            display_name="Guest Chair",
            category="furniture",
            tags=["office", "furniture", "seating"],
            bounds_min=Vector3(x=-0.25, y=0, z=-0.25),
            bounds_max=Vector3(x=0.25, y=0.85, z=0.25),
        ),
        "lamp": AssetInfo(
            asset_id="lamp_001",
            asset_path="decorations/lamp_01",
            display_name="Table Lamp",
            category="decoration",
            tags=["lighting", "decoration"],
            bounds_min=Vector3(x=-0.15, y=0, z=-0.15),
            bounds_max=Vector3(x=0.15, y=0.45, z=0.15),
        ),
        "floor_lamp": AssetInfo(
            asset_id="floor_lamp_001",
            asset_path="decorations/floor_lamp_01",
            display_name="Floor Lamp",
            category="decoration",
            tags=["lighting", "decoration"],
            bounds_min=Vector3(x=-0.2, y=0, z=-0.2),
            bounds_max=Vector3(x=0.2, y=1.6, z=0.2),
        ),
        "plant": AssetInfo(
            asset_id="plant_001",
            asset_path="decorations/plant_01",
            display_name="Potted Plant",
            category="decoration",
            tags=["decoration", "nature"],
            bounds_min=Vector3(x=-0.25, y=0, z=-0.25),
            bounds_max=Vector3(x=0.25, y=0.8, z=0.25),
        ),
        "rug": AssetInfo(
            asset_id="rug_001",
            asset_path="decorations/rug_01",
            display_name="Area Rug",
            category="decoration",
            tags=["decoration", "floor"],
            bounds_min=Vector3(x=-1.5, y=0, z=-1.0),
            bounds_max=Vector3(x=1.5, y=0.02, z=1.0),
        ),
    }

    def __init__(self, assets_dir: Optional[Path] = None):
        self.assets_dir = assets_dir or get_settings().assets_path
        self._catalog: dict[str, AssetInfo] = {}
        self._load_catalog()

    def _load_catalog(self) -> None:
        """Load asset catalog from metadata.json or use defaults"""
        metadata_path = self.assets_dir / "metadata.json"

        if metadata_path.exists():
            try:
                with open(metadata_path) as f:
                    data = json.load(f)
                    for asset_type, info in data.get("assets", {}).items():
                        self._catalog[asset_type] = AssetInfo(
                            asset_id=info.get("asset_id", f"{asset_type}_001"),
                            asset_path=info.get("asset_path", f"unknown/{asset_type}"),
                            display_name=info.get("display_name", asset_type.title()),
                            category=info.get("category", "furniture"),
                            tags=info.get("tags", []),
                            bounds_min=Vector3(**info.get("bounds_min", {"x": -0.5, "y": 0, "z": -0.5})),
                            bounds_max=Vector3(**info.get("bounds_max", {"x": 0.5, "y": 1.0, "z": 0.5})),
                        )
            except (json.JSONDecodeError, KeyError) as e:
                print(f"Warning: Failed to load asset catalog: {e}")
                self._catalog = dict(self.DEFAULT_ASSETS)
        else:
            self._catalog = dict(self.DEFAULT_ASSETS)

    def get_available_objects(self, category: Optional[str] = None) -> list[str]:
        """Get list of available object types"""
        if category:
            return [k for k, v in self._catalog.items() if v.category == category]
        return list(self._catalog.keys())

    def get_asset_info(self, object_type: str) -> Optional[AssetInfo]:
        """Get asset information by type"""
        return self._catalog.get(object_type)

    def get_asset_path(self, object_type: str) -> str:
        """Get asset file path by type"""
        info = self._catalog.get(object_type)
        return info.asset_path if info else ""

    def get_asset_bounds(self, object_type: str) -> tuple[Vector3, Vector3]:
        """Get asset bounding box"""
        info = self._catalog.get(object_type)
        if info:
            return info.bounds_min, info.bounds_max
        return Vector3.zero(), Vector3.one()

    def get_display_name(self, object_type: str) -> str:
        """Get human-readable display name"""
        info = self._catalog.get(object_type)
        return info.display_name if info else object_type.replace("_", " ").title()
