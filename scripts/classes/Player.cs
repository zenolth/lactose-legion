using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Godot.Composition;

[GlobalClass]
[Entity]
public partial class Player : CharacterBody3D {
	public enum MovementState {
		None,
		Crouch,
		Slide
	}
	
	public const float Speed = 10f;
	public const float BhopSpeedThreshold = 14f; // Which speed should player be considered to be bunnyhopping
	public const float AirSpeed = 1.5f;
	public const float MaxAirSpeed = 45f;

	public const float StandHeight = 2f;
	public const float CrouchHeight = 1f;
	public const float SlideHeight = 0.5f;
	
	/*
	 * RECOMMENDED VALUES :
	 * GROUND: 250
	 * AIR: 85
	 */
	
	public const float GroundAcceleration = 250f;
	public const float AirAcceleration = 85f;

	public const float SlideSteeringSpeed = 8f;

	public const float GroundFriction = 6.0f;
	public const float SlideFriction = 2.0f;

	public const float MinY = -50f;

	public const int BhopFrames = 1;
	
	public static float Gravity => (float) ProjectSettings.GetSetting("physics/3d/default_gravity").AsDouble();
	public static Vector3 GravityVector => Vector3.Down * Gravity;

	public bool ShouldJump { get; private set; } = false;
	public bool Landing { get; private set; } = false;

	public int FrameTimer { get; private set; } = BhopFrames;
	
	public Vector3 ViewmodelPosition { get; set; } = Vector3.Zero;
	public Vector3 ViewmodelRotation { get; set; } = Vector3.Zero;
	
	public Vector3 VelocityBeforeLanding { get; private set; } = Vector3.Zero;

	public Vector3 SpawnPosition { get; private set; } = Vector3.Up * StandHeight;
	
	public MovementState State { get; set; } = MovementState.None;

	public static Vector2 InputDir => Input.GetVector("strafe_left", "strafe_right", "move_forward", "move_backward");
	public Vector3 ForwardMove => this.GlobalBasis.Z * InputDir.Y;
	public Vector3 RightMove => this.GlobalBasis.X * InputDir.X;
	public Vector3 WishDir => this.State == MovementState.Slide ? this.SlideDir : Vector3.Zero + (this.ForwardMove + this.RightMove);
	public Vector3 SlideDir { get; private set; } = Vector3.Forward;
	
	public float Friction => this.State == MovementState.Slide ? SlideFriction : GroundFriction;

	public float SpeedMultiplier {
		get {
			return this.State switch {
				MovementState.Crouch => 0.7f,
				MovementState.Slide => 2.2f,
				_ => 1f
			};
		}
	}
	public float WishSpeed => (this.IsOnFloor() && this.FrameTimer >= BhopFrames ? Speed : AirSpeed) * this.SpeedMultiplier;

	public Vector3 RelativeVelocity {
		get {
			var vel = this.Velocity.Normalized().Rotated(Vector3.Up, -this.Rotation.Y);
			vel.Y = 0f;
			vel *= this.CurrentSpeed;
			return vel;
		}
	}

	public float Height {
		get {
			if (this.CollisionShape.Shape is BoxShape3D hitbox) {
				return hitbox.Size.Y;
			}

			return StandHeight;
		}
		set {
			if (this.CollisionShape.Shape is BoxShape3D hitbox) {
				var oldSize = hitbox.Size;
				oldSize.Y = value;
				hitbox.Size = oldSize;
			}
		}
	}

	public float TopY => this.Height / 2f;
	public float BottomY => -this.TopY;
	
	public float JumpForce => Mathf.Sqrt(this.Height * Gravity);
	public float Acceleration => this.IsOnFloor() ? GroundAcceleration : AirAcceleration;

	public float CurrentSpeed {
		get {
			var velocity = this.GetVelocity();
			velocity.Y = 0f;
			return velocity.Length();
		}
	}

	public float DeltaTime =>
		(float)(Engine.IsInPhysicsFrame() ? this.GetPhysicsProcessDeltaTime() : this.GetProcessDeltaTime());

	public Label SpeedLabel;

	public List<Weapon> Weapons = new();
	private int CurrentWeapon = 0;

	private RandomNumberGenerator Random = new();
	
	private FirstPersonCamera _camera;
	public FirstPersonCamera Camera => this._camera ?? this.GetComponent<FirstPersonCamera>();
	
	[Export]
	public Node3D ViewmodelNode;

	public CollisionShape3D CollisionShape { get; private set; }

	public override void _Ready() {
		this.Random.Randomize();
		this.SpawnPosition = this.Position;
		this.InitializeEntity();
		this.SpeedLabel = this.GetNode<Label>("DebugHUD/Speed");

		foreach (var node in this.ViewmodelNode.GetChildren()) {
			if (node is Weapon weapon) {
				weapon._Player = this;
				this.Weapons.Add(weapon);
			}
		}
		
		foreach (var node in this.GetChildren()) {
			if (node is FirstPersonCamera camera) {
				this._camera = camera;
				break;
			}
		}

		foreach (var node in this.GetChildren()) {
			if (node is CollisionShape3D collisionShape) {
				this.CollisionShape = collisionShape;
				break;
			}
		}

		this.UpdateCurrentWeapon();
	}

	public void EquipWeapon(int index) {
		if (this.CurrentWeapon == index) return;
		this.CurrentWeapon = index;
		this.Weapons[index].OnEquip();
	}

	public Weapon GetWeapon() {
		return this.Weapons[this.CurrentWeapon];
	}

	public override void _Input(InputEvent @event) {
		if (@event is InputEventKey keyEvent) {
			switch (keyEvent.PhysicalKeycode) {
				case Key.Key1:
					this.EquipWeapon(0);
					break;
				case Key.Key2:
					this.EquipWeapon(1);
					break;
				case Key.Key3:
					this.EquipWeapon(2);
					break;
			}
		}
	}

	public void UpdateFrameTimer() {
		if (this.IsOnFloor()) {
			this.FrameTimer++;
		}
		else {
			this.FrameTimer = 0;
		}
	}

	public Vector3 ApplyFriction(Vector3 previousVelocity,float friction) {
		var speed = previousVelocity.Length();

		if (speed != 0f) {
			var drop = speed * friction * this.DeltaTime;
			previousVelocity *= Mathf.Max(speed - drop, 0f) / speed;
		}

		return previousVelocity;
	}
	
	public Vector3 Accelerate(Vector3 direction,Vector3 previousVelocity,float amount,float maxSpeed) {
		var projVel = previousVelocity.Dot(this.WishDir);
		var accelVel = amount * this.DeltaTime;

		if (projVel + accelVel > maxSpeed) {
			accelVel = maxSpeed - projVel;
		}

		return previousVelocity + direction * Mathf.Max(accelVel,0f);
	}

	public Vector3 CapSpeed(Vector3 previousVelocity) {
		var velocity = previousVelocity;
		velocity.Y = 0f;
		var speed = velocity.Length();
		var direction = velocity.Normalized();

		if (speed > MaxAirSpeed) {
			previousVelocity -= direction * Mathf.Max(speed - MaxAirSpeed, 0f);
		}

		return previousVelocity;
	}

	public void UpdateCurrentWeapon() {
		foreach (var (weapon, index) in this.Weapons.Select(((weapon, i) => (weapon, i)))) {
			if (this.CurrentWeapon == index) {
				weapon.Visible = true;
				weapon.ProcessMode = ProcessModeEnum.Inherit;
			} else {
				weapon.Visible = false;
				weapon.ProcessMode = ProcessModeEnum.Disabled;
			}
		}
	}

	public void SetVelocity(float x, float y, float z) {
		this.Velocity = new(x, y, z);
	}

	public static readonly Array<int> SideAngleMultipliers = new() { -2,-1,1,2 };

	public void OnLand() {
		if (this.CurrentSpeed <= BhopSpeedThreshold) {
			this.Random.Randomize();
			var fallVelocity = Mathf.Abs(this.VelocityBeforeLanding.Y) / 2f;
			var rollAngle = Mathf.Min(100f,fallVelocity);
			var sideAngle = rollAngle * SideAngleMultipliers[this.Random.RandiRange(0,SideAngleMultipliers.Count - 1)];
			this.Camera.CameraRotationDegrees = new(-rollAngle, sideAngle / 2f, sideAngle);
			this.Camera.CameraPosition.Y = -fallVelocity / 2f;
			this.Camera.CameraPosition.Z = fallVelocity / 2f;
		}
	}

	public const float FallSettleSpeed = 10f;

	public override void _Process(double delta) {
		var lerpAlpha = 1f - Mathf.Pow(0.5f, (float)delta * FallSettleSpeed);
		this.Camera.CameraPosition = this.Camera.CameraPosition.Lerp(Vector3.Zero, lerpAlpha);
		this.Camera.CameraRotationDegrees = this.Camera.CameraRotationDegrees.Lerp(Vector3.Zero, lerpAlpha);
		
		this.UpdateCurrentWeapon();
	}

	public Vector3 DoGravity(Vector3 previousVelocity) {
		if (!this.IsOnFloor()) {
			previousVelocity += GravityVector * this.DeltaTime;
		}

		return previousVelocity;
	}

	public Vector3 Jump(Vector3 previousVelocity) {
		if (!this.IsOnFloor()) return previousVelocity;
		
		previousVelocity.Y = this.JumpForce;
		
		return previousVelocity;
	}

	public override void _PhysicsProcess(double delta) {
		this.Height = this.State switch {
			MovementState.None => StandHeight,
			MovementState.Crouch => CrouchHeight,
			MovementState.Slide => SlideHeight,
			_ => StandHeight
		};

		var previousVelocity = this.GetVelocity();

		if (this.IsOnFloor() && this.FrameTimer >= BhopFrames || this.State == MovementState.Slide) {
			previousVelocity = this.ApplyFriction(previousVelocity, this.Friction);
		}

		previousVelocity = this.Accelerate(this.WishDir, previousVelocity, this.Acceleration, this.WishSpeed);

		previousVelocity = this.CapSpeed(previousVelocity);
		
		previousVelocity = this.DoGravity(previousVelocity);
		
		if (Input.IsActionPressed("jump_action") && this.IsOnFloor() && this.State != MovementState.Slide) {
			previousVelocity = this.Jump(previousVelocity);
		}

		if (Input.IsActionJustPressed("crouch")) {
			if (this.CurrentSpeed >= Speed / 2f) {
				this.State = MovementState.Slide;
				previousVelocity.X = this.SlideDir.X * this.WishSpeed;
				previousVelocity.Z = this.SlideDir.Z * this.WishSpeed;
			} else {
				if (this.State != MovementState.Slide) {
					this.State = MovementState.Crouch;
				}
			}
		} else if (Input.IsActionJustReleased("crouch")) {
			this.State = MovementState.None;
		}

		if (Input.IsKeyPressed(Key.B)) {
			previousVelocity.Y = Gravity;
		}

		var forward = -this.GlobalBasis.Z.Normalized();

		if (this.State == MovementState.Slide) {
			if (this.SlideDir != forward) {
				this.SlideDir = this.SlideDir.Normalized().Slerp(forward, 1f - Mathf.Pow(0.5f,SlideSteeringSpeed * (float) delta));
			}
		} else {
			this.SlideDir = forward;
		}
		
		if (this.IsOnFloor()) {
			if (this.Landing) {
				this.OnLand();
				this.Landing = false;
			}
		} else {
			this.VelocityBeforeLanding = this.Velocity;
			if (!this.Landing) {
				this.Landing = true;
			}
		}
		
		this.UpdateFrameTimer();

		this.Velocity = previousVelocity;
		
		this.SpeedLabel.Text = $"SPEED: {this.CurrentSpeed}\nVELOCITY: {this.RelativeVelocity.ToString()}\nON GROUND: {this.IsOnFloor()}\nBHOP FRAMES: {this.FrameTimer}";

		this.CollisionShape.Rotation = new(0, -this.Rotation.Y, 0);
		
		this.MoveAndSlide();

		if (this.Position.Y <= MinY) {
			this.Position = this.SpawnPosition;
		}
	}
}