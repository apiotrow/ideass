using UnityEngine;
using System.Collections;

public class MultiDragRigidbody2D : MonoBehaviour
{
	public int maxTouch = 2;
	[Range(0,31)]
	public int layerMask = 0;
	public float distance = 0.2f;
	public float dampingRatio = 1;
	public float frequency = 1.8f;
	public float linearDrag = 1.0f;
	public float angularDrag = 5.0f;
	public bool centerOfMass = false;
	private SpringJoint2D[] springJoints;


	//public float drag = 1.0f; // this doesn't exist on 2D Spring...
	//public float angularDrag = 5.0f;
	//var distance = 0.2;
	public bool attachToCenterOfMass = false;
	private SpringJoint2D springJoint;
	
	void Start ()
	{
		springJoints = new SpringJoint2D[maxTouch];
		
		for (int i = 0; i < maxTouch; i++) {
			GameObject go = new GameObject ("Dragger" + (i + 1));
			go.transform.parent = this.transform;
			
			Rigidbody2D body = go.AddComponent ("Rigidbody2D") as Rigidbody2D;
			springJoints [i] = go.AddComponent ("SpringJoint2D") as SpringJoint2D;
			body.isKinematic = true;
		}
	}

	void MouseDragging(){
		if (!Input.GetMouseButtonDown (0))
			return;
		
		Camera mainCamera = FindCamera ();
		int layerMask = 1 << 8;
		RaycastHit2D hit = Physics2D.Raycast (mainCamera.ScreenToWorldPoint (Input.mousePosition), Vector2.zero, Mathf.Infinity, layerMask);
		Debug.Log ("Layermask: " + LayerMask.LayerToName (8));
		// I have proxy collider objects (empty gameobjects with a 2D Collider) as a child of a 3D rigidbody - simulating collisions between 2D and 3D objects
		// I therefore set any 'touchable' object to layer 8 and use the layerMask above for all touchable items
		
		if (hit.collider != null && hit.rigidbody.isKinematic == true) {
			return;
		}
		
		if (hit.collider != null && hit.rigidbody.isKinematic == false) {
			
			
			if (!springJoint) {
				GameObject go = new GameObject ("Rigidbody2D Dragger");
				Rigidbody2D body = go.AddComponent ("Rigidbody2D") as Rigidbody2D;
				springJoint = go.AddComponent ("SpringJoint2D") as SpringJoint2D;
				
				body.isKinematic = true;
			}
			
			springJoint.transform.position = hit.point;
			
			
			if (attachToCenterOfMass) {
				
				Debug.Log ("Currently 'centerOfMass' isn't reported for 2D physics like 3D Physics - it will be added in a future release.");
				// Currently 'centerOfMass' isn't reported for 2D physics like 3D Physics yet - it will be added in a future release.
				
				//Vector3 anchor = transform.TransformDirection(hit.rigidbody.centerOfMass) + hit.rigidbody.transform.position; in c# might be Vector2?
				
				//anchor = springJoint.transform.InverseTransformPoint(anchor);
				//springJoint.anchor = anchor;
			} else {
				
				//springJoint.anchor = Vector3.zero;
			}
			
			springJoint.distance = distance; // there is no distance in SpringJoint2D
			springJoint.dampingRatio = dampingRatio;// there is no damper in SpringJoint2D but there is a dampingRatio
			//springJoint.maxDistance = distance;  // there is no MaxDistance in the SpringJoint2D - but there is a 'distance' field
			//  see http://docs.unity3d.com/Documentation/ScriptReference/SpringJoint2D.html
			//springJoint.maxDistance = distance;
			springJoint.connectedBody = hit.rigidbody;
			Vector3 localPoint = transform.InverseTransformPoint (hit.point);
			springJoint.connectedAnchor = localPoint;
			
			// maybe check if the 'fraction' is normalised. See http://docs.unity3d.com/Documentation/ScriptReference/RaycastHit2D-fraction.html
			StartCoroutine ("DragObject", hit.fraction);
			
			
			
		} // end of hit true condition
	}

	void MultiTouchDragging(){
		foreach (Touch touch in Input.touches) {
			int Id = touch.fingerId;
			
			if (Id < maxTouch && touch.phase == TouchPhase.Began) {
				Camera mainCamera = FindCamera ();
				Ray ray = mainCamera.ScreenPointToRay (touch.position);
				RaycastHit2D hit = Physics2D.GetRayIntersection (ray, Mathf.Infinity, 1 << layerMask);
				
				if (hit.rigidbody != null && hit.rigidbody.isKinematic == false) {
					springJoints [Id].transform.position = hit.point;
					springJoints [Id].connectedBody = hit.rigidbody;
					
					//					if (centerOfMass)
					//						springJoints [Id].connectedAnchor = hit.rigidbody.centerOfMass;
					//					else
					springJoints [Id].connectedAnchor = hit.transform.InverseTransformPoint (hit.point);
					
					float length = (hit.transform.position - mainCamera.transform.position).magnitude;
					StartCoroutine (DragObject (Id, length));
				}
			}
		}
	}
	
	void Update ()
	{
		MouseDragging ();
		MultiTouchDragging ();

	}
	
	IEnumerator DragObject (int Id, float length)
	{
		float oldDrag = springJoints [Id].connectedBody.drag;
		float oldAngularDrag = springJoints [Id].connectedBody.angularDrag;
		springJoints [Id].distance = distance;
		springJoints [Id].dampingRatio = dampingRatio;
		springJoints [Id].frequency = frequency;
		springJoints [Id].connectedBody.drag = linearDrag;
		springJoints [Id].connectedBody.angularDrag = angularDrag;
		Camera mainCamera = FindCamera ();
		
		while (true) {
			bool touchExists = false;
			foreach (Touch touch in Input.touches) {
				if (touch.fingerId == Id) {
					touchExists = true;
					Ray ray = mainCamera.ScreenPointToRay (touch.position);
					springJoints [Id].transform.position = ray.GetPoint (length);
				}
			}
			if (touchExists)
				yield return null;
			else
				break;
		}
		
		if (springJoints [Id].connectedBody) {
			springJoints [Id].connectedBody.drag = oldDrag;
			springJoints [Id].connectedBody.angularDrag = oldAngularDrag;
			springJoints [Id].connectedBody = null;
		}  
	}
	
	Camera FindCamera ()
	{
		if (camera)
			return camera;
		else
			return Camera.main;
	}
}