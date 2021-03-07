#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Scripts.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BetterAudioSource))]
    public class BetterAudioSourceEditor : UnityEditor.Editor
    {
        [SerializeField] private int previewDistance = 100;
        private const int PreviewSteps = 100;

        public override void OnInspectorGUI()
        {
            // if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
            // {
            //     return;
            // }

            UdonSharpGUI.DrawUtilities(target);
            UdonSharpGUI.DrawSyncSettings(target);

            DrawDefaultInspector();

            var betterAudio = (BetterAudioSource) target;
            if (!betterAudio)
            {
                return;
            }

            ShowGeneralSettings(betterAudio);
            ShowNoisePreview(betterAudio);


            ValidateAudioSource(betterAudio.GetAudioSourceProxy());
        }

        private static void ShowGeneralSettings(BetterAudioSource betterAudio)
        {
            var editorNoisePreviewKey = "Guribo.UdonBetterAudio.BetterAudioSourceEditor.showGeneralSettings";
            var showGeneralSettings = EditorPrefs.GetBool(editorNoisePreviewKey, false);
            showGeneralSettings =
                EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true, EditorStyles.foldout);
            if (showGeneralSettings || !betterAudio.betterAudioPool)
            {
                betterAudio.betterAudioPool = (BetterAudioPool) EditorGUILayout.ObjectField("Audio Pool",
                    betterAudio.betterAudioPool, typeof(BetterAudioPool), true);
                if (!betterAudio.betterAudioPool)
                {
                    EditorGUILayout.HelpBox(
                        "An audio pool is required for this BetterAudioSource to work!\n" +
                        "Add the prefab to your scene and select it here.",
                        MessageType.Warning, true);
                }
            }

            EditorPrefs.SetBool(editorNoisePreviewKey, showGeneralSettings);
        }

        public void ValidateAudioSource(AudioSource audioSource)
        {
            if (!audioSource)
            {
                Debug.LogError("Invalid audioSource");
                return;
            }

            if (audioSource.enabled)
            {
                audioSource.enabled = false;
            }

            if (audioSource.playOnAwake)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] Please set PlayOnEnable in the udon behaviour instead", this);
                audioSource.playOnAwake = false;
            }

            if (audioSource.outputAudioMixerGroup)
            {
                Debug.LogError(
                    "outputAudioMixerGroup not yet supported by Udon, set it globally in the pool prefab instead",
                    this);
                audioSource.outputAudioMixerGroup = null;
            }

            if (audioSource.bypassEffects)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] bypassEffects not yet supported by Udon, set it globally in the pool prefab instead",
                    this);
                audioSource.bypassEffects = false;
            }

            if (audioSource.bypassListenerEffects)
            {
                Debug.LogError(
                    "bypassListenerEffects not yet supported by Udon, set it globally in the pool prefab instead",
                    this);
                audioSource.bypassListenerEffects = false;
            }

            if (audioSource.mute)
            {
                Debug.LogWarning("[<color=#008000>BetterAudio</color>] audio source is muted", this);
                audioSource.mute = false;
            }
        }

        private void ShowNoisePreview(BetterAudioSource betterAudio)
        {
            var editorNoisePreviewKey = "Guribo.UdonBetterAudio.BetterAudioSourceEditor.showNoisePreview";
            var editorNoiseDistanceKey = "Guribo.UdonBetterAudio.BetterAudioSourceEditor.noiseDistance";

            var showNoisePreview = EditorPrefs.GetBool(editorNoisePreviewKey, false);
            var noiseDistance = EditorPrefs.GetInt(editorNoiseDistanceKey, 100);

            showNoisePreview = EditorGUILayout.Foldout(showNoisePreview, "Distance Noise Preview",
                true,
                EditorStyles.foldout);
            if (showNoisePreview)
            {
                noiseDistance = EditorGUILayout.IntSlider(noiseDistance, 1, 1000);
                var stepSize = ((float) previewDistance / PreviewSteps);
                var noisePreview = new AnimationCurve();

                for (var i = 0; i < PreviewSteps; i++)
                {
                    float time = i * stepSize;
                    float value = betterAudio.NoiseHeight(time,
                        0,
                        betterAudio.noiseScale,
                        betterAudio.noiseLayers,
                        betterAudio.noiseAmplitudeWeight,
                        betterAudio.noiseFrequencyIncrease,
                        betterAudio.noiseChangeRate) * betterAudio.distanceNoise;
                    noisePreview.AddKey(time, value);
                }

                EditorGUILayout.CurveField("Distance Noise Preview", noisePreview);
                EditorGUILayout.HelpBox($"This graph represents the distance noise over {previewDistance} m distance",
                    MessageType.Info);
            }

            EditorPrefs.SetBool(editorNoisePreviewKey, showNoisePreview);
            EditorPrefs.SetInt(editorNoiseDistanceKey, noiseDistance);
        }
    }
}
#endif