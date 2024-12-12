using Godot;
using System;
using Godot.Collections;
using Godot.Composition;
using LactoseLegion.scripts.util;
using Vector3 = Godot.Vector3;

[GlobalClass]
[Component(typeof(Weapon))]
public partial class HitscanComponent : Node3D {
	[Export] public float FireAnimationSpeed = 3f;
	[Export] public float FireRate = 0.1f;
	[Export] public float Range = 100f;
	[Export] public float Spread = 50f;
	[Export(PropertyHint.Range,"0,0.99")] public float MinSpreadModifier = 0f;
	
	[Export] public Array<AnimatedPart> AnimatedParts;

	public float AdjustedSpread {
		get {
			var spread = this.Spread / 100f;
			if (this.CurrentPlayer.CurrentSpeed > 0.5f) return spread;
			var speedMul = 1f + this.CurrentPlayer.CurrentSpeed / Player.MaxAirSpeed * 2f;
			return spread * speedMul;
		}
	}

	public float MinSpread => this.AdjustedSpread * this.MinSpreadModifier;
	
	[Export(PropertyHint.Range,"1,265")] public int BurstCount = 1;
	[Export] public bool Auto = false;

	[Export] public Vector3 FireWeaponPositionOffset = Vector3.Zero;
	[Export] public Vector3 FireWeaponRotationOffset = Vector3.Zero;
	
	public float ScaledFireAnimationSpeed => this.FireAnimationSpeed * (1f + this.CurrentWeapon.Scale.Length());
	
	public static PackedScene BulletHole = GD.Load<PackedScene>("res://scenes/prefabs/bullet_hole.tscn");

	public PhysicsDirectSpaceState3D SpaceState => this.GetWorld3D().GetDirectSpaceState();
	public Player CurrentPlayer => Util.FindAncestorOrNull<Player>(this);
	public Weapon CurrentWeapon => this.GetParentOrNull<Weapon>();

	public float FireCooldown = 0f;

	public RandomNumberGenerator Random = new();
	
	public override void _Ready() {
		this.Random.Randomize();
		this.FireCooldown = this.FireRate;
		this.InitializeComponent();
	}

	public void FireSingle() {
		this.FireCooldown = this.FireRate;
		
		this.CurrentWeapon.OffsetPosition = this.FireWeaponPositionOffset * this.CurrentWeapon.Scale.Length();
		this.CurrentWeapon.OffsetRotation = new(Mathf.DegToRad(this.FireWeaponRotationOffset.X),Mathf.DegToRad(this.FireWeaponRotationOffset.Y),Mathf.DegToRad(this.FireWeaponRotationOffset.Z));
		
		this.Random.Randomize();

		var forward = -this.CurrentPlayer.Camera.GlobalBasis.Z.Normalized();
		var right = this.CurrentPlayer.Camera.GlobalBasis.X.Normalized();
		var up = this.CurrentPlayer.Camera.GlobalBasis.Y.Normalized();
		
		var origin = this.CurrentPlayer.Camera.GlobalPosition;
		var direction = forward + up.Rotated(forward, Mathf.DegToRad(this.Random.RandfRange(-360f,360f))) * this.Random.RandfRange(this.MinSpread, this.AdjustedSpread);
		var target = origin + direction.Normalized() * this.Range;
			
		var result = Util.Raycast(this, origin, target,new Array<Rid> {this.CurrentPlayer.GetRid()});

		if (result.HasValue) {
			var bulletHole = BulletHole.Instantiate<MeshInstance3D>();
			this.CurrentPlayer.Owner.AddChild(bulletHole);
			bulletHole.Scale *= this.Random.RandfRange(0.8f,1.2f);
			bulletHole.SetPosition(result.Value.HitPosition);
		}
	}

	public void Fire() {
		if (this.FireCooldown > 0) return;

		for (var i = 0; i < this.BurstCount; i++) {
			this.FireSingle();
		}
	}

	public override void _Process(double delta) {
		if (this.FireCooldown > 0f) {
			this.FireCooldown -= (float) delta;
		}

		if (this.Auto) {
			if (Input.IsActionPressed("primary_attack")) {
				this.Fire();
			}
		} else {
			if (Input.IsActionJustPressed("primary_attack")) {
				this.Fire();
			}
		}
		
		var lerpWeight = 1f - Mathf.Pow(0.5f, (float)delta * this.ScaledFireAnimationSpeed);
		this.CurrentWeapon.OffsetPosition = this.CurrentWeapon.OffsetPosition.Lerp(Vector3.Zero,lerpWeight);
		this.CurrentWeapon.OffsetRotation = this.CurrentWeapon.OffsetRotation.Lerp(Vector3.Zero,lerpWeight);
	}
}
