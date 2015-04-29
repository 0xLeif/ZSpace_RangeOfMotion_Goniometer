using UnityEngine;
using System.Collections;

public class BoxRotation : MonoBehaviour {
	public GameObject one,two,three;
	private float speed, count;
	private bool rotate;

	void Start () {
		speed = 1f;
		count = 0;
		rotate = false;
	}

	void Update () {
		if(rotate){
			RotateBoxes();
		}
	}

	public void RotateTrue(){
		rotate = true;
	}


	private void RotateBoxes(){
		if(count <= 550){
			one.transform.Rotate(new Vector3(-1,0,0),speed,0);
			two.transform.Rotate(new Vector3(-1,0,0),speed,0);
			three.transform.Rotate(new Vector3(-1,0,0),speed,0);
			count++;
			if(count % 2 == 0){
				speed+=.05f;
			}
			if(count >= 350){
				one.transform.rotation = new Quaternion(0,90,0,0);
			}
			if(count >= 450){
				two.transform.rotation = new Quaternion(0,90,0,0);
			}
		}else{
			rotate = false;
			three.transform.rotation = new Quaternion(0,90,0,0);
		}
	}

}
