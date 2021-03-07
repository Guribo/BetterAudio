#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Scripts.Tests
{
    public class BetterAudioTestReadme : MonoBehaviour
    {
        [TextArea(3, 20)] [Tooltip("README")]
        public string information = "README";
    }
}

#endif