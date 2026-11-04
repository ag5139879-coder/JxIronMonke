using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;

[assembly: MelonInfo(typeof(IronMonke.JxIronMonke), "JxIronMonke", "3.1.0", "Jx")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    // Works on this build with no Gorilla City map.
    // Assembly-CSharp has GorillaTagger.leftHandTransform, rightHandTransform,
    // playerRigidBody, offlineVRRig, and EasyInputs secondary/trigger buttons.
    // Hands are not covered so IronMonke.dll gloves can stay.
    public class JxIronMonke : MelonMod
    {
        private Transform bodyBone;
        private Transform headBone;
        private Transform leftHand;
        private Transform rightHand;
        private Rigidbody bodyRb;

        private GameObject suit;
        private GameObject hud;
        private GameObject[] bars;

        private bool ready;
        private float nextTry;

        private MethodInfo secondaryBtn;
        private MethodInfo triggerBtn;
        private object easyLeft;
        private object easyRight;

        private const float Thrust = 26f;

        private static readonly Color Red = new Color(0.75f, 0.08f, 0.08f);
        private static readonly Color Dark = new Color(0.12f, 0.03f, 0.03f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.12f);
        private static readonly Color Cyan = new Color(0.35f, 1f, 1f);

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("JxIronMonke 3.1.0 standalone (no Gorilla City)");
            MelonLogger.Msg("Y/B thrust, trigger blast. Hands left for IronMonke gloves.");
        }

        public override void OnFixedUpdate()
        {
            if (!ready)
            {
                if (Time.time < nextTry) return;
                nextTry = Time.time + 1.2f;
                TrySetup();
                return;
            }

            bool l = Btn(secondaryBtn, true);
            bool r = Btn(secondaryBtn, false);
            if (bodyRb != null)
            {
                if (l && leftHand != null) bodyRb.AddForce(-leftHand.up * Thrust, ForceMode.Acceleration);
                if (r && rightHand != null) bodyRb.AddForce(-rightHand.up * Thrust, ForceMode.Acceleration);
            }

            float spd = bodyRb != null ? bodyRb.velocity.magnitude : 0f;
            PaintBars(spd);
        }

        public override void OnUpdate()
        {
            if (!ready || hud == null || headBone == null) return;
            hud.transform.position = headBone.position + headBone.rotation * new Vector3(0f, -0.02f, 0.45f);
            hud.transform.rotation = headBone.rotation;
        }

        private void TrySetup()
        {
            object tagger = null;
            Type taggerType = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                taggerType = a.GetType("GorillaTagger");
                if (taggerType == null) continue;
                PropertyInfo inst = taggerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                tagger = inst != null ? inst.GetValue(null, null) : null;
                if (tagger != null) break;
            }
            if (tagger == null || taggerType == null) return;

            leftHand = AsTransform(GetMem(taggerType, tagger, "leftHandTransform"));
            rightHand = AsTransform(GetMem(taggerType, tagger, "rightHandTransform"));
            bodyRb = GetMem(taggerType, tagger, "playerRigidBody") as Rigidbody;

            object rig = GetMem(taggerType, tagger, "offlineVRRig");
            Transform rigT = AsTransform(rig);
            if (rigT != null)
            {
                bodyBone = rigT.Find("rig/body");
                if (bodyBone == null) bodyBone = FindDeep(rigT, "body");
                headBone = rigT.Find("rig/body/head");
                if (headBone == null) headBone = FindDeep(rigT, "head");
            }
            if (headBone == null)
            {
                object hc = GetMem(taggerType, tagger, "headCollider");
                headBone = AsTransform(hc);
            }
            if (bodyBone == null && rigT != null) bodyBone = rigT;
            if (bodyBone == null || headBone == null) return;

            CacheEasy();
            BuildSuit();
            BuildHud();
            ready = true;
            MelonLogger.Msg("Ready. rb=" + (bodyRb != null) + " L=" + (leftHand != null) + " R=" + (rightHand != null) + " easy=" + (secondaryBtn != null));
        }

        private void CacheEasy()
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = a.GetType("easyInputs.EasyInputs") ?? a.GetType("EasyInputs");
                if (t == null) continue;
                secondaryBtn = t.GetMethod("GetSecondaryButtonDown", BindingFlags.Public | BindingFlags.Static);
                triggerBtn = t.GetMethod("GetTriggerButtonDown", BindingFlags.Public | BindingFlags.Static);
                if (secondaryBtn == null) continue;
                Type hand = secondaryBtn.GetParameters()[0].ParameterType;
                easyLeft = Enum.ToObject(hand, 0);
                easyRight = Enum.ToObject(hand, 1);
                return;
            }
        }

        private bool Btn(MethodInfo m, bool left)
        {
            if (m == null || easyLeft == null) return false;
            try
            {
                object r = m.Invoke(null, new object[] { left ? easyLeft : easyRight });
                return r is bool b && b;
            }
            catch { return false; }
        }

        private void BuildSuit()
        {
            suit = new GameObject("JxSuit");
            suit.transform.SetParent(bodyBone, false);
            suit.transform.localPosition = Vector3.zero;
            suit.transform.localRotation = Quaternion.identity;
            suit.transform.localScale = Vector3.one;

            // Gorilla torso scale. Thin plates, parented so they turn with the body.
            Plate("Chest", new Vector3(0f, 0.05f, 0.12f), new Vector3(0.22f, 0.16f, 0.03f), Red);
            Plate("Core", new Vector3(0f, 0.05f, 0.14f), new Vector3(0.06f, 0.06f, 0.012f), Gold);
            Plate("Gut", new Vector3(0f, -0.08f, 0.11f), new Vector3(0.16f, 0.08f, 0.025f), Dark);
            Plate("ShL", new Vector3(-0.16f, 0.14f, 0.04f), new Vector3(0.08f, 0.05f, 0.08f), Red);
            Plate("ShR", new Vector3(0.16f, 0.14f, 0.04f), new Vector3(0.08f, 0.05f, 0.08f), Red);

            GameObject helm = new GameObject("Helm");
            helm.transform.SetParent(headBone, false);
            helm.transform.localPosition = new Vector3(0f, 0.02f, 0.04f);
            helm.transform.localRotation = Quaternion.identity;
            Box(helm.transform, "Visor", new Vector3(0f, 0.02f, 0.06f), new Vector3(0.1f, 0.04f, 0.02f), Cyan);
            Box(helm.transform, "Cap", new Vector3(0f, 0.06f, 0f), new Vector3(0.12f, 0.04f, 0.12f), Red);
        }

        private void BuildHud()
        {
            hud = new GameObject("JxHud");
            bars = new GameObject[8];
            Box(hud.transform, "Back", Vector3.zero, new Vector3(0.22f, 0.08f, 0.004f), new Color(0f, 0f, 0f, 0.7f));
            for (int i = 0; i < 8; i++)
            {
                float x = -0.07f + i * 0.02f;
                bars[i] = Box(hud.transform, "B" + i, new Vector3(x, 0f, -0.004f), new Vector3(0.014f, 0.04f, 0.004f), Dark);
            }
            Box(hud.transform, "Dot", new Vector3(0f, 0.03f, -0.004f), new Vector3(0.012f, 0.012f, 0.004f), Gold);
        }

        private void PaintBars(float speed)
        {
            int n = (int)(speed / 2f);
            if (n > 8) n = 8;
            if (n < 0) n = 0;
            for (int i = 0; i < bars.Length; i++)
            {
                Renderer r = bars[i].GetComponent<Renderer>();
                if (r == null) continue;
                r.material.color = i < n ? Cyan : Dark;
            }
        }

        private void Plate(string name, Vector3 pos, Vector3 scale, Color color)
        {
            Box(suit.transform, name, pos, scale, color);
        }

        private GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            Collider c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.Destroy(c);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
            return go;
        }

        private static object GetMem(Type t, object inst, string name)
        {
            PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null) return p.GetValue(inst, null);
            FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f != null) return f.GetValue(inst);
            return null;
        }

        private static Transform AsTransform(object o)
        {
            if (o is Transform t) return t;
            if (o is Component c) return c.transform;
            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform hit = FindDeep(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
