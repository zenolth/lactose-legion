using Godot;
using System;
using Godot.Composition;

[GlobalClass]
[Component(typeof(Player))]
public partial class Hud : Control {

	public float ViewportWidth => this.GetViewportRect().Size.X;
	public float ViewportHeight => this.GetViewportRect().Size.Y;
	
	public override void _Ready() {
		this.InitializeComponent();
	}

	public override void _Draw() {
		this.DrawCircle(new(this.ViewportWidth / 2,this.ViewportHeight / 2),3f,Colors.Black);
		this.DrawCircle(new(this.ViewportWidth / 2,this.ViewportHeight / 2),2f,Colors.White);
	}

	public override void _Process(double delta) {
		this.QueueRedraw();
	}
}
