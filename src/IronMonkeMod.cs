using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.3.0", "Jx")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    public class IronMonkeMod : MelonMod
    {
        private Transform leftHand;
        private Transform rightHand;
        private GameObject leftGlove;
        private GameObject rightGlove;
        private bool glovesCreated;
        private float nextSearchTime;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("JxIronMonke 1.3.0 started - searching for hands...");
        }

        public override void OnUpdate()
        {
            if (glovesCreated) return;

            // Only search every 2 seconds so we don't spam
            if (Time.time < nextSearchTime) return;
            nextSearchTime = Time.time + 2f;

            TryFindHandsAndCreateGloves();
        }

        private void TryFindHandsAndCreateGloves()
        {
            // Try common player object names
            GameObject player = GameObject.Find("Player");
            if (player == null) player = GameObject.Find("LocalPlayer");
            if (player == null) player = GameObject.Find("GorillaPlayer");
            if (player == null) player = GameObject.Find("Player Objects");
            if (player == null)
            {
                MelonLogger.Msg("Player not found yet...");
                return;
            }

            MelonLogger.Msg("Found player object: " + player.name);

            leftHand = FindChildRecursive(player.transform, "LeftHand");
            if (leftHand == null) leftHand = FindChildRecursive(player.transform, "hand_left");
            if (leftHand == null) leftHand = FindChildRecursive(player.transform, "Left Hand");
            if (leftHand == null) leftHand = FindChildRecursive(player.transform, "palm.01.L");

            rightHand = FindChildRecursive(player.transform, "RightHand");
            if (rightHand == null) rightHand = FindChildRecursive(player.transform, "hand_right");
            if (rightHand == null) rightHand = FindChildRecursive(player.transform, "Right Hand");
            if (rightHand == null) rightHand = FindChildRecursive(player.transform, "palm.01.R");

            if (leftHand == null || rightHand == null)
            {
                MelonLogger.Msg("Hands not found. Left=" + (leftHand != null) + " Right=" + (rightHand != null));
                // Log some child names to help us find the real ones
                LogChildren(player.transform, 0, 3);
                return;
            }

            MelonLogger.Msg("Hands found! Creating gloves...");
            leftGlove = CreateGlove("LeftIronGlove", leftHand);
            rightGlove = CreateGlove("RightIronGlove", rightHand);
            glovesCreated = true;
            MelonLogger.Msg("Gloves created successfully!");
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindChildRecursive(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private void LogChildren(Transform t, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;
            string indent = new string(' ', depth * 2);
            MelonLogger.Msg(indent + t.name);
            for (int i = 0; i < t.childCount && i < 12; i++)
                LogChildren(t.GetChild(i), depth + 1, maxDepth);
        }

        private GameObject CreateGlove(string name, Transform parent)
        {
            GameObject glove = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glove.name = name;
            glove.transform.SetParent(parent, false);
            glove.transform.localPosition = Vector3.zero;
            glove.transform.localRotation = Quaternion.identity;
            glove.transform.localScale = new Vector3(0.08f, 0.11f, 0.08f);

            // Red color
            Renderer rend = glove.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.85f, 0.12f, 0.12f);
            }

            // Yellow thruster pad
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "ThrusterPad";
            pad.transform.SetParent(glove.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pad.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);

            Renderer padRend = pad.GetComponent<Renderer>();
            if (padRend != null)
            {
                padRend.material.color = Color.yellow;
            }

            return glove;
        }
    }
}
