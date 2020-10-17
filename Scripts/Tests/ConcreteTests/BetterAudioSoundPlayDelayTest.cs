﻿using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts.Tests.ConcreteTests
{
    public class BetterAudioSoundPlayDelayTest : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [HideInInspector] public BetterAudioTestController betterAudioTestController;

        public void Initialize()
        {
            if (!betterAudioTestController)
            {
                Debug.LogError("Test.Initialize: invalid test controller", this);
                return;
            }

            Debug.Log("Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!betterAudioTestController)
            {
                Debug.LogError("Test.Run: invalid test controller", this);
                return;
            }

            Debug.Log("Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!betterAudioTestController)
            {
                Debug.LogError("Test.CleanUp: invalid test controller", this);
                return;
            }

            Debug.Log("Test.CleanUp", this);
            CleanUpTest();
        }

        #endregion

        #region EDIT HERE

        [SerializeField] private BetterAudioSource betterAudioSource;
        private float _expectedDelay = 3f;
        private float _expectedPlayOffset = 0.5f;
        private float _expectedPlayPosition = 1f;
        private Vector3 _playPosition = new Vector3(3f * 343f, 0, 0);
        private bool _pendingCheck;
        private float _scheduledCheckTime;
        private float _maxDifference;

        private void InitializeTest()
        {
            // TODO your init behaviour here
            // ...

            if (!betterAudioSource)
            {
                Debug.LogError("Invalid better audio source", this);
                betterAudioTestController.TestInitialized(false);
                return;
            }

            if (betterAudioSource.playOnEnable)
            {
                Debug.LogError("Better audio source must have play on enable set to false", this);
                betterAudioTestController.TestInitialized(false);
                return;
            }
            
            if (Mathf.Abs(betterAudioSource.playOffset - _expectedPlayOffset) > 0.001f)
            {
                Debug.LogError($"Better audio source must have a play offset 0f {_expectedPlayOffset} seconds", this);
                betterAudioTestController.TestInitialized(false);
                return;
            }

            var localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                betterAudioSource.transform.position = localPlayer.GetPosition() + _playPosition;
            }
            else
            {
                betterAudioSource.transform.position = _playPosition;
            }

            betterAudioSource.Stop();
            betterAudioSource.gameObject.SetActive(false);
            _pendingCheck = false;

            // whenever the test is ready to be started call _betterAudioTestController.TestInitialized,
            // can be later in update or whenever but MUST be called at some point
            betterAudioTestController.TestInitialized(true);
        }

        private void RunTest()
        {
            var proxyAudioSource = betterAudioSource.GetAudioSourceProxy();
            if (!proxyAudioSource)
            {
                Debug.LogError("Better audio source has no audio source proxy", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (!proxyAudioSource.loop)
            {
                Debug.LogError("Proxy audio source must have loop set to true", this);
                betterAudioTestController.TestInitialized(false);
                return;
            }

            if (proxyAudioSource.clip.length < 4f)
            {
                Debug.LogError("Test requires the audio clip to be at least 4 seconds long", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            betterAudioSource.Play(true);

            var actualAudioSource = betterAudioSource.GetActualAudioSource();
            if (!actualAudioSource)
            {
                Debug.LogError("Better audio source has no actual audio source and is thus not playing", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (actualAudioSource.isPlaying)
            {
                Debug.LogError("Actual audio source is already playing", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            if (!betterAudioSource.IsPlaying())
            {
                Debug.LogError("Better audio source is not saying it is playing", this);
                betterAudioTestController.TestCompleted(false);
                return;
            }

            _scheduledCheckTime = Time.time + _expectedDelay + _expectedPlayOffset;

            _pendingCheck = true;
            _maxDifference = Time.deltaTime;

            Debug.Log("Waiting for delayed checks", this);
        }

        private void CleanUpTest()
        {
            if (betterAudioSource)
            {
                betterAudioSource.Stop();
            }

            betterAudioTestController.TestCleanedUp(true);
        }

        private void Update()
        {
            if (_pendingCheck && Time.time > _scheduledCheckTime)
            {
                _pendingCheck = false;

                var actualAudioSource = betterAudioSource.GetActualAudioSource();
                if (!actualAudioSource)
                {
                    Debug.LogError("No actual audio available", this);
                    betterAudioTestController.TestCompleted(false);
                    return;
                }

                if (!actualAudioSource.isPlaying)
                {
                    Debug.LogError("Actual audio source is not playing", this);
                    betterAudioTestController.TestCompleted(false);
                    return;
                }

                var playedForExpectedDuration = Mathf.Abs(actualAudioSource.time - _expectedPlayPosition) <
                                                _maxDifference + Time.deltaTime;
                if (!playedForExpectedDuration)
                {
                    Debug.LogError(
                        $"Actual audio didn't play for the expected duration of {_expectedPlayPosition}, is {actualAudioSource.time}",
                        this);
                    betterAudioTestController.TestCompleted(false);
                    return;
                }

                betterAudioTestController.TestCompleted(true);
            }
        }

        #endregion
    }
}