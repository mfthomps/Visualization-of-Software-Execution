/*
 * This software was created by United States Government employees
 * and may not be copyrighted.
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
 * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
*/
using UnityEngine;
using System.Collections;

public class connectSphere : MonoBehaviour {
	Vector3 start;
	Vector3 finish;
	float journeyLength;
	float speed = 65.0f;
	float startTime;
	// Use this for initialization
	void Start(){
	}
	public void doConnect () {
		startup.pause (false);
		startup.pauseLabelString = "connection";
		GameObject network = GameObject.Find ("Network");
		finish = network.transform.position;
		start = finish;
		start.x -= 80;
		start.y -= 20;
		start.z += 60;
		this.journeyLength = Vector3.Distance(start, finish);
		startTime = Time.time;

	}
	
	// Update is called once per frame
	void Update () {
		float now = Time.time;
		float distCovered = (now - startTime) * this.speed;
		float fracJourney = distCovered / journeyLength;
		if(fracJourney < 1.0){
			//Debug.Log ("speed "+speed+" dist covered is "+distCovered+" len is "+journeyLength+" now is " + Time.time + " frac is " + fracJourney);
			transform.position = Vector3.Lerp(this.start, this.finish, fracJourney);
		}else{
			startup.resume ();
			Destroy(gameObject);
		}
	}
}
