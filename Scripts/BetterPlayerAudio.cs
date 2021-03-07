using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Scripts
{
    [DefaultExecutionOrder(10010)]
    public class BetterPlayerAudio : UdonSharpBehaviour
    {
        #region Constants

        private const int EnvironmentLayerMask = 1 << 11;
        private const int UILayerMask = 1 << 5;

        #endregion

        [Header("General Settings")] [SerializeField]
        private UdonBehaviour uiController;

        /// <summary>
        /// How many player updates should be performed every second (framerate independent). Example: with 60 players
        /// in the world and playerUpdateRate = 20 it will take 60/20 = 3 seconds until every player got updated.
        /// If set to -1 it will update ALL PLAYERS EVERY FRAME (don't use this option in worlds with up to
        /// 80 people as this can potentially have a serious performance impact!)
        /// </summary>
        [Tooltip(
            "How many player updates should be performed every second (framerate independent). Example: with 60 players in the world and playerUpdateRate = 20 it will take 60/20 = 3 seconds until every player got updated.")]
        [SerializeField]
        protected int playerUpdateRate = 60;

        /// <summary>
        /// The name of the function in the UI controller script that should be called when the master control
        /// is enabled and the master changed any value
        /// </summary>
        [Tooltip(
            "The name of the function in the UI controller script that should be called when the master control is enabled and the master changed any value")]
        [SerializeField]
        private string updateUiEventName = "UpdateUi";

        /// <summary>
        /// Layers which can reduce voice and avatar sound effects when they are in between the local player (listener)
        /// and the player/avatar producing the sound
        /// Default layers: 11 and 5 (Environment and UI which includes the capsule colliders of other players)
        /// </summary>
        [Tooltip(
            "Objects on these layers reduce the voice/avatar sound volume when they are in-between the local player and the player/avatar that produces the sound")]
        public LayerMask occlusionMask = EnvironmentLayerMask | UILayerMask;

        #region default values for resetting

        /// <summary>
        /// When enabled the master can change the settings of all players
        /// </summary>
        [Tooltip("When enabled the master can change the settings of all players")]
        public bool defaultAllowMasterControl;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the
        /// voice/player to the current distance and make him/her/them in-audible
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float defaultOcclusionFactor = 0.7f;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// Occlusion when a player is occluded by another player.
        /// A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the
        /// voice/player to the current distance and make him/her/them in-audible
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Occlusion when a player is occluded by another player. A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float defaultPlayerOcclusionFactor = 0.85f;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 1.0 reduces the ranges by up to 100% when the listener is facing away from a voice/avatar
        /// and thus making them more quiet.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when the listener is facing away from a voice/avatar and thus making them more quiet.")]
        public float defaultListenerDirectionality = 0.5f;

        /// <summary>
        /// Range 0.0 to 1.0.
        /// A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is
        /// facing away from the listener.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is facing away from the listener.")]
        public float defaultPlayerDirectionality = 0.3f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-disable-lowpass</remarks>
        /// </summary>
        [Header("Voice Settings")] public bool defaultEnableVoiceLowpass = true;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-distance-near</remarks>
        /// </summary>
        [Range(0, 1000000)] public float defaultVoiceDistanceNear;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/playewar-audio#set-voice-distance-far</remarks>
        /// </summary>
        [Range(0, 1000000)] public float defaultVoiceDistanceFar = 25f;

        /// <summary>
        /// Default is 15. In my experience this may lead to clipping when being close to someone with a loud microphone.
        /// My recommendation is to use 0 instead.
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-gain</remarks>
        /// </summary>
        [Range(0, 24)] public float defaultVoiceGain = 15f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#set-voice-volumetric-radius</remarks>
        /// </summary>
        [Range(0, 1000)] public float defaultVoiceVolumetricRadius;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudioforcespatial</remarks>
        /// </summary>
        [Header("Avatar Settings")] public bool defaultForceAvatarSpatialAudio;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiocustomcurve</remarks>
        /// </summary>
        public bool defaultAllowAvatarCustomAudioCurves = true;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudionearradius</remarks>
        /// </summary>
        public float defaultAvatarNearRadius = 1f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiofarradius</remarks>
        /// </summary>
        public float defaultAvatarFarRadius = 100f;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiogain</remarks>
        /// </summary>
        [Range(0, 10)] public float defaultAvatarGain;

        /// <summary>
        /// <remarks>https://docs.vrchat.com/docs/player-audio#setavataraudiovolumetricradius</remarks>
        /// </summary>
        public float defaultAvatarVolumetricRadius;

        #endregion

        #region currently used values

        /// <summary>
        /// <inheritdoc cref="defaultAllowMasterControl"/>
        /// </summary>
        private bool _allowMasterControl;

        /// <summary>
        /// <inheritdoc cref="defaultOcclusionFactor"/>
        /// </summary>
        [NonSerialized] public float OcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="defaultPlayerOcclusionFactor"/>
        /// </summary>
        [NonSerialized] public float PlayerOcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="defaultListenerDirectionality"/>
        /// </summary>
        [NonSerialized] public float ListenerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultPlayerDirectionality"/>
        /// </summary>
        [NonSerialized] public float PlayerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultEnableVoiceLowpass"/>
        /// </summary>
        [NonSerialized] public bool EnableVoiceLowpass;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceNear"/>
        /// </summary>
        [NonSerialized] public float TargetVoiceDistanceNear;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceFar"/>
        /// </summary>
        [NonSerialized] public float TargetVoiceDistanceFar;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceGain"/>
        /// </summary>
        [NonSerialized] public float TargetVoiceGain;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceVolumetricRadius"/>
        /// </summary>
        [NonSerialized] public float TargetVoiceVolumetricRadius;

        /// <summary>
        /// <inheritdoc cref="defaultForceAvatarSpatialAudio"/>
        /// </summary>
        [NonSerialized] public bool ForceAvatarSpatialAudio;

        /// <summary>
        /// <inheritdoc cref="defaultAllowAvatarCustomAudioCurves"/>
        /// </summary>
        [NonSerialized] public bool AllowAvatarCustomAudioCurves;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarNearRadius"/>
        /// </summary>
        [NonSerialized] public float TargetAvatarNearRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarFarRadius"/>
        /// </summary>
        [NonSerialized] public float TargetAvatarFarRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarGain"/>
        /// </summary>
        [NonSerialized] public float TargetAvatarGain;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarVolumetricRadius"/>
        /// </summary>
        [NonSerialized] public float TargetAvatarVolumetricRadius;

        #endregion

        #region Synched values

        /// <summary>
        /// <inheritdoc cref="defaultOcclusionFactor"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterOcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="defaultPlayerOcclusionFactor"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterPlayerOcclusionFactor;

        /// <summary>
        /// <inheritdoc cref="defaultListenerDirectionality"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterListenerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultPlayerDirectionality"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterPlayerDirectionality;

        /// <summary>
        /// <inheritdoc cref="defaultEnableVoiceLowpass"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public bool masterEnableVoiceLowpass;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceNear"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetVoiceDistanceNear;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceDistanceFar"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetVoiceDistanceFar;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceGain"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetVoiceGain;

        /// <summary>
        /// <inheritdoc cref="defaultVoiceVolumetricRadius"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetVoiceVolumetricRadius;

        /// <summary>
        /// <inheritdoc cref="defaultForceAvatarSpatialAudio"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public bool masterForceAvatarSpatialAudio;

        /// <summary>
        /// <inheritdoc cref="defaultAllowAvatarCustomAudioCurves"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public bool masterAllowAvatarCustomAudioCurves;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarNearRadius"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetAvatarNearRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarFarRadius"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetAvatarFarRadius;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarGain"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetAvatarGain;

        /// <summary>
        /// <inheritdoc cref="defaultAvatarVolumetricRadius"/>
        /// </summary>
        [UdonSynced] [HideInInspector] public float masterTargetAvatarVolumetricRadius;

        #endregion

        private bool _initialized;
        private bool _isReallyOwner;
        private int _playerIndex;
        private VRCPlayerApi[] _players = new VRCPlayerApi[1];
        private int[] _playersToIgnore;
        private readonly RaycastHit[] _rayHits = new RaycastHit[2];

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            // skip local player
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            var playerCount = UpdatePlayerList();
            if (playerCount < 2)
            {
                return;
            }

            var pendingPlayerUpdates = GetPendingPlayerUpdates(playerCount);
            for (var playerUpdate = 0; playerUpdate < pendingPlayerUpdates; ++playerUpdate)
            {
                _playerIndex = (_playerIndex + 1) % playerCount;
                var otherPlayer = _players[_playerIndex];
                if (otherPlayer == null)
                {
                    // this should never be the case!!!
                    continue;
                }

                UpdatePlayer(localPlayer, otherPlayer);
            }
        }

        #endregion

        private void UpdatePlayer(VRCPlayerApi localPlayer, VRCPlayerApi otherPlayer)
        {
            if (otherPlayer == null
                || otherPlayer.playerId == localPlayer.playerId
                || PlayerIsIgnored(otherPlayer))
            {
                return;
            }

            var listenerHead = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var otherPlayerHead = otherPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            var listenerToPlayer = (otherPlayerHead.position - listenerHead.position);
            var direction = listenerToPlayer.normalized;
            var distance = listenerToPlayer.magnitude;

            var occlusionFactor = CalculateOcclusion(listenerHead.position, direction, distance);
            var directionality =
                CalculateDirectionality(listenerHead.rotation, otherPlayerHead.rotation, direction);

            var distanceReduction = directionality * occlusionFactor;
            var voiceDistanceFactor =
                CalculateRangeReduction(distance, distanceReduction, TargetVoiceDistanceFar);
            UpdateVoiceAudio(otherPlayer, voiceDistanceFactor);

            var avatarDistanceFactor = CalculateRangeReduction(distance, distanceReduction, TargetAvatarFarRadius);
            UpdateAvatarAudio(otherPlayer, avatarDistanceFactor);
        }

        private bool PlayerIsIgnored(VRCPlayerApi vrcPlayerApi)
        {
            if (_playersToIgnore == null || !Utilities.IsValid(vrcPlayerApi)) return false;

            return Array.BinarySearch(_playersToIgnore, vrcPlayerApi.playerId) > -1;
        }

        private int GetPendingPlayerUpdates(int playerCount)
        {
            if (playerUpdateRate == -1)
            {
                // this will update all players every update
                return playerCount;
            }

            // calculate how many players need to be updated during this update to perform the requested updates
            // every second
            var pendingUpdates = Mathf.FloorToInt(playerUpdateRate * Time.deltaTime);

            // make sure at least one player gets updated and no player gets updated twice
            return Mathf.Clamp(pendingUpdates, 1, playerCount);
        }


        /// <summary>
        /// initializes all runtime variables using the default values.
        /// Has no effect if called again or if start() was already received.
        /// To reset values use ResetToDefault() instead.
        /// </summary>
        public void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                ResetToDefault();
            }
        }

        public void ResetToDefault()
        {
            OcclusionFactor = defaultOcclusionFactor;
            PlayerOcclusionFactor = defaultPlayerOcclusionFactor;
            ListenerDirectionality = defaultListenerDirectionality;
            PlayerDirectionality = defaultPlayerDirectionality;
            EnableVoiceLowpass = defaultEnableVoiceLowpass;
            TargetVoiceDistanceNear = defaultVoiceDistanceNear;
            TargetVoiceDistanceFar = defaultVoiceDistanceFar;
            TargetVoiceGain = defaultVoiceGain;
            TargetVoiceVolumetricRadius = defaultVoiceVolumetricRadius;
            ForceAvatarSpatialAudio = defaultForceAvatarSpatialAudio;
            AllowAvatarCustomAudioCurves = defaultAllowAvatarCustomAudioCurves;
            TargetAvatarNearRadius = defaultAvatarNearRadius;
            TargetAvatarFarRadius = defaultAvatarFarRadius;
            TargetAvatarGain = defaultAvatarGain;
            TargetAvatarVolumetricRadius = defaultAvatarVolumetricRadius;
        }

        private float CalculateRangeReduction(float distance, float distanceReduction, float maxAudibleRange)
        {
            if (maxAudibleRange <= 0f || Mathf.Abs(distanceReduction - 1f) < 0.01f)
            {
                return 1f;
            }

            var remainingDistanceToFarRadius = maxAudibleRange - distance;
            var playerCouldBeAudible = remainingDistanceToFarRadius > 0f;

            var occlusion = 1f;
            if (playerCouldBeAudible)
            {
                var postOcclusionFarDistance = remainingDistanceToFarRadius * distanceReduction + distance;
                occlusion = postOcclusionFarDistance / maxAudibleRange;
            }

            return occlusion;
        }

        private float CalculateDirectionality(Quaternion listenerHeadRotation, Quaternion playerHeadRotation,
            Vector3 directionToPlayer)
        {
            var listenerForward = listenerHeadRotation * Vector3.forward;
            var playerBackward = playerHeadRotation * Vector3.back;


            var dotListener = 0.5f * (Vector3.Dot(listenerForward, directionToPlayer) + 1f);
            var dotSource = 0.5f * (Vector3.Dot(playerBackward, directionToPlayer) + 1f);

            return Mathf.Clamp01(dotListener + (1 - ListenerDirectionality)) *
                   Mathf.Clamp01(dotSource + (1 - PlayerDirectionality));
        }

        private float CalculateOcclusion(Vector3 listenerHead, Vector3 direction, float distance)
        {
            var occlusionIsOff = Mathf.Abs(OcclusionFactor - 1f) < 0.01f
                                 && Mathf.Abs(PlayerOcclusionFactor - 1f) < 0.01f;
            if (occlusionIsOff)
            {
                // don't waste time ray casting when it doesn't have any effect
                return 1f;
            }

            var hits = Physics.RaycastNonAlloc(listenerHead,
                direction,
                _rayHits,
                distance,
                occlusionMask);
            if (hits == 0)
            {
                // nothing to do
                return 1f;
            }

            // if the UI layer is used for occlusion (UI layer contains the player capsules) allow at least one hit
            var playersCanOcclude = (occlusionMask | UILayerMask) > 0;
            if (!playersCanOcclude)
            {
                // result when players can't occlude other players
                return hits > 0 ? OcclusionFactor : 1f;
            }

            if (hits < 2)
            {
                // sometimes the other player's head leaves it's own UI player capsule which causes
                // the number of hits to go down by 1
                // or there was no environment hit while the player UI capsule was hit

                // check how far away the hit is from the player and if it is above a certain threshold
                // assume an object occludes the player (threshold is 1m for now)
                // TODO find a solution that also works for bigger avatars for which the radius of the capsule can exceed 1m
                var minOcclusionTriggerDistance = distance - 1f;
                var occlusionTriggered = _rayHits[0].distance < minOcclusionTriggerDistance;
                if (!occlusionTriggered)
                {
                    return 1f;
                }

                // if the transform of the hit is not null (due to filtering of player objects by UDON)
                // then the environment got hit and we use regular occlusion values
                return _rayHits[0].transform ? OcclusionFactor : PlayerOcclusionFactor;
            }

            // more then 1 hit indicates the ray hit another player first or hit the environment
            // _rayHits contains 2 valid hits now (not ordered by distance!!!
            // see https://docs.unity3d.com/ScriptReference/Physics.RaycastNonAlloc.html)

            // if in both of the hits the transform is now null (due to filtering of player objects by UDON)
            // this indicates that another player occluded the emitting player we ray casted to.
            var anotherPlayerOccludes = !_rayHits[0].transform && !_rayHits[1].transform;
            if (anotherPlayerOccludes)
            {
                return PlayerOcclusionFactor;
            }

            // just return the occlusion factor for everything else
            return OcclusionFactor;
        }

        private void UpdateVoiceAudio(VRCPlayerApi vrcPlayerApi, float distanceFactor)
        {
            vrcPlayerApi.SetVoiceLowpass(EnableVoiceLowpass);

            vrcPlayerApi.SetVoiceGain(TargetVoiceGain * distanceFactor);
            vrcPlayerApi.SetVoiceDistanceFar(TargetVoiceDistanceFar * distanceFactor);
            vrcPlayerApi.SetVoiceDistanceNear(TargetVoiceDistanceNear * distanceFactor);
            vrcPlayerApi.SetVoiceVolumetricRadius(TargetVoiceVolumetricRadius);
        }

        private void UpdateAvatarAudio(VRCPlayerApi vrcPlayerApi, float occlusion)
        {
            vrcPlayerApi.SetAvatarAudioForceSpatial(ForceAvatarSpatialAudio);
            vrcPlayerApi.SetAvatarAudioCustomCurve(AllowAvatarCustomAudioCurves);

            vrcPlayerApi.SetAvatarAudioGain(TargetAvatarGain * occlusion);
            vrcPlayerApi.SetAvatarAudioFarRadius(TargetAvatarFarRadius * occlusion);
            vrcPlayerApi.SetAvatarAudioNearRadius(TargetAvatarNearRadius * occlusion);
            vrcPlayerApi.SetAvatarAudioVolumetricRadius(TargetAvatarVolumetricRadius);
        }

        /// <summary>
        /// updates the player array for iteration
        /// </summary>
        /// <returns>current player count which can be less then the player array length</returns>
        private int UpdatePlayerList()
        {
            var playerCount = VRCPlayerApi.GetPlayerCount();
            if (_players == null || _players.Length < playerCount)
            {
                _players = new VRCPlayerApi[playerCount];
            }

            VRCPlayerApi.GetPlayers(_players);
            return playerCount;
        }

        public bool IsOwner()
        {
            return _isReallyOwner;
        }

        public override void OnDeserialization()
        {
            if (_isReallyOwner)
            {
                Debug.Log("[<color=#008000>BetterAudio</color>] Taking away ownership as data is received");
                _isReallyOwner = false;
            }

            TryUseMasterValues();
        }

        public override void OnPreSerialization()
        {
            if (Networking.IsOwner(gameObject))
            {
                _isReallyOwner = true;

                masterOcclusionFactor = OcclusionFactor;
                masterPlayerOcclusionFactor = PlayerOcclusionFactor;
                masterListenerDirectionality = ListenerDirectionality;
                masterPlayerDirectionality = PlayerDirectionality;
                masterEnableVoiceLowpass = EnableVoiceLowpass;
                masterTargetVoiceDistanceNear = TargetVoiceDistanceNear;
                masterTargetVoiceDistanceFar = TargetVoiceDistanceFar;
                masterTargetVoiceGain = TargetVoiceGain;
                masterTargetVoiceVolumetricRadius = TargetVoiceVolumetricRadius;
                masterForceAvatarSpatialAudio = ForceAvatarSpatialAudio;
                masterAllowAvatarCustomAudioCurves = AllowAvatarCustomAudioCurves;
                masterTargetAvatarNearRadius = TargetAvatarNearRadius;
                masterTargetAvatarFarRadius = TargetAvatarFarRadius;
                masterTargetAvatarGain = TargetAvatarGain;
                masterTargetAvatarVolumetricRadius = TargetAvatarVolumetricRadius;
            }
            else
            {
                Debug.LogWarning("[<color=#008000>BetterAudio</color>] " +
                                 "Is not really owner but tries to serialize data");
            }
        }

        public override void OnOwnershipTransferred()
        {
            _allowMasterControl = false;
            if (uiController)
            {
                uiController.SendCustomEvent(updateUiEventName);
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject))
            {
                _allowMasterControl = false;
            }

            if (uiController)
            {
                uiController.SendCustomEvent(updateUiEventName);
            }
        }

        public void SetUseMasterControls(bool use)
        {
            if (use && !_allowMasterControl)
            {
                _allowMasterControl = true;
                TryUseMasterValues();
            }
            else
            {
                _allowMasterControl = use;
            }
        }

        public bool AllowMasterTakeControl()
        {
            return _allowMasterControl;
        }

        private bool TryUseMasterValues()
        {
            if (_allowMasterControl && uiController)
            {
                OcclusionFactor = masterOcclusionFactor;
                PlayerOcclusionFactor = masterPlayerOcclusionFactor;
                ListenerDirectionality = masterListenerDirectionality;
                PlayerDirectionality = masterPlayerDirectionality;
                EnableVoiceLowpass = masterEnableVoiceLowpass;
                TargetVoiceDistanceNear = masterTargetVoiceDistanceNear;
                TargetVoiceDistanceFar = masterTargetVoiceDistanceFar;
                TargetVoiceGain = masterTargetVoiceGain;
                TargetVoiceVolumetricRadius = masterTargetVoiceVolumetricRadius;
                ForceAvatarSpatialAudio = masterForceAvatarSpatialAudio;
                AllowAvatarCustomAudioCurves = masterAllowAvatarCustomAudioCurves;
                TargetAvatarNearRadius = masterTargetAvatarNearRadius;
                TargetAvatarFarRadius = masterTargetAvatarFarRadius;
                TargetAvatarGain = masterTargetAvatarGain;
                TargetAvatarVolumetricRadius = masterTargetAvatarVolumetricRadius;

                uiController.SendCustomEvent(updateUiEventName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add a player that shall not be affected by any effect of this script. Upon adding the player
        /// all values of the player will be set to the currently defined values on this script.
        /// Occlusion and directionality effects are reverted if the player was affected.
        /// Multiple calls have no further effect.
        /// Providing an invalid player has no effect.
        /// The ignored players are internally kept in a sorted array (ascending by player id) which is cleaned up every
        /// time a player is successfully added or removed.
        /// This function is local only.
        /// </summary>
        /// <param name="playerToIgnore"></param>
        public void IgnorePlayer(VRCPlayerApi playerToIgnore)
        {
            // validate the player
            if (!Utilities.IsValid(playerToIgnore))
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] " +
                               "BetterPlayerAudio.IgnorePlayer: invalid argument");
                return;
            }

            var vrcPlayerApi = VRCPlayerApi.GetPlayerById(playerToIgnore.playerId);
            if (!Utilities.IsValid(vrcPlayerApi))
            {
                Debug.LogError(
                    $"[<color=#008000>BetterAudio</color>] BetterPlayerAudio.IgnorePlayer: player {playerToIgnore} doesn't exist");
                return;
            }

            var noPlayerIgnoredYet = _playersToIgnore == null || _playersToIgnore.Length < 1;
            if (noPlayerIgnoredYet)
            {
                // simply add the player and return
                _playersToIgnore = new[]
                {
                    vrcPlayerApi.playerId
                };
                return;
            }

            // make sure all contained players are still alive, otherwise remove them
            var validPlayers = 0;
            var stillValidIgnoredPlayers = new int[_playersToIgnore.Length];
            var playerAdded = false;

            foreach (var playerId in _playersToIgnore)
            {
                if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(playerId)))
                {
                    // skip (=remove) the player
                    continue;
                }

                // keep all valid players
                stillValidIgnoredPlayers[validPlayers] = playerId;
                ++validPlayers;

                // keep track if the player is already in the array while validating all players in the array
                var playerIsAlreadyIgnored = playerId == vrcPlayerApi.playerId;
                if (playerIsAlreadyIgnored)
                {
                    playerAdded = true;
                    continue;
                }

                // insert the new player at the current position if the insert position is found
                var insertPositionFound = playerId < vrcPlayerApi.playerId && !playerAdded;
                if (!insertPositionFound)
                {
                    continue;
                }

                var longerStillValidIgnoredPlayers = new int[stillValidIgnoredPlayers.Length + 1];
                stillValidIgnoredPlayers.CopyTo(longerStillValidIgnoredPlayers, 0);
                stillValidIgnoredPlayers = longerStillValidIgnoredPlayers;
                stillValidIgnoredPlayers[validPlayers] = vrcPlayerApi.playerId;
                ++validPlayers;
            }

            // shrink the validated array content (happens when ignored players have left the world)
            // and store it again in the old array
            _playersToIgnore = new int[validPlayers];
            for (var i = 0; i < validPlayers; i++)
            {
                _playersToIgnore[i] = stillValidIgnoredPlayers[i];
            }
        }

        /// <summary>
        /// Remove a player from the ignore list and let it be affected again by this script.
        /// The ignored players are internally kept in a sorted array (ascending by player id) which is cleaned up every
        /// time a player is removed.
        /// This function is local only.
        /// </summary>
        /// <param name="ignoredPlayer"></param>
        public void UnIgnorePlayer(VRCPlayerApi ignoredPlayer)
        {
            // validate the player
            if (ignoredPlayer == null)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] " +
                               "BetterPlayerAudio.UnIgnorePlayer: invalid argument");
                return;
            }

            var vrcPlayerApi = VRCPlayerApi.GetPlayerById(ignoredPlayer.playerId);
            if (vrcPlayerApi == null)
            {
                Debug.LogError($"[<color=#008000>BetterAudio</color>] " +
                               $"BetterPlayerAudio.UnIgnorePlayer: player {ignoredPlayer} doesn't exist");
                return;
            }

            if (_playersToIgnore == null || _playersToIgnore.Length < 2)
            {
                _playersToIgnore = null;
                return;
            }

            // make sure all contained players are still alive, otherwise remove them
            var validPlayers = 0;
            var stillValidIgnoredPlayers = new int[_playersToIgnore.Length];

            foreach (var playerId in _playersToIgnore)
            {
                if (VRCPlayerApi.GetPlayerById(playerId) == null)
                {
                    continue;
                }

                // keep all valid players
                stillValidIgnoredPlayers[validPlayers] = playerId;
                ++validPlayers;

                // decrement the index by one again if the player id is found
                if (playerId == vrcPlayerApi.playerId)
                {
                    --validPlayers;
                }
            }

            // shrink the validated array content (happens when ignored players have left the world)
            // and store it again in the old array
            _playersToIgnore = new int[validPlayers];
            for (var i = 0; i < validPlayers; i++)
            {
                _playersToIgnore[i] = stillValidIgnoredPlayers[i];
            }
        }
    }
}