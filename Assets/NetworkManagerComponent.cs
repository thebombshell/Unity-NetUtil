using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManagerComponent : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
		if (!NetUtilManager.isInitialized) {

			NetUtilManager.Setup();
		}
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetKeyDown("i")) {

			if (NetUtilManager.isConnected) {

				DebugConsole.Log("NetUtil is already connected");
			}
			else {
			
				NetUtilManager.Host();
			}
		}

		if (Input.GetKeyDown("o")) {

			if (NetUtilManager.isConnected) {

				DebugConsole.Log("NetUtil is already connected");
			}
			else {

				NetUtilManager.Connect("127.0.0.1");
			}
		}

		if (Input.GetKeyDown("p")) {

			NetUtilManager.Talk("ping pong");
		}

		if (NetUtilManager.isConnected) {

			NetUtilManager.Update();
		}
	}
}
