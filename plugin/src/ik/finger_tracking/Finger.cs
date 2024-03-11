using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TTIK.Ik.FingerTracking;

public class Finger
{
	private Vector3 axis = Vector3.back;
	private float totalCurl = 180;
	private Dictionary<Transform, float> bones = new Dictionary<Transform, float>();
	private Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();

	public Finger(Vector3? axis = null, float totalCurl = 180)
	{
		this.axis = axis ?? this.axis;
		this.totalCurl = totalCurl;
	}

	public Finger(Dictionary<Transform, float> bones, Vector3? axis = null, float totalCurl = 180) : this(axis, totalCurl)
	{
		this.bones = bones;
	}

	public Finger WithBone(Transform transform)
	{
		AddBone(transform);
		return this;
	}

	public Finger WithBone(Transform transform, float factor)
	{
		AddBone(transform, factor);
		return this;
	}

	public void AddBone(Transform transform)
	{
		AddBone(transform, 1);
	}

	public void AddBone(Transform transform, float factor)
	{
		bones.Add(transform, factor);
		originalRotations.Add(transform, transform.localRotation);
	}

	public void CurlFinger(float factor)
	{
		var relativeFactor = factor / bones.Values.Sum();
		foreach (var (bone, boneFactor) in bones)
		{
			var originalRotation = originalRotations[bone];
			var combinedFactor = boneFactor * relativeFactor;
			// Rotate around forward axis
			bone.localRotation = originalRotation * Quaternion.AngleAxis(totalCurl * combinedFactor, axis);
		}
	}
}
