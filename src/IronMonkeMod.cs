using MelonLoader;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "2.0.0", "Jx + IronMonke")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    // Uses the same bone paths and Player.Instance pattern as IronMonkeMelon 1.0.3 (buzzbb)
    public class IronMonkeMod : MelonMod
    {
        // Exact paths from original IronMonke.dll
        private const string LeftHandPath = "rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L";
        private const string RightHandPath = "rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R";

        private Transform leftHand;
        private Transform rightHand;
        private GameObject leftGlove;
        private GameObject rightGlove;

        private Rigidbody body;
        private bool ready;
        private int setupTries;
        private float nextSetup;

        private const float ThrustForce = 28f;
        private const float BlastSpeed = 28f;
        private const float BlastLife = 1.6f;
        private const float BlastCooldown = 0.12f;
        private const float BlastRecoil = 1.5f;

        private float leftCd;
        private float rightCd;
        private bool leftWasTrigger;
        private bool rightWasTrigger;

        private readonly List<Blast> blasts = new List<Blast>();

        private static readonly Color ArmorRed = new Color(0.8f, 0.1f, 0.1f);
        private static readonly Color ArmorBlack = new Color(0.05f, 0.05f, 0.08f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.12f);
        private static readonly Color GoldHot = new Color(1f, 0.95f, 0.4f);
        private static readonly Color BlastOrange = new Color(1f, 0.45f, 0.05f);

        // Reflection caches for GorillaLocomotion.Player + EasyInputs
        private static PropertyInfo playerInstanceProp;
        private static PropertyInfo playerRbProp;
        private static MethodInfo easySecondary;
        private static MethodInfo easyTrigger;
        private static bool reflected;

        private class Blast
        {
            public GameObject go;
            public Vector3 vel;
            public float dieAt;
        }

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("JxIronMonke 2.0.0 - IronMonke paths + Y/B thrust");
            MelonLogger.Msg("Thrust: Y (left) / B (right)  |  Trigger = repulsor blast");
            CacheReflection();
        }

        public override void OnFixedUpdate()
        {
            if (!ready)
            {
                if (Time.time < nextSetup) return;
                nextSetup = Time.time + 1f;
                TrySetup();
                return;
            }

            bool leftThrust = ReadSecondary(true);
            bool rightThrust = ReadSecondary(false);
            bool leftTrig = ReadTrigger(true);
            bool rightTrig = ReadTrigger(false);

            // Fallback: palm-down if EasyInputs missing
            if (!leftThrust && !rightThrust && easySecondary == null)
            {
                leftThrust = PalmDown(leftHand);
                rightThrust = PalmDown(rightHand);
            }

            SetPad(leftGlove, leftThrust);
            SetPad(rightGlove, rightThrust);

            if (body != null)
            {
                if (leftThrust && leftHand != null)
                    body.AddForce(-leftHand.up * ThrustForce, ForceMode.Acceleration);
                if (rightThrust && rightHand != null)
                    body.AddForce(-rightHand.up * ThrustForce, ForceMode.Acceleration);
            }

            // Trigger edge = fire blast (like original)
            if (leftTrig && !leftWasTrigger && leftCd <= 0f && leftHand != null)
            {
                FireBlast(leftHand);
                leftCd = BlastCooldown;
            }
            if (rightTrig && !rightWasTrigger && rightCd <= 0f && rightHand != null)
            {
                FireBlast(rightHand);
                rightCd = BlastCooldown;
            }
            leftWasTrigger = leftTrig;
            rightWasTrigger = rightTrig;

            leftCd -= Time.fixedDeltaTime;
            rightCd -= Time.fixedDeltaTime;
            UpdateBlasts();
        }

        private void CacheReflection()
        {
            if (reflected) return;
            reflected = true;
            try
            {
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type playerType = a.GetType("GorillaLocomotion.Player");
                    if (playerType != null)
                    {
                        playerInstanceProp = playerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                        playerRbProp = playerType.GetProperty("playerRigidBody", BindingFlags.Public | BindingFlags.Instance)
                                   ?? playerType.GetProperty("Rigidbody", BindingFlags.Public | BindingFlags.Instance);
                        MelonLogger.Msg("Found GorillaLocomotion.Player");
                    }

                    Type easyType = a.GetType("easyInputs.EasyInputs") ?? a.GetType("EasyInputs");
                    if (easyType != null)
                    {
                        easySecondary = easyType.GetMethod("GetSecondaryButtonDown", BindingFlags.Public | BindingFlags.Static);
                        easyTrigger = easyType.GetMethod("GetTriggerButtonDown", BindingFlags.Public | BindingFlags.Static);
                        MelonLogger.Msg("Found EasyInputs");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Reflection: " + ex.Message);
            }
        }

        private Rigidbody GetPlayerBody()
        {
            try
            {
                if (playerInstanceProp != null)
                {
                    object inst = playerInstanceProp.GetValue(null, null);
                    if (inst != null)
                    {
                        if (playerRbProp != null)
                        {
                            object rb = playerRbProp.GetValue(inst, null);
                            if (rb is Rigidbody r) return r;
                        }
                        // Component on player object
                        if (inst is Component c)
                        {
                            Rigidbody r2 = c.GetComponent<Rigidbody>();
                            if (r2 != null) return r2;
                            r2 = c.GetComponentInChildren<Rigidbody>();
                            if (r2 != null) return r2;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private bool ReadSecondary(bool left)
        {
            try
            {
                if (easySecondary != null)
                {
                    // Original: EasyInputs.GetSecondaryButtonDown((EasyHand)(!left))
                    // EasyHand enum: Left=0, Right=1 — original uses (!left) so left hand -> Right enum value weirdness;
                    // Try both common conventions: left=0 and left=1
                    object handEnum = left ? 0 : 1;
                    object result = easySecondary.Invoke(null, new object[] { handEnum });
                    if (result is bool b && b) return true;
                    handEnum = left ? 1 : 0;
                    result = easySecondary.Invoke(null, new object[] { handEnum });
                    if (result is bool b2) return b2;
                }
            }
            catch { }
            return false;
        }

        private bool ReadTrigger(bool left)
        {
            try
            {
                if (easyTrigger != null)
                {
                    object handEnum = left ? 0 : 1;
                    object result = easyTrigger.Invoke(null, new object[] { handEnum });
                    if (result is bool b && b) return true;
                    handEnum = left ? 1 : 0;
                    result = easyTrigger.Invoke(null, new object[] { handEnum });
                    if (result is bool b2) return b2;
                }
            }
            catch { }
            return false;
        }

        private bool PalmDown(Transform hand)
        {
            if (hand == null) return false;
            return Mathf.Max(Vector3.Dot(hand.up, Vector3.down), Vector3.Dot(-hand.up, Vector3.down)) > 0.35f;
        }

        private void FireBlast(Transform hand)
        {
            Vector3 dir = -hand.up;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "IronBlast";
            go.transform.position = hand.position + dir * 0.15f;
            go.transform.localScale = Vector3.one * 0.09f;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = BlastOrange;

            blasts.Add(new Blast { go = go, vel = dir * BlastSpeed, dieAt = Time.time + BlastLife });

            if (body != null)
                body.AddForce(-dir * BlastRecoil, ForceMode.VelocityChange);
        }

        private void UpdateBlasts()
        {
            float t = Time.time;
            float dt = Time.fixedDeltaTime;
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
            }
        }

        private void SetPad(GameObject glove, bool on)
        {
            if (glove == null) return;
            Transform pad = glove.transform.Find("Pad");
            if (pad == null) return;
            Renderer r = pad.GetComponent<Renderer>();
            if (r != null) r.material.color = on ? GoldHot : Gold;
        }

        private void TrySetup()
        {
            setupTries++;
            if (setupTries > 40)
            {
                MelonLogger.Warning("Giving up on gloves (will keep trying body only)");
                ready = true;
                return;
            }

            // Prefer GorillaTagger offline VR rig like original
            Transform rigRoot = null;
            try
            {
                Type taggerType = null;
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    taggerType = a.GetType("GorillaTagger");
                    if (taggerType != null) break;
                }
                if (taggerType != null)
                {
                    PropertyInfo inst = taggerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    object tagger = inst != null ? inst.GetValue(null, null) : null;
                    if (tagger != null)
                    {
                        FieldInfo rigField = taggerType.GetField("offlineVRRig") ?? taggerType.GetField("offlineVRRig", BindingFlags.Public | BindingFlags.Instance);
                        PropertyInfo rigProp = taggerType.GetProperty("offlineVRRig");
                        object rig = rigField != null ? rigField.GetValue(tagger) : (rigProp != null ? rigProp.GetValue(tagger, null) : null);
                        if (rig is Component rc) rigRoot = rc.transform;
                    }
                }
            }
            catch { }

            if (rigRoot == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player == null) player = GameObject.Find("LocalPlayer");
                if (player == null) player = GameObject.Find("GorillaPlayer");
                if (player != null) rigRoot = player.transform;
            }

            if (rigRoot == null) return;

            leftHand = rigRoot.Find(LeftHandPath);
            rightHand = rigRoot.Find(RightHandPath);

            // Fallback search
            if (leftHand == null) leftHand = Find(rigRoot, "hand.L") ?? Find(rigRoot, "LeftHand");
            if (rightHand == null) rightHand = Find(rigRoot, "hand.R") ?? Find(rigRoot, "RightHand");

            body = GetPlayerBody();
            if (body == null)
            {
                body = rigRoot.GetComponentInChildren<Rigidbody>();
                if (body == null) body = rigRoot.GetComponentInParent<Rigidbody>();
            }

            if (leftHand == null || rightHand == null)
            {
                MelonLogger.Msg("Hands not found yet try " + setupTries);
                return;
            }

            leftGlove = BuildGlove("gloveL", leftHand, true);
            rightGlove = BuildGlove("gloveR", rightHand, false);
            ready = true;
            MelonLogger.Msg("Ready. L=" + leftHand.name + " R=" + rightHand.name + " body=" + (body != null));
        }

        private Transform Find(Transform root, string name)
        {
            if (root.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform t = Find(root.GetChild(i), name);
                if (t != null) return t;
            }
            return null;
        }

        private GameObject BuildGlove(string name, Transform hand, bool left)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(hand, false);

            // Offsets inspired by original BuildGlove rootPos values (scaled for procedural cubes)
            if (left)
            {
                root.transform.localPosition = new Vector3(-0.01f, 0.01f, 0f);
                root.transform.localRotation = Quaternion.identity;
            }
            else
            {
                root.transform.localPosition = new Vector3(0.01f, 0.01f, 0f);
                root.transform.localRotation = Quaternion.identity;
            }
            root.transform.localScale = Vector3.one;

            Part(root.transform, "Palm", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0.02f), Quaternion.identity,
                new Vector3(0.09f, 0.038f, 0.11f), ArmorRed);

            Part(root.transform, "Back", PrimitiveType.Cube,
                new Vector3(0f, 0.022f, 0.02f), Quaternion.identity,
                new Vector3(0.08f, 0.018f, 0.08f), ArmorBlack);

            Part(root.transform, "Cuff", PrimitiveType.Cube,
                new Vector3(0f, 0f, -0.05f), Quaternion.identity,
                new Vector3(0.085f, 0.048f, 0.04f), ArmorBlack);

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
