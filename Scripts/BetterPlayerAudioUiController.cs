using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BetterPlayerAudioUiController : UdonSharpBehaviour
    {
        [SerializeField] private BetterPlayerAudio betterPlayerAudio;

        [Header("General Settings")] [SerializeField]
        private Slider sliderOcclusionFactor;

        [SerializeField] private Slider sliderPlayerOcclusionFactor;

        [SerializeField] private Slider sliderListenerDirectionality;
        [SerializeField] private Slider sliderPlayerDirectionality;

        [SerializeField] private Text textListenerDirectionality;
        [SerializeField] private Text textPlayerDirectionality;
        [SerializeField] private Text textOcclusionFactor;
        [SerializeField] private Text textPlayerOcclusionFactor;

        [SerializeField] private Text textAllowMasterControl;
        [SerializeField] private Toggle toggleAllowMasterControl;

        [Header("Voice Settings")] [SerializeField]
        private Slider sliderVoiceDistanceNear;

        [SerializeField] private Slider sliderVoiceDistanceFar;
        [SerializeField] private Slider sliderVoiceGain;
        [SerializeField] private Slider sliderVoiceVolumetricRadius;

        [SerializeField] private Text textVoiceDistanceNear;
        [SerializeField] private Text textVoiceDistanceFar;
        [SerializeField] private Text textVoiceGain;
        [SerializeField] private Text textVoiceVolumetricRadius;

        [SerializeField] private Toggle toggleVoiceLowpass;

        [Header("Avatar Settings")] [SerializeField]
        private Slider sliderAvatarDistanceNear;

        [SerializeField] private Slider sliderAvatarDistanceFar;
        [SerializeField] private Slider sliderAvatarGain;
        [SerializeField] private Slider sliderAvatarVolumetricRadius;

        [SerializeField] private Text texAvatarDistanceNear;
        [SerializeField] private Text texAvatarDistanceFar;
        [SerializeField] private Text texAvatarGain;
        [SerializeField] private Text texAvatarVolumetricRadius;

        [SerializeField] private Toggle toggleAvatarSpatialize;
        [SerializeField] private Toggle toggleAvatarCustomCurve;

        private RectTransform[] _tabs;

        private bool _preventChangeEvent;

        public void Start()
        {
            if (!betterPlayerAudio)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] Invalid betterPlayerAudio");
            }

            // make sure the betterPlayerAudio component is initialized, to prevent accidentally setting everything to 0
            betterPlayerAudio.Initialize();
            ResetAll();
        }


        public void OnSettingsChanged()
        {
            if (_preventChangeEvent)
            {
                return;
            }

            betterPlayerAudio.OcclusionFactor = sliderOcclusionFactor.value;
            betterPlayerAudio.PlayerOcclusionFactor = sliderPlayerOcclusionFactor.value;
            betterPlayerAudio.PlayerDirectionality = sliderPlayerDirectionality.value;
            betterPlayerAudio.ListenerDirectionality = sliderListenerDirectionality.value;

            betterPlayerAudio.TargetVoiceDistanceNear = sliderVoiceDistanceNear.value;
            betterPlayerAudio.TargetVoiceDistanceFar = sliderVoiceDistanceFar.value;
            betterPlayerAudio.TargetVoiceGain = sliderVoiceGain.value;
            betterPlayerAudio.TargetVoiceVolumetricRadius = sliderVoiceVolumetricRadius.value;
            betterPlayerAudio.EnableVoiceLowpass = toggleVoiceLowpass.isOn;

            betterPlayerAudio.TargetAvatarNearRadius = sliderAvatarDistanceNear.value;
            betterPlayerAudio.TargetAvatarFarRadius = sliderAvatarDistanceFar.value;
            betterPlayerAudio.TargetAvatarGain = sliderAvatarGain.value;
            betterPlayerAudio.TargetAvatarVolumetricRadius = sliderAvatarVolumetricRadius.value;
            betterPlayerAudio.ForceAvatarSpatialAudio = toggleAvatarSpatialize.isOn;
            betterPlayerAudio.AllowAvatarCustomAudioCurves = toggleAvatarCustomCurve.isOn;

            Debug.Log($"OnSettingsChanged: Values in UI "
                      + $"{sliderOcclusionFactor.value}, "
                      + $"{sliderPlayerOcclusionFactor.value}, "
                      + $"{sliderPlayerDirectionality.value}, "
                      + $"{ sliderListenerDirectionality.value}, "
                      + $"{ sliderVoiceDistanceNear.value}, "
                      + $"{sliderVoiceDistanceFar.value}, "
                      + $"{sliderVoiceGain.value}, "
                      + $"{sliderVoiceVolumetricRadius.value}, "
                      + $"{ toggleVoiceLowpass.isOn}, "
                      + $"{ sliderAvatarDistanceNear.value}, "
                      + $"{ sliderAvatarDistanceFar.value}, "
                      + $"{ sliderAvatarGain.value}, "
                      + $"{sliderAvatarVolumetricRadius.value}, "
                      + $"{ toggleAvatarSpatialize.isOn}, "
                      + $"{ toggleAvatarCustomCurve.isOn}");

            Debug.Log($"OnSettingsChanged: Values in BetterPlayerAudio updated from UI: "
                      + $"{betterPlayerAudio.OcclusionFactor}, "
                      + $"{betterPlayerAudio.PlayerOcclusionFactor}, "
                      + $"{betterPlayerAudio.PlayerDirectionality}, "
                      + $"{betterPlayerAudio.ListenerDirectionality}, "
                      + $"{betterPlayerAudio.TargetVoiceDistanceNear}, "
                      + $"{betterPlayerAudio.TargetVoiceDistanceFar}, "
                      + $"{betterPlayerAudio.TargetVoiceGain}, "
                      + $"{betterPlayerAudio.TargetVoiceVolumetricRadius}, "
                      + $"{betterPlayerAudio.EnableVoiceLowpass}, "
                      + $"{betterPlayerAudio.TargetAvatarNearRadius}, "
                      + $"{betterPlayerAudio.TargetAvatarFarRadius}, "
                      + $"{betterPlayerAudio.TargetAvatarGain}, "
                      + $"{betterPlayerAudio.TargetAvatarVolumetricRadius}, "
                      + $"{betterPlayerAudio.ForceAvatarSpatialAudio}, "
                      + $"{betterPlayerAudio.AllowAvatarCustomAudioCurves}");
            
            betterPlayerAudio.SetUseMasterControls(toggleAllowMasterControl.isOn);
            betterPlayerAudio.SetDirty();

            textOcclusionFactor.text = sliderOcclusionFactor.value.ToString("F");
            textPlayerOcclusionFactor.text = sliderPlayerOcclusionFactor.value.ToString("F");
            textPlayerDirectionality.text = sliderPlayerDirectionality.value.ToString("F");
            textListenerDirectionality.text = sliderListenerDirectionality.value.ToString("F");

            textVoiceDistanceNear.text = sliderVoiceDistanceNear.value.ToString("F1");
            textVoiceDistanceFar.text = sliderVoiceDistanceFar.value.ToString("F1");
            textVoiceGain.text = sliderVoiceGain.value.ToString("F1");
            textVoiceVolumetricRadius.text = sliderVoiceVolumetricRadius.value.ToString("F1");

            texAvatarDistanceNear.text = sliderAvatarDistanceNear.value.ToString("F1");
            texAvatarDistanceFar.text = sliderAvatarDistanceFar.value.ToString("F1");
            texAvatarGain.text = sliderAvatarGain.value.ToString("F1");
            texAvatarVolumetricRadius.text = sliderAvatarVolumetricRadius.value.ToString("F1");

            var locallyControlled = !betterPlayerAudio.AllowMasterTakeControl() || betterPlayerAudio.IsOwner();

            sliderOcclusionFactor.interactable = locallyControlled;
            sliderPlayerOcclusionFactor.interactable = locallyControlled;
            sliderListenerDirectionality.interactable = locallyControlled;
            sliderPlayerDirectionality.interactable = locallyControlled;
            sliderVoiceDistanceNear.interactable = locallyControlled;
            sliderVoiceDistanceFar.interactable = locallyControlled;
            sliderVoiceGain.interactable = locallyControlled;
            sliderVoiceVolumetricRadius.interactable = locallyControlled;
            toggleVoiceLowpass.interactable = locallyControlled;
            sliderAvatarDistanceNear.interactable = locallyControlled;
            sliderAvatarDistanceFar.interactable = locallyControlled;
            sliderAvatarGain.interactable = locallyControlled;
            sliderAvatarVolumetricRadius.interactable = locallyControlled;
            toggleAvatarSpatialize.interactable = locallyControlled;
            toggleAvatarCustomCurve.interactable = locallyControlled;

            toggleAllowMasterControl.interactable = !betterPlayerAudio.IsOwner();
        }

        public void ResetAll()
        {
            _preventChangeEvent = true;

            // Resetting the sliders/toggles will cause the betterPlayerAudio script to be reset automatically
            // due to the change events being triggered, so there is no need to call betterPlayerAudio.Reset()
            toggleAllowMasterControl.isOn = betterPlayerAudio.defaultAllowMasterControl;

            sliderOcclusionFactor.value = betterPlayerAudio.defaultOcclusionFactor;
            sliderPlayerOcclusionFactor.value = betterPlayerAudio.defaultPlayerOcclusionFactor;
            sliderListenerDirectionality.value = betterPlayerAudio.defaultListenerDirectionality;
            sliderPlayerDirectionality.value = betterPlayerAudio.defaultPlayerDirectionality;

            sliderVoiceDistanceNear.value = betterPlayerAudio.defaultVoiceDistanceNear;
            sliderVoiceDistanceFar.value = betterPlayerAudio.defaultVoiceDistanceFar;
            sliderVoiceGain.value = betterPlayerAudio.defaultVoiceGain;
            sliderVoiceVolumetricRadius.value = betterPlayerAudio.defaultVoiceVolumetricRadius;

            toggleVoiceLowpass.isOn = betterPlayerAudio.defaultEnableVoiceLowpass;

            sliderAvatarDistanceNear.value = betterPlayerAudio.defaultAvatarNearRadius;
            sliderAvatarDistanceFar.value = betterPlayerAudio.defaultAvatarFarRadius;
            sliderAvatarGain.value = betterPlayerAudio.defaultAvatarGain;
            sliderAvatarVolumetricRadius.value = betterPlayerAudio.defaultAvatarVolumetricRadius;

            toggleAvatarSpatialize.isOn = betterPlayerAudio.defaultForceAvatarSpatialAudio;
            toggleAvatarCustomCurve.isOn = betterPlayerAudio.defaultAllowAvatarCustomAudioCurves;

            _preventChangeEvent = false;
            OnSettingsChanged();
        }

        /// <summary>
        /// Callback function, can be called by the BetterPlayerAudio to update the UI
        /// </summary>
        public void UpdateUi()
        {
            var owner = Networking.GetOwner(betterPlayerAudio.gameObject);
            if (Utilities.IsValid(owner))
            {
                textAllowMasterControl.text = $"Let {owner.displayName} (owner) control everything";
            }
            
            _preventChangeEvent = true;

            sliderOcclusionFactor.value = betterPlayerAudio.OcclusionFactor;
            sliderPlayerOcclusionFactor.value = betterPlayerAudio.PlayerOcclusionFactor;
            sliderListenerDirectionality.value = betterPlayerAudio.ListenerDirectionality;
            sliderPlayerDirectionality.value = betterPlayerAudio.PlayerDirectionality;

            sliderVoiceDistanceNear.value = betterPlayerAudio.TargetVoiceDistanceNear;
            sliderVoiceDistanceFar.value = betterPlayerAudio.TargetVoiceDistanceFar;
            sliderVoiceGain.value = betterPlayerAudio.TargetVoiceGain;
            sliderVoiceVolumetricRadius.value = betterPlayerAudio.TargetVoiceVolumetricRadius;

            toggleVoiceLowpass.isOn = betterPlayerAudio.EnableVoiceLowpass;

            sliderAvatarDistanceNear.value = betterPlayerAudio.TargetAvatarNearRadius;
            sliderAvatarDistanceFar.value = betterPlayerAudio.TargetAvatarFarRadius;
            sliderAvatarGain.value = betterPlayerAudio.TargetAvatarGain;
            sliderAvatarVolumetricRadius.value = betterPlayerAudio.TargetAvatarVolumetricRadius;

            toggleAvatarSpatialize.isOn = betterPlayerAudio.ForceAvatarSpatialAudio;
            toggleAvatarCustomCurve.isOn = betterPlayerAudio.AllowAvatarCustomAudioCurves;

            toggleAllowMasterControl.isOn = betterPlayerAudio.AllowMasterTakeControl();
            
            _preventChangeEvent = false;
            
            OnSettingsChanged();
        }
    }
}