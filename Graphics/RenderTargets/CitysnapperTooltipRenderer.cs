﻿using Aequus.Common;
using Aequus.Items.Tools.CarpenterTools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Capture;
using Terraria.ID;
using Terraria.ModLoader;

namespace Aequus.Graphics.RenderTargets
{
    public class CitysnapperTooltipRenderer : RequestableRenderTarget
    {
        public static List<CitysnapperClip> renderRequests;
        public static int tileSize = 16;
        public static int tilePadding = 6;

        public override void Load(Mod mod)
        {
            base.Load(mod);
            renderRequests = new List<CitysnapperClip>();
        }

        public override void Unload()
        {
            tileSize = 16;
            tilePadding = 12;
            renderRequests?.Clear();
        }

        protected override void PrepareRenderTargetsForDrawing(GraphicsDevice device, SpriteBatch spriteBatch)
        {
            tileSize = 16;
            tilePadding = 12;
            if (renderRequests.Count == 0 || renderRequests[0].tileMap == null || renderRequests[0].TooltipTexture == null)
                return;

            int size = tileSize;
            int sub = tilePadding * tileSize;
            PrepareARenderTarget_WithoutListeningToEvents(ref _target, device, renderRequests[0].tileMap.Width * size - sub, renderRequests[0].tileMap.Height * size - sub, RenderTargetUsage.PreserveContents);
            PrepareARenderTarget_WithoutListeningToEvents(ref helperTarget, device, renderRequests[0].tileMap.Width * size - sub, renderRequests[0].tileMap.Height * size - sub, RenderTargetUsage.DiscardContents);
        }

        protected override void DrawOntoTarget(GraphicsDevice device, SpriteBatch spriteBatch)
        {
            if (renderRequests.Count == 0)
                return;

            if (renderRequests[0].tileMap == null || renderRequests[0].tileMap == null)
            {
                renderRequests.RemoveAt(0);
                return;
            }

            var tileMap = renderRequests[0].tileMap;
            var texture = renderRequests[0].TooltipTexture;

            DrawCapture(device, spriteBatch, tileMap, renderRequests[0]);

            spriteBatch.Begin();
            device.SetRenderTarget(_target);
            device.Clear(Color.Transparent);

            spriteBatch.Draw(helperTarget, new Rectangle(0, 0, helperTarget.Width, helperTarget.Height), Color.White);

            spriteBatch.End();

            texture.Value = _target;

            _target = null;

            renderRequests.RemoveAt(0);

        }
        private void DrawCapture(GraphicsDevice device, SpriteBatch spriteBatch, TileMapCache map, CitysnapperClip clip)
        {
            var area = map.Area;

            area.X = (int)(clip.worldXPercent * Main.maxTilesX);
            area.Y = (int)(clip.worldYPercent * Main.maxTilesY);

            area = area.Fluffize(10);

            var entities = new List<Entity>();
            for (int i = 0; i < Main.maxItems; i++)
            {
                if (Main.item[i].active)
                {
                    entities.Add(Main.item[i]);
                    Main.item[i].active = false;
                }
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active)
                {
                    entities.Add(Main.npc[i]);
                    Main.npc[i].active = false;
                }
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active)
                {
                    entities.Add(Main.projectile[i]);
                    Main.projectile[i].active = false;
                }
            }

            var myPlayer = Main.LocalPlayer.position;
            Main.BlackFadeIn = 255;
            Main.LocalPlayer.position = new Vector2(area.X * 16 - Main.LocalPlayer.width * 2f, area.Y * 16 - Main.LocalPlayer.height * 2f);
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active)
                {
                    entities.Add(Main.player[i]);
                    Main.player[i].active = false;
                }
            }

            var gore = new List<Gore>();
            for (int i = 0; i < Main.maxGore; i++)
            {
                if (Main.gore[i].active)
                {
                    gore.Add(Main.gore[i]);
                    Main.gore[i].active = false;
                }
            }

            var dust = new List<Dust>();
            for (int i = 0; i < Main.maxGore; i++)
            {
                if (Main.dust[i].active)
                {
                    dust.Add(Main.dust[i]);
                    Main.dust[i].active = false;
                }
            }

            var rain = new List<Rain>();
            for (int i = 0; i < Main.maxRain; i++)
            {
                if (Main.rain[i] != null && Main.rain[i].active)
                {
                    rain.Add(Main.rain[i]);
                    Main.rain[i].active = false;
                }
            }

            var renderArea = area;
            renderArea.X += tilePadding / 2;
            renderArea.Y += tilePadding / 2;
            renderArea.Width -= tilePadding;
            renderArea.Height -= tilePadding;

            //AequusHelpers.dustDebug(area.WorldRectangle());
            //AequusHelpers.dustDebug(renderArea.WorldRectangle(), DustID.CursedTorch);

            var oldMap = new TileMapCache(area);

            for (int i = 0; i < map.Width; i++)
            {
                for (int j = 0; j < map.Height; j++)
                {
                    var p = new Point(area.X + i, area.Y + j);
                    if (i == 0 || j == 0 || i == map.Width - 1 || j == map.Height - 1)
                    {
                        Main.tile[p].Get<TileTypeData>() = new TileTypeData() { Type = TileID.DiamondGemsparkOff, };
                        Main.tile[p].Get<LiquidData>() = map[i, j].Liquid;
                        Main.tile[p].Get<TileWallWireStateData>() = map[i, j].Misc;
                        Main.tile[p].Get<WallTypeData>() = map[i, j].Wall;
                        Main.tile[p].Active(value: true);
                        continue;
                    }
                    Main.tile[p].Get<TileTypeData>() = map[i, j].Type;
                    Main.tile[p].Get<LiquidData>() = map[i, j].Liquid;
                    Main.tile[p].Get<TileWallWireStateData>() = map[i, j].Misc;
                    Main.tile[p].Get<WallTypeData>() = map[i, j].Wall;
                }
            }

            for (int i = 0; i < area.Width; i++)
            {
                for (int j = 0; j < area.Height; j++)
                {
                }
            }

            var time = Main.time;
            bool daytime = Main.dayTime;
            Main.dayTime = clip.daytime;
            Main.time = clip.time;

            try
            {
                Main.LocalPlayer.ForceUpdateBiomes();
                for (int i = 0; i < 5; i++)
                    Main.instance.DrawCapture(renderArea, new CaptureSettings() { Area = renderArea, CaptureBackground = true, CaptureEntities = true, CaptureMech = false, UseScaling = false, Biome = CaptureBiome.GetCaptureBiome(-1), });
            }
            catch
            {
            }

            Main.dayTime = daytime;
            Main.time = time;

            for (int i = 0; i < area.Width; i++)
            {
                for (int j = 0; j < area.Height; j++)
                {
                    var p = new Point(area.X + i, area.Y + j);
                    Main.tile[p].Get<TileTypeData>() = oldMap[i, j].Type;
                    Main.tile[p].Get<LiquidData>() = oldMap[i, j].Liquid;
                    Main.tile[p].Get<TileWallWireStateData>() = oldMap[i, j].Misc;
                    Main.tile[p].Get<WallTypeData>() = oldMap[i, j].Wall;
                }
            }

            for (int i = 0; i < area.Width; i++)
            {
                for (int j = 0; j < area.Height; j++)
                {
                    WorldGen.SquareWallFrame(area.X + i, area.Y + j);
                    WorldGen.SquareTileFrame(area.X + i, area.Y + j);
                }
            }

            foreach (var n in entities)
            {
                n.active = true;
            }

            foreach (var g in gore)
            {
                g.active = true;
            }

            foreach (var d in dust)
            {
                d.active = true;
            }

            foreach (var r in rain)
            {
                r.active = true;
            }

            Main.LocalPlayer.position = myPlayer;
        }

        protected override bool SelfRequest()
        {
            return renderRequests.Count > 0;
        }
    }
}