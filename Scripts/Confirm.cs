//Folosit pentru confirmarea unor actiuni, cum ar fi deschiderea unui link sau stergerea permanenta a unui user
using Godot;
using System;

public partial class Confirm : Node2D
{
	private DefaultData _data;
	[Export]public int reason; //0: Sterge, 1: Iesire chestionar, 2: Link
	[Export]public string link; //linkul accesat

	//pentru timer
	public Timer timer;
	public bool timeout;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	_data = (DefaultData)GetNode("/root/DefaultData");
		timer = GetNode<Timer>("timer");
		timeout = false;
		GetNode<Label>("Subpanels/Link/Panel/Link").Text = link;	//Afiseaza linkul intr-un Label
		//Se schimba layoutul in functie de actiune
		switch(reason)
		{
			case 0:
				GetNode<Control>("Subpanels/Delete").Visible = true;
				break;
			case 1:
				GetNode<Control>("Subpanels/Exit").Visible = true;
				break;
			case 2:
				GetNode<Control>("Subpanels/Link").Visible = true;
				break;
		}
		//Animatii
		if(_data.currentStats.Anims)
		{	var scale = Scale;
			var pos = Position;
			scale.X = (float)0.5;
			scale.Y = (float)0.5;
			pos.X = 432;
			pos.Y = 224;
			GetNode<Control>("Subpanels").Position = pos;
			GetNode<Control>("Subpanels").Scale = scale;
			scale.X = (float)1.5;
			scale.Y = (float)1.5;
			pos.X = 0;
			pos.Y = 0;
			var tween = GetTree().CreateTween();
			tween.SetPauseMode((Tween.TweenPauseMode)2);	//Ca sa fie procesat chiar si daca SceneTree este in pauza
			tween.TweenProperty(GetNode<Control>("Subpanels"), "position", pos, 0.5).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
			tween.Parallel().TweenProperty(GetNode<Control>("Subpanels"), "scale", scale, 0.5).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
		}
		GetTree().Paused = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	//Pentru butonul de stergere
		if (!timer.IsStopped()) GetNode<ProgressBar>("Subpanels/Delete/Panel/Bar").Value = 3 - timer.TimeLeft;
		else GetNode<ProgressBar>("Subpanels/Delete/Panel/Bar").Value = 0;
		if(timeout) GetNode<ProgressBar>("Subpanels/Delete/Panel/Bar").Value = 3;
	}

	private void _on_cancel_pressed()	//Atunci cand se anuleaza actiunea, dispare fereastra, indiferent de tip
	{	GetTree().Paused = false;
		QueueFree();
	}
	private void _on_confirmexit_pressed()	//Atunci cand confirmi iesirea din chestionar
	{	GetTree().Paused = false;
		_data.questiontype = 0;
		GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
	}
	private void _on_confirmlink_pressed()	//Atunci cand confirmi deschiderea linkului intr-un browser
	{	GetTree().Paused = false;
		OS.ShellOpen(link);
		QueueFree();
	}
	//Toate trei functiile de mai jos sunt pentru confirmarea stergerii utilizatorului
	private void _on_hold_down()	//Incepi sa tii apasat butonul
	{	timer.Start(3.0);
	}
	private void _on_hold_up()	//Eliberarea butonului cand acesta este rosu in totalitate
	{	if(timer.IsStopped() && timeout) 
		{	GetTree().Paused = false;
			_data.DeleteSave(_data.LoggedUser);
		}
		else timer.Stop();
	}
	private void _on_timeout()	//Se termina timerul (butonul s-a facut rosu in totalitate)
	{	timeout = true;
		GetNode<Label>("Subpanels/Delete/Panel/Bar/Text").Text = "Elibereaza butonul";
	}
}
