using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts
{
    /// <summary>
    /// This override contains values that can be used to override the default audio settings in
    /// <see cref="BetterPlayerAudio"/> for a group of players.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BetterPlayerAudioOverride : UdonSharpBehaviour
    {
        #region Constants

        private const int EnvironmentLayerMask = 1 << 11;
        private const int UILayerMask = 1 << 5;

        #endregion

        /// <summary>
        /// Players that this override should be applied to. Must be sorted at all times to allow searching inside with binary search!
        /// </summary>
        protected int[] AffectedPlayers = null;

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

        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultListenerDirectionality"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when the listener is facing away from a voice/avatar and thus making them more quiet.")]
        public float listenerDirectionality = 0.5f;

        [Header("Voice Settings")]
        /// <summary>
        /// <inheritdoc cref="BetterPlayerAudio.defaultPlayerDirectionality"/>
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "A value of 1.0 reduces the ranges by up to 100% when someone is speaking/playing avatar sounds but is facing away from the listener.")]
        public float playerDirectionality = 0.3f;

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

        [Header("Avatar Settings")]
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


        /// <summary>
        /// Add players to the list of players that should make use of the here defined override values.
        /// Methods can be expensive when call with a lot of players! Avoid using in Update in every frame!
        /// </summary>
        /// <param name="playersToAffect"></param>
        /// <returns>true if the players were added/were already added before</returns>
        public bool AffectPlayers(VRCPlayerApi[] playersToAffect)
        {
            if (playersToAffect == null || playersToAffect.Length == 0)
            {
                Debug.LogError(
                    $"[<color=#008000>BetterAudio</color>] BetterPlayerAudioOverride.AffectPlayers: received invalid player");
                return false;
            }

            // add all players
            foreach (var playerToAffect in playersToAffect)
            {
                if (!Utilities.IsValid(playerToAffect))
                {
                    Debug.LogError(
                        $"[<color=#008000>BetterAudio</color>] BetterPlayerAudioOverride.AffectPlayers: playersToAffect contains invalid player");
                    continue;
                }

                // if no player is affected yet simply add the player
                if (AffectedPlayers == null)
                {
                    AffectedPlayers = new[]
                    {
                        playerToAffect.playerId
                    };
                    continue;
                }

                var playerAlreadyAffected = Array.BinarySearch(AffectedPlayers, playerToAffect.playerId) > -1;
                if (playerAlreadyAffected)
                {
                    // player already affected, nothing to do
                    continue;
                }

                // add the player to the list 
                var tempArray = new int[AffectedPlayers.Length + 1];
                AffectedPlayers.CopyTo(tempArray, 0);
                AffectedPlayers = tempArray;
                AffectedPlayers[AffectedPlayers.Length - 1] = playerToAffect.playerId;

                // sort it afterwards to allow binary search to work again
                Sort(AffectedPlayers);
            }

            return true;
        }

        private readonly VRCPlayerApi[] _singlePlayer = new VRCPlayerApi[1];

        /// <summary>
        /// Add a single player to the list of players that should make use of the here defined override values.
        /// When adding multiple players use <see cref="AffectPlayers"/> instead.
        /// Methods can be expensive when call with a lot of players! Avoid using in Update in every frame!
        /// </summary>
        /// <param name="playerToAffect"></param>
        /// <returns>true if the player was added/was already added before</returns>
        public bool AffectPlayer(VRCPlayerApi playerToAffect)
        {
            _singlePlayer[0] = playerToAffect;
            return AffectPlayers(_singlePlayer);
        }

        /// <summary>
        /// Remove players from the list of players that should make use of the here defined override values.
        /// </summary>
        /// <param name="playersToRemove"></param>
        /// <returns>true if the players were removed/not affected yet</returns>
        public bool RemoveAffectedPlayers(VRCPlayerApi[] playersToRemove)
        {
            if (playersToRemove == null || playersToRemove.Length == 0)
            {
                Debug.LogError(
                    $"[<color=#008000>BetterAudio</color>] BetterPlayerAudioOverride.AffectPlayers: received invalid player");
                return false;
            }

            if (AffectedPlayers == null || AffectedPlayers.Length == 0)
            {
                // nothing to do
                return true;
            }

            // create a look-up list for players to be removed
            var playerIdsToRemove = new int[playersToRemove.Length];
            for (var i = 0; i < playersToRemove.Length; i++)
            {
                if (Utilities.IsValid(playersToRemove[i]))
                {
                    playerIdsToRemove[i] = playersToRemove[i].playerId;
                }
            }

            // sort for checking existence with binary search
            Sort(playerIdsToRemove);

            var removedPlayers = 0;

            // remove all players
            for (var i = 0; i < AffectedPlayers.Length; i++)
            {
                // mark invalid players/players to be removed for removal
                var affectedPlayer = VRCPlayerApi.GetPlayerById(AffectedPlayers[i]);
                if (!Utilities.IsValid(affectedPlayer)
                    || Array.BinarySearch(playerIdsToRemove, affectedPlayer.playerId) > -1)
                {
                    // player should be removed, mark for disposal
                    AffectedPlayers[i] = int.MaxValue;
                    removedPlayers++;
                }
            }

            // sort the players afterwards which moves all invalid player ids to the end of the array
            Sort(AffectedPlayers);

            // shrink the array which automatically removes the invalid player ids
            var tempArray = new int[AffectedPlayers.Length - removedPlayers];
            Array.ConstrainedCopy(AffectedPlayers, 0, tempArray, 0, AffectedPlayers.Length - removedPlayers);
            AffectedPlayers = tempArray;
            return true;
        }

        /// <summary>
        /// Remove player from the list of players that should make use of the here defined override values.
        /// </summary>
        /// <param name="playerToRemove"></param>
        /// <returns>true if the player was removed/not affected yet</returns>
        public bool RemoveAffectedPlayer(VRCPlayerApi playerToRemove)
        {
            _singlePlayer[0] = playerToRemove;
            return RemoveAffectedPlayers(_singlePlayer);
        }

        /// <summary>
        /// Check whether the player given as playerId should make use of the here defined override values
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns>true if the player should be affected</returns>
        public bool IsAffected(int playerId)
        {
            if (AffectedPlayers == null || AffectedPlayers.Length == 0)
            {
                return false;
            }

            return Array.BinarySearch(AffectedPlayers, playerId) > -1;
        }

        /// <summary>
        /// Returns array of affected players (can contain invalid players so make sure to check for validity with <see cref="Utilities.IsValid"/>.
        /// </summary>
        /// <returns>affected players or empty array if none are affected</returns>
        public int[] GetAffectedPlayers()
        {
            if (AffectedPlayers == null)
            {
                return new int[0];
            }

            return AffectedPlayers;
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
                for (var j = 0; j < arrayLength -1; j++)
                {
                    var next = j+1;

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