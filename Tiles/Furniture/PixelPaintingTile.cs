﻿using Aequus.Common;
using Aequus.Graphics.RenderTargets;
using Aequus.Items.Tools.Camera;
using Aequus.Projectiles.Misc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;

namespace Aequus.Tiles.Furniture
{
    public abstract class PixelPaintingTile : ModTile
    {
        public override string Texture => Aequus.BlankTexture;

        public abstract int StateID { get; }

        public static int[] PhotoStateToTileID { get; private set; }

        public override void Load()
        {
            if (PhotoStateToTileID == null)
                PhotoStateToTileID = new int[PixelCameraProj.StateID.Count];
        }

        public override void SetStaticDefaults()
        {
            PhotoStateToTileID[StateID] = Type;
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.Width = PixelCameraProj.DimensionsForState[StateID].X;
            TileObjectData.newTile.Height = PixelCameraProj.DimensionsForState[StateID].Y;
            TileObjectData.newTile.CoordinateHeights = new int[PixelCameraProj.DimensionsForState[StateID].Y];
            for (int i = 0; i < TileObjectData.newTile.CoordinateHeights.Length; i++)
            {
                TileObjectData.newTile.CoordinateHeights[i] = 16;
            }
            TileObjectData.newTile.Origin = new Point16(TileObjectData.newTile.Width / 2, TileObjectData.newTile.Height / 2);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(ModContent.GetInstance<TEPixelPainting>().Hook_AfterPlacement, -1, 0, false);

            TileObjectData.addTile(Type);
            DustType = DustID.WoodFurniture;
            AddMapEntry(new Color(120, 85, 60), Language.GetText("MapObject.Painting"));
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            //Item.NewItem(i * 16, j * 16, 32, 48, ModContent.ItemType<Items.Placeable.ElementalPurge>());
            ModContent.GetInstance<TEPixelPainting>().Kill(i, j);
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            int x = i - Main.tile[i, j].TileFrameX / 18;
            int y = j - Main.tile[i, j].TileFrameY / 18;
            if (TileEntity.ByPosition.TryGetValue(new Point16(x, y), out var te) && te is TEPixelPainting painting)
            {
                if (painting.texture == null)
                {
                    painting.texture = new Ref<RenderTarget2D>();
                }
                if (painting.texture.Value == null)
                {
                    painting.mapCache.UpdateColorLookup();
                    if (Main.tile[i, j].TileFrameX == 0 && Main.tile[i, j].TileFrameY == 0)
                    {
                        ColorImageRenderer.RenderRequests.Add(new ColorImageRenderer.RequestInfo()
                        { width = painting.mapCache.width, height = painting.mapCache.height, arr = painting.mapCache.colorLookup, target = painting.texture, });
                    }
                }
                else if (painting.texture != null && painting.texture.Value != null)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                    Main.spriteBatch.Draw(painting.texture.Value, new Vector2(i * 16f, j * 16f) + AequusHelpers.TileDrawOffset - Main.screenPosition,
                        new Rectangle(Main.tile[i, j].TileFrameX / 2, Main.tile[i, j].TileFrameY / 2, 8, 8), Lighting.GetColor(i, j), 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin();
                }
            }
            return false;
        }
    }

    // screw tiles, honestly.
    public class PixelPainting_2x2 : PixelPaintingTile
    {
        public override int StateID => PixelCameraProj.StateID.State_2x2;
    }
    public class PixelPainting_2x3 : PixelPaintingTile
    {
        public override int StateID => PixelCameraProj.StateID.State_2x3;
    }
    public class PixelPainting_3x2 : PixelPaintingTile
    {
        public override int StateID => PixelCameraProj.StateID.State_3x2;
    }
    public class PixelPainting_3x3 : PixelPaintingTile
    {
        public override int StateID => PixelCameraProj.StateID.State_3x3;
    }
    public class PixelPainting_6x4 : PixelPaintingTile
    {
        public override int StateID => PixelCameraProj.StateID.State_6x4;
    }

    public class TEPixelPainting : ModTileEntity
    {
        public MapTileCache mapCache;
        public Ref<RenderTarget2D> texture;
        private int netSpam = -1;

        public override bool IsTileValidForEntity(int x, int y)
        {
            return Main.tile[x, y].TileType > Main.maxTileSets && TileLoader.GetTile(Main.tile[x, y].TileType) is PixelPaintingTile;
        }

        public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
        {
            if (Main.netMode == NetmodeID.Server || Main.LocalPlayer.HeldItemFixed()?.ModItem is not PixelCameraClip clip || type < Main.maxTileSets || TileLoader.GetTile(type) is not PixelPaintingTile painting)
            {
                return -1;
            }
            int offX = PixelCameraProj.DimensionsForState[painting.StateID].X / 2;
            int offY = PixelCameraProj.DimensionsForState[painting.StateID].Y / 2;
            int x = i - offX;
            int y = j - offY;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendTileSquare(Main.myPlayer, x, y, 6, 4);
                var p = Aequus.GetPacket(PacketType.PlacePixelPainting);
                p.Write(x);
                p.Write(y);
                clip.mapCache.NetSend(p);
                p.Send();
                return -1;
            }
            int id = Place(x, y);
            if (ByID.TryGetValue(id, out var te) && te is TEPixelPainting paintingTE)
            {
                paintingTE.mapCache = clip.mapCache;
            }
            return id;
        }

        public override void Update()
        {
            if (netSpam > 0)
                netSpam--;
        }

        public override void SaveData(TagCompound tag)
        {
            if (mapCache.mapTiles == null)
                return;
            tag["Width"] = mapCache.width;
            tag["Height"] = mapCache.height;
            tag["MapTileIDs"] = Array.ConvertAll(mapCache.mapTiles, (t) => t.Type);
            tag["MapTileColor"] = Array.ConvertAll(mapCache.mapTiles, (t) => t.Color);
        }

        public override void LoadData(TagCompound tag)
        {
            if (!tag.ContainsKey("MapTileIDs"))
                return;
            mapCache.width = tag.Get<int>("Width");
            mapCache.height = tag.Get<int>("Height");
            var mapTileIDs = tag.Get<ushort[]>("MapTileIDs");
            var mapTilePaints = tag.Get<byte[]>("MapTileColor");

            mapCache.colorLookup = new Color[mapCache.width * mapCache.height];
            mapCache.mapTiles = new MapTile[mapCache.width * mapCache.height];
            mapCache.UpdateColorLookup();
            if (mapTileIDs.Length != mapTilePaints.Length || mapTileIDs.Length != mapCache.colorLookup.Length || mapTilePaints.Length != mapCache.colorLookup.Length)
                return;

            for (int i = 0; i < mapCache.mapTiles.Length; i++)
            {
                mapCache.mapTiles[i] = MapTile.Create(mapTileIDs[i], byte.MaxValue, mapTilePaints[i]);
            }
        }
        public override void NetSend(BinaryWriter writer)
        {
            base.NetSend(writer);
            writer.Write(netSpam > 0);
            if (netSpam > 0)
            {
                return;
            }
            netSpam += 30;
            if (netSpam < 0)
            {
                return;
            }
            mapCache.NetSend(writer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            base.NetReceive(reader);
            if (reader.ReadBoolean())
                return;
            mapCache = MapTileCache.NetReceive(reader);
        }

        public override void OnNetPlace()
        {
            netSpam = -30;
        }
    }
}