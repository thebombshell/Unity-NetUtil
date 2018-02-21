﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.SceneManagement;
using System.Net.Security;
using Random = System.Random;

/// <summary>
/// a delegate of a custom message callback
/// </summary>
/// <param name="t_data"></param>
public delegate void NetUtilMessageCallback(object t_data);

/// <summary>
/// NetUtil message types container
/// </summary>
public static class NetUtilMessageType {

	public const byte ERROR = 0;
	public const byte CONNECTION_INFO = 1;
	public const byte HOST_INFO = 2;
	public const byte TALK = 3;
	public const byte BROADCAST = 4;
	public const byte CLIENT_UPDATE = 5;

	public const byte CREATE_OBJECT = 8;
	public const byte DESTROY_OBJECT = 9;
	public const byte REQUEST_OBJECT = 10;
	public const byte SET_SCENE = 11;

	public const byte SYNC_TRANSFORM = 16;

	public const byte CREATE_HOST_OBJECT = 32;
	public const byte DESTROY_HOST_OBJECT = 33;
	public const byte UPDATE_HOST_OBJECT = 34;

	public const byte CUSTOM_MESSAGE = 255;
}

/// <summary>
/// describes the current functionality of the NetUtil
/// </summary>
[Serializable]
public enum NetUtilType : byte {

	/// <summary>
	/// Listens to and collects information on joinable remote games
	/// </summary>
	LISTENER,
	/// <summary>
	/// Hosts a game for other to join
	/// </summary>
	HOST,
	/// <summary>
	/// Joins and participates in a hosted game
	/// </summary>
	CLIENT
}

/// <summary>
/// a data object describing a custom network event
/// </summary>
[Serializable]
public struct NetUtilCustomMessage {

	/// <summary>
	/// creates a message which triggers a netuitl message handler when recieved
	/// </summary>
	/// <param name="t_messageName">the name of the message handler to trigger</param>
	/// <param name="t_messageData">the object to send to the message handler</param>
	public NetUtilCustomMessage(string t_messageName, object t_messageData) {

		isTargeted = false;
		target = "";
		name = t_messageName;
		data = t_messageData;
	}

	/// <summary>
	/// creates a message which calls a function on the target object when recieved
	/// </summary>
	/// <param name="t_targetName">the network name of the target object</param>
	/// <param name="t_messageName">the name of the function to call</param>
	/// <param name="t_messageData">the object to send to the function</param>
	public NetUtilCustomMessage(string t_targetName, string t_messageName, object t_messageData) {

		isTargeted = true;
		target = t_targetName;
		name = t_messageName;
		data = t_messageData;
	}

	/// <summary>
	/// false if message should be processed by a netutil message handler, true if the message should call a function on the target object
	/// </summary>
	public bool isTargeted;
	/// <summary>
	/// the network name of the object to target, if isTargeted is false defaults to ""
	/// </summary>
	public string target;
	/// <summary>
	/// the name of the handler or function triggered by this message
	/// </summary>
	public string name;
	/// <summary>
	/// the data object to be passed to the function or handler triggered by this message
	/// </summary>
	public object data;
}

/// <summary>
/// NetUtil connection info message
/// </summary>
[Serializable]
public struct NetUtilConnection {

	public int connectionId;
	public string name;
	public float ping;
	public float lastConfirmed;
	public bool isClient;
}

/// <summary>
/// NetUtil create object message
/// </summary>
[Serializable]
public struct NetUtilObjectCreator {

	public string name;
	public string prefab;
	public NetUtilVector3 position;
	public NetUtilQuaternion rotation;
	public bool isNetworked;
	[SerializeField]
	public object data;
}

[Serializable]
public struct NetUtilHostObject {

	public string name;
	public string type;
	public int connection;
	public object hostObject;
}

/// <summary>
/// NetUtil transform sync message
/// </summary>
[Serializable]
public struct NetUtilSyncTransform {

	public NetUtilSyncTransform(string t_name, float t_time, Transform t_transform) {

		name = t_name;
		time = t_time;
		position = new NetUtilVector3(t_transform.position);
		rotation = new NetUtilQuaternion(t_transform.rotation);
		localScale = new NetUtilVector3(t_transform.localScale);
	}

	public string name;
	public float time;
	public NetUtilVector3 position;
	public NetUtilQuaternion rotation;
	public NetUtilVector3 localScale;
}

/// <summary>
/// NetUtil host state message
/// </summary>
[Serializable]
public struct NetUtilHostState {

	public float time;
	public string scene;
	public int salt;
}

/// <summary>
/// NetUtil state enumerables
/// </summary>
public enum NetUtilState : byte {

	UNINITIALIZED,
	INITIALIZED,
	CONNECTED
}

/// <summary>
/// NetUtil broadcast message
/// </summary>
[Serializable]
public struct NetUtilGameAdvert {

	public string name;
	public int playerCount;
	public int playerLimit;
}

/// <summary>
/// NetUtil data object containing information about a remote game
/// </summary>
[Serializable]
public struct NetUtilGameInfo {

	public string name;
	public string ip;
	public int port;
	public int playerCount;
	public int playerLimit;
	public float time;
}

/// <summary>
/// a NetUtil data object for communicating with online lobby servers
/// </summary>
[Serializable]
public struct NetUtilJSONGameInfo {

	public string name;
	public int name_length;
	public string ip;
	public int ip_length;
	public int port;
	public int players;
	public int max_players;
	public Int64 time;
}

/// <summary>
/// a NetUtil data object for communicating with online lobby servers
/// </summary>
[Serializable]
public struct NetUtilJSONGameList {

	public NetUtilJSONGameInfo[] games;
}

/// <summary>
/// Network Utility helper class
/// </summary>
public class NetUtil {

	/// <summary>
	/// random number generator used for generating id's
	/// </summary>
	private static Random g_random;

	/// <summary>
	/// generates a network unique identifier and salts it
	/// </summary>
	public string GenId() {

		DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		double epochTime = (DateTime.UtcNow - epochStart).TotalSeconds;
		if ( g_random == null ) {

			g_random = new Random((int)epochTime);
		}
		string id = Convert.ToString((int)epochTime, 16) + Convert.ToString(g_random.Next(), 16) + Convert.ToString(g_random.Next(), 16) + Convert.ToString(isHost ? 0 : m_salt, 16);
		return id;
	}

	// states

	/// <summary>
	/// true if setup is successful
	/// </summary>
	private NetUtilState m_state = NetUtilState.UNINITIALIZED;

	/// <summary>
	/// true if setup is successful
	/// </summary>
	public bool isSetup {

		get {

			return m_state == NetUtilState.INITIALIZED || m_state == NetUtilState.CONNECTED;
		}
	}

	/// <summary>
	/// true if a connection has been confirmed
	/// </summary>
	public bool isConnected {

		get {

			return m_state == NetUtilState.CONNECTED;
		}
	}

	/// <summary>
	/// true if net util is host
	/// </summary>
	public bool isHost {

		get {

			return m_functionality == NetUtilType.HOST && (m_state == NetUtilState.INITIALIZED || m_state == NetUtilState.CONNECTED);
		}
	}

	// transport channels

	/// <summary>
	/// index of channel for low priority synchronization
	/// </summary>
	private int m_lowSyncChannel;

	/// <summary>
	/// index of channel for high priority synchronization
	/// </summary>
	private int m_highSyncChannel;

	/// <summary>
	/// index of channel for ordered messages
	/// </summary>
	private int m_messageChannel;

	// transport hosts

	/// <summary>
	/// index of udp host
	/// </summary>
	private int m_host;

	// connection informations

	/// <summary>
	/// index of the connection id connected to
	/// </summary>
	private int m_hostId;

	/// <summary>
	/// the time at which the host was setup
	/// </summary>
	private float m_hostTime;

	/// <summary>
	/// the time at which the host was setup
	/// </summary>
	public float hostTime {

		get {

			return m_hostTime;
		}
	}

	/// <summary>
	/// a collection of active connections sorted by their unique identifiers
	/// </summary>
	private Dictionary<int, NetUtilConnection> m_connections = new Dictionary<int, NetUtilConnection>();

	/// <summary>
	/// a collection of the current active objects in the scene
	/// </summary>
	private Dictionary<string, NetUtilObjectCreator> m_activeObjects = new Dictionary<string, NetUtilObjectCreator>();

	/// <summary>
	/// the current network scene
	/// </summary>
	private string m_hostScene;

	/// <summary>
	/// the current scene of the host
	/// </summary>
	public string sceneName {
		
		get {

			return m_hostScene;
		}
	}

	/// <summary>
	/// true if scene loaded is host scene
	/// </summary>
	public bool isSceneReady {
		
		get {

			return m_hostScene == SceneManager.GetActiveScene().name;
		}
	}

	/// <summary>
	/// true if client is set up and ready to create network objects
	/// </summary>
	private bool m_isReady = false;

	/// <summary>
	/// true if proper level is loaded and client is ready to create network objects
	/// </summary>
	public bool isReady {

		get {

			return m_hostScene == SceneManager.GetActiveScene().name && m_isReady;
		}
	}

	// local information

	/// <summary>
	/// a collection of prefabs whichc an be used to create objects over the network
	/// </summary>
	private Dictionary<string, GameObject> m_prefabs = new Dictionary<string, GameObject>();

	/// <summary>
	/// a collection of callback functions for handling custom network messages
	/// </summary>
	private Dictionary<string, NetUtilMessageCallback> m_messageHandlers = new Dictionary<string, NetUtilMessageCallback>();

	/// <summary>
	/// a collection of game objects created on the network
	/// </summary>
	private Dictionary<string, GameObject> m_gameObjects = new Dictionary<string, GameObject>();

	/// <summary>
	/// a collection of network objects, associeted with a host on the network
	/// </summary>
	private Dictionary<string, NetUtilHostObject> m_hostObjects = new Dictionary<string, NetUtilHostObject>(); 

	/// <summary>
	/// a dictionary of game object names, sorted by owner
	/// </summary>
	private Dictionary<int, List<string>> m_gameObjectNames = new Dictionary<int, List<string>>();

	/// <summary>
	/// a dictionary of host object names, sorted by owner
	/// </summary>
	private Dictionary<int, List<string>> m_hostObjectNames = new Dictionary<int, List<string>>();

	/// <summary>
	/// a collection of host object names, sorted by type
	/// </summary>
	private Dictionary<string, List<string>> m_hostObjectTypes = new Dictionary<string, List<string>>();

	/// <summary>
	/// a collection of remotely hosted games capable of being joined
	/// </summary>
	private Dictionary<string, NetUtilGameInfo> m_remoteGames = new Dictionary<string, NetUtilGameInfo>();

	/// <summary>
	/// a collection of lan hosted games capable of being joined
	/// </summary>
	private List<NetUtilGameInfo> m_lanGames = new List<NetUtilGameInfo>();

	/// <summary>
	/// a collection of online hosted games capable of being joined
	/// </summary>
	private List<NetUtilGameInfo> m_onlineGames = new List<NetUtilGameInfo>();

	/// <summary>
	/// a WWW request for a list of online games
	/// </summary>
	private WWW m_onlineGamesQuery;

	/// <summary>
	/// a time stamp for the last time a request for an updated online games list was sent.
	/// </summary>
	private float m_onlineGamesQueryTime = 0.0f;

	// client information

	/// <summary>
	/// name of this host on the network
	/// </summary>
	private string m_name = "phillip";

	/// <summary>
	/// name of this host on the network
	/// </summary>
	public string name {

		get {

			return m_name;
		}
	}

	/// <summary>
	/// a unique client integer used in the generation of ids
	/// </summary>
	private int m_salt = 0;

	/// <summary>
	/// the salt of this host on the nextwork
	/// </summary>
	public int salt {
		
		get {

			return m_salt;
		}
	}

	/// <summary>
	/// index of the host socket
	/// </summary>
	private int m_hostSocket;

	/// <summary>
	/// index of the client socket
	/// </summary>
	private int m_clientSocket;

	/// <summary>
	/// a unique index for broadcasting with
	/// </summary>
	private int m_gameId;

	/// <summary>
	/// the major version of the game
	/// </summary>
	private int m_majorVersion;

	/// <summary>
	/// the minor version of the game
	/// </summary>
	private int m_minorVersion;

	/// <summary>
	/// the time of the last ping check
	/// </summary>
	private float m_lastPing;

	/// <summary>
	/// the functionality type of the NetUtil
	/// </summary>
	private NetUtilType m_functionality;

	/// <summary>
	/// the functionality type of the NetUtil
	/// </summary>
	public NetUtilType functionality {

		get {

			return m_functionality;
		}
	}
	
	// constructor

	/// <summary>
	/// Creates a new network utility object
	/// </summary>
	/// <param name="t_name">name of this host on the network</param>
	public NetUtil(int t_hostSocket = 7108, int t_clientSocket = 7109, int t_gameId = 69, int t_majorVersion = 1, int t_minorVersion = 0) {

		m_hostSocket = t_hostSocket;
		m_clientSocket = t_clientSocket;
		m_gameId = t_gameId;
		m_majorVersion = t_majorVersion;
		m_minorVersion = t_minorVersion;
		m_lastPing = 0.0f;

		SceneManager.activeSceneChanged += OnSceneChange;
	}

	/// <summary>
	/// a callback function preparing the NetUtil in the event of a scene change
	/// </summary>
	private void OnSceneChange(Scene t_previousScene, Scene t_nextScene) {

		if ( isHost ) {

			m_hostScene = t_nextScene.name;
			NetUtilObjectCreator[] activeObjects = new NetUtilObjectCreator[m_activeObjects.Count];
			m_activeObjects.Values.CopyTo(activeObjects, 0);
			foreach (NetUtilObjectCreator creator in activeObjects) {

				DestroyObject(creator.name);
			}
			SendMessage(NetUtilMessageType.SET_SCENE, t_nextScene.name);
		}
	}

	/// <summary>
	/// sets the client to ready and creates all queued network objects
	/// </summary>
	public void SetReady() {
	
		foreach ( NetUtilObjectCreator creator in m_activeObjects.Values ) {

			if ( !m_gameObjects.ContainsKey(creator.name) ) {

				CreateLocalObject(creator.prefab, creator.name, creator.position.toVector3(), creator.rotation.toQuaternion(), true, creator.data);
			}
		}
		m_isReady = true;
	}

	// setup

	/// <summary>
	/// sets up network capabilities of a listener, looking for a lobby to join
	/// </summary>
	public void SetupListener() {

		Setup(NetUtilType.LISTENER);
	}

	/// <summary>
	/// sets up network capabilities of a host, hosting a game for others to join
	/// </summary>
	public void SetupHost() {

		Setup(NetUtilType.HOST);
	}

	/// <summary>
	/// sets up network capabilities of a client, joining a remotely hosted game
	/// </summary>
	public void SetupClient() {

		Setup(NetUtilType.CLIENT);
	}

	/// <summary>
	/// sets up the networking capabilities of netutil
	/// </summary>
	private void Setup(NetUtilType t_type) {

		// set up network transport
		if ( !NetworkTransport.IsStarted ) {

			NetworkTransport.Init();
		}

		// set up network topology
		ConnectionConfig config = new ConnectionConfig();
		config.PacketSize = 1024;
		config.FragmentSize = 512 - 128;
		m_lowSyncChannel = config.AddChannel(QosType.Unreliable);
		m_highSyncChannel = config.AddChannel(QosType.Reliable);
		m_messageChannel = config.AddChannel(QosType.ReliableSequenced);
		HostTopology topology = new HostTopology(config, 16);

		// set up host

		switch ( t_type ) {

		case NetUtilType.LISTENER: {

			m_isReady = false;
			m_host = NetworkTransport.AddHost(topology, m_clientSocket);
			byte error;
			NetworkTransport.SetBroadcastCredentials(m_host, m_gameId, m_majorVersion, m_minorVersion, out error);
		}
		break;
		case NetUtilType.HOST: {

			m_isReady = true;
			m_host = NetworkTransport.AddHost(topology, m_hostSocket);
		}
		break;
		case NetUtilType.CLIENT: {

			m_isReady = false;
			m_host = NetworkTransport.AddHost(topology);
		}
		break;
		}

		m_functionality = t_type;

		// set state bools

		m_state = NetUtilState.INITIALIZED;

		DebugConsole.Log("Socket opened");
	}

	/// <summary>
	/// shuts down the networking capabilities of netutil
	/// </summary>
	public void Shutdown() {

		// shut down hosts

		m_state = NetUtilState.UNINITIALIZED;
		NetworkTransport.RemoveHost(m_host);
	}

	/// <summary>
	/// attempts to connect to a similar peer
	/// </summary>
	/// <param name="t_ip">the address of the peer to connect to</param>
	public void Connect(string t_ip, int t_port = -1) {

		byte error = 0;
		if ( t_port <= 0 ) {

			t_port = m_hostSocket;
		}
		m_hostId = NetworkTransport.Connect(m_host, t_ip, t_port, 0, out error);
		AddConnection(m_hostId, "host");

		m_state = NetUtilState.INITIALIZED;
	}

	/// <summary>
	/// starts periodically advertising the hosted game over local network
	/// </summary>
	/// <param name="t_gameName">the label of the hosted game</param>
	public void StartAdvertiseLAN(string t_gameName) {

		byte error;
		NetUtilGameAdvert advert = new NetUtilGameAdvert();
		advert.name = t_gameName;
		advert.playerCount = m_connections.Count;
		advert.playerLimit = 16;
		byte[] message = ConstructMessage(NetUtilMessageType.BROADCAST, advert);
		NetworkTransport.StartBroadcastDiscovery(m_host, m_clientSocket, m_gameId, m_majorVersion, m_minorVersion, message, message.Length, 300, out error);
	}

	/// <summary>
	/// stops advertising the hosted game over local network
	/// </summary>
	public void StopAdvertiseLAN() {

		NetworkTransport.StopBroadcastDiscovery();
	}

	/// <summary>
	/// advertises the game online via php and mysql
	/// </summary>
	/// <param name="t_gameName">the label of the game</param>
	public WWW AdvertiseOnline(string t_gameName) {

		// find ip
		IPHostEntry host;
		string localIp = "";
		host = Dns.GetHostEntry(Dns.GetHostName());
		foreach ( IPAddress ip in host.AddressList ) {

			if ( ip.AddressFamily == AddressFamily.InterNetwork ) {

				localIp = ip.ToString();
				break;
			}
		}

		// create form and fill fields
		WWWForm form = new WWWForm();
		form.AddField("strnk_tnk_name", t_gameName);
		form.AddField("strnk_tnk_ip", localIp);
		form.AddField("strnk_tnk_port", m_hostSocket);
		form.AddField("strnk_tnk_players", m_connections.Count);
		form.AddField("strnk_tnk_max_players", m_connections.Count);

		// create request
		return new WWW("http://www.bombshell93.co.uk/php/stronk_tonk_advertise.php", form);
	}

	/// <summary>
	/// updates the remote games list
	/// </summary>
	private void UpdateRemoteGames() {

		m_remoteGames.Clear();
		GetLANGames();
		GetOnlineGames();
	}

	/// <summary>
	/// updates and refills the remote games list with lan games
	/// </summary>
	private void GetLANGames() {

		List<NetUtilGameInfo> removeGames = new List<NetUtilGameInfo>();
		foreach ( NetUtilGameInfo info in m_lanGames ) {

			if ( Time.time - info.time > 3.0f ) {

				removeGames.Add(info);
			}
			else {

				m_remoteGames[info.ip] = info;
			}
		}
		foreach ( NetUtilGameInfo info in removeGames ) {

			m_lanGames.Remove(info);
		}
	}

	/// <summary>
	/// updates the online games list
	/// </summary>
	private void UpdateOnlineGames() {

		if ( m_onlineGamesQuery == null && Time.time - m_onlineGamesQueryTime > 3.0f ) {

			m_onlineGamesQueryTime = Time.time;
			m_onlineGamesQuery = new WWW("http://www.bombshell93.co.uk/php/stronk_tonk_get_adverts.php");
		}
		else if ( m_onlineGamesQuery != null && m_onlineGamesQuery.isDone ) {

			try {

				NetUtilJSONGameList gameList = JsonUtility.FromJson<NetUtilJSONGameList>(m_onlineGamesQuery.text);
				foreach ( NetUtilJSONGameInfo game in gameList.games ) {

					NetUtilGameInfo info;
					info.name = game.name.Substring(0, game.name_length);
					info.ip = game.ip.Substring(0, game.ip_length);
					info.port = game.port;
					info.playerCount = game.players;
					info.playerLimit = game.max_players;
					info.time = Time.time;
					m_onlineGames.Add(info);
				}
			}
			catch ( Exception e ) {

				DebugConsole.Log(e.Message);
				DebugConsole.Log("json error: " + m_onlineGamesQuery.text);
			}
			m_onlineGamesQuery.Dispose();
			m_onlineGamesQuery = null;
		}
	}

	/// <summary>
	/// updates and refills the remote games list with online games
	/// </summary>
	private void GetOnlineGames() {

		List<NetUtilGameInfo> removeGames = new List<NetUtilGameInfo>();
		foreach ( NetUtilGameInfo info in m_onlineGames ) {

			if ( Time.time - info.time > 3.0f ) {

				removeGames.Add(info);
			}
			else {

				m_remoteGames[info.ip] = info;
			}
		}
		foreach ( NetUtilGameInfo info in removeGames ) {

			m_onlineGames.Remove(info);
		}
	}

	/// <summary>
	/// adds a connection with given connection id and name to netutil
	/// </summary>
	/// <param name="t_id">connection id of the new connection</param>
	/// <param name="t_name">name of the new connection</param>
	private void AddConnection(int t_id, string t_name) {

		NetUtilConnection connection = new NetUtilConnection();
		connection.connectionId = t_id;
		connection.name = t_name;
		connection.ping = 0.0f;
		m_connections[t_id] = connection;
	}

	// handlers

	/// <summary>
	/// periodically updates ping values on host
	/// </summary>
	public void HandleConnections() {

		if ( isHost && Time.time - m_lastPing > 3.0f ) {

			NetUtilConnection connection;
			foreach ( NetUtilConnection info in m_connections.Values ) {

				connection = info;
				connection.connectionId = 1 + info.connectionId;
				connection.isClient = true;
				SendSync(NetUtilMessageType.CONNECTION_INFO, connection);
			}
			connection = new NetUtilConnection();
			connection.name = m_name;
			connection.ping = 0.0f;
			connection.isClient = false;
			SendSync(NetUtilMessageType.CONNECTION_INFO, connection);

			m_lastPing = Time.time;
			SendMessage(NetUtilMessageType.CLIENT_UPDATE, m_lastPing);
		}

		List<int> timedOutConnections = new List<int>();

		foreach ( NetUtilConnection info in m_connections.Values ) {

			if ( Time.time - info.lastConfirmed > 10.0f ) {

				timedOutConnections.Add(info.connectionId);
			}
		}

		foreach ( int id in timedOutConnections ) {

			m_connections.Remove(id);
		}
	}

	/// <summary>
	/// checks for and handles messages recieved via udp host
	/// </summary>
	public void HandleMessages() {

		int hostId;
		int connectionId;
		int channelId;
		byte[] buffer = new byte[1024];
		int size;
		byte error;
		NetworkEventType eventType;

		if ( m_functionality == NetUtilType.LISTENER ) {

			UpdateOnlineGames();
			UpdateRemoteGames();
		}

		do {

			eventType = NetworkTransport.Receive(out hostId, out connectionId, out channelId, buffer, 1024, out size, out error);
			if ( eventType != NetworkEventType.Nothing ) {

				//Debug.Log("[" + hostId + ", " + connectionId + ", " + channelId + ", " + eventType + "]: " + size + ", " + buffer[0]);
			}
			switch ( eventType ) {

			case NetworkEventType.DataEvent:

			HandleData(connectionId, buffer, size);
			break;
			case NetworkEventType.ConnectEvent:

			HandleConnect(connectionId);
			break;
			case NetworkEventType.DisconnectEvent:

			HandleDisconnect(connectionId);
			break;
			case NetworkEventType.BroadcastEvent:

			if ( m_functionality == NetUtilType.LISTENER ) {

				HandleBroadcast();
			}
			break;
			}
		}
		while ( eventType != NetworkEventType.Nothing );
	}

	/// <summary>
	/// handles data network messages
	/// </summary>
	/// <param name="connectionId">connection id the message came from</param>
	/// <param name="buffer">the serialized buffer of the message</param>
	/// <param name="size">the size of the message</param>
	private void HandleData(int connectionId, byte[] buffer, int size) {

		switch ( buffer[0] ) {
		case NetUtilMessageType.CONNECTION_INFO:

		HandleConnectionInfo(connectionId, buffer, size);
		break;
		case NetUtilMessageType.HOST_INFO:

		HandleHostInfo(connectionId, buffer, size);
		break;
		case NetUtilMessageType.TALK:

		HandleTalk(connectionId, buffer, size);
		break;
		case NetUtilMessageType.CREATE_OBJECT:

		HandleCreateObject(connectionId, buffer, size);
		break;
		case NetUtilMessageType.DESTROY_OBJECT:

		HandleDestroyObject(connectionId, buffer, size);
		break;
		case NetUtilMessageType.REQUEST_OBJECT:

		HandleRequestObject(connectionId, buffer, size);
		break;
		case NetUtilMessageType.SYNC_TRANSFORM:

		HandleSyncTransform(connectionId, buffer, size);
		break;
		case NetUtilMessageType.CREATE_HOST_OBJECT:

		HandleCreateHostObject(connectionId, buffer, size);
		break;
		case NetUtilMessageType.DESTROY_HOST_OBJECT:

		HandleDestroyHostObject(connectionId, buffer, size);
		break;
		case NetUtilMessageType.UPDATE_HOST_OBJECT:

		HandleUpdateHostObject(connectionId, buffer, size);
		break;
		case NetUtilMessageType.CUSTOM_MESSAGE:

		HandleCustomMessage(connectionId, buffer, size);
		break;
		case NetUtilMessageType.CLIENT_UPDATE:

		HandleClientUpdate(connectionId, buffer, size);
		break;
		case NetUtilMessageType.SET_SCENE:

		HandleSetScene(connectionId, buffer, size);
		break;
		}
	}

	/// <summary>
	/// handles connection messages
	/// </summary>
	/// <param name="connectionId">connection id the message came from</param>
	private void HandleConnect(int connectionId) {

		if ( isHost ) {

			if ( !m_connections.ContainsKey(connectionId) ) {

				AddConnection(connectionId, "");
				byte error;

				NetUtilHostState state = new NetUtilHostState();

				state.time = m_hostTime;
				state.scene = m_hostScene;
				state.salt = ++m_salt;

				SendMessage(m_host, connectionId, m_messageChannel, NetUtilMessageType.HOST_INFO, state, out error);

				foreach ( NetUtilObjectCreator creator in m_activeObjects.Values ) {

					SendMessage(m_host, connectionId, m_messageChannel, NetUtilMessageType.CREATE_OBJECT, creator, out error);
				}
				foreach ( NetUtilHostObject hostObject in m_hostObjects.Values ) {

					SendMessage(m_host, connectionId, m_messageChannel, NetUtilMessageType.CREATE_HOST_OBJECT, hostObject, out error);
				}

				m_state = NetUtilState.CONNECTED;
			}
		}
		else {

			if ( connectionId != m_hostId ) {

				DebugConsole.Log("an unknown connection attempt has occured.");
			}
			else {

				NetUtilConnection connection = new NetUtilConnection();
				connection.connectionId = -1;
				connection.name = m_name;
				connection.ping = 0.0f;
				SendMessage(NetUtilMessageType.CONNECTION_INFO, connection);
			}
		}
	}

	/// <summary>
	/// handles disconnection
	/// </summary>
	private void HandleDisconnect(int connectionId) {

		if ( !isHost && !m_connections[connectionId].isClient ) {

			throw new Exception("You have been disconnected from the host");
		}
		else {

			m_connections.Remove(connectionId);
		}
	}

	/// <summary>
	/// handles broadcast messages
	/// </summary>
	private void HandleBroadcast() {

		string ip;
		int port;
		byte error;
		byte[] buffer = new byte[1024];
		int size;

		NetworkTransport.GetBroadcastConnectionInfo(m_hostId, out ip, out port, out error);
		NetworkTransport.GetBroadcastConnectionMessage(m_hostId, buffer, 1024, out size, out error);

		NetUtilGameAdvert advert = ConstructType<NetUtilGameAdvert>(buffer, 1, size - 1);
		NetUtilGameInfo gameInfo = new NetUtilGameInfo();
		gameInfo.name = advert.name;
		gameInfo.playerCount = advert.playerCount;
		gameInfo.playerLimit = advert.playerLimit;
		gameInfo.ip = ip;
		gameInfo.port = port;
		gameInfo.time = Time.time;
		
		m_lanGames.Add(gameInfo);
	}

	/// <summary>
	/// handles incoming connection info messages
	/// </summary>
	private void HandleConnectionInfo(int connectionId, byte[] buffer, int size) {

		NetUtilConnection connection = ConstructType<NetUtilConnection>(buffer, 1, size - 1);
		connection.lastConfirmed = Time.time;
		if ( isHost ) {

			connection.connectionId = connectionId;
			connection.isClient = true;
			foreach ( NetUtilConnection ownConnection in m_connections.Values ) {

				if ( ownConnection.name == connection.name ) {

					connection.name = connection.name + "#" + GenId();
					break;
				}
			}
			m_connections[connectionId] = connection;
		}
		else if ( connection.isClient ) {

			m_connections[connection.connectionId] = connection;
		}
		else {

			m_connections[connectionId] = connection;
		}
	}

	/// <summary>
	/// handles an incoming host info message
	/// </summary>
	private void HandleHostInfo(int connectionId, byte[] buffer, int size) {

		NetUtilHostState state = ConstructType<NetUtilHostState>(buffer, 1, size - 1);

		m_hostTime = state.time;
		m_hostScene = state.scene;
		m_salt = state.salt;

		if ( SceneManager.GetActiveScene().name != m_hostScene ) {

			SceneManager.LoadScene(m_hostScene, LoadSceneMode.Single);
		}

		m_state = NetUtilState.CONNECTED;
	}

	/// <summary>
	/// Handles client ping update messages
	/// </summary>
	private void HandleClientUpdate(int t_connectionId, byte[] t_buffer, int t_size) {

		float time = ConstructType<float>(t_buffer, 1, t_size - 1);
		if ( isHost ) {

			NetUtilConnection connection = m_connections[t_connectionId];
			connection.ping = Time.time - time;
			connection.lastConfirmed = Time.time;
			m_connections[t_connectionId] = connection;
		}
		else {

			SendMessage(NetUtilMessageType.CLIENT_UPDATE, time);
		}
	}

	/// <summary>
	/// handles incoming talk messages
	/// </summary>
	private void HandleTalk(int connectionId, byte[] buffer, int size) {

		string message = ConstructType<string>(buffer, 1, size - 1);
		if ( m_connections.ContainsKey(connectionId) ) {

			DebugConsole.Log("[" + m_connections[connectionId].name + "]: " + message);
		}
		else {

			DebugConsole.Log("[" + connectionId + "]: " + message);
		}
	}

	/// <summary>
	/// handles incoming create object messages
	/// </summary>
	private void HandleCreateObject(int connectionId, byte[] buffer, int size) {

		NetUtilObjectCreator message = ConstructType<NetUtilObjectCreator>(buffer, 1, size - 1);
		CreateObject(message.prefab, message.position.toVector3(), message.rotation.toQuaternion(), message.data, message.isNetworked, message.name, connectionId);
	}

	/// <summary>
	/// handles incoming destroy object messages
	/// </summary>
	private void HandleDestroyObject(int connectionId, byte[] buffer, int size) {

		string name = ConstructType<string>(buffer, 1, size - 1);

		if ( !m_gameObjects.ContainsKey(name) ) {

			return;
		}

		DestroyObject(name, connectionId);
	}

	/// <summary>
	/// handles incoming create object messages
	/// </summary>
	private void HandleCreateHostObject(int connectionId, byte[] buffer, int size) {

		NetUtilHostObject message = ConstructType<NetUtilHostObject>(buffer, 1, size - 1);
		CreateHostObject(message.type, message.hostObject, message.name, connectionId);
	}

	/// <summary>
	/// handles incoming destroy object messages
	/// </summary>
	private void HandleDestroyHostObject(int connectionId, byte[] buffer, int size) {

		string name = ConstructType<string>(buffer, 1, size - 1);

		if ( !m_hostObjects.ContainsKey(name) ) {

			return;
		}

		DestroyHostObject(name, connectionId);
	}

	/// <summary>
	/// handles incoming update object messages
	/// </summary>
	private void HandleUpdateHostObject(int connectionId, byte[] buffer, int size) {

		NetUtilHostObject message = ConstructType<NetUtilHostObject>(buffer, 1, size - 1);
		UpdateHostObject(message.name, message.hostObject, connectionId);
	}

	/// <summary>
	/// handles incoming object ownership request messages
	/// </summary>
	private void HandleRequestObject(int connectionId, byte[] buffer, int size) {

		string message = ConstructType<string>(buffer, 1, size - 1);
		RequestObject(message, connectionId);
	}

	/// <summary>
	/// handles incoming set scene messages
	/// </summary>
	private void HandleSetScene(int connectionId, byte[] buffer, int size) {

		if ( !isHost ) {

			m_hostScene = ConstructType<string>(buffer, 1, size - 1);
			m_activeObjects.Clear();
			m_isReady = false;
			SceneManager.LoadScene(m_hostScene, LoadSceneMode.Single);
		}
	}

	/// <summary>
	/// handles incoming sync transform messages
	/// </summary>
	private void HandleSyncTransform(int connectionId, byte[] buffer, int size) {

		try {
			NetUtilSyncTransform message = ConstructType<NetUtilSyncTransform>(buffer, 1, size - 1);
			SyncTransform(message, connectionId);
		}
		catch ( Exception ) {

			Debug.Log(buffer.Length);
			Debug.Log(size);
		}
	}

	/// <summary>
	/// handles incoming custom messages
	/// </summary>
	private void HandleCustomMessage(int connectionId, byte[] buffer, int size) {

		NetUtilCustomMessage message = ConstructType<NetUtilCustomMessage>(buffer, 1, size - 1);
		HandleCustomMessage(message);
		if ( isHost ) {

			SendMessage(message, connectionId);
		}
	}

	/// <summary>
	/// enacts custom messages instructions
	/// </summary>
	private void HandleCustomMessage(NetUtilCustomMessage t_message) {

		if ( t_message.isTargeted ) {

			if ( m_gameObjects.ContainsKey(t_message.target) ) {

				m_gameObjects[t_message.target].BroadcastMessage(t_message.name, t_message.data);
			}
		}
		else if ( m_messageHandlers.ContainsKey(t_message.name) ) {

			m_messageHandlers[t_message.name](t_message.data);
		}
	}

	// message helpers

	/// <summary>
	/// Serialize type and object to message
	/// </summary>
	/// <param name="t_type">type of message</param>
	/// <param name="t_object">object associated with message</param>
	/// <returns>the buffer of the constructed message</returns>
	private byte[] ConstructMessage(byte t_type, object t_object) {

		IFormatter formatter = new BinaryFormatter();
		MemoryStream stream = new MemoryStream();
		stream.WriteByte(t_type);
		formatter.Serialize(stream, t_object);
		return stream.ToArray();
	}

	/// <summary>
	/// Deserialize object from message
	/// </summary>
	/// <typeparam name="T">type to deserialize to</typeparam>
	/// <param name="buffer">buffer to deserialize from</param>
	/// <param name="offset">offset of object in buffer</param>
	/// <param name="size">size of object in buffer</param>
	/// <returns>the deserialized object</returns>
	private T ConstructType<T>(byte[] buffer, int offset, int size) {

		IFormatter formatter = new BinaryFormatter();
		MemoryStream stream = new MemoryStream(buffer, offset, size);
		return (T)formatter.Deserialize(stream);
	}

	/// <summary>
	/// private helper function for packaging and sending messages
	/// </summary>
	/// <param name="t_host">host id to send message by</param>
	/// <param name="t_connection">connection id to send message to</param>
	/// <param name="t_channel">channel to send message with</param>
	/// <param name="t_type">type of message to send</param>
	/// <param name="t_object">object associated with message</param>
	/// <param name="error">out error from transport layer</param>
	private void SendMessage(int t_host, int t_connection, int t_channel, byte t_type, object t_object, out byte error) {

		byte[] buffer = ConstructMessage(t_type, t_object);
		if ( buffer.Length > 1024 ) {

			throw new ArgumentException("this message is too large to be handled properly");
		}
		NetworkTransport.Send(t_host, t_connection, t_channel, buffer, buffer.Length, out error);
	}

	// raw message senders

	/// <summary>
	/// sends a message which will arrive and in order
	/// </summary>
	/// <param name="t_type">type of message to send</param>
	/// <param name="t_object">object to send with message</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	private void SendMessage(byte t_type, object t_object, int t_connectionOrigin = -1) {

		byte error;
		if ( isHost ) {

			// if is host send message to all connections
			foreach ( int connectionId in m_connections.Keys ) {

				if ( connectionId != t_connectionOrigin ) {

					SendMessage(m_host, connectionId, m_messageChannel, t_type, t_object, out error);
				}
			}
		}
		else if ( m_hostId != t_connectionOrigin ) {

			// if is client send message to host
			SendMessage(m_host, m_hostId, m_messageChannel, t_type, t_object, out error);
		}
	}

	/// <summary>
	/// sends a message which may arrive
	/// </summary>
	/// <param name="t_type">type of message to send</param>
	/// <param name="t_object">object to send with message</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	private void SendSync(byte t_type, object t_object, int t_connectionOrigin = -1) {

		byte error;
		if ( isHost ) {

			// if is host send message to all connections
			foreach ( int connectionId in m_connections.Keys ) {

				if ( connectionId != t_connectionOrigin ) {

					SendMessage(m_host, connectionId, m_lowSyncChannel, t_type, t_object, out error);
				}
			}
		}
		else if ( m_hostId != t_connectionOrigin ) {

			// if is client send message to host
			SendMessage(m_host, m_hostId, m_lowSyncChannel, t_type, t_object, out error);
		}
	}

	/// <summary>
	/// sends a message which will arrive
	/// </summary>
	/// <param name="t_type">type of message to send</param>
	/// <param name="t_object">object to send with message</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	private void SendPrioritySync(byte t_type, object t_object, int t_connectionOrigin = -1) {

		byte error;
		if ( isHost ) {

			// if is host send message to all connections
			foreach ( int connectionId in m_connections.Keys ) {

				if ( connectionId != t_connectionOrigin ) {

					SendMessage(m_host, connectionId, m_highSyncChannel, t_type, t_object, out error);
				}
			}
		}
		else if ( m_hostId != t_connectionOrigin ) {

			// if is client send message to host
			SendMessage(m_host, m_hostId, m_highSyncChannel, t_type, t_object, out error);
		}
	}

	// custom message senders

	/// <summary>
	/// sends a custom message which is will arrive and in order
	/// </summary>
	/// <param name="t_message">the message to send</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	public void SendMessage(NetUtilCustomMessage t_message, int t_connectionOrigin = -1) {

		SendMessage(NetUtilMessageType.CUSTOM_MESSAGE, t_message, t_connectionOrigin);
		HandleCustomMessage(t_message);
	}

	/// <summary>
	/// sends a custom message which may arrive
	/// </summary>
	/// <param name="t_message">the message to send</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	public void SendSync(NetUtilCustomMessage t_message, int t_connectionOrigin = -1) {

		SendSync(NetUtilMessageType.CUSTOM_MESSAGE, t_message, t_connectionOrigin);
		HandleCustomMessage(t_message);
	}

	/// <summary>
	/// sends a custom message which will arrive
	/// </summary>
	/// <param name="t_message">the message to send</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	public void SendPrioritySync(NetUtilCustomMessage t_message, int t_connectionOrigin = -1) {

		SendPrioritySync(NetUtilMessageType.CUSTOM_MESSAGE, t_message, t_connectionOrigin);
		HandleCustomMessage(t_message);
	}

	// wrapped up message senders

	/// <summary>
	/// sends a talk message
	/// </summary>
	/// <param name="t_text">the text message to send</param>
	public void Talk(string t_text, int t_connectionOrigin = -1) {

		SendMessage(NetUtilMessageType.TALK, t_text, t_connectionOrigin);
	}

	/// <summary>
	/// creates a game object over the network
	/// </summary>
	/// <param name="t_prefab">the name of the prefab used to create the object</param>
	/// <param name="t_position">the spawn position of the object</param>
	/// <param name="t_rotation">the spawn rotation of the object</param>
	/// <param name="t_data">a serializable object to pass other creation data with</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	/// <returns>the unique netutil name of the object</returns>
	public string CreateObject(string t_prefab, Vector3 t_position, Quaternion t_rotation, object t_data, bool t_isNetworked = true, string t_name = "", int t_connectionOrigin = -1) {

		// ensure a dupplicate is not being made
		if ( m_gameObjects.ContainsKey(t_name) ) {

			throw new ArgumentException("\"" + t_name + "\" is already the name of a network gameobject");
		}

		// ensure the data object is valid
		if ( t_data == null ) {

			t_data = "";
		}
		else if ( !t_data.GetType().IsSerializable ) {

			throw new ArgumentException("the type \"" + t_data.GetType().FullName + "\" is not serializable and can not be properly sent over network");
		}

		// if no name for the object is given, generate one
		if ( t_name == "" ) {

			t_name = GenId();
		}

		NetUtilObjectCreator creator = new NetUtilObjectCreator();
		creator.name = t_name;
		creator.prefab = t_prefab;
		creator.position = new NetUtilVector3(t_position);
		creator.rotation = new NetUtilQuaternion(t_rotation);
		creator.isNetworked = t_isNetworked;
		creator.data = t_data;


		// if object is networked, add to relevant lists
		if ( t_isNetworked ) {

			m_activeObjects[t_name] = creator;

			// if created localled add to local authority objects
			if ( !m_gameObjectNames.ContainsKey(t_connectionOrigin) ) {

				m_gameObjectNames.Add(t_connectionOrigin, new List<string>());
			}
			m_gameObjectNames[t_connectionOrigin].Add(t_name);
		}
		// if is ready to spawn objects, proceed
		if ( isReady ) {

			SendMessage(NetUtilMessageType.CREATE_OBJECT, creator, t_connectionOrigin);
			CreateLocalObject(t_prefab, t_name, t_position, t_rotation, t_isNetworked, t_data);
		}
		else {

			Debug.Log("Not Ready");
		}
		return t_name;
	}

	/// <summary>
	/// destroys a game object over the network
	/// </summary>
	/// <param name="t_name">the name of the network game object to destroy</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	public void DestroyObject(string t_name, int t_connectionOrigin = -1) {

		if ( !m_gameObjects.ContainsKey(t_name) ) {

			throw new ArgumentException("\"" + t_name + "\" is not an existing netutil game object");
		}
		m_activeObjects.Remove(t_name);
		foreach ( int connection in m_gameObjectNames.Keys ) {

			if ( m_gameObjectNames[connection].Contains(t_name) ) {

				m_gameObjectNames[connection].Remove(t_name);
				break;
			}
		}
		if ( isReady ) {

			SendMessage(NetUtilMessageType.DESTROY_OBJECT, t_name, t_connectionOrigin);
			DestroyLocalObject(t_name);
		}
	}

	/// <summary>
	/// requests local ownership of a network game object
	/// </summary>
	/// <param name="t_name">the name of the game object</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	public void RequestObject(string t_name, int t_connectionOrigin = -1) {

		// ensure the object exists
		if ( !m_gameObjects.ContainsKey(t_name) ) {

			throw new ArgumentException("\"" + t_name + "\" is not an existing netutil game object");
		}

		// if called locally
		if ( t_connectionOrigin == -1 ) {

			// ensure object is not already owned
			if ( m_gameObjectNames.ContainsKey(-1) && m_gameObjectNames[-1].Contains(t_name) ) {

				throw new ArgumentException("\"" + t_name + "\" is already owned locally");
			}
			SendMessage(NetUtilMessageType.REQUEST_OBJECT, t_name, t_connectionOrigin);
			if ( !m_gameObjectNames.ContainsKey(-1) ) {

				m_gameObjectNames.Add(-1, new List<string>());
			}
			m_gameObjectNames[-1].Add(t_name);
			m_gameObjects[t_name].BroadcastMessage("NetUtilAuthorityGranted");
		}
		// if called remotely
		else {

			// if has object relinquish ownership of object
			if ( m_gameObjectNames.ContainsKey(-1) && m_gameObjectNames[-1].Contains(t_name) ) {

				DebugConsole.Log("I have relinquished control of " + t_name);
				m_gameObjects[t_name].BroadcastMessage("NetUtilAuthoritySiezed");
				m_gameObjectNames[-1].Remove(t_name);
			}
			// else propogate the message
			else {

				SendMessage(NetUtilMessageType.REQUEST_OBJECT, t_name, t_connectionOrigin);
			}
		}

	}

	/// <summary>
	/// creates a host object over the network
	/// </summary>
	/// <param name="t_object">the host object to create</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	/// <returns>the unique netutil name of the object</returns>
	public string CreateHostObject(string t_type, object t_data = null, string t_name = "", int t_connectionOrigin = -1) {

		// ensure a dupplicate is not being made
		if ( m_hostObjects.ContainsKey(t_name) ) {

			throw new ArgumentException("\"" + t_name + "\" is already the name of a network host object");
		}

		// ensure the data object is valid
		if ( t_data == null ) {

			t_data = "";
		}
		else if ( !t_data.GetType().IsSerializable ) {

			throw new ArgumentException("the type \"" + t_data.GetType().FullName + "\" is not serializable and can not be properly sent over network");
		}

		// if no name for the object is given, generate one
		if ( t_name == "" ) {

			t_name = GenId();
		}

		NetUtilHostObject hostObject = new NetUtilHostObject();
		hostObject.name = t_name;
		hostObject.type = t_type;
		hostObject.hostObject = t_data;
		if ( !m_hostObjectTypes.ContainsKey(t_type) ) {

			m_hostObjectTypes.Add(t_type, new List<string>());
		}
		m_hostObjectTypes[t_type].Add(t_name);
		m_hostObjects[t_name] = hostObject;
		SendMessage(NetUtilMessageType.CREATE_HOST_OBJECT, hostObject, t_connectionOrigin);
		if ( !m_hostObjectNames.ContainsKey(t_connectionOrigin) ) {

			m_hostObjectNames.Add(t_connectionOrigin, new List<string>());
		}
		m_hostObjectNames[t_connectionOrigin].Add(t_name);
		return t_name;
	}

	/// <summary>
	/// destroys a host object over the network
	/// </summary>
	/// <param name="t_name">the name of the host object to destroy</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	public void DestroyHostObject(string t_name, int t_connectionOrigin = -1) {

		if ( !m_hostObjects.ContainsKey(t_name) ) {

			throw new ArgumentException("\"" + t_name + "\" is not an existing netutil host object");
		}
		string type = m_hostObjects[t_name].type;
		m_hostObjectTypes[type].Remove(t_name);
		if ( m_hostObjectTypes[type].Count <= 0 ) {

			m_hostObjectTypes.Remove(type);
		}
		m_hostObjects.Remove(t_name);
		SendMessage(NetUtilMessageType.DESTROY_HOST_OBJECT, t_name, t_connectionOrigin);
		foreach ( int connection in m_hostObjectNames.Keys ) {

			if ( m_hostObjectNames[connection].Contains(t_name) ) {

				m_hostObjectNames[connection].Remove(t_name);
				break;
			}
		}
	}

	/// <summary>
	/// updates a host object over the network
	/// </summary>
	/// <param name="t_name">the name of the host object to update</param>
	/// <param name="t_data">the new data to be held in host object</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	public void UpdateHostObject(string t_name,  object t_data = null, int t_connectionOrigin = -1) {

		if ( !m_hostObjects.ContainsKey(t_name) ) {

			throw new ArgumentException("\"" + t_name + "\" is not an existing netutil host object");
		}
		if ( t_data == null ) {

			t_data = "";
		}
		NetUtilHostObject hostObject = new NetUtilHostObject();
		hostObject.name = t_name;
		hostObject.type = m_hostObjects[t_name].type;
		hostObject.hostObject = t_data;
		m_hostObjects[t_name] = hostObject;
		SendMessage(NetUtilMessageType.UPDATE_HOST_OBJECT, hostObject, t_connectionOrigin);
	}

	/// <summary>
	/// syncs a game objects transform over the network
	/// </summary>
	/// <param name="t_name">the name of the object to synchronize</param>
	/// <param name="t_transform">the transform to synchronize with</param>
	/// <param name="t_connectionOrigin">the origin of the create object message, defaults to -1 for local</param>
	public void SyncTransform(NetUtilSyncTransform t_message, int t_connectionOrigin = -1) {

		if ( t_connectionOrigin == -1 && !m_gameObjects.ContainsKey(t_message.name) ) {

			throw new ArgumentException("\"" + t_message.name + "\" is not an existing netutil game object");
		}
		if ( !m_gameObjects.ContainsKey(t_message.name) ) {

			return;
		}
		SendPrioritySync(NetUtilMessageType.SYNC_TRANSFORM, t_message, t_connectionOrigin);
		if ( t_connectionOrigin != -1 ) {

			m_gameObjects[t_message.name].BroadcastMessage("NetUtilSyncTransform", t_message);
		}
	}

	// wrapper helpers

	/// <summary>
	/// creates a network game object locally
	/// </summary>
	/// <param name="t_prefab">the name of the prefab used to create the object</param>
	/// <param name="t_name">the name of the object</param>
	/// <param name="t_position">the spawn position of the object</param>
	/// <param name="t_rotation">the spawn rotation of the object</param>
	/// <param name="t_data">a serializable object to pass other creation data with</param>
	private void CreateLocalObject(string t_prefab, string t_name, Vector3 t_position, Quaternion t_rotation, bool t_isNetworked, object t_data) {

		// ensure the prefab exists
		if ( !m_prefabs.ContainsKey(t_prefab) ) {

			throw new ArgumentException("\"" + t_prefab + "\" is a prefab not registered in netuitl");
		}

		GameObject gameObject = GameObject.Instantiate(m_prefabs[t_prefab], t_position, t_rotation);
		gameObject.name = t_name;
		if ( t_isNetworked ) {

			m_gameObjects[t_name] = gameObject;
		}
		try {

			gameObject.SendMessage("NetUtilInitialize", t_data);
		}
		catch (Exception) {

		}
	}

	/// <summary>
	/// destroys a network game object locally
	/// </summary>
	/// <param name="t_name">the name of the object to destroy</param>
	private void DestroyLocalObject(string t_name) {

		GameObject gameObject = m_gameObjects[t_name];
		m_gameObjects.Remove(t_name);
		GameObject.Destroy(gameObject);
	}

	/// <summary>
	/// registers an object as a prefab for creation over the network
	/// </summary>
	/// <param name="t_name">the name used to refer to the prefab</param>
	/// <param name="t_object">the prefab gameobject</param>
	public void RegisterPrefab(string t_name, GameObject t_object) {

		m_prefabs[t_name] = t_object;
	}

	/// <summary>
	/// registers a callback function to handle custom network messages
	/// </summary>
	/// <param name="t_type">the type of the message to handle</param>
	/// <param name="t_handler">the callback function acting as the message handler</param>
	public void RegisterMessageHandler(string t_type, NetUtilMessageCallback t_handler) {

		m_messageHandlers[t_type] = t_handler;
	}

	// object management methods

	/// <summary>
	/// returns true if an network game object is owned locally
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public bool OwnsObject(string t_name) {

		return m_gameObjectNames.ContainsKey(-1) && m_gameObjectNames[-1].Contains(t_name);
	}

	/// <summary>
	/// returns true if a network host object is owned locally
	/// </summary>
	/// <param name="t_name">the name of the local host object</param>
	public bool OwnsHostObject(string t_name) {

		return m_hostObjectNames.ContainsKey(-1) && m_hostObjectNames[-1].Contains(t_name);
	}

	/// <summary>
	/// returns true if a network game object exists with the given name
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public bool Contains(string t_name) {

		return m_gameObjects.ContainsKey(t_name);
	}

	/// <summary>
	/// returns a local instance of a network game object
	/// </summary>
	/// <param name="t_name">the name of the object</param>
	public GameObject GetObject(string t_name) {

		return m_gameObjects[t_name];
	}

	/// <summary>
	/// returns a local instance of a network host object
	/// </summary>
	/// <param name="t_name">the network name of the host object</param>
	public object GetHostObject(string t_name) {

		if ( !m_hostObjects.ContainsKey(t_name) ) {

			throw new ArgumentOutOfRangeException("A host object of name \"" + t_name + "\" does not exist.");
		}
		return m_hostObjects[t_name].hostObject;
	}
	
	/// <summary>
	/// returns a list of network names relating to host objects of the given type
	/// </summary>
	/// <param name="t_type">the name of the type of host object to get</param>
	public List<string> GetHostObjectNames(string t_type) {

		List<string> output = new List<string>();

		if ( m_hostObjectTypes.ContainsKey(t_type) ) {

			output.AddRange(m_hostObjectTypes[t_type]);
		}
		return output;
	}

	/// <summary>
	/// returns a collection of detected remote games capable of being joined
	/// </summary>
	public NetUtilGameInfo[] GetGameList() {

		NetUtilGameInfo[] output = new NetUtilGameInfo[m_remoteGames.Count];
		m_remoteGames.Values.CopyTo(output, 0);
		return output;
	}
	
	/// <summary>
	/// returns a list of connections to the host including name and ping
	/// </summary>
	public NetUtilConnection[] GetConnections() {

		NetUtilConnection[] connections = new NetUtilConnection[isHost ? m_connections.Count + 1 : m_connections.Count];
		m_connections.Values.CopyTo(connections, 0);
		if ( isHost ) {

			NetUtilConnection hostConnection = new NetUtilConnection();
			hostConnection.connectionId = -1;
			hostConnection.name = m_name;
			hostConnection.isClient = false;
			hostConnection.ping = 0.0f;
			hostConnection.lastConfirmed = Time.time;
			connections[m_connections.Count] = hostConnection;
		}
		return connections;
	}
}