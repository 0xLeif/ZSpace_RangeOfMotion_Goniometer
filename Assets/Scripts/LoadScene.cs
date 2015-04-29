using UnityEngine;
using System.Collections;

public class LoadScene : MonoBehaviour {
	public int sceneNum;

	// Use this for initialization
	void Start () {
		if(GameData.zSpace){
			Screen.showCursor = false;
			Screen.lockCursor = true;
		}
		switch(sceneNum){
			case 1:
			Camera.main.transform.position = !GameData.zSpace ? GameData.webCameraLocSlots:GameData.zSpaceCameraLocSlots;
				break;
			case 2:
			Camera.main.transform.position = !GameData.zSpace ? GameData.webCameraLocFlashCard:GameData.zSpaceCameraLocFlashCard;
				break;
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
