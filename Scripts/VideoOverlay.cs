using Godot;
using System;

public partial class VideoOverlay : Node2D
{
	double videocurrenttime, videotime;          //Simpilficate VideoStreamPlayer.StreamPosition si  VideoStreamPlayer.GetStreamLength()
	bool paused;                                 //Pentru a verifica daca utilizatorul a pus pauza
	bool dragging;                               //Pentru a verifica daca utilizatorul foloseste oricare slider (Mai mult pentru animatie)
	Godot.Vector2 mousepos, mouseposprev;
	TimeSpan vct, vt;                            //Pentru formatare text
	private DefaultData _data;                   //DefaultData
	private VideoStreamPlayer _stream;           //Video
	private HSlider _slider;                     //Seekbar
	double progresspos;
	private HSlider _volume;                     //Volum
	private Timer _timer;
	public override void _Ready()
	{	_data = (DefaultData)GetNode("/root/DefaultData");
		_stream = (VideoStreamPlayer)GetNode("Panel/VideoStreamPlayer");
		_slider = (HSlider)GetNode("ControlsTint/Seekbar/Seekbar");
		_volume = (HSlider)GetNode("ControlsTint/Controls/Volum/Panel/Slider");
		_timer = GetNode<Timer>("Timer");
		_stream.Stop();
		videotime = _stream.GetStreamLength();
		_slider.MaxValue = videotime;

		_volume.Value = _data.currentStats.VideoVolume;
		_stream.VolumeDb = _data.currentStats.VideoVolume;
		if(_volume.Value == -60) GetNode<TextureButton>("ControlsTint/Controls/Volum/Volum").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/NoVolume.png");
		else if (_volume.Value <= -30) GetNode<TextureButton>("ControlsTint/Controls/Volum/Volum").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/VolumeLow.png");
		else GetNode<TextureButton>("ControlsTint/Controls/Volum/Volum").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/VolumeHigh.png");

		if(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen) 
		{	GetNode<TextureButton>("ControlsTint/Controls/Fullscr").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/Winscr.png");
			GetNode<TextureButton>("ControlsTint/Controls/Fullscr").TexturePressed = GD.Load<CompressedTexture2D>("res://Sprites/WinscrHighlight.png");
		}
		GetNode<TextureButton>("ControlsTint/Controls/Play").GrabFocus();  //Cand apesi pe spacebar, mai apare acest Node in spate.
		vt = TimeSpan.FromSeconds(videotime);
		paused = true;
		dragging = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	videocurrenttime = _stream.StreamPosition;
		vct = TimeSpan.FromSeconds(videocurrenttime);

		progresspos = Mathf.Floor((1066.67d / videotime) * Mathf.Round((float)videocurrenttime));
		GetNode<Line2D>("ControlsTint/Seekbar/Progress").SetPointPosition(1, new Godot.Vector2((float)progresspos, 0));

		mousepos = GetViewport().GetMousePosition();
		if(mousepos != mouseposprev) _on_mouse_moved();
		else _on_mouse_stopped();
		mouseposprev = mousepos;

		if(_stream.IsPlaying())
			if(!_stream.Paused) _slider.Value = videocurrenttime;
			else _stream.StreamPosition = _slider.Value;
		else 
		{
			GetNode<TextureButton>("ControlsTint/Controls/Play").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/Play.png");
			GetNode<TextureButton>("ControlsTint/Controls/Play").TexturePressed = GD.Load<CompressedTexture2D>("res://Sprites/PlayHighlight.png");
		}
		//Text
		if(_stream.GetStreamLength() >= 3600) GetNode<Label>("ControlsTint/Controls/Time").Text = vct.ToString("hh\\:mm\\:ss") + "/" + vt.ToString("hh\\:mm\\:ss");
		else GetNode<Label>("ControlsTint/Controls/Time").Text = vct.ToString("mm\\:ss") + "/" + vt.ToString("mm\\:ss");
	}
	public override void _UnhandledInput(InputEvent input)
	{	if (input is InputEventKey eventKey)
        	if (eventKey.Pressed && !eventKey.Echo)
			{	//if(eventKey.Keycode == Key.Space) _on_play_pressed();
				if(eventKey.Keycode == Key.Right) _on_forward_pressed();
				if(eventKey.Keycode == Key.Left) _on_backward_pressed();
			}
	}
	private void _on_play_pressed()
	{
		//Daca nu a dat play, porneste. Altfel pauza
		if(!_stream.IsPlaying())
		{	_stream.Stop();
			_stream.Play();
			GetNode<TextureButton>("ControlsTint/Controls/Play").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/Pause.png");
			GetNode<TextureButton>("ControlsTint/Controls/Play").TexturePressed = GD.Load<CompressedTexture2D>("res://Sprites/PauseHighlight.png");
		}
		else _stream.Paused = !_stream.Paused;
		paused = _stream.Paused;
		if(!_stream.Paused) 
		{	GetNode<TextureButton>("ControlsTint/Controls/Play").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/Pause.png");
			GetNode<TextureButton>("ControlsTint/Controls/Play").TexturePressed = GD.Load<CompressedTexture2D>("res://Sprites/PauseHighlight.png");
			_on_timer_timeout();
		}
		else 
		{	GetNode<TextureButton>("ControlsTint/Controls/Play").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/Play.png");
			GetNode<TextureButton>("ControlsTint/Controls/Play").TexturePressed = GD.Load<CompressedTexture2D>("res://Sprites/PlayHighlight.png");
		}
	}
	//Scade StreamPosition cu 10s
	private void _on_backward_pressed()
	{	if(videocurrenttime < 10) _stream.StreamPosition = 0;
		else _stream.StreamPosition = _stream.StreamPosition - 10;
	}
	//Creste StreamPosition cu 10s
	private void _on_forward_pressed()
	{	if(videocurrenttime > _stream.GetStreamLength() - 10) _stream.StreamPosition = _stream.GetStreamLength();
		else _stream.StreamPosition = _stream.StreamPosition + 10;
	}
	private void _on_back_pressed()
	{	_slider.Value = 0; //Daca nu-l setezi la 0, cand iesi si reintrii, o sa inceapa de unde a ramas
		_stream.Stop();
		_stream.Paused = false;
		GetNode<TextureButton>("ControlsTint/Controls/Play").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/Play.png");
		GetNode<TextureButton>("ControlsTint/Controls/Play").SetPressedNoSignal(false);
		QueueFree();
	}
	//Cand tii apasat pe Seekbar...
	private void _on_drag_started()
	{	_stream.Paused = true;
		dragging = true;
		_timer.Stop();
	}
	private void _on_volume_drag_started()
	{	dragging = true;
		_timer.Stop();
	}
	//...si cand te opresti
	private void _on_drag_ended(bool value_changed)
	{	if(!paused)_stream.Paused = false;
		dragging = false;
	}

	private void _on_volume_changed(float value)
	{	dragging = false;
		_data.currentStats.VideoVolume = value;
		_stream.VolumeDb = _data.currentStats.VideoVolume;
		if(value == -60) GetNode<TextureButton>("ControlsTint/Controls/Volum/Volum").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/NoVolume.png");
		else if (value <= -30) GetNode<TextureButton>("ControlsTint/Controls/Volum/Volum").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/VolumeLow.png");
		else GetNode<TextureButton>("ControlsTint/Controls/Volum/Volum").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/VolumeHigh.png");
		_data.WriteSave(_data.LoggedUser);
	}
	//Butonul de fullscreen
	private void _on_fullscr_pressed()
	{	if(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed)
		{	DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			_data.currentStats.FullScr = true;
			GetNode<TextureButton>("ControlsTint/Controls/Fullscr").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/Winscr.png");
			GetNode<TextureButton>("ControlsTint/Controls/Fullscr").TexturePressed = GD.Load<CompressedTexture2D>("res://Sprites/WinscrHighlight.png");
		}
		else
		{	DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			_data.currentStats.FullScr = false;
			GetNode<TextureButton>("ControlsTint/Controls/Fullscr").TextureNormal = GD.Load<CompressedTexture2D>("res://Sprites/Fullscr.png");
			GetNode<TextureButton>("ControlsTint/Controls/Fullscr").TexturePressed = GD.Load<CompressedTexture2D>("res://Sprites/FullscrHighlight.png");
		}
		_data.WriteSave(_data.LoggedUser);
	}
	//Functiile de mai jos sunt pentru a ascunde butoanele atunci cand cursorul nu este intr-o anumita zona a ecranului
	private void _on_timer_timeout()
	{
		var tween = GetTree().CreateTween();
		tween.Parallel().TweenProperty(GetNode<ColorRect>("ControlsTint"), "modulate", new Color(1, 1, 1, 0), 0.15);
		tween.Parallel().TweenProperty(GetNode<ColorRect>("BackTint"), "modulate", new Color(1, 1, 1, 0), 0.15);
	}
	private async void _on_mouse_stopped()
	{	if(!dragging && _timer.IsStopped()) _timer.Start(1);
	}
	private void _on_mouse_moved()
	{	_timer.Stop();
		var tween = GetTree().CreateTween();
		tween.Parallel().TweenProperty(GetNode<ColorRect>("ControlsTint"), "modulate", new Color(1, 1, 1, 1), 0.15);
		tween.Parallel().TweenProperty(GetNode<ColorRect>("BackTint"), "modulate", new Color(1, 1, 1, 1), 0.15);
	}
	private void _on_seekbar_mouse_entered()
	{	var tween = GetTree().CreateTween();
		tween.TweenProperty(GetNode<HSlider>("ControlsTint/Seekbar/Seekbar"), "modulate", new Color(1, 1, 1, 1), 0.15);
	}
	private void _on_seekbar_mouse_exited() 
	{	var tween = GetTree().CreateTween();
		tween.TweenProperty(GetNode<HSlider>("ControlsTint/Seekbar/Seekbar"), "modulate", new Color(1, 1, 1, 0), 0.15);
	}
	private void _on_volum_pressed()
	{	if(_data.currentStats.VideoVolume != -60) _on_volume_changed(-60);
		else _on_volume_changed(0);
		_volume.Value = _data.currentStats.VideoVolume;
	}
	private void _on_volum_mouse_entered()
	{	var tween = GetTree().CreateTween();
		tween.TweenProperty(GetNode<Panel>("ControlsTint/Controls/Volum/Panel"), "custom_minimum_size", new Vector2(110, 0), 0.15);
	}
	private void _on_volum_mouse_exited()
	{	var tween = GetTree().CreateTween();
		tween.TweenProperty(GetNode<Panel>("ControlsTint/Controls/Volum/Panel"), "custom_minimum_size", new Vector2(0.01f, 0), 0.15);
	}
}
