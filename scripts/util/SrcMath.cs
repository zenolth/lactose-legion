using Godot;

namespace LactoseLegion.scripts.util;

public static class SrcMath {
	public static Vector3 VectorMA(Vector3 start, float scale, Vector3 direction) {
		return new(
			start.X + scale * direction.X,
			start.Y + scale * direction.Y,
			start.Z + scale * direction.Z
		);
	}
}