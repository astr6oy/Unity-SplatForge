"""Common types matching Unity's serialization format"""

from pydantic import BaseModel, Field


class Vector3(BaseModel):
    """3D vector matching Unity's Vector3 serialization"""

    x: float = 0.0
    y: float = 0.0
    z: float = 0.0

    @classmethod
    def zero(cls) -> "Vector3":
        return cls(x=0, y=0, z=0)

    @classmethod
    def one(cls) -> "Vector3":
        return cls(x=1, y=1, z=1)

    @classmethod
    def up(cls) -> "Vector3":
        return cls(x=0, y=1, z=0)

    def __add__(self, other: "Vector3") -> "Vector3":
        return Vector3(x=self.x + other.x, y=self.y + other.y, z=self.z + other.z)

    def __sub__(self, other: "Vector3") -> "Vector3":
        return Vector3(x=self.x - other.x, y=self.y - other.y, z=self.z - other.z)

    def __mul__(self, scalar: float) -> "Vector3":
        return Vector3(x=self.x * scalar, y=self.y * scalar, z=self.z * scalar)


class Quaternion(BaseModel):
    """Quaternion matching Unity's serialization (typically use Euler angles instead)"""

    x: float = 0.0
    y: float = 0.0
    z: float = 0.0
    w: float = 1.0

    @classmethod
    def identity(cls) -> "Quaternion":
        return cls(x=0, y=0, z=0, w=1)
