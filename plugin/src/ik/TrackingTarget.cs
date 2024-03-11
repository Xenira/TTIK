using System;
using Mirror;
using PiUtils.Debug;
using PiUtils.Util;
using UnityEngine;

namespace TTIK.Ik;

public class TrackingTarget : MonoBehaviour
{
	private static PluginLogger Logger = PluginLogger.GetLogger<TrackingTarget>();
	public NetworkTransform target;
	private Transform trackingTarget;
	public Vector3 positionOffset;
	public Quaternion rotationOffset;
	public bool calibrating;


	public TrackingTarget(NetworkTransform target)
	{
		if (target == null)
		{
			throw new ArgumentException("Target does not have a NetworkTransform component.");
		}

		this.target = target;
	}

	public TrackingTarget(GameObject target) : this(target.GetComponent<NetworkTransform>())
	{
	}

	public void StartCalibration(Transform trackingTarget, Transform anchor)
	{
		calibrating = true;
		this.trackingTarget = trackingTarget;
		transform.SetParent(anchor, false);
		positionOffset = Vector3.zero;
		rotationOffset = Quaternion.identity;
	}

	public void FinishCalibration()
	{
		calibrating = false;
		// rotationOffset = Quaternion.Inverse(transform.rotation) * target.transform.rotation;
		// Logger.LogDebug($"rotation offset {rotationOffset}");
		// positionOffset = transform.position - target.transform.position;
	}

	private void Start()
	{
		gameObject.AddComponent<Gizmo>();
		target.gameObject.AddComponent<Gizmo>();
		var debugLine = gameObject.AddComponent<DebugLine>();
		debugLine.start = target.transform;
		debugLine.end = transform;
	}

	private void Update()
	{
		if (calibrating)
		{
			UpdateCalibrationTransform();
		}
		else
		{
			UpdateTransform();
		}
	}

	private void UpdateTransform()
	{
		if (target != null)
		{
			target.transform.rotation = transform.rotation * rotationOffset;
			target.transform.position = transform.position + (transform.rotation * positionOffset);
			// target.transform.SetPositionAndRotation(transform.position + positionOffset, transform.rotation * rotationOffset);
		}
	}

	private void UpdateCalibrationTransform()
	{
		if (trackingTarget != null)
		{
			target.transform.position = trackingTarget.position;
			target.transform.rotation = trackingTarget.rotation;
		}
	}
}
