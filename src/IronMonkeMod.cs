using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.1.0", "Jx + Community")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    public class IronMonkeMod : MelonMod
    {
        private Transform leftHand;
        private Transform rightHand;
        private Transform playerHead;

        private GameObject leftGlove;
        private GameObject rightGlove;
        private GameObject hudRoot;

        private bool isThrusting;
        private float currentSpeed;
        private bool initialized;

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("JxIronMonke loaded");
        }

        public override void OnUpdate()
        {
            if (!initialized)
            {
                FindPlayerTransforms();
                if (leftHand != null && rightHand != null)
                {
                    CreateGloves();
                    CreateSimpleHUD();
                    initialized = true;
                    MelonLogger.Msg("JxIronMonke initialized - gloves + HUD ready");
                }
                return;
            }

            // Temporary test thrust (replace with real input later)
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

            rightHand = FindDeepChild(player.transform, "RightHand");
            if (rightHand == null) rightHand = FindDeepChild(player.transform, "hand_right");

            playerHead = FindDeepChild(player.transform, "Head");
            if (playerHead == null) playerHead = FindDeepChild(player.transform, "Camera");
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name.ToLower().Contains(name.ToLower()))
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindDeepChild(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private void CreateGloves()
        {
            leftGlove = CreateGlove("LeftIronGlove", leftHand);
            rightGlove = CreateGlove("RightIronGlove", rightHand);
            MelonLogger.Msg("Gloves created");
        }

        private GameObject CreateGlove(string name, Transform parent)
        {
            // Simple red cube as glove placeholder
            GameObject glove = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glove.name = name;
            glove.transform.SetParent(parent, false);
            glove.transform.localPosition = Vector3.zero;
            glove.transform.localRotation = Quaternion.identity;
            glove.transform.localScale = new Vector3(0.09f, 0.12f, 0.09f);

            // Try to color it red (may fail on some IL2CPP setups, that's ok)
            try
            {
                var rend = glove.GetComponent<Renderer>();
                if (rend != null && rend.material != null)
                    rend.material.color = new Color(0.85f, 0.1f, 0.1f);
            }
            catch { }

            // Yellow thruster pad
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "ThrusterPad";
            pad.transform.SetParent(glove.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pad.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);

            try
            {
                var rend = pad.GetComponent<Renderer>();
                if (rend != null && rend.material != null)
                    rend.material.color = Color.yellow;
            }
            catch { }

            return glove;
        }

        private void UpdateGloves()
        {
            if (leftGlove == null || rightGlove == null) return;

            Color c = isThrusting ? new Color(1f, 0.95f, 0.2f) : new Color(0.7f, 0.55f, 0.1f);
            SetPadColor(leftGlove, c);
            SetPadColor(rightGlove, c);
        }

        private void SetPadColor(GameObject glove, Color c)
        {
            Transform pad = glove.transform.Find("ThrusterPad");
            if (pad == null) return;
            try
            {
                var rend = pad.GetComponent<Renderer>();
                if (rend != null && rend.material != null)
                    rend.material.color = c;
            }
            catch { }
        }

        // Very simple HUD using only a floating text object (no Unity UI)
        private void CreateSimpleHUD()
        {
            hudRoot = new GameObject("IronManHUD");
            // Just an empty object we will move in front of the player.
            // Real text UI needs extra Unity modules the current DLLs don't provide.
            MelonLogger.Msg("Simple HUD placeholder created (full UI needs more DLLs)");
        }

        private void UpdateHUD()
        {
            if (hudRoot == null || playerHead == null) return;

            // Keep a marker in front of the player so we know the system is alive
            hudRoot.transform.position = playerHead.position + playerHead.forward * 0.5f + playerHead.up * 0.1f;
            hudRoot.transform.rotation = playerHead.rotation;
        }

        public override void OnApplicationQuit()
        {
            if (hudRoot != null) Object.Destroy(hudRoot);
            if (leftGlove != null) Object.Destroy(leftGlove);
            if (rightGlove != null) Object.Destroy(rightGlove);
        }
    }
}
