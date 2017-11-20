﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

public static class NetUtilMessageType {

	public const byte ERROR = 0;
	public const byte CONNECTION_INFO = 1;
	public const byte TALK = 2;
	public const byte CREATE_OBJECT = 3;
	public const byte DESTROY_OBJECT = 4;
}

[Serializable]
public struct NetUtilConnection {

	public int connectionId;
	public string name;
}

/// <summary>
/// Network Utility helper class
/// </summary>
public class NetUtil {

	// state booleans

	/// <summary>
	/// true if setup is successful
	/// </summary>
	private bool m_isSetup = false;

	/// <summary>
	/// true if net util is host
	/// </summary>
	private bool m_isHost;

	/// <summary>
	/// true if setup is successful
	/// </summary>
	public bool isSetup {
		
		get {

			return m_isSetup;
		}
	}

	/// <summary>
	/// true if net util is host
	/// </summary>
	public bool isHost {
		
		get {

			return m_isHost;
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
	/// a collection of active connections sorted by their unique identifiers
	/// </summary>
	private Dictionary<int, NetUtilConnection> m_connections = new Dictionary<int, NetUtilConnection>();

	// client information

	/// <summary>
	/// name of this host on the network
	/// </summary>
	private string m_name = "phillip";

	/// <summary>
	/// index of the host socket
	/// </summary>
	private int m_hostSocket;

	/// <summary>
	/// index of the client socket
	/// </summary>
	private int m_clientSocket;

	/// <summary>
	/// name of this host on the network
	/// </summary>
	public string name {

		get {

			return m_name;
		}
	}

	/// <summary>
	/// Creates a new network utility object
	/// </summary>
	/// <param name="t_name">name of this host on the network</param>
	public NetUtil(int t_hostSocket, int t_clientSocket) {
	
		m_hostSocket = t_hostSocket;
		m_clientSocket = t_clientSocket;
	}

	/// <summary>
	/// sets up the networking capabilities of netutil
	/// </summary>
	public void Setup(bool isHosting) {

		// set up network transport
		NetworkTransport.Init();

		// set up network topology
		ConnectionConfig config = new ConnectionConfig();
		m_lowSyncChannel = config.AddChannel(QosType.StateUpdate);
		m_highSyncChannel = config.AddChannel(QosType.ReliableStateUpdate);
		m_messageChannel = config.AddChannel(QosType.ReliableSequenced);
		HostTopology topology = new HostTopology(config, 16);

		// set up host
		if (isHosting) {

			m_host = NetworkTransport.AddHost(topology, m_hostSocket);
		}
		else {

			m_host = NetworkTransport.AddHost(topology);
		}

		// set state bools
		m_isHost = isHosting;
		m_isSetup = true;

		DebugConsole.Log("Socket opened");
	}

	/// <summary>
	/// shuts down the networking capabilities of netutil
	/// </summary>
	public void Shutdown() {

		// shut down hosts
		NetworkTransport.RemoveHost(m_host);
	}

	/// <summary>
	/// attempts to connect to a similar peer
	/// </summary>
	/// <param name="t_ip">the address of the peer to connect to</param>
	public void Connect(string t_ip) {

		byte error = 0;
		m_hostId = NetworkTransport.Connect(m_host, t_ip, m_hostSocket, 0, out error);
		AddConnection(m_hostId, "host");
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
		m_connections[t_id] = connection;
	}

	/// <summary>
	/// checks for and handles messages recieved via udp host
	/// </summary>
	public void HandleMessages() {

		int hostId;
		int connectionId;
		int channelId;
		byte[] buffer = new byte[512];
		int size;
		byte error;
		NetworkEventType eventType;
		do {

			eventType = NetworkTransport.Receive(out hostId, out connectionId, out channelId, buffer, 512, out size, out error);
			if (eventType != NetworkEventType.Nothing) {
			
				DebugConsole.Log("message: " + eventType.ToString() + ", " + hostId + ", " + connectionId + ", " + channelId);
			}
			switch (eventType) {

				case NetworkEventType.DataEvent:
					HandleData(connectionId, buffer, size);
				break;
				case NetworkEventType.ConnectEvent:
					HandleConnect(connectionId);
				break;
				case NetworkEventType.DisconnectEvent:
				break;
				case NetworkEventType.BroadcastEvent:
				break;
			}
		}
		while (eventType != NetworkEventType.Nothing);
	}

	/// <summary>
	/// handles data network messages
	/// </summary>
	/// <param name="connectionId">connection id the message came from</param>
	/// <param name="buffer">the serialized buffer of the message</param>
	/// <param name="size">the size of the message</param>
	private void HandleData(int connectionId, byte[] buffer, int size) {

		switch (buffer[0]) {
			case NetUtilMessageType.CONNECTION_INFO:
			
				HandleConnectionInfo(connectionId, buffer, size);
			break;
			case NetUtilMessageType.TALK:

				HandleTalk(connectionId, buffer, size);
			break;
		}
	}

	/// <summary>
	/// handles connection messages
	/// </summary>
	/// <param name="connectionId">connection id the message came from</param>
	private void HandleConnect(int connectionId) {
		
		if (m_isHost) {

			if (!m_connections.ContainsKey(connectionId)) {
			
				AddConnection(connectionId, "");
			}
		}
		else {

			if (connectionId != m_hostId) {
			
				DebugConsole.Log("an unknown connection attempt has occured.");
			}
			else {

				SendMessage(NetUtilMessageType.CONNECTION_INFO, m_name);
			}
		}
	}

	/// <summary>
	/// handles incoming connection info messages
	/// </summary>
	private void HandleConnectionInfo(int connectionId, byte[] buffer, int size) {

		NetUtilConnection connection = new NetUtilConnection();
		connection.name = ConstructType<string>(buffer, 1, size - 1);
		connection.connectionId = connectionId;
		m_connections[connectionId] = connection;
	}

	/// <summary>
	/// handles incoming talk messages
	/// </summary>
	private void HandleTalk(int connectionId, byte[] buffer, int size) {

		string message = ConstructType<string>(buffer, 1, size - 1);
		if (m_connections.ContainsKey(connectionId)) {

			DebugConsole.Log("[" + m_connections[connectionId].name + "]: " + message);
		}
		else {

			DebugConsole.Log("[" + connectionId + "]: " + message);
		}
	}

	/// <summary>
	/// Serialize type and object to message
	/// </summary>
	/// <param name="t_type">type of message</param>
	/// <param name="t_object">object associated with message</param>
	/// <returns></returns>
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
	/// <returns></returns>
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
		NetworkTransport.Send(t_host, t_connection, t_channel, buffer, buffer.Length, out error);
	}

	/// <summary>
	/// helper function for sending messages which must arrive, and in order. (messages will always arrive)
	/// </summary>
	/// <param name="t_type">type of message to send</param>
	/// <param name="t_object">object to send with message</param>
	public void SendMessage(byte t_type, object t_object) {

		byte error;
		if (m_isHost) {

			// if is host send message to all connections
			foreach (int connectionId in m_connections.Keys) {

				SendMessage(m_host, connectionId, m_messageChannel, t_type, t_object, out error);
			}
		}
		else {

			// if is client send message to host
			SendMessage(m_host, m_hostId, m_messageChannel, t_type, t_object, out error);
		}
	}

	/// <summary>
	/// helper function for sending messages which may arrive, only in order. (messages may not arrive)
	/// </summary>
	/// <param name="t_type">type of message to send</param>
	/// <param name="t_object">object to send with message</param>
	public void SendSync(byte t_type, object t_object) {

		byte error;
		if (m_isHost) {

			// if is host send message to all connections
			foreach (int connectionId in m_connections.Keys) {

				SendMessage(m_host, connectionId, m_lowSyncChannel, t_type, t_object, out error);
			}
		}
		else {

			// if is client send message to host
			SendMessage(m_host, m_hostId, m_lowSyncChannel, t_type, t_object, out error);
		}
	}

	/// <summary>
	/// helper function for sending messages which will arrive, only in order. (out of date messages may not arrive)
	/// </summary>
	/// <param name="t_type">type of message to send</param>
	/// <param name="t_object">object to send with message</param>
	public void SendPrioritySync(byte t_type, object t_object) {

		byte error;
		if (m_isHost) {

			// if is host send message to all connections
			foreach (int connectionId in m_connections.Keys) {

				SendMessage(m_host, connectionId, m_highSyncChannel, t_type, t_object, out error);
			}
		}
		else {

			// if is client send message to host
			SendMessage(m_host, m_hostId, m_highSyncChannel, t_type, t_object, out error);
		}
	}

	/// <summary>
	/// sends a talk message
	/// </summary>
	/// <param name="t_text">the text message to send</param>
	public void Talk(string t_text) {

		SendMessage(NetUtilMessageType.TALK, t_text);
	}
}