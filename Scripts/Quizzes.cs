//Pentru fereastra cu chestionare si teste
using Godot;
using System;

public partial class Quizzes : Control
{
	private DefaultData _data;
	private Vector2 mousepos;
	private bool inputgrab;
	private Vector2 dif;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	_data = (DefaultData)GetNode("/root/DefaultData");
		//Calculam numarul de lectii terminate
		_data.currentStats.FinishedLes = 0;
		int i = 1;
		while(_data.lessonList.ContainsKey(i))
		{	if((int)_data.currentStats.LessonCompletion[i] == 100 && (int)_data.lessonList[i][1] != 2) _data.currentStats.FinishedLes++;
			i++;
		}
		GD.Print(_data.currentStats.FinishedLes);
		//Daca nu indeplineste conditia, nu lasa utilizatorul sa faca chestionare si teste
		if(_data.currentStats.FinishedLes >= 2) 
		{	GetNode<Label>("Panel/Requirement").Hide();
			GetNode<Panel>("Panel/Settings").Show();
		}
		else
		{	GetNode<Label>("Panel/Requirement").Show();
			GetNode<Panel>("Panel/Settings").Hide();
		}
		GetNode<Label>("Panel/Settings/Stats").Text = "Chestionare terminate: " + _data.currentStats.Questionaires + "\nLectii terminate: " + _data.currentStats.FinishedLes;
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
	private void _on_back_pressed() => QueueFree();
	//Cele doua functii sunt pentru modul Antrenament
	private async void _on_training_pressed()
	{	_data.questiontype = 0;
		_data.LoadScene("res://Scenes/Quizztime.tscn");
	}
	private void _on_training_mouse_entered() => GetNode<Label>("Panel/Settings/Description2").Text = "In modul antrenament, se genereaza un chestionar aleatoriu." + 
	"\nNu conteaza daca ai raspuns corect sau gresit.";
	//Cele doua functii sunt pentru modul Test
	private void _on_test_pressed()
	{	_data.questiontype = 1;
		_data.LoadScene("res://Scenes/Quizztime.tscn");
	}
	private void _on_test_entered() => GetNode<Label>("Panel/Settings/Description2").Text = "In modul test, se genereaza un chestionar aleatoriu." +
	"\nAi doua minute pentru rezolvare" +
	"\nNumarul raspunsurilor corecte si gresite se vor arata la finalul testului" + 
	"\nMult noroc";
	
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
}
