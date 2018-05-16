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
using System;
using System.Xml;
// handle visualization menus.  Also loads/store configuration options.
public class menus : MonoBehaviour
{

	public GUISkin guiSkin;
	public Texture2D background, LOGO;
	public bool DragWindow = true;
	public string levelToLoadWhenClickedPlay = "";
	public string[] AboutTextLines = new string[0];
	public string editableText;
	public int gridSelect = -1;
	public Vector2 scrollPosition;
	public static string clicked = "";
	private Rect WindowRect = new Rect(10, 10, 250, 300);
	//private Rect WindowRect = new Rect((Screen.width / 2) - 100, Screen.height / 2, 200, 200);
	private float volume = 1.0f;
	public bool inHelp = false;
	private Color savedColor;
	private Color workingColor;
	GUIStyle helpStyle;
	Texture2D black;
	static string initialBookmark = null;
	static private GameObject backdrop;
	private void Start()
	{

		helpStyle = new GUIStyle ();
		black = (Texture2D)Resources.Load ("Materials/black") as Texture2D;

		helpStyle.normal.background = black;
		helpStyle.normal.textColor = Color.white;

	}
	private void OnGUI(){
		GUI.skin = guiSkin;
		checkSelect ();
	}
	private void checkSelect()
	{

		if (clicked == "" && !inHelp)
		{
			string pplabel = "Pause";
			if(startup.userPause)
				pplabel = "Play";
			GUILayout.BeginArea(new Rect(5, 5, 150, 100));

			GUILayout.BeginHorizontal();
			if(GUILayout.Button ("Menu")){
				clicked = "menu";
			}else if(GUILayout.Button ("Home")){
				startup.setCamera();
			}else if(GUILayout.Button(pplabel)){
				startup.doUserPause();
			}
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}else if (clicked == "menu"){
			startup.pause (true);

			if (background != null){
				GUI.DrawTexture(new Rect(0,0,Screen.width , Screen.height),background);
			}
			if (LOGO != null && clicked != "about"){
				//GUI.DrawTexture(new Rect((Screen.width / 2) - 100, 30, 200, 200), LOGO);
				GUI.DrawTexture(new Rect((Screen.width / 6) - 100, 30, 200, 200), LOGO);
			}
			WindowRect = GUI.Window(1, WindowRect, menuItemsFunc, "Menu");
		}else if (clicked == "help"){
			Debug.Log ("asked help");
			Application.OpenURL("file://"+startup.helpHome+"/README.html");
			clicked = "";
			/*
			inHelp = true;
			//GUI.Box(new Rect (0,0,Screen.width,Screen.height), helpText.text);
			Debug.Log("clicked help");
			//startup.askedHelp();
			//clicked = "";

			scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height));
			GUILayout.Label(helpText.text, helpStyle);
			if (GUILayout.Button("Close")){
				clicked = "";
				inHelp = false;
				startup.setCamera();
			}

			GUILayout.EndScrollView();
            */
		}else if (clicked == "create bookmark"){
			Debug.Log ("clicked is bookmark");
			WindowRect = GUI.Window(1, WindowRect, newBookmarkFunc, "Bookmark");

		}else if (clicked == "goto bookmark"){
			//Debug.Log ("clicked is goto bookmark");
			WindowRect = GUI.Window(1, WindowRect, goToBookmarkFunc, "Goto Bookmark");
		}else if (clicked == "open project"){
			WindowRect = GUI.Window(1, WindowRect, openProjectFunc, "Open project");
		}else if (clicked == "save config"){
			clicked="";
			saveConfig();
		}else if (clicked == "edit config"){
			WindowRect = GUI.Window(1, WindowRect, editConfig, "Edit configuration");
		}else if (clicked == "edit function color"){
			WindowRect = GUI.Window(1, WindowRect, editFunctionConfig, "Edit function color"); 
		}else if (clicked == "edit background color"){
			WindowRect = GUI.Window(1, WindowRect, editBackgroundColor, "Edit background color");
		}else if (clicked == "edit backdrop color"){
			WindowRect = GUI.Window(1, WindowRect, editBackdropColor, "Edit backdrop color");
		}else if (clicked == "edit text color"){
			WindowRect = GUI.Window(1, WindowRect, editTextColor, "Edit text color");
		}else if (clicked == "edit flow color"){
			WindowRect = GUI.Window(1, WindowRect, editFlowColor, "Edit control color");
		}else if (clicked == "edit session1 color"){
			WindowRect = GUI.Window(1, WindowRect, editCube1Color, "Protected memory color");
		}else if (clicked == "edit session2 color"){
			WindowRect = GUI.Window(1, WindowRect, editCube2Color, "Session 2 color");
		}else if (clicked == "edit memory color"){
			WindowRect = GUI.Window(1, WindowRect, editMemoryColor, "Memory color");
		}else if (clicked == "edit network color"){
			WindowRect = GUI.Window(1, WindowRect, editNetworkColor, "Network color");
		}else if (clicked == "set initial bookmark"){
			WindowRect = GUI.Window(1, WindowRect, setInitialBookmark, "Initial bookmark");
		}
	}


	private void menuItemsFunc(int id){
		if (GUILayout.Button("Help"))
		{
			clicked = "help";
		}else if (GUILayout.Button("Create bookmark")){
			editableText = "bookmarkx";
			clicked = "create bookmark";
		}else if (GUILayout.Button("goto bookmark")){
			gridSelect = -1;
			clicked = "goto bookmark";
		}else if (GUILayout.Button("Open project")){
			clicked = "open project";
		}else if (GUILayout.Button("Save Configuration")){
			clicked = "save config";
		}else if (GUILayout.Button("Edit Configuration")){
			savedColor = startup.color2;
			clicked = "edit config";
		}else if (GUILayout.Button ("Quit")){
			clicked = "";
			Application.Quit();
			Debug.Log ("quit from menu");
		}else if (GUILayout.Button ("Close menu")){
			clicked = "";
		}

	}
	private void newBookmarkFunc(int id){
		editableText = GUILayout.TextField (editableText);
		if(GUILayout.Button("OK")){
			bookmarks.createBookmark (editableText);
			clicked="";
		}
	}
	private void goToBookmarkFunc(int id){
		string[] pArr = (string[])bookmarks.bookmarkList.ToArray();
		
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		
		gridSelect = GUILayout.SelectionGrid(gridSelect, pArr, 1);
		GUILayout.EndScrollView();
		
		if(gridSelect >=0){
			//Debug.Log ("grid select is " + gridSelect);
			clicked = "";
			bookmarks.loadBookmark(bookmarks.bookmarkList[gridSelect]);
		}
		
	}	
	private void setInitialBookmark(int id){
		string[] pArr = (string[])bookmarks.bookmarkList.ToArray();
		
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		
		gridSelect = GUILayout.SelectionGrid(gridSelect, pArr, 1);
		GUILayout.EndScrollView();
		
		if(gridSelect >=0){
			//Debug.Log ("grid select is " + gridSelect);
			clicked = "";
			initialBookmark = bookmarks.bookmarkList[gridSelect];
		}
		
	}
	private void openProjectFunc(int id){
		string[] pArr = (string[])getProjects().ToArray();
		
		//gridSelect = GUILayout.SelectionGrid(new Rect(25, 25, 100, 50), gridSelect, pArr, 1);
		gridSelect = GUILayout.SelectionGrid(gridSelect, pArr, 1);
		if(gridSelect >=0){
			Debug.Log ("grid select is " + gridSelect);
			clicked = "";
			startup.openProject(pArr[gridSelect]);
		}
	}
	public static void loadConfigFromXml(){
		backdrop = GameObject.Find ("backdrop");

		XmlDocument xmlDoc = new XmlDocument();
		string path = startup.home + "/configurations/configuration.xml";
		if(!File.Exists(path)){
			Debug.Log ("no config file, use defaults");
			return;
		}
		xmlDoc.Load(path);
		XmlNode functionColor = xmlDoc.SelectSingleNode ("//configuration/function_color_2");
		startup.color2 = myUtils.colorFromRGBA (functionColor.InnerText);
		functionDatabase.updateColors ();
		XmlNode backgroundColor = xmlDoc.SelectSingleNode ("//configuration/background_color");
		Color bg = myUtils.colorFromRGBA (backgroundColor.InnerText);
		Camera.main.backgroundColor = bg;
		XmlNode bdColor = xmlDoc.SelectSingleNode ("//configuration/backdrop_color");
		if(bdColor != null){
			Color bd = myUtils.colorFromRGBA (bdColor.InnerText);
			backdrop.renderer.material.color = bd;
		}

		XmlNode textColor = xmlDoc.SelectSingleNode ("//configuration/text_color");
		if(textColor != null){
			Color t = myUtils.colorFromRGBA (textColor.InnerText);
			startup.textColor = t;
			startup.updateTextColor ();
		}

		XmlNode flowColor = xmlDoc.SelectSingleNode ("//configuration/flow_color");
		Color f = myUtils.colorFromRGBA (flowColor.InnerText);
		manageControl.controlLine.SetColor(f);
		XmlNode session1Color = xmlDoc.SelectSingleNode ("//configuration/session1_color");
		Color c = myUtils.colorFromRGBA (session1Color.InnerText);
		pipeBehavior.session1Color = c;
		XmlNode session2Color = xmlDoc.SelectSingleNode ("//configuration/session2_color");
		c = myUtils.colorFromRGBA (session2Color.InnerText);
		pipeBehavior.session2Color = c;
		XmlNode memoryColor = xmlDoc.SelectSingleNode ("//configuration/memory_color");
		c = myUtils.colorFromRGBA (memoryColor.InnerText);
		mmap.setMemoryColor (c);
		XmlNode networkColor = xmlDoc.SelectSingleNode ("//configuration/network_color");
		if(networkColor != null){
			c = myUtils.colorFromRGBA (networkColor.InnerText);
			pipeBehavior.setNetworkColor (c);
			pipeBehavior.updateNetworkColor();
		}
		XmlNode bookmark = xmlDoc.SelectSingleNode ("//configuration/initial_bookmark");
		if(bookmark != null){
			initialBookmark = bookmark.InnerText;
			Debug.Log ("load initial bookmark "+initialBookmark);
			bookmarks.loadBookmark(initialBookmark);
		}

		XmlNode functionLabels = xmlDoc.SelectSingleNode ("//configuration/function_labels");
		if(functionLabels != null)
			startup.functionLabels = bool.Parse(functionLabels.InnerText);
		XmlNode protectedReadBreak = xmlDoc.SelectSingleNode ("//configuration/protected_read_break");
		if(protectedReadBreak != null)
			memory.protectedReadBreak = bool.Parse(protectedReadBreak.InnerText);
		XmlNode cameraHomeNode = xmlDoc.SelectSingleNode ("//configuration/camera_home");
		if(cameraHomeNode != null){
			float x = float.Parse(cameraHomeNode.SelectSingleNode("x").InnerText);
			float y = float.Parse(cameraHomeNode.SelectSingleNode("y").InnerText);
			float z = float.Parse(cameraHomeNode.SelectSingleNode("z").InnerText);
			startup.setCameraHome(x, y, z);
		}
		XmlNode cameraRotateNode = xmlDoc.SelectSingleNode ("//configuration/camera_rotate");
		if(cameraRotateNode != null){
			float x = float.Parse(cameraRotateNode.SelectSingleNode("x").InnerText);
			float y = float.Parse(cameraRotateNode.SelectSingleNode("y").InnerText);
			float z = float.Parse(cameraRotateNode.SelectSingleNode("z").InnerText);
			startup.setCameraRotate(x, y, z);
		}
	}
	private void loadConfig(int id){
	}
	private void saveConfig(){
		XmlDocument xmlDoc = new XmlDocument();
		XmlNode rootNode = xmlDoc.CreateElement("configuration");
		xmlDoc.AppendChild(rootNode);
		XmlNode functionColorNode2 = xmlDoc.CreateElement ("function_color_2");
		functionColorNode2.InnerText = startup.color2.ToString();
		rootNode.AppendChild (functionColorNode2);
		XmlNode backgroundColor = xmlDoc.CreateElement ("background_color");
		backgroundColor.InnerText = Camera.main.backgroundColor.ToString();
		rootNode.AppendChild (backgroundColor);
		XmlNode backdropColor = xmlDoc.CreateElement ("backdrop_color");
		backdropColor.InnerText = backdrop.renderer.material.color.ToString();
		rootNode.AppendChild (backdropColor);
		
		XmlNode textColor = xmlDoc.CreateElement ("text_color");
		textColor.InnerText = startup.textColor.ToString();
		rootNode.AppendChild (textColor);

		XmlNode flowColor = xmlDoc.CreateElement ("flow_color");
		flowColor.InnerText = manageControl.controlLine.GetColor(0).ToString();
		rootNode.AppendChild (flowColor);
		XmlNode session1Color = xmlDoc.CreateElement ("session1_color");
		session1Color.InnerText = pipeBehavior.session1Color.ToString();
		rootNode.AppendChild (session1Color);
		XmlNode session2Color = xmlDoc.CreateElement ("session2_color");
		session2Color.InnerText = pipeBehavior.session2Color.ToString();
		rootNode.AppendChild (session2Color);

		XmlNode memoryColor = xmlDoc.CreateElement ("memory_color");
		memoryColor.InnerText = memory.memoryColor.ToString();
		rootNode.AppendChild (memoryColor);

		XmlNode network = xmlDoc.CreateElement ("network_color");
		network.InnerText = pipeBehavior.getNetworkColor().ToString();
		rootNode.AppendChild (network);

		XmlNode functionLabels = xmlDoc.CreateElement ("function_labels");
		functionLabels.InnerText = startup.functionLabels.ToString ();
		rootNode.AppendChild (functionLabels);

		XmlNode protectedRead = xmlDoc.CreateElement ("protected_read_break");
		protectedRead.InnerText = memory.protectedReadBreak.ToString ();
		rootNode.AppendChild (protectedRead);

		XmlNode bookmark = xmlDoc.CreateElement ("initial_bookmark");
		bookmark.InnerText = initialBookmark;
		rootNode.AppendChild (bookmark);

		XmlNode cameraHomeNode = xmlDoc.CreateElement ("camera_home");
		XmlNode x = xmlDoc.CreateElement ("x");
		XmlNode y = xmlDoc.CreateElement ("y");
		XmlNode z = xmlDoc.CreateElement ("z");
		Vector3 cameraHome = startup.getCameraHome ();
		x.InnerText = cameraHome.x.ToString ();
		y.InnerText = cameraHome.y.ToString ();
		z.InnerText = cameraHome.z.ToString ();
		cameraHomeNode.AppendChild (x);
		cameraHomeNode.AppendChild (y);
		cameraHomeNode.AppendChild (z);
		rootNode.AppendChild (cameraHomeNode);

		XmlNode cameraRoateNode = xmlDoc.CreateElement ("camera_rotate");
		x = xmlDoc.CreateElement ("x");
		y = xmlDoc.CreateElement ("y");
		z = xmlDoc.CreateElement ("z");
		Vector3 cameraRoate = startup.getCameraRotate ();
		x.InnerText = cameraRoate.x.ToString ();
		y.InnerText = cameraRoate.y.ToString ();
		z.InnerText = cameraRoate.z.ToString ();
		cameraRoateNode.AppendChild (x);
		cameraRoateNode.AppendChild (y);
		cameraRoateNode.AppendChild (z);
		rootNode.AppendChild (cameraRoateNode);

		string path = startup.home + "/configurations/";
		System.IO.Directory.CreateDirectory (path);

		xmlDoc.Save(path+"configuration.xml");
	}
	private void editConfig(int id){
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		if (GUILayout.Button("Function Color"))
		{
			clicked = "edit function color";
		}else if (GUILayout.Button("Background Color")){
			clicked = "edit background color";
			savedColor = Camera.main.backgroundColor;
			workingColor = Camera.main.backgroundColor;
		}else if (GUILayout.Button("Backdrop Color")){
			clicked = "edit backdrop color";
			savedColor = backdrop.renderer.material.color;
			workingColor = backdrop.renderer.material.color;
		}else if (GUILayout.Button("Text Color")){
			clicked = "edit text color";
			savedColor = startup.textColor;
			workingColor = startup.textColor;
		}else if (GUILayout.Button("Control Flow Color")){
			clicked = "edit flow color";
			savedColor = manageControl.controlLine.GetColor(0);
			workingColor = savedColor;
		}else if (GUILayout.Button("Protected Memory Color")){
			clicked = "edit session1 color";
			savedColor = pipeBehavior.session1Color;
			workingColor = savedColor;
		}else if (GUILayout.Button("Session2 Color")){
			clicked = "edit session2 color";
			savedColor = pipeBehavior.session2Color;
			workingColor = savedColor;
		}else if (GUILayout.Button("Memory Color")){
			clicked = "edit memory color";
			savedColor = memory.memoryColor;
			workingColor = savedColor;
		}else if (GUILayout.Button("Network Color")){
			clicked = "edit network color";
			savedColor = pipeBehavior.getNetworkColor();
			workingColor = savedColor;
		}else if (GUILayout.Button ("Initial Bookmark")){
			gridSelect = -1;
			clicked = "set initial bookmark";
		}else if (GUILayout.Button ("New camera home")){
			startup.updateCameraHome();
			clicked = "";
		}
		startup.functionLabels = GUILayout.Toggle(startup.functionLabels, "Function labels");
		memory.protectedReadBreak = GUILayout.Toggle(memory.protectedReadBreak, "Protected Memory");
		startup.showDebugLabels = GUILayout.Toggle(startup.showDebugLabels, "Debug Labels");
		if(GUILayout.Button ("Close")){
			clicked = "";
		}
		GUILayout.EndScrollView();
		if (DragWindow)
			GUI.DragWindow(new Rect(0, 0, Screen.width, Screen.height));

	}
	private void editFunctionConfig(int id){
		GUILayout.Box("function color");
		GUILayout.Box("red");
		startup.color2.r = GUILayout.HorizontalSlider(startup.color2.r ,0.0f,1.0f);
		GUILayout.Box("green");
		startup.color2.g = GUILayout.HorizontalSlider(startup.color2.g ,0.0f,1.0f);
		GUILayout.Box("blue");
		startup.color2.b = GUILayout.HorizontalSlider(startup.color2.b ,0.0f,1.0f);
		GUILayout.Box("alpha");
		startup.color2.a = GUILayout.HorizontalSlider(startup.color2.a ,0.0f,1.0f);
		functionDatabase.updateColors ();
		GUILayout.Box (startup.color2.ToString ());
		if (GUILayout.Button("OK"))
		{
			clicked = "";
		}
		if (GUILayout.Button("Cancel"))
		{
			startup.color2 = savedColor;
			functionDatabase.updateColors();
			clicked = "";
		}

	}
	private void colorSliders(string title){
		GUILayout.Box(title);
		GUILayout.Box("red");
		workingColor.r = GUILayout.HorizontalSlider(workingColor.r ,0.0f,1.0f);
		GUILayout.Box("green");
		workingColor.g = GUILayout.HorizontalSlider(workingColor.g ,0.0f,1.0f);
		GUILayout.Box("blue");
		workingColor.b = GUILayout.HorizontalSlider(workingColor.b ,0.0f,1.0f);
		GUILayout.Box("alpha");
		workingColor.a = GUILayout.HorizontalSlider(workingColor.a ,0.0f,1.0f);
		GUILayout.Box (workingColor.ToString ());
	}
	private void editBackgroundColor(int id){
		colorSliders ("Background color");
		Camera.main.backgroundColor = workingColor;
		if (GUILayout.Button("OK"))
		{
			clicked = "";
		}
		if (GUILayout.Button("Cancel"))
		{
			Camera.main.backgroundColor = savedColor;
			//functionDatabase.updateColors();
			clicked = "";
		}	
	}
	private void editBackdropColor(int id){
		colorSliders ("Backdrop color");
		backdrop.renderer.material.color = workingColor;
		if (GUILayout.Button("OK"))
		{
			clicked = "";
		}
		if (GUILayout.Button("Cancel"))
		{
			backdrop.renderer.material.color = savedColor;
			clicked = "";
		}	
	}	
	private void editTextColor(int id){
		colorSliders ("Text color");
		startup.textColor = workingColor;
		startup.updateTextColor ();
		if (GUILayout.Button("OK"))
		{
			clicked = "";
		}
		if (GUILayout.Button("Cancel"))
		{
			startup.textColor = savedColor;
			startup.updateTextColor ();
			//functionDatabase.updateColors();
			clicked = "";
		}	
	}
	private void editFlowColor(int id){
		colorSliders ("Control flow color");
		manageControl.controlLine.SetColor(workingColor);
		if (GUILayout.Button("OK"))
		{
			clicked = "";
		}
		if (GUILayout.Button("Cancel"))
		{
			manageControl.controlLine.SetColor (savedColor);
			//functionDatabase.updateColors();
			clicked = "";
		}	
	}
	private void editCube1Color(int id){
		Color colorWas = pipeBehavior.session1Color;
		colorSliders ("Session1 (protected memroy) color");
		pipeBehavior.session1Color = workingColor;
		mmap.setCubeColor(colorWas, workingColor);
		//pipeBehavior.setPipeColor (workingColor);
		if (GUILayout.Button("OK"))
		{
			clicked = "";
			pipeBehavior.updatePipeColor();
		}
		if (GUILayout.Button("Cancel"))
		{
			pipeBehavior.session1Color = savedColor;
			mmap.setCubeColor(workingColor, savedColor);
			//pipeBehavior.setPipeColor (savedColor);
			//functionDatabase.updateColors();
			clicked = "";
		}	
	}
	private void editCube2Color(int id){
		Color colorWas = pipeBehavior.session2Color;
		colorSliders ("Session2 color");
		pipeBehavior.session2Color = workingColor;
		mmap.setCubeColor(colorWas, workingColor);
		//pipeBehavior.setPipeColor (workingColor);
		if (GUILayout.Button("OK"))
		{
			clicked = "";
			pipeBehavior.updatePipeColor();
		}
		if (GUILayout.Button("Cancel"))
		{
			pipeBehavior.session2Color = savedColor;
			mmap.setCubeColor(workingColor, savedColor);
			//pipeBehavior.setPipeColor (savedColor);
			//functionDatabase.updateColors();
			clicked = "";
		}	
	}
	private void editMemoryColor(int id){
		colorSliders ("Memory color");
		mmap.setMemoryColor (workingColor);
		if (GUILayout.Button("OK"))
		{
			clicked = "";
		}
		if (GUILayout.Button("Cancel"))
		{
			clicked = "";
			mmap.setMemoryColor (savedColor);
		}	
	}
	private void editNetworkColor(int id){
		colorSliders ("Network color");
		pipeBehavior.setNetworkColor (workingColor);
		pipeBehavior.updateNetworkColor ();
		if (GUILayout.Button("OK"))
		{
			clicked = "";
		}
		if (GUILayout.Button("Cancel"))
		{
			clicked = "";
			pipeBehavior.setNetworkColor(savedColor);
			pipeBehavior.updateNetworkColor();
		}	
	}
	private void menuFunc(int id)
	{
		//buttons 
		if (GUILayout.Button("menu"))
		{
			//play game is clicked
			//Application.LoadLevel(1);
			clicked = "menu";
		}else{
			if(GUILayout.Button ("close")){
				clicked = "";
				startup.setCamera();
			}
		}
		if (DragWindow)
			GUI.DragWindow(new Rect(0, 0, Screen.width, Screen.height));
	}
	private List<string> getProjects(){
		List<string> projects = new List<string> ();
		DirectoryInfo di = new DirectoryInfo(startup.home);
		
		DirectoryInfo[] oldmarks = di.GetDirectories();
		
		Array.Sort (oldmarks, (x, y) => StringComparer.OrdinalIgnoreCase.Compare (x.LastWriteTime, y.LastWriteTime));
		Debug.Log ("num projects in " + startup.home + " is " + oldmarks.Length);
		for(int i=0; i<oldmarks.Length; i++){
			if(Path.GetFileName(oldmarks[i].Name) != "configurations")
				projects.Add (Path.GetFileName(oldmarks[i].Name));
		}
		return projects;
	}
	private void Update()
	{	
	    if (clicked != "" && Input.GetKeyDown("escape")){
			clicked = "";
			inHelp = false;
			startup.setCamera();
		}
	}

}
