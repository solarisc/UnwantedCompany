using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using UnwantedCompany.Players;
using System.Linq;
using UnwantedCompany.Menu;
using UnwantedCompany.Render;

namespace UnwantedCompany
{
    public class MainEntry : MelonMod
    {
        public static PlayerManager _playerManager;

        private PlayerControllerHandler playerControllerHandler;

        private InteractTrigger pumpkinTrigger = null;

        private Rect windowRect = new Rect(10, 10, 200, 200);
        private Rect player_windowRect = new Rect(10, 10, 200, 200);

        private MenuClass _menu;

        public override void OnApplicationStart()
        {
            _playerManager = new PlayerManager();
            _menu = new MenuClass();
            playerControllerHandler = new PlayerControllerHandler();

            var harmony = new HarmonyLib.Harmony("com.unwantedcompany.UnwantedCompany");
            harmony.PatchAll();

            Settings.Instance.NoClipSpeed = 0.1f;
            Settings.Instance.SpeedHack = 4.6f;
            Settings.Instance.GrabDistance = 3f;
            Settings.Instance.JumpForce = 13f;
            Settings.Instance.LateJoin = true;
            Settings.Instance.ESP = true;
        }

        public override void OnLateUpdate()
        {
            if (Keyboard.current[Key.Insert].wasPressedThisFrame)
            {
                Settings.Instance.menuOpen = !Settings.Instance.menuOpen;
            }
        }

        private List<Collider> disabledColliders = new List<Collider>();

        public override void OnUpdate()
        {
            _playerManager.UpdatePlayers();

            if (Settings.Instance.SpamRPCTerminal)
            {
                var terminal = GameObject.FindObjectOfType<Terminal>();
                if (terminal != null)
                {
                    terminal.PlayTerminalAudioServerRpc(Settings.Instance.selectedAudioClip);
                }
            }

            if (Settings.Instance.CallStartGameServerRpc)
            {
                Settings.Instance.CallStartGameServerRpc = false;

                var startOfRound = GameObject.FindObjectOfType<StartOfRound>();
                if (startOfRound != null)
                {
                    startOfRound.StartGame();
                    startOfRound.StartGameServerRpc();
                }
            }

            if (Settings.Instance.CallEndGameServerRpc)
            {
                Settings.Instance.CallEndGameServerRpc = false;
                StartOfRound startOfRound = GameObject.FindObjectOfType<StartOfRound>();

                if (startOfRound != null)
                {
                    startOfRound.EndGameClientRpc(1);
                    startOfRound.EndGameServerRpc(1);
                }
            }

            if (Settings.Instance.NoClip)
            {
                if (LocalPlayer.IsValid())
                {
                    float speed = Settings.Instance.NoClipSpeed;
                    Vector3 direction = new Vector3();

                    if (Keyboard.current.wKey.isPressed)
                        direction += LocalPlayer.localPlayer.PlayerController.transform.forward;
                    if (Keyboard.current.sKey.isPressed)
                        direction -= LocalPlayer.localPlayer.PlayerController.transform.forward;
                    if (Keyboard.current.aKey.isPressed)
                        direction -= LocalPlayer.localPlayer.PlayerController.transform.right;
                    if (Keyboard.current.dKey.isPressed)
                        direction += LocalPlayer.localPlayer.PlayerController.transform.right;
                    if (Keyboard.current.spaceKey.isPressed)
                        direction += LocalPlayer.localPlayer.PlayerController.transform.up;
                    if (Keyboard.current.ctrlKey.isPressed)
                        direction -= LocalPlayer.localPlayer.PlayerController.transform.up;

                    LocalPlayer.localPlayer.PlayerController.transform.position += direction * speed;
                }
            }

            if (LocalPlayer.IsValid())
            {
                if (Settings.Instance.GodMode)
                {
                    playerControllerHandler.SetLocalPlayerHealth(100);
                }
            }

            if (Settings.Instance.PumpkinSpam)
            {
                if (pumpkinTrigger == null)
                {
                    var pumpkinContainer = UnityEngine.Object.FindObjectsOfType<GameObject>()
                        .FirstOrDefault(gameObject => gameObject.name == "PumpkinUnlockableContainer(Clone)");

                    if (pumpkinContainer != null)
                    {
                        var hitPumpkinTrigger = pumpkinContainer.transform.Find("HitPumpkinTrigger");

                        if (hitPumpkinTrigger != null)
                        {
                            pumpkinTrigger = hitPumpkinTrigger.GetComponent<InteractTrigger>();
                            if (pumpkinTrigger != null)
                            {
                                pumpkinTrigger.cooldownTime = -1f;
                            }
                        }
                    }
                }

                pumpkinTrigger?.Interact(LocalPlayer.localPlayer.PlayerController.transform);
            }

            if (Settings.Instance.NoClip)
            {
                Collider[] array = UnityEngine.Object.FindObjectsOfType<Collider>();
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] != null && array[i].enabled)
                    {
                        array[i].enabled = false;
                        disabledColliders.Add(array[i]);
                    }
                }

                Collider playerCollider = LocalPlayer.localPlayer.PlayerController.GetComponent<Collider>();
                if (playerCollider != null && playerCollider.enabled)
                {
                    playerCollider.enabled = false;
                    disabledColliders.Add(playerCollider);
                }
            }
            else
            {
                foreach (var collider in disabledColliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = true;
                    }
                }
                disabledColliders.Clear();
            }
        }

        public override void OnGUI()
        {
            if (Settings.Instance.menuOpen)
            {
                windowRect = GUILayout.Window(1, windowRect, _menu.DrawMenu, "My Menu");

                if (Settings.Instance.showPlayerMenu)
                {
                    player_windowRect = GUILayout.Window(2, player_windowRect, DrawPlayerMenu, "Player Menu");
                }
            }

            DrawESP();

            if (Settings.Instance.crosshair)
            {
                GUI.DrawTexture(new Rect(Screen.width / 2 - 5, Screen.height / 2 - 5, 5, 5), Texture2D.whiteTexture);
            }
        }

        private void DrawPlayerMenu(int windowID)
        {
            if (Settings.Instance.selectedPlayer != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    Settings.Instance.showPlayerMenu = false;
                    return;
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("Player: " + Settings.Instance.selectedPlayer.playerUsername);
                if (GUILayout.Button("Open Steam Profile ->: " + Settings.Instance.selectedPlayer.playerSteamId))
                {
                    Application.OpenURL("https://steamcommunity.com/profiles/" + Settings.Instance.selectedPlayer.playerSteamId);
                }
                if (GUILayout.Button("Kill"))
                {
                    //Vector3 vector3 = new Vector3(1f, 1f, 1f);
                    //Settings.Instance.selectedPlayer.KillPlayer(vector3, true, CauseOfDeath.Suffocation, 0);
                    Settings.Instance.selectedPlayer.DamagePlayerFromOtherClientServerRpc(1000, new Vector3(0f, 0f, 0f), 0);
                }
                if (GUILayout.Button("Heal (Host)"))
                {
                    Settings.Instance.selectedPlayer.HealServerRpc();
                }
                if (GUILayout.Button("Teleport To"))
                {
                    LocalPlayer.localPlayer.PlayerController.transform.position = Settings.Instance.selectedPlayer.transform.position;
                }
                if (GUILayout.Button("Teleport To Me (Host)"))
                {
                    Settings.Instance.selectedPlayer.transform.position = LocalPlayer.localPlayer.PlayerController.transform.position;
                }
            }
            GUI.DragWindow();
        }

        private void DrawESP()
        {
            if (!Settings.Instance.ESP) return;

            foreach (var playerController in _playerManager.GetPlayers())
            {
                if (playerController == null || playerController.health <= 0) continue;

                Vector3 playerPosition = playerController.transform.position + new Vector3(0, playerController.thisController.height / 2, 0);
                Vector3 screenPos = LocalPlayer.localPlayer.PlayerController.gameplayCamera.WorldToScreenPoint(playerPosition);

                if (screenPos.z <= 0) continue;

                float distance = Vector3.Distance(LocalPlayer.localPlayer.PlayerController.gameplayCamera.transform.position, playerController.transform.position);
                distance = (float)Math.Round(distance);

                float scaleX = Screen.width / (Screen.width / 2f);
                float screenPosX = screenPos.x * scaleX;

                Vector2 lineStart = new Vector2(Screen.width / 2f, Screen.height);
                Vector2 lineEnd = new Vector2(screenPosX + 100f, Screen.height - (screenPos.y * scaleX + 10f));
                Rendering.DrawLine(lineStart, lineEnd, Color.green, 2f);

                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;

                string labelText = $"{playerController.playerUsername} {distance}m";
                Vector2 labelSize = style.CalcSize(new GUIContent(labelText));

                Rect boxRect = new Rect(screenPosX + 75f, Screen.height - screenPos.y * scaleX - 25f, labelSize.x, labelSize.y);
                GUI.Box(boxRect, "");

                GUI.Label(boxRect, labelText, style);

            }
        }
    }
}
