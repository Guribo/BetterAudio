using System;
using Guribo.UdonUtils.Scripts.Testing;
using UdonSharp;
using UnityEngine;

namespace Guribo.UdonBetterAudio.Scripts.Tests.ConcreteTests
{
    public class BetterAudioPlayOnEnableTest : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [HideInInspector] public TestController testController;

        public void Initialize()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.CleanUp", this);
            CleanUpTest();
        }

        #endregion

        #region EDIT HERE

        [SerializeField] private BetterAudioSource betterAudioSource;

        private bool _pendingCheck;
        private float _scheduledCheckTime;
        private float _expectedAudioTime;
        private float _maxDifference;
        private bool _secondPass;

        private void InitializeTest()
        {
            if (!betterAudioSource)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Invalid better audio source",
                    this);
                testController.TestInitialized(false);
                return;
            }

            if (!betterAudioSource.gameObject.activeInHierarchy)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Better audio source must be active in the scene",
                    this);
                testController.TestInitialized(false);
                return;
            }

            betterAudioSource.playOnEnable = true;
            betterAudioSource.playDelayedAccordingToDistance = false;

            _pendingCheck = false;
            _secondPass = false;

            testController.TestInitialized(true);
        }

        private void RunTest()
        {
            var proxyAudioSource = betterAudioSource.GetAudioSourceProxy();
            if (!proxyAudioSource)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Better audio source has no audio source proxy",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (!proxyAudioSource.loop)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Proxy audio source must have loop set to true",
                    this);
                testController.TestInitialized(false);
                return;
            }

            var actualAudioSource = betterAudioSource.GetActualAudioSource();
            if (!actualAudioSource)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Better audio source has no actual audio source and is thus not playing",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (!actualAudioSource.isPlaying)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Actual audio source is not playing",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(proxyAudioSource.minDistance - actualAudioSource.minDistance) > 0.001f)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Actual audio source has not been initialized properly from the proxy audio source",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(proxyAudioSource.maxDistance - actualAudioSource.maxDistance) > 0.001f)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Actual audio source has not been initialized properly from the proxy audio source",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(actualAudioSource.dopplerLevel) > 0.001f)
            {
                Debug.LogError(
                    $"[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] DopplerLevel is not 0, is {actualAudioSource.dopplerLevel}",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(proxyAudioSource.pitch - 1f) > 0.001f)
            {
                Debug.LogError(
                    $"[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test requires proxy audio source to have a pitch of 1, is {proxyAudioSource.pitch}",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (Mathf.Abs(proxyAudioSource.pitch - actualAudioSource.pitch) > 0.001f)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Actual audio source must have the same pitch as the proxy audio source for this test",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (proxyAudioSource.clip.length < 4f)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test requires the audio clip to be at least 4 seconds long",
                    this);
                testController.TestCompleted(false);
                return;
            }

            var waitDuration = 3f;

            _scheduledCheckTime = Time.time + waitDuration;

            actualAudioSource.time = 0f;
            _expectedAudioTime = waitDuration;
            _pendingCheck = true;
            _maxDifference = Time.deltaTime;

            Debug.Log(
                "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Waiting for delayed checks",
                this);
        }

        private void Update()
        {
            if (_pendingCheck && Time.time > _scheduledCheckTime)
            {
                _pendingCheck = false;

                var actualAudioSource = betterAudioSource.GetActualAudioSource();
                if (!actualAudioSource)
                {
                    Debug.LogError(
                        "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] No actual audio available",
                        this);
                    testController.TestCompleted(false);
                    return;
                }

                if (!actualAudioSource.isPlaying)
                {
                    Debug.LogError(
                        "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Actual audio source is not playing",
                        this);
                    testController.TestCompleted(false);
                    return;
                }

                var playedForExpectedDuration = Mathf.Abs(actualAudioSource.time - _expectedAudioTime) <
                                                _maxDifference + Time.deltaTime;
                if (!playedForExpectedDuration)
                {
                    Debug.LogError(
                        $"Actual audio didn't play for the expected duration of {_expectedAudioTime}, is {actualAudioSource.time}",
                        this);
                    testController.TestCompleted(false);
                    return;
                }

                if (_secondPass)
                {
                    testController.TestCompleted(true);
                }
                else
                {
                    // test toggling the audio source and test once more
                    betterAudioSource.gameObject.SetActive(false);
                    betterAudioSource.gameObject.SetActive(true);

                    _secondPass = true;
                    RunTest();
                }
            }
        }


        private void CleanUpTest()
        {
            _pendingCheck = false;
            testController.TestCleanedUp(true);
        }

        #endregion
    }
}