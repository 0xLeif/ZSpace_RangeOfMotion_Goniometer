public var customGuiStyle : GUIStyle;
var widelogo : Texture2D;
var color: Color;
var ModelGUISkin : GUISkin;


function Start () {
}

function Update () {
}

function OnGUI () {
	
	GUI.skin = ModelGUISkin;
	GUI.Label (Rect (Screen.width/2-300,5,600,100),widelogo);
	GUI.Box (Rect(Screen.width/2-300,95,600,40),"Range Of Motion Minigames",customGuiStyle);
	GUI.Box (Rect(Screen.width/2-300,95,600,350),""); //outer box
	
//*****Main Menu Functions ****
	if (GUI.Button (Rect (Screen.width/2-240,145,180,70), "Slote Machine")) {
		Application.LoadLevel (1);
	}
	if (GUI.Button (Rect (Screen.width/2-240,235,180,70), "Flash Card")) {
		Application.LoadLevel (2);
	}
	if (GUI.Button (Rect (Screen.width/2-240,325,180,70), "Mini Game 2")) {
		Application.LoadLevel (1);
	}
	if (GUI.Button (Rect (Screen.width/2+60,145,180,70), "Mini Game 3 ")) {
		Application.LoadLevel (1);
	}	
	if (GUI.Button (Rect (Screen.width/2+60,235,180,70), "Mini Game 4")) {
		Application.LoadLevel (1);
	}
	if (GUI.Button (Rect (Screen.width/2+60,325,180,70), "Mini Game 4")) {
		Application.LoadLevel (1);
	}
} 
if (Input.GetKeyDown(KeyCode.F))
		Screen.fullScreen = !Screen.fullScreen;
if(Input.GetKeyDown(KeyCode.Escape)){
		if(!Screen.fullScreen){Application.Quit();}
		else
		Screen.fullScreen = !Screen.fullScreen;
	}