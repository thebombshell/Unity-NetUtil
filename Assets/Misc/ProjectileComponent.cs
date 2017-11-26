using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ProjectileInfo {

	public NetUtilVector3 velocity;
	public bool isOwnedByHost;
}

public class ProjectileComponent : MonoBehaviour {

	public Vector3 velocity;
	public float age;

	void Update () {
		
		if (!NetUtilManager.OwnsObject(gameObject.name)) {

			return;
		}

		transform.position += velocity * Time.deltaTime;
		transform.rotation *= Quaternion.AngleAxis(Time.deltaTime * 360.0f, velocity.normalized);
		transform.localScale = Vector3.one + Vector3.one * Mathf.Cos(Time.time) * 0.5f;
		age += Time.deltaTime;
		if (age > 5.0f) {

			NetUtilManager.DestroyObject(gameObject.name);
			Debug.Log("kill");
		}
	}

	void NetUtilInitialize(object t_data) {
	
		ProjectileInfo info = (ProjectileInfo)t_data;
		velocity = info.velocity.toVector3();
		if (info.isOwnedByHost && NetUtilManager.isHost && !NetUtilManager.OwnsObject(gameObject.name)) {

			DebugConsole.Log("requesting " + gameObject.name);
			NetUtilManager.RequestObject(gameObject.name);
		}

	}

	void Jump(object t_data) {

		if (!NetUtilManager.OwnsObject(gameObject.name)) {

			return;
		}

		bool goUp = (bool)t_data;
		transform.position += new Vector3(0.0f, goUp ? 2.0f : -2.0f, 0.0f);
	}
}
