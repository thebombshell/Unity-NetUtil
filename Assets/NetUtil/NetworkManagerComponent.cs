using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerComponent : MonoBehaviour {

	public GameObject buttonPrefab;
	public NetUtilPrefab[] prefabs;
	private List<GameObject> localPlayers = new List<GameObject>();
	private List<GameObject> gameButtons = new List<GameObject>();
	private string name = "";

	void Start () {

		Application.runInBackground = true;
		if (!NetUtilManager.isInitialized) {

			NetUtilManager.Setup();
			NetUtilManager.RegisterPrefabs(prefabs);
		}
	}

	public void HostGameLAN() {

		NetUtilManager.Host();
		name = GameObject.Find("Game Name Field").GetComponent<InputField>().text;
		NetUtilManager.StartAdvertiseLAN(name);
		GameObject.Destroy(GameObject.Find("Canvas"));
	}

	public void HostGameOnline() {

		NetUtilManager.Host();
		name = GameObject.Find("Game Name Field").GetComponent<InputField>().text;
		NetUtilManager.StartAdvertiseOnline(name);
		GameObject.Destroy(GameObject.Find("Canvas"));
	}

	public void RefreshList() {

		NetUtilGameInfo[] gameList = NetUtilManager.GetGameList();

		int index = 0;
		foreach (NetUtilGameInfo game in gameList) {

			GameObject button = GameObject.Instantiate(buttonPrefab, GameObject.Find("Canvas").transform);
			button.transform.position += new Vector3(0.0f, -30.0f, 0.0f) * ++index;
			button.GetComponentInChildren<Text>().text = game.name + " (" + game.playerCount + " / " + game.playerLimit + ")";
			button.GetComponent<Button>().onClick.AddListener(() => {

				NetUtilManager.Connect(game.ip, game.port);
				GameObject.Destroy(GameObject.Find("Canvas"));
			});
		}
	}


	void Update () {
		
		if (NetUtilManager.isSetup) {
		
			NetUtilManager.Update();
		}

		if (NetUtilManager.isConnected) {

			if (localPlayers.Count <= 0) {

				string name = NetUtilManager.CreateObject("player", Vector3.zero, Quaternion.identity, "");
				localPlayers.Add(NetUtilManager.GetObject(name));
			}
		}
	}
}
