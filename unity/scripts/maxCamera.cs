//
//Filename: maxCamera.cs
//
// original: http://www.unifycommunity.com/wiki/index.php?title=MouseOrbitZoom
//
// --01-18-2010 - create temporary target, if none supplied at start

using UnityEngine;
using System.Collections;


//[AddComponentMenu("Camera-Control/3dsMax Camera Style")]
public class maxCamera : MonoBehaviour
{
	public Transform target;
	public Vector3 targetOffset;
	public float distance = 35.0f;
	public float maxDistance = 35;
	public float minDistance = .6f;
	public float xSpeed = 200.0f;
	public float ySpeed = 200.0f;
	public int yMinLimit = -80;
	public int yMaxLimit = 80;
	public int zoomRate = 40;
	public float panSpeed = 0.3f;
	public float zoomDampening = 5.0f;
	
	private float xDeg = 0.0f;
	private float yDeg = 0.0f;
	private float currentDistance;
	private float desiredDistance;
	private Quaternion currentRotation;
	private Quaternion desiredRotation;
	private Quaternion rotation;
	private Vector3 position;
	private Transform originalTransform;

	public float perspectiveZoomSpeed = 0.5f;        // The rate of change of the field of view in perspective mode.
	public float orthoZoomSpeed = 0.5f;        // The rate of change of the orthographic size in orthographic mode.


	private bool noObject = true;
	void Start() { Init(); }
	void OnEnable() { Init(); }

	public void setObject(Vector3 pos){
		target.position = pos;
		noObject = false;
		distance = 5;
		currentDistance = distance;
		desiredDistance = distance;
	}
	public void setPosition(Vector3 pt){
		//Debug.Log ("maxCamera camera set to " + pt);
		//transform.position = pt;
		distance = 35.0f; 
		pt.z = pt.z + distance;
		target.position = pt;
		currentDistance = distance;
		desiredDistance = distance;
		Vector3 rot = new Vector3 (0.0f,  0, 0);
		rotation = Quaternion.Euler (rot);
		//rotation = Quaternion.Euler (Vector3.zero);
		//Debug.Log ("maxCamera target position " + pt);
		//hackcheck = false;
		noObject = true;
		//transform.LookAt (target.transform.position);
	}
	public void restore(){
		target = originalTransform;
		noObject = true;
	}
	public void moveY(int delta){
		Vector3 pos = target.position;
		pos.y = pos.y + delta;
		target.position = pos;
	}
	public void moveX(int delta){
		Vector3 pos = target.position;
		pos.x = pos.x + delta;
		target.position = pos;
	}
	public void moveZ(int delta){
		Debug.Log ("moving z");
		Vector3 pos = target.position;
		pos.z = pos.z + delta;
		target.position = pos;
	}
	public void Init()
	{
		noObject = true;
		//If there is no target, create a temporary target at 'distance' from the cameras current viewpoint
		if (!target)
		{
			GameObject go = new GameObject("Cam Target");
			go.transform.position = transform.position + (transform.forward * distance);
			target = go.transform;
			originalTransform = target;
		}else{
			distance = Vector3.Distance(transform.position, target.position);
		}
		
		currentDistance = 0;
		desiredDistance = distance;
		
		//be sure to grab the current rotations as starting points.
		position = transform.position;
		rotation = transform.rotation;
		currentRotation = transform.rotation;
		desiredRotation = transform.rotation;
		
		xDeg = Vector3.Angle(Vector3.right, transform.right );
		yDeg = Vector3.Angle(Vector3.up, transform.up );
	}
	void checkMulti(){
		//Debug.Log ("touchcount is " + Input.touchCount);
		if (Input.touchCount == 2)
		{
			Debug.Log ("touchcount is 2");
			// Store both touches.
			Touch touchZero = Input.GetTouch(0);
			Touch touchOne = Input.GetTouch(1);
			
			// Find the position in the previous frame of each touch.
			Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
			Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
			
			// Find the magnitude of the vector (the distance) between the touches in each frame.
			float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
			float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
			
			// Find the difference in the distances between each frame.
			float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
			
			// If the camera is orthographic...
			if (camera.isOrthoGraphic)
			{
				// ... change the orthographic size based on the change in distance between the touches.
				camera.orthographicSize += deltaMagnitudeDiff * orthoZoomSpeed;
				
				// Make sure the orthographic size never drops below zero.
				camera.orthographicSize = Mathf.Max(camera.orthographicSize, 0.1f);
			}
			else
			{
				// Otherwise change the field of view based on the change in distance between the touches.
				camera.fieldOfView += deltaMagnitudeDiff * perspectiveZoomSpeed;
				
				// Clamp the field of view to make sure it's between 0 and 180.
				camera.fieldOfView = Mathf.Clamp(camera.fieldOfView, 0.1f, 179.9f);
			}
		}
	}	
	/*
     * Camera logic on LateUpdate to only update after all character movement logic has been handled. 
     */
	void LateUpdate()
	{
		//checkMulti ();
		//if(Input.GetMouseButton(1)){
		//	Debug.Log ("mouse 2");
		//}

		//if(Input.GetKey(KeyCode.LeftAlt)){
		//	Debug.Log ("leftAlt");
		//}
		// If Control and Alt and Middle button? ZOOM!
		if (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl))
		{
			desiredDistance -= Input.GetAxis("Mouse Y") * Time.deltaTime * zoomRate*0.125f * Mathf.Abs(desiredDistance);
		}
		// If middle mouse and left alt are selected? ORBIT
		//else if (Input.GetKey(KeyCode.LeftAlt))
		else if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt))
		{
			xDeg += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
			yDeg -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
			
			////////OrbitAngle
			
			//Clamp the vertical axis for the orbit
			yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);
			// set camera rotation 
			desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
			currentRotation = transform.rotation;
			
			rotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * zoomDampening);
			transform.rotation = rotation;
		}
		// otherwise if middle mouse is selected, we pan by way of transforming the target in screenspace
		else if (Input.GetMouseButton(2) || (Input.GetKey (KeyCode.LeftAlt)))
		{
			//grab the rotation of the camera so we can move in a psuedo local XY space
			target.rotation = transform.rotation;
			target.Translate(Vector3.right * -Input.GetAxis("Mouse X") * panSpeed);
			target.Translate(transform.up * -Input.GetAxis("Mouse Y") * panSpeed, Space.World);
		}
		
		////////Orbit Position
		
		// affect the desired Zoom distance if we roll the scrollwheel
		//if(!hackcheck)
			desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);
		//Debug.Log ("desired distance " + desiredDistance);
		//clamp the zoom min/max
		if(!noObject){
			//Debug.Log ("clamping distance");
			desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
		}else{
			//Debug.Log ("no object");
		}
		// For smoothing of the zoom, lerp distance
		float damp = Time.deltaTime * zoomDampening;
		//Debug.Log ("currrent distance: "+currentDistance+" desired "+desiredDistance+" damp is " + damp);
		//if(!hackcheck)
     		currentDistance = Mathf.Lerp(currentDistance, desiredDistance, damp);

		// calculate position based on the new currentDistance 
		if(targetOffset != Vector3.zero){
			//Debug.Log("target offset "+targetOffset);
		}
		//if(!hackcheck){
		    position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);
		//}else{
		//	position = target.position;
		//	hackcheck = false;
		//}
		//position = target.position - (rotation * Vector3.forward * currentDistance);
		if(transform.position != position){
			//Debug.Log ("moving from "+transform.position+" to "+position+" currentDistance "+currentDistance);
		}
		transform.position = position;
	}
	
	private static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360)
			angle += 360;
		if (angle > 360)
			angle -= 360;
		return Mathf.Clamp(angle, min, max);
	}
}