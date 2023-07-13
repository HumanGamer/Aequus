﻿using Aequus.Items;
using Aequus.Items.Accessories.Combat.Passive.Stormcloak;
using Aequus.Items.Weapons.Magic.Misc.GaleStreams.WindFan;
using Aequus.Items.Weapons.Melee.Misc.PhaseDisc;
using Aequus.NPCs.Monsters.BossMonsters.DustDevil;
using Terraria.ID;
using Terraria.ModLoader;

namespace Aequus.Items.Consumables.TreasureBag {
    public class DustDevilBag : TreasureBagBase {
        protected override int InternalRarity => ItemRarityID.LightPurple;
        protected override bool PreHardmode => true;

        public override void SetDefaults() {
            base.SetDefaults();
            Item.Aequus().itemGravityCheck = 255;
        }

        public override void ModifyItemLoot(ItemLoot itemLoot) {
            this.CreateLoot(itemLoot)
                .Add<Stormcloak>(chance: 1, stack: 1)
                .AddOptions(chance: 1, ModContent.ItemType<PhaseDisc>(), ModContent.ItemType<WindFan>())
                .Coins<DustDevil>();
        }
    }
}