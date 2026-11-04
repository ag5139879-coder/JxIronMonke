using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;

[assembly: MelonInfo(typeof(IronMonke.JxIronMonke), "JxIronMonke", "3.0.0", "Jx")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    // One mod. Gloves stay on IronMonke.dll (or the map's own hands).
    // Armor + HUD are the Gorilla City scene objects (IronFly_* and IronHUD),
    // not cubes. They only exist after the Gorilla City map is loaded.
    public class JxIronMonke : MelonMod
    {
        private static readonly string[] SuitBones = new string[]
        {
            "torso",
            "head",
            "upper_arm.L",
            "forearm.L",
            "upper_arm.R",
            "forearm.R"
        };

        private static readonly string[] PlayerPaths = new string[]
        {
            "rig/body",
            "rig/body/head",
            "rig/body/shoulder.L/upper_arm.L",
            "rig/body/shoulder.L/upper_arm.L/forearm.L",
            "rig/body/shoulder.R/upper_arm.R",
            "rig/body/shoulder.R/upper_arm.R/forearm.R"
        };

        private Transform rig;
        private Transform head;
        private Transform[] playerBones;
        private GameObject[] suitParts;
        private GameObject face;
        private GameObject hud;

        private bool ready;
        private bool missingLogged;
        private float nextTry;

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("JxIronMonke 3.0.0");
            MelonLogger.Msg("Uses Gorilla City IronFly armor + IronHUD. Load that map.");
            MelonLogger.Msg("Does not build cube armor. Leave IronMonke.dll in Mods for gloves/flight.");
        }

        public override void OnUpdate()
        {
            if (!ready)
            {
                if (Time.time < nextTry) return;
                nextTry = Time.time + 1.5f;
                TrySetup();
                return;
            }

            StickArmor();
            StickHud();
        }

        private void TrySetup()
        {
            rig = FindRig();
            if (rig == null) return;

            GameObject torso = GameObject.Find("IronFly_torso");
            GameObject hudGo = GameObject.Find("IronHUD");
            if (torso == null && hudGo == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    MelonLogger.Msg("Gorilla City suit not in this scene yet. Join the Gorilla City map.");
                }
                return;
            }

            playerBones = new Transform[SuitBones.Length];
            suitParts = new GameObject[SuitBones.Length];
            int found = 0;
            for (int i = 0; i < SuitBones.Length; i++)
            {
                playerBones[i] = rig.Find(PlayerPaths[i]);
                if (playerBones[i] == null) playerBones[i] = FindDeep(rig, SuitBones[i]);
                suitParts[i] = GameObject.Find("IronFly_" + SuitBones[i]);
                if (suitParts[i] != null) found++;
            }

            face = GameObject.Find("IronFace");
            hud = hudGo;
            head = playerBones[1];
            if (head == null && Camera.main != null) head = Camera.main.transform;

            for (int i = 0; i < suitParts.Length; i++)
            {
                if (suitParts[i] != null) suitParts[i].SetActive(true);
            }
            if (face != null) face.SetActive(true);
            if (hud != null) hud.SetActive(true);

            ready = found > 0 || hud != null;
            MelonLogger.Msg("City suit pieces=" + found + " face=" + (face != null) + " hud=" + (hud != null));
        }

        private void StickArmor()
        {
            for (int i = 0; i < suitParts.Length; i++)
            {
                GameObject part = suitParts[i];
                Transform bone = playerBones[i];
                if (part == null || bone == null) continue;
                // Map script already driving it (near the body). Don't fight it.
                if ((part.transform.position - bone.position).sqrMagnitude < 4f) continue;
                part.transform.SetPositionAndRotation(bone.position, bone.rotation);
            }

            if (face != null && head != null)
            {
                if ((face.transform.position - head.position).sqrMagnitude > 4f)
                    face.transform.SetPositionAndRotation(head.position, head.rotation);
            }
        }

        private void StickHud()
        {
            if (hud == null || head == null) return;
            // Same offset the map script uses: head rotation * (0, 0.14, 0.11)
            Vector3 pos = head.position + head.rotation * new Vector3(0f, 0.14f, 0.11f);
            if ((hud.transform.position - pos).sqrMagnitude < 0.05f) return;
            hud.transform.SetPositionAndRotation(pos, head.rotation);
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
                    object rigObj = f != null ? f.GetValue(tagger) : (p != null ? p.GetValue(tagger, null) : null);
                    Component c = rigObj as Component;
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
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform t = FindDeep(root.GetChild(i), name);
                if (t != null) return t;
            }
            return null;
        }
    }
}
