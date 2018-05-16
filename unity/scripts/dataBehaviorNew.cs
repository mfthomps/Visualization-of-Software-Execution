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
using System.Xml;
/*
 * Data object.  Manage flight along route given in flyTo method.
 */
public class dataBehaviorNew : MonoBehaviour {
	public FlyType[] markers;
	public int startMark;
	public int endMark;
	public float speed;
	private float startTime = 0.0F;
	private float journeyLength;
	public Transform target;
	public float smooth = 5.0F;
	private bool doneMoving = true;
	private bool caboose;
	private bool leader;
	private bool destroy;
	private GameObject replace;
	private Vector3 origScale;
	int changeAt;
	Color changeTo;
	// affects bytes per cube. The local x scale of a cube is
	// divided by the reduce value.  Further reduction hurts fps.
	public static int reduce = 4;
	//public static int reduce = 15;
	//public static float flyBloat = 2.0f;
	public uint address;

	// Use this for initialization
	void Start () {
		startTime = Time.time;

	}
	public void showSummary(){
		Debug.Log ("data from address: " + address.ToString ("x"));
	}
	public void reset(){
		transform.localScale = origScale;
	}
	public void Awake(){
		Vector3 scale = transform.localScale;
		scale.x = scale.x / reduce;
		//Debug.Log ("in data, xscale is " + scale.x);
		transform.localScale = scale;
		origScale = scale;

	}
	public void setCaboose(){
		this.caboose = true;
		if(doneMoving){
			startup.resume();
		}
		
	}
	public class FlyType{
		public Vector3 point;
		public float speed;
		public bool doScale;
		public Vector3 scale;
		public FlyType(Vector3 point, float speed, bool doScale, Vector3 scale){
			this.point = point;
			this.speed = speed;
			this.doScale = doScale;
			this.scale = scale;
		}

	}
	// define the flight path of this data object
	// caboose: is this the last data in an operation, i.e., should consumption of instructions resume?
	// destroy: e.g., part of a network write, object disappears upon arrival at final destination
	// replace: another data object that this one will replace.
	// address: only for informational purposes
	public void flyTo(FlyType[] points, bool caboose, bool leader, bool destroy, GameObject replace, uint address, 
	                  int changeAt, Color changeTo, float flyBloat){
		//Debug.Log ("in flyTo");
		this.markers = points;
		this.caboose = caboose;
		this.destroy = destroy;
		this.replace = replace;
		this.leader = leader;
		this.startMark = 0;
		this.endMark = 1;
		this.address = address;
		this.changeAt = changeAt;
		this.changeTo = changeTo;
		//if(this.caboose){
		//	Debug.Log("I am the caboose");
		//}
		this.journeyLength = Vector3.Distance(markers[startMark].point, markers[endMark].point);
		// til now, update has just returned for this object
		this.doneMoving = false;
		Vector3 scale = transform.localScale;
		if(!points[0].doScale){
			scale.x = scale.x * flyBloat;
			scale.y = scale.y * flyBloat;
			scale.z = scale.z * flyBloat;
		}else{
			scale.x = scale.x * points[0].scale.x;
			scale.y = scale.y * points[0].scale.y;
			scale.z = scale.z * points[0].scale.z;
		}
		transform.localScale = scale;

		//Debug.Log ("startTime is " + startTime);
	}
	public void quickCopy(Vector3 destination, uint address){
		if(destination == Vector3.zero){
			memory.freeData(gameObject);
		}else{
			transform.position = destination;
			doneMoving = true;
			this.address = address;
			dataBehaviorNew thisScript = (dataBehaviorNew) gameObject.GetComponent(typeof(dataBehaviorNew));
			thisScript.enabled = false;
		}
	}
	private static void appendXMLString(ref XmlDocument xmlDoc, ref XmlNode parent, string name, string value){
		XmlNode theNode = xmlDoc.CreateElement (name);
		theNode.InnerText = value;
		parent.AppendChild (theNode);
	}
	public void toXML(ref XmlDocument xmlDoc, ref XmlNode parent, int index){
		XmlNode cubeNode = xmlDoc.CreateElement ("cube");
		appendXMLString (ref xmlDoc, ref cubeNode, "address", this.address.ToString("x"));
		XmlNode aPoint = xmlDoc.CreateElement("position");
		appendXMLString (ref xmlDoc, ref aPoint, "x", transform.position.x.ToString("f"));
		appendXMLString (ref xmlDoc, ref aPoint, "y", transform.position.y.ToString("f"));
		appendXMLString (ref xmlDoc, ref aPoint, "z", transform.position.z.ToString("f"));
		cubeNode.AppendChild(aPoint);

		XmlNode colorNode = xmlDoc.CreateElement("color");
		colorNode.InnerText = gameObject.renderer.material.color.ToString ();
		cubeNode.AppendChild (colorNode);

		XmlNode indexNode = xmlDoc.CreateElement("index");
		indexNode.InnerText = index.ToString();
		cubeNode.AppendChild (indexNode);
		parent.AppendChild (cubeNode);

	}
	public int fromXML(XmlNode cubeNode){
		XmlNode addressNode = cubeNode.SelectSingleNode ("address");
		address = uint.Parse (addressNode.InnerText, System.Globalization.NumberStyles.HexNumber);
		XmlNode colorNode = cubeNode.SelectSingleNode ("color");
		gameObject.renderer.material.color = myUtils.colorFromRGBA (colorNode.InnerText);
		XmlNode indexNode = cubeNode.SelectSingleNode ("index");
		int index = int.Parse (indexNode.InnerText);
		dataBehaviorNew thisScript = (dataBehaviorNew) gameObject.GetComponent(typeof(dataBehaviorNew));
		thisScript.enabled = false;
		return index;

	}
	// Animate the fllight of this data object
	void Update () {
		//Debug.Log ("moving");
		if(doneMoving){
			return;
		}
		float now = Time.time;
		float distCovered = (now - startTime) * this.markers[startMark].speed;
		float fracJourney = distCovered / journeyLength;
		if(fracJourney < 1.0){
			//Debug.Log ("speed "+speed+" dist covered is "+distCovered+" len is "+journeyLength+" now is " + Time.time + " frac is " + fracJourney);
			transform.position = Vector3.Lerp(this.markers[startMark].point, this.markers[endMark].point, fracJourney);
		}else if(this.endMark < this.markers.Length-1){
			this.startMark++;
			this.endMark++;
			if(this.markers[startMark].doScale){
				Vector3 scale = origScale;
				scale.x = scale.x * this.markers[startMark].scale.x;
				scale.y = scale.y * this.markers[startMark].scale.y;
				scale.z = scale.z * this.markers[startMark].scale.z;
				transform.localScale = scale;
			}
			float randOffset = Random.Range (0, 0.1F);
			this.journeyLength = Vector3.Distance(this.markers[startMark].point, this.markers[endMark].point);
			this.startTime = Time.time;
			if(startMark == changeAt){
				renderer.material.color = changeTo;
			}
			//Debug.Log ("data, my 3rd pt is "+this.thirdMarker);
		}else{
			doneMoving = true;
			transform.position = this.markers[this.markers.Length-1].point;
			transform.localScale = origScale;
			if(this.caboose){
				//Debug.Log ("CABOOOOOOOOOSE");
				startup.resume();
			}
			if(this.destroy){
				//Destroy(gameObject, 0.1F);
				memory.freeData(gameObject);
			}
			if(this.replace){
				memory.freeData(this.replace);
				this.replace = null;
			}
			dataBehaviorNew thisScript = (dataBehaviorNew) gameObject.GetComponent(typeof(dataBehaviorNew));
			thisScript.enabled = false;
		}
	}
}

