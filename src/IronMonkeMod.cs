using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.1.0", "Jx + Community")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    public class IronMonkeMod : MelonMod
    {
        public static bool EnableHUD = true;
        public static bool EnableCustomGloves = true;

        private Transform leftHand;
        private Transform rightHand;
        private Transform playerHead;

        private GameObject leftGlove;
        private GameObject rightGlove;

        private GameObject hudRoot;
        private Text speedText;
        private Text statusText;
        private Text chargeText;

        private bool isThrusting;
        private float currentSpeed;
        private bool initialized;

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("JxIronMonke loaded - HUD + improved gloves ready");
        }

        public override void OnUpdate()
        {
            if (!initialized)
            {
                FindPlayerTransforms();
                if (leftHand != null && rightHand != null)
                {
                    if (EnableCustomGloves) CreateGloves();
                    if (EnableHUD) CreateHUD();
                    initialized = true;
                    MelonLogger.Msg("JxIronMonke fully initialized");
                }
                return;
            }

            // Placeholder thrust detection - replace with real input later
            // Using a simple time-based toggle for testing if Input is problematic
            isThrusting = (Time.time % 4f) < 2f;

            currentSpeed = isThrusting ? 12.5f : 0f;

            UpdateGloves();
            UpdateHUD();
        }

        private void FindPlayerTransforms()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null) player = GameObject.Find("LocalPlayer");
            if (player == null) player = GameObject.Find("GorillaPlayer");
            if (player == null) return;

            leftHand = FindDeepChild(player.transform, "LeftHand");
            if (leftHand == null) leftHand = FindDeepChild(player.transform, "hand_left");
            if (leftHand == null) leftHand = FindDeepChild(player.transform, "Left Hand");

            rightHand = FindDeepChild(player.transform, "RightHand");
            if (rightHand == null) rightHand = FindDeepChild(player.transform, "hand_right");
            if (rightHand == null) rightHand = FindDeepChild(player.transform, "Right Hand");

            playerHead = FindDeepChild(player.transform, "Head");
            if (playerHead == null) playerHead = FindDeepChild(player.transform, "Camera");
            if (playerHead == null) playerHead = FindDeepChild(player.transform, "Main Camera");
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name.ToLower().Contains(name.ToLower())) return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindDeepChild(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private void CreateGloves()
        {
            leftGlove = CreateSimpleGlove("LeftIronGlove", leftHand);
            rightGlove = CreateSimpleGlove("RightIronGlove", rightHand);
            MelonLogger.Msg("Created improved glove visuals");
        }

        private GameObject CreateSimpleGlove(string name, Transform parent)
        {
            GameObject glove = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glove.name = name;
            glove.transform.SetParent(parent, false);
            glove.transform.localPosition = Vector3.zero;
            glove.transform.localRotation = Quaternion.identity;
            glove.transform.localScale = new Vector3(0.09f, 0.12f, 0.09f);

            Renderer rend = glove.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = new Color(0.8f, 0.1f, 0.1f);

            GameObject thruster = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            thruster.name = "ThrusterPad";
            thruster.transform.SetParent(glove.transform, false);
            thruster.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            thruster.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            thruster.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);

            Renderer thrusterRend = thruster.GetComponent<Renderer>();
            if (thrusterRend != null)
                thrusterRend.material.color = Color.yellow;

            Collider col1 = glove.GetComponent<Collider>();
            if (col1 != null) Object.Destroy(col1);
            Collider col2 = thruster.GetComponent<Collider>();
            if (col2 != null) Object.Destroy(col2);

            return glove;
        }

        private void UpdateGloves()
        {
            if (leftGlove == null || rightGlove == null) return;

            Color thrusterColor = isThrusting ? new Color(1f, 0.9f, 0.2f) : new Color(0.7f, 0.6f, 0.1f);

            UpdateThrusterColor(leftGlove, thrusterColor);
            UpdateThrusterColor(rightGlove, thrusterColor);
        }

        private void UpdateThrusterColor(GameObject glove, Color color)
        {
            Transform pad = glove.transform.Find("ThrusterPad");
            if (pad == null) return;
            Renderer r = pad.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
        }

        private void CreateHUD()
        {
            hudRoot = new GameObject("IronManHUD");
            Canvas canvas = hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            hudRoot.transform.localScale = Vector3.one * 0.0012f;

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(hudRoot.transform, false);

            Image img = panel.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.55f);

            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = new Vector2(600f, 220f);

            speedText = CreateHUDText(panel.transform, "SpeedText", new Vector2(0f, 60f), 42);
            statusText = CreateHUDText(panel.transform, "StatusText", new Vector2(0f, 0f), 36);
            chargeText = CreateHUDText(panel.transform, "ChargeText", new Vector2(0f, -55f), 32);

            MelonLogger.Msg("HUD created");
        }

        private Text CreateHUDText(Transform parent, string name, Vector2 position, int fontSize)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, 0f);

            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.color = Color.cyan;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = new Vector2(580f, 50f);

            return text;
        }

        private void UpdateHUD()
        {
            if (hudRoot == null || playerHead == null) return;

            hudRoot.transform.position = playerHead.position + playerHead.forward * 0.45f + playerHead.up * 0.08f;
            hudRoot.transform.rotation = Quaternion.LookRotation(hudRoot.transform.position - playerHead.position);

            if (speedText != null)
                speedText.text = "SPD  " + currentSpeed.ToString("F1");

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
