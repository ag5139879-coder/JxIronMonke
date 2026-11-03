using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.4.0", "Jx")]
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
            MelonLogger.Msg("JxIronMonke 1.4.0 - better gloves");
        }

        public override void OnUpdate()
        {
            if (glovesCreated) return;

            if (Time.time < nextSearchTime) return;
            nextSearchTime = Time.time + 2f;

            TryFindHandsAndCreateGloves();
        }

        private void TryFindHandsAndCreateGloves()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null) player = GameObject.Find("LocalPlayer");
            if (player == null) player = GameObject.Find("GorillaPlayer");
            if (player == null) player = GameObject.Find("Player Objects");
            if (player == null) return;

            leftHand = FindChildRecursive(player.transform, "LeftHand");
            if (leftHand == null) leftHand = FindChildRecursive(player.transform, "hand_left");
            if (leftHand == null) leftHand = FindChildRecursive(player.transform, "Left Hand");
            if (leftHand == null) leftHand = FindChildRecursive(player.transform, "palm.01.L");

            rightHand = FindChildRecursive(player.transform, "RightHand");
            if (rightHand == null) rightHand = FindChildRecursive(player.transform, "hand_right");
            if (rightHand == null) rightHand = FindChildRecursive(player.transform, "Right Hand");
            if (rightHand == null) rightHand = FindChildRecursive(player.transform, "palm.01.R");

            if (leftHand == null || rightHand == null) return;

            MelonLogger.Msg("Creating improved gloves...");
            leftGlove = CreateGlove("LeftIronGlove", leftHand, true);
            rightGlove = CreateGlove("RightIronGlove", rightHand, false);
            glovesCreated = true;
            MelonLogger.Msg("Improved gloves created!");
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

        private GameObject CreateGlove(string name, Transform parent, bool isLeft)
        {
            // Root
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            Color armorRed = new Color(0.75f, 0.08f, 0.08f);
            Color darkRed = new Color(0.35f, 0.05f, 0.05f);
            Color gold = new Color(1f, 0.85f, 0.15f);
            Color brightGold = new Color(1f, 0.95f, 0.4f);

            // === Palm plate ===
            GameObject palm = MakePart(root.transform, "Palm", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.02f),
                Quaternion.identity,
                new Vector3(0.095f, 0.04f, 0.11f),
                armorRed);

            // === Back of hand (slightly raised) ===
            MakePart(root.transform, "BackPlate", PrimitiveType.Cube,
                new Vector3(0f, 0.025f, 0.01f),
                Quaternion.identity,
                new Vector3(0.085f, 0.025f, 0.09f),
                darkRed);

            // === Wrist / cuff ===
            MakePart(root.transform, "Wrist", PrimitiveType.Cube,
                new Vector3(0f, 0f, -0.07f),
                Quaternion.identity,
                new Vector3(0.08f, 0.055f, 0.05f),
                armorRed);

            // Gold ring on wrist
            MakePart(root.transform, "WristRing", PrimitiveType.Cylinder,
                new Vector3(0f, 0f, -0.05f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.09f, 0.015f, 0.09f),
                gold);

            // === Knuckle plate ===
            MakePart(root.transform, "Knuckles", PrimitiveType.Cube,
                new Vector3(0f, 0.02f, 0.07f),
                Quaternion.identity,
                new Vector3(0.09f, 0.03f, 0.04f),
                darkRed);

            // === Main thruster pad (palm) - bright and visible ===
            GameObject thruster = MakePart(root.transform, "ThrusterPad", PrimitiveType.Cylinder,
                new Vector3(0f, -0.028f, 0.025f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.07f, 0.012f, 0.07f),
                brightGold);

            // Inner glow of thruster
            MakePart(thruster.transform, "ThrusterCore", PrimitiveType.Cylinder,
                new Vector3(0f, 0f, 0f),
                Quaternion.identity,
                new Vector3(0.55f, 1.2f, 0.55f),
                new Color(1f, 1f, 0.6f));

            // === Small finger armor pieces ===
            float[] fingerX = { -0.032f, -0.011f, 0.011f, 0.032f };
            for (int i = 0; i < 4; i++)
            {
                MakePart(root.transform, "Finger" + i, PrimitiveType.Cube,
                    new Vector3(fingerX[i], 0.01f, 0.095f),
                    Quaternion.identity,
                    new Vector3(0.018f, 0.025f, 0.045f),
                    armorRed);
            }

            // Thumb
            float thumbSide = isLeft ? 0.05f : -0.05f;
            MakePart(root.transform, "Thumb", PrimitiveType.Cube,
                new Vector3(thumbSide, 0f, 0.03f),
                Quaternion.Euler(0f, 0f, isLeft ? -35f : 35f),
                new Vector3(0.025f, 0.03f, 0.05f),
                armorRed);

            return root;
        }

        private GameObject MakePart(Transform parent, string name, PrimitiveType type,
            Vector3 localPos, Quaternion localRot, Vector3 localScale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            // Remove collider so it doesn't push things
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = color;
            }

            return go;
        }
    }
}
