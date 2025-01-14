using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using System.Drawing.Drawing2D;


namespace SightJacking
{
    public class Sightjacking : Script
    {
        private Camera jackCam;
        Vector3 pedPosition;
        Vector3 pedRotation;
        Ped playerPed = Game.Player.Character;
        private Blip jackedPedBlip;

        //ini file
        ScriptSettings config;
        Keys switchKey;

        public Sightjacking()
        {
            this.Tick += onTick;
            this.KeyDown += onKeyDown;

            //add an ini
            config = ScriptSettings.Load("scripts/Sightjacking.ini");
            switchKey = config.GetValue<Keys>("INPUT", "ModSwitch", Keys.J);
        }

        private void onTick(object sender, EventArgs e)
        {
            // Check if player is alive and the sightjcking camera is on
            if (!playerPed.IsDead && jackCam != null)
            {
                try
                {
                    // Get the player's position
                    Vector3 playerPosition = playerPed.Position;

                    // Find and store the closest Ped
                    Ped closestPed = GetClosestPed(playerPosition);
                    
                    // Set the ped variables to the closest ped's position and rotation
                    pedPosition = closestPed.Position;
                    pedRotation = closestPed.Rotation;

                    // Display the position of the closest NPC
                    //GTA.UI.Screen.ShowHelpText("Closest NPC Position: " + pedPosition, 10, false, false);

                    if (!closestPed.IsHuman) //if the NPC is an animal
                    {
                        // If ped is non-human: Attach the jackCam's position to the head's (skeletton) coordinates and and the ped's rotation
                        Function.Call(Hash.ATTACH_CAM_TO_PED_BONE, jackCam, closestPed, 31086, 0f, 0.1f, 0f, true);
                        jackCam.Rotation = pedRotation;
                    }
                    else //if the NPC is human
                    {
                        // If ped is human: Attach the jackCam's position to the head's (model) coordinates and and the ped's rotation
                        Function.Call(Hash.ATTACH_CAM_TO_PED_BONE, jackCam, closestPed, 12844, 0f, 0.1f, 0f, true);
                        jackCam.Rotation = pedRotation;

                        // Alternative that includes head rotation. Leads to less clipping but more "bobbing" in the camera movement.
                        //Function.Call(Hash.HARD_ATTACH_CAM_TO_PED_BONE, jackCam, closestPed, 12844, 0f, 90f, 0f, 0f, 0.1f, 0f, true);
                    }

                    // Show a blip at the position of the currently sigthjacked ped
                    if (jackedPedBlip == null || !jackedPedBlip.Exists())
                    {
                        // Create a new blip 
                        jackedPedBlip = World.CreateBlip(pedPosition);
                        jackedPedBlip.Sprite = BlipSprite.Standard; // Set blip icon
                        jackedPedBlip.Color = BlipColor.Green;      // Set blip color
                        jackedPedBlip.Name = "Sigthjacked Ped";       // Set blip name
                    }

                    // Update the blip's position every frame
                    jackedPedBlip.Position = pedPosition;
                }

                catch (Exception ex)
                {
                    if (jackedPedBlip != null) jackedPedBlip.Delete();
                }
            }

            else
            {
                if (jackedPedBlip != null) jackedPedBlip.Delete();
            }
        }
        private void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == switchKey)
            {
                if (jackCam == null) //turn on the sightjacking camera
                {
                    jackCam = World.CreateCamera(playerPed.Position, playerPed.Rotation, 50f);
                    jackCam.IsActive = true;
                    World.RenderingCamera = jackCam;
                }
                else //turn off the sightjacking camera
                {
                    World.RenderingCamera = null;
                    jackCam.Delete();
                    jackCam = null;
                    if (jackedPedBlip != null) jackedPedBlip.Delete();
                }
            }
        }
        private Ped GetClosestPed(Vector3 playerPosition)
        {
            // Get all the peds in the game world
            var allPeds = World.GetAllPeds();

            // Find the closest ped by calculating the distance to each
            Ped closestPed = null;
            float closestDistance = float.MaxValue;

            foreach (var ped in allPeds)
            {
                // Skip if the ped is dead, not valid or the player character
                if (ped.IsDead || !ped.Exists() || ped == playerPed) continue;

                // Calculate the distance from the player to the NPC
                float distance = playerPosition.DistanceTo(ped.Position);

                // Update the closest NPC if we found a closer one
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPed = ped;
                }
            }

            return closestPed;
        }
    }
}