using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// a serializable vector 2 container
/// </summary>
[Serializable]
public struct NetUtilVector2 {

	public NetUtilVector2(Vector2 t_vector) {

		x = t_vector.x;
		y = t_vector.y;
	}

	public Vector2 toVector2() {

		return new Vector2(x, y);
	}

	public float x;
	public float y;
}

/// <summary>
/// a serializable vector 3 container
/// </summary>
[Serializable]
public struct NetUtilVector3 {

	public NetUtilVector3(Vector3 t_vector) {

		x = t_vector.x;
		y = t_vector.y;
		z = t_vector.z;
	}

	public Vector3 toVector3() {

		return new Vector3(x, y, z);
	}

	public float x;
	public float y;
	public float z;
}

/// <summary>
/// a serializable vector 4 container
/// </summary>
[Serializable]
public struct NetUtilVector4 {

	public NetUtilVector4(Vector4 t_vector) {

		x = t_vector.x;
		y = t_vector.y;
		z = t_vector.z;
		w = t_vector.w;
	}

	public Vector4 toVector4() {

		return new Vector4(x, y, z, w);
	}

	public float x;
	public float y;
	public float z;
	public float w;
}

/// <summary>
/// a serializable quaternion container
/// </summary>
[Serializable]
public struct NetUtilQuaternion {

	public NetUtilQuaternion(Quaternion t_vector) {

		x = t_vector.x;
		y = t_vector.y;
		z = t_vector.z;
		w = t_vector.w;
	}

	public Quaternion toQuaternion() {

		return new Quaternion(x, y, z, w);
	}

	public float x;
	public float y;
	public float z;
	public float w;
}