using UnityEngine;
using System.Collections;

public class GameData : MonoBehaviour {
	public bool has_zSpace;
	public static Vector3 
		zSpaceCameraLocSlots = new Vector3 (6, 8, -27),
		webCameraLocSlots = new Vector3 (6, 5, -13), 
		webCameraLocFlashCard = new Vector3 (7.3f,3.8f,-10),
		zSpaceCameraLocFlashCard = new Vector3 (7.3f,5.3f,-16.7f),
		webLocFlashCardAnimationFinal = new Vector3(7.4f,3.5f,-9),
		zSpaceLocFlashCardAnimationFinal = new Vector3(7.3f,4.7f,-15),
		flashCardAnimationFinalRoation = new Vector3(-70,0,0);
	public static float framePositionZ = 5.675756f;
	public static bool zSpace;

	void Start () {
		zSpace = has_zSpace;
	}
}
