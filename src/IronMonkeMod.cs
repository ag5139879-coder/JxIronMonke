using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.1.0", "Jx + Community")]
[assembly: MelonGame(null, null)] // Works with com.JxStudios.Jxmoddinggame

namespace IronMonke
{
    public class IronMonkeMod : MelonMod
    {
        // ---- Configuration ----
        public static bool EnableHUD = true;
        public static bool EnableCustomGloves = true;

        // Hand references (filled at runtime)
        private Transform leftHand;
        private Transform rightHand;
        private Transform playerHead;

        // Visuals
        private GameObject leftGlove;
        private GameObject rightGlove;

        // HUD
        private GameObject hudRoot;
        private Text speedText;
        private Text statusText;
        private Text chargeText;

        // State
        private bool isThrusting;
        private float currentSpeed;

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("JxIronMonke loaded - HUD + improved gloves ready");
        }

        public override void OnUpdate()
        {
            // Try to find player hands/head if not yet found
            if (leftHand == null || rightHand == null)
            {
                FindPlayerTransforms();
            }

            if (leftHand == null) return;

            // Update glove positions (they stay parented)
            UpdateGloves();

            // Simple thrust detection (you already have real logic in original mod)
            // This is a placeholder - replace with your real thrust input check
            isThrusting = Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.JoystickButton5);

            // Rough speed calculation (placeholder)
            if (playerHead != null)
            {
                // In a real mod you would track velocity properly
                currentSpeed = isThrusting ? 12.5f : 0f;
            }

            UpdateHUD();
        }

        private void FindPlayerTransforms()
        {
            // These paths are common in Gorilla Tag style games.
            // You may need to adjust them for Jxmoddinggame.
            // Use a hierarchy viewer or MelonLoader log to find the correct names.

            GameObject player = GameObject.Find("Player") ?? GameObject.Find("LocalPlayer") ?? GameObject.Find("GorillaPlayer");
            if (player == null) return;

            // Common hand names - change these if needed
            leftHand = FindDeepChild(player.transform, "LeftHand") 
                    ?? FindDeepChild(player.transform, "hand_left")
                    ?? FindDeepChild(player.transform, "Left Hand");

            rightHand = FindDeepChild(player.transform, "RightHand")
                     ?? FindDeepChild(player.transform, "hand_right")
                     ?? FindDeepChild(player.transform, "Right Hand");

            playerHead = FindDeepChild(player.transform, "Head")
                      ?? FindDeepChild(player.transform, "Camera")
                      ?? FindDeepChild(player.transform, "Main Camera");

            if (leftHand != null && rightHand != null)
            {
                MelonLogger.Msg("Found player hands");
                if (EnableCustomGloves)
                    CreateGloves();

                if (EnableHUD)
                    CreateHUD();
            }
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name.ToLower().Contains(name.ToLower())) return parent;

            foreach (Transform child in parent)
            {
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        // -------------------- GLOVES / SUIT VISUALS --------------------

        private void CreateGloves()
        {
            // Simple procedural gloves for now (no external models required)
            // Later you can replace these with real FBX models loaded from Assets

            leftGlove = CreateSimpleGlove("LeftIronGlove", leftHand);
            rightGlove = CreateSimpleGlove("RightIronGlove", rightHand);

            MelonLogger.Msg("Created improved glove visuals");
        }

        private GameObject CreateSimpleGlove(string name, Transform parent)
        {
            GameObject glove = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glove.name = name;
            glove.transform.SetParent(parent);
            glove.transform.localPosition = Vector3.zero;
            glove.transform.localRotation = Quaternion.identity;
            glove.transform.localScale = new Vector3(0.09f, 0.12f, 0.09f);

            // Red Iron Man color
            var renderer = glove.GetComponent<Renderer>();
            renderer.material.color = new Color(0.8f, 0.1f, 0.1f);

            // Yellow thruster pad
            GameObject thruster = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thruster.name = "ThrusterPad";
            thruster.transform.SetParent(glove.transform);
            thruster.transform.localPosition = new Vector3(0, 0, 0.06f);
            thruster.transform.localRotation = Quaternion.Euler(90, 0, 0);
            thruster.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);

            var thrusterRend = thruster.GetComponent<Renderer>();
            thrusterRend.material.color = Color.yellow;

            // Remove colliders so they don't interfere
            Object.Destroy(glove.GetComponent<Collider>());
            Object.Destroy(thruster.GetComponent<Collider>());

            return glove;
        }

        private void UpdateGloves()
        {
            if (leftGlove == null || rightGlove == null) return;

            // Make thruster pads brighter while thrusting
            Color thrusterColor = isThrusting ? new Color(1f, 0.9f, 0.2f) : new Color(0.7f, 0.6f, 0.1f);

            foreach (var glove in new[] { leftGlove, rightGlove })
            {
                var pad = glove.transform.Find("ThrusterPad");
                if (pad != null)
                {
                    pad.GetComponent<Renderer>().material.color = thrusterColor;
                }
            }
        }

        // -------------------- HUD --------------------

        private void CreateHUD()
        {
            hudRoot = new GameObject("IronManHUD");
            var canvas = hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // Make it small and readable in VR
            hudRoot.transform.localScale = Vector3.one * 0.0012f;

            // Background panel
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(hudRoot.transform);
            panel.transform.localPosition = Vector3.zero;
            var img = panel.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);

            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 220);

            // Speed text
            speedText = CreateHUDText(panel.transform, "SpeedText", new Vector2(0, 60), 42);
            statusText = CreateHUDText(panel.transform, "StatusText", new Vector2(0, 0), 36);
            chargeText = CreateHUDText(panel.transform, "ChargeText", new Vector2(0, -55), 32);

            MelonLogger.Msg("HUD created");
        }

        private Text CreateHUDText(Transform parent, string name, Vector2 position, int fontSize)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(position.x, position.y, 0);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.color = Color.cyan;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(580, 50);

            return text;
        }

        private void UpdateHUD()
        {
            if (hudRoot == null || playerHead == null) return;

            // Keep HUD in front of the player
            hudRoot.transform.position = playerHead.position + playerHead.forward * 0.45f + playerHead.up * 0.08f;
            hudRoot.transform.rotation = Quaternion.LookRotation(hudRoot.transform.position - playerHead.position);

            // Update values
            if (speedText != null)
                speedText.text = $"SPD  {currentSpeed:F1}";

            if (statusText != null)
            {
                statusText.text = isThrusting ? "THRUSTERS ONLINE" : "STANDBY";
                statusText.color = isThrusting ? Color.green : Color.cyan;
            }

            if (chargeText != null)
                chargeText.text = isThrusting ? "ENERGY ████████░░" : "ENERGY ██████████";
        }

        public override void OnApplicationQuit()
        {
            if (hudRoot != null) Object.Destroy(hudRoot);
            if (leftGlove != null) Object.Destroy(leftGlove);
            if (rightGlove != null) Object.Destroy(rightGlove);
        }
    }
}
