using Database;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace GottaCatchEmAll
{
    public class GottaCatchEmAllPatches
    {
        [HarmonyPatch(typeof(KleiItems))]
        [HarmonyPatch(nameof(KleiItems.HasItem))]
        public class GottaCatchEmAll_KleiItems_Patch
        {
            /// <summary>
            /// Set every Item to unlocked
            /// </summary>
            /// <param name="itemType"></param>
            /// <param name="__result"></param>
            /// <param name="__instance"></param>
            /// <returns></returns>
            public static bool Prefix(string itemType, ref bool __result, KleiItems __instance)
            {
                __result = true;

                return false;
            }
        }

        [HarmonyPatch(typeof(PermitResource))]
        [HarmonyPatch(nameof(PermitResource.IsUnlocked))]
        public class GottaCatchEmAll_PermitResource_IsUnlocked_Patch
        {
            /// <summary>
            /// Set every Permit to unlocked
            /// </summary>
            /// <param name="__result"></param>
            /// <param name="__instance"></param>
            static void Postfix(ref bool __result, PermitResource __instance)
            {
                __result = true;
            }
        }

        public static Dictionary<string, int> ItemsDictionary = new Dictionary<string, int>();

        [HarmonyPatch(typeof(KleiItems))]
        [HarmonyPatch(nameof(KleiItems.GetOwnedItemCount))]
        public class GottaCatchEmAll_KleiItems_GetOwnedItemCount_Patch
        {
            /// <summary>
            /// Set the amount of any "un-owned" item to int.MaxValue
            /// </summary>
            /// <param name="itemType"></param>
            /// <param name="__result"></param>
            static void Postfix(string itemType, ref int __result)
            {
                ItemsDictionary[itemType] = __result;

                if (__result == 0)
                {
                    __result = int.MaxValue;
                }
            }
        }

        [HarmonyPatch(typeof(LocString))]
        [HarmonyPatch(nameof(LocString.Replace))]
        public class GottaCatchEmAll_LocString_Replace_Patch
        {
            /// <summary>
            /// Replace LocString that contains reference to owned items and the amount of item is int.MaxValue to say "rent" instead of the amount of item
            /// </summary>
            /// <param name="search"></param>
            /// <param name="replacement"></param>
            /// <param name="__result"></param>
            /// <param name="__instance"></param>
            /// <returns></returns>
            static bool Prefix(string search, string replacement, ref string __result, LocString __instance)
            {
                if (replacement == "2147483647")
                {
                    if (__instance == STRINGS.UI.KLEI_INVENTORY_SCREEN.ITEM_PLAYER_OWNED_AMOUNT_ICON)
                    {
                        __result = "Rented";
                        return false;
                    }
                    if (__instance == STRINGS.UI.KLEI_INVENTORY_SCREEN.ITEM_PLAYER_OWNED_AMOUNT)
                    {
                        __result = "My colony doesn't have any of these blueprints, but we rent one";
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(KleiInventoryScreen))]
        [HarmonyPatch("RefreshBarterPanel")]
        public class GottaCatchEmAll_RefreshBarterPanel_Patch
        {
            /// <summary>
            /// Re-Creation of oni default Barter panel to take in account "rented" items 
            /// </summary>
            /// <param name="__instance"></param>
            static void Postfix(KleiInventoryScreen __instance)
            {
                var buy = AccessTools.Field(typeof(KleiInventoryScreen), "barterBuyButton");

                var buyItem = buy?.GetValue(__instance) as KButton;

                var sell = AccessTools.Field(typeof(KleiInventoryScreen), "barterSellButton");

                var sellItem = sell?.GetValue(__instance) as KButton;

                var online = AccessTools.Field(typeof(KleiInventoryScreen), "IS_ONLINE");

                if(sellItem == null || buyItem == null)
                    return;
                
                if (!(online.GetValue(__instance) is bool value) || !value)
                {
                    sellItem.isInteractable = false;
                    buyItem.isInteractable = false;
                    return;
                }

                var selected = AccessTools.Property(typeof(KleiInventoryScreen), "SelectedPermit");

                var selectedItem = selected?.GetValue(__instance) as PermitResource;

                int ownCount = PermitItems.GetOwnedCount(selectedItem);

                HierarchyReferences buyHierarchy = buyItem.GetComponent<HierarchyReferences>();
                HierarchyReferences sellHierarchy = sellItem.GetComponent<HierarchyReferences>();
                LocText buyCostLabel = buyHierarchy.GetReference<LocText>("CostLabel");
                LocText sellCostLabel = sellHierarchy.GetReference<LocText>("CostLabel");

                Color color = new Color(0.6f, 0.9529412f, 0.5019608f);

                ulong buy_price, sell_price;

                var printabilityState = GetPermitPrintabilityState(selectedItem, ownCount, out buy_price, out sell_price, __instance);

                buyItem.isInteractable = printabilityState == PermitPrintabilityState.Printable;

                sellItem.isInteractable = (ownCount > 0 &&
                                           ownCount != int.MaxValue);

                sellItem.ClearOnClick();
                buyItem.ClearOnClick();

                var buyscreen = AccessTools.Field(typeof(KleiInventoryScreen), "barterConfirmationScreenPrefab");
                var buyscreenItem = buyscreen?.GetValue(__instance) as GameObject;

                switch (printabilityState)
                {
                    case PermitPrintabilityState.Printable:
                        buyItem.isInteractable = true;
                        buyItem.GetComponent<ToolTip>().SetSimpleTooltip(string.Format(STRINGS.UI.KLEI_INVENTORY_SCREEN.BARTERING.TOOLTIP_BUY_ACTIVE, buy_price.ToString()));
                        buyCostLabel.SetText("-" + buy_price);

                        buyItem.onClick += () =>
                        {
                            GameObject go = Util.KInstantiateUI(buyscreenItem, LockerNavigator.Instance.gameObject);
                            go.rectTransform().sizeDelta = Vector2.zero;
                            go.GetComponent<BarterConfirmationScreen>().Present(selectedItem, true);
                        };

                        break;
                    case PermitPrintabilityState.AlreadyOwned:
                        buyItem.isInteractable = false;
                        buyItem.GetComponent<ToolTip>().SetSimpleTooltip(STRINGS.UI.KLEI_INVENTORY_SCREEN.BARTERING.TOOLTIP_UNBUYABLE_ALREADY_OWNED);
                        buyCostLabel.SetText("-" + buy_price);
                        break;
                    case PermitPrintabilityState.TooExpensive:
                        buyItem.isInteractable = false;
                        buyItem.GetComponent<ToolTip>().SetSimpleTooltip(STRINGS.UI.KLEI_INVENTORY_SCREEN.BARTERING.TOOLTIP_BUY_CANT_AFFORD.text);
                        buyCostLabel.SetText("-" + buy_price);
                        break;
                    case PermitPrintabilityState.NotForSale:
                        buyItem.isInteractable = false;
                        buyItem.GetComponent<ToolTip>().SetSimpleTooltip(STRINGS.UI.KLEI_INVENTORY_SCREEN.BARTERING.TOOLTIP_UNBUYABLE);
                        buyCostLabel.SetText("");
                        break;
                    case PermitPrintabilityState.NotForSaleYet:
                        buyItem.isInteractable = false;
                        buyItem.GetComponent<ToolTip>().SetSimpleTooltip(STRINGS.UI.KLEI_INVENTORY_SCREEN.BARTERING.TOOLTIP_UNBUYABLE_BETA);
                        buyCostLabel.SetText("");
                        break;
                }


                if (sell_price == 0UL)
                {
                    sellItem.isInteractable = false;
                    sellItem.GetComponent<ToolTip>().SetSimpleTooltip((string)STRINGS.UI.KLEI_INVENTORY_SCREEN.BARTERING.TOOLTIP_UNSELLABLE);
                    sellCostLabel.SetText("");
                    sellCostLabel.color = Color.white;
                }
                else
                {
                    bool flag = ownCount > 0 && ownCount != int.MaxValue;
                    sellItem.isInteractable = flag;
                    sellItem.GetComponent<ToolTip>().SetSimpleTooltip(flag ? string.Format((string)STRINGS.UI.KLEI_INVENTORY_SCREEN.BARTERING.TOOLTIP_SELL_ACTIVE, (object)sell_price.ToString()) : STRINGS.UI.KLEI_INVENTORY_SCREEN.BARTERING.TOOLTIP_NONE_TO_SELL.text);
                    if (flag)
                    {
                        sellCostLabel.color = color;
                        sellCostLabel.SetText("+" + sell_price.ToString());
                    }
                    else
                    {
                        sellCostLabel.color = Color.white;
                        sellCostLabel.SetText("+" + sell_price.ToString());
                    }
                    sellItem.onClick += (System.Action)(() =>
                    {
                        GameObject go = Util.KInstantiateUI(buyscreenItem, LockerNavigator.Instance.gameObject);
                        go.rectTransform().sizeDelta = Vector2.zero;
                        go.GetComponent<BarterConfirmationScreen>().Present(selectedItem, false);
                    });
                }

                return;

            }

            static PermitPrintabilityState GetPermitPrintabilityState(PermitResource permit, int ownCount, out ulong buy_price, out ulong sell_price, KleiInventoryScreen __instance)
            {

                PermitItems.TryGetBarterPrice(permit.Id, out buy_price, out sell_price);

                if (buy_price == 0UL)
                {
                    return (permit.Rarity == PermitRarity.Universal || permit.Rarity == PermitRarity.Loyalty ||
                            permit.Rarity == PermitRarity.Unknown
                        ? PermitPrintabilityState.NotForSale
                        : PermitPrintabilityState.NotForSaleYet);
                }

                if (ownCount > 0 && ownCount != int.MaxValue)
                    return PermitPrintabilityState.AlreadyOwned;
                
                return (KleiItems.GetFilamentAmount() < buy_price ? PermitPrintabilityState.TooExpensive : PermitPrintabilityState.Printable);
            }
        }

        private enum PermitPrintabilityState
        {
            Printable,
            AlreadyOwned,
            TooExpensive,
            NotForSale,
            NotForSaleYet
        }

    }
}

