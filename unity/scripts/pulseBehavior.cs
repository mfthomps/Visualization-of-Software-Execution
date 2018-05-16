using UnityEngine;
using System.Collections;

public class pulseBehavior : MonoBehaviour {
	float size;
	float inc;
	Mesh mesh;
	bool done = false;
	// Use this for initialization
	void Start () {
	
	}
	public void doPulse(Vector3 loc, float size, float inc, Color color){
		transform.localScale = Vector3.zero;
		gameObject.renderer.material.color = color;
		transform.position = loc;
		this.size = size;
		this.inc = inc;

	}
	// Update is called once per frame
	void Update () {
		if(done)
			return;
		Vector3 scale = transform.localScale;
		Component[] comps2 =  GetComponents(typeof(MeshFilter));
		mesh = ((MeshFilter)comps2[0]).mesh;
		float sizeNow = mesh.bounds.size.x * scale.x;
		//Debug.Log ("pulse size is " + this.size + " mesh x is " + sizeNow);
		if(sizeNow > size){
			Destroy(gameObject);
			done = true;
		}else{
			scale.x = scale.x + inc;
			scale.y = scale.y + inc;
			scale.z = scale.z + inc;
			transform.localScale = scale;
		}
	}
}
