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
using Vectrosity;
using System.IO;
using System.Xml;


/*
 *  memory visualization is a simple 2d plane representing the heap.  Memory addresses are translated into grid coordinates using
 *  values read from a ranges.txt file.  The mmap operation results in an additional plane.
 * This class handles the copying of memory values, through use of the dataBehavior class.  Network access to memory is in pipBehavior.
 */
public class memory : MonoBehaviour {
	private int xstart;
	private int zstart;
	public static float cubeSizeX;
	public static float cubeSizeZ;
	private float scaledCubeX;
	private float scaledCubeZ;
	public int bytesPerRow;
	public int bytesPerCube;
	public int cubesPerRow;
	public int numRows;
	// the data objects in this memory plane
	private GameObject[] cubes;
	public int maxCubes = 1000000;
	private Vector3[][] cubePoints;
	public uint minDataAddress = 0;
	public uint maxDataAddress = 0;
	public static Color memoryColor = Color.gray;
	public  bool readProtectedData = false;
	private bool waitForUnpause = false;
	private dataBehaviorNew recentCubeScript = null;
	// track movement of data during a move operation
	private int currentCount = 0;
	private int origCount = 0;
	private uint currentSource;
	private uint currentDestination;
	private memory destinationRegion;
	private Vector3 vertex;
	private Color currentColor;
	// whether copies result in flying data or instant copies
	private bool noFly;
	// bss values are used to scale subsequent memory planes
	private static uint bssSize;
	private static int bssCubesPerRow;
	private static int bssNumRows;
	private static int bssBytesPerCube;
	// re-use data objects rather than destroying them (performance)
	private static List<GameObject> availableData;
	// stop simulation on very first protected memory read?
	public static bool protectedReadBreak;
	// Use this for initialization
	void Start () {

		updateColor ();
		availableData = new List<GameObject> ();
		//prebuildData (10000);
	}
	private static void prebuildData(int count){
		for(int i=0; i<count; i++){
			GameObject myData = Instantiate (Resources.Load ("dataPrefab")) as GameObject;
			myData.SetActive(false);
			//myData.renderer.collider.enabled = false;
			dataBehaviorNew script = (dataBehaviorNew) myData.GetComponent(typeof(dataBehaviorNew));
			script.enabled = false;
			availableData.Add(myData);
		}
	}
	public static GameObject getFreeData(){
		if(availableData.Count == 0){
			//prebuildData(10000);
			GameObject myData = Instantiate (Resources.Load ("dataPrefab")) as GameObject;
			return myData;
		}
		GameObject go =  availableData[availableData.Count-1];
		availableData.RemoveAt(availableData.Count-1);
		go.SetActive (true);
		//go.renderer.collider.enabled = false;
		dataBehaviorNew script = (dataBehaviorNew) go.GetComponent(typeof(dataBehaviorNew));
		script.enabled = true;
		return go;
	}
	public static void freeData(GameObject data){
		Destroy (data);
		return;
		data.SetActive (false);
		//data.renderer.collider.enabled = false;
		dataBehaviorNew script = (dataBehaviorNew) data.GetComponent(typeof(dataBehaviorNew));
		script.reset ();
		script.enabled = false;
		availableData.Add (data);
	}
	public void updateColor(){
		gameObject.renderer.material.color = memoryColor;
	}
	// called for the initial memory plane, intended to be the initial heap allocation.  Subseqent 
	// mmaps result in calls to setRange
	public void doInit(){
		// figure out bytes per cube, cubes per row, etc.
		StreamReader reader = null;
		Vector3 scale = transform.localScale;
		Component c = GetComponent (typeof(MeshFilter));
		Mesh m = ((MeshFilter)c).mesh;
		// x, y and z are the size of the memory plane
		float x = m.bounds.size.x * scale.x;
		float y = m.bounds.size.y * scale.y;
		float z = m.bounds.size.z * scale.z;
		GameObject myData = Instantiate (Resources.Load ("dataPrefab")) as GameObject;
		c = myData.GetComponent (typeof(MeshFilter));
		m = ((MeshFilter)c).mesh;
		scale = myData.transform.localScale;
		float cubeX = m.bounds.size.x * scale.x;
		float cubeZ = m.bounds.size.z * scale.z;
		Debug.Log ("cube x " + cubeX + " cube z " + cubeZ+" cube scale x is "+scale.x);
		Destroy(myData);
		try{
			reader = new StreamReader (startup.projectHome+"/ranges.txt");
		}catch(FileNotFoundException e){
			startup.errorString = "No ranges file";
			Debug.Log ("No ranges file");
			//Debug.Break();
		}
		string line;
		if(reader != null){
			using (reader)
			{
				while ((line = reader.ReadLine()) != null) {
					string[] parts = line.Split(':');
					if(parts[0].Trim() == "min data"){
						minDataAddress = uint.Parse(parts[1],System.Globalization.NumberStyles.HexNumber);
					}else if(parts[0].Trim() == "max data"){
						maxDataAddress = uint.Parse(parts[1],System.Globalization.NumberStyles.HexNumber);
					}
				}
				reader.Close ();
			}
		}
		bssSize = maxDataAddress - minDataAddress + 1;
		cubeSizeX = cubeX;
		cubeSizeZ = cubeZ;
		cubesPerRow = (int)(x / cubeX);
		if(x % cubeX != 0)
			cubesPerRow++;
		numRows = (int) (z / cubeZ);
		if(z % cubeZ != 0)
			numRows ++;
		bytesPerRow = (int)(bssSize / numRows);
		if(bssSize % numRows != 0)
			bytesPerRow++;
		bytesPerCube = (int)(bytesPerRow / cubesPerRow);
		if(bytesPerRow % cubesPerRow != 0)
			bytesPerCube++;
		Debug.Log ("size "+bssSize+" cubesPerRow " + cubesPerRow + " numRows " + numRows + " bytesPerRow " + bytesPerRow + " bytes per cube " + bytesPerCube);
		initCubes ();
		bssCubesPerRow = cubesPerRow;
		bssNumRows = numRows;
		bssBytesPerCube = bytesPerCube;
		clearCubes ();
		updateColor ();
		xstart = -1 * cubesPerRow / 2;
		int xend = cubesPerRow / 2;
		zstart = -1 * numRows / 2;
		int yend = numRows / 2;
		scaledCubeX = cubeX / transform.localScale.x;
		scaledCubeZ = cubeZ / transform.localScale.z;


	}
	// intnteded to be called to initialize memory regions resulting from mmap calls.
	// placement is managed by the mmap.cs class.
	public void setRange(uint min, uint max){
		Debug.Log ("memory set range for " + gameObject.name + " min " + min.ToString ("x"));
		minDataAddress = min;
		maxDataAddress = max;
		bytesPerCube = bssBytesPerCube;
		uint size = maxDataAddress - minDataAddress + 1;
		int numCubes = (int) (size / bssBytesPerCube);
		if(size % bytesPerCube != 0){
			numCubes++;
		}
		// TBD put them in 1K rows for now.
		Debug.Log ("size is " + size + " numCubes is " + numCubes);
		//cubesPerRow = 1024 / bytesPerCube;
		cubesPerRow = 256 / bytesPerCube;
		numRows = numCubes / cubesPerRow;
		if(numCubes % cubesPerRow !=0){
			numRows++;
		}
		xstart = -1 * cubesPerRow / 2;
		zstart = -1 * numRows / 2;
		cubes = new GameObject[maxCubes];
		cubePoints = new Vector3[cubesPerRow][];
		for(int i=0; i<cubesPerRow; i++){
			cubePoints[i] = new Vector3[numRows];
			for(int j=0; j<numRows; j++){
				cubePoints[i][j] = Vector3.zero;
			}
		}
		float xratio = 1.0f*cubesPerRow/bssCubesPerRow;
		float zratio = 1.0f * numRows / bssNumRows;
		//Debug.Log ("xratio " + xratio + " zratio " + zratio);
		Vector3 scale = transform.localScale;
		scale.x =  scale.x*xratio;
		//scale.y = ratio;
		scale.z = scale.z*zratio;
		transform.localScale = scale;
		scaledCubeX = cubeSizeX / transform.localScale.x;
		scaledCubeZ = cubeSizeZ / transform.localScale.z;
		updateColor ();

	}
	public bool inRange(uint address){
		if(address >= minDataAddress && address <= maxDataAddress){
			return true;
		}else{
			return false;
		}
	}
	void initCubes(){
		cubes = new GameObject[maxCubes];
		cubePoints = new Vector3[cubesPerRow][];
		for(int i=0; i<cubesPerRow; i++){
			cubePoints[i] = new Vector3[numRows];
			for(int j=0; j<numRows; j++){
				cubePoints[i][j] = Vector3.zero;
			}
		}
	}
	public void remove(){
		for(int i=0; i<cubes.Length; i++){
			if(cubes[i] != null){
				freeData(cubes[i]);
			}
		}
	}
	private uint getScaled(uint address){
		address = address - minDataAddress;
		//address = address - 0x828fe68;
		uint scaledAddress = (uint)(address / bytesPerCube +0.5f);
		return scaledAddress;
	}
	public void setCubeColor(Color oldColor, Color newColor){
		for(int i=0; i<cubes.Length; i++){
			if(cubes[i] != null && cubes[i].renderer.material.color == oldColor){
				cubes[i].renderer.material.color = newColor;
			}
		}
	}
	public GameObject getCube(uint address){
		if(!inRange(address)){
			//Debug.Log ("memory getCube address out of range "+address.ToString("x"));
			return null;
		}
		uint scaled = getScaled (address);
		if(scaled <0 || scaled >= cubes.Length){
			Debug.Log("Address "+address.ToString("x")+" scaled to "+scaled+" which is out of range for cubes");
			//Debug.Break();
			return null;
		}
		return cubes [scaled];
	}
	public bool putCube(uint address, GameObject cube){
		if(!this.inRange(address)){
			Debug.Log ("memory putCube address out of range "+address.ToString("x")+" region: "+gameObject.name);
			return false;
		}
		uint scaled = this.getScaled (address);
		this.cubes [scaled] = cube;
		return true;
	}
	private void clearCubes(){
		currentCount = 0;
		origCount = 0;
		for(int i=0; i<cubes.Length; i++){
			if(cubes[i] != null){
				cubes[i].renderer.enabled = false;
				freeData(cubes[i]);
				cubes[i] = null;
			}
		}
	}
	public void toXML(ref XmlDocument xmlDoc, ref XmlNode parent){
		XmlNode memoryNode = xmlDoc.CreateElement ("memory");
		XmlNode nameNode = xmlDoc.CreateElement ("name");
		nameNode.InnerText = gameObject.name;
		memoryNode.AppendChild (nameNode);
		XmlNode protectedNode = xmlDoc.CreateElement ("read_protected_data");
		protectedNode.InnerText = readProtectedData.ToString();
		memoryNode.AppendChild (protectedNode);
		XmlNode session1Color = xmlDoc.CreateElement ("session1_color");
		session1Color.InnerText = pipeBehavior.session1Color.ToString ();
		XmlNode session2Color = xmlDoc.CreateElement ("session2_color");
		session2Color.InnerText = pipeBehavior.session2Color.ToString ();
		memoryNode.AppendChild (session1Color);
		memoryNode.AppendChild (session2Color);

		for(int i=0; i<cubes.Length; i++){
			if(cubes[i] != null){
				dataBehaviorNew script = (dataBehaviorNew) cubes[i].GetComponent(typeof(dataBehaviorNew));
				script.toXML(ref xmlDoc, ref memoryNode, i);
				//Debug.Log ("memory toXML saved cube "+i);
			}
		}
		parent.AppendChild (memoryNode);
	}
	public void fromXml(XmlDocument xmlDoc){
		clearCubes ();
		XmlNode memoryNode = xmlDoc.SelectSingleNode ("//bookmark/memory");
		XmlNode nameNode = memoryNode.SelectSingleNode ("name");
		gameObject.name = nameNode.InnerText;
		XmlNode protectedNode = memoryNode.SelectSingleNode ("read_protected_data");
		if(protectedNode != null)
			readProtectedData = bool.Parse (protectedNode.InnerText);
		Color s1 = pipeBehavior.session1Color;
		Color s2 = pipeBehavior.session2Color;
		XmlNode session1Color = memoryNode.SelectSingleNode ("session1_color");
		if(session1Color != null){
			s1 = myUtils.colorFromRGBA (session1Color.InnerText);
			XmlNode session2Color = memoryNode.SelectSingleNode ("session2_color");
			s2 = myUtils.colorFromRGBA (session2Color.InnerText);
		}
		XmlNodeList cubeNodes = memoryNode.SelectNodes ("cube");
		//Debug.Log ("memory fromXML # cubes is " + cubeNodes.Count);
		for(int i=0; i<cubeNodes.Count; i++){
			GameObject myData = getFreeData();
			//GameObject myData = Instantiate (Resources.Load ("dataPrefab")) as GameObject;
			dataBehaviorNew script = (dataBehaviorNew) myData.GetComponent(typeof(dataBehaviorNew));
			int index = script.fromXML(cubeNodes[i]);

			script.enabled = false;
			//myData.collider.enabled = false;
			//myData.collider2D.enabled = false;
			Vector3 pos = this.getAddressLocation(script.address);
			myData.transform.position = pos;
			//Debug.Log ("memory fromXML loaded cube "+index);
			cubes[index] = myData;
		}
		if(session1Color != null){
			setCubeColor (s1, pipeBehavior.session1Color);
			setCubeColor (s2, pipeBehavior.session2Color);
		}
	}
	// Update is called once per frame.  Manage memory copy operations.
	void Update () {
		if(waitForUnpause){
			if(!startup.userPause){
				waitForUnpause = false;
			}else{
				//Debug.Log ("wait for unpause, returing");
				return;
			}
		}
		Color color;
		int bundle = 0;
		// bundle some number of data objects into a single frame
		while(this.currentCount > 0 && bundle < dataBehaviorNew.reduce){
			bool done = false;
			while(!done){
				GameObject cube = getCube(this.currentSource);
				if(cube != null){
					color  = cube.renderer.material.color;
					if(protectedReadBreak && color != pipeBehavior.getPipeColor() && !readProtectedData){
						Debug.Log ("Read of protected data");
						startup.doUserPause();
						startup.pauseLabelString = "Protected Data Read";
						readProtectedData = true;
						waitForUnpause = true;
						cube.renderer.material.color = Color.black;
					}
				}else{
					//Debug.Log ("move update no cube at currentSource "+this.currentSource.ToString("x"));
					color = this.currentColor;
					//done = true;
				}
				// is the data move's destination going to overwrite existing data?
				GameObject replace = this.destinationRegion.getCube(this.currentDestination);
				Vector3 destination = this.destinationRegion.getAddressLocation (this.currentDestination);
				Vector3 source = getAddressLocation (this.currentSource);
				float randY = Random.Range (0, 2);
				source.y = source.y + randY;
				GameObject myData = getFreeData();
				myData.renderer.material.color = color;
				myData.transform.position = source;
				dataBehaviorNew script = (dataBehaviorNew) myData.GetComponent(typeof(dataBehaviorNew));
				dataBehaviorNew.FlyType[] flyPoints = new dataBehaviorNew.FlyType[3];
				float speed = 30f;
				flyPoints[0] = new dataBehaviorNew.FlyType(source, speed, false, Vector3.zero);
				flyPoints[1] = new dataBehaviorNew.FlyType(vertex, speed, false, Vector3.zero);
				flyPoints[2] = new dataBehaviorNew.FlyType(destination, speed, false, Vector3.zero);
				bool caboose = (this.currentCount <= bytesPerCube);
				// save time by instantly handling small moves.  TBD, move to configuration data
				if(origCount > 10 && !noFly){
					script.flyTo (flyPoints, caboose, false, false, replace, (uint)(this.currentDestination+bundle), -1, Color.clear, 3.0f);
				}else{
					//Debug.Log ("small move, just put cube, destination "+destination);
					if(replace != null){
						freeData(replace);
					}
					script.quickCopy(destination, this.currentDestination);
				}
				if(!this.destinationRegion.putCube(this.currentDestination, myData)){
					Debug.Log ("failed putting cube to destination "+this.currentDestination.ToString("x"));
					Debug.Break ();
					return;
				}
				//Debug.Log ("memory update put cube to "+this.currentDestination.ToString("x"));
				recentCubeScript = script;
				if(!noFly)
					done = true;
				this.currentSource = this.currentSource + (uint) bytesPerCube;
				this.currentDestination = this.currentDestination + (uint) bytesPerCube;
				this.currentCount = this.currentCount - bytesPerCube;
				// if this is the last data object in a copy, set the caboose flag to unpause
				// upon completion of the copy.
				if(this.currentCount <= 0){
					if(!noFly){
						if(recentCubeScript != null){
							recentCubeScript.setCaboose();
						}else{
							startup.resume();
						}
					}
					done = true;
				}
				bundle++;
			}

		}

	}	
	// Given a memory address, determine the corresponding location on the memory plane, and then
	// the resulting world coordinate.
	public Vector3 getAddressLocation(uint address){
		if(address <= minDataAddress)
			return Vector3.zero;
		uint scaledAddress = getScaled (address);
		int z = (int) (scaledAddress / cubesPerRow);
		int x = (int) (scaledAddress % cubesPerRow);
		//Debug.Log ("scaled adress for " + address.ToString("x") + " is " + scaledAddress.ToString("x") + " cubesPerRow was " + cubesPerRow +"num rows" +numRows);
		//Debug.Log ("x " + x + " z " + z);


		float zLoc = (zstart + z)*scaledCubeZ;
		float xLoc = (xstart + x)*scaledCubeX;
		if(cubePoints[x][z] == Vector3.zero){
			float xOverZ = transform.localScale.x / transform.localScale.z;
			Vector3 lpos = new Vector3 (xLoc, 0, zLoc);
			//Debug.Log ("xloc "+xLoc+" zLoc "+zLoc);
			Matrix4x4 thisMatrix = transform.localToWorldMatrix;
			Vector3 worldPoint = thisMatrix.MultiplyPoint3x4(lpos);
			cubePoints[x][z] = worldPoint;
		}
		return cubePoints[x][z];

	}
	// external function to initiate the copying of data objects from one memory address to a destination.
	public void copyBytes(uint source, uint destination, memory destinationRegion, int count, Vector3 vertex, Color color, bool noFly){
		//Debug.Log ("copyBytes from " + source.ToString ("x") + " to " + destination.ToString ("x") + " count "+count);
		/*
		if(!inRange(source) || !destinationRegion.inRange(destination)){
			Debug.Log ("memory copyBytes address out of range "+source.ToString("x")+" to "+destination.ToString("x"));
			//Debug.Break ();
			return;
		}
		*/
		if(!noFly){
			startup.pauseLabel ("Memory Copy");
			startup.pause (false);
		}
		this.currentSource = source;
		this.currentDestination = destination;
		this.vertex = vertex;
		this.recentCubeScript = null;
		this.currentCount = count;
		this.origCount = count;
		this.currentColor = color;
		this.noFly = noFly;
		this.destinationRegion = destinationRegion;
		//Debug.Log ("copyBytes "+count+" to " + currentDestination.ToString ("x") + " of " + destinationRegion.name);
		//Debug.Log ("copyBytes currentCount " + this.currentCount);
		// If a quick copy, don't wait until next frame because we may be skipping animation.
		if(noFly)
			Update();
	}
}
