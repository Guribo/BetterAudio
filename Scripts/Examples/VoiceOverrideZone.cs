using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.UdonBetterAudio.Scripts.Examples
{
    public class VoiceOverrideZone : UdonSharpBehaviour
    {
        public BetterPlayerAudioOverride playerAudioOverride;
        public BetterPlayerAudio playerAudio;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) // do nothing for invalid players
                || Networking.LocalPlayer == player // or when the player was the local player
                || !Utilities.IsValid(playerAudioOverride) // or when the required components are invalid
                || !Utilities.IsValid(playerAudio))
            {
                return;
            }

            // add the player to the override
            playerAudioOverride.AffectPlayer(player);
            
            // have the controller affect all players that are currently added to the override
            playerAudio.OverridePlayerSettings(playerAudioOverride);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) // do nothing for invalid players
                || Networking.LocalPlayer == player // or when the player was the local player
                || !Utilities.IsValid(playerAudioOverride) // or when the required components are invalid
                || !Utilities.IsValid(playerAudio))
            {
                return;
            }

            // remove player from the override
            playerAudioOverride.RemoveAffectedPlayer(player);
            
            // make the controller apply default settings to the player again
            playerAudio.ClearPlayerOverride(player.playerId);
        }
    }
}
