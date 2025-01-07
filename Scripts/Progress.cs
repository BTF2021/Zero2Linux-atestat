//Pentru fereastra de progres
using Godot;
using System;

public partial class Progress : Control
{
	private DefaultData _data;
	private VBoxContainer _item;	//Itemul-model
	private int spc, unfinished;	//Variabile locale pentru numararea lectiilor speciale, respectiv cele neterminate
	private Vector2 mousepos;
	private bool inputgrab;
	private Vector2 dif;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	_data = (DefaultData)GetNode("/root/DefaultData");
		_item = GetNode<VBoxContainer>("Panel/Panel/ScrollContainer/VBoxContainer/Item");
		GetNode<Label>("Panel/Panel/ScrollContainer/VBoxContainer/Account/Name").Text = _data.currentStats.UsrName;
		GetNode<Label>("Panel/Panel/ScrollContainer/VBoxContainer/Account/BigLetter").Text = _data.currentStats.UsrName;
		GetNode<Sprite2D>("Panel/Panel/ScrollContainer/VBoxContainer/Account/Bg").SelfModulate = _data.currentStats.FavColor;

		//Calculam cate lectii sunt deja terminate
		_data.currentStats.FinishedLes = 0;
		int i = 1;
		while(_data.lessonList.ContainsKey(i))
		{	if((int)_data.currentStats.LessonCompletion[i] == 100 && (int)_data.lessonList[i][1] != 2) _data.currentStats.FinishedLes++;
			else if((int)_data.currentStats.LessonCompletion[i] == 100 && (int)_data.lessonList[i][1] == 2) spc++;
			else if((int)_data.currentStats.LessonCompletion[i] < 100 && (int)_data.currentStats.LessonCompletion[i] > 0) unfinished++;
			i++;
		}

		//Adaugam elemente in lista
		if(unfinished > 0)
		{	AddItem("Lectii incepute: " + unfinished, null);
			i=1;
			while(_data.lessonList.ContainsKey(i))
			{	if((int)_data.currentStats.LessonCompletion[i] < 100 && (int)_data.currentStats.LessonCompletion[i] > 0) AddItem((string)_data.lessonList[i][0], GetNode<VBoxContainer>("Panel/Panel/ScrollContainer/VBoxContainer").GetChild<VBoxContainer>(-1));
				i++;
			}
		}
		else AddItem("Nu ai nicio lectie inceputa", null);
		if(_data.currentStats.FinishedLes > 0)
		{	AddItem("Lectii finalizate: " + (_data.currentStats.FinishedLes + spc), null);
			i=1;
			while(_data.lessonList.ContainsKey(i))
			{	if((int)_data.currentStats.LessonCompletion[i] == 100) AddItem((string)_data.lessonList[i][0], GetNode<VBoxContainer>("Panel/Panel/ScrollContainer/VBoxContainer").GetChild<VBoxContainer>(-1));
				i++;
			}
		}
		else AddItem("Nu ai nicio lectie terminata", null);
		AddItem("Chestionare facute: " + _data.currentStats.Questionaires, null);
		AddItem("Teste facute: " + _data.currentStats.Testsnum, null);
		AddItem("Peste nota 5: " + _data.currentStats.goodtests, GetNode<VBoxContainer>("Panel/Panel/ScrollContainer/VBoxContainer").GetChild<VBoxContainer>(-1));
		AddItem("Peste nota 7: " + _data.currentStats.greattest, GetNode<VBoxContainer>("Panel/Panel/ScrollContainer/VBoxContainer").GetChild<VBoxContainer>(-1));
		AddItem("Nota 10: " + _data.currentStats.flawlesstests, GetNode<VBoxContainer>("Panel/Panel/ScrollContainer/VBoxContainer").GetChild<VBoxContainer>(-1));

		if(_data.currentStats.Anims)
		{
			var tween = GetTree().CreateTween();
			GetNode<Sprite2D>("Panel").Modulate = new Color(1, 1, 1, 0);
			var pos = Position;
			pos.X = 645;
			pos.Y = 339 - 25;
			GetNode<Sprite2D>("Panel").Position = pos;
			pos.Y = 339;
			tween.TweenProperty(GetNode<Sprite2D>("Panel"), "modulate", new Color(1, 1, 1, 1), 0.15).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
			tween.Parallel().TweenProperty(GetNode<Sprite2D>("Panel"), "position", pos, 0.15).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	//Pentru miscarea ferestrei
		mousepos = GetViewport().GetMousePosition();
		var winpos = GetNode<Sprite2D>("Panel").Position;
		var newpos = Position;
		//Pozitia este raportata la centrul ferestrei
		newpos.X = Mathf.Clamp(Mathf.Lerp(winpos.X, mousepos.X + dif.X, 1), 0, 1280);
		newpos.Y = Mathf.Clamp(Mathf.Lerp(winpos.Y, mousepos.Y + dif.Y, 1), 279, 920);
		if(inputgrab) GetNode<Sprite2D>("Panel").Position = newpos;
	}
	private void _on_back_pressed() => QueueFree();		//Inchide fereastra
	private void _on_drag_down()	//Cand partea de sus a ferestrei este apasata
	{	GD.Print("Hi");
		#if GODOT_ANDROID
			//Desi mousepos este preluat in _Proccess(), mousepos ramane aceeasi valoare dupa ce ecranul a fost atins
			//Presupun ca ii ia un frame ca sa proceseze noua pozitie, ceea ce nu este de ajuns pentru aceasta functie
			//Asa ca il actualizam acum
			mousepos = GetViewport().GetMousePosition();
		#endif
		var winpos = GetNode<Sprite2D>("Panel").Position;
		dif.X = winpos.X - mousepos.X;
		dif.Y = winpos.Y - mousepos.Y;
		GD.Print(dif);
		inputgrab = true;
		GetParent().MoveChild(this, -1);
	}
	private void _on_drag_up()	//Cand partea de sus a ferestrei nu mai este apasata
	{	GD.Print("Bye");
		inputgrab = false;
	}
	//Buton pentru aratarea/ascunderea subcategoriilor
	private void _on_button_pressed(Node _node)
	{	_node.GetNode<HBoxContainer>("SubContainer").Visible = !_node.GetNode<HBoxContainer>("SubContainer").Visible;
		_node.GetNode<TextureRect>("Panel/Contents/Arrow").FlipV = !_node.GetNode<TextureRect>("Panel/Contents/Arrow").FlipV;
	}
	//Functie pentru crearea elementelor din lista
	//Ca sa putem face o sublista pentru o anumita categorie, ne trebuie si nodul respectiv
	private void AddItem(string text, VBoxContainer parent_node)
	{	var button = (VBoxContainer)_item.Duplicate(8);
		button.Visible = true;
		button.GetNode<Label>("Panel/Contents/Stat").Text = text;
		button.GetNode<Button>("Panel/Button").Pressed += () => _on_button_pressed(button);
		if(parent_node != null)
		{	parent_node.GetNode<Button>("Panel/Button").Disabled = false;
			parent_node.GetNode<Button>("Panel/Button").Visible = true;
			parent_node.GetNode<TextureRect>("Panel/Contents/Arrow").Visible = true;
			parent_node.GetNode<VBoxContainer>("SubContainer/Child").AddChild(button);
		}
		else GetNode<VBoxContainer>("Panel/Panel/ScrollContainer/VBoxContainer").AddChild(button);
	}
}
