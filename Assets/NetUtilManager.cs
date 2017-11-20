using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

public static class NetUtilManager {

	private static NetUtil m_singleton;

	public static bool isInitialized {
		
		get {

			return m_singleton != null;
		}
	}

	public static bool isConnected {
		
		get {

			return m_singleton.isSetup;
		}
	}

	public static void Setup() {

		m_singleton = new NetUtil(7108, 7109);
	}

	public static void Host() {

		m_singleton.Setup(true);
	}

	public static void Connect(string t_ip) {

		m_singleton.Setup(false);
		m_singleton.Connect(t_ip);
	}

	public static void Shutdown() {

		m_singleton = null;
	}

	public static void Talk(string t_text) {

		m_singleton.Talk(t_text);
	}

	public static void Update() {

		m_singleton.HandleMessages();
	}
}
