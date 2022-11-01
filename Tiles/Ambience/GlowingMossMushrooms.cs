﻿using Aequus.Items.Placeable.Nature.MossMushrooms;
using Aequus.Tiles.CrabCrevice;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Aequus.Tiles.Ambience
{
    public class GlowingMossMushrooms : ModTile
    {
        public const int Argon = 0;
        public const int Krypton = 1;
        public const int Xenon = 2;
        public const int Neon = 3;
        public const int Helium = 4;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLighted[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 18, };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.RandomStyleRange = 3;
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.LavaPlacement = LiquidPlacement.NotAllowed;
            TileObjectData.newTile.AnchorValidTiles = new int[]
            {
                TileID.Sand,
                TileID.Stone,
                TileID.Ebonstone,
                TileID.Crimstone,
                TileID.Pearlstone,
                TileID.BlueMoss,
                TileID.BrownMoss,
                TileID.GreenMoss,
                TileID.LavaMoss,
                TileID.PurpleMoss,
                TileID.RedMoss,
                TileID.ArgonMoss,
                TileID.ArgonMossBrick,
                TileID.KryptonMoss,
                TileID.KryptonMossBrick,
                TileID.XenonMoss,
                TileID.XenonMossBrick,
                TileID.GrayBrick,
                ModContent.TileType<SedimentaryRockTile>(),
            };
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(208, 0, 126), CreateMapEntryName("ArgonMushroom"));
            AddMapEntry(new Color(144, 254, 2), CreateMapEntryName("KryptonMushroom"));
            AddMapEntry(new Color(0, 197, 208), CreateMapEntryName("XenonMushroom"));
            AddMapEntry(new Color(208, 0, 160), CreateMapEntryName("NeonMushroom"));
            AddMapEntry(Color.White, CreateMapEntryName("HeliumMushroom"));
            HitSound = SoundID.Item10.WithPitchOffset(0.9f);
        }

        public override ushort GetMapOption(int i, int j) => (ushort)(Main.tile[i, j].TileFrameX / 108 % 3);

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            switch (Main.tile[i, j].TileFrameX / 108)
            {
                default:
                    {
                        r = 1.05f;
                        g = 0f;
                        b = 0.62f;
                    }
                    break;

                case 1:
                    {
                        r = 0.72f;
                        g = 1.4f;
                        b = 0f;
                    }
                    break;

                case 2:
                    {
                        r = 0f;
                        g = 1f;
                        b = 1.05f;
                    }
                    break;
            }
        }

        public override void RandomUpdate(int i, int j)
        {
            int reps = 20;
            int maxDist = 30;
            int frame = Main.tile[i, j].TileFrameX / 108;
            for (int o = 0; o < reps; o++)
            {
            Reset:
                int x = i + WorldGen.genRand.Next(-maxDist, maxDist);
                int y = j + WorldGen.genRand.Next(-maxDist, maxDist);
                var w = new Vector2(x * 16f, y * 16f);
                var m = new Vector2(i * 16f, j * 16f);
                if (!WorldGen.InWorld(x, y, 10) || !Main.tile[x, y].HasTile)
                {
                    continue;
                }
                int moss = TileID.ArgonMoss;
                switch (frame)
                {
                    case Krypton:
                        moss = TileID.KryptonMoss;
                        break;
                    case Xenon:
                        moss = TileID.XenonMoss;
                        break;
                }

                if (Main.tile[x, y].TileType == moss && reps < 40)
                {
                    reps += 4;
                    maxDist = 10;
                    i = x;
                    j = y;
                    goto Reset;
                }

                if (!Collision.CanHitLine(w + Vector2.Normalize(m - w) * 20f, 16, 16, m + Vector2.Normalize(w - m) * 20f, 16, 16))
                {
                    continue;
                }

                if (Main.tile[x, y].TileType == TileID.Stone || Main.tile[x, y].TileType == TileID.ArgonMoss || Main.tile[x, y].TileType == TileID.KryptonMoss || Main.tile[x, y].TileType == TileID.XenonMoss)
                {
                    if (AequusTile.GrowGrass(x, y, moss))
                    {
                        WorldGen.SquareTileFrame(x, y, resetFrame: true);
                        reps += 4;
                        maxDist = 10;
                        i = x;
                        j = y;
                        goto Reset;
                    }
                }
                else if (Main.tile[x, y].TileType == TileID.GrayBrick)
                {
                    switch (frame)
                    {
                        case Argon:
                            AequusTile.GrowGrass(x, y, TileID.ArgonMossBrick);
                            break;
                        case Krypton:
                            AequusTile.GrowGrass(x, y, TileID.KryptonMossBrick);
                            break;
                        case Xenon:
                            AequusTile.GrowGrass(x, y, TileID.XenonMossBrick);
                            break;
                    }
                }
            }
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            switch (Main.tile[i, j].TileFrameX / 108)
            {
                default:
                    type = DustID.ArgonMoss;
                    break;
                case 1:
                    type = DustID.KryptonMoss;
                    break;
                case 2:
                    type = DustID.XenonMoss;
                    break;
            }
            return true;
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            var source = new EntitySource_TileBreak(i, j);
            switch (frameX / 108)
            {
                default:
                    {
                        Item.NewItem(source, i * 16, j * 16, 32, 32, ModContent.ItemType<ArgonMushroom>());
                    }
                    break;

                case 1:
                    {
                        Item.NewItem(source, i * 16, j * 16, 32, 32, ModContent.ItemType<KryptonMushroom>());
                    }
                    break;

                case 2:
                    {
                        Item.NewItem(source, i * 16, j * 16, 32, 32, ModContent.ItemType<XenonMushroom>());
                    }
                    break;
            }
        }
    }
}