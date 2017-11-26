using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetUtilTransformComponent : MonoBehaviour {

	public float updateFrequency = 12.0f;
	public bool predictPosition = true;
	public bool predictRotation = true;
	public bool predictScale = true;

	private float oldTime = 0.0f;
	private Vector3 oldPosition;
	private Quaternion oldRotation;
	private Vector3 oldScale;

	private float currentTime = 0.0f;
	private Vector3 samplePosition;
	private Quaternion sampleRotation;
	private Vector3 sampleScale;

	private float newTime = 0.0f;
	private Vector3 newPosition;
	private Quaternion newRotation;
	private Vector3 newScale;

	private Vector3 findPosition() {

		if (!predictPosition) {

			return newPosition;
		}

		if (oldTime > 0.0f) {

			float delta = newTime - oldTime;
			float alpha = (currentTime - oldTime) / delta;

			return Vector3.Lerp(
				Vector3.Lerp(oldPosition, samplePosition, alpha),
				Vector3.Lerp(samplePosition, newPosition, alpha),
				alpha);
		}

		return Vector3.Lerp(oldPosition, newPosition, Time.deltaTime);
	}

	private Quaternion findRotation() {

		if (!predictRotation) {

			return newRotation;
		}

		if (oldTime > 0.0f) {

			float delta = newTime - oldTime;
			float alpha = (currentTime - oldTime) / delta;

			return Quaternion.Slerp(
				Quaternion.Slerp(oldRotation, sampleRotation, alpha),
				Quaternion.Slerp(sampleRotation, newRotation, alpha),
				alpha);
		}

		return Quaternion.Slerp(oldRotation, newRotation, Time.deltaTime);
	}

	private Vector3 findScale() {

		if (!predictScale) {

			return newScale;
		}

		if (oldTime > 0.0f) {

			float delta = newTime - oldTime;
			float alpha = (currentTime - oldTime) / delta;

			return Vector3.Lerp(
				Vector3.Lerp(oldScale, sampleScale, alpha),
				Vector3.Lerp(sampleScale, newScale, alpha),
				alpha);
		}

		return Vector3.Lerp(oldScale, newScale, Time.deltaTime);
	}

	private void Start() {

		newPosition = oldPosition = transform.position;
		newRotation = oldRotation = transform.rotation;
		newScale = oldScale = transform.localScale;
	}

	// Update is called once per frame
	void Update() {

		if (NetUtilManager.OwnsObject(gameObject.name)) {

			if (Time.time - currentTime > 1.0f / updateFrequency) {

				currentTime = Time.time;
				NetUtilManager.SyncTransform(gameObject.name, transform);
			}
		}
		else {

			currentTime += Time.deltaTime;

			if (predictPosition) {

				transform.position = findPosition();
			}
			if (predictRotation) {

				transform.rotation = findRotation();// Quaternion.SlerpUnclamped(Quaternion.identity, deltaRotation, Time.deltaTime / deltaTime);
			}
			if (predictScale) {

				transform.localScale = findScale();
			}
		}
	}

	void NetUtilSyncTransform(object t_object) {

		if (NetUtilManager.OwnsObject(gameObject.name)) {
		
			return;
		}

		NetUtilSyncTransform netTransform = (NetUtilSyncTransform)t_object;

		if (netTransform.time < newTime) {

			return;
		}

		oldTime = newTime;
		oldPosition = newPosition;
		oldRotation = newRotation;
		oldScale = newScale;
		
		samplePosition = transform.position;
		sampleRotation = transform.rotation;
		sampleScale = transform.localScale;

		newTime = netTransform.time;
		newPosition = netTransform.position.toVector3();
		newRotation = netTransform.rotation.toQuaternion();
		newScale = netTransform.localScale.toVector3();
		
		currentTime = oldTime;
	}

}
