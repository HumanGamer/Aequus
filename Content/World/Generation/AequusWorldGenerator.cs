﻿using Aequus.Common.Preferences;
using Aequus.Content.Biomes.CrabCrevice;
using Aequus.Content.Biomes.GoreNest;
using Aequus.Content.Biomes.RadonBiome;
using Aequus.Content.Biomes.UGForest;
using Aequus.Content.CursorDyes.Items;
using Aequus.Items.Accessories.Combat.Necro;
using Aequus.Items.Accessories.Combat.OnHit.Debuff;
using Aequus.Items.Accessories.Misc;
using Aequus.Items.Tools;
using Aequus.Items.Vanity.Pets.Light;
using Aequus.Items.Weapons.Melee;
using Aequus.Items.Weapons.Melee.Thrown;
using Aequus.Items.Weapons.Necromancy.Candles;
using Aequus.Items.Weapons.Necromancy.Scepters;
using Aequus.Items.Weapons.Ranged.Misc;
using Aequus.Tiles;
using Aequus.Tiles.Misc.BigGems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Social;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace Aequus.Content.World.Generation {
    public class AequusWorldGenerator : ModSystem {
        public static GoreNestGenerator GenGoreNest { get; private set; }
        public static RockmanChestGenerator RockmanGenerator { get; private set; }
        public static RadonCaveGenerator RadonCaves { get; private set; }
        public static CaveVarietyGenerator CaveVariety { get; private set; }
        public static List<Generator> Generators { get; internal set; }

        internal static int tileFrameLoop;

        public const int ShimmerEdgeDistance = 600;

        public override void Load() {
            CaveVariety = new CaveVarietyGenerator();
            RadonCaves = new RadonCaveGenerator();
            RockmanGenerator = new RockmanChestGenerator();
            GenGoreNest = new GoreNestGenerator();

            On_WorldGen.ShimmerMakeBiome += On_WorldGen_ShimmerMakeBiome;
            On_WorldGen.TileFrame += WorldGen_TileFrame;
            Terraria.IO.On_WorldFile.SaveWorld_bool_bool += WorldFile_SaveWorld_bool_bool;
        }

        private static bool On_WorldGen_ShimmerMakeBiome(On_WorldGen.orig_ShimmerMakeBiome orig, int X, int Y) {
            X = Math.Clamp(X, ShimmerEdgeDistance, Main.maxTilesX - ShimmerEdgeDistance);

            return orig(X, Y);
        }

        private static void WorldGen_TileFrame(On_WorldGen.orig_TileFrame orig, int i, int j, bool resetFrame, bool noBreak) {
            if (tileFrameLoop >= 100) {
                return;
            }

            tileFrameLoop++;
            orig(i, j, resetFrame, noBreak);
            tileFrameLoop--;
        }

        private static void WorldFile_SaveWorld_bool_bool(Terraria.IO.On_WorldFile.orig_SaveWorld_bool_bool orig, bool useCloudSaving, bool resetTime) {
            if (Generators != null && (!useCloudSaving || SocialAPI.Cloud != null)) {
                var stopwatch = new Stopwatch();
                for (int i = 0; i < Generators.Count; i++) {
                    var generator = Generators[i]; // Remove list index checks for the while(..) loop
                    stopwatch.Start();
                    while (generator.generating && stopwatch.ElapsedMilliseconds < 7500)
                    // Prevent world from saving while something is generating,
                    // giving it 7.5 seconds before determining it's going through an infinite loop
                    {
                    }
                    generator.generating = false;
                    stopwatch.Reset();
                }
            }
            orig(useCloudSaving, resetTime);
        }

        public override void AddRecipeGroups() {
            RockmanGenerator.PopulateInvalidTiles();
        }

        public override void Unload() {
            Generators?.Clear();
            Generators = null;
            CaveVariety = null;
            RadonCaves = null;
            RockmanGenerator = null;
            GenGoreNest = null;
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight) {
            AequusWorld.Structures = new();
            foreach (var g in Generators) {
                g.Initialize();
                g.AddPass(tasks, ref totalWeight);
            }
            if (GameplayConfig.Instance.CaveVariety > 0f) {
                AddPass("Wavy Caves", "Cave Variety", (progress, configuration) => {
                    progress.Message = Language.GetTextValue("Mods.Aequus.WorldGeneration.WeirdCaves");
                    CaveVariety.TallCaves();
                    CaveVariety.WeirdCaves();
                }, tasks);
            }
            AddPass("Dungeon", "Radon Biome", (progress, configuration) => {
                progress.Message = Language.GetTextValue("Mods.Aequus.WorldGeneration.RadonBiome");
                RadonCaves.GenerateWorld();
            }, tasks);
            AddPass("Statues", "Rockman Biome", (progress, configuration) => {
                progress.Message = Language.GetTextValue("Mods.Aequus.WorldGeneration.Rockman");
                RockmanGenerator.Generate(progress, configuration);
            }, tasks);

            AddPass("Gem Caves", "Bigger Gems", (progress, configuration) => {
                progress.Message = Language.GetTextValue("Mods.Aequus.WorldGeneration.BigGems");
                BigGemsTile.Generate();
            }, tasks);

            AddPass("Dungeon", "Crab Home", (progress, configuration) => {
                progress.Message = TextHelper.GetTextValue("WorldGeneration.CrabCrevice");
                ModContent.GetInstance<CrabCreviceGenerator>().Generate(progress, configuration);
            }, tasks);
            AddPass("Create Ocean Caves", "Crab Sand Fix", (progress, configuration) => {
                ModContent.GetInstance<CrabCreviceGenerator>().FixSand();
            }, tasks);
            AddPass("Moss", "Crab Growth", (progress, configuration) => {
                progress.Message = TextHelper.GetTextValue("WorldGeneration.CrabCreviceGrowth");
                var crabCreviceGen = ModContent.GetInstance<CrabCreviceGenerator>();
                crabCreviceGen.SetGenerationValues(progress, configuration);
                crabCreviceGen.FinalizeGeneration();
                crabCreviceGen.SetGenerationValues(null, null);
            }, tasks);
            AddPass("Pots", "Crab Pottery", (progress, configuration) => {
                progress.Message = TextHelper.GetTextValue("WorldGeneration.CrabCrevicePots");
                ModContent.GetInstance<CrabCreviceGenerator>().TransformPots();
            }, tasks);

            AddPass("Underworld", "Gore Nests", (progress, configuration) => {
                progress.Message = TextHelper.GetTextValue("WorldGeneration.GoreNests");
                GenGoreNest.Generate();
            }, tasks);
            AddPass("Tile Cleanup", "Gore Nest Cleanup", (progress, configuration) => {
                progress.Message = TextHelper.GetTextValue("WorldGeneration.GoreNestCleanup");
                GenGoreNest.Cleanup();
            }, tasks);
        }
        public static void AddPass(string task, string myName, WorldGenLegacyMethod generation, List<GenPass> tasks) {
            int i = tasks.FindIndex((t) => t.Name.Equals(task));
            if (i != -1)
                tasks.Insert(i + 1, new PassLegacy("Aequus: " + myName, generation));
        }
        public static void CopyPass(string name, int amt, List<GenPass> tasks) {
            int i = tasks.FindIndex((t) => t.Name.Equals(name));
            if (i != -1) {
                var t = tasks[i];
                for (int k = 0; k < amt; k++) {
                    tasks.Insert(i, new PassLegacy($"Aequus: {t.Name} {i}", (p, c) => t.Apply(p, c)));
                }
            }
        }
        public static void RemovePass(string name, List<GenPass> tasks) {
            int i = tasks.FindIndex((t) => t.Name.Equals(name));
            if (i != -1) {
                tasks[i] = new PassLegacy(tasks[i].Name, (p, c) => { });
            }
        }

        public override void PostWorldGen() {
            var rockmanChests = new List<int>();

            var placedItems = new HashSet<int>();
            var r = WorldGen.genRand;
            for (int k = 0; k < Main.maxChests; k++) {
                Chest c = Main.chest[k];
                if (c != null && WorldGen.InWorld(c.x, c.y, 40)) {
                    var tile = Main.tile[c.x, c.y];
                    var wall = tile.WallType;
                    if (wall == WallID.SandstoneBrick) {
                        continue;
                    }

                    int style = ChestType.GetStyle(c);
                    if (Main.tile[c.x, c.y].TileType == TileID.Containers) {
                        if (style == ChestType.Gold || style == ChestType.Marble || style == ChestType.Granite || style == ChestType.Mushroom || style == ChestType.RichMahogany) {
                            UndergroundChestLoot(k, c, rockmanChests, placedItems, r);
                        }
                        else if (style == ChestType.LockedGold && Main.wallDungeon[wall]) {
                            int choice = -1;
                            for (int i = 0; i < 4; i++) {
                                int item = DungeonChestItem(i);
                                if (!placedItems.Contains(item)) {
                                    choice = item;
                                }
                            }
                            if (choice == -1 && r.NextBool(4)) {
                                choice = DungeonChestItem(r.Next(4));
                            }

                            if (choice != -1) {
                                c.Insert(choice, 1);
                                placedItems.Add(choice);
                            }
                        }
                        else if (style == ChestType.Frozen) {
                            rockmanChests.Add(k);

                            if (r.NextBool(6)) {
                                AddGlowCore(c, placedItems);
                            }
                        }
                        else if (style == ChestType.Skyware || style == ChestType.LockedGold && !Main.wallDungeon[Main.tile[c.x, c.y].WallType] && c.y < (int)Main.worldSurface) {
                            if (!placedItems.Contains(ModContent.ItemType<Slingshot>()) || r.NextBool()) {
                                c.Insert(ModContent.ItemType<Slingshot>(), 1);
                                placedItems.Add(ModContent.ItemType<Slingshot>());
                            }
                        }
                    }
                    else if (Main.tile[c.x, c.y].TileType == TileID.Containers2) {
                        if (style == ChestType.DeadMans) {
                            UndergroundChestLoot(k, c, rockmanChests, placedItems, r);
                        }
                        else if (style == ChestType.Sandstone) {
                            rockmanChests.Add(k);
                        }
                    }
                }
            }
        }
        public static void UndergroundChestLoot(int k, Chest c, List<int> rockmanChests, HashSet<int> placedItems, UnifiedRandom r) {
            rockmanChests.Add(k);

            if (!placedItems.Contains(ModContent.ItemType<SwordCursor>()) || r.NextBool(20)) {
                for (int i = 0; i < Chest.maxItems; i++) {
                    if (c.item[i].IsAir) {
                        c.item[i].SetDefaults(ModContent.ItemType<SwordCursor>());
                        placedItems?.Add(ModContent.ItemType<SwordCursor>());
                        break;
                    }
                }
            }
            if (r.NextBool(5)) {
                AddGlowCore(c, placedItems);
            }

            switch (r.Next(5)) {
                case 0:
                    c.Insert(ModContent.ItemType<BoneRing>(), 1);
                    break;

                case 2:
                    c.Insert(ModContent.ItemType<Bellows>(), 1);
                    break;
            }
        }
        public static bool AddGlowCore(Chest c, HashSet<int> placedItems = null) {
            for (int i = 0; i < Chest.maxItems; i++) {
                if (!c.item[i].IsAir && (c.item[i].type == ItemID.Torch || c.item[i].type == ItemID.Glowstick)) {
                    if (WorldGen.genRand.NextBool() && placedItems.Contains(ModContent.ItemType<MiningPetSpawner>()) || !placedItems.Contains(ModContent.ItemType<GlowCore>())) {
                        c.item[i].SetDefaults(ModContent.ItemType<GlowCore>());
                        placedItems?.Add(ModContent.ItemType<GlowCore>());
                    }
                    else {
                        c.item[i].SetDefaults(ModContent.ItemType<MiningPetSpawner>());
                        placedItems?.Add(ModContent.ItemType<MiningPetSpawner>());
                    }
                    return true;
                }
            }
            return false;
        }
        public static int DungeonChestItem(int type) {
            return type switch {
                1 => ModContent.ItemType<Revenant>(),
                2 => ModContent.ItemType<DungeonCandle>(),
                3 => ModContent.ItemType<PandorasBox>(),
                _ => ModContent.ItemType<Valari>(),
            };
        }

        public static bool CanPlaceStructure(int middleX, int middleY, int width, int height, int padding = 0) {
            return CanPlaceStructure(new Rectangle(middleX - width / 2, middleY - height / 2, width, height), padding);
        }
        public static bool CanPlaceStructure(Rectangle rect, int padding = 0) {
            return GenVars.structures?.CanPlace(rect) != false;
        }

        public static bool CanPlaceStructure(int middleX, int middleY, int width, int height, bool[] invalidTiles, int padding = 0) {
            return CanPlaceStructure(new Rectangle(middleX - width / 2, middleY - height / 2, width, height), padding);
        }
        public static bool CanPlaceStructure(Rectangle rect, bool[] invalidTiles, int padding = 0) {
            return GenVars.structures?.CanPlace(rect, invalidTiles, padding) != false;
        }

        public override void PostUpdateWorld() {
            if (NPC.downedBoss1 && !AequusWorld.eyeOfCthulhuOres && GameplayConfig.Instance.EyeOfCthulhuOres) {
                var generator = ModContent.GetInstance<EOCOresGenerator>();
                generator.GenerateOnThread(null, null);
                TextHelper.Broadcast(generator.GetMessage(), TextHelper.EventMessage);
                AequusWorld.eyeOfCthulhuOres = true;
            }
            tileFrameLoop /= 2;
        }
    }
}