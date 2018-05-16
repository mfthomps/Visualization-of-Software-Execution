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
using System.Collections.Generic;
using System.IO;
using Vectrosity;
using System.Xml;
/*
 * Read the functionList file (functions and basic blocks) and create function objects with a suitable number
 * of vertices (at least as many as there are basic blocks).
 * 
 * This file also defines the functionItem class, which perhaps should be integrated into the functionBehavior class.
 * However the way Unity manages objects makes that awkward, at least for the author.
 */
public class functionDatabase : MonoBehaviour {
	static Dictionary<uint, functionItem> functions = null;
	static List<uint> functionList;
	// vertical location of first function row
	public static int top = 0;
	// read in the functions, assign shapes and do the layout
	public static void doInit(string fname){
		int levelStart = 10;
		if(functions != null){
			for(int i=0; i<functionList.Count; i++){
				functions[functionList[i]].clear();
				Destroy(functions[functionList[i]].go);
			}
			functions.Clear();
			functionList.Clear();
		}
		functions = new Dictionary<uint, functionItem>();
		functionList = new List<uint>();
		StreamReader reader = null;
		// read the functionList and assign verticle levels
		try{
			reader = new StreamReader (fname);
		}catch(FileNotFoundException e){
			startup.errorString = e.ToString ();
			//Debug.Break();
			return;
		}
		string line;
		int lastLevel = -1;
		int curx = -1;
		int maxX = -1;
		bool first = true;
		List<int> funsPerLevel = new List<int>();
		int levelCount = 0;
		int val;
		using (reader)
		{
			while ((line = reader.ReadLine()) != null) {
				
				functionItem fi = new functionItem(line);
				functionList.Add(fi.address);
				if(fi.level > lastLevel && !first){
					if(curx > maxX){
						maxX = curx;
					}
					if(curx >= 0){
						val = curx+1;
						//Debug.Log ("adding funcsPerlevel "+val+" entry "+levelCount);
						funsPerLevel.Add (val);
					}else{
						Debug.Log ("New level, but curx is zero??");
						funsPerLevel.Add(0);
					}
					levelCount++;
					curx = -1;
				}else{
					curx++;
				}
				first = false;
				lastLevel = fi.level;
				fi.x = curx;
				functions.Add(fi.address, fi);
				//Debug.Log ("line: " + line);
			}
			val = curx+1;
			//Debug.Log ("adding funcsPerlevel "+val+" entry "+levelCount);
			funsPerLevel.Add (curx+1);
			//for(int i=0; i < funsPerLevel.Count; i++){
			//	Debug.Log ("level "+i+" has "+funsPerLevel[i]+" functions ");
			//}
		}
		// Adjust the function height per their levels
		Debug.Log ("len of funsPerLevel is "+funsPerLevel.Count+" numlevels is "+lastLevel);
		top = lastLevel + levelStart;
		//float currentZ = 0.5F;
		for(int i=0; i<functionList.Count; i++){
			functionItem fi = functions[functionList[i]];
			fi.y = top - fi.level;
			//fi.z = currentZ;
			//Debug.Log ("look at level "+ fi.level);
			int tmp = (maxX - funsPerLevel[fi.level-1])/2;
			fi.x = fi.x + tmp;
			//currentZ = currentZ * -1.0F;
		}
		assignObjects ();
	}
	// pick a shape based on quantity of vertices & # of basic blocks
	private static void assignObjects(){
		for(int i = 0; i < length(); i++){
			uint address = getFunctionAddress(i);
			functionItem fi = getFunction(address);
			string polyType = "stellatedPrefab";
			if(fi.num_blocks <= 5){
				polyType = "pyramidPrefab";
				
			}else if (fi.num_blocks <= 6){
				polyType = "octoPrefab";
				
			}else if (fi.num_blocks <= 8){
				polyType = "cubePrefab";
				
			}else if (fi.num_blocks <= 10){
				polyType = "isoPrefab";
				
			}else if (fi.num_blocks <= 20){
				polyType = "decoPrefab";
				
			}else if (fi.num_blocks <= 26){
				polyType = "thirtysixPrefab";
				fi.subtype = 1;
				fi.xOff = 0.3f;
				fi.yOff = -0.2f;
				fi.zOff = -0.3f;
				
			}else if (fi.num_blocks <= 60){
				polyType = "soccerPrefab";
				
			}else if (fi.num_blocks <= 70){
				polyType = "buckyPrefab";
				fi.xOff = -0.3f;
				fi.subtype = 1;
				
				//}else if (fi.num_blocks <= 74){
				//	polyType = "rhombicPrefab";
				
				//}else if (fi.num_blocks <= 127){
				//	polyType = "ditriPrefab";
				//	fi.subtype = 1;
				
			}else if (fi.num_blocks <= 80){
				polyType = "conjecturePrefab";
				fi.subtype = 1;
				
			}else if (fi.num_blocks <= 90){
				polyType = "pentakisPrefab";
				fi.yOff = -0.32f;
				
			}else if (fi.num_blocks <= 110){
				polyType = "romanPrefab";
				fi.subtype = 1;
				
			}else if (fi.num_blocks <= 200){
				polyType = "stellarPrefab";
				fi.subtype = 1;
				fi.xOff = 0.6f;
				fi.yOff = 0.1f;
				fi.zOff = -0.073f;
			}else{ //stellatedPrefab
				polyType = "stellatedPrefab";
				fi.xOff = 0.2f;
				fi.yOff = 0.25f;
				fi.zOff = -0.17f;
			}
			fi.setType(polyType);
			GameObject go;
			//go = (GameObject) Instantiate(isoPrefab, pt, Quaternion.identity) as GameObject;
			go = (GameObject) Instantiate(Resources.Load (polyType)) as GameObject;
			functionBehaive fb = (functionBehaive) go.GetComponent(typeof(functionBehaive));
			fb.setFunction(fi);
			SphereCollider sc = go.AddComponent<SphereCollider>();

			Vector3 center = new Vector3(fi.xOff/go.transform.localScale.x, fi.yOff/go.transform.localScale.y, fi.zOff/go.transform.localScale.z);
			//sc.radius = 0.6f/go.transform.localScale.x;
			sc.radius = 0.6f/go.transform.localScale.x;
			if(polyType == "stellarPrefab"){
				//Debug.Log ("obect "+go.name+" radius is "+sc.radius+" sizex is "+sc.bounds.size.x+" "+sc.bounds.size.y+" "+sc.center.x+" "+sc.center.y);
				//sc.center = Vector3.zero;
				center.y -= 1.0f/go.transform.localScale.y;
				center.x -= 0.5f/go.transform.localScale.x;
			}
			sc.center = center;
			Vector3 pt = new Vector3(fi.x-fi.xOff, fi.y-fi.yOff, fi.z-fi.zOff);
			go.transform.position = pt;
			fi.go = go;
			fi.setShader(startup.shader2);
			fi.setColor (startup.color2);
			fi.setScript (fb);
			
		}
	}
	// intended for use when editing configuration (e.g., via a slider)
	public static void updateColors(){
		for(int i=0; i<functionList.Count; i++){
			functions[functionList[i]].setColor(startup.color2);
		}
	}
	public static void toXML(ref XmlDocument xmlDoc,ref  XmlNode parent){
		for(int i=0; i<functionList.Count; i++){
			functions[functionList[i]].addXML(ref xmlDoc, ref parent);
		}
	}
	public static void fromXML(XmlDocument xmlDoc){
		XmlNode functionsNode = xmlDoc.SelectSingleNode ("//bookmark/functions");
		XmlNodeList functionNodes = functionsNode.SelectNodes ("function");
		//Debug.Log ("functionData fromXML found # function nodes: " + functionNodes.Count);
		for(int i=0; i<functionNodes.Count; i++){
			functions[functionList[i]].fromXML(functionNodes[i]);
		}
	}
	public static Vector3 getVertex(uint address, int num){
		return functions[address].getVertex(num);
	}
	public static int length()
	{
		return functions.Count;
	}
	public static int getTop(){
		return top;
	}
	public static functionItem getFunction(uint address)
	{
		return functions [address];
	}
	public static uint getFunctionAddress(int index){
		return functionList [index];
	}
	/*
	 * Manage position and track invokation of function objects.  Also manage
	 * the pin objects (springs) that illustrate the original position of the function.
	 * This class also manages the individual spotlights that illuminate functions that
	 * are currently invoked.
	 */
	// TBD better integrate these functions with what goes on in functionBehavior
	public class functionItem
	{
		public string name;
		public int level;
		public int num_blocks;
		public uint address;
		public float x;
		public float y;
		public float z;
		public float xOff;
		public float yOff;
		public float zOff;
		public GameObject go;
		public int called = 0;
		// # of control line points per call frame in this functon
		public List<int> pointsPerCall;
		public GameObject light = null;
		public VectorLine spring = null;
		Color springColor;
		public string kind;
		public int subtype = 0;
		public int calledTotal;
		public functionBehaive script;
		//public List<functionItem> callStack = null;
		public functionItem(string line)
		{
			var parts = line.Split ();
			this.name = parts[0];
			this.level = int.Parse(parts[1]);
			this.num_blocks = int.Parse(parts[2]);
			this.address = uint.Parse(parts[3],System.Globalization.NumberStyles.HexNumber);
			this.x = 0;
			this.y = 0;
			this.z = 0;
			this.go = null;
			this.pointsPerCall = new List<int>();
			this.called = 0;
			this.calledTotal = 0;
			this.light = null;
			this.springColor = new Color(83.0f/255,134.0f/255,139.0f/255, 0.5f);
			//this.springColor = new Color(238.0f/256,221.0f/256,130.0f/256, 0.5f);
			//this.callStack = new List<functionItem>();
		}
		public void clear(){
			if(this.light != null)
				Destroy(this.light);
			if(this.spring != null){
				VectorLine.Destroy(ref this.spring);
			}
		}
		private void appendXMLString(ref XmlDocument xmlDoc, ref XmlNode parent, string name, string value){
			XmlNode theNode = xmlDoc.CreateElement (name);
			theNode.InnerText = value;
			parent.AppendChild (theNode);
 		}
		public void fromXML(XmlNode function){
			XmlNode calledNode = function.SelectSingleNode ("called");
			if(calledNode == null){
				Debug.Log ("functionData could not find called element in function ");
				XmlNode nameNode = function.SelectSingleNode("name");
				Debug.Log ("name is "+nameNode.InnerText);
				//Debug.Break();
			}
			this.called = int.Parse (calledNode.InnerText);
			XmlNode calledTotalNode = function.SelectSingleNode ("calledTotal");
			this.calledTotal = int.Parse (calledTotalNode.InnerText);
			XmlNodeList pointsPer = function.SelectNodes ("pointsPerCall");
			this.pointsPerCall.Clear ();
			for(int i=0; i< pointsPer.Count; i++){
				//Debug.Log ("adding pointsper call for "+this.name+" pts "+pointsPer[i].InnerText);
				this.pointsPerCall.Add (int.Parse(pointsPer[i].InnerText));
			}
			if(this.light != null){
				//this.light.light.enabled = false;
				GameObject.Destroy(this.light);
				this.light = null;
			}
			if(this.spring != null){
				this.spring.drawEnd = 0;
			}
			Vector3 pos = this.go.transform.position;
			if(this.called > 0){
				pos.z = this.z + manageControl.funForwardRunning;
				this.go.transform.position = pos;
				doSpring();
				if(this.light == null){
					makeLight();
				}else{
					this.light.light.enabled = true;
				}
				this.setColor(startup.color2);

			}else if(this.calledTotal > 0){
				pos.z = this.z + manageControl.funForwardCalled;
				this.go.transform.position = pos;
			}

		}

		public XmlNode addXML(ref XmlDocument xmlDoc, ref XmlNode parent){
			XmlNode functionNode = xmlDoc.CreateElement ("function");
			appendXMLString (ref xmlDoc, ref functionNode, "name", this.name);
			appendXMLString (ref xmlDoc, ref functionNode, "called", this.called.ToString());
			appendXMLString (ref xmlDoc, ref functionNode, "calledTotal", this.calledTotal.ToString());
			for(int i=0; i<this.pointsPerCall.Count; i++){
				appendXMLString(ref xmlDoc, ref functionNode, "pointsPerCall", this.pointsPerCall[i].ToString());
			}
			parent.AppendChild (functionNode);
			return functionNode;
		}
		public Vector3 getPosition(){
			Vector3 pos = this.go.transform.position;
			Vector3 offset = new Vector3 (this.xOff, this.yOff, this.zOff);
			return pos - offset;
		}
		public void setType(string kind){
			this.kind = kind;
		}
		public void setScript(functionBehaive script){
			this.script = script;
		}
		public void doSpring(){
			if(this.spring == null){
				Vector3 pt1 = this.go.transform.position;
				Vector3 pt2 = pt1;
				Vector3 pt3 = pt1;
				SphereCollider sc = (SphereCollider) go.GetComponent(typeof(SphereCollider));
				//pt1.z = pt1.z + 0.45f;
				//Debug.Log ("size of "+this.kind+" is "+sc.bounds.size.z);
				pt1.z = pt1.z + (sc.bounds.size.z/3.0f);
				if(this.kind == "stellatedPrefab"){
					pt1.y += 0.4f;
					pt2.y += 0.4f;
				}
				pt2.z = pt1.z+0.1f;
				pt3.z = this.z + manageControl.funForwardCalled;
				this.spring = VectorLine.SetLine3D(this.springColor, pt3, pt2, pt1);
				//this.spring.lineWidth = 1.0f;
				float[] widths = new float[2];
				widths[0] = 0.5f;
				widths[1] = 5.0f;
				this.spring.SetWidths(widths);
				this.spring.smoothWidth = true;
			}else{
				this.spring.drawEnd = 2;
			}
		}
		public void didCall(){
			this.called++;
			if(this.called == 1){
				this.setColor(startup.color2);
			}
			this.calledTotal++;
			this.pointsPerCall.Add (1);
			if(this.light == null) 
			{
				makeLight ();
			}else{
				this.light.light.enabled = true;
			}
			doSpring ();

		}
		public void didReturn(){
			int len = this.pointsPerCall.Count;
			//Debug.Log ("before didReturn len now "+len+" currentPoints now " + currentPoints ());
			if(len > 0){
				this.pointsPerCall.RemoveAt(len-1);
				len = this.pointsPerCall.Count;
				//Debug.Log ("didReturn len now "+len+" currentPoints now " + currentPoints ());
			}else{
				Debug.Log ("didReturn but nothing on stack");
			}
			this.called--;
			if(this.called == 0){
				//Destroy(this.light);
				//this.light = null;
				if(this.light != null){
					this.light.light.enabled = false;
				}
				if(this.spring != null){
					//VectorLine.Destroy(ref this.spring);
					this.spring.drawEnd = 0;
				}
				this.setColor(startup.color2);

			}
		}
		public void didGoto(){
			int len = this.pointsPerCall.Count;
			if (len == 0) {
				//Debug.Log ("didGoto but no points in stack, add one");
				this.pointsPerCall.Add (1);
			}else{
				this.pointsPerCall [len - 1] = this.pointsPerCall [len - 1] + 1;
			}
		}
		public int currentPoints(){
			int len = this.pointsPerCall.Count;
			if(len > 0){
				return this.pointsPerCall [len - 1];
			}else{
				return 0;
			}
		}
		public void setShader(Shader shader){
			Component[] comps = this.go.GetComponents(typeof(Renderer));
			for(int i = 0; i < comps.Length; i++){
				Renderer r = (Renderer) comps[i];
				for(int j = 0; j< r.materials.Length; j++){
					r.materials[j].shader = shader;
				}
			}
			comps = this.go.GetComponentsInChildren(typeof(Renderer));
			for(int i = 0; i < comps.Length; i++){
				Renderer r = (Renderer) comps[i];
				for(int j = 0; j< r.materials.Length; j++){
					r.materials[j].shader = shader;
				}
			}

		}
		public void setColor(Color colorIn){
			Color color = colorIn;
			if(this.called == 0){
				color.a = 0.80f;
			}
			Component[] comps = this.go.GetComponents(typeof(Renderer));
			for(int i = 0; i < comps.Length; i++){
				Renderer r = (Renderer) comps[i];
				for(int j = 0; j< r.materials.Length; j++){
					r.materials[j].color = color;
				}
			}
			comps = this.go.GetComponentsInChildren(typeof(Renderer));
			for(int i = 0; i < comps.Length; i++){
				Renderer r = (Renderer) comps[i];
				for(int j = 0; j< r.materials.Length; j++){
					r.materials[j].color = color;
				}
			}
		}
		public Vector3 getVertex(int num){
			return this.script.getVertex (num);

		}
		public void makeLight(){
			this.light = new GameObject(this.name+" light");
			this.light.AddComponent<Light>();
			this.light.light.color = Color.white;
			this.light.light.intensity = 7;
			this.light.light.range = 5;
			Vector3 pos = this.go.transform.position;
			pos.z = pos.z - 3;
			//pos.x = pos.x + 1;
			pos.x = pos.x + 2;
			pos.y = pos.y + 1;
			this.light.light.type = LightType.Spot;
			this.light.light.spotAngle = 18;
			this.light.transform.position = pos;
			this.light.transform.LookAt (this.go.transform.position);
		}
	}

}
