//Pentru chestionare (modurile Antrenament si Test)
using Godot;
using System;

public partial class Quizztime : Node2D
{
	private DefaultData _data;
	public PackedScene _item;	//Intrebare
	public Node2D _transition;
	Godot.Collections.Array<Godot.Collections.Array> _list;		//Lista cu intrebari
	int total = 10;
	int intrebari = 0;
	int corecte = 0;
	int gresite = 0;
	int timp = 120; //Timpul de rezolvare
	Godot.Collections.Array chars = new Godot.Collections.Array(){"/", "%", "&", "$", "@", "$", "#", "(", ")", "-", "0", "O"};   //Pentru efectul de glitch. Probabil o sa fac o functie globala in DefaultData
	string randomstr = "";	//Tot pentru efectul de glitch
	public Timer timer;
	public Timer timp_ramas;
	TimeSpan tp;
	[Signal] public delegate void GetAnswersEventHandler(bool correct, bool ignore, int index);	//Semnal pentru intrebarile din lectie
	[Signal] public delegate void SkipEventHandler();	//Semnal pentru trecerea peste o intrebare
	[Signal] public delegate void NextEventHandler();	//Semnal pentru trecerea la urmatoarea intrebare
	// Called when the node enters the scene tree for the first time.
	public async override void _Ready()
	{	_data = (DefaultData)GetNode("/root/DefaultData");
		_item = GD.Load<PackedScene>("res://Scenes/Quizitem.tscn");
		_transition = GetNode<Node2D>("Transition");
		//Conectam semnalele la functiile lor
		GetAnswers += SendAnswers;
		Skip += SkipQuestion;
		Next += NextQuestion;
		_list = _data.GenerateQuestionSet(total);	//Genereaza lista de intrebari
		GD.Print(_list);
		if(_data.questiontype == 1)	//Daca este test
		{	GetNode<Label>("Body/Title").Text = "Test";
			GetNode<RichTextLabel>("Body/Correct").TooltipText = "Eroare: nr_intrebari_corecte nu poate fi afisat";
			GetNode<RichTextLabel>("Body/Wrong").TooltipText = "Eroare: nr_intrebari_gresite nu poate fi afisat";
			GetNode<Label>("Body/RemainedTime").Show();
			timp_ramas = GetNode<Timer>("Timp");
			timp_ramas.Timeout += _timp_scurs;
			timp_ramas.Start(timp);
			if(!_data.currentStats.QNumOnly) _timeout();
		}
		if(!_data.currentStats.QNumOnly && _data.currentStats.Anims)
		{	timer = GetNode<Timer>("ShakeTimer");
			timer.Autostart = true;
			timer.Timeout += _timeout;
			timer.Start((float)0.15);
		}
		else if(_data.currentStats.QNumOnly && _data.questiontype == 1)
		{	GetNode<RichTextLabel>("Body/Correct").Hide();
			GetNode<RichTextLabel>("Body/Wrong").Hide();
			var pos = GetNode<Label>("Body/Number").Position;
			pos.X = 456;
			GetNode<Label>("Body/Number").Position = pos;
		}

		if(_data.currentStats.Anims)
		{	if(_data.questiontype == 1) timp_ramas.Paused = true;
			GetNode<Node2D>("Body").Hide();
			GetNode<TextureButton>("Back").Hide();
			GetNode<Node2D>("Transition").Show();
			GetNode<Label>("Transition/Title").Text = GetNode<Label>("Body/Title").Text;
			if(_data.questiontype == 1) GetNode<Label>("Transition/Title").Text = GetNode<Label>("Transition/Title").Text + "\nTimp de rezolvare: " + TimeSpan.FromSeconds(timp_ramas.TimeLeft).ToString("mm\\:ss");
			var tween = GetTree().CreateTween();
			var pos = Position;
			pos.X = 0;
			pos.Y = 268 - 10;
			GetNode<Label>("Transition/Title").Modulate = new Color(1, 1, 1, 0);
			GetNode<Label>("Transition/Title").Position = pos;
			pos.Y = 268;
			tween.TweenProperty(GetNode<Label>("Transition/Title"), "modulate", new Color(1, 1, 1, 1), 0.25);
			tween.Parallel().TweenProperty(GetNode<Label>("Transition/Title"), "position", pos, 0.25);
			await ToSignal(tween, Tween.SignalName.Finished);
			var timer = GetTree().CreateTimer(1);
			await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
			tween.Stop();
			tween = GetTree().CreateTween();
			GetNode<Node2D>("Body").Show();
			GetNode<TextureButton>("Back").Show();
			GetNode<Node2D>("Body").SelfModulate = new Color(1, 1, 1, 0);
			GetNode<TextureButton>("Back").SelfModulate = new Color(1, 1, 1, 0);
			tween.TweenProperty(GetNode<Label>("Transition/Title"), "modulate", new Color(1, 1, 1, 0), 0.25);
			tween.Parallel().TweenProperty(GetNode<Node2D>("Body"), "self_modulate", new Color(1, 1, 1, 1), 0.25);
			tween.Parallel().TweenProperty(GetNode<TextureButton>("Back"), "self_modulate", new Color(1, 1, 1, 1), 0.25);
			await ToSignal(tween, Tween.SignalName.Finished);
			if(_data.questiontype == 1) timp_ramas.Paused = false;
		}
		NextQuestion();	//Treci la prima intrebare
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	//Afisarea textelor de sus
		GetNode<Label>("Body/Number").Text = "Intrebare: " + intrebari + "/" + total;
		if(_data.questiontype == 0)
		{	GetNode<RichTextLabel>("Body/Correct").Text = "Corecte: " + corecte;
			GetNode<RichTextLabel>("Body/Wrong").Text = "Gresite: " + gresite;
		}
		else if(_data.questiontype == 1)	//Timerul pentru test
		{	tp = TimeSpan.FromSeconds(timp_ramas.TimeLeft);
			GetNode<Label>("Body/RemainedTime").Text = "Timp ramas: " + tp.ToString("mm\\:ss");
			if(timp_ramas.TimeLeft <= (float)6.0) GetNode<Label>("Body/RemainedTime").SelfModulate = new Color(1, 0, 0, 1);
			else if(timp_ramas.TimeLeft <= (float)11.0) GetNode<Label>("Body/RemainedTime").SelfModulate = new Color(1, 1, 0, 1);
		}
	}
	private void _on_continue_pressed()	//Butonul pentru continuare, cand este afisata si explicatia
	{	GetNode<Button>("Continue").Hide();
		NextQuestion();
	}

	private void _on_back_pressed()
	{	//Creeaza o fereastra de confirmare, daca s-a raspuns la cel putin o intrebare
		GD.Print("Pressed");
		int n = 0;
		for(int i = 0; i < total; i++) if(_list[i].Count != 0) n++;
		GD.Print(n);
		if(n < 10 && n != 0)
		{	var scene = (Confirm)GD.Load<PackedScene>("res://Scenes/Confirm.tscn").Instantiate();
			scene.reason = 1;
			AddChild(scene);
			GetTree().Paused = true;
		}
		else
		{	_data.questiontype = 0;
			GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
		}
	}
	private void _on_retry_pressed() => GetTree().ReloadCurrentScene();		//Reincearca un nou set de intrebari
	//Functie pentru raspunsuri
	public void SendAnswers(bool correct, bool ignore, int index)
	{	//Ignore este mai mult pentru functia de sarit peste
		if(!ignore)
		{
			if(correct)
			{	corecte++;
				_list[intrebari-1]=null;
				GD.Print("Am sters " + intrebari);
				if(_data.questiontype == 0) GetNode<Button>("Continue").Show();
			}
			else if(!correct)
			{	gresite++;
				_list[intrebari-1]=null;
				GD.Print("Am sters " + intrebari);
				NextQuestion();
				return;
			}
		}
		if(intrebari<=total)
		{
			if(_data.questiontype == 1) NextQuestion();   //Nu arata explicatia. Treci mai departe
		}
		else 
		{	var name = (String)GetChild(-1).Name;
			if(name.Contains("Quizitem")) GetChild(-1).QueueFree();
			_finished();
			return;
		}
	}
	//Functie pentru generarea unui nou string pentru efectul de glitch
	public void _timeout()
	{	randomstr = "";
		var rand = new Godot.Collections.Array(){(string)chars[GD.RandRange(0, 11)], (string)chars[GD.RandRange(0, 11)]};
		randomstr = String.Join("", rand);
		if(_data.questiontype == 1) GetNode<RichTextLabel>("Body/Correct").Text = "Corecte: [shake rate=100.0 level=20 connected=1][color=#e5e5e5]" + randomstr  + "[/color][/shake]";
		randomstr = "";
		rand = new Godot.Collections.Array(){(string)chars[GD.RandRange(0, 11)], (string)chars[GD.RandRange(0, 11)]};
		randomstr = String.Join("", rand);
		if(_data.questiontype == 1) GetNode<RichTextLabel>("Body/Wrong").Text = "Gresite: [shake rate=100.0 level=20 connected=1][color=#e5e5e5]" + randomstr + "[/color][/shake]";
	}
	public void _timp_scurs()
	{	GD.Print("TIMPUL S-A SCURS!");
		int nr=0;
		for(int i=1;i<=total;i++)
		{	if(_list[i-1].Count != 0) nr++;
		}
		intrebari=total-nr;
		var name = (String)GetChild(-1).Name;
		if(name.Contains("Quizitem")) GetChild(-1).QueueFree();
		GetNode<Label>("Body/RemainedTime").Hide();
		QuestionFinished();
	}
	public async void NextQuestion()
	{	if(intrebari<total)
		{	GD.Print(_list[intrebari]);
			if(_list[intrebari].Count != 0) intrebari++;    //Daca exista urmatoarea intrebare, doar aduna-l cui 1
			//Altfel, cauta urmatoarea urmatoarea intrebarea in vector
			else 
			{	for(int i=intrebari+1;i<=total;i++)
				{	if(_list[i-1].Count != 0)
					{	intrebari=i-1;
						GD.Print("Sunt la " + (i-1));
						NextQuestion();
						return;
					}
				}
				//Trecem si prin elementele anterioare
				for(int i=1;i<=intrebari;i++)
				{	if(_list[i-1].Count != 0)
					{	intrebari=i-1;
						GD.Print("Sunt la " + (i-1));
						NextQuestion();
						return;
					}
				}
				//Daca n-am gasit o intrebare, atunci trebuie sa fi terminat
				intrebari=total;
				var name1 = (String)GetChild(-1).Name;
				if(name1.Contains("Quizitem")) GetChild(-1).QueueFree();
				_finished();
				return;
			}
			//Creeam intrebarea
			GD.Print(intrebari);
			var name = (String)GetChild(-1).Name;
			if(name.Contains("Quizitem")) GetChild(-1).QueueFree();
			var _panel =  _item.Instantiate<Quizitem>();
			_panel.answnum = (int)_list[intrebari-1][0];
			_panel.answ = (int)_list[intrebari-1][1];
			_panel.question = (string)_list[intrebari-1][2];
			_panel.answer1 = (string)_list[intrebari-1][3];
			_panel.answer2 = (string)_list[intrebari-1][4];
			_panel.answer3 = (string)_list[intrebari-1][5];
			_panel.answer4 = (string)_list[intrebari-1][6];
			_panel.explanation = (string)_list[intrebari-1][7];
			var pos = Position;
			pos.X = 296;
			pos.Y = 202;
			_panel.Position = pos;
			_panel.type = _data.questiontype + 1;
			if(_panel.type == 2) 
			{
				int nr=0;
				for(int i=1;i<=total;i++)
				{	if(_list[i-1].Count != 0) nr++;
				}
				if(nr >= 2) _panel.GetNode<CanvasItem>("PanelContainer/HBoxContainer/Skip").Show();
			}
			AddChild(_panel);
			_panel.Name = "Quizitem";
			//Animatie
			if(_data.currentStats.Anims)
			{	var tween = GetTree().CreateTween();
				_panel.SelfModulate = new Color(1, 1, 1, (float)0.2);
				tween.TweenProperty(_panel, "self_modulate", new Color(1, 1, 1, 1), 0.2);
				await ToSignal(tween, Tween.SignalName.Finished);
			}
		}
		else
		{	//Cautam daca mai sunt intrebari neraspunses
			if(_data.questiontype == 1)
			{	for(int i=1;i<=total;i++)
				{	if(_list[i-1].Count != 0)
					{	intrebari=i-1;
						GD.Print("Sunt la " + (i-1));
						NextQuestion();
						return;
					}
				}
			}
			//Daca nu mai sunt, atunci am terminat
			var name = (String)GetChild(-1).Name;
			if(name.Contains("Quizitem")) GetChild(-1).QueueFree();
			_finished();
		}
	}
	//functie de sarit peste intrebare
	public async void SkipQuestion()
	{	GD.Print("Salt peste " + intrebari);
		SendAnswers(true, true, intrebari);
	}
	//Functie pentru terminarea chestionarului si initializam secventa de final
	private void _finished()
	{	if(_data.questiontype == 0) GetNode<Button>("Continue").Hide();
		GD.Print("Done");
		QuestionFinished();
	}
	//Functia pentru afisarea rezultatului si cele doua butoane de jos
	private async void QuestionFinished()
	{	GetNode<Timer>("Timp").Stop();
		GetNode<Timer>("ShakeTimer").Stop();
		GetNode<TextureButton>("Back").Disabled = true;
		var tween = GetTree().CreateTween();
		tween.TweenProperty(GetNode<Label>("Body/Title"), "modulate", new Color(1, 1, 1, 0), 0.25);
		tween.Parallel().TweenProperty(GetNode<Label>("Body/RemainedTime"), "modulate", new Color(1, 1, 1, 0), 0.25);
		tween.Parallel().TweenProperty(GetNode<TextureButton>("Back"), "modulate", new Color(1, 1, 1, 0), 0.25);
		await ToSignal(tween, Tween.SignalName.Finished);
		tween.Stop();
		tween = GetTree().CreateTween();
		var pos = Position;
		pos.Y = 250;
		pos.X = 157;
		tween.TweenProperty(GetNode<Label>("Body/Number"), "position", pos, 1).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		pos.X = 562;
		if(_data.questiontype == 1)
		{	var rand = new Godot.Collections.Array(){(string)chars[GD.RandRange(0, 11)], (string)chars[GD.RandRange(0, 11)]};
			randomstr = String.Join("", rand);
			GetNode<RichTextLabel>("Body/Correct").Text = "Corecte: [shake rate=100.0 level=20 connected=1][color=#e5e5e5]" + randomstr  + "[/color][/shake]";
			GetNode<RichTextLabel>("Body/Correct").SelfModulate = new Color(1, 1, 1, 0);
		}
		GetNode<RichTextLabel>("Body/Correct").Show();
		tween.Parallel().TweenProperty(GetNode<RichTextLabel>("Body/Correct"), "self_modulate", new Color(1, 1, 1, 1), 1).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tween.Parallel().TweenProperty(GetNode<RichTextLabel>("Body/Correct"), "position", pos, 1).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		pos.X = 863;
		
		if(_data.questiontype == 1)
		{	var rand = new Godot.Collections.Array(){(string)chars[GD.RandRange(0, 11)], (string)chars[GD.RandRange(0, 11)]};
			randomstr = String.Join("", rand);
			GetNode<RichTextLabel>("Body/Wrong").Text = "Gresite: [shake rate=100.0 level=20 connected=1][color=#e5e5e5]" + randomstr  + "[/color][/shake]";
			GetNode<RichTextLabel>("Body/Wrong").SelfModulate = new Color(1, 1, 1, 0);
		}
		GetNode<RichTextLabel>("Body/Wrong").Show();
		tween.Parallel().TweenProperty(GetNode<RichTextLabel>("Body/Wrong"), "self_modulate", new Color(1, 1, 1, 1), 1).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tween.Parallel().TweenProperty(GetNode<RichTextLabel>("Body/Wrong"), "position", pos, 1).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		var timer = GetTree().CreateTimer(0.45);
		await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
		if(_data.questiontype == 1)
		{	timer = GetTree().CreateTimer(1.15);
			await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
			GetNode<RichTextLabel>("Body/Correct").Text = "Corecte: " + corecte;
			GetNode<RichTextLabel>("Body/Wrong").Text = "Gresite: " + gresite;
			if(corecte>=5)	_data.currentStats.goodtests++;
			if(corecte>=7)	_data.currentStats.greattest++;
			if(corecte==10) 
			{
				_data.currentStats.flawlesstests++;
				//Confeti
				if(_data.currentStats.Anims)
				{	GetNode<GpuParticles2D>("Confetti/Left").Emitting = true;
					GetNode<GpuParticles2D>("Confetti/Center").Emitting = true;
					GetNode<GpuParticles2D>("Confetti/Right").Emitting = true;
				}
			}
			_data.currentStats.Testsnum++;
			_data.WriteSave(_data.LoggedUser);
		}
		else
		{	_data.currentStats.Questionaires++;
			_data.WriteSave(_data.LoggedUser);
		}
		tween = GetTree().CreateTween();
		GetNode<Label>("Feedback").Show();
		if(corecte == 10) 
		switch((int)GD.RandRange(1, 5))
		{	case 1:
				GetNode<Label>("Feedback").Text = "Nicio greseala. Felicitari!";
				break;
			case 2:
				GetNode<Label>("Feedback").Text = "10 cu steluta!";
				break;
			case 3:
				if(_data.questiontype == 0)GetNode<Label>("Feedback").Text = "Felicitari! Cu siguranta te vei descurca si la test!";
				else GetNode<Label>("Feedback").Text = "Felicitari! Continua tot asa!";
				break;
			case 4:
				GetNode<Label>("Feedback").Text = "Uimitor!";
				break;
			case 5:
				GetNode<Label>("Feedback").Text = "Nu am cuvinte!";
				break;
		}
		else if(corecte >= 7) 
			switch((int)GD.RandRange(1, 2))
			{	case 1:
					GetNode<Label>("Feedback").Text = "Foarte bine!";
					break;
				case 2:
					GetNode<Label>("Feedback").Text = "Bravo!";
					break;
			}
		else if(corecte >= 5) GetNode<Label>("Feedback").Text = "Te-ai descurcat destul de bine, dar stiu ca poti sa faci si mai bine!";
		else if(corecte >= 3) GetNode<Label>("Feedback").Text = "Stiu ca poti si mai bine!";
		else 
		switch((int)GD.RandRange(1, 3))
		{	case 1:
				GetNode<Label>("Feedback").Text = "Te rog consulta lectiile.";
				break;
			case 2:
				GetNode<Label>("Feedback").Text = "Citeste cu atentie intrebarile.";
				break;
			case 3:
				GetNode<Label>("Feedback").Text = ":(";
				break;
		}
		GetNode<Button>("Exit").Visible = true;
		GetNode<Button>("Retry").Visible = true;
		tween.TweenProperty(GetNode<Label>("Feedback"), "self_modulate", new Color(1, 1, 1, 1), 0.25).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(GetNode<Button>("Exit"), "self_modulate", new Color(1, 1, 1, 1), 0.2).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(GetNode<Button>("Retry"), "self_modulate", new Color(1, 1, 1, 1), 0.2).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
	}
}
