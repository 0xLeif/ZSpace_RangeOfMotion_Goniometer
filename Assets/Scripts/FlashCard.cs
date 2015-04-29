using UnityEngine;
using System.Collections;

public class FlashCard : MonoBehaviour {
	public ZSStylusSelector stylus;
	private bool askingQuestion;

	// Use this for initialization
	void Start () {
		if(!GameData.zSpace){
			stylus = null;
			GameObject core = GameObject.FindGameObjectWithTag("zscore");
			Destroy(core);
		}
		askingQuestion = false;
	}
	
	// Update is called once per frame
	void Update () {
		if(!askingQuestion){
			askQuestion();
		}else{
		if (stylus == null) {
			if (Input.GetMouseButtonUp (0)) {
					Vector3 test = Camera.main.ScreenToWorldPoint(Input.mousePosition);
					print(test.x);
					print(test.y);
					print(test.z);
			}
		} else {
			if (stylus.GetButtonUp (0)) {
				print ("Stylus");
			}
		}
		}
		
	}

	private bool finished = false;
	private int count = 0;
	void askQuestion(){
		if(stylus == null){
			if(transform.position.x <= GameData.webLocFlashCardAnimationFinal.x){
				transform.Translate(1*Time.deltaTime,0,0);
				finished = false;
			}else{
				finished = true;
			}
			if(transform.position.y <= GameData.webLocFlashCardAnimationFinal.y){
				transform.Translate(0,1*Time.deltaTime,0);
				finished = false;
			}else{
				finished = true;
			}
			if(transform.position.z >= GameData.webLocFlashCardAnimationFinal.z){
				transform.Translate(0,0,-1*Time.deltaTime);
				finished = false;
			}else{
				finished = true;
			}
			if(count != GameData.flashCardAnimationFinalRoation.x){
				transform.rotation = Quaternion.Euler(count--,0,0);
				finished = false;
			}else{
				finished = true;
			}
		}else{
			if(transform.position.x <= GameData.zSpaceLocFlashCardAnimationFinal.x){
				transform.Translate(1*Time.deltaTime,0,0);
				finished = false;
			}else{
				finished = true;
			}
			if(transform.position.y <= GameData.zSpaceLocFlashCardAnimationFinal.y){
				transform.Translate(0,1*Time.deltaTime,0);
				finished = false;
			}else{
				finished = true;
			}
			if(transform.position.z >= GameData.zSpaceLocFlashCardAnimationFinal.z){
				transform.Translate(0,0,-1*Time.deltaTime);
				finished = false;
			}else{
				finished = true;
			}
			if(count != GameData.flashCardAnimationFinalRoation.x){
				transform.rotation = Quaternion.Euler(count--,0,0);
				finished = false;
			}else{
				finished = true;
			}
		}
		if(finished)
			askingQuestion = true;
	}
}
