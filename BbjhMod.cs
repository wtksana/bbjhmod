using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
using Game.Window.Battle;
using Game.Utils;
using UnityEngine;
using TableDR;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace bbjhmod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class BbjhMod : BaseUnityPlugin
    {
        public const string PluginGuid = "bbjhmod.free-shop-refresh";
        public const string PluginName = "bbjhmod";
        public const string PluginVersion = "1.0.0";
        private const int DesiredShopItemCount = 8;
        private const int BuiltInShopSlotCount = 7;

        private Harmony harmony;

        private void Awake()
        {
            this.harmony = new Harmony(PluginGuid);
            this.harmony.PatchAll();
            Logger.LogInfo("Free shop refresh patch loaded");
        }

        private void OnDestroy()
        {
            if (this.harmony != null)
            {
                this.harmony.UnpatchSelf();
            }
        }

        [HarmonyPatch(typeof(BattleInfoFrag), "get_RefreshShopCost")]
        private static class BattleInfoFragRefreshShopCostPatch
        {
            private static void Postfix(ref int __result)
            {
                __result = 1;
            }
        }

        [HarmonyPatch(typeof(ShopConfig), "get_ShopNum")]
        private static class ShopConfigShopNumPatch
        {
            private static void Postfix(ref int __result)
            {
                __result = DesiredShopItemCount;
            }
        }

        [HarmonyPatch(typeof(BattleInfoFrag), "ShowShop")]
        private static class BattleInfoFragShowShopPatch
        {
            private static void Prefix(BattleInfoFrag __instance, ref int __state)
            {
                Traverse traverse = Traverse.Create(__instance);
                __state = traverse.Field("maxShopGoodNum").GetValue<int>();
                if (__state <= BuiltInShopSlotCount)
                {
                    return;
                }

                Vector3[] slotPos = traverse.Field("slotPos").GetValue<Vector3[]>();
                if (slotPos == null || slotPos.Length < __state)
                {
                    traverse.Field("slotPos").SetValue(new Vector3[__state]);
                }
                traverse.Field("maxShopGoodNum").SetValue(BuiltInShopSlotCount);
            }

            private static void Postfix(BattleInfoFrag __instance, int __state)
            {
                if (__state <= BuiltInShopSlotCount)
                {
                    return;
                }

                Traverse traverse = Traverse.Create(__instance);
                traverse.Field("maxShopGoodNum").SetValue(__state);

                Vector3[] compactPositions = BuildCompactShopPositions(__instance, __state);
                traverse.Field("slotPos").SetValue(compactPositions);
                RepositionShopItems(__instance, compactPositions);
            }

            private static Vector3[] BuildCompactShopPositions(BattleInfoFrag instance, int slotCount)
            {
                RectTransform trSlots = Traverse.Create(instance).Field("trSlots").GetValue<RectTransform>();
                Vector3[] positions = new Vector3[slotCount];
                if (trSlots == null || trSlots.childCount == 0)
                {
                    return positions;
                }

                List<Vector3> anchorPositions = new List<Vector3>();
                int anchorCount = Mathf.Min(BuiltInShopSlotCount, trSlots.childCount);
                for (int i = 0; i < anchorCount; i++)
                {
                    anchorPositions.Add(trSlots.GetChild(i).position);
                }

                List<float> sortedX = anchorPositions.Select(v => v.x).OrderBy(v => v).ToList();
                float minX = sortedX.First();
                float maxX = sortedX.Last();
                float horizontalGap = 120f;
                if (sortedX.Count > 1)
                {
                    List<float> gaps = new List<float>();
                    for (int i = 1; i < sortedX.Count; i++)
                    {
                        float gap = sortedX[i] - sortedX[i - 1];
                        if (gap > 1f)
                        {
                            gaps.Add(gap);
                        }
                    }
                    if (gaps.Count > 0)
                    {
                        horizontalGap = gaps.Average();
                    }
                }

                float centerY = anchorPositions.Average(v => v.y);
                float columnGap = horizontalGap * 1.36f;
                float rightX = maxX - horizontalGap * 1.2f;
                float leftX = rightX - columnGap * 3f;
                float topY = centerY + horizontalGap * 0.68f;
                float bottomY = centerY - horizontalGap * 0.68f;
                float z = anchorPositions[0].z;

                for (int i = 0; i < slotCount; i++)
                {
                    int column = i % 4;
                    int row = i / 4;
                    float y = (row == 0) ? topY : bottomY;
                    positions[i] = new Vector3(leftX + columnGap * column, y, z);
                }

                return positions;
            }

            private static void RepositionShopItems(BattleInfoFrag instance, Vector3[] positions)
            {
                Traverse traverse = Traverse.Create(instance);
                Dictionary<int, ItemGObj> itemMap = traverse.Field("shopItemGObjDic").GetValue<Dictionary<int, ItemGObj>>();
                IDictionary itemDataMap = traverse.Field("shopItemData").GetValue<IDictionary>();
                IDictionary priceCellMap = traverse.Field("shopItemPriceCellDic").GetValue<IDictionary>();

                if (itemMap == null)
                {
                    return;
                }

                foreach (KeyValuePair<int, ItemGObj> entry in itemMap)
                {
                    int slotIndex = entry.Key;
                    if (slotIndex < 0 || slotIndex >= positions.Length)
                    {
                        continue;
                    }

                    ItemGObj item = entry.Value;
                    Vector3 targetPos = positions[slotIndex];
                    item.RectTransform.position = targetPos;

                    object itemData = (itemDataMap == null) ? null : itemDataMap[item.Uid];
                    if (itemData != null)
                    {
                        Traverse itemDataTraverse = Traverse.Create(itemData);
                        itemDataTraverse.Field("Pos").SetValue(targetPos);
                        itemDataTraverse.Field("SlotIndex").SetValue(slotIndex);
                    }

                    object priceCell = (priceCellMap == null) ? null : priceCellMap[slotIndex];
                    if (priceCell != null)
                    {
                        AccessTools.Method(priceCell.GetType(), "SetPos").Invoke(priceCell, new object[] { item.IconRectTrans });
                    }
                }
            }
        }
    }
}
