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
/*
 * Provide vertex location information for a function (each vertex corresponds to a basic block).
 * Also manage colliders and report function summary information.
 */
// TBD move function item methods here.  But need to account for instantiating object before we know the number of vertices?
public class functionBehaive : MonoBehaviour {
	public functionDatabase.functionItem function = null;
	static string summary = null;
	static bool useFunctionPosition4Summary = false;
	static GameObject summaryFunction;
	static Texture2D summaryBackground;

	List<VertexInfo> vertices = null;
	private bool hasVertex(Vector3 pt){
		for(int i=0; i<vertices.Count; i++){
			if(vertices[i].pt == pt){
				return true;
			}
		}
		return false;
	}
	public class VertexInfo
	{
		public Vector3 pt;
		public Vector3 worldPt;
		public Transform t;
		public bool hasWorld;
		public VertexInfo(Vector3 pt, Transform t){
			this.pt = pt;
			this.t = t;
			this.hasWorld = false;
			this.worldPt = Vector3.zero;
		}
	}
		//public List<functionItem> callStack = null;
	// Use this for initialization
	void Start () {
		//Debug.Log ("functionBehaive starting, name is " + gameObject.name);
		summaryBackground = (Texture2D) Resources.Load ("Materials/pipeGranite",typeof(Texture2D));
		Texture2D texture = (Texture2D)Resources.Load ("Materials/red", typeof(Texture2D));
		Component[] comps = GetComponents(typeof(Renderer));
		for(int i = 0; i < comps.Length; i++){
			Renderer r = (Renderer) comps[i];
			for(int j = 0; j< r.materials.Length; j++){
				r.materials[j].mainTexture = texture;
			}
			//Debug.Log ("num materials: "+r.materials.Length);
			
			r.material.mainTexture = texture;
		}
		comps = GetComponentsInChildren(typeof(Renderer));
		for(int i = 0; i < comps.Length; i++){
			Renderer r = (Renderer) comps[i];
			for(int j = 0; j< r.materials.Length; j++){
				r.materials[j].mainTexture = texture;
			}
			//Debug.Log ("num materials: "+r.materials.Length);
			
			r.material.mainTexture = texture;
		}
		Mesh mesh;
		if(this.function.subtype == 1){ 
			Component[] comps2 =  GetComponentsInChildren(typeof(MeshFilter));
			mesh = ((MeshFilter)comps2[0]).mesh;
			//this.vertices = mesh.vertices;
			buildVertices(mesh.vertices, comps2[0].transform);
		}else if(this.function.kind == "rhombicPrefab" || this.function.kind == "stellatedPrefab" || this.function.kind == "pentakisPrefab"){
			Component[] comps2 =  GetComponentsInChildren(typeof(MeshFilter));
			vertices = new List<VertexInfo>();
			//if(this.function.kind == "pentakisPrefab")
			//	Debug.Log ("num meshes is "+comps2.Length);
			for(int i=0; i< comps2.Length; i++){
				mesh = ((MeshFilter)(comps2[i])).mesh;
				for(int j=0; j<mesh.vertexCount; j++){
					if(!hasVertex(mesh.vertices[j])){
						VertexInfo vi = new VertexInfo(mesh.vertices[j], comps2[i].transform);
						this.vertices.Add(vi);
					}
				}

			}


		}else{
			mesh = GetComponent<MeshFilter>().mesh;
			//this.vertices = mesh.vertices;
			buildVertices(mesh.vertices, transform);
		}
	}
	void buildVertices(Vector3[] verts, Transform t){
		this.vertices = new List<VertexInfo>();
		for(int i=0; i<verts.Length; i++){
			if(!hasVertex(verts[i])){
				VertexInfo ti = new VertexInfo(verts[i], t);
				this.vertices.Add(ti);
			}
		}

	}
	public Vector3 getVertexPosition(int block){
		Vector3 retval;
		int index = block % (this.vertices.Count - 1);
		if (this.vertices [index].hasWorld) {
			retval = this.vertices [index].worldPt;
		}else{
			Transform t = vertices [index].t;
			Vector3 pt = vertices [index].pt;
			Matrix4x4 thisMatrix = t.localToWorldMatrix;
			retval = thisMatrix.MultiplyPoint3x4(pt);
			vertices[index].worldPt = retval;
			vertices[index].hasWorld = true;
		}
		return retval;

	}
	public void setFunction(functionDatabase.functionItem fi){
		this.function = fi;
	}
	void Awake(){
		//Debug.Log ("from " + transform.position);
	}
	void OnGUI () {
		if(summary != null && (gameObject == summaryFunction)){
			if(Input.GetKeyDown("escape")){
				summary = null;
			}else{
				//Debug.Log ("putting a summary via label");
				GUIStyle style = new GUIStyle ();
				style.normal.background = summaryBackground;
				Vector3 pos;
				if(useFunctionPosition4Summary){
					Vector3 mypos = transform.position;
					SphereCollider sc = (SphereCollider) gameObject.GetComponent(typeof(SphereCollider));			
					mypos.x += sc.bounds.size.x/2;
					mypos.y -= sc.bounds.size.y/4;
					pos = Camera.main.WorldToScreenPoint(mypos);
				}else{
					pos = Input.mousePosition;
				}
				style.normal.textColor = Color.black;
				style.fontSize = 18;
				float y = Screen.height-pos.y;
				GUI.Label(new Rect(pos.x, y, 500, 200), summary, style);	
			}
		}
	}
	public void showSummary(bool replace, bool useFunctionPosition){
		useFunctionPosition4Summary = useFunctionPosition;
		summaryFunction = gameObject;
		if (summary == null || replace) {
			//Debug.Log ("showing funtion summary");
			summary = "Function: "+this.function.name+"\n";
			summary = summary+"function address: 0x" + this.function.address.ToString("x")+"\nnumber of basic blocks: "+this.function.num_blocks;
			//Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
			//Vector3[] vertices = mesh.vertices;
			Vector3 pt = this.function.getPosition();
			summary = summary+"\n kind: "+this.function.kind+" number of vertices: "+this.vertices.Count;
			summary = summary+"\n position "+pt.x+" "+pt.y+" "+pt.z;
			summary = summary+"\n times called: "+this.function.calledTotal;
		}else{
			summary = null;
		}
	}
	public Vector3 getVertex(int i){
		int index = i % (this.vertices.Count - 1);
		return this.vertices [index].pt;
	}


}
