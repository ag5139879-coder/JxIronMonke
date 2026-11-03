using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.5.1", "Jx")]
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

        private Rigidbody playerRb;
        private float thrustForce = 16f;

        // Simple toggle: thrust is on when palm faces mostly downward (common Iron Man pose)
        // Later we can wire real controller grips when we have the right input module

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("JxIronMonke 1.5.1 - gloves + orientation thrust");
        }

        public override void OnUpdate()
        {
            if (!glovesCreated)
            {
                if (Time.time >= nextSearchTime)
                {
                    nextSearchTime = Time.time + 1.5f;
                    TryFindHandsAndCreateGloves();
                }
                return;
            }

            SyncGlove(leftGlove, leftHand);
            SyncGlove(rightGlove, rightHand);

            bool leftThrust = IsPalmDown(leftHand);
            bool rightThrust = IsPalmDown(rightHand);

            ApplyThrust(leftHand, leftThrust);
            ApplyThrust(rightHand, rightThrust);

            SetThrusterGlow(leftGlove, leftThrust);
            SetThrusterGlow(rightGlove, rightThrust);
        }

        private bool IsPalmDown(Transform hand)
        {
            if (hand == null) return false;
            // Palm roughly facing world down
            return Vector3.Dot(hand.up, Vector3.down) > 0.4f
                || Vector3.Dot(-hand.up, Vector3.down) > 0.4f;
        }

        private void SyncGlove(GameObject glove, Transform hand)
        {
            if (glove == null || hand == null) return;
            glove.transform.position = hand.position;
            glove.transform.rotation = hand.rotation;
        }

        private void ApplyThrust(Transform hand, bool active)
        {
            if (!active || hand == null || playerRb == null) return;
            Vector3 dir = -hand.up;
            playerRb.AddForce(dir * thrustForce, ForceMode.Acceleration);
        }

        private void SetThrusterGlow(GameObject glove, bool active)
        {
            if (glove == null) return;
            Transform pad = glove.transform.Find("ThrusterPad");
            if (pad == null) return;
            Renderer r = pad.GetComponent<Renderer>();
            if (r == null) return;
            r.material.color = active ? new Color(1f, 0.95f, 0.35f) : new Color(1f, 0.75f, 0.1f);
        }

        private void TryFindHandsAndCreateGloves()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null) player = GameObject.Find("LocalPlayer");
            if (player == null) player = GameObject.Find("GorillaPlayer");
            if (player == null) player = GameObject.Find("Player Objects");
            if (player == null) return;

            playerRb = player.GetComponent<Rigidbody>();
            if (playerRb == null) playerRb = player.GetComponentInChildren<Rigidbody>();
            if (playerRb == null) playerRb = player.GetComponentInParent<Rigidbody>();

            leftHand = FindChildRecursive(player.transform, "LeftHand");
            if (leftHand == null) leftHand = FindChildRecursive(player.transform, "hand_left");
            if (leftHand == null) leftHand = FindChildRecursive(player.transform, "Left Hand");
            if (leftHand == null) leftHand = FindChildRecursive(player.transform, "palm.01.L");

            rightHand = FindChildRecursive(player.transform, "RightHand");
            if (rightHand == null) rightHand = FindChildRecursive(player.transform, "hand_right");
            if (rightHand == null) rightHand = FindChildRecursive(player.transform, "Right Hand");
            if (rightHand == null) rightHand = FindChildRecursive(player.transform, "palm.01.R");

            if (leftHand == null || rightHand == null) return;

            leftGlove = CreateGlove("LeftIronGlove", leftHand);
            rightGlove = CreateGlove("RightIronGlove", rightHand);
            glovesCreated = true;
            MelonLogger.Msg("Gloves ready. Flight RB=" + (playerRb != null));
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

        private GameObject CreateGlove(string name, Transform hand)
        {
            GameObject root = new GameObject(name);
            root.transform.position = hand.position;
            root.transform.rotation = hand.rotation;

            Color red = new Color(0.8f, 0.1f, 0.1f);
            Color dark = new Color(0.25f, 0.05f, 0.05f);
            Color gold = new Color(1f, 0.8f, 0.12f);

            MakePart(root.transform, "Palm", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.03f), Quaternion.identity,
                new Vector3(0.07f, 0.02f, 0.09f), red);

            MakePart(root.transform, "Back", PrimitiveType.Cube,
                new Vector3(0f, 0.018f, 0.02f), Quaternion.identity,
                new Vector3(0.065f, 0.015f, 0.07f), dark);

            MakePart(root.transform, "Wrist", PrimitiveType.Cube,
                new Vector3(0f, 0f, -0.04f), Quaternion.identity,
                new Vector3(0.06f, 0.035f, 0.035f), red);

            MakePart(root.transform, "ThrusterPad", PrimitiveType.Cylinder,
                new Vector3(0f, -0.018f, 0.03f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.05f, 0.008f, 0.05f), gold);

            return root;
        }

        private GameObject MakePart(Transform parent, string partName, PrimitiveType type,
            Vector3 pos, Quaternion rot, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = partName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;

            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;

            return go;
        }
    }
}
