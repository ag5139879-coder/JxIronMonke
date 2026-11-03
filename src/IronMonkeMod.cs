using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.7.0", "Jx")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    public class IronMonkeMod : MelonMod
    {
        private Transform leftHand;
        private Transform rightHand;

        private GameObject leftGlove;
        private GameObject rightGlove;
        private GameObject leftBeam;
        private GameObject rightBeam;

        private bool ready;
        private float nextSearch;

        private Rigidbody body;
        private const float THRUST = 22f;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("JxIronMonke 1.7.0 - hand sized gloves + repulsor + flight");
        }

        public override void OnUpdate()
        {
            if (!ready)
            {
                if (Time.time < nextSearch) return;
                nextSearch = Time.time + 1f;
                TryInit();
                return;
            }

            // Parenting keeps rotation correct - do not fight it with SetPositionAndRotation
            bool L = PalmThrust(leftHand);
            bool R = PalmThrust(rightHand);

            ShowBeam(leftBeam, leftHand, L);
            ShowBeam(rightBeam, rightHand, R);
            Glow(leftGlove, L);
            Glow(rightGlove, R);

            if (body != null)
            {
                if (L) body.AddForce(-leftHand.up * THRUST, ForceMode.Acceleration);
                if (R) body.AddForce(-rightHand.up * THRUST, ForceMode.Acceleration);
            }
        }

        private bool PalmThrust(Transform hand)
        {
            if (hand == null) return false;
            // Active when palm roughly aims down
            float d1 = Vector3.Dot(hand.up, Vector3.down);
            float d2 = Vector3.Dot(-hand.up, Vector3.down);
            return Mathf.Max(d1, d2) > 0.35f;
        }

        private void ShowBeam(GameObject beam, Transform hand, bool on)
        {
            if (beam == null || hand == null) return;
            if (!on)
            {
                beam.SetActive(false);
                return;
            }
            beam.SetActive(true);
            // Cylinder default axis is Y - aim -hand.up
            Vector3 dir = -hand.up;
            beam.transform.position = hand.position + dir * 0.08f;
            beam.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            beam.transform.localScale = new Vector3(0.035f, 0.18f, 0.035f);
        }

        private void Glow(GameObject glove, bool on)
        {
            if (glove == null) return;
            Transform pad = glove.transform.Find("Pad");
            if (pad == null) return;
            Renderer r = pad.GetComponent<Renderer>();
            if (r == null) return;
            r.material.color = on ? new Color(1f, 0.95f, 0.3f) : new Color(1f, 0.75f, 0.1f);
        }

        private void TryInit()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null) player = GameObject.Find("LocalPlayer");
            if (player == null) player = GameObject.Find("GorillaPlayer");
            if (player == null) player = GameObject.Find("Player Objects");
            if (player == null) return;

            body = player.GetComponent<Rigidbody>();
            if (body == null) body = player.GetComponentInChildren<Rigidbody>();
            if (body == null) body = player.GetComponentInParent<Rigidbody>();

            leftHand = Find(player.transform, "LeftHand")
                    ?? Find(player.transform, "hand_left")
                    ?? Find(player.transform, "Left Hand")
                    ?? Find(player.transform, "palm.01.L");

            rightHand = Find(player.transform, "RightHand")
                     ?? Find(player.transform, "hand_right")
                     ?? Find(player.transform, "Right Hand")
                     ?? Find(player.transform, "palm.01.R");

            if (leftHand == null || rightHand == null) return;

            leftGlove = MakeGlove("LGlove", leftHand);
            rightGlove = MakeGlove("RGlove", rightHand);
            leftBeam = MakeBeam("LBeam");
            rightBeam = MakeBeam("RBeam");

            ready = true;
            MelonLogger.Msg("Ready. hands=" + leftHand.name + "/" + rightHand.name + " body=" + (body != null));
        }

        private Transform Find(Transform root, string name)
        {
            if (root.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform t = Find(root.GetChild(i), name);
                if (t != null) return t;
            }
            return null;
        }

        private GameObject MakeGlove(string name, Transform hand)
        {
            // PARENT to hand so rotation is always correct (same as first working version)
            GameObject root = new GameObject(name);
            root.transform.SetParent(hand, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            Color red = new Color(0.82f, 0.12f, 0.12f);
            Color gold = new Color(1f, 0.8f, 0.12f);

            // Hand-sized palm (close to the first cube size that looked ok)
            Part(root.transform, "Palm", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.02f), Quaternion.identity,
                new Vector3(0.09f, 0.035f, 0.11f), red);

            Part(root.transform, "Wrist", PrimitiveType.Cube,
                new Vector3(0f, 0f, -0.05f), Quaternion.identity,
                new Vector3(0.08f, 0.045f, 0.04f), red);

            // Palm thruster pad
            Part(root.transform, "Pad", PrimitiveType.Cylinder,
                new Vector3(0f, -0.022f, 0.02f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.06f, 0.01f, 0.06f), gold);

            return root;
        }

        private GameObject MakeBeam(string name)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = name;
            Collider col = b.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            Renderer r = b.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1f, 0.4f, 0.05f);
            b.SetActive(false);
            return b;
        }

        private void Part(Transform parent, string n, PrimitiveType t,
            Vector3 pos, Quaternion rot, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(t);
            go.name = n;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
        }
    }
}
