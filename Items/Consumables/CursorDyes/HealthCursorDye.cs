﻿using Aequus.Content.CursorDyes;
using Aequus.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Aequus.Items.Consumables.CursorDyes
{
    public class HealthCursorDye : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            ShopQuotes.Database.GetNPC(NPCID.DyeTrader).AddModItemQuote(Type);
            CursorDyeSystem.Register(Type, new ColorChangeCursor(() => Color.Lerp(Color.Black, Color.Red, MathHelper.Clamp(Main.LocalPlayer.statLife / (float)Main.LocalPlayer.statLifeMax2, 0f, 1f))));
        }

        public override void SetDefaults()
        {
            Item.DefaultToCursorDye();
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(gold: 1);
        }

        public override bool CanUseItem(Player player)
        {
            return player.Aequus().CursorDye != CursorDyeSystem.ItemIDToCursor(Type).Type;
        }

        public override bool? UseItem(Player player)
        {
            player.Aequus().CursorDye = CursorDyeSystem.ItemIDToCursor(Type).Type;
            return true;
        }
    }
}