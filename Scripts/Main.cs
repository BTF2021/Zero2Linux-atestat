//Folosit pentru meniul principal
using Godot;
using System;

public partial class Main : Node2D
{
	private DefaultData _data;
	private HttpRequest request;	//Request http pentru verificarea unei noi versiuni a programului
	[Signal] public delegate void DownloadEventHandler(int mode);	//Semnal trimis de NewVer.cs atunci cand se descarca o noua versiune
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	_data = (DefaultData)GetNode("/root/DefaultData");
		_data.ReadSave(_data.LoggedUser);	//Aici citim ce este salvat pentru utilizator. Cred ca este singurul apel a functiei in tot programul
		GetNode<CanvasItem>("/root/Transition").Hide(); //A se vedea funtia logging din Logare.cs
		Download += down;	//Conectam semnalul la functie
		if(!_data.currentStats.Anims)
		{	GetNode("UI/Bg/Bg").QueueFree();
		}
		#if GODOT_ANDROID
			GetNode<TextureButton>("UI/Bar/HBoxContainer/Power").QueueFree();
		#endif
		//Preluam de pe Github fisierul version.txt
		if(!_data.verifiedver)
		{	request = new HttpRequest();
			AddChild(request);
			request.RequestCompleted += OnRequestCompleted;                                           //Cand se apeleaza Request => functia OnRequestCompleted
			request.Request("https://raw.githubusercontent.com/BTF2021/Zero2Linux/main/version.txt"); //version.txt de pe Github
		}
		
		if(_data.currentStats.Anims)
		{	var pos = Position;
			pos.X = 490;
			pos.Y = 352;
			GetNode<Panel>("UI/Panel").Position = pos;
			pos.Y = 337;
			GetNode<Panel>("UI/Panel").Modulate = new Color(1, 1, 1, 0);
			var tween = GetTree().CreateTween();
			tween.TweenProperty(GetNode<Panel>("UI/Panel"), "modulate", new Color(1, 1, 1, 1), 0.25);
			tween.Parallel().TweenProperty(GetNode<Panel>("UI/Panel"), "position", pos, 0.25);
			pos.X = 643;
			pos.Y = 740;
			GetNode<Sprite2D>("UI/Bar").Position = pos;
			pos.Y = 686;
			tween.TweenProperty(GetNode<Sprite2D>("UI/Bar"), "position", pos, 0.25);
		}
		else 
		{	
			#if GODOT_WINDOWS || GODOT_LINUXBSD
				GetNode<VideoStreamPlayer>("UI/Bg/Bg").Paused = true;
			#endif
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	//Ceasul
		if(HasNode("UI/Bar")) 
		{
			GetNode<Label>("UI/Bar/HBoxContainer/Time").Text = Time.GetTimeStringFromSystem();
			GetNode<Label>("UI/Bar/HBoxContainer/Time").TooltipText = Time.GetDateStringFromSystem();
		}
	}

	public override void _Notification(int what)
	{	//Daca dai inapoi pe Android
		if (what == NotificationWMGoBackRequest)
        	GetTree().Quit();
	}

	//Semnal pentru NewVer.cs ca sa nu poata utilizatorul sa paraseasca scena Main
	private void down(int mode)
	{	if(mode ==1)	//Descarcarea este in desfasurare
		{	
			#if GODOT_LINUXBSD || GODOT_WINDOWS
				GetNode<TextureButton>("UI/Bar/HBoxContainer/Power").Disabled = true;
				GetNode<TextureButton>("UI/Bar/HBoxContainer/Power").Hide();
			#endif
			GetNode<TextureButton>("UI/Bar/HBoxContainer/Logout").Disabled = true;
			GetNode<TextureButton>("UI/Bar/HBoxContainer/Logout").Hide();
			GetNode<Button>("UI/Panel/Buttons/Course").Disabled = true;
			GetNode<Button>("UI/Panel/Buttons/Quizzes").Disabled = true;
			GetNode<Button>("UI/Panel/Buttons/Settings").Disabled = true;
		}
		else	//Descarcarea s-a terminat
		{	
			#if GODOT_LINUXBSD || GODOT_WINDOWS
				GetNode<TextureButton>("UI/Bar/HBoxContainer/Power").Disabled = false;
				GetNode<TextureButton>("UI/Bar/HBoxContainer/Power").Show();
			#endif
			GetNode<TextureButton>("UI/Bar/HBoxContainer/Logout").Disabled = false;
			GetNode<TextureButton>("UI/Bar/HBoxContainer/Logout").Show();
			GetNode<Button>("UI/Panel/Buttons/Course").Disabled = false;
			GetNode<Button>("UI/Panel/Buttons/Quizzes").Disabled = false;
			GetNode<Button>("UI/Panel/Buttons/Settings").Disabled = false;
		}
	}

	private void _on_quit_pressed() => GetTree().Quit(0);
	private void _on_logout_pressed()	//Delogare
	{
		GD.Print("Delogare: " + _data.LoggedUser);
		_data.verifiedver = false;
		_data.WriteSave(_data.LoggedUser);
		_data.LoggedUser = " ";
		_data.currentStats = new stats();
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
		GetTree().ChangeSceneToFile("res://Scenes/Logare.tscn");
	}
	private void _on_notification_pressed()	//Apare fereastra pentru o noua versiune
	{	var newver = (GD.Load<PackedScene>("res://Scenes/NewVer.tscn")).Instantiate();
		newver.GetNode<Label>("Panel/Panel/ScrollContainer/VBoxContainer/Title2").Text = newver.GetNode<Label>("Panel/Panel/ScrollContainer/VBoxContainer/Title2").Text + (String)ProjectSettings.GetSetting("application/config/version") + "\nVersiunea actuala este: " + _data.newversion[0] + "\n ";
		newver.GetNode<Label>("Panel/Panel/ScrollContainer/VBoxContainer/HBoxContainer2/Title3").Text = _data.newversion[1];
		AddChild(newver);
	}
	//Cele patru functii corespund celor patru butoane din centru
	private void _on_course_pressed() => AddChild((GD.Load<PackedScene>("res://Scenes/Courses.tscn")).Instantiate());

	private void _on_settings_pressed() => AddChild((GD.Load<PackedScene>("res://Scenes/Settings.tscn")).Instantiate());

	private void _on_quizzes_pressed() => AddChild((GD.Load<PackedScene>("res://Scenes/Quizzes.tscn")).Instantiate());
	private void _on_progress_pressed() => AddChild((GD.Load<PackedScene>("res://Scenes/Progress.tscn")).Instantiate());

	//Daca a fost primit un raspuns de la _request
	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{	if(result == 0)
		{	string data = System.Text.Encoding.UTF8.GetString(body);		//tot ce este in version.txt
			_data.newversion.Clear();
			_data.newversion.Add(data.Substring(0, data.IndexOf(";", 0)));		//Versiunea programului
			_data.newversion.Add(data.Substring(data.IndexOf("-", 0)));		//Textul fara versiunea programului
			if(!((String)ProjectSettings.GetSetting("application/config/version")).Contains(_data.newversion[0]) && _data.currentStats.ChkUpdates) //Daca versiunea programului corespunde cu versiunea din version.txt
			{
				GD.Print("Versiune veche");
				GetNode<TextureButton>("UI/Bar/HBoxContainer/Notification").Disabled = false;
				GetNode<TextureButton>("UI/Bar/HBoxContainer/Notification").Visible = true;
			}
			else GD.Print("Mergem in continuare");
		}
		else
		{
			GD.Print("Eroare HttpRequest: " + (HttpRequest.Result)result);
			return;
		}
		_data.verifiedver = true;
		request.QueueFree();       //Nu mai e nevoie de HttpRequest. Putem sterge
	}
}
