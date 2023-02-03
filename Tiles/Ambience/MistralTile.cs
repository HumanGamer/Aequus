﻿using Aequus.Graphics.Tiles;
using Aequus.Items.Misc.Pollen;
using Aequus.Items.Placeable.Nature;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Aequus.Tiles.Ambience
{
    public class MistralTile : HerbTileBase, ISpecialTileRenderer
    {
        public virtual int TurnFrames => 155;

        protected override int[] GrowableTiles => new int[]
        {
            TileID.Grass,
            TileID.HallowedGrass,
            TileID.Cloud,
            TileID.RainCloud,
            TileID.SnowCloud,
        };

        protected override Color MapColor => new Color(186, 122, 255, 255);
        public override Vector3 GlowColor => new Vector3(0.1f, 0.66f, 0.15f);
        protected override int DrawOffsetY => -8;

        public override bool IsBlooming(int i, int j)
        {
            return Main.WindyEnoughForKiteDrops;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            var clr = GlowColor;
            float multiplier = Math.Max(Main.tile[i, j].TileFrameX / 56, 0.1f);
            r = clr.X * multiplier;
            g = clr.Y * multiplier;
            b = clr.Z * multiplier;
        }

        public override bool Drop(int i, int j)
        {
            bool regrowth = Main.player[Player.FindClosest(new Vector2(i * 16f, j * 16f), 16, 16)].HeldItemFixed().type == ItemID.StaffofRegrowth;
            if (Main.tile[i, j].TileFrameX >= FrameShiftX)
            {
                Item.NewItem(new EntitySource_TileBreak(i, j), new Rectangle(i * 16, j * 16, 16, 16), ModContent.ItemType<MistralPollen>(), regrowth ? Main.rand.Next(1, 3) : 1);
            }
            if (CanBeHarvestedWithStaffOfRegrowth(i, j))
            {
                Item.NewItem(new EntitySource_TileBreak(i, j), new Rectangle(i * 16, j * 16, 16, 16), ModContent.ItemType<MistralSeeds>(), regrowth ? Main.rand.Next(1, 6) : Main.rand.Next(1, 4));
            }
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = 6;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (Main.tile[i, j].TileFrameX >= FrameWidth * 2)
            {
                SpecialTileRenderer.Add(i, j, TileRenderLayer.PreDrawVines);
            }
            var texture = TextureAssets.Tile[Type].Value;
            var effects = SpriteEffects.None;
            SetSpriteEffects(i, j, ref effects);
            var frame = new Rectangle(Main.tile[i, j].TileFrameX, 0, FrameWidth, FrameHeight);
            var offset = (AequusHelpers.TileDrawOffset - Main.screenPosition).Floor();
            var groundPosition = new Vector2(i * 16f + 8f, j * 16f + 16f).Floor();
            Main.spriteBatch.Draw(texture, groundPosition + offset, frame, Lighting.GetColor(i, j), 0f, new Vector2(FrameWidth / 2f, FrameHeight - 2f), 1f, effects, 0f);
            return false;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            return false;
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            frame = (frame + (int)(Main.windSpeedCurrent * 100)) % (int)(MathHelper.TwoPi * TurnFrames);
        }

        public void Render(int i, int j, TileRenderLayer layer)
        {
            var groundPosition = new Vector2(i * 16f + 8f, j * 16f + 16f).Floor();
            if (Main.tile[i, j].TileFrameX >= FrameWidth * 2)
            {
                var pinwheel = ModContent.Request<Texture2D>($"{this.GetPath()}_Pinwheel").Value;
                Main.spriteBatch.Draw(pinwheel, groundPosition - Main.screenPosition - new Vector2(0f, 20f), null, Lighting.GetColor(i, j),
                    Main.tileFrame[Type] / (float)TurnFrames, pinwheel.Size() / 2f, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}