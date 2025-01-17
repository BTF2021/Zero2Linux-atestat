//Folosit pentru incarcarea scenelor, cum ar fi lectiile si chestionarele (desi nu dureaza mult incarcarea)
using Godot;
using System;

public partial class Incarcare : Node2D
{
	private DefaultData _data;
	[Export]public string target;			//Scena incarcata
	private bool done;						//Pentru avertizarile legate de Tweenuri
	public Godot.Collections.Array progress, progresslesson;	//Progresul
	public override async void _Ready()
	{	_data = (DefaultData)GetNode("/root/DefaultData");
		GetNode<RichTextLabel>("Tip").Text = "[center]";
		done = false;
		GD.Print(target);
		Godot.Collections.Array<string> splash = new Godot.Collections.Array<string>(){		//Mesajele de mai jos. Se alege unul la intamplare
			"Se incarca...",
			"Insereaza un text aici",
			"Acest text este [i]pseudo[/i]-aleatoriu",
			"Viziteaza pagina de Github al acestui proiect pentru noi actualizari",
			"Fun fact: Android este bazat pe un kernel modificat de linux",
			"Fun fact: Majoritatea serverelor folosesc Linux",
			"Fun fact: Linux Mint este derivat dintr-o derivata a Debian",
			"Fun fact: Linux Torvalds foloseste Fedora",
			"Fun Fact: Freax era numele oficial pentru Linux",
			"Fun fact: Probabil ai mai multe dispozitive Linux decat crezi",
			"Fun fact: Exista un antivirus open-source pentru Linux",
			"Fun fact: Poti folosi Linux si pe un stick usb",
			"Viziteaza alternativeto.net pentru a vedea o lista de alternative pentru programe",
			"Daca nu stii cum se utilizeaza un program, exista intotdeauna manualul/wikiul",
			"Vim, Nano, Emacs... nu conteaza atata timp cat poti edita un fisier text",
			":q + Enter pentru a iesi din Vim",
			"E GNU/Linux",
			"[i]Read the friendly manual[/i]",
			"Eu folosesc Arch Linux (by the way)",
			"Este cumva " + Time.GetDateStringFromSystem().Substr(0, 4) + " anul Linux?",
			"Ai auzit de Justin Bieber Linux?",
			"Ai auzit de Hannah Montana Linux?",
			"[wave amp=45.0 freq=7.0 connected=1]<ooooooooooooooooooooooooooooooooooooooooooooooooooooe[/wave]",    //Ar trebui sa arate a sarpe
			"[i]Conform tuturor legilor cunoscute ale aviatie, o albina nu ar trebui sa poata zbura[/i]",
			"     Ha ha! Textul nu mai este centrat",
			"[i]Poate sa ruleze Windows Defender?[/i]",
			"[i]Pur si simplu functioneaza[/i]    -Absolut nimeni",
			"[i]Nu uita sa respiri. Foarte important[/i]",
			"Nu am spus ca acest text o sa fie util...",
			"Segmentation fault (core dumped)"
		};
		int size = splash.Count;
		var tip = (int)GD.RandRange(0, size);
		if(tip==size)
		{
			var name = "";
			switch(OS.GetName())	//Mesajul este in functie de sistemul de operare
			{	case "Windows":
					name = "Poti incerca o distributie de linux intr-o masina virtuala sau intr-un container";
					break;
				case "Linux":
					name = "Nu uita sa-ti actualizezi periodic programele instalate!";
					break;
				case "Android":
					name = "Fun fact: Se poate rula Linux si pe dispozitive cu procesoare ARM";
					break;
			}
			GetNode<RichTextLabel>("Tip").Text = GetNode<RichTextLabel>("Tip").Text + name + "[/center]";
		}
		else
		{
			GetNode<RichTextLabel>("Tip").Text = GetNode<RichTextLabel>("Tip").Text + splash[tip] + "[/center]";
		}
		//Easter Egg bazat pe data sistemului :)
		if(Time.GetDatetimeStringFromSystem().Find("09", 4) == 5 && Time.GetDatetimeStringFromSystem().Find("17", 7) == 8) GetNode<RichTextLabel>("Tip").Text = "[center]La multi ani Linux![/center]";
		else if(Time.GetDatetimeStringFromSystem().Find("12", 4) == 5 && Time.GetDatetimeStringFromSystem().Find("28", 7) == 8) GetNode<RichTextLabel>("Tip").Text = "[center]La multi ani Linus Torvalds![/center]";

		GetNode<ProgressBar>("Bara").Value = 0;
		if(ResourceLoader.Exists(target)) ResourceLoader.LoadThreadedRequest(target);	//Cere incarcarea scenei pe un alt thread
		//Daca scena incarcata e lectie, incarca si continutul pe un alt thread
		if(target == "res://Scenes/Lesson.tscn" && ResourceLoader.Exists("res://Courses/Lesson_" + _data.CurrentLesson + "/Lesson.tscn"))
			ResourceLoader.LoadThreadedRequest("res://Courses/Lesson_" + _data.CurrentLesson + "/Lesson.tscn");
		if(_data.currentStats.Anims)
		{	GetNode<Node2D>(".").Modulate = new Color(1, 1, 1, 0);
			var tween = GetTree().CreateTween();
			tween.TweenProperty(GetNode<Node2D>("."), "modulate", new Color(1, 1, 1, 1), 0.25);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	if(!GetNode<AnimationPlayer>("Icon/Animation").IsPlaying()) GetNode<AnimationPlayer>("Icon/Animation").Play("Rotate");	//Roteste iconita
		bool ok1 = false;
		bool ok2 = false;
		if(target != "res://Scenes/Lesson.tscn") ok2 = true;
		var status = ResourceLoader.LoadThreadedGetStatus(target, progress);
		ResourceLoader.ThreadLoadStatus statuslesson = (ResourceLoader.ThreadLoadStatus)0;
		if(target == "res://Scenes/Lesson.tscn") statuslesson = ResourceLoader.LoadThreadedGetStatus("res://Courses/Lesson_" + _data.CurrentLesson + "/Lesson.tscn", progresslesson);
		if(status != (ResourceLoader.ThreadLoadStatus)0)
			switch(status)
			{
				case (ResourceLoader.ThreadLoadStatus)1:
				//In progress
				if(target != "res://Scenes/Lesson.tscn") GetNode<ProgressBar>("Bara").Value = ((float)progress[0]) * 100;
				else GetNode<ProgressBar>("Bara").Value = ((float)progress[0]) * 50;
				break;
			case (ResourceLoader.ThreadLoadStatus)3:
				//Incarcat
				ok1 =  true;
				break;
			case (ResourceLoader.ThreadLoadStatus)2:
				//Eroare
				GD.Print("Incarcare esuata: Eroare in timp ce se incarca");
				GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
				break;
			}
		if(statuslesson != (ResourceLoader.ThreadLoadStatus)0)
			switch(statuslesson)
			{
				case (ResourceLoader.ThreadLoadStatus)1:
				//In progress
				GetNode<ProgressBar>("Bara").Value = GetNode<ProgressBar>("Bara").Value + ((float)progresslesson[0]) * 50;
				break;
			case (ResourceLoader.ThreadLoadStatus)3:
				//Incarcat
				ok2 =  true;
				break;
			case (ResourceLoader.ThreadLoadStatus)2:
				//Eroare
				GD.Print("Incarcare esuata: Eroare in timp ce se incarca");
				GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
				break;
			}
		if(!done && ok1 == ok2 && ok1)
		{	GetNode<ProgressBar>("Bara").Value = 100;
			ChangeScene(100);
			done = true;
		}
	}
	private async void ChangeScene(float value)
	{	
		if(value == 100)
		{	var tween = GetTree().CreateTween();
			tween.TweenProperty(GetNode<ProgressBar>("Bara"), "value", 120, 1.5);
			await ToSignal(tween, Tween.SignalName.Finished);
			tween.Stop();
			tween = GetTree().CreateTween();
			tween.TweenProperty(GetNode<Node2D>("."), "modulate", new Color(1, 1, 1, 0), 0.5);
			await ToSignal(tween, Tween.SignalName.Finished);
			tween.Stop(); 
			//Dupa multe incercari am reusit sa nu mai am erori de genul "Object reference not set to an instance of an object" in aceasta functie
			//Era nevoie doar de un if
			var packed = (PackedScene)(ResourceLoader.LoadThreadedGet(target));
			if(packed != null) GetTree().ChangeSceneToPacked(packed);
		}
	}
}
