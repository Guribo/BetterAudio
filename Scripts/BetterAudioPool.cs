using UdonSharp;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterAudioPool : UdonSharpBehaviour
    {
        [SerializeField] private BetterAudioFactory betterAudioFactory;

        #region Interface

        public Transform GetAudioSourceInstance(BetterAudioSource betterAudioSource)
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] BetterAudioPool.GetAudioSourceInstance");
            if (!betterAudioSource)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] BetterAudioPool.GetAudioSourceInstance: received an invalid BetterAudioSource");
                return null;
            }

            var audioSourceInstance = GetPooledAudioSourceInstance();
            if (!audioSourceInstance)
            {
                audioSourceInstance = betterAudioFactory.CreateAudioSourceInstance();
            }

            if (!audioSourceInstance)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] BetterAudioPool.GetAudioSourceInstance: " +
                               "failed to get or create audioSourceInstance");
                return null;
            }

            var sourceInstance = audioSourceInstance.transform;
            sourceInstance.parent = null;

            if (audioSourceInstance.gameObject.activeSelf)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] BetterAudioPool.GetAudioSourceInstance: " +
                               "GetAudioSourceInstance expected the audioSourceInstance to be inactive");
                return null;
            }

            Spawn(betterAudioSource, sourceInstance);

            return sourceInstance;
        }


        public void ReturnToPool(GameObject audioSourceInstance)
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] ReturnToPool");

            if (audioSourceInstance)
            {
                audioSourceInstance.SetActive(false);

                var t = audioSourceInstance.transform;
                if (t)
                {
                    if (gameObject)
                    {
                        t.parent = gameObject.transform;
                    }
                }
            }
        }

        #endregion

        private GameObject GetPooledAudioSourceInstance()
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] GetPooledAudioSourceInstance");
            if (transform.childCount > 0)
            {
                var child = transform.GetChild(0);
                return child.gameObject;
            }

            return null;
        }


        private void Spawn(BetterAudioSource betterAudioSource, Transform audioSourceInstance)
        {
            var spawnTransform = betterAudioSource.transform;
            audioSourceInstance.SetPositionAndRotation(spawnTransform.position, spawnTransform.rotation);

            var proxyAudioSource = betterAudioSource.GetAudioSourceProxy();
            var actualAudioSource = audioSourceInstance.GetComponent<AudioSource>();
            CopyProxySettings(actualAudioSource, proxyAudioSource);

            actualAudioSource.enabled = true;
            audioSourceInstance.gameObject.SetActive(true);
        }

        private void CopyProxySettings(AudioSource actualAudioSource, AudioSource proxyAudioSource)
        {
            // initialize audio source by copying appropriate values
            actualAudioSource.time = 0;

            actualAudioSource.clip = proxyAudioSource.clip;
            // actualAudioSource.outputAudioMixerGroup = proxyAudioSource.outputAudioMixerGroup; // TODO not yet supported by udon

            actualAudioSource.mute = proxyAudioSource.mute;
            // actualAudioSource.bypassEffects = proxyAudioSource.bypassEffects; // TODO not yet supported by udon
            // actualAudioSource.bypassListenerEffects = proxyAudioSource.bypassListenerEffects; // TODO not yet supported by udon
            actualAudioSource.bypassReverbZones = proxyAudioSource.bypassReverbZones;
            actualAudioSource.playOnAwake = false;
            actualAudioSource.loop = proxyAudioSource.loop;

            actualAudioSource.priority = proxyAudioSource.priority;
            actualAudioSource.volume = proxyAudioSource.volume;
            actualAudioSource.pitch = proxyAudioSource.pitch;
            actualAudioSource.panStereo = proxyAudioSource.panStereo;
            actualAudioSource.spatialBlend = proxyAudioSource.spatialBlend;
            actualAudioSource.reverbZoneMix = proxyAudioSource.reverbZoneMix;

            // 3d sound settings
            actualAudioSource.dopplerLevel = 0;
            actualAudioSource.spread = proxyAudioSource.spread;
            actualAudioSource.rolloffMode = proxyAudioSource.rolloffMode;
            actualAudioSource.minDistance = proxyAudioSource.minDistance;
            actualAudioSource.maxDistance = proxyAudioSource.maxDistance;

            actualAudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
                proxyAudioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff));

            actualAudioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend,
                proxyAudioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend));

            actualAudioSource.SetCustomCurve(AudioSourceCurveType.Spread,
                proxyAudioSource.GetCustomCurve(AudioSourceCurveType.Spread));

            actualAudioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix,
                proxyAudioSource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));

            // don't use these, these are set by choosing the pool
            // actualAudioSource.spatialize = proxyAudioSource.spatialize;
            // actualAudioSource.spatializePostEffects = false;
        }
    }
}