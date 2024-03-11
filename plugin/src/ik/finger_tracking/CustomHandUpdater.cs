using System.Collections.Generic;

namespace TTIK.Ik.FingerTracking;

class CustomHandUpdater
{
	public static FingerType[] ALL_FINGER_TYPES = [FingerType.Thumb, FingerType.Index, FingerType.Middle, FingerType.Ring, FingerType.Pinky];

	public Dictionary<FingerType, Finger> fingers = new Dictionary<FingerType, Finger>();

	public void OnBoneTransformsUpdated(FingerType fingerType, float value)
	{
		if (fingers.TryGetValue(fingerType, out var finger))
		{
			finger.CurlFinger(value);
		}
	}
}

// Not currently used but kept for reference or future change from curl to actual finger movement
// https://github.com/ValveSoftware/openvr/wiki/Hand-Skeleton
enum Bone
{
	Root = 0,
	Wrist,

	Thumb0,
	Thumb1,
	Thumb2,
	Thumb3,

	IndexFinger0,
	IndexFinger1,
	IndexFinger2,
	IndexFinger3,
	IndexFinger4,

	MiddleFinger0,
	MiddleFinger1,
	MiddleFinger2,
	MiddleFinger3,
	MiddleFinger4,

	RingFinger0,
	RingFinger1,
	RingFinger2,
	RingFinger3,
	RingFinger4,

	PinkyFinger0,
	PinkyFinger1,
	PinkyFinger2,
	PinkyFinger3,
	PinkyFinger4,

	Aux_Thumb,
	Aux_IndexFinger,
	Aux_MiddleFinger,
	Aux_RingFinger,
	Aux_PinkyFinger,
	Count
}
