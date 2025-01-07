//Clasa pentru intrebarii. Acestea pot aparea fie in chestionare, fie in lectii
using Godot;
using System;

public partial class Quizitem : PanelContainer
{
	public int type = 0;         //0: Lectie,        1: Normal,       2: Test        O schimbam cand initializam scena corespunzatoare
	[Export(PropertyHint.Range, "2,4,")] public int answnum = 4;      //Nr de raspunsuri posibile (intre 2 si 4)
	[Export(PropertyHint.Range, "1,4,")] public int answ = 1;
	public bool Complete = false;	//Pentru lectii
	public int Index = 0;	//Mai mult pentru GetChild()
	//Intrebarea, respectiv variantele de raspuns si explicatia
	[Export] public string question = "";
	[Export] public string answer1 = "";
	[Export] public string answer2 = "";
	[Export] public string answer3 = "";
	[Export] public string answer4 = "";
	[Export] public string explanation = "";
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	//Conectam Checkboxurile la functia _on_raspuns_pressed()
		GetNode<CheckBox>("PanelContainer/Raspuns1").Pressed += () => _on_raspuns_pressed(1);
		GetNode<CheckBox>("PanelContainer/Raspuns2").Pressed += () => _on_raspuns_pressed(2);
		GetNode<CheckBox>("PanelContainer/Raspuns3").Pressed += () => _on_raspuns_pressed(3);
		GetNode<CheckBox>("PanelContainer/Raspuns4").Pressed += () => _on_raspuns_pressed(4);
		
		if(answnum < 2 || answnum > 4) answnum = 4;
		if(answ > answnum) answ = answnum;
		//Cate raspunsuri posibile sunt 2, 3 si 4
		switch(answnum)
		{	case 2:
				GetNode<CheckBox>("PanelContainer/Raspuns3").Disabled = true;
				GetNode<CheckBox>("PanelContainer/Raspuns3").Hide();
				GetNode<CheckBox>("PanelContainer/Raspuns4").Disabled = true;
				GetNode<CheckBox>("PanelContainer/Raspuns4").Hide();
				break;
			case 3:
				GetNode<CheckBox>("PanelContainer/Raspuns4").Disabled = true;
				GetNode<CheckBox>("PanelContainer/Raspuns4").Hide();
				break;
		}
		
		GetNode<Label>("PanelContainer/Name").Text = question;
		GetNode<CheckBox>("PanelContainer/Raspuns1").Text = answer1;
		GetNode<CheckBox>("PanelContainer/Raspuns2").Text = answer2;
		GetNode<CheckBox>("PanelContainer/Raspuns3").Text = answer3;
		GetNode<CheckBox>("PanelContainer/Raspuns4").Text = answer4;
		GetNode<Label>("Corect/hBoxContainer/Explanation").Text = explanation;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	//Ca sa nu trimiti raspunsul fara sa alegi o optiune
		var pressed = false;
		for (int i = 1; i <=4; i++) if(GetNode<CheckBox>("PanelContainer/Raspuns" + i).ButtonPressed == true) pressed = true;
		GetNode<Button>("PanelContainer/HBoxContainer/Send").Disabled = !pressed;
	}
	private void _on_raspuns_pressed(int index)
	{	//Asta este mai mult ca sa avem doar un singur raspuns bifat
		if(GetNode<CheckBox>("PanelContainer/Raspuns" + index).ButtonPressed == true);
		{	for (int i = 1; i <=4; i++) if(i!=index && GetNode<CheckBox>("PanelContainer/Raspuns" + i).ButtonPressed == true) GetNode<CheckBox>("PanelContainer/Raspuns" + i).SetPressedNoSignal(false);
		}
	}
	public void _disable()
	{
		GetNode<CheckBox>("PanelContainer/Raspuns1").Disabled = true;
		GetNode<CheckBox>("PanelContainer/Raspuns2").Disabled = true;
		GetNode<CheckBox>("PanelContainer/Raspuns3").Disabled = true;
		GetNode<CheckBox>("PanelContainer/Raspuns4").Disabled = true;
		GetNode<Button>("PanelContainer/HBoxContainer/Send").Disabled = true;
		GetNode<Button>("PanelContainer/HBoxContainer/Skip").Disabled = true;
	}
	private void _on_send_pressed()
	{	//Facem o lista cu toate casutele bifate
		var a = new Godot.Collections.Array()
		{	GetNode<CheckBox>("PanelContainer/Raspuns1").ButtonPressed, 
			GetNode<CheckBox>("PanelContainer/Raspuns2").ButtonPressed,
			GetNode<CheckBox>("PanelContainer/Raspuns3").ButtonPressed,
			GetNode<CheckBox>("PanelContainer/Raspuns4").ButtonPressed
		};
		//determinam daca este raspuns corect
		var ok = true;
		for (int i = 1; i <=4; i++)
		{	if(answ == i && (bool)a[i-1] != true) ok = false;
			else if(answ != i && (bool)a[i-1] != false) ok = false; 
		}
		if(ok) ShowCorrect();
		GetNode("/root").GetChild(-1).EmitSignal("GetAnswers", ok, false, Index);
	}
	private void _on_skip_pressed() => GetNode("/root").GetChild(-1).EmitSignal("Skip");
	private void ShowCorrect()
	{	if(type < 2)
		{	GetNode<VBoxContainer>("PanelContainer").Hide(); 
			GetNode<VBoxContainer>("Corect").Show();
			if(type == 1) GetNode<HBoxContainer>("Corect/Button").Show();
			_disable();
		}
	}
}
