using System;
using System.Collections.Generic;
using FIMSpace;
using FIMSpace.BonesStimulation;
using PiUtils.Debug;
using PiUtils.Util;
using RootMotion.FinalIK;
using UnityEngine;

namespace TTIK.Ik;

public class IkPlayer : MonoBehaviour
{
	private static PluginLogger Logger = PluginLogger.GetLogger<IkPlayer>();

	public static IkPlayer instance;

	public GameObject avatar;
	public VRIK ik;

	public float? scale { get; private set; } = null;

	public event Action<float> OnScaleChanged;
	public event Action OnCalibrating;
	public event Action OnCalibrated;

	private float avatarHeight;
	private Dictionary<TrackingTargetType, TrackingTarget> trackingTargets = new Dictionary<TrackingTargetType, TrackingTarget>();

	private Transform head;
	public Transform headTarget;
	public Transform headTargetParent;
	private Transform leftHand;
	public Transform leftHandTarget;
	private Transform rightHand;
	public Transform rightHandTarget;

	private static VRIK.References references;
	public bool? calibrating = null;

	public enum TrackingTargetType
	{
		Head,
		LeftHand,
		RightHand
	}

	public IkPlayer(Transform headTarget, Transform leftHandTarget, Transform rightHandTarget)
	{
		this.headTarget = headTarget;
		this.leftHandTarget = leftHandTarget;
		this.rightHandTarget = rightHandTarget;
	}

	public IkPlayer(TrackingTarget headTarget, TrackingTarget leftHandTarget, TrackingTarget rightHandTarget)
	{
		trackingTargets.Add(TrackingTargetType.Head, headTarget);
		this.headTarget = headTarget.transform;

		trackingTargets.Add(TrackingTargetType.LeftHand, leftHandTarget);
		this.leftHandTarget = leftHandTarget.transform;

		trackingTargets.Add(TrackingTargetType.RightHand, rightHandTarget);
		this.rightHandTarget = rightHandTarget.transform;
	}

	private void Start()
	{
		if (instance != null)
		{
			Logger.LogWarning("IkPlayer already exists, destroying it...");
			Destroy(instance.gameObject);
		}

		instance = this;
	}

	private void init()
	{
		Logger.LogDebug("Initializing ik player...");
		if (avatar != null)
		{
			Logger.LogWarning("Avatar already exists, destroying it");
			Destroy(avatar);
		}

		avatar = Instantiate(GameObjectFinder.FindChildObjectByName("Ground Breaker", GameObjectFinder.FindObjectByName("ThirdPersonDisplay")));
		avatar.GetComponent<Animator>().enabled = false;
		avatar.GetComponent<LeaningAnimator>().enabled = false;
		avatar.GetComponentInChildren<BonesStimulator>().enabled = false;
		Logger.LogDebug($"Avatar instantiated {avatar.transform}, parent {transform}");
		avatar.transform.parent = transform;
		avatar.transform.localPosition = Vector3.zero;
		avatar.transform.localRotation = Quaternion.identity;

		head = GameObjectFinder.FindChildObjectByName("head", avatar).transform;
		leftHand = GameObjectFinder.FindChildObjectByName("hand_l", avatar).transform;
		rightHand = GameObjectFinder.FindChildObjectByName("hand_r", avatar).transform;

		setReferences();
		avatarHeight = references.head.position.y - references.root.position.y;
		Logger.LogDebug($"Avatar height: {avatarHeight}");

		ik = avatar.AddComponent<VRIK>();
		ik.solver.spine.headTarget = headTarget;
		ik.solver.leftArm.target = leftHandTarget;
		ik.solver.rightArm.target = rightHandTarget;
		ik.references = references;
		ik.solver.plantFeet = false;
		ik.solver.locomotion.maxVelocity *= 4;
		ik.solver.locomotion.rootSpeed *= 4;
		ik.solver.locomotion.stepThreshold /= 2;
		ik.solver.locomotion.footDistance /= 1.25f;
		// ik.solver.locomotion.stepSpeed *= 2;
		ik.enabled = false;

		Logger.LogDebug($"Ik locomotion weight: {ik.solver.locomotion.weight}");
		Logger.LogDebug($"Ik locomotion max velocity: {ik.solver.locomotion.maxVelocity}");
		Logger.LogDebug($"Ik locomotion root speed: {ik.solver.locomotion.rootSpeed}");
		Logger.LogDebug($"Ik locomotion step speed: {ik.solver.locomotion.stepSpeed}");
		Logger.LogDebug($"Ik locomotion step threshold: {ik.solver.locomotion.stepThreshold}");

		Logger.LogDebug($"Ik left arm weight: {ik.solver.leftArm.positionWeight}");
		Logger.LogDebug($"Ik right arm weight: {ik.solver.rightArm.positionWeight}");

		gameObject.AddComponent<Gizmo>();
		headTarget.gameObject.AddComponent<Gizmo>();
		leftHandTarget.gameObject.AddComponent<Gizmo>();
		rightHandTarget.gameObject.AddComponent<Gizmo>();
	}

	private void setReferences()
	{
		references = new VRIK.References();
		references.pelvis = GameObjectFinder.FindChildObjectByName("pelvis", avatar).transform;
		references.root = references.pelvis.parent.parent;

		references.spine = GameObjectFinder.FindChildObjectByName("spine_01", references.pelvis).transform;
		references.chest = GameObjectFinder.FindChildObjectByName("spine_02", references.spine).transform;
		references.neck = GameObjectFinder.FindChildObjectByName("neck_01", references.chest).transform;
		references.head = head;

		references.leftShoulder = GameObjectFinder.FindChildObjectByName("clavicle_l", references.chest).transform;
		references.leftUpperArm = GameObjectFinder.FindChildObjectByName("upperarm_l", references.leftShoulder).transform;
		references.leftForearm = GameObjectFinder.FindChildObjectByName("lowerarm_l", references.leftUpperArm).transform;
		references.leftHand = leftHand;

		references.rightShoulder = GameObjectFinder.FindChildObjectByName("clavicle_r", references.chest).transform;
		references.rightUpperArm = GameObjectFinder.FindChildObjectByName("upperarm_r", references.rightShoulder).transform;
		references.rightForearm = GameObjectFinder.FindChildObjectByName("lowerarm_r", references.rightUpperArm).transform;
		references.rightHand = rightHand;

		references.leftThigh = GameObjectFinder.FindChildObjectByName("thigh_l", references.pelvis).transform;
		references.leftCalf = GameObjectFinder.FindChildObjectByName("calf_l", references.leftThigh).transform;
		references.leftFoot = GameObjectFinder.FindChildObjectByName("foot_l", references.leftCalf).transform;

		references.rightThigh = GameObjectFinder.FindChildObjectByName("thigh_r", references.pelvis).transform;
		references.rightCalf = GameObjectFinder.FindChildObjectByName("calf_r", references.rightThigh).transform;
		references.rightFoot = GameObjectFinder.FindChildObjectByName("foot_r", references.rightCalf).transform;
	}

	private void OnDestroy()
	{
		Logger.LogInfo("Destroying ik player...");
	}

	private void OnEnable()
	{
		Logger.LogInfo("Enabling ik player...");
	}

	private void OnDisable()
	{
		Logger.LogInfo("Disabling ik player...");
	}

	private void Update()
	{
		if (calibrating == true)
		{
			setScale(getScale(headTargetParent.position));
			return;
		}
	}

	private float getScale(Vector3 headPosition)
	{
		return (headPosition.y - ik.references.root.position.y) / avatarHeight + 0.1f;
	}

	public void setScale(float scale)
	{
		this.scale = scale;
		OnScaleChanged?.Invoke(scale);
		Logger.LogDebug($"Setting object scale to {Vector3.one * scale} ({scale})");
		ik.references.root.localScale = Vector3.one * scale;
	}

	public void calibrate(Transform headTargetParent, Transform leftHandTargetParent, Transform rightHandTargetParent)
	{
		calibrate();

		this.headTargetParent = headTargetParent;

		trackingTargets[TrackingTargetType.Head].StartCalibration(head, headTargetParent);
		trackingTargets[TrackingTargetType.LeftHand].StartCalibration(leftHand, leftHandTargetParent);
		trackingTargets[TrackingTargetType.RightHand].StartCalibration(rightHand, rightHandTargetParent);

		calibrating = true;
	}

	internal void calibrate()
	{
		init();

		OnCalibrating?.Invoke();
	}

	public void calibrated(float? scale)
	{
		trackingTargets.Values.ForEach(t => t.FinishCalibration());
		var settings = new VRIKCalibrator.Settings();
		setScale(this.scale ?? scale ?? 1f);
		calibrating = false;
		ik.enabled = true;
		OnCalibrated?.Invoke();
	}

	private Transform TrackingTargetOrDefault(TrackingTargetType type, Transform defaultTransform)
	{
		return trackingTargets.GetValueOrDefault(type, null)?.transform ?? defaultTransform;
	}
}
