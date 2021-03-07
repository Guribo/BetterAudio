using System;
using System.IO;
using System.Security.Cryptography;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts
{
    public class Tester : UdonSharpBehaviour
    {
        public GameObject[] elems;
        public BetterAudioSource[] audios;
        private readonly int maxRotationSpeed = 30;

        private bool increasing;
        [UdonSynced()] public int rotationSpeed = 2;


        private float startTime = 1000f;

        private void Update()
        {
            if (elems != null && Input.GetKeyDown(KeyCode.T))
            {
                foreach (var elem in elems)
                {
                    if (elem)
                    {
                        elem.SetActive(!elem.activeSelf);
                    }
                }
                
                foreach (var elem in audios)
                {
                    if (elem)
                    {
                        elem.Stop();
                        elem.Play(true);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
                if (increasing && rotationSpeed >= maxRotationSpeed)
                {
                    increasing = !increasing;
                }

                if (!increasing && rotationSpeed <= 0)
                {
                    increasing = !increasing;
                }

                rotationSpeed = increasing ? rotationSpeed + 1 : rotationSpeed - 1;
            }
        }

        private void FixedUpdate()
        {
            transform.Rotate(0, Time.fixedDeltaTime * rotationSpeed, 0);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
            {
                player.SetJumpImpulse(3f);
                player.SetRunSpeed(10f);
                startTime = Time.time + 10f;
            }
        }
    }
}