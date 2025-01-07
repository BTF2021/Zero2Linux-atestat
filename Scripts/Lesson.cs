//Folosit pentru lectii
using Godot;
using System;

public partial class Lesson : Node2D
{
	private DefaultData _data;
	public Node _node;	//Containerul in care sunt puse elementele lectiei (text, intrebari,...)
	public Godot.Collections.Array<Quizitem> _questions;	//Vector cu toate intrebarile din lectie
	public int lessonid = 0;
	private int totalquestioncount;		//Nr total de intrebari din lectie
	private int totallessonblocks;		//Nr total de blocuri din lectie (Un bloc poate contine text, imagini, separatoare etc). Separatoarele se pot pune si intre blocuri si intrebari
	private int questionansw;		//Nr de intrebari deja raspunse
	private int blocksread;			//Nr de blocuri deja vazute
	private int percent;		//Procentul progresului
	[Signal] public delegate void GetAnswersEventHandler(bool correct, bool ignore, int index);	//Semnal pentru intrebarile din lectie

	public async void _Treeentered() => _Ready();
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		_data = (DefaultData)GetNode("/root/DefaultData");
		lessonid = _data.CurrentLesson;
		GetNode<Control>("Panel/ScrollContainer/MarginContainer/Body").AddChild((GD.Load<PackedScene>("res://Courses/Lesson_" + lessonid + "/Lesson.tscn")).Instantiate());	//Lectia propriu-zisa
		_questions = new Godot.Collections.Array<Quizitem>{};
		_node = GetNode<VBoxContainer>("Panel/ScrollContainer/MarginContainer/Body/Content");
		GetAnswers += SendAnswers;	//Conecteaza semnalul la functie

		//Daca nu putem reda videoclipurile, ramane doar imaginea
		if(!ResourceLoader.Exists("res://Courses/Lesson_" + lessonid + "/VidBg.png"))
		{	GetNode<HSeparator>("Panel/ScrollContainer/MarginContainer/Body/HSeparator2").QueueFree();
			GetNode<ColorRect>("Panel/ScrollContainer/MarginContainer/Body/VideoPreview/Spoiler").QueueFree();
			GetNode<Sprite2D>("Panel/ScrollContainer/MarginContainer/Body/VideoPreview/textureRect").QueueFree();
			GetNode<TextureRect>("Panel/ScrollContainer/MarginContainer/Body/VideoPreview").QueueFree();
		}
		else if(!ResourceLoader.Exists("res://Courses/Lesson_" + lessonid + "/Video.webm") || !_data.isvideoavailable) GetNode<Sprite2D>("Panel/ScrollContainer/MarginContainer/Body/VideoPreview/textureRect").QueueFree();
		GetNode<TextureRect>("Panel/ScrollContainer/MarginContainer/Body/VideoPreview").Texture = GD.Load<CompressedTexture2D>("res://Courses/Lesson_" + lessonid + "/VidBg.png");

		//Titlu si rearanjarea elementelor
		GetNode<Label>("Panel/ScrollContainer/MarginContainer/Body/Title").Text = "";
		if((int)_data.lessonList[lessonid][1] == 1) 
		{
			GetNode<Label>("Panel/ScrollContainer/MarginContainer/Body/Title").Text = "Avansat: ";
			GetNode<RichTextLabel>("Panel/ScrollContainer/MarginContainer/Body/Nota").Visible = true;
			GetNode<RichTextLabel>("Panel/ScrollContainer/MarginContainer/Body/Nota").Text = "[center][color=ffffff99][i]Aceasta este o lectie optionala. Daca este activat din setari, intrebarile pot aparea si in chestionare si teste[/i][/color][/center]";
			GetNode<CanvasItem>("Panel/ScrollContainer/MarginContainer/Body/HSeparator3").Visible = true;
		}
		else if((int)_data.lessonList[lessonid][1] == 2) 
		{
			GetNode<Label>("Panel/ScrollContainer/MarginContainer/Body/Title").Text = "Special: ";
			GetNode<RichTextLabel>("Panel/ScrollContainer/MarginContainer/Body/Nota").Visible = true;
			GetNode<RichTextLabel>("Panel/ScrollContainer/MarginContainer/Body/Nota").Text = "[center][color=ffffff99][i]Aceasta este o lectie optionala. Nu este numarata in lectii terminate si nu vor fi intrebari in chestionare si teste[/i][/color][/center]";
			GetNode<CanvasItem>("Panel/ScrollContainer/MarginContainer/Body/HSeparator3").Visible = true;
		}
		GetNode<Label>("Panel/ScrollContainer/MarginContainer/Body/Title").Text = GetNode<Label>("Panel/ScrollContainer/MarginContainer/Body/Title").Text + (string)_data.lessonList[lessonid][0];
		_node.GetParent().MoveChild(GetNode("Panel/ScrollContainer/MarginContainer/Body/HSeparator4"), -1);
		_node.GetParent().MoveChild(GetNode("Panel/ScrollContainer/MarginContainer/Body/End"), -1);

		//Conecteaza toate RichTextLabelurile cu functia de deschidere a linkurilor
		for (int i = 0; i <= GetNode("Panel/ScrollContainer/MarginContainer/Body/Content").GetChildCount()-1; i++)
		{	if(GetNode("Panel/ScrollContainer/MarginContainer/Body/Content").GetChild(i).Name.ToString().Contains("Block"))
			{	var name = GetNode("Panel/ScrollContainer/MarginContainer/Body/Content").GetChild(i).Name.ToString();
				for (int j = 0; j <= GetNode("Panel/ScrollContainer/MarginContainer/Body/Content/" + name).GetChildCount()-1; j++)
				{	if(GetNode("Panel/ScrollContainer/MarginContainer/Body/Content/" + name).GetChild(j).Name.ToString().Contains("RichTextLabel"))	
						GetNode("Panel/ScrollContainer/MarginContainer/Body/Content/" + name).GetChild<RichTextLabel>(j).MetaClicked += _on_text_link;
				}
			}
		}
		
		//Calculeaza nr de intrebari si blocuri
		percent = (int)_data.currentStats.LessonCompletion[lessonid];
		totalquestioncount = 0;
		totallessonblocks = 0;
		for (int i = 0; i <= _node.GetChildCount()-1; i++)
		{	if(_node.GetChild(i).Name.ToString().Contains("Quizitem"))
			{	_node.GetChild<Quizitem>(i).Index = i;
				_node.GetChild<Quizitem>(i).Complete = false;
				_questions.Add((Quizitem)_node.GetChild<Quizitem>(i));
				totalquestioncount++;
			}
			else if(_node.GetChild(i).Name.ToString().Contains("Block")) totallessonblocks++;
		}

		//Aici aratam lectia in fuctie de progresul salvat (daca este sub 100%)
		//Cred ca se putea si mai bine
		if(_data.currentStats.LessonCompletion[lessonid] < 100)
		{
			//Luam tot ce este in Content si adaugam nr de intrebari si blocuri si calculam progresul
			//Pana cand acesta este egal cu progresul salvat (Daca nu a fost modificat save.json folosind surse externe)
			GD.Print("Intainte de calcule: " + questionansw + "/" + totalquestioncount + " " + blocksread + "/" + totallessonblocks + " " + (questionansw + blocksread) * 100 / (totalquestioncount + totallessonblocks));
			for (int i = 0; i <= _node.GetChildCount()-1; i++)
			{	var calc = (questionansw + blocksread) * 100 / (totalquestioncount + totallessonblocks);
				//Daca am ajuns la progresul din save.json
				if(calc == _data.currentStats.LessonCompletion[lessonid])
				{
					ShowObjects(i);
					break;
				}
				GD.Print(questionansw + "/" + totalquestioncount + " " + blocksread + "/" + totallessonblocks + " " + (questionansw + blocksread) * 100 / (totalquestioncount + totallessonblocks));
				//Altfel continuam
				if(_node.GetChild(i).Name.ToString().Contains("Quizitem")) 
				{	_node.GetChild<Quizitem>(i).Complete = true;
					questionansw++;
				}
				else if(_node.GetChild(i).Name.ToString().Contains("Block")) blocksread++;
			}
		}
		//100%
		//Mai mult ca sa nu trecem prin toate calculele de mai sus
		else if (_data.currentStats.LessonCompletion[lessonid] == 100)
		{
			for (int i = 0; i <= _node.GetChildCount()-1; i++)
				if(_node.GetChild(i).Name.ToString().Contains("Quizitem")) 
					_node.GetChild<Quizitem>(i).Complete = true;     //Daca intrebarile nu sunt setate la true, se poate suprascrie progresul prin raspunderea corecta a acestora si se strica totul
			_node.GetParent().GetChild<CanvasItem>(_node.GetParent().GetChildCount()-2).Visible = true;
			_node.GetParent().GetChild<CanvasItem>(_node.GetParent().GetChildCount()-1).Visible = true;
		}

		//Tranzitie
		if(_data.currentStats.Anims)
		{	GetNode<Panel>("Panel").Hide();
			GetNode<TextureButton>("Back").Hide();
			GetNode<Node2D>("Transition").Show();
			GetNode<Label>("Transition/Title").Text = GetNode<Label>("Panel/ScrollContainer/MarginContainer/Body/Title").Text;
			var tween = GetTree().CreateTween();
			var pos = Position;
			pos.X = 0;
			pos.Y = 296 - 10;
			GetNode<Label>("Transition/Title").Modulate = new Color(1, 1, 1, 0);
			GetNode<Label>("Transition/Title").Position = pos;
			pos.Y = 296;
			tween.TweenProperty(GetNode<Label>("Transition/Title"), "modulate", new Color(1, 1, 1, 1), 0.25);
			tween.Parallel().TweenProperty(GetNode<Label>("Transition/Title"), "position", pos, 0.25);
			await ToSignal(tween, Tween.SignalName.Finished);
			var timer = GetTree().CreateTimer(1);
			await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
			GD.Print("Partea a doua");
			tween.Stop();
			var scale = Scale;
			scale.X = (float)0.72;
			scale.Y = (float)0.72;
			var size = Position;
			size.X = 1210;
			size.Y = 96;
			tween = GetTree().CreateTween();
			pos.X = -173 - 10;
			pos.Y = -1;
			GetNode<ScrollContainer>("Panel/ScrollContainer").VerticalScrollMode = (ScrollContainer.ScrollMode)3;
			//Daca se seteaza la Disabled ((ScrollContainer.ScrollMode)0), cand se reactiveaza, nu mai poti da scroll
			//Probabil nu se activeaza cum trebuie
			GetNode<ScrollContainer>("Panel/ScrollContainer").Position = pos;
			pos.X = 190;
			pos.Y = -1;
			GetNode<Panel>("Panel").Show();
			GetNode<TextureButton>("Back").Show();
			GetNode<Panel>("Panel").Modulate = new Color(1, 1, 1, 0);
			GetNode<TextureButton>("Back").Modulate = new Color(1, 1, 1, 0);
			GetNode<Label>("Panel/ScrollContainer/MarginContainer/Body/Title").SelfModulate = new Color(1, 1, 1, 0);
			tween.TweenProperty(GetNode<Label>("Transition/Title"), "position", pos, 0.75).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			tween.Parallel().TweenProperty(GetNode<Label>("Transition/Title"), "scale", scale, 0.75).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			tween.Parallel().TweenProperty(GetNode<Label>("Transition/Title"), "size", size, 0.75).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			tween.Parallel().TweenProperty(GetNode<Panel>("Panel"), "modulate", new Color(1, 1, 1, 1), 0.75);
			tween.Parallel().TweenProperty(GetNode<TextureButton>("Back"), "modulate", new Color(1, 1, 1, 1), 0.75);
			timer = GetTree().CreateTimer(0.75);
			await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
			tween.Stop();
			pos.X = -173;
			pos.Y = -1;
			GetNode<ScrollContainer>("Panel/ScrollContainer").VerticalScrollMode = (ScrollContainer.ScrollMode)1;
			GetNode<ScrollContainer>("Panel/ScrollContainer").Position = pos;
			GetNode<Label>("Panel/ScrollContainer/MarginContainer/Body/Title").SelfModulate = new Color(1, 1, 1, 1);
			GetNode<Label>("Transition/Title").Hide();
			GetNode<Node2D>("Transition").QueueFree();
		}
		else	GetNode<Node2D>("Transition").Hide();
	}

	/*Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	*/

	//Aceasta functie arata partile din lectie pana la o intrebare la care nu sa raspuns/ nu sa raspuns corect
	private async void ShowObjects(int index)
	{	var foundquestion = false;
		for (int i = index; i <= _node.GetChildCount()-1; i++)
		{	if(foundquestion) _node.GetChild<CanvasItem>(i).Visible = false;
			else 
			{
				if(_node.GetChild(i).Name.ToString().Contains("Block")) blocksread++;
				_node.GetChild<CanvasItem>(i).Visible = true;
			}
			if(_node.GetChild(i).Name.ToString().Contains("Quizitem")) foundquestion = true;
		}
		if (!foundquestion)
		{
			_node.GetParent().GetChild<CanvasItem>(_node.GetParent().GetChildCount()-2).Visible = true;
			_node.GetParent().GetChild<CanvasItem>(_node.GetParent().GetChildCount()-1).Visible = true;
		}
	}
	//Functie pentru intrebari
	public void SendAnswers(bool correct, bool ignore, int index)
	{
		if(correct)
		{	//Daca nu a fost deja raspuns
			if(!_node.GetChild<Quizitem>(index).Complete)
			{
				ShowObjects(index+1);
				_node.GetChild<Quizitem>(index).Complete = true;

				//Calculam cat la suta din lectie a fost citita si-l salvam progresul
				questionansw = 0;
				for (int i = 0; i <= _questions.Count-1; i++)
				{
					if((bool)_questions[i].Complete)questionansw++;
				}
				GD.Print(questionansw + "/" + totalquestioncount + " " + blocksread + "/" + totallessonblocks + " " + ((questionansw + blocksread) * 100 / (totalquestioncount + totallessonblocks)));
				_data.currentStats.LessonCompletion[lessonid] = (questionansw + blocksread) * 100 / (totalquestioncount + totallessonblocks);
				_data.WriteSave(_data.LoggedUser);
			}
			//Altfel, facem intrebarea verde pentru un moment
			else
			{	var tween = GetTree().CreateTween();
				_node.GetChild<CanvasItem>(index).Modulate = new Color((float)0.05, 1, (float)0.05, 1);  //Culoare verde
				tween.TweenProperty(_node.GetChild<CanvasItem>(index), "modulate", new Color(1, 1, 1, 1), 0.5);
			}
		}
		else 
		{	var tween = GetTree().CreateTween();
			_node.GetChild<CanvasItem>(index).Modulate = new Color(1, (float)0.05, (float)0.05, 1);      //Culoare rosie
			tween.TweenProperty(_node.GetChild<CanvasItem>(index), "modulate", new Color(1, 1, 1, 1), 0.5);
		}
	}
	//Butonul pentru intoarcerea in meniul principal
	private void _on_back_pressed()
	{	GD.Print("Pressed");
		GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
	}
	//Cand apesi pe play
	private void _on_watch_pressed()
	{	var _video = (ResourceLoader.Load<PackedScene>("res://Scenes/VideoOverlay.tscn")).Instantiate();
		_video.GetNode<VideoStreamPlayer>("Panel/VideoStreamPlayer").Stream.File = "res://Courses/Lesson_" + lessonid + "/Video.webm";	//Incarcam videoclipul dorit
		_video.GetNode<Sprite2D>("Bg").Texture = GD.Load<CompressedTexture2D>("res://Courses/Lesson_" + lessonid + "/VidBg.png");	//Fundalul de dinainte sa dai play (VideoOverlay)
		AddChild(_video);
	}
	//Pentru linkurile din text
	private void _on_text_link(Variant meta)
	{	//Creeaza o fereastra de confirmare
		var scene = (Confirm)GD.Load<PackedScene>("res://Scenes/Confirm.tscn").Instantiate();
		scene.reason = 2;
		scene.link = (string)meta;
		AddChild(scene);
	}
}
