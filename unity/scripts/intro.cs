using UnityEngine;
using System.Collections;

public class intro : MonoBehaviour {
	Vector3 startCamera = new Vector3(0, 0.47f, -6.5f);
	Vector3 startCameraRotation = new Vector3(0,0,360.0f);
	Vector3 endCamera = new Vector3(5.2f, 2.36f, -0.13f);
	float journeyLength;
	float speed = 5.0f;
	float startTime;
	bool done = false;
	bool start = false;
	float lightIntense;
	GameObject introLight;
	// Use this for initialization
	void Start () {
		this.journeyLength = Vector3.Distance(startCamera, endCamera);
		introLight = GameObject.Find ("introLight");
		lightIntense = introLight.light.intensity;
		Camera.main.transform.position = startCamera;
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown("space")){
			startTime = Time.time;
			Debug.Log ("got space, start");
			start=true;
		}
		if(done || !start)
			return;
		float now = Time.time;
		float distCovered = (now - startTime) * this.speed;
		float fracJourney = distCovered / journeyLength;
		Debug.Log ("update fracj is " + fracJourney);
		if(fracJourney < 1.0){
			//Debug.Log ("speed "+speed+" dist covered is "+distCovered+" len is "+journeyLength+" now is " + Time.time + " frac is " + fracJourney);
			Camera.main.transform.position = Vector3.Lerp(this.startCamera, this.endCamera, fracJourney);
			if(fracJourney > 0.5){
				introLight.light.intensity = (float)(lightIntense * (1.0-fracJourney));
			}
		}else{
			Application.LoadLevel("mike3");
			done = true;
		}
	}
}
