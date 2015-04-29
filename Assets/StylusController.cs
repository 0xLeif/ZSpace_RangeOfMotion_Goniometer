using UnityEngine;
using System.Collections;

public class StylusController : MonoBehaviour {
	public ZSStylusSelector stylus;
	// Use this for initialization
	void Start () {
		if(!GameData.zSpace){
			stylus = null;
			GameObject core = GameObject.FindGameObjectWithTag("zscore");
			Destroy(core);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
