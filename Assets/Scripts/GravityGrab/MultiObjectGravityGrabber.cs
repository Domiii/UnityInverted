﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MultiObjectGravityGrabber : MonoBehaviour {
	/// <summary>
	/// The name of the physical layer, this grabber grabs objects of.
	/// </summary>
	public string grabbableObjectsLayerName = "GrabbableObjects";

	/// <summary>
	/// The transform to pull the grabbed object toward.
	/// </summary>
	public Transform gravityPoint;

	/// <summary>
	/// The power of the pull on grabbed objects
	/// </summary>
	public float pullPower = 200;

	public float maxRadius = 10;

	public new Light light;

	HashSet<Rigidbody> grabbedObjects = new HashSet<Rigidbody>();
	HashSet<Rigidbody> tmpObjects = new HashSet<Rigidbody> ();
	Collider[] foundObjects = new Collider[64];

	public int grabbableLayerMask;

	//Vector3 previousGravityPointPosition;

	void Start() {
		grabbableLayerMask = 1 << LayerMask.NameToLayer (grabbableObjectsLayerName);

		if (grabbableLayerMask == 0 || grabbableObjectsLayerName.Length == 0) {
			throw new UnityException ("You must define a `" + grabbableObjectsLayerName + "` Layer for " + GetType().Name + " to work.");
		}

		if (!gravityPoint) {
			gravityPoint = transform;
		}
	}

	void Update() {
		// keep everything in sync with the currently set maxDistance

		// height should always be equal to the radius
		var pos = transform.position;
		if (pos.y != maxRadius) {
			transform.position = new Vector3 (pos.x, maxRadius, pos.z);
		}


		if (light) {
			light.range = maxRadius * 2;
		}
	}

	void FixedUpdate() {
		var isPulling = Input.GetMouseButton (0);
		if (!isPulling) {
			// drop object when mouse is released
			DropObjects ();
		} else {
			// keep pulling object
			CollectObjects();
			PullObjects ();
		}
	}

	/// <summary>
	/// Find all objects in the vaccinity that are not being pulled yet
	/// </summary>
	void CollectObjects() {
		var p1 = gravityPoint.position;
		p1.y = -100;	// very small value
		var p2 = gravityPoint.position;
		p2.y = 100;	// very large value
		var count = Physics.OverlapCapsuleNonAlloc (p1, p2, maxRadius, foundObjects, grabbableLayerMask);
		while (count >= foundObjects.Length) {
			// the buffer is too small -> double size and try again
			foundObjects = new Collider[foundObjects.Length*2];
			count = Physics.OverlapCapsuleNonAlloc (p2, p1, maxRadius, foundObjects, grabbableLayerMask);
		}

		// remove all objects that are not around this time
		foreach (var body in grabbedObjects) {
			tmpObjects.Add (body);
		}
		for (var i = 0; i < count; ++i) {
			var collider = foundObjects [i];
			tmpObjects.Remove (collider.GetComponent<Rigidbody>());
		}
		foreach (var body in tmpObjects) {
			StopObjectGrab (body);
		}
		tmpObjects.Clear ();

		// add all new objects
		for (var i = 0; i < count; ++i) {
			var collider = foundObjects [i];
			var body = collider.GetComponent<Rigidbody> ();
			if (!grabbedObjects.Contains (body)) {
				StartObjectGrab (body);
			}
		}
	}

	void StartObjectGrab(Rigidbody body) {
		grabbedObjects.Add (body);
		body.GetComponent<ResourceObject> ().SetGrabbed(true, this);
	}

	/// <summary>
	/// Exert a damped gravitational force to pull the object toward a location in front of the player.
	/// </summary>
	void PullObjects() {
		foreach (var body in grabbedObjects) {
			if (body) {
				PullObject (body);
			}
		}
	}

	void PullObject(Rigidbody body) {
		// compute force direction
		var objPos = body.GetComponent<Collider> ().bounds.center;
		var dir = gravityPoint.transform.position - objPos;
		var dist = dir.magnitude;
		if (dir.magnitude < 0.1f) {
			// stop pulling when very close
			return;
		}

		dir.Normalize ();

		// the further, the stronger the pull!
		//var delta = dir * (pullPower * Time.fixedDeltaTime * (dist * dist));

		// pull strength is independent of distance
		var speed = pullPower * Time.fixedDeltaTime;
		var delta = dir * speed;


		//delta *= dist;
		if (dist < speed * 0.1f) {
			// if is very close, scale with dist, to damp oscillation
			delta *= dist * dist;
		}

		body.velocity = delta;
	}

//	/// <summary>
//	/// Calculate swing momentum: given by how far the gravityPoint moved in a frame.
//	/// NOTE: The gravityPoint is locked with mouse/controller.
//	/// </summary>
//	Vector3 CalcSwingMomentum() {
//		var p1 = previousGravityPointPosition;
//		var p2 = gravityPoint.transform.position;
//
//		var swingDelta = p2 - p1;
//
//		var swingMomentum = swingDelta * swingPower;
//
//		// update previous position (smoothed over time)
//		previousGravityPointPosition = (previousGravityPointPosition * (swingSmoothFactor - 1) + p2) / swingSmoothFactor;
//
//		return swingMomentum;
//	}

	void StopObjectGrab(Rigidbody body) {
		if (body) {
			body.GetComponent<ResourceObject> ().SetGrabbed(false, this);
		}
		grabbedObjects.Remove (body);
	}

	void DropObjects() {
		foreach (var body in grabbedObjects) {
			if (body) {
				body.GetComponent<ResourceObject> ().SetGrabbed(false, this);
			}
		}
		grabbedObjects.Clear ();
	}
}
