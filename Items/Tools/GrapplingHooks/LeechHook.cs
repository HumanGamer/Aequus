﻿using Aequus.Common.Recipes;
using Aequus.Items.Accessories.Offense.Crit;
using Aequus.Projectiles.Misc.GrapplingHooks;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace Aequus.Items.Tools.GrapplingHooks
{
    public class LeechHook : ModItem
    {
        public static int ArmorPenetration = 5;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ArmorPenetration); 

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.damage = 6;
            Item.ArmorPenetration += ArmorPenetration;
            Item.knockBack = 0f;
            Item.shoot = ModContent.ProjectileType<LeechHookProj>();
            Item.shootSpeed = 13f;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Green;
            Item.noMelee = true;
            Item.value = ItemDefaults.ValueBloodMoon;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.RemoveCritChance();
            tooltips.RemoveKnockback();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.Aequus().leechHookNPC = -1;
            return true;
        }

        public override bool WeaponPrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            AequusRecipes.AddShimmerCraft(Type, ModContent.ItemType<HighSteaks>());
        }
    }
}