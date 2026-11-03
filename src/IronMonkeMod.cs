using MelonLoader;
using UnityEngine;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.9.0", "Jx + IronMonke style")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    // Style + behavior inspired by IronMonkeMelon (buzzbb):
    // BuildGlove, thrust via palm, HandleBlast projectiles
    public class IronMonkeMod : MelonMod
    {
        private Transform leftHand;
        private Transform rightHand;
        private GameObject leftGlove;
        private GameObject rightGlove;

        private bool ready;
        private float nextSearch;

        private Rigidbody body;

        // Tuned similar to classic IronMonke feel
        private const float ThrustForce = 26f;
        private const float BlastSpeed = 18f;
        private const float BlastLife = 1.2f;
        private const float BlastCooldown = 0.35f;
        private const float BlastRecoil = 4f;

        private float leftBlastCd;
        private float rightBlastCd;

        private readonly List<Blast> blasts = new List<Blast>();

        private static readonly Color ArmorRed = new Color(0.8f, 0.1f, 0.1f);
        private static readonly Color ArmorBlack = new Color(0.05f, 0.05f, 0.07f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.1f);
        private static readonly Color GoldHot = new Color(1f, 0.95f, 0.4f);
        private static readonly Color BlastOrange = new Color(1f, 0.45f, 0.05f);

        private class Blast
        {
            public GameObject go;
            public Vector3 vel;
            public float dieAt;
        }

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("JxIronMonke 1.9.0 - IronMonke style gloves/thrust/blast");
        }

        public override void OnUpdate()
        {
            if (!ready)
            {
                if (Time.time < nextSearch) return;
                nextSearch = Time.time + 1f;
                TrySetupGloves();
                return;
            }

            float dt = Time.deltaTime;
            leftBlastCd -= dt;
            rightBlastCd -= dt;

            bool leftThrust = IsThrustPose(leftHand);
            bool rightThrust = IsThrustPose(rightHand);

            SetPadHot(leftGlove, leftThrust);
            SetPadHot(rightGlove, rightThrust);

            // Continuous thruster flight (IronMonke thrustOn style)
            if (body != null)
            {
                if (leftThrust)
                    body.AddForce(-leftHand.up * ThrustForce, ForceMode.Acceleration);
                if (rightThrust)
                    body.AddForce(-rightHand.up * ThrustForce, ForceMode.Acceleration);
            }

            // Repulsor blast when palm is pushed forward-ish + thrust pose
            if (leftThrust && leftBlastCd <= 0f && IsBlastPose(leftHand))
            {
                HandleBlast(leftHand, true);
                leftBlastCd = BlastCooldown;
            }
            if (rightThrust && rightBlastCd <= 0f && IsBlastPose(rightHand))
            {
                HandleBlast(rightHand, false);
                rightBlastCd = BlastCooldown;
            }

            UpdateBlasts();
        }

        private bool IsThrustPose(Transform hand)
        {
            if (hand == null) return false;
            float down = Mathf.Max(Vector3.Dot(hand.up, Vector3.down), Vector3.Dot(-hand.up, Vector3.down));
            return down > 0.3f;
        }

        private bool IsBlastPose(Transform hand)
        {
            if (hand == null) return false;
            // Palm facing mostly away from body / forward in world
            return Vector3.Dot(-hand.up, Vector3.up) < 0.2f;
        }

        private void HandleBlast(Transform hand, bool left)
        {
            Vector3 dir = -hand.up;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = left ? "LeftBlast" : "RightBlast";
            go.transform.position = hand.position + dir * 0.12f;
            go.transform.localScale = Vector3.one * 0.08f;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = BlastOrange;

            blasts.Add(new Blast
            {
                go = go,
                vel = dir * BlastSpeed,
                dieAt = Time.time + BlastLife
            });

            // Small recoil like original BlastRecoil
            if (body != null)
                body.AddForce(-dir * BlastRecoil, ForceMode.VelocityChange);
        }

        private void UpdateBlasts()
        {
            float t = Time.time;
            float dt = Time.deltaTime;
            for (int i = blasts.Count - 1; i >= 0; i--)
            {
                Blast b = blasts[i];
                if (b.go == null || t >= b.dieAt)
                {
                    if (b.go != null) Object.Destroy(b.go);
                    blasts.RemoveAt(i);
                    continue;
                }
                b.go.transform.position += b.vel * dt;
                // Shrink slightly over life
                float life = Mathf.Clamp01((b.dieAt - t) / BlastLife);
                b.go.transform.localScale = Vector3.one * (0.05f + 0.04f * life);
            }
        }

        private void SetPadHot(GameObject glove, bool on)
        {
            if (glove == null) return;
            Transform pad = glove.transform.Find("Pad");
            if (pad == null) return;
            Renderer r = pad.GetComponent<Renderer>();
            if (r != null) r.material.color = on ? GoldHot : Gold;
        }

        private void TrySetupGloves()
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

            leftGlove = BuildGlove("LeftIronGlove", leftHand);
            rightGlove = BuildGlove("RightIronGlove", rightHand);
            ready = true;
            MelonLogger.Msg("Gloves ready. body=" + (body != null) + " L=" + leftHand.name + " R=" + rightHand.name);
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

        // BuildGlove - red / black / gold IronMonke look, hand-sized, parented
        private GameObject BuildGlove(string name, Transform hand)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(hand, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // Red gauntlet palm
            Part(root.transform, "Palm", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.02f), Quaternion.identity,
                new Vector3(0.09f, 0.038f, 0.11f), ArmorRed);

            // Black back plate
            Part(root.transform, "Back", PrimitiveType.Cube,
                new Vector3(0f, 0.022f, 0.02f), Quaternion.identity,
                new Vector3(0.08f, 0.018f, 0.08f), ArmorBlack);

            // Black wrist cuff
            Part(root.transform, "Cuff", PrimitiveType.Cube,
                new Vector3(0f, 0f, -0.05f), Quaternion.identity,
                new Vector3(0.085f, 0.048f, 0.04f), ArmorBlack);

            // Red wrist accent
            Part(root.transform, "CuffRed", PrimitiveType.Cube,
                new Vector3(0f, 0.012f, -0.065f), Quaternion.identity,
                new Vector3(0.075f, 0.025f, 0.02f), ArmorRed);

            // Gold palm thruster (IronMonke signature)
            Part(root.transform, "Pad", PrimitiveType.Cylinder,
                new Vector3(0f, -0.024f, 0.025f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.06f, 0.01f, 0.06f), Gold);

            return root;
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
