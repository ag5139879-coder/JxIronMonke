using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.6.0", "Jx")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    public class IronMonkeMod : MelonMod
    {
        private Transform leftHand;
        private Transform rightHand;
        private Transform head;

        private GameObject leftGlove;
        private GameObject rightGlove;
        private GameObject leftBeam;
        private GameObject rightBeam;

        private bool glovesCreated;
        private float nextSearchTime;

        private Rigidbody playerRb;
        private float thrustForce = 35f;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("JxIronMonke 1.6.0 - big gloves + repulsor + flight");
        }

        public override void OnUpdate()
        {
            if (!glovesCreated)
            {
                if (Time.time >= nextSearchTime)
                {
                    nextSearchTime = Time.time + 1.2f;
                    TrySetup();
                }
                return;
            }

            // Force gloves to match hands every frame
            if (leftHand != null && leftGlove != null)
            {
                leftGlove.transform.SetPositionAndRotation(leftHand.position, leftHand.rotation);
            }
            if (rightHand != null && rightGlove != null)
            {
                rightGlove.transform.SetPositionAndRotation(rightHand.position, rightHand.rotation);
            }

            bool leftOn = IsThrusting(leftHand);
            bool rightOn = IsThrusting(rightHand);

            UpdateBeam(leftBeam, leftHand, leftOn);
            UpdateBeam(rightBeam, rightHand, rightOn);

            SetPadColor(leftGlove, leftOn);
            SetPadColor(rightGlove, rightOn);

            if (playerRb != null)
            {
                if (leftOn) playerRb.AddForce(-leftHand.up * thrustForce, ForceMode.Acceleration);
                if (rightOn) playerRb.AddForce(-rightHand.up * thrustForce, ForceMode.Acceleration);

                // Extra help if mostly upward
                if (leftOn || rightOn)
                {
                    playerRb.AddForce(Vector3.up * 4f, ForceMode.Acceleration);
                }
            }
        }

        private bool IsThrusting(Transform hand)
        {
            if (hand == null) return false;
            // Palm facing mostly down OR away from body in a "push" pose
            float down = Mathf.Max(
                Vector3.Dot(hand.up, Vector3.down),
                Vector3.Dot(-hand.up, Vector3.down),
                Vector3.Dot(hand.forward, Vector3.down),
                Vector3.Dot(-hand.forward, Vector3.down)
            );
            return down > 0.25f;
        }

        private void UpdateBeam(GameObject beam, Transform hand, bool on)
        {
            if (beam == null || hand == null) return;
            beam.SetActive(on);
            if (!on) return;

            // Beam shoots out of the palm
            beam.transform.position = hand.position + (-hand.up * 0.12f);
            beam.transform.rotation = Quaternion.LookRotation(-hand.up);
            beam.transform.localScale = new Vector3(0.06f, 0.06f, 0.55f);
        }

        private void SetPadColor(GameObject glove, bool on)
        {
            if (glove == null) return;
            Transform pad = glove.transform.Find("ThrusterPad");
            if (pad == null) return;
            Renderer r = pad.GetComponent<Renderer>();
            if (r == null) return;
            r.material.color = on ? new Color(1f, 0.9f, 0.2f) : new Color(1f, 0.7f, 0.1f);
        }

        private void TrySetup()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null) player = GameObject.Find("LocalPlayer");
            if (player == null) player = GameObject.Find("GorillaPlayer");
            if (player == null) player = GameObject.Find("Player Objects");
            if (player == null) return;

            // Try hard to find a Rigidbody
            playerRb = player.GetComponent<Rigidbody>();
            if (playerRb == null) playerRb = player.GetComponentInChildren<Rigidbody>();
            if (playerRb == null) playerRb = player.GetComponentInParent<Rigidbody>();
            if (playerRb == null)
            {
                Rigidbody[] rbs = Object.FindObjectsOfType<Rigidbody>();
                for (int i = 0; i < rbs.Length; i++)
                {
                    if (rbs[i] != null && rbs[i].gameObject.name.ToLower().Contains("player"))
                    {
                        playerRb = rbs[i];
                        break;
                    }
                }
            }

            leftHand = FindHand(player.transform, true);
            rightHand = FindHand(player.transform, false);

            head = FindChildRecursive(player.transform, "Head");
            if (head == null) head = FindChildRecursive(player.transform, "Camera");

            if (leftHand == null || rightHand == null)
            {
                MelonLogger.Msg("Hands still missing...");
                return;
            }

            leftGlove = BuildGlove("LeftIronGlove");
            rightGlove = BuildGlove("RightIronGlove");
            leftBeam = BuildBeam("LeftRepulsor");
            rightBeam = BuildBeam("RightRepulsor");

            glovesCreated = true;
            MelonLogger.Msg("Setup done. Left=" + leftHand.name + " Right=" + rightHand.name + " RB=" + (playerRb != null));
        }

        private Transform FindHand(Transform root, bool left)
        {
            string[] names = left
                ? new string[] { "LeftHand", "hand_left", "Left Hand", "palm.01.L", "Hand_L", "l_hand", "LeftPalm" }
                : new string[] { "RightHand", "hand_right", "Right Hand", "palm.01.R", "Hand_R", "r_hand", "RightPalm" };

            for (int i = 0; i < names.Length; i++)
            {
                Transform t = FindChildRecursive(root, names[i]);
                if (t != null) return t;
            }
            return null;
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

        private GameObject BuildGlove(string name)
        {
            GameObject root = new GameObject(name);

            Color red = new Color(0.85f, 0.12f, 0.12f);
            Color dark = new Color(0.3f, 0.05f, 0.05f);
            Color gold = new Color(1f, 0.82f, 0.1f);

            // MUCH bigger pieces so they are obvious in VR
            MakePart(root.transform, "Palm", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.04f), Quaternion.identity,
                new Vector3(0.14f, 0.04f, 0.16f), red);

            MakePart(root.transform, "Back", PrimitiveType.Cube,
                new Vector3(0f, 0.03f, 0.03f), Quaternion.identity,
                new Vector3(0.12f, 0.03f, 0.12f), dark);

            MakePart(root.transform, "Wrist", PrimitiveType.Cube,
                new Vector3(0f, 0f, -0.08f), Quaternion.identity,
                new Vector3(0.12f, 0.07f, 0.07f), red);

            MakePart(root.transform, "ThrusterPad", PrimitiveType.Cylinder,
                new Vector3(0f, -0.03f, 0.04f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.1f, 0.015f, 0.1f), gold);

            // Finger stubs
            float[] xs = { -0.045f, -0.015f, 0.015f, 0.045f };
            for (int i = 0; i < 4; i++)
            {
                MakePart(root.transform, "F" + i, PrimitiveType.Cube,
                    new Vector3(xs[i], 0.01f, 0.14f), Quaternion.identity,
                    new Vector3(0.028f, 0.03f, 0.07f), red);
            }

            return root;
        }

        private GameObject BuildBeam(string name)
        {
            // Orange repulsor cone / beam
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = name;
            beam.transform.localScale = new Vector3(0.06f, 0.06f, 0.55f);

            Collider c = beam.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);

            Renderer r = beam.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(1f, 0.45f, 0.05f, 0.9f);
            }

            beam.SetActive(false);
            return beam;
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
