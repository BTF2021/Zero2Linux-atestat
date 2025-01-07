//Pentru meniul de logare
using Godot;
using System;
using Newtonsoft.Json;                           //Pentru culoarea cercului (in AddProfiles())

public partial class Logare : Node2D
{
	private DefaultData _data;
	private Button _profile;	//Iconul pentru user
	private System.Array _names;         //Vector pentru numele profilelor
	private int ListLenght;		//Lungimea listei de utilizatori
	private int Index;
	private bool profilespresent;        //Daca exista profile
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	_data = (DefaultData)GetNode("/root/DefaultData");
		_profile = GetNode<Button>("Profiles/Square/Vlist/List/>,,<");	//butonul-model are un nume considerat de aplicatie invalid
		_names = new string[101];	//vectori pentru numele utilizatorilor
		GetNode<Label>("Bg/Version").Text = "Zero2Linux Ver " + (String)ProjectSettings.GetSetting("application/config/version");
		GetNode<CanvasItem>("/root/Transition").Show(); //A se vedea funtia logging
		CheckUsers();
		GetNode<Control>("Profiles").Visible = false;
		GetNode<Control>("NoUser").Visible = false;
		GetNode<Control>("Create").Visible = false;
		//Daca nu sunt utilizatori existenti, atunci trimite-i la ecranul de bun venit. Altfel, trimite-i la lista de utilizatori
		if(!profilespresent)
		{
			GetNode<Control>("Profiles").Visible = false;
			GetNode<Control>("NoUser").Visible = true;

			var pos = Position;
			pos.X = 0;
			pos.Y = 78 - 30;
			GetNode<Control>("NoUser").Position = pos;
			pos.Y = 78;
			GetNode<Control>("NoUser").SelfModulate = new Color(1, 1, 1, 0);
			var tween = GetTree().CreateTween();
			tween.TweenProperty(GetNode<Control>("NoUser"), "modulate", new Color(1, 1, 1, 1), 0.25);
			tween.Parallel().TweenProperty(GetNode<Control>("NoUser"), "position", pos, 0.25);
		}
		else
		{	GetNode<Control>("Profiles").Visible = true;
			var pos = Position;
			pos.X = 0;
			pos.Y = 254 - 30;
			GetNode<Control>("Profiles").Position = pos;
			pos.Y = 254;
			GetNode<Control>("Profiles").Modulate = new Color(1, 1, 1, 0);
			var tween = GetTree().CreateTween();
			tween.TweenProperty(GetNode<Control>("Profiles"), "modulate", new Color(1, 1, 1, 1), 0.4);
			tween.Parallel().TweenProperty(GetNode<Control>("Profiles"), "position", pos, 0.4);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	GetNode<Label>("Bg/Time").Text = Time.GetTimeStringFromSystem();	//Ceasul
		if(GetNode<Control>("Create").Visible)	//Pentru previzualizarea iconitei de utilizator
		{	GetNode<Label>("Create/Preview/Name").Text = GetNode<LineEdit>("Create/Nume/Nume").Text;
			GetNode<Label>("Create/Preview/BigLetter").Text = GetNode<LineEdit>("Create/Nume/Nume").Text;
			GetNode<Sprite2D>("Create/Preview/Bg").SelfModulate = GetNode<ColorPicker>("Create/Culoare/Panel/CuloarePicker").Color;
		}
	}
	public override void _Notification(int what)
	{	//Daca dai inapoi pe Android
		if (what == NotificationWMGoBackRequest && GetNode<Control>("Profiles").Visible == true)
        	GetTree().Quit();
	}
	private void _on_nume_text_changed(string new_text)
	{	//Aici este partea unde eliminam caracterele interzise din nume
		if(new_text.IndexOf(" ") >= 0) new_text = new_text.Remove(new_text.IndexOf(" "));
		if(new_text.IndexOf("/") >= 0) new_text = new_text.Remove(new_text.IndexOf("/"));
		if(new_text.IndexOf(".") >= 0) new_text = new_text.Remove(new_text.IndexOf("."));
		if(new_text.IndexOf(":") >= 0) new_text = new_text.Remove(new_text.IndexOf(":"));
		if(new_text.IndexOf(",") >= 0) new_text = new_text.Remove(new_text.IndexOf(","));
		if(new_text.IndexOf("@") >= 0) new_text = new_text.Remove(new_text.IndexOf("@"));
		if(new_text.IndexOf("'") >= 0) new_text = new_text.Remove(new_text.IndexOf("'"));
		if(new_text.IndexOf("%") >= 0) new_text = new_text.Remove(new_text.IndexOf("%"));
		if(new_text.IndexOf('"') >= 0) new_text = new_text.Remove(new_text.IndexOf('"'));
		var caret = GetNode<LineEdit>("Create/Nume/Nume").CaretColumn;	//Salveaza pozitia caretului inainte de a salva textul
		GetNode<LineEdit>("Create/Nume/Nume").Text = new_text;
		GetNode<LineEdit>("Create/Nume/Nume").CaretColumn = caret;
	}
	private void _on_create_pressed()
	{
		if(GetNode<LineEdit>("Create/Nume/Nume").Text.Length <= 0) 	//Daca numele nu exista
		{	GD.Print("Nu se poate creea utilizator: Nu exista nume");
			GetNode<Label>("Create/Eroare").Show();
			GetNode<Label>("Create/Eroare").Text = "Nu se poate creea utilizator: Nu exista nume";
			return;
		}
		else
		{	var ok = true;
			var names = _data.GetSaves();
			//Verificam daca deja exista nume
			if(names != null)
				for (int i = 0; i< names.Length; i++) if(GetNode<LineEdit>("Create/Nume/Nume").Text == (string)names.GetValue(i)) ok = false;
			GD.Print(ok);
			if(ok)
			{	//Aici este partea unde cream un fisier folosind numele si culoarea deja date de utilizator
				_data.currentStats.UsrName = GetNode<LineEdit>("Create/Nume/Nume").Text;
				_data.currentStats.FavColor = GetNode<ColorPicker>("Create/Culoare/Panel/CuloarePicker").Color;
				var file = FileAccess.Open("user://" + _data.currentStats.UsrName +"_save.json", FileAccess.ModeFlags.Write);
				if (file == null) GD.Print("Nu se poate deschide fisierul. Eroare: " + FileAccess.GetOpenError());
				file.StoreString(JsonConvert.SerializeObject(_data.currentStats));
				file.Close();
				GetNode<LineEdit>("Create/Nume/Nume").Text = "";
				GetNode<ColorPicker>("Create/Culoare/Panel/CuloarePicker").Color = new Color(1, 1, 1, 1);
				_data.currentStats = new stats();
				GetNode<Button>("Create/Back").Disabled = true;
				GetNode<Control>("Create").Visible = false;
				GetNode<Control>("Profiles").Visible = true;
				
				CheckUsers();

				var pos = Position;
				pos.X = 0;
				pos.Y = 254 - 30;
				GetNode<Control>("Profiles").Position = pos;
				pos.Y = 254;
				GetNode<Control>("Profiles").Modulate = new Color(1, 1, 1, 0);
				var tween = GetTree().CreateTween();
				tween.TweenProperty(GetNode<Control>("Profiles"), "modulate", new Color(1, 1, 1, 1), 0.25);
				tween.Parallel().TweenProperty(GetNode<Control>("Profiles"), "position", pos, 0.25);
			}
			else
			{	GD.Print("Nu se poate creea utilizator: Deja exista un utilizator cu acel nume");
				GetNode<Label>("Create/Eroare").Show();
				GetNode<Label>("Create/Eroare").Text = "Nu se poate creea utilizator: Deja exista un utilizator cu acel nume";
				return;
			}
		}
	}
	//Butonu "Continua ->" din meniul de bun venit
	private void _on_intro_pressed()
	{	GetNode<Control>("NoUser").Visible = false;
		GetNode<Control>("Create").Visible = true;

		GetNode<Button>("Create/Back").Disabled = true;
		GetNode<Button>("Create/Back").Hide();
		var pos = Position;
		pos.X = 0 + 30;
		pos.Y = 87;
		GetNode<Control>("Create").Position = pos;
		pos.X = 0;
		GetNode<Control>("Create").Modulate = new Color(1, 1, 1, 0);
		var tween = GetTree().CreateTween();
		GetNode<Button>("Create/Back").Disabled = true;
		GetNode<Button>("Create/Back").Hide();
		GetNode<Label>("Create/Eroare").Hide();
		tween.TweenProperty(GetNode<Control>("Create"), "modulate", new Color(1, 1, 1, 1), 0.25);
		tween.Parallel().TweenProperty(GetNode<Control>("Create"), "position", pos, 0.25);
	}
	//Butonul pentru creearea unui nou utilizator. Diferenta dintre cele doua este ca, in prima functie, nu se poate da inapoi la ecranul de bun venit
	private void _on_createuser_pressed()
	{	GetNode<Control>("Profiles").Visible = false;
		GetNode<Control>("Create").Visible = true;

		GetNode<Button>("Create/Back").Disabled = false;
		GetNode<Button>("Create/Back").Show();
		GetNode<Label>("Create/Eroare").Hide();
		var pos = Position;
		pos.X = 0 + 30;
		pos.Y = 87;
		GetNode<Control>("Create").Position = pos;
		pos.X = 0;
		GetNode<Control>("Create").Modulate = new Color(1, 1, 1, 0);
		var tween = GetTree().CreateTween();
		tween.TweenProperty(GetNode<Control>("Create"), "modulate", new Color(1, 1, 1, 1), 0.25);
		tween.Parallel().TweenProperty(GetNode<Control>("Create"), "position", pos, 0.25);
	}
	//Butonul de iesire din meniul de creeare a utilizatorului
	private void _on_back_pressed()
	{	GetNode<Control>("Create").Visible = false;
		GetNode<Control>("Profiles").Visible = true;

		var pos = Position;
		pos.X = 0;
		pos.Y = 254 - 30;
		GetNode<Control>("Profiles").Position = pos;
		pos.Y = 254;
		GetNode<Control>("Profiles").Modulate = new Color(1, 1, 1, 0);
		var tween = GetTree().CreateTween();
		tween.TweenProperty(GetNode<Control>("Profiles"), "modulate", new Color(1, 1, 1, 1), 0.25);
		tween.Parallel().TweenProperty(GetNode<Control>("Profiles"), "position", pos, 0.25);
		CheckUsers();
	}
	//Pur si simplu pentru refacerea listei de utilizatori in meniu
	private void CheckUsers()
	{	//Stergem toate iconitele care nu sunt model
		for(int i = 0; i< GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChildCount(); i++) 
		{	for(int j = 0; j< GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChild(i).GetChildCount(); j++)
			{	if(GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChild(i).GetChild<Node>(j).Name != (StringName)">,,<")
					GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChild(i).GetChild<Node>(j).QueueFree();
			}
		}
		if(!_data.SaveExists()) profilespresent = false;
		else
		{	ListLenght = _data.GetSaves().Length;
			profilespresent = true;
			if(ListLenght >= 13)GetNode<Panel>("Profiles/Panel").Visible = true;	//Daca lista trebuie afisata pe mai mult de o linie
			else GetNode<Panel>("Profiles/Panel").Visible = false;
			System.Array.Copy(_data.GetSaves(), 0, _names, 0, ListLenght);
			if(ListLenght == 100) 
			{	if(_names.GetValue(99) != null)
				{	GetNode<Button>("Profiles/Create").Disabled = true;
					GetNode<Button>("Profiles/Create").TooltipText = "Nu se pot creea noi utilizatori.\nVa rugam sa stergeti un utilizator pentru a creea altul";
				}
				else GetNode<Button>("Profiles/Create").TooltipText = "Creeaza un utilizator nou";
			}
			else GetNode<Button>("Profiles/Create").TooltipText = "Creeaza un utilizator nou";
			//Adaugam iconitele utilizatorilor
			for(int i = 0; i< _names.Length; i++) 
				if(_names.GetValue(i) != null) AddProfiles(i);
		}
	}
	//Functia pentru logare
	private void logging(string name)
	{	_data.LoggedUser = name;
		GD.Print("Logat in: " + name);
		
		//Dezactiveaza toate butoanele
		for(int i = 0; i< GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChildCount(); i++) 
			for(int j = 0; j< GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChild(i).GetChildCount(); j++)
				GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChild(i).GetChild<Button>(j).Disabled = true;
		
		var pos = Position;
		pos.X = 0;
		pos.Y = 254;
		GetNode<Control>("Profiles").Position = pos;
		pos.Y = 254 - 30;
		GetNode<Control>("Profiles").Modulate = new Color(1, 1, 1, 1);
		var tween = GetTree().CreateTween();
		tween.TweenProperty(GetNode<Control>("Profiles"), "modulate", new Color(1, 1, 1, 0), 0.3);
		tween.Parallel().TweenProperty(GetNode<Control>("Profiles"), "position", pos, 0.3);
		var pos1 = GetNode<Label>("Bg/Time").Position;
		pos1.Y = pos1.Y - 30;
		tween.Parallel().TweenProperty(GetNode<Label>("Bg/Time"), "position", pos1, 0.3);
		tween.Parallel().TweenProperty(GetNode<Label>("Bg/Time"), "modulate", new Color(1, 1, 1, 0), 0.3);
		//Aceasta instructiune este motivul pentru care exista o alta scena in Autoload
		//In timpul in care Godot a eliberat din memorie aceasta scena si incarca scena Main, o sa apara gri
		//De aceea aratam scena Transition in _Ready()
		tween.Finished += () => GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
	}
	
	//Functie pentru adaugarea profilelor existente
	private void AddProfiles(int index)
	{	//Facem iconita
		Index = index;
		var button = (Button)_profile.Duplicate(8);
		var file = FileAccess.Open("user://" + (string)_names.GetValue(index) + "_save.json", FileAccess.ModeFlags.Read);
		if (file == null) GD.Print("Nu se poate deschide fisierul. Eroare: " + FileAccess.GetOpenError());
		stats content = JsonConvert.DeserializeObject<stats>(file.GetAsText());
		file.Close();
		button.Name = content.UsrName;
		button.TooltipText = button.TooltipText + content.UsrName;
		button.GetNode<Label>("Name").Text = content.UsrName;
		button.GetNode<Label>("BigLetter").Text = content.UsrName;
		button.GetNode<Sprite2D>("Bg").SelfModulate = content.FavColor;
		button.Show();
		//Aici este partea unde plasam iconita pe un anumit rand
		if(index % 6 == 0 && index > 0 && GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChildCount() != (index / 6) + 1)
		{	//Duplicam randul si-l punem ca child in VList, dar stergem iconitele din acea lista
			Node list = GetNode<HBoxContainer>("Profiles/Square/Vlist/List").Duplicate(8);
			int count = list.GetChildCount();
			for(int i = 0; i < count; i++) 
				list.GetChild(i).QueueFree();
			GetNode<VBoxContainer>("Profiles/Square/Vlist").AddChild(list);
		}
		GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChild((index / 6)).AddChild(button);
		GD.Print("Adaugat " + content.UsrName + " cu indexul " + index + " in " + GetNode<VBoxContainer>("Profiles/Square/Vlist").GetChild((index / 6)).Name);
		button.Pressed += () => logging(content.UsrName);	//Conectam functia logging ca sa stim cine se logheaza
	}
}
