using System;
using System.Linq;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;
using PiUtils.Util;
using TTIK.Ik;
using TTIK.Ik.FingerTracking;
using UnityEngine;

namespace TTIK.Network;


public class NetworkIkPlayer : NetworkBehaviour
{
	const ulong IK_STATE_MASK = 1 << 0;
	const ulong HEAD_TARGET_MASK = 1 << 1;
	const ulong LEFT_HAND_TARGET_MASK = 1 << 2;
	const ulong RIGHT_HAND_TARGET_MASK = 1 << 3;
	const ulong SCALE_MASK = 1 << 3;
	const ulong PLAYER_NET_ID_MASK = 1 << 4;
	const ulong LEFT_THUMB_CURL_MASK = 1 << 5;
	const ulong LEFT_INDEX_CURL_MASK = 1 << 6;
	const ulong LEFT_MIDDLE_CURL_MASK = 1 << 7;
	const ulong LEFT_RING_CURL_MASK = 1 << 8;
	const ulong LEFT_PINKY_CURL_MASK = 1 << 9;
	const ulong RIGHT_THUMB_CURL_MASK = 1 << 10;
	const ulong RIGHT_INDEX_CURL_MASK = 1 << 11;
	const ulong RIGHT_MIDDLE_CURL_MASK = 1 << 12;
	const ulong RIGHT_RING_CURL_MASK = 1 << 13;
	const ulong RIGHT_PINKY_CURL_MASK = 1 << 14;

	private static PluginLogger Logger = PluginLogger.GetLogger<NetworkIkPlayer>();
	public IkPlayer ikPlayer;
	public static NetworkIkPlayer localInstance;
	public static event Action<NetworkIkPlayer> OnLocalPlayerInitialized;
	public static event Action<IkPlayer> OnIkInitialized;

	private NetworkedThirdPerson playerDisplay;

	public TrackingType trackingType = TrackingType.ThreePt;

	#region SyncVar Declarations
	#region IkState
	private IkState _ikState = IkState.Disabled;
	public IkState NetworkIkState
	{
		get => _ikState;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._ikState))
			{
				return;
			}
			Logger.LogTrace($"Set ikState to {value} (old: {_ikState})");
			IkState oldIkState = this._ikState;
			SetSyncVar(value, ref _ikState, IK_STATE_MASK);

			if (getSyncVarHookGuard(IK_STATE_MASK))
			{
				return;
			}
			setSyncVarHookGuard(IK_STATE_MASK, true);
			try
			{
				OnIkStateChanged(oldIkState, value);
			}
			catch (Exception e)
			{
				Logger.LogError("Error invoking OnIkStateChanged event.");
			}
			finally
			{
				setSyncVarHookGuard(IK_STATE_MASK, false);
			}
		}
	}
	#endregion

	#region Targets
	private uint _headTargetNetId;
	public uint NetworkHeadTargetNetId
	{
		get => _headTargetNetId;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._headTargetNetId))
			{
				return;
			}
			SetSyncVar(value, ref _headTargetNetId, HEAD_TARGET_MASK);
		}
	}

	private uint _leftHandTargetNetId;
	public uint NetworkLeftHandTargetNetId
	{
		get => _leftHandTargetNetId;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._leftHandTargetNetId))
			{
				return;
			}
			SetSyncVar(value, ref _leftHandTargetNetId, LEFT_HAND_TARGET_MASK);
		}
	}

	private uint _rightHandTargetNetId;
	public uint NetworkRightHandTargetNetId
	{
		get => _rightHandTargetNetId;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._rightHandTargetNetId))
			{
				return;
			}
			SetSyncVar(value, ref _rightHandTargetNetId, RIGHT_HAND_TARGET_MASK);
		}
	}
	#endregion

	private float _scale = 1f;
	public float NetworkScale
	{
		get => _scale;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._scale))
			{
				return;
			}
			float oldScale = this._scale;
			SetSyncVar(value, ref _scale, SCALE_MASK);

			if (getSyncVarHookGuard(SCALE_MASK))
			{
				return;
			}
			setSyncVarHookGuard(SCALE_MASK, true);
			OnScaleChanged(oldScale, value);
			setSyncVarHookGuard(SCALE_MASK, false);
		}
	}

	private uint _playerNetId;
	public uint NetworkplayerNetId
	{
		get => _playerNetId;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._playerNetId))
			{
				return;
			}
			SetSyncVar(value, ref _playerNetId, PLAYER_NET_ID_MASK);
		}
	}

	#region Finger Curls
	#region Left Hand
	private float _leftThumbCurl;
	public float NetworkLeftThumbCurl
	{
		get => _leftThumbCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._leftThumbCurl))
			{
				return;
			}
			float oldLeftThumbCurl = this._leftThumbCurl;
			SetSyncVar(value, ref _leftThumbCurl, LEFT_THUMB_CURL_MASK);

			if (getSyncVarHookGuard(LEFT_THUMB_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(LEFT_THUMB_CURL_MASK, true);
			OnFingerCurlChange(HandType.Left, FingerType.Thumb, value);
			setSyncVarHookGuard(LEFT_THUMB_CURL_MASK, false);
		}
	}

	private float _leftIndexCurl;
	public float NetworkLeftIndexCurl
	{
		get => _leftIndexCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._leftIndexCurl))
			{
				return;
			}
			float oldLeftIndexCurl = this._leftIndexCurl;
			SetSyncVar(value, ref _leftIndexCurl, LEFT_INDEX_CURL_MASK);

			if (getSyncVarHookGuard(LEFT_INDEX_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(LEFT_INDEX_CURL_MASK, true);
			OnFingerCurlChange(HandType.Left, FingerType.Index, value);
			setSyncVarHookGuard(LEFT_INDEX_CURL_MASK, false);
		}
	}

	private float _leftMiddleCurl;
	public float NetworkLeftMiddleCurl
	{
		get => _leftMiddleCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._leftMiddleCurl))
			{
				return;
			}
			float oldLeftMiddleCurl = this._leftMiddleCurl;
			SetSyncVar(value, ref _leftMiddleCurl, LEFT_MIDDLE_CURL_MASK);

			if (getSyncVarHookGuard(LEFT_MIDDLE_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(LEFT_MIDDLE_CURL_MASK, true);
			OnFingerCurlChange(HandType.Left, FingerType.Middle, value);
			setSyncVarHookGuard(LEFT_MIDDLE_CURL_MASK, false);
		}
	}

	private float _leftRingCurl;
	public float NetworkLeftRingCurl
	{
		get => _leftRingCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._leftRingCurl))
			{
				return;
			}
			float oldLeftRingCurl = this._leftRingCurl;
			SetSyncVar(value, ref _leftRingCurl, LEFT_RING_CURL_MASK);

			if (getSyncVarHookGuard(LEFT_RING_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(LEFT_RING_CURL_MASK, true);
			OnFingerCurlChange(HandType.Left, FingerType.Ring, value);
			setSyncVarHookGuard(LEFT_RING_CURL_MASK, false);
		}
	}

	private float _leftPinkyCurl;
	public float NetworkLeftPinkyCurl
	{
		get => _leftPinkyCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._leftPinkyCurl))
			{
				return;
			}
			float oldLeftPinkyCurl = this._leftPinkyCurl;
			SetSyncVar(value, ref _leftPinkyCurl, LEFT_PINKY_CURL_MASK);

			if (getSyncVarHookGuard(LEFT_PINKY_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(LEFT_PINKY_CURL_MASK, true);
			OnFingerCurlChange(HandType.Left, FingerType.Pinky, value);
			setSyncVarHookGuard(LEFT_PINKY_CURL_MASK, false);
		}
	}
	#endregion

	#region Right Hand
	private float _rightThumbCurl;
	public float NetworkRightThumbCurl
	{
		get => _rightThumbCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._rightThumbCurl))
			{
				return;
			}
			float oldRightThumbCurl = this._rightThumbCurl;
			SetSyncVar(value, ref _rightThumbCurl, RIGHT_THUMB_CURL_MASK);

			if (getSyncVarHookGuard(RIGHT_THUMB_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(RIGHT_THUMB_CURL_MASK, true);
			OnFingerCurlChange(HandType.Right, FingerType.Thumb, value);
			setSyncVarHookGuard(RIGHT_THUMB_CURL_MASK, false);
		}
	}

	private float _rightIndexCurl;
	public float NetworkRightIndexCurl
	{
		get => _rightIndexCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._rightIndexCurl))
			{
				return;
			}
			float oldRightIndexCurl = this._rightIndexCurl;
			SetSyncVar(value, ref _rightIndexCurl, RIGHT_INDEX_CURL_MASK);

			if (getSyncVarHookGuard(RIGHT_INDEX_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(RIGHT_INDEX_CURL_MASK, true);
			OnFingerCurlChange(HandType.Right, FingerType.Index, value);
			setSyncVarHookGuard(RIGHT_INDEX_CURL_MASK, false);
		}
	}

	private float _rightMiddleCurl;
	public float NetworkRightMiddleCurl
	{
		get => _rightMiddleCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._rightMiddleCurl))
			{
				return;
			}
			float oldRightMiddleCurl = this._rightMiddleCurl;
			SetSyncVar(value, ref _rightMiddleCurl, RIGHT_MIDDLE_CURL_MASK);

			if (getSyncVarHookGuard(RIGHT_MIDDLE_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(RIGHT_MIDDLE_CURL_MASK, true);
			OnFingerCurlChange(HandType.Right, FingerType.Middle, value);
			setSyncVarHookGuard(RIGHT_MIDDLE_CURL_MASK, false);
		}
	}

	private float _rightRingCurl;
	public float NetworkRightRingCurl
	{
		get => _rightRingCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._rightRingCurl))
			{
				return;
			}
			float oldRightRingCurl = this._rightRingCurl;
			SetSyncVar(value, ref _rightRingCurl, RIGHT_RING_CURL_MASK);

			if (getSyncVarHookGuard(RIGHT_RING_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(RIGHT_RING_CURL_MASK, true);
			OnFingerCurlChange(HandType.Right, FingerType.Ring, value);
			setSyncVarHookGuard(RIGHT_RING_CURL_MASK, false);
		}
	}

	private float _rightPinkyCurl;
	public float NetworkRightPinkyCurl
	{
		get => _rightPinkyCurl;
		[param: In]
		set
		{
			if (SyncVarEqual(value, ref this._rightPinkyCurl))
			{
				return;
			}
			float oldRightPinkyCurl = this._rightPinkyCurl;
			SetSyncVar(value, ref _rightPinkyCurl, RIGHT_PINKY_CURL_MASK);

			if (getSyncVarHookGuard(RIGHT_PINKY_CURL_MASK))
			{
				return;
			}
			setSyncVarHookGuard(RIGHT_PINKY_CURL_MASK, true);
			OnFingerCurlChange(HandType.Right, FingerType.Pinky, value);
			setSyncVarHookGuard(RIGHT_PINKY_CURL_MASK, false);
		}
	}
	#endregion

	#endregion
	#endregion

	public enum TrackingType
	{
		ThreePt,
		[Obsolete("Six point tracking is not yet supported yet.")]
		SixPt,
		[Obsolete("Seven point tracking is not yet supported yet.")]
		SevenPt,
	}

	public enum IkState : uint
	{
		Disabled = 0,
		Initialized = 1,
		Calibrating = 2,
		Calibrated = 3,
	}

	public NetworkIkPlayer()
	{
		// Commands
		RemoteCallHelper.RegisterCommandDelegate(typeof(NetworkIkPlayer), nameof(CmdInitIkPlayer), new CmdDelegate(InvokeUserCode_CmdInitIkPlayer), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(NetworkIkPlayer), nameof(CmdStartCalibration), new CmdDelegate(InvokeUserCode_CmdStartCalibration), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(NetworkIkPlayer), nameof(CmdFinishCalibration), new CmdDelegate(InvokeUserCode_CmdFinishCalibration), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(NetworkIkPlayer), nameof(CmdSetScale), new CmdDelegate(InvokeUserCode_CmdSetScale), true);
		RemoteCallHelper.RegisterCommandDelegate(typeof(NetworkIkPlayer), nameof(CmdSetFingerCurl), new CmdDelegate(InvokeUserCode_CmdSetFingerCurl), true);

		// Client RPCs
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkIkPlayer), nameof(RpcInitIk), new CmdDelegate(InvokeUserCode_RpcInitIk));
	}

	#region Sync Methods
	protected override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		bool shouldSync = base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteULong(ulong.MaxValue);
			writer.WriteUInt((uint)_ikState);
			writer.WriteUInt(_headTargetNetId);
			writer.WriteUInt(_leftHandTargetNetId);
			writer.WriteUInt(_rightHandTargetNetId);
			writer.WriteFloat(_scale);
			writer.WriteUInt(NetworkplayerNetId);
			writer.WriteFloat(_leftThumbCurl);
			writer.WriteFloat(_leftIndexCurl);
			writer.WriteFloat(_leftMiddleCurl);
			writer.WriteFloat(_leftRingCurl);
			writer.WriteFloat(_leftPinkyCurl);
			writer.WriteFloat(_rightThumbCurl);
			writer.WriteFloat(_rightIndexCurl);
			writer.WriteFloat(_rightMiddleCurl);
			writer.WriteFloat(_rightRingCurl);
			writer.WriteFloat(_rightPinkyCurl);
			return true;
		}

		var dirtyBits = syncVarDirtyBits;
		writer.WriteULong(dirtyBits);
		if ((dirtyBits & IK_STATE_MASK) != 0L)
		{
			writer.WriteUInt((uint)_ikState);
			shouldSync = true;
		}
		if ((dirtyBits & HEAD_TARGET_MASK) != 0L)
		{
			writer.WriteUInt(_headTargetNetId);
			shouldSync = true;
		}
		if ((dirtyBits & LEFT_HAND_TARGET_MASK) != 0L)
		{
			writer.WriteUInt(_leftHandTargetNetId);
			shouldSync = true;
		}
		if ((dirtyBits & RIGHT_HAND_TARGET_MASK) != 0L)
		{
			writer.WriteUInt(_rightHandTargetNetId);
			shouldSync = true;
		}
		if ((dirtyBits & SCALE_MASK) != 0L)
		{
			writer.WriteFloat(_scale);
			shouldSync = true;
		}
		if ((dirtyBits & PLAYER_NET_ID_MASK) != 0L)
		{
			writer.WriteUInt(_playerNetId);
			shouldSync = true;
		}

		if ((dirtyBits & LEFT_THUMB_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_leftThumbCurl);
			shouldSync = true;
		}
		if ((dirtyBits & LEFT_INDEX_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_leftIndexCurl);
			shouldSync = true;
		}
		if ((dirtyBits & LEFT_MIDDLE_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_leftMiddleCurl);
			shouldSync = true;
		}
		if ((dirtyBits & LEFT_RING_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_leftRingCurl);
			shouldSync = true;
		}
		if ((dirtyBits & LEFT_PINKY_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_leftPinkyCurl);
			shouldSync = true;
		}
		if ((dirtyBits & RIGHT_THUMB_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_rightThumbCurl);
			shouldSync = true;
		}
		if ((dirtyBits & RIGHT_INDEX_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_rightIndexCurl);
			shouldSync = true;
		}
		if ((dirtyBits & RIGHT_MIDDLE_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_rightMiddleCurl);
			shouldSync = true;
		}
		if ((dirtyBits & RIGHT_RING_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_rightRingCurl);
			shouldSync = true;
		}
		if ((dirtyBits & RIGHT_PINKY_CURL_MASK) != 0L)
		{
			writer.WriteFloat(_rightPinkyCurl);
			shouldSync = true;
		}

		return shouldSync;
	}

	protected override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		var flag = reader.ReadULong();
		Logger.LogDebug($"Deserializing sync vars: {Convert.ToString((long)flag, 2)}");

		if ((flag & IK_STATE_MASK) != 0)
		{
			Logger.LogDebug("Deserializing ikState...");
			NetworkIkState = (IkState)reader.ReadUInt();
			Logger.LogDebug($"Deserialized ikState: {NetworkIkState}");
		}
		if ((flag & HEAD_TARGET_MASK) != 0)
		{
			Logger.LogDebug("Deserializing headTargetNetId...");
			NetworkHeadTargetNetId = reader.ReadUInt();
			Logger.LogDebug($"Deserialized headTargetNetId: {NetworkHeadTargetNetId}");
		}
		if ((flag & LEFT_HAND_TARGET_MASK) != 0)
		{
			Logger.LogDebug("Deserializing leftHandTargetNetId...");
			NetworkLeftHandTargetNetId = reader.ReadUInt();
			Logger.LogDebug($"Deserialized leftHandTargetNetId: {NetworkLeftHandTargetNetId}");
		}
		if ((flag & RIGHT_HAND_TARGET_MASK) != 0)
		{
			Logger.LogDebug("Deserializing rightHandTargetNetId...");
			NetworkRightHandTargetNetId = reader.ReadUInt();
			Logger.LogDebug($"Deserialized rightHandTargetNetId: {NetworkRightHandTargetNetId}");
		}
		if ((flag & SCALE_MASK) != 0)
		{
			Logger.LogDebug("Deserializing scale...");
			NetworkScale = reader.ReadFloat();
			Logger.LogDebug($"Deserialized scale: {NetworkScale}");
		}
		if ((flag & PLAYER_NET_ID_MASK) != 0)
		{
			Logger.LogDebug("Deserializing playerNetId...");
			NetworkplayerNetId = reader.ReadUInt();
			Logger.LogDebug($"Deserialized playerNetId: {NetworkplayerNetId}");
		}

		if ((flag & LEFT_THUMB_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing leftThumbCurl...");
			NetworkLeftThumbCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized leftThumbCurl: {NetworkLeftThumbCurl}");
		}
		if ((flag & LEFT_INDEX_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing leftIndexCurl...");
			NetworkLeftIndexCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized leftIndexCurl: {NetworkLeftIndexCurl}");
		}
		if ((flag & LEFT_MIDDLE_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing leftMiddleCurl...");
			NetworkLeftMiddleCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized leftMiddleCurl: {NetworkLeftMiddleCurl}");
		}
		if ((flag & LEFT_RING_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing leftRingCurl...");
			NetworkLeftRingCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized leftRingCurl: {NetworkLeftRingCurl}");
		}
		if ((flag & LEFT_PINKY_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing leftPinkyCurl...");
			NetworkLeftPinkyCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized leftPinkyCurl: {NetworkLeftPinkyCurl}");
		}
		if ((flag & RIGHT_THUMB_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing rightThumbCurl...");
			NetworkRightThumbCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized rightThumbCurl: {NetworkRightThumbCurl}");
		}
		if ((flag & RIGHT_INDEX_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing rightIndexCurl...");
			NetworkRightIndexCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized rightIndexCurl: {NetworkRightIndexCurl}");
		}
		if ((flag & RIGHT_MIDDLE_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing rightMiddleCurl...");
			NetworkRightMiddleCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized rightMiddleCurl: {NetworkRightMiddleCurl}");
		}
		if ((flag & RIGHT_RING_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing rightRingCurl...");
			NetworkRightRingCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized rightRingCurl: {NetworkRightRingCurl}");
		}
		if ((flag & RIGHT_PINKY_CURL_MASK) != 0)
		{
			Logger.LogTrace("Deserializing rightPinkyCurl...");
			NetworkRightPinkyCurl = reader.ReadFloat();
			Logger.LogTrace($"Deserialized rightPinkyCurl: {NetworkRightPinkyCurl}");
		}
	}

	#endregion

	#region Commands
	#region CmdInitIkPlayer
	public void CmdInitIkPlayer()
	{
		PooledNetworkWriter networkWriterPooled = NetworkWriterPool.GetWriter();
		SendCommandInternal(typeof(NetworkIkPlayer), nameof(CmdInitIkPlayer), networkWriterPooled, 0);
		NetworkWriterPool.Recycle(networkWriterPooled);
	}

	protected static void InvokeUserCode_CmdInitIkPlayer(
						NetworkBehaviour obj,
						NetworkReader reader,
						NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
			Logger.LogError("Command CmdInitIkPlayer called on server.");
		else
			((NetworkIkPlayer)obj).UserCode_CmdInitIkPlayer();
	}

	protected void UserCode_CmdInitIkPlayer()
	{
		Logger.LogDebug("[Command] Initializing IkPlayer...");

		if (trackingType == TrackingType.SixPt)
		{
			Logger.LogError("Six point tracking is not supported yet.");
			throw new System.NotImplementedException();
		}

		var identity = connectionToClient.identity.netId;
		var headTarget = spawnTarget($"{identity} head target");
		var leftHandTarget = spawnTarget($"{identity} left hand target");
		var rightHandTarget = spawnTarget($"{identity} right hand target");
		NetworkHeadTargetNetId = headTarget.GetComponent<NetworkIdentity>().netId;
		NetworkLeftHandTargetNetId = leftHandTarget.GetComponent<NetworkIdentity>().netId;
		NetworkRightHandTargetNetId = rightHandTarget.GetComponent<NetworkIdentity>().netId;
		NetworkIkState = IkState.Initialized;

		switch (trackingType)
		{
			case TrackingType.ThreePt:
				RpcInitIk();
				break;
			default:
				Logger.LogError($"Unknown / Unsupported tracking type {trackingType}.");
				throw new System.NotImplementedException();
		}
	}
	#endregion

	#region CmdStartCalibration
	public void CmdStartCalibration()
	{
		PooledNetworkWriter networkWriterPooled = NetworkWriterPool.GetWriter();
		SendCommandInternal(typeof(NetworkIkPlayer), nameof(CmdStartCalibration), networkWriterPooled, 0);
		NetworkWriterPool.Recycle(networkWriterPooled);
	}

	protected static void InvokeUserCode_CmdStartCalibration(
						NetworkBehaviour obj,
						NetworkReader reader,
						NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
			Logger.LogError("Command CmdStartCalibration called on server.");
		else
			((NetworkIkPlayer)obj).UserCode_CmdStartCalibration();
	}

	[Command]
	public void UserCode_CmdStartCalibration()
	{
		Logger.LogDebug("[Command] Starting calibration...");
		NetworkIkState = IkState.Calibrating;
	}
	#endregion

	#region CmdFinishCalibration
	public void CmdFinishCalibration(float scale)
	{
		PooledNetworkWriter networkWriterPooled = NetworkWriterPool.GetWriter();
		networkWriterPooled.WriteFloat(scale);
		SendCommandInternal(typeof(NetworkIkPlayer), nameof(CmdFinishCalibration), networkWriterPooled, 0);
		NetworkWriterPool.Recycle(networkWriterPooled);
	}

	protected static void InvokeUserCode_CmdFinishCalibration(
						NetworkBehaviour obj,
						NetworkReader reader,
						NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
			Logger.LogError("Command CmdFinishCalibration called on server.");
		else
			((NetworkIkPlayer)obj).UserCode_CmdFinishCalibration(reader.ReadFloat());
	}

	[Command]
	public void UserCode_CmdFinishCalibration(float scale)
	{
		Logger.LogDebug("[Command] Finishing calibration...");
		NetworkScale = scale;
		NetworkIkState = IkState.Calibrated;
	}
	#endregion

	#region CmdSetScale
	public void CmdSetScale(float scale)
	{
		PooledNetworkWriter networkWriterPooled = NetworkWriterPool.GetWriter();
		networkWriterPooled.WriteFloat(scale);
		SendCommandInternal(typeof(NetworkIkPlayer), nameof(CmdSetScale), networkWriterPooled, 0);
		NetworkWriterPool.Recycle(networkWriterPooled);
	}

	protected static void InvokeUserCode_CmdSetScale(
						NetworkBehaviour obj,
						NetworkReader reader,
						NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
			Logger.LogError("Command CmdSetScale called on server.");
		else
			((NetworkIkPlayer)obj).UserCode_CmdSetScale(reader.ReadFloat());
	}

	[Command]
	public void UserCode_CmdSetScale(float scale)
	{
		Logger.LogDebug($"[Command] Setting scale to {scale}...");
		NetworkScale = scale;
	}
	#endregion

	#region CmdSetFingerCurl
	public void CmdSetFingerCurl(HandType hand, FingerType finger, float value)
	{
		PooledNetworkWriter networkWriterPooled = NetworkWriterPool.GetWriter();
		networkWriterPooled.WriteUInt((uint)hand);
		networkWriterPooled.WriteUInt((uint)finger);
		networkWriterPooled.WriteFloat(value);
		SendCommandInternal(typeof(NetworkIkPlayer), nameof(CmdSetFingerCurl), networkWriterPooled, 0);
		NetworkWriterPool.Recycle(networkWriterPooled);
	}

	protected static void InvokeUserCode_CmdSetFingerCurl(
						NetworkBehaviour obj,
						NetworkReader reader,
						NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
			Logger.LogError("Command CmdSetFingerCurl called on server.");
		else
			((NetworkIkPlayer)obj).UserCode_CmdSetFingerCurl((HandType)reader.ReadUInt(), (FingerType)reader.ReadUInt(), reader.ReadFloat());
	}

	[Command]
	public void UserCode_CmdSetFingerCurl(HandType hand, FingerType finger, float value)
	{
		Logger.LogTrace($"[Command] Setting {hand} {finger} curl to {value}...");
		switch (hand)
		{
			case HandType.Left:
				switch (finger)
				{
					case FingerType.Thumb:
						NetworkLeftThumbCurl = value;
						break;
					case FingerType.Index:
						NetworkLeftIndexCurl = value;
						break;
					case FingerType.Middle:
						NetworkLeftMiddleCurl = value;
						break;
					case FingerType.Ring:
						NetworkLeftRingCurl = value;
						break;
					case FingerType.Pinky:
						NetworkLeftPinkyCurl = value;
						break;
					default:
						Logger.LogError($"Unknown / Unsupported finger type {finger}.");
						break;
				}
				break;
			case HandType.Right:
				switch (finger)
				{
					case FingerType.Thumb:
						NetworkRightThumbCurl = value;
						break;
					case FingerType.Index:
						NetworkRightIndexCurl = value;
						break;
					case FingerType.Middle:
						NetworkRightMiddleCurl = value;
						break;
					case FingerType.Ring:
						NetworkRightRingCurl = value;
						break;
					case FingerType.Pinky:
						NetworkRightPinkyCurl = value;
						break;
					default:
						Logger.LogError($"Unknown / Unsupported finger type {finger}.");
						break;
				}
				break;
			default:
				Logger.LogError($"Unknown / Unsupported hand type {hand}.");
				break;
		}
	}

	#endregion
	#endregion

	#region Client RPCs
	#region RpcInitIk
	public void RpcInitIk()
	{
		PooledNetworkWriter networkWriterPooled = NetworkWriterPool.GetWriter();
		SendRPCInternal(typeof(NetworkIkPlayer), nameof(RpcInitIk), networkWriterPooled, 0, true);
		NetworkWriterPool.Recycle(networkWriterPooled);
	}

	protected static void InvokeUserCode_RpcInitIk(
							NetworkBehaviour obj,
							NetworkReader reader,
							NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
			Logger.LogError("ClientRPC RpcInitIk called on server.");
		else
			((NetworkIkPlayer)obj).UserCode_RpcInitIk();
	}

	private void UserCode_RpcInitIk()
	{
		AsyncGameObject.DelayUntil(() =>
		{
			if (!hasAuthority)
			{
				return;
			}
			OnIkInitialized?.Invoke(ikPlayer);
			ikPlayer.OnCalibrating += CmdStartCalibration;
			ikPlayer.OnCalibrated += () => CmdFinishCalibration(ikPlayer.scale ?? 1f);
		}, () => GetIkPlayer() != null);
	}
	#endregion
	#endregion

	private void OnIkStateChanged(IkState oldState, IkState newState)
	{
		Logger.LogDebug($"OnIkStateChanged: {oldState} -> {newState}");
		if (hasAuthority)
		{
			return;
		}

		switch (NetworkIkState)
		{
			case IkState.Disabled:
				if (ikPlayer != null)
				{
					Destroy(ikPlayer);
				}
				break;
			case IkState.Initialized:
				break;
			case IkState.Calibrating:
				AsyncGameObject.DelayUntil(() =>
					{
						GetIkPlayer().calibrate();
					}, () => GetIkPlayer() != null);
				break;
			case IkState.Calibrated:
				AsyncGameObject.DelayUntil(() =>
				{
					var ik = GetIkPlayer();
					if (oldState != IkState.Calibrating)
					{
						ik.calibrate();
					}
					ik.calibrated(NetworkScale);
					SetPlayerDisplay(false);
				}, () => GetIkPlayer() != null);
				break;
			default:
				Logger.LogError("Unknown / Unsupported IkState.");
				break;
		}
	}

	public IkPlayer GetIkPlayer()
	{
		if (ikPlayer != null)
		{
			return ikPlayer;
		}

		if (NetworkHeadTargetNetId == 0 || NetworkLeftHandTargetNetId == 0 || NetworkRightHandTargetNetId == 0)
		{
			Logger.LogError("Tracker data is not set.");
			return null;
		}

		NetworkedPlayer player = GameObject.FindObjectsOfType<NetworkedPlayer>().Where(p => p.netId == NetworkplayerNetId).FirstOrDefault();

		if (player == null)
		{
			Logger.LogError($"Player {NetworkplayerNetId} not found in {GameState.instance.allPlayers.Keys.Aggregate((a, b) => $"{a}, {b}")}");
			return null;
		}
		this.playerDisplay = player.display;

		player = GameState.instance.allPlayers
				.Where(p => p.Key == NetworkplayerNetId.ToString())
				.Select(p => p.Value)
				.FirstOrDefault();

		var headNetObject = getGameObjectFromNetId((uint)NetworkHeadTargetNetId);
		if (headNetObject == null)
		{
			Logger.LogWarning("Head target not found.");
			return null;
		}
		var leftHandNetObject = getGameObjectFromNetId((uint)NetworkLeftHandTargetNetId);
		if (leftHandNetObject == null)
		{
			Logger.LogWarning("Left hand target not found.");
			return null;
		}
		var rightHandNetObject = getGameObjectFromNetId((uint)NetworkRightHandTargetNetId);
		if (rightHandNetObject == null)
		{
			Logger.LogWarning("Right hand target not found.");
			return null;
		}
		if (hasAuthority)
		{
			var headTarget = new GameObject("Head Target").AddComponent(new TrackingTarget(headNetObject));
			var leftHandTarget = new GameObject("Left Hand Target").AddComponent(new TrackingTarget(leftHandNetObject));
			var rightHandTarget = new GameObject("Right Hand Target").AddComponent(new TrackingTarget(rightHandNetObject));

			ikPlayer = gameObject.AddComponent(new IkPlayer(headTarget, leftHandTarget, rightHandTarget));
			ikPlayer.OnScaleChanged += Callbacks.Unique<float>(Callbacks.Debounce<float>(CmdSetScale, 0.5f));
		}
		else
		{
			ikPlayer = gameObject.AddComponent(new IkPlayer(headNetObject.transform, leftHandNetObject.transform, rightHandNetObject.transform));
		}

		Logger.LogDebug($"IkPlayer initialized");

		return ikPlayer;
	}


	private void OnScaleChanged(float oldValue, float newValue)
	{
		Logger.LogDebug($"OnScaleChanged: {NetworkScale}");
		if (hasAuthority)
		{
			return;
		}


		if (NetworkIkState == IkState.Calibrated)
		{
			AsyncGameObject.DelayUntil(() =>
			{
				GetIkPlayer().setScale(NetworkScale);
			}, () => GetIkPlayer()?.ik != null);
			return;
		}

		var ikPlayer = GetIkPlayer();
		if (ikPlayer?.ik == null)
		{
			Logger.LogError("Scale was changed without an IkPlayer. This should not happen.");
			return;
		}

		ikPlayer.setScale(NetworkScale);
	}

	private void OnFingerCurlChange(HandType hand, FingerType finger, float newValue)
	{
		Logger.LogTrace($"OnFingerCurlChange: {hand} {finger} {newValue}");
		if (hasAuthority)
		{
			return;
		}

		var ikPlayer = GetIkPlayer();
		if (ikPlayer == null)
		{
			Logger.LogError("Finger curl was changed without an IkPlayer. This should not happen.");
			return;
		}

		ikPlayer.UpdateFingerCurl(hand, finger, newValue);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();

		Logger.LogDebug($"OnStartServer {connectionToClient.identity.netId}");
		NetworkplayerNetId = connectionToClient.identity.netId;
	}


	public override void OnStartAuthority()
	{
		base.OnStartAuthority();

		Logger.LogDebug("OnStartAuthority");
		localInstance = this;

		OnLocalPlayerInitialized?.Invoke(this);
	}

	public void UpdateFingerCurl(HandType hand, FingerType finger, float curl)
	{
		if (Mathf.Abs(GetFingerCurl(hand, finger) - curl) > ModConfig.fingerSyncDeadzone.Value)
		{
			CmdSetFingerCurl(hand, finger, curl);
		}

		if (hasAuthority)
		{
			GetIkPlayer()?.UpdateFingerCurl(hand, finger, curl);
		}
	}

	private float GetFingerCurl(HandType hand, FingerType finger)
	{
		switch (finger)
		{
			case FingerType.Thumb:
				switch (hand)
				{
					case HandType.Left:
						return NetworkLeftThumbCurl;
					case HandType.Right:
						return NetworkRightThumbCurl;
				}
				break;
			case FingerType.Index:
				switch (hand)
				{
					case HandType.Left:
						return NetworkLeftIndexCurl;
					case HandType.Right:
						return NetworkRightIndexCurl;
				}
				break;
			case FingerType.Middle:
				switch (hand)
				{
					case HandType.Left:
						return NetworkLeftMiddleCurl;
					case HandType.Right:
						return NetworkRightMiddleCurl;
				}
				break;
			case FingerType.Ring:
				switch (hand)
				{
					case HandType.Left:
						return NetworkLeftRingCurl;
					case HandType.Right:
						return NetworkRightRingCurl;
				}
				break;
			case FingerType.Pinky:
				switch (hand)
				{
					case HandType.Left:
						return NetworkLeftPinkyCurl;
					case HandType.Right:
						return NetworkRightPinkyCurl;
				}
				break;
		}
		return 0f;
	}

	public void Update()
	{
		if (!hasAuthority)
		{
			return;
		}

		transform.position = Player._instance.transform.position + new Vector3(0, 0, -0.15f);
	}

	private GameObject spawnTarget(string name)
	{
		Logger.LogDebug($"Spawning target {name}...");

		var target = Instantiate(TTIK.trackingTargetPrefab);
		target.name = name;

		NetworkServer.Spawn(target, connectionToClient);

		return target;
	}

	private GameObject getGameObjectFromNetId(uint netId)
	{
		return FindObjectsOfType<NetworkIdentity>().Where(ni => ni.netId == netId).FirstOrDefault()?.gameObject;
	}

	private void SetPlayerDisplay(bool enabled)
	{
		GameObjectFinder.FindChildObjectByName("Ground Breaker", this.playerDisplay.gameObject).SetActive(enabled);
	}
}
