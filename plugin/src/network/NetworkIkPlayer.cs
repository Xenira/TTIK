using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;
using PiUtils.Util;
using TTIK.Ik;
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

			if (!NetworkServer.localClientActive || getSyncVarHookGuard(IK_STATE_MASK))
			{
				return;
			}
			setSyncVarHookGuard(IK_STATE_MASK, true);
			try
			{
				OnIkStateChanged(oldIkState, value);
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

			if (!NetworkServer.localClientActive || getSyncVarHookGuard(SCALE_MASK))
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

		// Client RPCs
		RemoteCallHelper.RegisterRpcDelegate(typeof(NetworkIkPlayer), nameof(RpcInitIk), new CmdDelegate(InvokeUserCode_RpcInitIk));
	}

	#region Sync Methods

	protected override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		bool flag = base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteULong(ulong.MaxValue);
			writer.WriteUInt((uint)_ikState);
			writer.WriteUInt(_headTargetNetId);
			writer.WriteUInt(_leftHandTargetNetId);
			writer.WriteUInt(_rightHandTargetNetId);
			writer.WriteFloat(_scale);
			writer.WriteUInt(NetworkplayerNetId);
			return true;
		}

		writer.WriteULong(syncVarDirtyBits);
		if ((syncVarDirtyBits & IK_STATE_MASK) != 0L)
		{
			writer.WriteUInt((uint)_ikState);
			flag = true;
		}
		if ((syncVarDirtyBits & HEAD_TARGET_MASK) != 0L)
		{
			writer.WriteUInt(_headTargetNetId);
			flag = true;
		}
		if ((syncVarDirtyBits & LEFT_HAND_TARGET_MASK) != 0L)
		{
			writer.WriteUInt(_leftHandTargetNetId);
			flag = true;
		}
		if ((syncVarDirtyBits & RIGHT_HAND_TARGET_MASK) != 0L)
		{
			writer.WriteUInt(_rightHandTargetNetId);
			flag = true;
		}
		if ((syncVarDirtyBits & SCALE_MASK) != 0L)
		{
			writer.WriteFloat(_scale);
			flag = true;
		}
		if ((syncVarDirtyBits & PLAYER_NET_ID_MASK) != 0L)
		{
			writer.WriteUInt(_playerNetId);
			flag = true;
		}

		return flag;
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

		IkPlayer ik;
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
				ik = GetIkPlayer();
				ik.calibrate();
				break;
			case IkState.Calibrated:
				ik = GetIkPlayer();
				if (oldState != IkState.Calibrating)
				{
					ik.calibrate();
				}
				ik.calibrated(NetworkScale);
				SetPlayerDisplay(false);
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

		var ikPlayer = GetIkPlayer();
		if (ikPlayer == null)
		{
			Logger.LogError("Scale was changed without an IkPlayer. This should not happen.");
			return;
		}

		ikPlayer.setScale(NetworkScale);
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
