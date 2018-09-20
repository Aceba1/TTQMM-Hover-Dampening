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
                    HoverJetAim.AttachToBlock(__instance);
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
        public static void AttachToBlock(HoverJet gameObject)
        {
            var thing = gameObject.gameObject.AddComponent<HoverJetAim>();
            if (thing != null) thing.hover = gameObject;
        }

        TankBlock block;

        public void Start()
        {
            if (!k_set)
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
                if (block.tank)
                {
                    Rigidbody rigidbody = (!block.tank) ? block.rbody : block.tank.rbody;
                    Vector3 vector = rigidbody.position + (hover.effector.position - rigidbody.transform.position);
                    Ray ray = new Ray(vector - hover.effector.forward * hover.jetRadius, hover.effector.forward);
                    RaycastHit raycastHit;
                    if (Physics.SphereCast(ray, hover.jetRadius, out raycastHit, hover.forceRangeMax, k_LayerMask))
                    {
                        float distance = raycastHit.distance;
                        float num = lastdist - distance;
                        num *= Strength;
                        num = Mathf.Min(num, hover.forceMax * ForceMax);
                        num = Mathf.Max(hover.forceMax * ForceMin, num);
                        rigidbody.AddForceAtPosition(-hover.effector.forward * num, vector);
                        lastdist = distance;
                    }
                }
            }
            catch
            {

            }
        }

        private static int k_LayerMask;
        private static bool k_set = false;
        private HoverJet hover;
        private float lastdist;

        public float Strength = 1000f, ForceMin = -5f, ForceMax = 15f;
    }
}
