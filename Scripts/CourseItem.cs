//Folosit pentru lista de lectii
using Godot;
using System;

public partial class CourseItem : PanelContainer
{
	// Called when the node enters the scene tree for the first time.
	[Export] public int lesson = 0;		//Indexul lectiei
	[Export] public string lessonName = "";	//Numele lectiei
	[Export] public int complete = 0;	//Cat la suta a fost parcursa lectia
	[Export] public int lessonTag = 0;	//0: Normal; 1: Avansat; 2: Special; 3: Avansat+Special
}
