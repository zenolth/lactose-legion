using Godot;
using System;

[GlobalClass]
public partial class AnimatedPart : Resource {
	//[Export(PropertyHint.NodePathValidTypes)]
	[Export] public NodePath Node {get; set;}
	[Export] public Transform3D Transform {get; set;}
	
	public AnimatedPart() : this(null,Transform3D.Identity) {}

	public AnimatedPart(NodePath nodePath, Transform3D transform) {
		this.Node = nodePath;
		this.Transform = transform;
	}
}