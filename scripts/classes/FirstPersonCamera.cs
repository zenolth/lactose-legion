using Godot;
using System;
using System.Reflection.Metadata;
using Godot.Composition;
using LactoseLegion.scripts.util;

[GlobalClass]
[Component(typeof(Player))]
public partial class FirstPersonCamera : Node3D {
	
	[Export(PropertyHint.Range,"0,1")]
	public float Sensitivity = 0.4f;
	
	[Export]
	public Node3D ViewmodelNode;
	public Camera3D Camera;

	public float Height = 0.7f;
	public Vector3 HeadPosition => Vector3.Up * (this.CurrentPlayer.TopY - (1f - this.Height)); // this is probably the worst line of code that will cause the game to drop to 10 fps, but i couldnt care less at this point

	public Vector3 ViewmodelOffset = Vector3.Zero;
	public Vector3 ViewmodelRotation = Vector3.Zero;
	
	public Vector3 CameraPosition = Vector3.Zero;
	public Vector3 CameraRotationDegrees = Vector3.Zero;
	
	public Vector3 LastFacing = Vector3.Forward;

	public float Roll = 0f;
	public float ViewmodelRoll = 0f;
	public float LastPlayerYaw = 0f;
	public float BobTime = 0;
	
	public Player CurrentPlayer => this.GetOwnerOrNull<Player>();
	
	public override void _Ready() {
		this.InitializeComponent();
		this.Height = this.Position.Y;
		Input.SetMouseMode(Input.MouseModeEnum.Captured);
		
		foreach (var node in this.GetChildren()) {
			if (node is Camera3D camera) {
				this.Camera = camera;
			}
		}
	}

	public override void _Input(InputEvent @event) {
		base._Input(@event);

		if (@event is InputEventMouseMotion mouseMotion) {
			if (this.Owner is Node3D Owner3D) {
				Owner3D.RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * this.Sensitivity));
			}
			this.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * this.Sensitivity));
			this.SetRotationDegrees(new(Mathf.Clamp(this.RotationDegrees.X,-89f,89f),this.RotationDegrees.Y,this.RotationDegrees.Z));
		}
	}

	public const float BobSettleSpeed = 10f;
	public const float BobSpeedScale = 1.1f;
	public const float BobMaxSpeed = Player.Speed * BobSpeedScale;
	
	Vector3 bobOffset = Vector3.Zero;
	Vector3 bobVMRotation = Vector3.Zero;

	private void BobViewmodel(double delta) {
		//var bobOffset = Vector3.Zero;
		//var bobVMRotation = Vector3.Zero;
		
		var speed = Mathf.Clamp(this.CurrentPlayer.CurrentSpeed * BobSpeedScale, 0f, BobMaxSpeed);

		if (speed > 0.5f && this.CurrentPlayer.State != Player.MovementState.Slide) {
			var speedScale = Mathf.Ease(speed / Player.Speed,-1.8f);

			this.BobTime += (float) delta * speed;

			var sin = Mathf.Sin(this.BobTime);
			var cos = Mathf.Abs(Mathf.Cos(this.BobTime));

			this.bobOffset.X = Mathf.Lerp(0f,sin * 0.006f,speedScale);
			this.bobOffset.Y = Mathf.Lerp(0f,cos * 0.0015f,speedScale);
			this.bobVMRotation.X = Mathf.Lerp(0f,Mathf.DegToRad(cos * 1.5f),speedScale);
			this.bobVMRotation.Y = Mathf.Lerp(0f,Mathf.DegToRad(sin * 2f),speedScale);
			this.bobVMRotation.Z = Mathf.Lerp(0f,Mathf.DegToRad(sin * 1.5f),speedScale);
			this.ViewmodelRoll = Mathf.Lerp(this.ViewmodelRoll, 0f, 1f - Mathf.Pow(0.5f, (float)delta * BobSettleSpeed));
		} else {
			this.BobTime = 0;
			var lerpAlpha = 1f - Mathf.Pow(0.5f, (float)delta * BobSettleSpeed);
			this.bobOffset.X = Mathf.Lerp(this.bobOffset.X, 0f,lerpAlpha);
			this.bobOffset.Y = Mathf.Lerp(this.bobOffset.Y, 0f,lerpAlpha);
			this.bobVMRotation.X = Mathf.Lerp(this.bobVMRotation.X, 0f,lerpAlpha);
			this.bobVMRotation.Y = Mathf.Lerp(this.bobVMRotation.Y, 0f,lerpAlpha);
			this.bobVMRotation.Z = Mathf.Lerp(this.bobVMRotation.Z, 0f, lerpAlpha);
			this.ViewmodelRoll = Mathf.Lerp(this.ViewmodelRoll, this.CurrentPlayer.State == Player.MovementState.Slide ? 20f : 0f, lerpAlpha);
		}

		this.CurrentPlayer.ViewmodelRotation += this.bobVMRotation;
		this.ViewmodelOffset += this.bobOffset;
	}
	
	public const float ViewRollScale = 5f;
	public const float SwaySpeed = 7f; // 7
	public const float CameraSpeed = 13f;
	public const float SwayScale = 0.9f;
	public const float MaxViewModelLag = 10f; // 1.5

	private void LagViewmodel(double delta) {
		
		var swayAlpha = 1f - Mathf.Pow(0.5f, SwaySpeed * (float)delta);
		var cameraLerpAlpha = 1f - Mathf.Pow(0.5f, CameraSpeed * (float)delta);
		
		this.ViewmodelNode.RotationDegrees = new(0, 0, this.ViewmodelRoll);
		
		var viewRoll = Mathf.Clamp(-this.CurrentPlayer.RelativeVelocity.X / Player.Speed, -1f, 1f) * ViewRollScale;
		
		this.Camera.RotationDegrees = this.Camera.RotationDegrees.Lerp(new Vector3(0f,0f,viewRoll + this.Roll) + this.CameraRotationDegrees,cameraLerpAlpha);
		this.Position = this.Position.Lerp(this.HeadPosition + this.CameraPosition,cameraLerpAlpha);
	}

	public override void _Process(double delta) {
		if (this.CurrentPlayer == null) {
			return;
		}
		
		this.BobViewmodel(delta);
		this.LagViewmodel(delta);

		this.ViewmodelOffset = Vector3.Zero;
		//this.ViewmodelRotation = Vector3.Zero;
		this.CameraPosition = Vector3.Zero;
	}
}
