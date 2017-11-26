using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

[Serializable]
public struct NetUtilPrefab {

	public string name;
	public GameObject gameObject;
}

/// <summary>
/// a static management class for netutil
/// </summary>
public static class NetUtilManager {
	
	/// <summary>
	/// a singleton instance of netutil
	/// </summary>
	private static NetUtil m_singleton;

	/// <summary>
	/// the current state of online advert
	/// </summary>
	private static WWW m_advertQuery;

	/// <summary>
	/// the timer used for online adverts
	/// </summary>
	private static float m_advertTimer;

	/// <summary>
	/// the name of the currently hosted game
	/// </summary>
	private static string m_gameName;

	/// <summary>
	/// true if netutil has been setup
	/// </summary>
	public static bool isInitialized {

		get {

			return m_singleton != null;
		}
	}

	/// <summary>
	/// true if netutil has been setup
	/// </summary>
	public static bool isSetup {

		get {

			return m_singleton.isSetup;
		}
	}

	/// <summary>
	/// true if netutil is hosting or connected to another player
	/// </summary>
	public static bool isConnected {
		
		get {

			return m_singleton.isConnected;
		}
	}

	/// <summary>
	/// true if netutil is a host
	/// </summary>
	public static bool isHost {
		
		get {

			return m_singleton.isHost;
		}
	}

	/// <summary>
	/// the time at which the host was setup
	/// </summary>
	public static float hostTime {
		
		get {

			return m_singleton.hostTime;
		}
	}

	/// <summary>
	/// sets up netutil
	/// </summary>
	public static void Setup() {

		m_singleton = new NetUtil(7108, 7109);
		m_singleton.SetupListener();
	}

	/// <summary>
	/// sets up netutil as a host
	/// </summary>
	public static void Host() {

		m_singleton.Shutdown();
		m_singleton.SetupHost();
	}

	/// <summary>
	/// starts advertising the game over LAN
	/// </summary>
	/// <param name="t_name">the label of the game</param>
	public static void StartAdvertiseLAN(string t_name) {

		m_gameName = t_name;
		m_singleton.StartAdvertiseLAN(m_gameName);
	}

	/// <summary>
	/// stop advertising the game over LAN
	/// </summary>
	public static void StopAdvertiseLAN() {

		m_singleton.StopAdvertiseLAN();
	}

	/// <summary>
	/// starts advertising the game online
	/// </summary>
	/// <param name="t_name">the label of the game</param>
	public static void StartAdvertiseOnline(string t_name) {

		if (m_advertQuery != null) {
		
			m_advertQuery.Dispose();
		}
		m_gameName = t_name;
		m_advertQuery = m_singleton.AdvertiseOnline(m_gameName);
		m_advertTimer = Time.time;
	}

	/// <summary>
	/// stops advertising the game online
	/// </summary>
	public static void StopAdvertiseOnline() {

		m_advertQuery.Dispose();
		m_advertQuery = null;
	}

	/// <summary>
		/// sets up netutil as a client and connects to a host
		/// </summary>
		/// <param name="t_ip">the ip address of the host</param>
	public static void Connect(string t_ip, int t_port) {

		m_singleton.Shutdown();
		m_singleton.SetupClient();
		m_singleton.Connect(t_ip, t_port);
	}
	
	/// <summary>
	/// disconnects and resets netutil
	/// </summary>
	public static void Disconnect() {

		m_singleton.Shutdown();
	}

	/// <summary>
	/// shuts down netutil
	/// </summary>
	public static void Shutdown() {
	
		m_singleton = null;
	}

	/// <summary>
	/// sends a talk message
	/// </summary>
	/// <param name="t_text">the messages to speak</param>
	public static void Talk(string t_text) {

		m_singleton.Talk(t_text);
	}

	/// <summary>
	/// creates a game object over the network
	/// </summary>
	/// <param name="t_prefab">the name of the prefab to create</param>
	/// <param name="t_position">the position of the game object</param>
	/// <param name="t_rotation">the rotation of the game object</param>
	/// <param name="t_data">a serializable object to pass to the created object</param>
	/// <returns>returns the network name of the object</returns>
	public static string CreateObject(string t_prefab, Vector3 t_position, Quaternion t_rotation, object t_data) {

		return m_singleton.CreateObject(t_prefab, t_position, t_rotation, t_data);
	}

	/// <summary>
	/// destroys a game object over the network
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static void DestroyObject(string t_name) {
	
		m_singleton.DestroyObject(t_name);
	}

	/// <summary>
	/// requests local ownership of a network game object
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static void RequestObject(string t_name) {

		m_singleton.RequestObject(t_name);
	}

	/// <summary>
	/// syncs a game objects transform over the network
	/// </summary>
	/// <param name="t_name">the name of the object to sync</param>
	/// <param name="t_transform">the transform to sync with</param>
	public static void SyncTransform(string t_name, Transform t_transform) {
	
		m_singleton.SyncTransform(new NetUtilSyncTransform(t_name, Time.time, t_transform));
	}

	/// <summary>
	/// registers prefabs to be used over the network
	/// </summary>
	/// <param name="t_prefabs">an array of name / prefab pairs represehnting network prefabs</param>
	public static void RegisterPrefabs(NetUtilPrefab[] t_prefabs) {
		
		foreach (NetUtilPrefab prefab in t_prefabs) {

			m_singleton.RegisterPrefab(prefab.name, prefab.gameObject);
		}
	}

	/// <summary>
	/// registers a callback function to handle custom network messages
	/// </summary>
	/// <param name="t_type">the type of the message to handle</param>
	/// <param name="t_handler">the callback function acting as the message handler</param>
	public static void RegisterMessageHandler(string t_type, NetUtilMessageCallback t_handler) {

		m_singleton.RegisterMessageHandler(t_type, t_handler);
	} 

	/// <summary>
	/// allows netutil to pump out and react to messages
	/// </summary>
	public static void Update() {
	
		if (m_advertQuery != null) {
		
			if (Time.time - m_advertTimer >= 1.0f && m_advertQuery.isDone) {
				
				if (m_advertQuery.text != "Success") {

					DebugConsole.Log(m_advertQuery.text);
				}
				StartAdvertiseOnline(m_gameName);
			}
		}

		m_singleton.HandleMessages();
	}

	/// <summary>
	/// sends a custom message which is garunteed to arrive
	/// </summary>
	/// <param name="t_message">the message to send</param>
	public static void SendMessage(NetUtilCustomMessage t_message) {

		m_singleton.SendMessage(t_message);
	}

	/// <summary>
	/// sends a custom message which may be dropped due to network conditions
	/// </summary>
	/// <param name="t_message">the message to send</param>
	public static void SendSync(NetUtilCustomMessage t_message) {

		m_singleton.SendSync(t_message);
	}

	/// <summary>
	/// sends a custom message which may be dropped due to network conditions, but will always arrive in the order they are sent
	/// </summary>
	/// <param name="t_message">the message to send</param>
	public static void SendPrioritySync(NetUtilCustomMessage t_message) {

		m_singleton.SendPrioritySync(t_message);
	}

	/// <summary>
	/// returns true if an network game object is owned locally
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static bool OwnsObject(string t_name) {

		return m_singleton.OwnsObject(t_name);
	}

	/// <summary>
	/// returns true if a network game object exists with the given name
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static bool Contains(string t_name) {

		return m_singleton.Contains(t_name);
	}

	/// <summary>
	/// returns a local instance of a network game object
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static GameObject GetObject(string t_name) {

		return m_singleton.GetObject(t_name);
	}

	/// <summary>
	/// returns a collection of detected remote games capable of being joined
	/// </summary>
	public static NetUtilGameInfo[] GetGameList() {

		return m_singleton.GetGameList();
	}
}
