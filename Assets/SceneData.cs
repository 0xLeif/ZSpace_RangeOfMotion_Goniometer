using UnityEngine;
using System.Collections;

public class SceneData : MonoBehaviour {
	public static int mode;
	public ZSStylusSelector stylus;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(stylus.GetButtonUp(1)){
			if(mode < 5)
				mode++;
		}else if(stylus.GetButtonUp(2)){
			if(mode >0)
				mode--;
		}
	}
}
