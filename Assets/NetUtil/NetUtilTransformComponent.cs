using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A NetUtil transform communication component
/// </summary>
public class NetUtilTransformComponent : MonoBehaviour {

	/// <summary>
	/// the number of messages per second to send over network
	/// </summary>
	public float updateFrequency = 12.0f;
	/// <summary>
	/// if true position is predicted
	/// </summary>
	public bool predictPosition = true;
	/// <summary>
	/// if true rotation is predicted
	/// </summary>
	public bool predictRotation = true;
	/// <summary>
	/// if true scale is predicted
	/// </summary>
	public bool predictScale = true;

	private float m_oldTime = 0.0f;
	private Vector3 m_oldPosition;
	private Quaternion m_oldRotation;
	private Vector3 m_oldScale;

	private float m_currentTime = 0.0f;
	private Vector3 m_samplePosition;
	private Quaternion m_sampleRotation;
	private Vector3 m_sampleScale;

	private float m_newTime = 0.0f;
	private Vector3 m_newPosition;
	private Quaternion m_newRotation;
	private Vector3 m_newScale;

	/// <summary>
	/// finds the position given prediction rules
	/// </summary>
	private Vector3 findPosition() {

		if (!predictPosition) {

			return m_newPosition;
		}

		if (m_oldTime > 0.0f) {

			float delta = m_newTime - m_oldTime;
			float alpha = (m_currentTime - m_oldTime) / delta;

			return Vector3.Lerp(
				Vector3.Lerp(m_oldPosition, m_samplePosition, alpha),
				Vector3.Lerp(m_samplePosition, m_newPosition, alpha),
				alpha);
		}

		return Vector3.Lerp(m_oldPosition, m_newPosition, Time.deltaTime);
	}

	/// <summary>
	/// finds the rotation given prediction rules
	/// </summary>
	private Quaternion findRotation() {

		if (!predictRotation) {

			return m_newRotation;
		}

		if (m_oldTime > 0.0f) {

			float delta = m_newTime - m_oldTime;
			float alpha = (m_currentTime - m_oldTime) / delta;

			return Quaternion.Slerp(
				Quaternion.Slerp(m_oldRotation, m_sampleRotation, alpha),
				Quaternion.Slerp(m_sampleRotation, m_newRotation, alpha),
				alpha);
		}

		return Quaternion.Slerp(m_oldRotation, m_newRotation, Time.deltaTime);
	}

	/// <summary>
	/// finds the scale given prediction rules
	/// </summary>
	private Vector3 findScale() {

		if (!predictScale) {

			return m_newScale;
		}

		if (m_oldTime > 0.0f) {

			float delta = m_newTime - m_oldTime;
			float alpha = (m_currentTime - m_oldTime) / delta;

			return Vector3.Lerp(
				Vector3.Lerp(m_oldScale, m_sampleScale, alpha),
				Vector3.Lerp(m_sampleScale, m_newScale, alpha),
				alpha);
		}

		return Vector3.Lerp(m_oldScale, m_newScale, Time.deltaTime);
	}

	/// <summary>
	/// sets up the component
	/// </summary>
	private void Start() {

		m_newPosition = m_oldPosition = transform.position;
		m_newRotation = m_oldRotation = transform.rotation;
		m_newScale = m_oldScale = transform.localScale;
	}
	
	/// <summary>
	/// updates the component
	/// </summary>
	void Update() {

		if (NetUtilManager.OwnsObject(gameObject.name)) {

			if (Time.time - m_currentTime > 1.0f / updateFrequency) {

				m_currentTime = Time.time;
				NetUtilManager.SyncTransform(gameObject.name, transform);
			}
		}
		else {

			m_currentTime += Time.deltaTime;

			if (predictPosition) {

				transform.position = findPosition();
			}
			if (predictRotation) {

				transform.rotation = findRotation();
			}
			if (predictScale) {

				transform.localScale = findScale();
			}
		}
	}

	/// <summary>
	/// a netutil event for transform updating
	/// </summary>
	void NetUtilSyncTransform(object t_object) {

		if (NetUtilManager.OwnsObject(gameObject.name)) {
		
			return;
		}

		NetUtilSyncTransform netTransform = (NetUtilSyncTransform)t_object;

		if (netTransform.time < m_newTime) {

			return;
		}

		m_oldTime = m_newTime;
		m_oldPosition = m_newPosition;
		m_oldRotation = m_newRotation;
		m_oldScale = m_newScale;
		
		m_samplePosition = transform.position;
		m_sampleRotation = transform.rotation;
		m_sampleScale = transform.localScale;

		m_newTime = netTransform.time;
		m_newPosition = netTransform.position.toVector3();
		m_newRotation = netTransform.rotation.toQuaternion();
		m_newScale = netTransform.localScale.toVector3();
		
		m_currentTime = m_oldTime;
	}

}
