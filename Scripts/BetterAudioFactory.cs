using UdonSharp;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterAudioFactory : UdonSharpBehaviour
    {
        public GameObject audioSourcePrefab;

        public GameObject CreateAudioSourceInstance()
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] BetterAudioFactory.CreateAudioSourceInstance");
            if (audioSourcePrefab.activeSelf)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] BetterAudioFactory.CreateAudioSourceInstance: audioSourcePrefab prefab must be inactive");
                return null;
            }
            
            return VRCInstantiate(audioSourcePrefab);
        }
    }
}