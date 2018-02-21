using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetUtilComponent : MonoBehaviour {

	public NetUtilPrefab[] prefabs;

	private string m_gameName;

	private bool m_isAwake;

	/// <summary>
	/// the single instance of net util component
	/// </summary>
	private static NetUtilComponent g_singleton;

	/// <summary>
	/// the single instance of net util component
	/// </summary>
	public static NetUtilComponent singleton {
		
		get {

			return g_singleton;
		}
	}

	/// <summary>
	/// true if net util component exists
	/// </summary>
	public static bool isInitialized {
		
		get {

			return g_singleton != null;
		}
	}

	/// <summary>
	/// true if component is awake and all prefabs have been registered in net util
	/// </summary>
	public static bool isAwake {
		
		get {

			return g_singleton.m_isAwake;
		}
	}
	
	void Awake() {

		if ( isInitialized ) {

			throw new InvalidOperationException("there should only be one instance of net util component alive at a time");
		}
		g_singleton = this;
		Application.runInBackground = true;
		if (!NetUtilManager.isInitialized) {

			NetUtilManager.Setup();
		}
		foreach ( NetUtilPrefab prefab in prefabs ) {

			Debug.Log(prefab.name + ": " + prefab.gameObject);
		}
		NetUtilManager.RegisterPrefabs(prefabs);
		m_isAwake = true;
	}

	public void HostGameLAN(string t_name) {

		NetUtilManager.Host();
		m_gameName = t_name;
		NetUtilManager.StartAdvertiseLAN(m_gameName);
	}

	public void HostGameOnline(string t_name) {

		NetUtilManager.Host();
		m_gameName = t_name;
		NetUtilManager.StartAdvertiseOnline(m_gameName);
	}


	void Update () {

		if ( NetUtilManager.isInitialized ) {

			NetUtilManager.Update();
		}
		else {

			throw new InvalidOperationException("NetUtilManager must be initialized in order for netutilcomponent to run");
		}

		if (NetUtilManager.isConnected) {

		}
	}
}
