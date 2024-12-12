using Godot;
using System;
using Godot.Composition;

[GlobalClass]
[Component(typeof(Player))]
public partial class SpeedLines : ColorRect {
	public Player CurrentPlayer => this.GetOwnerOrNull<Player>();
	
	public override void _Ready() {
		this.InitializeComponent();
	}
	
	public override void _Process(double delta) {
		if (this.Material is ShaderMaterial shaderMaterial) {
			if (this.CurrentPlayer.CurrentSpeed > Player.BhopSpeedThreshold) {
				var speed = Mathf.Clamp((Player.BhopSpeedThreshold - this.CurrentPlayer.CurrentSpeed) / (Player.BhopSpeedThreshold - Player.MaxAirSpeed),0f,1f);

				speed = Mathf.Ease(speed,4.8f);
				
				shaderMaterial.SetShaderParameter("line_opacity",speed);
			} else {
				shaderMaterial.SetShaderParameter(
					"line_opacity",
					Mathf.Lerp((float) shaderMaterial.GetShaderParameter("line_opacity").AsDouble(),0f,1f - Mathf.Pow(0.5f,(float) delta * 5f))
					);
			}
		}
	}
}
