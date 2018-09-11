using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ModHelper.Config;

namespace BetterHovers
{
    public class QPatch
    {
        public static ModConfig config;
        public static void PatchGame()
        {
            var hinst = Harmony.HarmonyInstance.Create("aceba1.ttmm.hoverfix");
            hinst.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            
            config = new ModConfig();
        }

        private static class Patches
        {
            [Harmony.HarmonyPatch(typeof(HoverJet), "OnAttach")]
            private static class AddToBlock
            {
                private static void Postfix(HoverJet __instance)
                {
                    HoverJetAim.AttachToBlock(__instance.gameObject);
                }
            }
        }
    }

    public struct HoverType
    {
        public static Dictionary<BlockTypes, HoverType> cachedHovers = new Dictionary<BlockTypes, HoverType>();
        public static HoverType GetSettings(BlockTypes blockType)
        {
            if (cachedHovers.TryGetValue(blockType, out HoverType hoverType))
            {
                return hoverType;
            }
            return QPatch.config.GetConfigDeep<HoverType>(blockType.ToString());
        }
        public float strength, minForce, maxForce;
    }

    // Token: 0x02000002 RID: 2
    public class HoverJetAim : MonoBehaviour
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        public static void AttachToBlock(GameObject gameObject)
        {
            gameObject.AddComponent<HoverJetAim>();
        }

        // Token: 0x06000002 RID: 2 RVA: 0x0000205C File Offset: 0x0000025C
        public void Start()
        {
            bool flag = !k_set;
            if (flag)
            {
                k_set = true;
                k_LayerMask = LayerMask.GetMask("Terrain", "Water", "Landmarks");
            }
            block = base.gameObject.GetComponent<TankBlock>();
            try
            {
                try
                {
                    HoverType hoverType = HoverType.GetSettings(block.BlockType);
                    Strength = hoverType.strength;
                    ForceMin = hoverType.minForce;
                    ForceMax = hoverType.maxForce;
                }
                catch
                {
                    Debug.Log("Config is missing type for " + block.BlockType.ToString() + "!");
                }
                HoverJet[] components = gameObject.GetComponents<HoverJet>();
                HoverJet[] componentsInChildren = base.gameObject.GetComponentsInChildren<HoverJet>();
                hover = new HoverJet[components.Length + componentsInChildren.Length];
                lastdist = new float[hover.Length];
                components.CopyTo(hover, 0);
                componentsInChildren.CopyTo(hover, components.Length);
            }
            catch
            {
                Debug.Log("There was a problem with adding dampener to block " + block.BlockType.ToString() + "! Removing");
                Destroy(this);
            }
        }

        // Token: 0x06000003 RID: 3 RVA: 0x0000214C File Offset: 0x0000034C
        private void FixedUpdate()
        {
            try
            {
                bool flag = !block.tank;
                if (!flag)
                {
                    for (int i = 0; i < hover.Length; i++)
                    {
                        Rigidbody rigidbody = (!block.tank) ? block.rbody : block.tank.rbody;
                        Vector3 vector = rigidbody.position + (block.transform.position - rigidbody.transform.position);
                        Ray ray = new Ray(vector - hover[i].effector.forward * hover[i].jetRadius, hover[i].effector.forward);
                        RaycastHit raycastHit;
                        bool flag2 = Physics.SphereCast(ray, hover[i].jetRadius, out raycastHit, hover[i].forceRangeMax, k_LayerMask);
                        if (flag2)
                        {
                            float distance = raycastHit.distance;
                            float num = lastdist[i] - distance;
                            num *= Strength;
                            num = Mathf.Min(num, hover[i].forceMax * ForceMax);
                            num = Mathf.Max(hover[i].forceMax * ForceMin, num);
                            rigidbody.AddForceAtPosition(-hover[i].effector.forward * num, vector);
                            lastdist[i] = distance;
                        }
                        else
                        {
                            lastdist[i] = hover[i].forceRangeMax;
                        }
                    }
                }
            }
            catch
            {

            }
        }

        // Token: 0x04000001 RID: 1
        private static int k_LayerMask;

        // Token: 0x04000002 RID: 2
        private static bool k_set = false;

        // Token: 0x04000003 RID: 3
        private HoverJet[] hover;

        // Token: 0x04000004 RID: 4
        private float[] lastdist;

        // Token: 0x04000005 RID: 5
        private TankBlock block;

        public float Strength = 1000f, ForceMin = -5f, ForceMax = 15f;
    }
}
