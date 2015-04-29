using UnityEngine;
using System.Collections;

public class LeverController : MonoBehaviour {
	private bool resetting, firstTurn;
	private float oldPos;
	public BoxRotation slots;
	public ZSStylusSelector stylus;

	void Start () {
		resetting = false;
		firstTurn = true;
		oldPos = 0;
		if(!GameData.zSpace){
			stylus = null;
			GameObject core = GameObject.FindGameObjectWithTag("zscore");
			Destroy(core);
		}
	}

	void Update () {
		if(resetting){
			Reset();
		}else if(Input.GetMouseButton(0)){
			Checker(Input.GetAxis("Mouse Y")*5);
		}
		if(GameData.zSpace)
			if(stylus.GetButton(0)){
				float newPos = firstTurn ? 0 : stylus.transform.position.y;
				Checker((newPos - oldPos)*300);
				oldPos = firstTurn ? stylus.transform.position.y : newPos;
				firstTurn = false;
			}
	}

	private void Reset(){
		if(transform.rotation.x < 0){
			transform.Rotate(1, 0,0);
		}else{
			resetting = false;
			transform.rotation = new Quaternion(0,0,0,0);
		}
	}

	private void Checker(float value){
		if(transform.rotation.x <= 0 && transform.rotation.x >= -.7){
			transform.Rotate(value,0,0);
		}else if(transform.rotation.x < -.7){
			slots.RotateTrue();
			resetting = true;
		}else{
			transform.rotation = new Quaternion(0,0,0,0);
		}
	}
}
