using UnityEngine;
using System.Collections;

public class crsBehavior : MonoBehaviour {
	Vector3 rot = new Vector3(0, 0, 0);
	// Use this for initialization
	void Start () {
		float randX = Random.Range (0, 359);
		float randY = Random.Range (0, 359);
		rot.x += randX;
		rot.y += randY;
		transform.rotation = Quaternion.Euler (rot);
	}
	
	// Update is called once per frame
	void Update () {
		rot.x += 1.0f;
		rot.y += 1.0f;
		transform.rotation = Quaternion.Euler (rot);

	}
}
