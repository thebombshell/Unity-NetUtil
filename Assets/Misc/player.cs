using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour {

	string lastProjectile = "";
	
	void Update () {
		
		// if not local, stop execution
		if (!NetUtilManager.OwnsObject(gameObject.name)) {
		
			return;
		}

		// if local, simulate player

		if (Input.GetKey("w")) {

			transform.position += transform.forward * 5.0f * Time.deltaTime;
		}
		if (Input.GetKey("a")) {

			transform.position -= transform.right * 5.0f * Time.deltaTime;
		}
		if (Input.GetKey("s")) {

			transform.position -= transform.forward * 5.0f * Time.deltaTime;
		}
		if (Input.GetKey("d")) {

			transform.position += transform.right * 5.0f * Time.deltaTime;
		}
		if (Input.GetKey("right")) {

			transform.Rotate(0.0f, Time.deltaTime * 90.0f, 0.0f);
		}
		if (Input.GetKey("left")) {

			transform.Rotate(0.0f, Time.deltaTime * -90.0f, 0.0f);
		}
		if (Input.GetKeyDown("up")) {

			NetUtilManager.SendPrioritySync(new NetUtilCustomMessage(lastProjectile, "Jump", true));
		}
		if (Input.GetKeyDown("down")) {

			NetUtilManager.SendPrioritySync(new NetUtilCustomMessage(lastProjectile, "Jump", false));
		}
		if (Input.GetKeyDown("space")) {

			ProjectileInfo info = new ProjectileInfo();
			info.isOwnedByHost = true;
			info.velocity = new NetUtilVector3(transform.forward * 15.0f);
			lastProjectile = NetUtilManager.CreateObject("projectile", transform.position + transform.forward, transform.rotation, info);
		}
	}
}
