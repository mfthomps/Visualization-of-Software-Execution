using UnityEngine;
using System.Collections;

public class pipeGrow : MonoBehaviour {
	Vector3 startPipe;
	float endYScale;
	Vector3 endPipe;
	float journeyLength = 0;
	float startTime;
	GameObject nextPipe;
	Vector3 nextLoc;
	float pipeSpeed;
	float nextSpeed;
	// Use this for initialization
	public void growPipe(Vector3 start, float pipeSpeed, GameObject nextPipe, Vector3 nextLoc, float nextSpeed){
		this.startPipe = start;
		this.nextLoc = nextLoc;
		this.nextPipe = nextPipe;
		this.pipeSpeed = pipeSpeed;
		this.nextSpeed = nextSpeed;
		endYScale = transform.localScale.y;
		Vector3 tmpScale = transform.localScale;
		tmpScale.y = 0;
		gameObject.SetActive (true);
		endPipe = transform.localPosition;
		journeyLength = Vector3.Distance(startPipe, endPipe);
		Debug.Log ("end pipe at "+endPipe+" journeyLength = " + journeyLength);
		startTime = Time.time;	
	}

	
	// Update is called once per frame
	void Update () {
		if(journeyLength == 0)
			return;
		float now = Time.time;
		float distCovered = (now - startTime) * this.pipeSpeed;
		float fracJourney = distCovered / journeyLength;
		if(fracJourney < 1.0){
			//Debug.Log ("speed "+pipeSpeed+" dist covered is "+distCovered+" len is "+journeyLength+" now is " + Time.time + " frac is " + fracJourney);
			transform.localPosition = Vector3.Lerp(startPipe, endPipe, fracJourney);
			float scaley = (float)((fracJourney)*endYScale);
			Vector3 scale = transform.localScale;
			scale.y = scaley;
			transform.localScale = scale;
		}else{
			Debug.Log("finished grow at "+transform.localPosition);
			journeyLength = 0;
			if(this.nextPipe != null){
				pipeGrow pg = (pipeGrow) this.nextPipe.GetComponent(typeof(pipeGrow));
				pg.growPipe(this.nextLoc, this.nextSpeed, null, Vector3.zero, 0);
			}
		}

	}
}
