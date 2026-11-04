using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;

[assembly: MelonInfo(typeof(IronMonke.JxIronExtras), "JxIronExtras", "1.0.1", "Jx")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    // Companion for IronMonke.dll
    // Gloves/flight/repulsors stay in IronMonke.dll
    // This only adds chest armor + simple HUD lights
    public class JxIronExtras : MelonMod
    {
        private const string ChestPath = "rig/body";
        private const string HeadPath = "rig/body/head";

        private Transform body;
        private Transform head;

        private GameObject armorRoot;
        private GameObject hudRoot;

        private bool ready;
        private int tries;
        private float nextTry;

        private static readonly Color ArmorRed = new Color(0.75f, 0.08f, 0.08f);
        private static readonly Color ArmorDark = new Color(0.2f, 0.04f, 0.04f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.12f);
        private static readonly Color HudCyan = new Color(0.2f, 1f, 1f);

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("JxIronExtras 1.0.1 - HUD + armor (pair with IronMonke.dll)");
        }

        public override void OnUpdate()
        {
            if (!ready)
            {
                if (Time.time < nextTry) return;
                nextTry = Time.time + 1.2f;
                TrySetup();
                return;
            }

            UpdateHUD();
        }

        private void TrySetup()
        {
            tries++;
            if (tries > 50)
            {
                MelonLogger.Warning("JxIronExtras: could not find player rig");
                ready = true;
                return;
            }

            Transform rig = FindRig();
            if (rig == null) return;

            body = rig.Find(ChestPath);
            if (body == null) body = FindDeep(rig, "body") ?? rig;

            head = rig.Find(HeadPath);
            if (head == null) head = FindDeep(rig, "head");
            if (head == null) head = FindDeep(rig, "Head");
            if (head == null && Camera.main != null) head = Camera.main.transform;

            if (body == null) return;

            BuildArmor(body);
            if (head != null) BuildHUD();

            ready = true;
            MelonLogger.Msg("JxIronExtras ready. armor=" + (armorRoot != null) + " hud=" + (hudRoot != null));
        }

        private Transform FindRig()
        {
            try
            {
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t = a.GetType("GorillaTagger");
                    if (t == null) continue;
                    PropertyInfo inst = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    object tagger = inst != null ? inst.GetValue(null, null) : null;
                    if (tagger == null) continue;
                    FieldInfo f = t.GetField("offlineVRRig");
                    PropertyInfo p = t.GetProperty("offlineVRRig");
                    object rig = f != null ? f.GetValue(tagger) : (p != null ? p.GetValue(tagger, null) : null);
                    Component c = rig as Component;
                    if (c != null) return c.transform;
                }
            }
            catch { }

            GameObject go = GameObject.Find("Player");
            if (go == null) go = GameObject.Find("LocalPlayer");
            if (go == null) go = GameObject.Find("GorillaPlayer");
            return go != null ? go.transform : null;
        }

        private Transform FindDeep(Transform root, string name)
        {
            if (root.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform t = FindDeep(root.GetChild(i), name);
                if (t != null) return t;
            }
            return null;
        }

        private void BuildArmor(Transform chest)
        {
            armorRoot = new GameObject("JxIronArmor");
            armorRoot.transform.SetParent(chest, false);
            armorRoot.transform.localPosition = Vector3.zero;
            armorRoot.transform.localRotation = Quaternion.identity;
            armorRoot.transform.localScale = Vector3.one;

            Part(armorRoot.transform, "Chest", PrimitiveType.Cube,
                new Vector3(0f, 0.12f, 0.08f), Quaternion.identity,
                new Vector3(0.28f, 0.22f, 0.06f), ArmorRed);

            Part(armorRoot.transform, "Reactor", PrimitiveType.Cylinder,
                new Vector3(0f, 0.12f, 0.12f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.08f, 0.015f, 0.08f), Gold);

            Part(armorRoot.transform, "Abs", PrimitiveType.Cube,
                new Vector3(0f, -0.02f, 0.07f), Quaternion.identity,
                new Vector3(0.22f, 0.12f, 0.05f), ArmorDark);

            Part(armorRoot.transform, "ShoulderL", PrimitiveType.Cube,
                new Vector3(-0.2f, 0.22f, 0.02f), Quaternion.identity,
                new Vector3(0.12f, 0.08f, 0.12f), ArmorRed);

            Part(armorRoot.transform, "ShoulderR", PrimitiveType.Cube,
                new Vector3(0.2f, 0.22f, 0.02f), Quaternion.identity,
                new Vector3(0.12f, 0.08f, 0.12f), ArmorRed);
        }

        private void BuildHUD()
        {
            hudRoot = new GameObject("JxIronHUD");

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "HUDPanel";
            panel.transform.SetParent(hudRoot.transform, false);
            panel.transform.localScale = new Vector3(0.25f, 0.12f, 0.005f);
            UnityEngine.Object.Destroy(panel.GetComponent<Collider>());
            Renderer pr = panel.GetComponent<Renderer>();
            if (pr != null) pr.material.color = new Color(0f, 0f, 0f, 0.55f);

            MakeLight(hudRoot.transform, "L1", new Vector3(-0.08f, 0.03f, -0.01f), HudCyan);
            MakeLight(hudRoot.transform, "L2", new Vector3(0f, 0.03f, -0.01f), Gold);
            MakeLight(hudRoot.transform, "L3", new Vector3(0.08f, 0.03f, -0.01f), new Color(0.2f, 1f, 0.3f));
        }

        private void MakeLight(Transform parent, string name, Vector3 pos, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * 0.025f;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
        }

        private void UpdateHUD()
        {
            if (hudRoot == null || head == null) return;

            hudRoot.transform.position = head.position + head.forward * 0.55f + head.up * 0.05f;
            hudRoot.transform.rotation = Quaternion.LookRotation(hudRoot.transform.position - head.position, head.up);

            Transform l2 = hudRoot.transform.Find("L2");
            if (l2 != null)
            {
                Renderer r = l2.GetComponent<Renderer>();
                if (r != null)
                {
                    float pulse = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 3f));
                    r.material.color = new Color(1f, 0.84f * pulse, 0.12f);
                }
            }
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
            if (c != null) UnityEngine.Object.Destroy(c);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
        }
    }
}
