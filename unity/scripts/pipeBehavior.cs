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
using Vectrosity;
using System.Xml;
/*
 * Manage the network representation.  Note this code is convoluted in that it once managed a pipe that split toward two external
 * computers, with animated data flowing from the computers through the pipe.  Now it mostly just handle network reads and 
 * writes and tracks current colors.
 */
public class pipeBehavior : MonoBehaviour {
	int currentCount = 0;
	Vector3 vertex;
	uint currentAddr = 0;
	bool reading = false;
	uint skipFrames = 0;
	static GameObject pipe;
	// pipe no longer changes color.  use this to
	// manage color of cubes
	private static Color pipeColor;
	public static Color session1Color = Color.red;
	public static Color session2Color = Color.green;
	public static Color networkColor = Color.black;
	//private float pipeX = -7.0f;
	//private float pipeY = 3.0f;
	//private float pipeZ = -5.0f;
	private float pipeX = -57.0f;
	private float pipeY = 3.0f;
	private float pipeZ = 5.0f;
	public static int sessionNumber = 0;
	private int baseBytesPerCube;
	memory memoryScript;
	private bool connectionDone = false;

	// Use this for initialization
	void Awake(){
		transform.position = new Vector3 (pipeX, pipeY, pipeZ);
		//pipe = getPipe ("pipe");
		pipe = gameObject;
		GameObject mem = GameObject.Find ("Memory1");
		memoryScript = (memory) mem.GetComponent (typeof(memory));	
		baseBytesPerCube = memoryScript.bytesPerCube;
	}
	Vector3 getRemoteTeam(){
		Vector3 worldPoint = Vector3.zero;
		Transform ts = gameObject.transform.FindChild ("teamPrefab");
		SphereCollider sc = (SphereCollider) ts.GetComponent (typeof(SphereCollider));
		if(ts != null){
			//Debug.Log ("got team sphere");
			Matrix4x4 thisMatrix = ts.localToWorldMatrix;
			worldPoint = thisMatrix.MultiplyPoint3x4(sc.center);
			return worldPoint;
		}else{
			Debug.Log ("could not find team prefab");
		}
		return worldPoint;
	}
	Vector3 getLocalEndpoint(){
		Transform le = gameObject.transform.FindChild("localEndpoint");
		Matrix4x4 thisMatrix = le.localToWorldMatrix;
		Vector3 worldPoint = thisMatrix.MultiplyPoint3x4(le.localPosition);
		return worldPoint;
	}
	// determine starting points and distance for growing the pipe from the team prefab to the mixer
	void growTeamPipe(){
		Transform ts = gameObject.transform.FindChild ("teamPrefab");
		SphereCollider sc = (SphereCollider) ts.GetComponent (typeof(SphereCollider));
		float teamRadius = sc.radius*ts.localScale.x;
		//Debug.Log ("got team sphere, radius is "+ teamRadius);

		// generate a pulse -- just a quickly expanding sphere
		GameObject pulse = Instantiate (Resources.Load ("pulsePrefab")) as GameObject;
		pulseBehavior pb = (pulseBehavior) pulse.GetComponent(typeof(pulseBehavior));
		Matrix4x4 thisMatrix = ts.localToWorldMatrix;
		Vector3 worldPoint = thisMatrix.MultiplyPoint3x4(sc.center);
		pb.doPulse (worldPoint, 15, 1.5f, getRemoteColor ());

		// team pipe transform position should be middle of that pipe.
		// find distance between the two positions, and prorate based on radius
		// as a percentage of the distance.
		Transform teamPipe = gameObject.transform.FindChild ("teamPipe");
		//Vector3 mixer = transform.position;
		float team2Pipe = Vector3.Distance (ts.localPosition, teamPipe.localPosition);
		float percent = teamRadius / team2Pipe;
		//Debug.Log ("distance is " + team2Pipe + " radius " + teamRadius + " percent = " + percent);
		Vector3 startPipe = Vector3.Lerp(ts.localPosition, teamPipe.localPosition, percent);
		pipeGrow script = (pipeGrow)teamPipe.gameObject.GetComponent (typeof(pipeGrow));
		if(script == null){
			Debug.Log ("no script on team pipe");
			Debug.Break();
			return;
		}

	
		sc = (SphereCollider) transform.GetComponent (typeof(SphereCollider));
		//Debug.Log ("got local sphere");
		Transform localPipe = gameObject.transform.FindChild ("localPipe");
		//Vector3 mixer = transform.position;
		float mix2Pipe = Vector3.Distance (ts.localPosition, teamPipe.localPosition);
		float mixRadius = sc.radius*ts.localScale.x;
		percent = teamRadius / team2Pipe;
		//Debug.Log ("local distance is " + mix2Pipe + " radius " + mixRadius + " percent = " + percent);
		Vector3 startLocalPipe = Vector3.Lerp(sc.center, teamPipe.localPosition, percent);
		pipeGrow localPipeScript = (pipeGrow)localPipe.gameObject.GetComponent (typeof(pipeGrow));
		if(script == null){
			Debug.Log ("no script on team pipe");
			Debug.Break();
			return;
		}
		script.growPipe (startPipe, 3.3f, localPipe.gameObject, startLocalPipe, 5.0f);

	
	
	
	}
	void Start () {
		gameObject.name = "Network";
		networkColor.a = 0.5f;
		Transform teamPipe = gameObject.transform.FindChild ("teamPipe");
		teamPipe.gameObject.SetActive (false);
		Transform localPipe = gameObject.transform.FindChild ("localPipe");
		localPipe.gameObject.SetActive (false);

		/*
		Transform ts = gameObject.transform.FindChild ("teamPrefab");
		SphereCollider sc = (SphereCollider) ts.GetComponent (typeof(SphereCollider));
		if(ts != null){
			Debug.Log ("got team sphere");
			Matrix4x4 thisMatrix = ts.localToWorldMatrix;
			Vector3 worldPoint = thisMatrix.MultiplyPoint3x4(sc.center);
			VectorLine vl = VectorLine.SetLine3D(Color.black, worldPoint, transform.position);
			Transform le = gameObject.transform.FindChild("localEndpoint");
			thisMatrix = le.localToWorldMatrix;
			worldPoint = thisMatrix.MultiplyPoint3x4(le.localPosition);
			VectorLine vl1 = VectorLine.SetLine3D(Color.black, worldPoint, transform.position);

		}else{
			Debug.Log ("team sphere null");
		}
		*/
		//VectorLine.SetLine3D (Color.green, elbow, splitter);

	}
	public void initNetwork(){
		//pipe.renderer.material.color = session1Color;
		updateNetworkColor();
		this.currentAddr = 0;
		this.currentCount = 0;

	}
	public void toXML(ref XmlDocument xmlDoc, ref XmlNode parent){
		XmlNode pipeNode = xmlDoc.CreateElement ("network");
		XmlNode sessionNode = xmlDoc.CreateElement ("session");
		sessionNode.InnerText = sessionNumber.ToString ();
		pipeNode.AppendChild (sessionNode);
		parent.AppendChild (pipeNode);
	}
	public void fromXML(XmlDocument xmlDoc){
		initNetwork ();
		XmlNode pipeNode = xmlDoc.SelectSingleNode ("//bookmark/network");
		XmlNode sessionNode = pipeNode.SelectSingleNode ("session");
		sessionNumber = int.Parse (sessionNode.InnerText) - 1;
//		if(sessionNumber > 0){
			didAccept();
		if(sessionNumber > 1){
			if(!connectionDone){
				growTeamPipe();
				connectionDone = true;
			}
		}
//		}
	}
	public static void setNetworkColor(Color color){
		networkColor = color;
	}

	public static Color getPipeColor(){
		return pipeColor;
		//return pipe.renderer.material.color;
	}
	public static void updateNetworkColor(){
		pipe.renderer.material.color = networkColor;
	}
	public static Color getNetworkColor(){
		return networkColor;
		//return pipe.renderer.material.color;
	}
	public GameObject getPipe(string name){
		return gameObject;
		//Transform t = transform.Find (name);
		//return t.gameObject;
	}
	// intended for use by menu's when chaning configuration
	public static void updatePipeColor(){
		if(sessionNumber == 1){
			//Debug.Log ("will be first session");
			pipeColor = session1Color;
		}else if(sessionNumber == 2){
			//Debug.Log ("will be second session");
			pipeColor = session2Color;
		}
	}
	public void didAccept(){
		if(sessionNumber == 0){
			//Debug.Log ("will be first session");
			pipeColor = session1Color;
		}else if(sessionNumber == 1){
			//Debug.Log ("will be second session");
			pipeColor = session2Color;
		}
		sessionNumber++;

	}
	public void showStuff(bool show){
		var t = gameObject.GetComponentsInChildren(typeof(Renderer));
		for(int i=0; i<t.Length; i++){
			t[i].renderer.enabled = show;
		}
	}
	Color getRemoteColor(){
		return Color.green;
	}
	// Update is called once per frame
	void Update () {

		if(this.skipFrames > 0){
			skipFrames--;
			return;
		}
		int bundle = 0;
		//if(this.currentCount > 0){
		while(this.currentCount > 0 && bundle < dataBehaviorNew.reduce){
			Vector3 destination = Vector3.zero;
			GameObject replace = null;
			if(memoryScript){
				destination = memoryScript.getAddressLocation (this.currentAddr);
				replace = memoryScript.getCube(this.currentAddr);
			}
			//Debug.Log("pipe destination for address "+currentAddr.ToString("x")+" is "+destination);
			// NOTE on writes, source is actually the distination pipe
			Vector3 source = pipe.transform.position;
			float radius = 0.5F;
			float left = source.x - radius;
			float right = source.x + radius;
			float newx = Random.Range(left, right);
			float top = source.z - radius;
			float bot = source.z + radius;
			float newz = Random.Range (top, bot);
			float starty = Random.Range (0, 2.0F);
			float speed = 30.0f;
			dataBehaviorNew.FlyType[] flyPoints = new dataBehaviorNew.FlyType[5];
			// randDsource is destination if a write
			Vector3 randSource = new Vector3 (newx, source.y+starty, newz);
			GameObject myData;
			myData = memory.getFreeData();
			//myData = Instantiate (Resources.Load ("dataPrefab")) as GameObject;
			myData.renderer.material.color = getPipeColor();
			dataBehaviorNew script = (dataBehaviorNew) myData.GetComponent(typeof(dataBehaviorNew));
			bool destroyCube = false;
			bool caboose = false;
			if(memoryScript){
				caboose = (this.currentCount <= memoryScript.bytesPerCube);
			}else{
				caboose = (this.currentCount <= baseBytesPerCube);
				destroyCube = true;
			}
			bool leader = false;
			int changeAt = -1;
			Color changeTo = Color.black;
			Vector3 dataScale = new Vector3(4.0f, 4.0f, 4.0f);
			if(this.reading){
				changeAt = 1;
				changeTo = getPipeColor();
				//myData.renderer.material.color = getPipeColor();
				myData.renderer.material.color = getRemoteColor();
				flyPoints[0] = new dataBehaviorNew.FlyType(getRemoteTeam(), speed*1.2f, true, dataScale);
				flyPoints[1] = new dataBehaviorNew.FlyType(transform.position, speed*1.8f, true, dataScale);
				flyPoints[2] = new dataBehaviorNew.FlyType(getLocalEndpoint(), speed, false, Vector3.zero);
				//flyPoints[0] = new dataBehaviorNew.FlyType(randSource, speed, false, Vector3.zero);
				flyPoints[3] = new dataBehaviorNew.FlyType(vertex, speed, false, Vector3.zero);
				flyPoints[4] = new dataBehaviorNew.FlyType(destination, speed, false, Vector3.zero);
				script.flyTo (flyPoints, caboose, false, destroyCube, replace, (uint)(this.currentAddr+bundle), changeAt, changeTo, 1.0f);
				if(memoryScript)
				{
					if(!memoryScript.putCube(this.currentAddr, myData)){
						Debug.Log ("pipeBehavior could not put cube in region for address "+this.currentAddr.ToString("x"));
					}
				}else{
					Debug.Log ("pipeBehavior no memory region to put cube for "+this.currentAddr.ToString("x"));
				}
			}else{
				//Debug.Log ("writing to network in session "+sessionNumber);
				// writing AGAIN: source and destination are reversed
				Material m;
				m = myData.renderer.material;
				if(replace != null){
					//Debug.Log ("assigning write color to that of replace, which was "+replace.renderer.material.color);
					m.color = replace.renderer.material.color;
				}else{
					Debug.Log ("Writing from empty memory!!");
					m.color = getPipeColor();
				}
				//float randY = Random.Range (0, 2);
				//destination.y = destination.y + randY;
				myData.transform.position = destination;
				flyPoints[0] = new dataBehaviorNew.FlyType(destination, speed, true, dataScale);
				flyPoints[1] = new dataBehaviorNew.FlyType(vertex, speed, false, Vector3.zero);
				//flyPoints[2] = new dataBehaviorNew.FlyType(randSource, speed, false, Vector3.zero);
				flyPoints[2] = new dataBehaviorNew.FlyType(getLocalEndpoint(), speed, false, Vector3.zero);
				flyPoints[3] = new dataBehaviorNew.FlyType(transform.position, speed, false, Vector3.zero);
				flyPoints[4] = new dataBehaviorNew.FlyType(getRemoteTeam(), speed, false, Vector3.zero);
				script.flyTo (flyPoints, caboose, leader, true, null, (uint)(this.currentAddr+bundle), changeAt, changeTo, 1.0f);
			}
			if(memoryScript){
				this.currentAddr = this.currentAddr + (uint) memoryScript.bytesPerCube;
				this.currentCount = this.currentCount - memoryScript.bytesPerCube;
			}else{
				this.currentCount -= baseBytesPerCube;
			}
			bundle++;
		}
	}


	private Material setPipeAlpha(string name, float alpha){
		Transform t = gameObject.transform.Find (name);
		Color c = t.renderer.material.color;
		c.a = alpha;
		t.renderer.material.color = c;
		return t.renderer.material;
	}

	public void read(uint to, int count, memory region, Vector3 vertex){
		memoryScript = region;
		if(memoryScript && !memoryScript.inRange(to)){
			Debug.Log ("pipeBehavior read  address out of range "+to.ToString("x"));
			//Debug.Break ();
			return;
		}
		this.currentAddr = to;
		this.vertex = vertex;
		this.reading = true;
		this.currentCount = count;
		if(!connectionDone){
			growTeamPipe();
			connectionDone = true;
		}
	}
	public void write(uint from, int count, memory region, Vector3 vertex){
		memoryScript = region;
		if(memoryScript && !memoryScript.inRange(from)){
			Debug.Log ("pipeBehavior write address out of range "+from.ToString("x"));
			//Debug.Break ();
			return;
		}
		this.currentAddr = from;
		this.currentCount = count;
		this.vertex = vertex;
		this.reading = false;

	}
}
