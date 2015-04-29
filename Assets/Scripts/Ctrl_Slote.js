public var customGuiStyle : GUIStyle;
var ModelGUISkin : GUISkin;
function Start () {

}

function Update () {

}
function OnGUI () {
// Back to menu button
GUI.BeginGroup (Rect (Screen.width-165,Screen.height-50,300,180));
		if (GUI.Button (Rect (65,7.5,95,40), "Back to \n Menu")) {
			Application.LoadLevel (0);
		}
		GUI.EndGroup ();
} 
if (Input.GetKeyDown(KeyCode.F))
		Screen.fullScreen = !Screen.fullScreen;
if(Input.GetKeyDown(KeyCode.Escape)){
		if(!Screen.fullScreen){Application.Quit();}
		else
		Screen.fullScreen = !Screen.fullScreen;
	}
/**	
	GUI.skin = ModelGUISkin;
	GUI.Box (Rect(Screen.width/2-300,95,600,40),"Control Box",customGuiStyle);
	GUI.Box (Rect(Screen.width/2-300,95,600,350),""); //outer box
	
//*****Main Menu Functions ****
	if (GUI.Button (Rect (Screen.width/2-240,145,180,70), "Back to \n Menu")) {
		Application.LoadLevel (0);
	}
	**/