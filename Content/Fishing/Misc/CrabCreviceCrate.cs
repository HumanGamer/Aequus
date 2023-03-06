﻿using Aequus.Content.Fishing.Bait;
using Aequus.Items.Accessories.Offense;
using Aequus.Items.Accessories.Utility;
using Aequus.Items.Weapons.Magic;
using Aequus.Items.Weapons.Ranged.Misc;
using Aequus.Tiles.Furniture;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Aequus.Content.Fishing.Misc
{
    public class CrabCreviceCrate : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 10;
            CrateBait.BiomeCrates.Add(new CrateBait.BiomeCrateFishingInfo((f, p) => p.Aequus().ZoneCrabCrevice, Type, ModContent.ItemType<CrabCreviceCrateHard>()));
        }

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            var l = new ItemLoot(ItemID.OceanCrateHard, Main.ItemDropsDB).Get(includeGlobalDrops: false);
            foreach (var loot in l)
            {
                if (loot is AlwaysAtleastOneSuccessDropRule oneFromOptions)
                {
                    itemLoot.Add(ItemDropRule.OneFromOptions(1,
                        ModContent.ItemType<StarPhish>(), ModContent.ItemType<DavyJonesAnchor>(), ModContent.ItemType<ArmFloaties>()));
                    continue;
                }
                itemLoot.Add(loot);
            }
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.OceanCrate);
            Item.createTile = ModContent.TileType<FishingCrates>();
            Item.placeStyle = FishingCrates.CrabCreviceCrate;
        }

        public override bool CanRightClick()
        {
            return true;
        }
    }
}