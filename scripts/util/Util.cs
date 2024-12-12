using Godot;
using Godot.Collections;

namespace LactoseLegion.scripts.util;

public struct RaycastResult {
	public Vector3 HitPosition;
	public Vector3 HitNormal;
	public Node3D Collider;
	public Rid Rid;
	public int ShapeIndex;
}

public static class Util {
	public static Array<Rid> EmptyRidArray = new Array<Rid>();
	
	public static T FindAncestorOrNull<T>(Node node) where T : Node {
		var parent = node.GetParent();

		while (parent is not T) {
			parent = parent.GetParent();
			if (parent is null) {
				return null;
			}
		}
		
		return parent as T;
	}
	
	public static RaycastResult? Raycast(Node3D node,PhysicsRayQueryParameters3D queryParameters) {
		var spaceState = node.GetWorld3D().GetDirectSpaceState();
		var rawResult = spaceState.IntersectRay(queryParameters);

		if (!rawResult.ContainsKey("collider")) return null;

		return new RaycastResult {
			HitPosition = rawResult["position"].AsVector3(),
			HitNormal = rawResult["normal"].AsVector3(),
			Collider = rawResult["collider"].As<Node3D>(),
			Rid = rawResult["rid"].AsRid(),
			ShapeIndex = rawResult["shape"].AsInt32()
		};
	}
	
	public static RaycastResult? Raycast(Node3D node,Vector3 origin,Vector3 end,Array<Rid> exclude) {
		return Raycast(node, new() {
			From = origin,
			To = end,
			Exclude = exclude ?? EmptyRidArray,
			CollideWithAreas = false
		});
	}
}