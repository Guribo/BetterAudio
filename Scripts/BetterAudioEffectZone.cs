using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterAudioEffectZone : UdonSharpBehaviour
    {
        [SerializeField] private BoxCollider boxCollider;
        [SerializeField] private AudioReverbFilter audioReverbFilter;

        [Tooltip(
            "Use when the box collider size can change dynamically which requires updating the echo/reverb in the room, requires the gameobject to be not static")]
        [SerializeField]
        private bool autoUpdate;

        #region Monobehaviour Methods

        private void Start()
        {
            if (!boxCollider)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] BetterAudioZone.Start: boxCollider is not set", this);
                return;
            }

            if (!boxCollider.isTrigger)
            {
                boxCollider.isTrigger = true;
                Debug.LogWarning("[<color=#008000>BetterAudio</color>] BetterAudioZone.Start: boxCollider needed to be changed to trigger", this);
            }

            if (!audioReverbFilter)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] BetterAudioZone.Start: audioReverbFilter is not set", this);
                return;
            }

            audioReverbFilter.reverbPreset = AudioReverbPreset.User;
            UpdateRoomReverbFromSize(audioReverbFilter, boxCollider);

            if (autoUpdate && gameObject.isStatic)
            {
                autoUpdate = false;
                Debug.LogWarning("[<color=#008000>BetterAudio</color>] BetterAudioZone.Start: autoUpdate is disabled as the gameobject is static", this);
            }

            // TODO disable just the UdonBehaviour if autoupdate is false and check whether trigger events still trigger
            // enabled = autoUpdate;
            if (!autoUpdate)
            {
                Destroy(this);
            }
        }

        public void Update()
        {
            if (autoUpdate && boxCollider && audioReverbFilter)
            {
                UpdateRoomReverbFromSize(audioReverbFilter, boxCollider);
            }
            else
            {
                // TODO
                // enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] OnTriggerEnter {other}");
        }

        // private void OnTriggerStay(Collider other)
        // {
        // Debug.Log($"[<color=#008000>BetterAudio</color>] OnTriggerStay {other}");
        // }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] OnTriggerExit {other}");
        }

        #endregion


        /// <summary>
        /// updates reverb delays, duration and echo based on the size of the box collider of this zone
        /// </summary>
        public void RecalculateRoomReverbFromSize()
        {
            if (boxCollider && audioReverbFilter)
            {
                UpdateRoomReverbFromSize(audioReverbFilter, boxCollider);
            }
        }

        private void UpdateRoomReverbFromSize(AudioReverbFilter reverbFilter, BoxCollider trigger)
        {
            var localSize = trigger.size;

            var localSizeX = localSize.x;
            var localSizeY = localSize.y;
            var localSizeZ = localSize.z;
            var minSize = Mathf.Min(localSizeX, localSizeY, localSizeZ);
            // var maxSize = Mathf.Min(localSizeX, localSizeY, localSizeZ);
            var volume = localSizeX * localSizeY * localSizeZ;

            reverbFilter.decayTime = Mathf.Clamp(volume / 343f, 0.1f, 20f);
            reverbFilter.reverbDelay = Mathf.Clamp(minSize / 343f, 0f, 0.1f);
        }

        #region UdonBehaviour Methods

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] OnPlayerTriggerEnter {player}");
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            Debug.Log($"[<color=#008000>BetterAudio</color>] OnPlayerTriggerExit {player}");
        }

        #endregion
    }
}