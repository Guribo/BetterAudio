using Guribo.UdonUtils.Scripts.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts.Tests.ConcreteTests
{
    public class BetterAudioSonicBoomTest : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [HideInInspector] public TestController testController;

        public void Initialize()
        {
            if (!testController)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller", this);
                return;
            }
            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!testController)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller", this);
                return;
            }

            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!testController)
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller", this);
                return;
            }
            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.CleanUp", this);
            CleanUpTest();
        }

        #endregion

        #region EDIT HERE

        public BetterAudioSource audioSource;
        
        private readonly Vector3 _startOffset = Vector3.forward * 1000; 
        private void InitializeTest()
        {
            // TODO your init behaviour here
            // ...

            if (!Utilities.IsValid(audioSource))
            {
                Debug.LogError("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid BetterAudioSource", this);
                testController.TestInitialized(false);
            }
            
            audioSource.Stop();
            audioSource.gameObject.transform.position = transform.position + _startOffset;

            // whenever the test is ready to be started call betterAudioTestController.TestInitialized,
            // can be later in update or whenever but MUST be called at some point
            testController.TestInitialized(true);
        }

        private void RunTest()
        {
            // TODO your test behaviour here
            // ...

            // whenever the test is completed call betterAudioTestController.TestCompleted,
            // can be later in update or whenever but MUST be called at some point
            testController.TestCompleted(true);
        }

        private void CleanUpTest()
        {
            // TODO your clean up behaviour here
            // ...

            // whenever the test is cleaned up call betterAudioTestController.TestCleanedUp,
            // can be later in update or whenever but MUST be called at some point
            testController.TestCleanedUp(true);
        }

        #endregion
    }
}
