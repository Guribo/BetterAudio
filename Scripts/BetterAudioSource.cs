using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class BetterAudioSource : UdonSharpBehaviour
    {
        #region Better Audio

        [Header("Better Audio")] [Tooltip("Use either the spatialized AudioPool or the no-spatialized one")]
        public BetterAudioPool betterAudioPool;

        [Tooltip("Target the actual audio source shall follow, otherwise will not move")] [SerializeField]
        private Transform followTarget;

        #endregion

        #region Audio Settings

        [Header("Audio Source Settings")] [Tooltip("Starts playing the audio as soon as the behaviour is active")]
        public bool playOnEnable;

        public bool playDelayedAccordingToDistance = true;

        [Tooltip("Initial time in the audio clip where to start playing from, only used when not paused/stopped")]
        public float playOffset;


        [Header("Lowpass Filtering Settings")]
        [Tooltip(
            "If this value is not zero it applies lowpass filterung depending on the distance of the audible audio source to the listener using logarithmic falloff with either customDistanceLowpassFilterStart or minimum distance as a start distance (depending on which one is larger)")]
        [Range(0, 2)]
        public float distanceLowpassFiltering = 1f;

        [Tooltip(
            "Applying distance lowpass filtering uses logarithmic fall off. To calculate the falloff the larger value of customDistanceLowpassFilterStart and minimum distance (see AudioSource component) is used. Set this value if  for example a custom falloff curve is used.")]
        public float customDistanceLowpassFilterStart = 0;

        [Tooltip(
            "How much the audio is affected by fake air density changes when far away, increase to increase effect. Set to 0 to deactivate. Is only being used if distanceLowpassFiltering is bigger than 0")]
        [Range(0, 1)]
        public float distanceNoise = 0.5f;

        public float noiseScale = 0.015f;

        [Tooltip("Octaves")] public int noiseLayers = 2;

        [Tooltip("Factor that scales the how much each following layer (octave) contributes to the overall noise")]
        public float noiseAmplitudeWeight = 0.5f;

        [Tooltip("Factor by which the frequency is increased for every additional noise layer (octave)")]
        public float noiseFrequencyIncrease = 2.5f;

        [Tooltip(
            "How fast the noise changes for stationary audio sources, increase to speed up the change rate, can be negative")]
        public float noiseChangeRate = 0.5f;

        [Header("Occlusion Settings")]
        [Tooltip("Objects with collision on these layers can cause the audio source to be occluded")]
        public LayerMask occlusionLayers = 1 << 11; // Environment

        [Tooltip(
            "Smoothing value that controls how fast occlusion changes can be heard, increase for faster transitioning")]
        [Range(0.01f, 30f)]
        public float occlusionChangeSpeed = 10f;

        [Tooltip("Use to prevent checking for collisions all the way to the audio source")]
        public float minOcclusionCheckDistance = 1f;

        [Tooltip("How much occlusion affects volume and low pass filtering, increase value to increase effect")]
        [Range(0, 1)]
        public float occlusionDampeningStrength = 0.333f;

        [Header("Doppler Settings")]
        [Tooltip(
            "When enabled it uses the sonicBoom betterAudioSource and plays it whenever the shockwave of a supersonic object reaches the listener, requires a follow target to be set as well")]
        public bool useSonicBoom;

        [Tooltip("If set the audio source may trigger a sonic boom, requires a follow target to be set as well")]
        public BetterAudioSource sonicBoom;

        [Tooltip(
            "Number of seconds for the audio source to be super sonic before it can trigger a sonic boom, use to prevent frequent sonic booms when close to the super sonic audio source, default value should give around 1km no-sonic-boom zone")]
        public float minSuperSonicDuration = 3f;

        public float dopplerMinPitch = 0.5f;
        public float dopplerMaxPitch = 4f;

        #endregion

        #region Debug

        [Header("Debugging")] [Tooltip("Can be used to display information about this source")] [SerializeField]
        private Text debugText;

        [SerializeField] [Tooltip("Assign the main camera in the editor if required")]
        private Transform debugListener;

        #endregion

        #region States

        private bool _isPaused;
        private bool _isPendingPlay;
        private bool _isDestroyed;
        private bool _isPlaying;
        private bool _started;
        private bool _enabled;

        #endregion

        #region Audio Constants

        private const float SpeedOfSoundSeaLevel = 343f;
        private const float DivBySpeedOfSound = 1f / SpeedOfSoundSeaLevel;

        #endregion

        private VRCPlayerApi _listener;
        private AudioSource _actualAudioSource;
        private AudioSource _proxyAudioSource;
        private AudioLowPassFilter _audioLowPassFilter;
        private AudioReverbFilter _audioReverbFilter;

        private bool _listenerOutrunningAudio;
        private bool _pendingSonicBoom;
        private float _sonicBoomDelay;
        private Vector3 _sonicBoomPosition;
        private float _playStartTime;

        private float _previousListenerDistance;


        private Vector3 _previousPosition;
        // private Vector3 _deltaVelocity;

        private readonly RaycastHit[] _raycastHits = new RaycastHit[1];
        private float _previousOcclusionFactor;

        private const int HistoryLength = 1000;
        private readonly Vector3[] _positionHistory = new Vector3[HistoryLength];
        private readonly Quaternion[] _rotationHistory = new Quaternion[HistoryLength];
        private int _recordingPosition;
        private float _previousDelay;

        private Vector3 _previousListenerPosition;


        // life time is reset on stop and unaffected by pause
        private float _lifeTime;

        #region Monobehaviour

        public void Start()
        {
            if (_listener == null)
            {
                _listener = Networking.LocalPlayer;
            }

            if (_started)
            {
                return;
            }

            if (!_proxyAudioSource)
            {
                _proxyAudioSource = GetAudioSourceProxy();
            }

            CheckReady();
            _started = true;

            if (!_enabled)
            {
                OnEnable();
            }
        }

        private void CheckReady()
        {
            if (!betterAudioPool)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] betterAudioPool invalid", this);
            }

            if (occlusionLayers == 0)
            {
                Debug.LogWarning(
                    "[<color=#008000>BetterAudio</color>] No occlusion collision layer set, audio occlusion won't work",
                    this);
            }

            if (useSonicBoom && !sonicBoom)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] Use Sonic boom is ticked but no better audio source is set",
                    this);
            }

            if (useSonicBoom && !followTarget)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] In order for sonic booms to be triggered a follow target must be set as well",
                    this);
            }

            minOcclusionCheckDistance = Mathf.Max(minOcclusionCheckDistance, 0f);
        }

        public void OnEnable()
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] OnEnable");
            if (_enabled)
            {
                return;
            }

            _enabled = true;
            Debug.Log("[<color=#008000>BetterAudio</color>] BetterSoundSource.OnEnable");

            if (!_proxyAudioSource)
            {
                _proxyAudioSource = GetAudioSourceProxy();
            }

            _pendingSonicBoom = false;

            if (!betterAudioPool && _listener != null)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] BetterAudioSource has no BetterAudioPool assigned");
                gameObject.SetActive(false);
                return;
            }


            if (playOnEnable)
            {
                Play(playDelayedAccordingToDistance);
            }
        }


        private void FixedUpdate()
        {
            if (_actualAudioSource)
            {
                var isNotPlaying = !IsPlaying();
                var isNotPaused = !_isPaused;
                var isNotPendingSonicBoom = !_pendingSonicBoom;
                if (isNotPlaying
                    && isNotPaused
                    && isNotPendingSonicBoom)
                {
                    Debug.Log($"[<color=#008000>BetterAudio</color>] BetterAudioSource.FixedUpdate: nothing to do");
                    Stop();
                    return;
                }

                var deltaTime = Time.fixedDeltaTime;
                _lifeTime += deltaTime;

                var finalCutOffFrequency = 0f;
                var targetPitch = 0f;
                var targetVolume = 0f;

                // position where the sound would be emitting from physically if speed of sound was not a thing
                Vector3 physicalEmitPosition;
                if (followTarget)
                {
                    var followTargetPosition = followTarget.position;
                    var followTargetRotation = followTarget.rotation;

                    physicalEmitPosition = followTargetPosition;

                    // record the current position and rotation (used to simulate effects of speed of sound)
                    UpdateHistory(followTargetPosition, followTargetRotation);

                    _actualAudioSource.transform.SetPositionAndRotation(followTargetPosition,
                        followTargetRotation);
                }
                else
                {
                    physicalEmitPosition = _actualAudioSource.transform.position;
                }

                var listenerPosition = Vector3.zero;
                var listenerRotation = Quaternion.identity;

                if (_listener != null)
                {
                    var headTrackingData = _listener.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    listenerPosition = headTrackingData.position;
                    listenerRotation = headTrackingData.rotation;
                }
                else
                {
                    if (debugListener)
                    {
                        listenerPosition = debugListener.position;
                        listenerRotation = debugListener.rotation;
                    }
                }


                // distance to the listener
                var physicalEmitterDistance = Vector3.Distance(physicalEmitPosition, listenerPosition);

                // how long in seconds it would take for an emitted sound wave to reach the listener
                var audioDelay = physicalEmitterDistance * DivBySpeedOfSound;
                var unclampedAudioDelay = audioDelay;

                var listenerVelocity = (listenerPosition - _previousListenerPosition) / deltaTime;
                _previousListenerPosition = listenerPosition;

                // Debug.Log(listenerVelocity.magnitude);
                var emitterToListenerDirection = (listenerPosition - physicalEmitPosition).normalized;
                var relativeListenerVelocity = Vector3.Project(listenerVelocity, emitterToListenerDirection);
                if (Vector3.Dot(emitterToListenerDirection, relativeListenerVelocity) > 0f)
                {
                    if (relativeListenerVelocity.magnitude > SpeedOfSoundSeaLevel)
                    {
                        //listener is outrunning the audio of the audio source and shall not hear it
                        _listenerOutrunningAudio = true;
                    }
                    else
                    {
                        // listener is just moving away from audio source
                        _listenerOutrunningAudio = false;
                        audioDelay += relativeListenerVelocity.magnitude * DivBySpeedOfSound;
                    }
                }
                else
                {
                    // listener is moving towards audio source
                    _listenerOutrunningAudio = false;
                    audioDelay -= relativeListenerVelocity.magnitude * DivBySpeedOfSound;
                    audioDelay = Mathf.Max(0, audioDelay);
                }


                var delayChange = audioDelay - _previousDelay;
                if (delayChange > deltaTime)
                {
                    audioDelay = _previousDelay + 0.5f * deltaTime;
                }

                _previousDelay = audioDelay;


                if (playDelayedAccordingToDistance
                    && !_isPendingPlay
                    && _actualAudioSource.volume < 0.001f
                    && isNotPendingSonicBoom
                    && (!sonicBoom || (sonicBoom && !sonicBoom.IsPlaying()))
                ) // don't pause in case sonic boom just activated and time volume is still 0
                {
                    _playStartTime = _lifeTime;
                    _isPendingPlay = true;
                }

                if (playDelayedAccordingToDistance
                    && _isPendingPlay
                    && _lifeTime > _playStartTime + unclampedAudioDelay
                    && _proxyAudioSource.volume > 0.001f
                    && isNotPendingSonicBoom)
                {
                    _isPendingPlay = false;
                    _actualAudioSource.volume = 0f;
                    _actualAudioSource.Play();
                }


                // Debug.Log($"[<color=#008000>BetterAudio</color>] {_isPendingPlay} {_lifeTime} {_playStartTime} {unclampedAudioDelay}");


                float audibleSoundDistance;
                var projectedAudioSourceVelocity = 0f;
                if (followTarget)
                {
                    // get the past position and rotation of the sound source using the current audio delay caused by speed of sound

                    // make sure to not reuse any recorded positions and rotation from a past life by clamping to life time
                    var maxAllowedDelay = Mathf.Min(_lifeTime, audioDelay);

                    // how many updates to go into the past (exact value)
                    var historyDistance = maxAllowedDelay / deltaTime;

                    // actual number of updates to go into the past,
                    // the past location/rotation is between this index and the one before that
                    var historyIndexOffset = Mathf.FloorToInt(historyDistance);

                    // remainder used to blend between two recorded entries
                    var blendWeight = historyDistance - historyIndexOffset;

                    // prevent wrapping into recent history in case the history length is exceeded
                    var allowedLowerHistoryOffset = Mathf.Min(HistoryLength - 2, historyIndexOffset);
                    var allowedUpperHistoryOffset = allowedLowerHistoryOffset + 1;

                    var upperHistoryIndex = _recordingPosition - allowedLowerHistoryOffset;
                    if (upperHistoryIndex < 0)
                    {
                        // wrap integer manually instead of using modulo to make sure it works
                        upperHistoryIndex += HistoryLength;
                    }

                    var lowerHistoryIndex = _recordingPosition - allowedUpperHistoryOffset;
                    if (lowerHistoryIndex < 0)
                    {
                        // wrap integer manually instead of using modulo to make sure it works
                        lowerHistoryIndex += HistoryLength;
                    }

                    // exact past position based on audio delay from the current position to the listener
                    var pastPosition = Vector3.Lerp(_positionHistory[upperHistoryIndex],
                        _positionHistory[lowerHistoryIndex], blendWeight);

                    // exact past rotation based on audio delay from the current position to the listener
                    var pastRotation = Quaternion.Slerp(_rotationHistory[upperHistoryIndex],
                        _rotationHistory[lowerHistoryIndex], blendWeight);

                    // place the audible audio source at the past position
                    // to make it sound like the audio ist still being emitted from that location
                    _actualAudioSource.transform.SetPositionAndRotation(pastPosition,
                        pastRotation);

                    var audioSourceVelocity =
                        (_positionHistory[lowerHistoryIndex] - _positionHistory[upperHistoryIndex]) / deltaTime;
                    var audioToListenerDirection = (listenerPosition - pastPosition).normalized;
                    var projectedAudioSourceVelocityVector =
                        Vector3.Project(audioSourceVelocity, audioToListenerDirection);
                    projectedAudioSourceVelocity = projectedAudioSourceVelocityVector.magnitude *
                                                   -Mathf.Sign(Vector3.Dot(projectedAudioSourceVelocityVector,
                                                       audioToListenerDirection));
                    // Debug.Log(projectedAudioSourceVelocity);

                    // distance to the sound that can be heard by the listener
                    audibleSoundDistance = Vector3.Distance(pastPosition, listenerPosition);
                }
                else
                {
                    audibleSoundDistance = physicalEmitterDistance;
                }

                // rate in m/s at which the listener is moving away from the audible sound position (or vice versa)
                var distanceIncreaseVelocity = (audibleSoundDistance - _previousListenerDistance) / deltaTime;
                _previousListenerDistance = audibleSoundDistance;

                // check whether the listener is moving away from the sound faster then the sound can follow it


                var dopplerDistortion = 1f + (distanceIncreaseVelocity * DivBySpeedOfSound);

                if (_pendingSonicBoom)
                {
                    if (audioDelay < _sonicBoomDelay || _listenerOutrunningAudio)
                    {
                        _sonicBoomPosition = physicalEmitPosition;
                        _sonicBoomDelay = audioDelay;
                    }
                    else
                    {
                        _sonicBoomDelay -= deltaTime;

                        if (_sonicBoomDelay <= 0f && dopplerDistortion > 0f)
                        {
                            if (isNotPaused && _isPlaying)
                            {
                                // _audioSource.Play();
                                if (useSonicBoom && sonicBoom)
                                {
                                    sonicBoom.transform.position = _sonicBoomPosition;
                                    sonicBoom.Stop();
                                    sonicBoom.Play(false);
                                }
                            }

                            _pendingSonicBoom = false;
                        }
                    }
                }
                else
                {
                    targetVolume = _proxyAudioSource.volume;
                    targetPitch = _proxyAudioSource.pitch;
                    finalCutOffFrequency = 22000f;

                    var soundSourcePosition = _actualAudioSource.transform.position;


                    // occlusion effects
                    if (occlusionDampeningStrength > 0)
                    {
                        var occlusionFactor =
                            CalculateOcclusion(audibleSoundDistance, listenerPosition, soundSourcePosition,
                                deltaTime);
                        var occlusionFactorSquared = occlusionFactor * occlusionFactor;
                        finalCutOffFrequency *= occlusionFactorSquared;
                        targetVolume *= occlusionFactor;
                    }

                    // distance lowpass filtering and noise
                    if (distanceLowpassFiltering > 0.01f)
                    {
                        var audioDistanceRolloff = GetLogarithmicRolloff(
                            Mathf.Max(_actualAudioSource.minDistance, customDistanceLowpassFilterStart),
                            audibleSoundDistance);
                        // var audioDistanceRolloff = GetRollOffFromCurve(audibleSoundDistance); TODO await support of curves

                        var relativeFrequencyLossOverDistance = (1f - audioDistanceRolloff) * distanceLowpassFiltering;

                        if (distanceNoise > 0.01f)
                        {
                            var noise = NoiseHeight(soundSourcePosition.x,
                                soundSourcePosition.y,
                                noiseScale,
                                noiseLayers,
                                noiseAmplitudeWeight,
                                noiseFrequencyIncrease,
                                noiseChangeRate) * distanceNoise;
                            relativeFrequencyLossOverDistance *= noise;
                        }

                        finalCutOffFrequency *=
                            Mathf.Clamp01(audioDistanceRolloff * (1f + relativeFrequencyLossOverDistance));
                    }


                    if (projectedAudioSourceVelocity < SpeedOfSoundSeaLevel && isNotPendingSonicBoom)
                    {
                        if (_listenerOutrunningAudio)
                        {
                            targetVolume = 0;
                        }

                        if (_proxyAudioSource.dopplerLevel > 0.01f)
                        {
                            var dopplerLevel = dopplerDistortion * _proxyAudioSource.dopplerLevel;
                            var doppler = dopplerLevel > 0 ? targetPitch / dopplerLevel : dopplerMaxPitch * targetPitch;
                            targetPitch *= Mathf.Clamp(doppler, dopplerMinPitch, dopplerMaxPitch);
                        }
                    }
                    else
                    {
                        if (useSonicBoom && sonicBoom && isNotPendingSonicBoom && audioDelay > minSuperSonicDuration)
                        {
                            targetVolume = 0f;
                            targetPitch = 0f;
                            _pendingSonicBoom = true;
                            _sonicBoomDelay = audioDelay;
                            _sonicBoomPosition = physicalEmitPosition;
                        }
                    }

                    if (debugText)
                    {
                        debugText.text =
                            $"{distanceIncreaseVelocity} m/s\nvolume: {_actualAudioSource.volume}\npitch: {_actualAudioSource.pitch}";
                    }
                }


                if (debugText && _pendingSonicBoom)
                {
                    debugText.text = $"pending sonic boom in T-{_sonicBoomDelay}";
                }

                _actualAudioSource.volume = Mathf.Lerp(_actualAudioSource.volume, targetVolume, 0.25f);
                _actualAudioSource.pitch = Mathf.Lerp(_actualAudioSource.pitch, targetPitch, 0.1f);
                _audioLowPassFilter.cutoffFrequency =
                    Mathf.Lerp(_audioLowPassFilter.cutoffFrequency, finalCutOffFrequency, 0.1f);
            }
        }


        public void SetTime(float f)
        {
            if (_actualAudioSource)
            {
                _actualAudioSource.time = 0.1f;
            }
        }

        private float CalculateOcclusion(float distance, Vector3 listenerPosition, Vector3 soundSourcePosition,
            float deltaTime)
        {
            // occlusion checks
            var occlusionCheckDistance = Mathf.Max(0.1f, distance - minOcclusionCheckDistance);
            var hits = Physics.RaycastNonAlloc(listenerPosition,
                (soundSourcePosition - listenerPosition).normalized,
                _raycastHits, occlusionCheckDistance, occlusionLayers);
            var targetOcclusionFactor = hits > 0 ? 1f * (1f - occlusionDampeningStrength) : 1f;
            var occlusionFactor =
                Mathf.Lerp(_previousOcclusionFactor, targetOcclusionFactor,
                    deltaTime * occlusionChangeSpeed);
            _previousOcclusionFactor = occlusionFactor;
            return occlusionFactor;
        }

        public void OnDisable()
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] OnDisable");
            if (!_enabled)
            {
                return;
            }

            _enabled = false;
            Debug.Log("[<color=#008000>BetterAudio</color>] BetterSoundSource.OnDisable");

            if (IsPlaying())
            {
                Stop();
            }

            _lifeTime = 0f;
            _isPaused = false;
            _isPlaying = false;
            _pendingSonicBoom = false;

            if (_actualAudioSource && betterAudioPool)
            {
                betterAudioPool.ReturnToPool(_actualAudioSource.gameObject);
                _actualAudioSource = null;
            }
        }

        public void OnDestroy()
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] OnDestroy");
            if (_isDestroyed)
            {
                return;
            }

            _isDestroyed = true;
            Debug.Log("[<color=#008000>BetterAudio</color>] BetterSoundSource.OnDestroy");

            OnDisable();
        }

        #endregion

        public void Play(bool delayUsingSpeedOfSound)
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] BetterSoundSource.Play");
            if (!betterAudioPool)
            {
                if (_listener != null)
                {
                    Debug.LogError(
                        "[<color=#008000>BetterAudio</color>] BetterAudioSource has no BetterAudioPool assigned");
                    gameObject.SetActive(false);
                }

                return;
            }

            if (!_actualAudioSource)
            {
                var audioSourceInstance = betterAudioPool.GetAudioSourceInstance(this);
                if (!audioSourceInstance)
                {
                    Debug.LogError(
                        "[<color=#008000>BetterAudio</color>] BetterAudioSource.Play: received invalid audio source instance");
                    return;
                }

                var go = audioSourceInstance.gameObject;

                _actualAudioSource = go.GetComponent<AudioSource>();
                if (!_actualAudioSource)
                {
                    Debug.LogError(
                        "[<color=#008000>BetterAudio</color>] BetterAudioSource.Play: received audio source instance is missing an AudioSource");
                    return;
                }

                _audioReverbFilter = go.GetComponent<AudioReverbFilter>();
                if (!_audioReverbFilter)
                {
                    Debug.LogError(
                        "BetterAudioSource.Play: received audio source instance is missing an AudioReverbFilter");
                    return;
                }

                // _audioReverbFilter.reverbPreset = AudioReverbPreset.Off;

                _audioLowPassFilter = go.GetComponent<AudioLowPassFilter>();
                if (!_audioLowPassFilter)
                {
                    Debug.LogError(
                        "BetterAudioSource.Play: received audio source instance is missing an AudioLowPassFilter");
                    return;
                }
            }

            if (IsPlaying())
            {
                return;
            }

            // init the lowpass filter

            _isPlaying = true;
            playDelayedAccordingToDistance = delayUsingSpeedOfSound;

            _audioLowPassFilter.cutoffFrequency = 22000f;
            _playStartTime = _lifeTime;

            if (!playDelayedAccordingToDistance)
            {
                _isPendingPlay = false;
                _actualAudioSource.Play();
            }
            else
            {
                _isPendingPlay = true;
            }

            if (!_isPaused)
            {
                _actualAudioSource.time = playOffset;
            }

            _isPaused = false;

            gameObject.SetActive(true);
            FixedUpdate();
        }

        public void Pause()
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] Pause");
            if (_actualAudioSource)
            {
                _actualAudioSource.Pause();
            }

            _isPaused = true;
            _isPlaying = true;
            _pendingSonicBoom = false;
        }

        public void Stop()
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] BetterSoundSource.Stop");
            if (_actualAudioSource)
            {
                _actualAudioSource.Stop();
            }

            if (gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
            }
        }

        public AudioClip GetAudioClip()
        {
            Debug.Log("[<color=#008000>BetterAudio</color>] GetAudioClip");
            if (_actualAudioSource)
            {
                return _actualAudioSource.clip;
            }

            return _proxyAudioSource.clip;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.LocalPlayer == player)
            {
                _listener = player;
                _enabled = false;
                OnEnable();
                _started = false;
                Start();
            }
        }

        public float GetLogarithmicRolloff(float minDistance, float distance)
        {
            var divisor = (1 + (distance - 1));
            if (divisor > 0)
            {
                return Mathf.Clamp01(minDistance * (1 / divisor));
            }

            return 1f;
        }

        [Obsolete("Not yet supported by UDON, use GetLogarithmicRolloff for now")]
        public float GetRollOffFromCurve(float distance)
        {
            return 0f;
            // if (!_proxyAudioSource) return 0;
            // return _proxyAudioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff).Evaluate(distance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="positionScale"></param>
        /// <param name="octaves"> >= 1</param>
        /// <param name="amplitudeWeight"></param>
        /// <param name="frequencyIncrease"> >= 1</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="changeRate"></param>
        /// <returns></returns>
        public float NoiseHeight(
            float x,
            float y,
            float positionScale = 0.01f,
            int octaves = 3,
            float amplitudeWeight = 0.33f,
            float frequencyIncrease = 2f,
            float changeRate = 0.1f)
        {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            var xPos = x * positionScale;
            var yPos = y * positionScale;

            var offset = (float) Networking.GetServerTimeInSeconds() * changeRate;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = xPos / frequency + offset;
                float sampleY = yPos / frequency + offset;

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                amplitude *= amplitudeWeight;
                frequency *= frequencyIncrease;
            }

            return noiseHeight;
        }

        private void UpdateHistory(Vector3 position, Quaternion rotation)
        {
            _recordingPosition = (_recordingPosition + 1) % HistoryLength;
            _positionHistory[_recordingPosition] = position;
            _rotationHistory[_recordingPosition] = rotation;
        }

        public bool IsPlaying()
        {
            return _isPlaying && _actualAudioSource != null && (_actualAudioSource.isPlaying || _isPendingPlay);
        }

        public float GetVolume()
        {
            if (_proxyAudioSource)
            {
                return _proxyAudioSource.volume;
            }

            return 0;
        }

        public void SetVolume(float volume)
        {
            if (_proxyAudioSource)
            {
                _proxyAudioSource.volume = volume;
            }
        }


        public float GetMaxDistance()
        {
            if (_proxyAudioSource)
            {
                return _proxyAudioSource.maxDistance;
            }

            return 0;
        }

        public void SetPitch(float pitch)
        {
            if (_proxyAudioSource)
            {
                _proxyAudioSource.pitch = pitch;
            }
        }

        public float GetPitch()
        {
            if (_proxyAudioSource)
            {
                return _proxyAudioSource.pitch;
            }

            return 0;
        }

        public AudioSource GetAudioSourceProxy()
        {
            if (!_proxyAudioSource)
            {
                _proxyAudioSource = GetComponent<AudioSource>();
                if (!_proxyAudioSource)
                {
                    Debug.LogError(
                        "BetterAudioSource.GetAudioSourceProxy: BetterAudioSource must have an AudioSource on the gameobject",
                        this);
                }
            }

            return _proxyAudioSource;
        }

        /// <summary>
        /// DO NOT EDIT ANY VALUES ON THIS AUDIO SOURCE, TREAT AS READ ONLY!!! EDIT THE PROXY AUDIO SOURCE INSTEAD!!!
        /// </summary>
        /// <returns>the audio source that can actually be heard, used by the pooling system so DO NOT EDIT!!!</returns>
        public AudioSource GetActualAudioSource()
        {
            return _actualAudioSource;
        }

        // public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        // {
        //     // Debug.Log($"[<color=#008000>BetterAudio</color>] OnPlayerTriggerEnter {player}");
        // }
        //
        // public override void OnPlayerTriggerExit(VRCPlayerApi player)
        // {
        //     // Debug.Log($"[<color=#008000>BetterAudio</color>] OnPlayerTriggerExit {player}");
        // }
        //
        // public override void OnPlayerTriggerStay(VRCPlayerApi player)
        // {
        //     // Debug.Log($"[<color=#008000>BetterAudio</color>] OnPlayerTriggerStay {player}");
        // }
        //
        private void OnTriggerEnter(Collider other)
        {
            // Debug.Log($"[<color=#008000>BetterAudio</color>] OnTriggerEnter {other}");
        }

        // private void OnTriggerExit(Collider other)
        // {
        //     // Debug.Log($"[<color=#008000>BetterAudio</color>] OnTriggerExit {other}");
        //     if (!other) return;
        //     if (!_audioReverbFilter) return;
        //     var audioReverbFilterProxy = other.gameObject.GetComponent<AudioReverbFilter>();
        //     if (audioReverbFilterProxy)
        //     {
        //         _audioReverbFilter.enabled = false;
        //     }
        // }
        //
        // private void OnTriggerStay(Collider other)
        // {
        //     if (Time.frameCount % 30 != 0)
        //     {
        //         return;
        //     }
        //
        //     // Debug.Log($"[<color=#008000>BetterAudio</color>] OnTriggerStay {other}");
        //     if (!other) return;
        //     if (!_actualAudioSource) return;
        //     if (!_proxyAudioSource) return;
        //     if (_proxyAudioSource.bypassReverbZones) return;
        //     if (!_audioReverbFilter) return;
        //
        //     // var penetration = CalculateReverbZonePenetration(other);
        //     // Debug.Log($"[<color=#008000>BetterAudio</color>] Penetration = {penetration}");
        //
        //     var audioReverbFilterProxy = other.gameObject.GetComponent<AudioReverbFilter>();
        //     if (audioReverbFilterProxy)
        //     {
        //         _audioReverbFilter.reverbPreset = AudioReverbPreset.User;
        //         _audioReverbFilter.room =
        //             audioReverbFilterProxy.room; //Mathf.Lerp(-10000f, audioReverbFilterProxy.room, penetration);
        //         _audioReverbFilter.roomHF =
        //             audioReverbFilterProxy.room; //Mathf.Lerp(-10000f, audioReverbFilterProxy.roomHF, penetration);
        //         _audioReverbFilter.roomLF =
        //             audioReverbFilterProxy.room; //Mathf.Lerp(-10000f, audioReverbFilterProxy.roomLF, penetration);
        //         _audioReverbFilter.density = audioReverbFilterProxy.density;
        //         _audioReverbFilter.decayTime = audioReverbFilterProxy.decayTime;
        //         _audioReverbFilter.decayHFRatio = audioReverbFilterProxy.decayHFRatio;
        //         _audioReverbFilter.reflectionsDelay = audioReverbFilterProxy.reflectionsDelay;
        //         _audioReverbFilter.reflectionsLevel = audioReverbFilterProxy.reflectionsLevel;
        //         _audioReverbFilter.dryLevel = audioReverbFilterProxy.dryLevel;
        //         _audioReverbFilter.diffusion = audioReverbFilterProxy.diffusion;
        //         _audioReverbFilter.hfReference = audioReverbFilterProxy.hfReference;
        //         _audioReverbFilter.lfReference = audioReverbFilterProxy.lfReference;
        //         _audioReverbFilter.reverbDelay = audioReverbFilterProxy.reverbDelay;
        //         _audioReverbFilter.reverbLevel = audioReverbFilterProxy.reverbLevel;
        //         _audioReverbFilter.enabled = true;
        //     }
        //
        //     if (_actualAudioSource.spatialize)
        //     {
        //         var listenerIsNotInZone = !ListenerIsInZone(other);
        //         if (_actualAudioSource.spatializePostEffects != listenerIsNotInZone)
        //         {
        //             _actualAudioSource.volume = 0.01f;
        //             _actualAudioSource.spatializePostEffects = listenerIsNotInZone;
        //         }
        //     }
        // }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if listener is inside</returns>
        private bool ListenerIsInZone(Collider other)
        {
            Vector3 position = Vector3.zero;
            if (_listener != null)
            {
                position = _listener.GetPosition();
            }
            else if (debugListener)
            {
                position = debugListener.position;
            }

            var closestPointOnTrigger = other.ClosestPoint(position);
            var boundsCenter = other.bounds.center;
            var distanceSurfaceToZoneCenter = Vector3.Distance(boundsCenter, closestPointOnTrigger);
            var distanceAudioToZoneCenter = Vector3.Distance(boundsCenter, position);

            return Mathf.Abs(distanceSurfaceToZoneCenter - distanceAudioToZoneCenter) < 0.01f;
        }

        private float CalculateReverbZonePenetration(Collider other)
        {
            float penetration = 1f;
            var minDistance = _proxyAudioSource.minDistance;
            if (minDistance > 0)
            {
                var position = transform.position;
                var closestPoint = other.ClosestPoint(position);
                var boundsCenter = other.bounds.center;
                var distanceSurfaceToZoneCenter = Vector3.Distance(boundsCenter, closestPoint);
                var distanceAudioToZoneCenter = Vector3.Distance(boundsCenter, position);
                if (Mathf.Abs(distanceSurfaceToZoneCenter - distanceAudioToZoneCenter) < 0.01f)
                {
                    // audio source is entirely inside the zone
                }
                else if (distanceSurfaceToZoneCenter > 0f)
                {
                    var distanceAudioToContact = Vector3.Distance(closestPoint, position);

                    // if the audio source has a smaller range then the zone is big use the zone size at that
                    // point instead to determine penetration
                    var min = Mathf.Min(distanceSurfaceToZoneCenter, minDistance);
                    penetration = Mathf.Clamp01((minDistance - distanceAudioToContact) / min);
                }
            }

            return penetration;
        }

        public bool IsPendingSonicBoom()
        {
            return _pendingSonicBoom;
        }

        public float GetSonicBoomDelay()
        {
            return _sonicBoomDelay;
        }

        public Vector3 GetSonicBoomAudioPosition()
        {
            return _sonicBoomPosition;
        }
    }
}