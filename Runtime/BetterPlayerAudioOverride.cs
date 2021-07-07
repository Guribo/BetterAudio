﻿using System;
using Guribo.UdonUtils.Runtime.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Runtime
{
    /// <summary>
    /// This override contains values that can be used to override the default audio settings in
    /// <see cref="BetterPlayerAudio"/> for a group of players.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BetterPlayerAudioOverride : UdonSharpBehaviour
    {
        #region Settings

        [Header("General settings")]

        /// <summary>
        /// Determines whether it 
        /// Overrides with equal or higher values can override other overrides with lower priority.
        /// When removed from a player and it was currently the highest priority override it will
        /// fall back to the next lower override available.
        /// </summary>
        public int priority;
        
        #endregion

        /// <summary>
        /// Players that this override should be applied to. Must be sorted at all times to allow searching inside with binary search!
        /// </summary>
        protected int[] AffectedPlayers;

        #region Occlusion settings
        
        #region Constants

        private const int EnvironmentLayerMask = 1 << 11;
        private const int UILayerMask = 1 << 5;

        #endregion
        
        [Header("Occlusion settings")]

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.occlusionMask"/>
        /// </summary>
        [Tooltip(
            "Objects on these layers reduce the voice/avatar sound volume when they are in-between the local player and the player/avatar that produces the sound")]
        public LayerMask occlusionMask = EnvironmentLayerMask | UILayerMask;


        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultOcclusionFactor"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float occlusionFactor = 0.7f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultPlayerOcclusionFactor"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Occlusion when a player is occluded by another player. A value of 1.0 means occlusion is off. A value of 0 will reduce the max. audible range of the voice/player to the current distance and make him/her/them in-audible")]
        public float playerOcclusionFactor = 0.85f;

        #endregion

        #region Directionality settings
        
        [Header("Directionality settings")]
        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultListenerDirectionality"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when the listener is facing away from a voice/avatar and thus making them more quiet.")]
        public float listenerDirectionality = 0.5f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultPlayerDirectionality"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is facing away from the listener.")]
        public float playerDirectionality = 0.3f;

        #endregion
        
        #region Reverb settings

        [Header("Reverb settings")]
        public AudioReverbFilter optionalReverb;

        #endregion


        #region Voice settings

        [Header("Voice settings")]
        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultEnableVoiceLowpass"/>
        /// </summary>
        [Tooltip("When enabled the voice of a player sounds muffled when close to the max. audible range.")]
        public bool enableVoiceLowpass = true;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultVoiceDistanceNear"/>
        /// </summary>
        [Tooltip("The volume will stay at max. when the player is closer than this distance.")]
        [Range(0, 1000000)] public float voiceDistanceNear;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultVoiceDistanceFar"/>
        /// </summary>
        [Tooltip("Beyond this distance the player can't be heard.")]
        [Range(0, 1000000)] public float voiceDistanceFar = 25f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultVoiceGain"/>
        /// </summary>
        [Tooltip("Additional volume increase. Changing this may require re-adjusting occlusion parameters!")]
        [Range(0, 24)] public float voiceGain = 15f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultVoiceVolumetricRadius"/>
        /// </summary>
        [Tooltip(
            "Range in which the player voice is not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed.")]
        [Range(0, 1000)] public float voiceVolumetricRadius;

        #endregion

        #region Avatar settings

        [Header("Avatar settings")]
        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultForceAvatarSpatialAudio"/>
        /// </summary>
        [Tooltip("When set overrides all avatar audio sources to be spatialized.")]
        public bool forceAvatarSpatialAudio;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAllowAvatarCustomAudioCurves"/>
        /// </summary>
        [Tooltip("When set custom audio curves on avatar audio sources are used.")]
        public bool allowAvatarCustomAudioCurves = true;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAvatarNearRadius"/>
        /// </summary>
        [Tooltip("Max. distance at which player audio sources start to fall of in volume.")]
        public float targetAvatarNearRadius = 40f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAvatarFarRadius"/>
        /// </summary>
        [Tooltip("Max. allowed distance at which player audio sources can be heard.")]
        public float targetAvatarFarRadius = 40f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAvatarGain"/>
        /// </summary>
        [Range(0, 10)]
        [Tooltip("Volume increase in decibel.")]
        public float targetAvatarGain = 10f;

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultAvatarVolumetricRadius"/>
        /// </summary>
        [Tooltip(
            "Range in which the player audio sources are not spatialized. Increases experienced volume by a lot! May require extensive tweaking of gain/range parameters when being changed.")]
        public float targetAvatarVolumetricRadius;

        #endregion

        #region Privacy Settings

        /// <summary>
        /// Players affected by different overrides with the same privacy channel id can hear each other and can't be
        /// heard by non-affected players.
        /// If set to -1 the feature is turned off for this override component and only players affected by this
        /// override can hear each other.
        /// </summary>
        [Header("Privacy settings")]
        [Tooltip(
            "Players affected by different overrides with the same privacy channel id can hear each other and can't be heard by non-affected players. If set to -1 the feature is turned off and all players affected by this override can be heard.")]
        public int privacyChannelId = -1;

        /// <summary>
        /// If set to true affected players also can't hear non-affected players.
        /// Only in effect in combination with a privacy channel not equal to -1.
        /// </summary>
        [Tooltip(
            "If set to true affected players also can't hear non-affected players. Only in effect in combination with a privacy channel not equal to -1.")]
        public bool muteOutsiders = true;
        
        #endregion
        
        #region Mandatory references

        [Header("Mandatory references")]
        public BetterPlayerAudio betterPlayerAudio;
        public UdonDebug udonDebug;

        #endregion

        #region State

        private bool _hasStarted;

        #endregion
        
        public void Start()
        {
            if (_hasStarted)
            {
                return;
            }

            _hasStarted = true;
            DeactivateReverb();
        }

        /// <summary>
        /// Add a single player to the list of players that should make use of the here defined override values.
        /// When adding multiple players use <see cref="AffectPlayers"/> instead.
        /// Methods can be expensive when call with a lot of players! Avoid using in Update in every frame!
        /// </summary>
        /// <param name="playerToAffect"></param>
        /// <returns>true if the player was added/was already added before</returns>
        public bool AddPlayer(VRCPlayerApi playerToAffect)
        {
            if (!_hasStarted)
            {
                Start();
            }
            
            if (!Utilities.IsValid(playerToAffect))
            {
                return false;
            }

            // if no player is affected yet simply add the player
            var playerId = playerToAffect.playerId;
            if (AffectedPlayers == null)
            {
                AffectedPlayers = new[]
                {
                    playerId
                };
            }
            else
            {
                var playerAlreadyAffected = Array.BinarySearch(AffectedPlayers, playerId) > -1;
                if (playerAlreadyAffected)
                {
                    // player already affected, nothing to do
                    return true;
                }

                // add the player to the list 
                var tempArray = new int[AffectedPlayers.Length + 1];
                AffectedPlayers.CopyTo(tempArray, 0);
                AffectedPlayers = tempArray;
                AffectedPlayers[AffectedPlayers.Length - 1] = playerId;

                // sort it afterwards to allow binary search to work again
                Sort(AffectedPlayers);
            }

            if (playerToAffect.isLocal)
            {
                ActivateReverb();
            }
            
            // have the controller affect all players that are currently added to the override
            if (!Utilities.IsValid(betterPlayerAudio))
            {
                return false;
            }
            
            return betterPlayerAudio.OverridePlayerSettings(this, playerToAffect);
        }

        /// <summary>
        /// Remove player from the list of players that should make use of the here defined override values.
        /// </summary>
        /// <param name="playerToRemove"></param>
        /// <returns>true if the player was removed/not affected yet</returns>
        public bool RemovePlayer(VRCPlayerApi playerToRemove)
        {
            if (!udonDebug.Assert(Utilities.IsValid(playerToRemove), "Player to remove invalid", this))
            {
                return false;
            }

            if (AffectedPlayers == null || AffectedPlayers.Length == 0)
            {
                // nothing to do so player was not removed
                return false;
            }

            var removedPlayers = 0;

            // remove all players
            for (var i = 0; i < AffectedPlayers.Length; i++)
            {
                // mark invalid players/players to be removed for removal
                var affectedPlayer = VRCPlayerApi.GetPlayerById(AffectedPlayers[i]);
                if (!Utilities.IsValid(affectedPlayer)
                    || playerToRemove.playerId == affectedPlayer.playerId)
                {
                    // player should be removed, mark for disposal
                    AffectedPlayers[i] = int.MaxValue;
                    removedPlayers++;
                    break;
                }
            }

            // sort the players afterwards which moves all invalid player ids to the end of the array
            Sort(AffectedPlayers);

            // shrink the array which automatically removes the invalid player ids
            var tempArray = new int[AffectedPlayers.Length - removedPlayers];
            Array.ConstrainedCopy(AffectedPlayers, 0, tempArray, 0, AffectedPlayers.Length - removedPlayers);
            AffectedPlayers = tempArray;

            if (playerToRemove.isLocal)
            {
                DeactivateReverb();
            }

            // ReSharper disable once PossibleNullReferenceException invalid as checked with udonDebug.Assert
            if (!udonDebug.Assert(Utilities.IsValid(betterPlayerAudio), "PlayerAudio invalid", this))
            {
                return false;
            }
            
            // make the controller apply default settings to the player again
            return removedPlayers > 0 && betterPlayerAudio.ClearPlayerOverride(this, playerToRemove);
        }

        /// <summary>
        /// Check whether the player given as playerId should make use of the here defined override values
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns>true if the player should be affected</returns>
        public bool IsAffected(VRCPlayerApi player)
        {
            if (!udonDebug.Assert(Utilities.IsValid(player), "Player is invalid", this))
            {
                return false;
            }

            if (AffectedPlayers == null || AffectedPlayers.Length == 0)
            {
                return false;
            }

            return Array.BinarySearch(AffectedPlayers, player.playerId) > -1;
        }

        /// <summary>
        /// Returns a copy of the array of affected players (can contain invalid players so make sure to check for validity with <see cref="Utilities.IsValid"/>.
        /// </summary>
        /// <returns>affected players or empty array if none are affected</returns>
        public int[] GetAffectedPlayers()
        {
            if (AffectedPlayers == null || AffectedPlayers.Length == 0)
            {
                return new int[0];
            }

            var tempArray = new int[AffectedPlayers.Length];
            AffectedPlayers.CopyTo(tempArray,0);
            return tempArray;
        }
        
        public bool Clear()
        {
            DeactivateReverb();
            
            if (!udonDebug.Assert(Utilities.IsValid(betterPlayerAudio), "playerAudio invalid", this))
            {
                return false;
            }

            if (AffectedPlayers != null)
            {
                foreach (var affectedPlayer in AffectedPlayers)
                {
                    betterPlayerAudio.ClearPlayerOverride(this, VRCPlayerApi.GetPlayerById(affectedPlayer));
                }

                AffectedPlayers = new int[0];
            }
            
            return true;
        }
        
        private void DeactivateReverb()
        {
            if (ReverbValid())
            {
                optionalReverb.gameObject.SetActive(false);
            }
        }
        
        private void ActivateReverb()
        {
            if (ReverbValid())
            {
                optionalReverb.gameObject.SetActive(true);
            }
        }

        private bool ReverbValid()
        {
            return Utilities.IsValid(optionalReverb)
                   && udonDebug.Assert(Utilities.IsValid(optionalReverb.gameObject.GetComponent(typeof(AudioListener))),
                       "For reverb to work an AudioListener is required on the gameobject with the Reverb Filter",
                       this);
        }
        
        #region Sorting

        private void Sort(int[] array)
        {
            if (array == null || array.Length < 2)
            {
                return;
            }

            BubbleSort(array);
        }

        private void BubbleSort(int[] array)
        {
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
            {
                for (var j = 0; j < arrayLength - 1; j++)
                {
                    var next = j + 1;

                    if (array[j] > array[next])
                    {
                        var tmp = array[j];
                        array[j] = array[next];
                        array[next] = tmp;
                    }
                }
            }
        }

        public void TestSorting()
        {
            var array = new[] {0, 5, 3, 2, 10, 5, -1};
            var s = "Unsorted: ";
            foreach (var i in array)
            {
                s += i + ",";
            }

            Debug.Log(s);
            Sort(array);

            s = "Sorted: ";
            foreach (var i in array)
            {
                s += i + ",";
            }

            Debug.Log(s);
        }

        #endregion

    }
}