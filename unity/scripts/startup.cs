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
/*
 * Top level functions for managing the visualization.  It reads instructions from the
 * dataset and calls manageControl to perform the operation.  This module also manages
 * the suspension of animation (e.g., between network operations) to speed up playback.
 */

public class startup : MonoBehaviour {
	// local class that manages dataset file
	public static instructions instructs = null;
	// Network object and script
	public static GameObject pipe1;
	static pipeBehavior pipeScript;
	// primary memory object
	static memory memoryScript;
	//public static GameObject pipe2;
	//public static GameObject camera;
	public static maxCamera cameraScript;
	public const string GOTO = "goto";
	public const string READ = "read";
	public const string WRITE = "write";
	public const string MOVE = "move";
	static public Color color1 = Color.yellow;
	static public Color color2 = Color.red;
	static public Shader shader2;
	//color2 = Color.green;
	// are instructions being consumed?
	public static bool isPlaying = false;
	// did the user request a pause?
	public static bool userPause = true;
	// did the user request that we skip instructions until a data operation is found?
	// (also set when just showing data
	static private bool skipToData = false;
	static private bool skipGoto = true;
	// did the user request that always skip non-data instructions?
	static private bool justShowData = false;
	// skip to network read/write?
	static public bool skipToNetwork = false;
	static public string pauseLabelString = "paused";
	private static int userStackFrame = 0;
	public static string errorString = null;
	public static string addressLabel = "";
	public static string eipLabel = "";
	public static Color textColor = Color.white;
	static GUIStyle labelStyle = new GUIStyle ();
	GUIStyle helpStyle = new GUIStyle ();
	public static string helpHome = "./";
	// Home is chaged to a local directory if this is an application (vice development)
	public static string home = "/Volumes/disk2/cgc/cgc/users/mft/simics/visualization/datasets/";
	public static string projectHome; 
	public static string project = "bleed3";
	//public static Vector3 memoryStart = new Vector3 (-14.0f, 30f, -8.7f);
	public static Vector3 memoryStart = new Vector3 (-14.0f, 30f, -2.7f);
	static Vector3 memoryRotation = new Vector3(90,129,0);
	public static bool showDebugLabels = false;
	//static Vector3 memoryRotation = new Vector3(90,149,0);

	//public static Vector3 memoryStart = new Vector3 (-13.0f, 30f, -8.7f);

 	static Vector3 cameraHome = new Vector3 (-2.6f, 24.4f, -45.1f);
	static Vector3 cameraRotateDeg = new Vector3 (0, 350, 0);
	static Quaternion cameraRotate = Quaternion.Euler(Vector3.zero);

	// Use this for initialization
	//uint dum = 10;
	static int guiX = 40;

	public static bool functionLabels = true;

	int frameCount = 0;
	float dt = 0.0f;
	float fps = 0.0f;
	float updateRate = 4.0f;  // 4 updates per sec. (used for reporting FPS)
	/*
	 * Manage information display, including optional debug information.
	 */
	void OnGUI () {

		if(errorString != null){
			helpStyle.normal.textColor = textColor;
			helpStyle.fontSize = 18;
			GUI.Label(new Rect(40, 40, guiX, 300), errorString, helpStyle);
		}
		// Hide labels and network objects if zoomed in so they don't interfer with view of functions.
		if(Camera.main.transform.position.z >-20.0f){
			pipeScript.showStuff(false);
			//pipe1.renderer.enabled = false;
		}else{
			pipeScript.showStuff(true);
			//int x = Screen.width-150;
			if(menus.clicked == ""){
				GUI.Label (new Rect (guiX, 25, guiX+75, 30), pauseLabelString, labelStyle);
				if(addressLabel != ""){
					GUI.Label (new Rect (guiX, 35, guiX+75, 40), addressLabel, labelStyle);
				}
				GUI.Label (new Rect (guiX-20, 55, guiX+75, 60), manageControl.clockLabelString, labelStyle);
				GUI.Label (new Rect (guiX-20, 68, guiX+75, 72), manageControl.eipLabel, labelStyle);
			}

		}
		if(showDebugLabels){		
			GUI.Label (new Rect (guiX-30, 78, guiX+75, 84), manageControl.currentCycleString, labelStyle);
			GUI.Label (new Rect (guiX-20, 88, guiX+75, 95), manageControl.numInstructions.ToString("n0"), labelStyle);
			GUI.Label (new Rect (guiX-20, 105, guiX+75, 110), "fps: "+fps.ToString("n0"), labelStyle);
			string pauseState = "skip to Net: "+skipToNetwork.ToString()+"\n just Show Data "+justShowData.ToString();
			pauseState = pauseState+"\n skipToData: "+skipToData.ToString()+"\nuserPause "+userPause;
			GUI.Label (new Rect (guiX-30, 115, guiX+75, 120), pauseState, labelStyle);
		}
	}
	// Load the configuration from a configuration file (if there is one)
	void Start () {
		Debug.Log ("In startup start\n");
		labelStyle.normal.textColor = textColor;
		menus.loadConfigFromXml ();
		setCamera ();
		Debug.Log ("multitouch enabled? " + Input.multiTouchEnabled);

	}
	public static void updateTextColor(){
		labelStyle.normal.textColor = textColor;
	}
	// for use in bookmarks
	public static void resetInstructionTo(int instructNumber){
		instructs.resetInstructionTo (instructNumber);
	}
	// resume consuming instructions, e.g., after waiting for a memory move animation
	public static void resume(){
		if(!userPause){
			isPlaying = true;
			//Debug.Log ("RESUMED instruction handling at clock: "+manageControl.currentClock.ToString("x"));
			if(justShowData){
				pauseLabelString = "skipping...";
				skipToData = true;
			}else{
				pauseLabelString = "play";
			}
		}
	}

	public static void pause(bool force){
		if(force){
			userPause = true;
		}
		//Debug.Log ("PAUSED instruction handling at clock:" +manageControl.currentClock.ToString("x"));
		isPlaying = false;
		if(!skipToNetwork)
			// if(pauseLabelString == "play" || pauseLabelString == "skipping...")
				pauseLabelString = "paused";
	}
	public static void pauseLabel(string label){
		pauseLabelString = label;
	}
	public static void doUserPause(){
		if(!userPause){
			pauseLabelString = "paused(u)";
		}
		userPause = !userPause;
		if(justShowData && userPause){
			justShowData = false;
			skipToNetwork = false;
			skipToData = false;
			pause(false);
		}else if(isPlaying)
			pause(false);
		else
			resume();
	}

	// look for keyboard shortcuts and clicking of objects
	bool checkKeys(){
		bool retval = true;
		if(menus.clicked != ""){
			return true;
		}
		if( Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt))
		{
			// User clicked button (but not while naviaging via alt key
			//Debug.Log("clicked  in checkKyes");
			Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
			RaycastHit hit;
			
			if( Physics.Raycast( ray, out hit, 100 ) )
			{
				if(Input.GetKey(KeyCode.LeftShift)){
				    // user seeks information about an object
					functionBehaive funScript = (functionBehaive) hit.transform.gameObject.GetComponent(typeof(functionBehaive));
					if(funScript != null){
						funScript.showSummary(false, false);
					}else{
						Debug.Log ("perhaps hit data? "+hit.transform.gameObject.name);
						dataBehaviorNew dataScript = (dataBehaviorNew) hit.transform.gameObject.GetComponent(typeof(dataBehaviorNew));
						if(dataScript != null){
							dataScript.showSummary();
							addressLabel = "address: 0x"+dataScript.address.ToString("x");
						}
					}
				}else{
					// if function is clicked, zoom to it.
					Debug.Log( "HIT SOMETHING "+hit.transform.gameObject.name );
					functionBehaive funScript = (functionBehaive) hit.transform.gameObject.GetComponent(typeof(functionBehaive));
					if(funScript != null)
						cameraScript.setObject(funScript.function.getPosition());
				}
			}
		}else if(Input.GetMouseButtonUp(0)){
			addressLabel = "";
		}else if (Input.GetKeyDown("space")){
			doUserPause();
			//print("space key was pressed");
		//}else if (Input.GetKeyDown("escape")){
		//	//print("escape key was pressed");
		//	helpString = null;
		//	//cameraScript.restore();
		//	setCamera();
		}else if (Input.GetKeyDown("w") && Input.GetKey(KeyCode.LeftShift)){
			cameraScript.moveZ(1);
		}else if (Input.GetKeyDown("s") && Input.GetKey (KeyCode.LeftShift)){
			cameraScript.moveZ (-1);
		}else if (Input.GetKeyDown ("w")){
			cameraScript.moveY (1);
		}else if (Input.GetKeyDown("s")){
			cameraScript.moveY(-1);
		}else  if (Input.GetKeyDown("a")){
			cameraScript.moveX (1);
		}else if (Input.GetKeyDown("d")){
			cameraScript.moveX(-1);
//		}else if (Input.GetKeyDown("r") && (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift))){
		}else if (Input.GetKeyDown("r") && (Input.GetKey (KeyCode.LeftShift))){
			if(!justShowData){
				justShowData = true;
				skipToData = true;
				pauseLabelString = "skipping";
			}else{
				justShowData = false;
			}
		}else if(Input.GetKeyDown("f")){
		
			manageControl.follow (!manageControl.following());
		}else if (Input.GetKeyDown("r")){
			if(isPlaying){
				skipToData = true;
				pauseLabelString = "skipping";
				//Debug.Log("Skip to data at clock "+currentClock);
			}
		}else if (Input.GetKeyDown("h")){
			setCamera();
		}else if (Input.GetKeyDown("i")){
			if(!skipToNetwork){
				justShowData = true;
				skipToData = true;
				pauseLabelString = "skipping";

			}else{
				justShowData = false;
				skipToData = false;
				if(!userPause)
					pauseLabelString = "play";
			}
			skipToNetwork = !skipToNetwork;
		}else if (Input.GetKeyDown ("b")){
			pause(true);
			userStackFrame = manageControl.callStack.Count-1;
			functionDatabase.functionItem fi = manageControl.currentFunction;
			cameraScript.setObject(fi.getPosition());
			if(functionLabels)
				fi.script.showSummary(true, true);
		}else if (Input.GetKeyDown ("n") && (Input.GetKey (KeyCode.LeftShift))){
			if(userStackFrame < manageControl.callStack.Count-1){
				functionDatabase.functionItem fi = manageControl.callStack[userStackFrame];
				cameraScript.setObject(fi.getPosition());
				userStackFrame ++;
				if(functionLabels)
					fi.script.showSummary(true, true);
			}
		}else if (Input.GetKeyDown ("n")){
			if(userStackFrame >= 0){
				functionDatabase.functionItem fi = manageControl.callStack[userStackFrame];
				cameraScript.setObject(fi.getPosition());
				if(userStackFrame > 0)
					userStackFrame --;
				if(functionLabels)
					fi.script.showSummary(true, true);

			}
		}else if(Input.GetKeyDown("m")){
			pause(true);
			menus.clicked = "create bookmark";
			//createBookmark();

		}else if(Input.GetKeyDown ("l")){
			pause(true);
			menus.clicked = "goto bookmark";

		}else{
			retval = false;
		}
		return retval;
	}

	void cameraToObject(Vector3 pt){
		cameraScript.setObject (pt);
	}
	// for reporting on frames-per-second
	void checkFrames(){
		frameCount++;
		dt += Time.deltaTime;
		if (dt > 1.0f/updateRate)
		{
			fps = frameCount / dt ;
			frameCount = 0;
			dt -= 1.0f/updateRate;
		}
	}
	// Update is called once per frame
	void Update () {
		//Debug.Log ("empty frame");
		if(showDebugLabels)
			checkFrames ();
		//frameCount++;
		uint skipCount = 0;
		bool done = false;
		string cmd;
		// Normally, one operation is displayed per frame.  If skipping, then perform multiple operations in this frame,
		// e.g., until a data operation is reached.
		if(skipToNetwork)
			skipToData = true;
		while(!done && isPlaying){
			//Debug.Log ("isPlaying nust be true");
			skipCount++;
			string ins = instructs.getInstruction();
			if(ins != null && ins.Length > 0){
				cmd = manageControl.doInstruct(ins);
				if(cmd == "skip")
					continue;
				//Debug.Log ("command "+cmd+" at "+manageControl.currentClock.ToString("x"));
				if(skipGoto && cmd != GOTO){
					if(skipToData){
						if(cmd == READ || cmd == WRITE || (cmd == MOVE && !skipToNetwork)){
							skipToData = false;
							//Debug.Log ("got a data command "+cmd+" at "+manageControl.currentClock.ToString("x"));
							done = true;
						}else{
						}
					}else{
						done = true;
					}
				}else{
						if(!skipToData && !justShowData){
						done = true;
					}
				}
				if(skipCount > 10000){
					done = true;
				}else if((skipCount % 1000)==0){
					if(checkKeys())
						return;
				}
			}else{
				Debug.Log ("NO MORE INSTRUCTIONS");
				justShowData = false;
				skipToData = false;
				doUserPause();
				pauseLabelString = "Finished";
				done = true;
			}
		}
		//Debug.Log ("out of loop");
		checkKeys ();


	}

	/*
	 * Instantiate network and memory objects and initialize project
	 */
	void Awake(){
		//camera = GameObject.Find ("Main Camera");
		color1.a = 0.4f;
		color2.a = 0.4f;
		shader2 = Shader.Find("Transparent/VertexLit");
		cameraScript = (maxCamera) Camera.main.GetComponent(typeof(maxCamera));
		Debug.Log ("i am awake\n");

		//pipe1 = Instantiate (Resources.Load ("networkPrefab")) as GameObject;
		pipe1 = Instantiate (Resources.Load ("spherePrefab")) as GameObject;
		pipe1.name = "theNetwork";

		pipeScript = (pipeBehavior) pipe1.GetComponent(typeof(pipeBehavior));
		GameObject mem = GameObject.Find ("Memory1");
		memoryScript = (memory) mem.GetComponent(typeof(memory));
		mem.transform.position = memoryStart;
		mem.transform.rotation = Quaternion.Euler (memoryRotation);
		initProject (project);

	}
	private static void initProject(string projectName){
		project = projectName;
		projectHome = home + project;
		
		if(!Application.isEditor){
			home = Application.dataPath;
			helpHome = home;
			projectHome = home+"/"+project;
		}
		helpText.initHelp (helpHome);
		functionDatabase.doInit (projectHome + "/functionList.txt");
		manageControl.doInit (pipeScript, projectHome);
		instructs = new instructions (projectHome + "/combined.txt");
		isPlaying = false;
		pauseLabelString = "paused";
		bookmarks.doInit (projectHome, instructs, pipeScript, memoryScript);
		memoryScript.doInit ();
		pipeScript.initNetwork ();
		mmap.doInit ();
		setCamera ();

	}
	public static void updateCameraHome(){
		cameraHome = Camera.main.transform.position;
		Quaternion rotate = new Quaternion ();
		rotate = Camera.main.transform.rotation;
		//cameraRotate = new Vector3 (rotate.x, rotate.y, rotate.z);
		cameraRotate = rotate;
	}
	public static void setCameraHome(float x, float y, float z){
		cameraHome = new Vector3 (x, y, z);
		setCamera ();
	}
	public static void setCameraRotate(float x, float y, float z){
		Debug.Log ("Setting camera rotate to " + x + " " + y + " " + z);
		cameraRotate.x = x;
		cameraRotate.y = y;
		cameraRotate.z = z;
		setCamera ();
	}
	public static Vector3 getCameraRotate(){
		Quaternion rotate = new Quaternion ();
		rotate = Camera.main.transform.rotation;
		Debug.Log ("return rotate of " + rotate.x + " " + rotate.y + " " + rotate.z);
		return new Vector3 (rotate.x, rotate.y, rotate.z);
	}

	public static Vector3 getCameraHome(){
		return cameraHome;
	}
	public static void setCamera(){

		//Vector3 fixedPt = new Vector3 (-2.6f, 24.4f, -43.1f);
		//Vector3 fixedPt = new Vector3 (-2.6f, 24.4f, -45.1f);
		//camera = GameObject.Find ("Main Camera");
		//Quaternion rotate = new Quaternion ();
		//rotate = Camera.main.transform.rotation;
		//rotate.y = 350;
		//rotate.x = 0;
		//rotate.z = 0;
		Camera.main.transform.position = cameraHome;
		if(cameraRotate.x == 0 && cameraRotate.y == 0){
		//if(cameraRotate == Quaternion.Euler(Vector3.zero)){
				Debug.Log("using camera degrees,cameraRotate x is "+cameraRotate.x);
			Camera.main.transform.rotation = Quaternion.Euler(cameraRotateDeg);
		}else{
			Camera.main.transform.rotation = cameraRotate;
		}
		//camera.transform.rotation = rotate;
		//fixedPt.z +=10;
		//fixedPt.x += 10;
		//Debug.Log ("camera to " + fixedPt);
		cameraScript.setPosition (cameraHome);
	}
	public static void openProject(string projectName){
		initProject (projectName);
	}


	public class instructions
	{
		StreamReader reader = null;
		FileStream fstream;
		string fname;
		public instructions(string fname){
			this.fname = fname;
			fstream = File.OpenRead(fname);
			try{
				reader = new StreamReader (fstream);
			}catch(FileNotFoundException e){
				errorString = e.ToString();
			}
		}

	    public string getInstruction(){
			string line;
			if(reader == null)
				return null;
			//line = reader.ReadLine();
			line = reader.ReadLine ();

			if(line != null && (line.Contains("call") || line.Contains("return"))){
				//Debug.Log ("instruct line: " + line);
			}

			return line;
		}
		// getPosition and setPostion are not used; had trouble getting that to work.  use explicity instruction counts
		public long getPosition(){
			Debug.Log ("getPostion returns " + fstream.Position);
			return fstream.Position;
		}
		public void setPosition(long position){
			//fstream.Seek (position, SeekOrigin.Begin);
			fstream.Position = position;
			Debug.Log ("setPosition to "+position);
		}
		public void resetInstructionTo(int intructionNumber){
			fstream.Close ();
			reader.Close ();
			fstream = File.OpenRead(this.fname);
			try{
				reader = new StreamReader (fstream);
			}catch(FileNotFoundException e){
				errorString = e.ToString();
			}
			for(int i=0; i< intructionNumber; i++){
				reader.ReadLine();
			}
		}

	}

}
