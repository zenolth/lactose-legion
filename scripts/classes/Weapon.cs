using Godot;
using System;
using Godot.Composition;
using LactoseLegion.scripts.util;

[GlobalClass]
[Entity]
public partial class Weapon : Node3D {

	public const bool Quakeify = false;
	
	[ExportCategory("Equip Transforms")]
	[Export]
	public Vector3 DrawPosition = Vector3.Zero;
	[Export]
	public Vector3 DrawRotation = Vector3.Zero;
	[Export]
	public float DrawSpeed = 0.6f;

	public float ScaledDrawSpeed => this.DrawSpeed * (1f + this.Scale.Length());

	public Vector3 InitialPosition;
	public Vector3 InitialRotation;
	
	public Vector3 OffsetPosition = Vector3.Zero;
	public Vector3 OffsetRotation = Vector3.Zero;
	
	public Vector3 ViewmodelPosition => this.CurrentPlayer.ViewmodelPosition;
	public Vector3 ViewmodelRotation => this.CurrentPlayer.ViewmodelRotation;

	public Player _Player;

	public Player CurrentPlayer => this._Player ?? Util.FindAncestorOrNull<Player>(this);
	
	public override void _Ready() {
		this.InitializeEntity();
		this.InitialPosition = this.Position;
		this.InitialRotation = this.Rotation;
		
		if (Quakeify) {
			this.InitialPosition.X = 0f;
			this.InitialRotation.Z = 0f;
		}
	}

	public void OnEquip() {
		this.Position = this.DrawPosition;
		this.RotationDegrees = this.DrawRotation;
	}
	
	public override void _Process(double delta) {
		var lerpWeight = 1f - Mathf.Pow(0.5f, (float)delta * this.ScaledDrawSpeed);
		this.Position = this.Position.Lerp(this.InitialPosition + this.OffsetPosition + this.ViewmodelPosition, lerpWeight);
		this.Rotation = this.Rotation.Lerp(this.InitialRotation + this.OffsetRotation + this.ViewmodelRotation, lerpWeight);

		this.CurrentPlayer.ViewmodelRotation = Vector3.Zero;
		this.CurrentPlayer.ViewmodelPosition = Vector3.Zero;
	}
}
