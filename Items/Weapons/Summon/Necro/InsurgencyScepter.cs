﻿using Aequus.Items.Misc;
using Aequus.Projectiles.Summon;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Aequus.Items.Weapons.Summon.Necro
{
    public class InsurgencyScepter : ZombieScepter
    {
        public override void SetDefaults()
        {
            Item.DefaultToNecromancy(50);
            Item.SetWeaponValues(25, 0.8f, 0);
            Item.shoot = ModContent.ProjectileType<InsurgentSkull>();
            Item.shootSpeed = 30f;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(gold: 5);
            Item.mana = 20;
            Item.UseSound = SoundID.Item8;
            Item.autoReuse = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<RevenantScepter>()
                .AddIngredient<Hexoplasm>(12)
                .AddIngredient(ItemID.SoulofNight, 8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}