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
using System.Collections.Generic;
using System.IO;
using System.Xml;
/*
 *  Parses operations and manages execution and data flow.
*/

public class manageControl : MonoBehaviour {
	public static List<functionDatabase.functionItem> callStack = new List<functionDatabase.functionItem>();
	public static functionDatabase.functionItem currentFunction = null;
	static Vector3 currentPoint;
	static GameObject pipe1;
	static pipeBehavior pipeScript;

	public static int funForwardRunning = -10;
	public static int funForwardCalled = -3;
	//GameObject pipe2;
	static memory memoryScript;
	public static Color flowColor = Color.white;
	public static VectorLine controlLine = null;
	public static int lineElement = 0;
	static private long startClock = 0;
	static public bool skipToData = false;
	static public bool skipGoto = true;
	static public bool justShowData = true;
	static public string clockLabelString = "0";
	static public string currentCycleString = "0";
	public static int numInstructions = 0;
	static public long currentClock;
	public static Vector3[] lineVector;
	private static bool followFunction = false;
	private static List<long> breakpoints;
	private static List<memory> memoryRegions;
	public static string eipLabel;
	//public static GameObject camera;

	void Start () {
	// Use this for initialization
	}
	//void OnGUI () {
	//	int x = Screen.width-150;
	//	GUI.Label (new Rect (x-20, 55, x+75, 60), clockLabelString);
	//}
	// Update is called once per frame
	void Update () {
	
	}
	private static void zeroLine(){
		for(int i=0; i <10000; i++){
			lineVector[i] = Vector3.zero;
		}
	}
	public static void doInit(pipeBehavior ps, string home){
		//pipe1 = pipe1In;
		currentFunction = null;
		lineVector = new Vector3[10000];
		zeroLine ();
		GameObject memoryObject = GameObject.Find("Memory1");
		memoryScript = (memory) memoryObject.GetComponent(typeof(memory));
		memoryRegions = new List<memory> ();
		memoryRegions.Add (memoryScript);
		//camera = GameObject.Find ("Main Camera");
		if(controlLine != null){
			VectorLine.Destroy(ref controlLine);
		}
		controlLine = new VectorLine ("myLines", lineVector, flowColor, null, 0.5f, LineType.Continuous);
		controlLine.maxDrawIndex = 0;
		lineElement = 0;
		pipeScript = ps;
		if(pipeScript == null){
			Debug.Log ("pipeScript is null");
		}
		pipe1 = pipeScript.getPipe ("pipe");
		breakpoints = new List<long> ();
		StreamReader reader = null;
		if(System.IO.File.Exists(home+"/breakpoints.txt")){
			reader = new StreamReader (home+"/breakpoints.txt");
			Debug.Log ("Found breakpoints");
		}else{
			Debug.Log ("No breakpoint file in "+home);
		}
		if(reader != null){
			string line;
			using (reader)
			{
				while ((line = reader.ReadLine()) != null) {
					long value = long.Parse(line,System.Globalization.NumberStyles.HexNumber);
					Debug.Log ("add breakpoint at "+value.ToString("x"));
					breakpoints.Add (value);

				}
			}
		}
		clockLabelString = "0";
		currentCycleString = "0";
		numInstructions = 0;
		currentClock=0;
		startClock = 0;
		updateClock (0);

	}

	private static void appendXMLString(ref XmlDocument xmlDoc, ref XmlNode parent, string name, string value){
		XmlNode theNode = xmlDoc.CreateElement (name);
		theNode.InnerText = value;
		parent.AppendChild (theNode);
	}
	public static void toXML(ref XmlDocument xmlDoc, ref XmlNode parent){
		XmlNode controlNode = xmlDoc.CreateElement ("control");
		appendXMLString (ref xmlDoc, ref controlNode, "currentClock", currentClock.ToString("x"));
		appendXMLString (ref xmlDoc, ref controlNode, "startClock", startClock.ToString("x"));
		if(currentFunction != null)
			appendXMLString (ref xmlDoc, ref controlNode, "currentFunction", currentFunction.address.ToString("x"));
		appendXMLString (ref xmlDoc, ref controlNode, "numberInstructions", numInstructions.ToString("x"));
		XmlNode callStackNode = xmlDoc.CreateElement ("callStack");
		for(int i=0; i<callStack.Count; i++){
			appendXMLString(ref xmlDoc, ref callStackNode, "call", callStack[i].address.ToString("x"));
		}
		controlNode.AppendChild (callStackNode);
		XmlNode lineNode = xmlDoc.CreateElement ("controlLine");
		controlNode.AppendChild (lineNode);
		appendXMLString (ref xmlDoc, ref lineNode, "lineElement", lineElement.ToString());
		for(int i=0; i<= lineElement; i++){
			XmlNode aPoint = xmlDoc.CreateElement("linePoint");
			appendXMLString (ref xmlDoc, ref aPoint, "x", lineVector[i].x.ToString("f"));
			appendXMLString (ref xmlDoc, ref aPoint, "y", lineVector[i].y.ToString("f"));
			appendXMLString (ref xmlDoc, ref aPoint, "z", lineVector[i].z.ToString("f"));
			lineNode.AppendChild(aPoint);
		}

		parent.AppendChild (controlNode);
	}
	public static void fromXML(XmlDocument xmlDoc){
		XmlNode controlNode = xmlDoc.SelectSingleNode ("//bookmark/control");
		XmlNode currentClockNode = controlNode.SelectSingleNode ("currentClock");
		currentClock = long.Parse (currentClockNode.InnerText, System.Globalization.NumberStyles.HexNumber);
		XmlNode startClockNode = controlNode.SelectSingleNode ("startClock");
		if(startClockNode != null)
			startClock = long.Parse (startClockNode.InnerText, System.Globalization.NumberStyles.HexNumber);
		updateClock (currentClock);

		XmlNode currentFunctionNode = controlNode.SelectSingleNode ("currentFunction");
		if(currentFunctionNode != null){
			uint addr = uint.Parse (currentFunctionNode.InnerText, System.Globalization.NumberStyles.HexNumber);
			currentFunction = functionDatabase.getFunction (addr);
			eipLabel = "EIP: 0x"+addr.ToString("x");
			Debug.Log ("currentFunction is " + currentFunction.name);
		}
		XmlNode numInstructNode = controlNode.SelectSingleNode ("numberInstructions");
		numInstructions = int.Parse (numInstructNode.InnerText, System.Globalization.NumberStyles.HexNumber);
		startup.resetInstructionTo (numInstructions);

		zeroLine ();

		XmlNode lineNode = controlNode.SelectSingleNode ("controlLine");
		XmlNode elementNode = lineNode.SelectSingleNode ("lineElement");
		int tmpLineElement = int.Parse (elementNode.InnerText);
		XmlNodeList pointNodes = lineNode.SelectNodes ("linePoint");
		for(int i=0; i<=tmpLineElement; i++){
			XmlNode xNode = pointNodes[i].SelectSingleNode("x");
			lineVector[i].x = float.Parse(xNode.InnerText);
			XmlNode yNode = pointNodes[i].SelectSingleNode("y");
			lineVector[i].y = float.Parse(yNode.InnerText);
			XmlNode zNode = pointNodes[i].SelectSingleNode("z");
			lineVector[i].z = float.Parse(zNode.InnerText);

		}
		currentPoint = lineVector [tmpLineElement];
		//Debug.Log ("line elements: " + tmpLineElement + " last " + lineVector [tmpLineElement]);
		// TBD hack needed to keep line from going to zero if this is the first draw.
		lineElement = 3;
		drawControl (true);
		lineElement = tmpLineElement;
		drawControl (true);
		XmlNode callStackNode = controlNode.SelectSingleNode ("callStack");
		XmlNodeList callNodes = callStackNode.SelectNodes ("call");
		callStack.Clear ();
		for(int i=0; i<callNodes.Count; i++){
			uint a = uint.Parse (callNodes[i].InnerText, System.Globalization.NumberStyles.HexNumber);
			functionDatabase.functionItem fi = functionDatabase.getFunction(a);
			callStack.Add(fi);
		}

	}
	// Draw the execution control line.  Width shrinks when camera is up close.
	public static void drawControl(bool force){
		if(lineElement < 2)
			return;
		float width = 1.0f;
		//Debug.Log ("drawControl lineElement is " + lineElement);
		controlLine.maxDrawIndex = lineElement;
		controlLine.drawEnd = lineElement;
		
		if(force || !skipToData){
			if(Camera.main.transform.position.z > -10){
				//Debug.Log ("camera close, thin line");
				width = 0.5f;
			}
			controlLine.lineWidth = width;
			
			controlLine.Draw3D();
		}
	}
	public static void follow(bool follow){
		followFunction = follow;
	}
	public static bool following(){
		return followFunction;
	}
	public static void updateClock(long value){
		if(startClock != 0){
			long delta = value - startClock;
			clockLabelString = "cpu cycles: "+delta.ToString ("x8");
			currentCycleString = "current cycle: "+value.ToString("x10");
		}
	}
	/*
	 * Parse an instruction and perform the associated command 
	*/
	public static string doInstruct(string line){
		var parts = line.Split ();
		numInstructions++;

		//Debug.Log ("line: " + line);
		string command = null;
		try{
			currentClock = long.Parse(parts[0],System.Globalization.NumberStyles.HexNumber);
			command = parts [2];
			eipLabel = "EIP: 0x"+parts [1];
		}catch(System.FormatException fe){
			if(parts[0] == "break"){
				command = "break";
			}else{
				Debug.Log ("manageControl doInstruct could not parse clock from line "+line);
				Debug.Break ();
				return "error";
			}
		}
		updateClock (currentClock);
		//Debug.Log ("start doInstruct command is " + command);
		uint address;
		uint source;
		uint destination;
		int count;
		Vector3 globalPoint;
		Vector3 pos;
		functionDatabase.functionItem newFunction = null;
		memory sourceRegion;
		memory destinationRegion;
		//newFunction = null;
		// hack to init thisMatrix
		//Matrix4x4 thisMatrix = transform.localToWorldMatrix;
		address = 0;
		if(command != "accept" && command != "break"){
			try{
				address = uint.Parse(parts[4],System.Globalization.NumberStyles.HexNumber);
			}catch(System.Exception){
				Debug.Log ("crap at line "+line);
			}
		}
		int block = 0;
		switch(command)
		{
		case startup.GOTO:
		case "return":
		case "start":
		case "call":
			if(parts.Length < 6){
				Debug.Log("manageControl error parsing operation, no block found in "+line);
				//Debug.Break();
				return "error";
			}
			block = int.Parse (parts [5]);
			if(address == 0){
				Debug.Log ("no address for line: "+line);
				//Debug.Break();
			}
			break;
		}
		//Debug.Log ("line is "+line);
		//if(command == GOTO){
		//
		//  Debug.Log("newFunction now is "+newFunction.name);
		//}

		switch(command)
		{
		case "start":
			startClock = currentClock;
			newFunction = functionDatabase.getFunction(address);
			pos = newFunction.go.transform.position;
			pos.z = funForwardRunning;
			newFunction.go.transform.position = pos;
			currentPoint = newFunction.script.getVertexPosition(block);
			lineVector[lineElement] = currentPoint;
			controlLine.maxDrawIndex = 0;
			controlLine.drawEnd = 0;
			newFunction.didCall();
			//Debug.Log ("start at function "+newFunction.name+" currentpoint is "+currentPoint);
			currentFunction = newFunction;
			break;
		case "break":
			Debug.Log ("GOT A BREAK "+parts[1]);
			startup.doUserPause();
			startup.pauseLabelString = parts[1]+" (Breakpoint)";
			break;
		case "call":
			//if (!validateCall(newFunction)){
			//	isPlaying = false;
			//}
			newFunction = functionDatabase.getFunction(address);
			if(followFunction){
				startup.cameraScript.setObject(newFunction.getPosition());
			}
			if(newFunction.called == 0){
				// first invocation in this call chain, bring the function forward
				pos = newFunction.go.transform.position;
				pos.z = newFunction.z+funForwardRunning;
				newFunction.go.transform.position = pos;
				//newFunction.setShader(startup.shader2);
				//newFunction.setColor(startup.color2);
				//Debug.Log ("set newfun to pos "+pos);
			}
			globalPoint = newFunction.script.getVertexPosition(block);

			lineElement++;
			lineVector[lineElement] = globalPoint;
			//Debug.Log ("call "+newFunction.name+" from "+currentFunction.name+ " from "+currentPoint+" to "+globalPoint+" #pts "+controlLine.maxDrawIndex);
			currentPoint = globalPoint;
			//newFunction.addLine(vl);
			callStack.Add (currentFunction);
			//newFunction.called ++;
			newFunction.didCall();
			drawControl(false);
			//pause();
			//newFunction.callStack = new List<functionItem>(callStack);
			//Debug.Break();
			break;
		case "return":
			newFunction = functionDatabase.getFunction(address);
			if(followFunction){
				startup.cameraScript.setObject(newFunction.getPosition());
			}
			currentPoint = newFunction.script.getVertexPosition(block);

			//currentFunction.called --;
			if(currentFunction.called == 1){
				// last return for this funtion in current call stack, put it backwards (but not all the way)
				pos = currentFunction.go.transform.position;
				pos.z = currentFunction.z+funForwardCalled;
				currentFunction.go.transform.position = pos;
			}
			lineElement = lineElement - currentFunction.currentPoints();
			currentFunction.didReturn ();
			drawControl(false);
			
			callStack.RemoveAt(callStack.Count-1);
			break;
		case startup.GOTO:

			//Debug.Log("goto current function is "+currentFunction.name);
			globalPoint = currentFunction.script.getVertexPosition(block);

			lineElement++;
			lineVector[lineElement] = globalPoint;
			currentPoint = globalPoint;
			currentFunction.didGoto();
			//Debug.Log ("goto "+currentFunction.name+ " to "+block+" pts "+controlLine.maxDrawIndex+" currentPoints is "+currentFunction.currentPoints());
			drawControl(false);
			eipLabel = "EIP: 0x"+address.ToString("x");
			break;
		case "read":
			if(pipeBehavior.sessionNumber == 0)
				return "skip";
			//Debug.Log ("is a read");
			address = uint.Parse (parts[3], System.Globalization.NumberStyles.HexNumber);
			count = int.Parse(parts[4]);
			if(count > 0){
				drawControl(true);
				doRead(address, count, currentPoint);
			}
			//Debug.Log ("back from doRead");
			break;
		case "write":
			if(pipeBehavior.sessionNumber == 0)
				return "skip";
			//Debug.Log ("is a write");
			address = uint.Parse (parts[3], System.Globalization.NumberStyles.HexNumber);
			count = int.Parse(parts[4]);
			if(count > 0){
				drawControl(true);
				doWrite(address, count, currentPoint);
			}
			break;
		case "move_local":
			if(pipeBehavior.sessionNumber == 0)
				return "skip";
			count = int.Parse (parts[4]);
			// TBD hack to speed things up
			drawControl(false);
			if(count > 0){
				destination = uint.Parse(parts[3],System.Globalization.NumberStyles.HexNumber);
				//Debug.Log ("is a move_local to "+destination.ToString("x"));
				Color color = pipeBehavior.getPipeColor();
				destinationRegion = mmap.findMemoryRegion(destination);

				if(destinationRegion)
					destinationRegion.copyBytes(0, destination, destinationRegion, count, currentPoint, color, true);
				else
					Debug.Log ("copy to "+destination.ToString("x")+" assumed write to stack, ignore");
				
			}
			break;
		case "move":
			if(pipeBehavior.sessionNumber == 0)
				return "skip";
			count = int.Parse (parts[5]);
			// TBD hack to speed things up
			drawControl(true);
			if(count > 0){
				source = uint.Parse(parts[3],System.Globalization.NumberStyles.HexNumber);
				destination = uint.Parse(parts[4],System.Globalization.NumberStyles.HexNumber);
				//Debug.Log ("is a move from "+source.ToString("x")+" to "+destination.ToString("x"));
				Color color = pipeBehavior.getPipeColor();
				sourceRegion = mmap.findMemoryRegion(source);
				destinationRegion = mmap.findMemoryRegion(destination);
				//Debug.Log ("current point is "+currentPoint+" function is "+currentFunction.name);
				if(sourceRegion)
				{
					if(destinationRegion){
						sourceRegion.copyBytes(source, destination, destinationRegion, count, currentPoint, color, startup.skipToNetwork);
					}else{
						Debug.Log ("copy to "+destination.ToString("x")+" assumed write to stack, ignore");
					}
				}else{
					// copy from stack
					if(destinationRegion)
						destinationRegion.copyBytes(0, destination, destinationRegion, count, currentPoint, color, true);
					else
						Debug.Log ("copy to "+destination.ToString("x")+" assumed write to stack, ignore");

				}
				//startup.pause(true);
			}else{
				Debug.Log ("skip move");
				command = "skip";
			}
			break;
		case "accept":
			Debug.Log("ACCEPT ******");
			pipeScript.didAccept();
			if(pipeBehavior.sessionNumber == 1){
				mmap.firstSession();
			}
			//GameObject connection = Instantiate (Resources.Load ("connectSpherePrefab")) as GameObject;
			//connectSphere script = (connectSphere) connection.GetComponent(typeof(connectSphere));
			//script.doConnect();
			break;
		case "mmap":
			address = uint.Parse (parts[3], System.Globalization.NumberStyles.HexNumber);
			count = int.Parse (parts[4]);
			mmap.newRegion(address, count);
			//Debug.Log ("got an mmap address "+address.ToString("x")+" count "+count);

			break;
		case "munmap":
			//Debug.Log ("found munmap for address "+address.ToString("x"));
			address = uint.Parse (parts[3], System.Globalization.NumberStyles.HexNumber);
			count = int.Parse (parts[4]);
			mmap.removeRegion(address, count);
			break;
		}
		if(newFunction != null){
			currentFunction = newFunction;
		}
		if(breakpoints.Contains(currentClock)){
			startup.doUserPause();
			startup.pauseLabelString = "breakpoint";
		}
		//startup.doUserPause ();
		return command;
	}
	// for a given function, look at all higher level functions and identify those in the current call chain.
	public bool validateCall(functionDatabase.functionItem fi){
		float newY = fi.y;
		bool retval = true;
		for(int i=0; i<functionDatabase.length (); i++){
			functionDatabase.functionItem tmpf = functionDatabase.getFunction(functionDatabase.getFunctionAddress(i));
			if(tmpf.y >= newY){
				//if(tmpf.callStack.Count > 0){
				//	Debug.Log("validateCall called "+fi.name+" level "+fi.y+" but "+tmpf.name+" has been called at level "+tmpf.level);
				//retval = false;
				//}
			}
		}
		return retval;
	}
	// use the pipeBehavior class to read from the network
	static void doRead(uint to, int count, Vector3 pt){ 
		memory region = mmap.findMemoryRegion (to);
		if(region == null){
		//if(!memoryScript.inRange(to)){
			//Debug.Log ("pipeBehavior read  address out of range "+to.ToString("x"));
			//Debug.Break ();
			return;
		}
		if(to < region.minDataAddress)
			return;
		//Debug.Log ("in doRead to " + to + " count " + count + " pt " + pt);
		pipeScript.read (to, count, region, pt);
		// stop processing instructions until the data completes its trip.
		// see dataBehavior and use of the "caboose" variable.
		startup.pause (false);
		startup.pauseLabelString = "Network Read";
	}
	// use the pipeBehavior class to write to the network
	static void doWrite(uint from, int count, Vector3 pt){ 
		memory region = mmap.findMemoryRegion (from);
		if(region == null){
			//if(!memoryScript.inRange(to)){
			Debug.Log ("pipeBehavior write  address out of range "+from.ToString("x"));
			//Debug.Break();
			return;
		}
		//Debug.Log ("in doRead to " + to + " count " + count + " pt " + pt);
		pipeScript.write (from, count, region, pt);

		startup.pause (false);
		startup.pauseLabelString = "Network Write";
	}


}
