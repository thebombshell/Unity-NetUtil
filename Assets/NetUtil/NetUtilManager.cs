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
	private static NetUtil g_singleton;

    public static NetUtil singleton {
        get {
            return g_singleton;
        }
    }
	/// <summary>
	/// the current state of online advert
	/// </summary>
	private static WWW g_advertQuery;

	/// <summary>
	/// the timer used for online adverts
	/// </summary>
	private static float g_advertTimer;

	/// <summary>
	/// the name of the currently hosted game
	/// </summary>
	private static string g_gameName;

	/// <summary>
	/// true if netutil has been setup
	/// </summary>
	public static bool isInitialized {

		get {

			return g_singleton != null;
		}
	}

	/// <summary>
	/// true if netutil has been setup
	/// </summary>
	public static bool isSetup {

		get {

			return g_singleton.isSetup;
		}
	}

	/// <summary>
	/// true if netutil is hosting or connected to another player
	/// </summary>
	public static bool isConnected {
		
		get {

			return g_singleton.isConnected;
		}
	}

	/// <summary>
	/// true if netutil is a host
	/// </summary>
	public static bool isHost {
		
		get {

			return g_singleton.isHost;
		}
	}

	/// <summary>
	/// the time at which the host was setup
	/// </summary>
	public static float hostTime {
		
		get {

			return g_singleton.hostTime;
		}
	}

	/// <summary>
	/// true if netutil is ready to accept network messages
	/// </summary>
	public static bool isReady {
		
		get {

			return g_singleton.isReady;
		}
	}

	/// <summary>
	/// true if scene is the same as host
	/// </summary>
	public static bool isSceneReady {

		get {

			return g_singleton.isSceneReady;
		}
	}

	/// <summary>
	/// sets up netutil
	/// </summary>
	public static void Setup() {

		g_singleton = new NetUtil(7108, 7109);
		g_singleton.SetupListener();
	}

	/// <summary>
	/// sets up netutil as a host
	/// </summary>
	public static void Host() {

		g_singleton.Shutdown();
		g_singleton.SetupHost();
	}

	/// <summary>
	/// starts advertising the game over LAN
	/// </summary>
	/// <param name="t_name">the label of the game</param>
	public static void StartAdvertiseLAN(string t_name) {

		g_gameName = t_name;
		g_singleton.StartAdvertiseLAN(g_gameName);
	}

	/// <summary>
	/// stop advertising the game over LAN
	/// </summary>
	public static void StopAdvertiseLAN() {

		g_singleton.StopAdvertiseLAN();
	}

	/// <summary>
	/// starts advertising the game online
	/// </summary>
	/// <param name="t_name">the label of the game</param>
	public static void StartAdvertiseOnline(string t_name) {

		if (g_advertQuery != null) {
		
			g_advertQuery.Dispose();
		}
		g_gameName = t_name;
		g_advertQuery = g_singleton.AdvertiseOnline(g_gameName);
		g_advertTimer = Time.time;
	}

	/// <summary>
	/// stops advertising the game online
	/// </summary>
	public static void StopAdvertiseOnline() {

		g_advertQuery.Dispose();
		g_advertQuery = null;
	}

	/// <summary>
		/// sets up netutil as a client and connects to a host
		/// </summary>
		/// <param name="t_ip">the ip address of the host</param>
	public static void Connect(string t_ip, int t_port) {

		g_singleton.Shutdown();
		g_singleton.SetupClient();
		g_singleton.Connect(t_ip, t_port);
	}
	
	/// <summary>
	/// disconnects and resets netutil
	/// </summary>
	public static void Disconnect() {

		g_singleton.Shutdown();
	}

	/// <summary>
	/// shuts down netutil
	/// </summary>
	public static void Shutdown() {
	
		g_singleton = null;
	}

	/// <summary>
	/// sends a talk message
	/// </summary>
	/// <param name="t_text">the messages to speak</param>
	public static void Talk(string t_text) {

		g_singleton.Talk(t_text);
	}

	/// <summary>
	/// creates a game object over the network
	/// </summary>
	/// <param name="t_prefab">the name of the prefab to create</param>
	/// <param name="t_position">the position of the game object</param>
	/// <param name="t_rotation">the rotation of the game object</param>
	/// <param name="t_data">a serializable object to pass to the created object</param>
	/// <returns>returns the network name of the object</returns>
	public static string CreateObject(string t_prefab, Vector3 t_position, Quaternion t_rotation, object t_data, bool t_isNetworked = true) {

		return g_singleton.CreateObject(t_prefab, t_position, t_rotation, t_data, t_isNetworked);
	}

	/// <summary>
	/// destroys a game object over the network
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static void DestroyObject(string t_name) {
	
		g_singleton.DestroyObject(t_name);
	}

	/// <summary>
	/// creates a host object over the network
	/// </summary>
	/// <param name="t_type">the name of the type of host object to create</param>
	/// <param name="t_data">the object to share over network</param>
	/// <returns>returns the network name of the object</returns>
	public static string CreateHostObject(string t_type, object t_data) {

		return g_singleton.CreateHostObject(t_type, t_data);
	}

	/// <summary>
	/// destroys a host object over the network
	/// </summary>
	/// <param name="t_name">the name of the host object</param>
	public static void DestroyHostObject(string t_name) {

		g_singleton.DestroyHostObject(t_name);
	}

	/// <summary>
	/// updates a host object over the network
	/// </summary>
	/// <param name="t_type">the name of the type of host object to create</param>
	/// <param name="t_data">the object to share over network</param>
	public static void UpdateHostObject(string t_name, object t_data) {

		g_singleton.UpdateHostObject(t_name, t_data);
	}

	/// <summary>
	/// requests local ownership of a network game object
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static void RequestObject(string t_name) {

		g_singleton.RequestObject(t_name);
	}

	/// <summary>
	/// syncs a game objects transform over the network
	/// </summary>
	/// <param name="t_name">the name of the object to sync</param>
	/// <param name="t_transform">the transform to sync with</param>
	public static void SyncTransform(string t_name, Transform t_transform) {
	
		g_singleton.SyncTransform(new NetUtilSyncTransform(t_name, Time.time, t_transform));
	}

	/// <summary>
	/// registers prefabs to be used over the network
	/// </summary>
	/// <param name="t_prefabs">an array of name / prefab pairs represehnting network prefabs</param>
	public static void RegisterPrefabs(NetUtilPrefab[] t_prefabs) {
		
		foreach (NetUtilPrefab prefab in t_prefabs) {

			g_singleton.RegisterPrefab(prefab.name, prefab.gameObject);
		}
	}

	/// <summary>
	/// registers a callback function to handle custom network messages
	/// </summary>
	/// <param name="t_type">the type of the message to handle</param>
	/// <param name="t_handler">the callback function acting as the message handler</param>
	public static void RegisterMessageHandler(string t_type, NetUtilMessageCallback t_handler) {

		g_singleton.RegisterMessageHandler(t_type, t_handler);
	} 

	/// <summary>
	/// allows netutil to pump out and react to messages
	/// </summary>
	public static void Update() {
	
		if (g_advertQuery != null) {
		
			if (Time.time - g_advertTimer >= 1.0f && g_advertQuery.isDone) {
				
				if (g_advertQuery.text != "Success") {

					Debug.Log(g_advertQuery.text);
				}
				StartAdvertiseOnline(g_gameName);
			}
		}

		if ( NetUtilComponent.isInitialized && NetUtilComponent.isAwake && NetUtilManager.isSceneReady && !NetUtilManager.isReady ) {

			NetUtilManager.SetReady();
		}

		g_singleton.HandleConnections();
		g_singleton.HandleMessages();
	}

	/// <summary>
	/// sends a custom message which is garunteed to arrive
	/// </summary>
	/// <param name="t_message">the message to send</param>
	public static void SendMessage(NetUtilCustomMessage t_message) {

		g_singleton.SendMessage(t_message);
	}

	/// <summary>
	/// sends a custom message which may be dropped due to network conditions
	/// </summary>
	/// <param name="t_message">the message to send</param>
	public static void SendSync(NetUtilCustomMessage t_message) {

		g_singleton.SendSync(t_message);
	}

	/// <summary>
	/// sends a custom message which may be dropped due to network conditions, but will always arrive in the order they are sent
	/// </summary>
	/// <param name="t_message">the message to send</param>
	public static void SendPrioritySync(NetUtilCustomMessage t_message) {

		g_singleton.SendPrioritySync(t_message);
	}

	/// <summary>
	/// sets the client ready and creates all queued network objects
	/// </summary>
	public static void SetReady() {

		g_singleton.SetReady();
	}

	/// <summary>
	/// returns true if an network game object is owned locally
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static bool OwnsObject(string t_name) {

		return g_singleton.OwnsObject(t_name);
	}

	/// <summary>
	/// returns true if a network host object is owned locally
	/// </summary>
	/// <param name="t_name">the name of the host object</param>
	public static bool OwnsHostObject(string t_name) {

		return g_singleton.OwnsHostObject(t_name);
	}

	/// <summary>
	/// returns true if a network game object exists with the given name
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static bool Contains(string t_name) {

		return g_singleton.Contains(t_name);
	}

	/// <summary>
	/// returns a local instance of a network game object
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public static GameObject GetObject(string t_name) {

		return g_singleton.GetObject(t_name);
	}

	/// <summary>
	/// returns a local instance of a network host object
	/// </summary>
	/// <param name="t_name">the network name of the host object</param>
	public static T GetHostObject<T>(string t_name) {

		return (T)g_singleton.GetHostObject(t_name);
	}

	/// <summary>
	/// returns a list of network names relating to host objects of the given type
	/// </summary>
	/// <param name="t_type">the name of the type of host object to get</param>
	public static List<string> GetHostObjectNames(string t_type) {

		return singleton.GetHostObjectNames(t_type);
	}

	/// <summary>
	/// returns a collection of detected remote games capable of being joined
	/// </summary>
	public static NetUtilGameInfo[] GetGameList() {

		return g_singleton.GetGameList();
	}

	/// <summary>
	/// returns a collection of connections to the host, with name, identifier and ping
	/// </summary>
	public static NetUtilConnection[] GetConnections() {

		return g_singleton.GetConnections();
	}

}
