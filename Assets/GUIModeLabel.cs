using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GUIModeLabel : MonoBehaviour {
	private Text mode;

	// Use this for initialization
	void Start () {
		mode = GetComponent<Text> ();
	}
	
	// Update is called once per frame
	void Update () {
		mode.text = SceneData.mode + "";
	}
}
