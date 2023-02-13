﻿using System;
using System.Collections.Generic;
using System.Threading;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace Aequus.Content.WorldGeneration
{
    public abstract class Generator : ModType
    {
        private GenerationProgress progress;
        private GameConfiguration config;
        internal bool generating;

        public bool Generating => generating;
        protected UnifiedRandom Rand => WorldGen.genRand;
        public virtual float Weight => 1f;

        protected sealed override void Register()
        {
            if (AequusWorldGenerator.Generators == null)
            {
                AequusWorldGenerator.Generators = new List<Generator>();
            }
            AequusWorldGenerator.Generators.Add(this);
        }

        public sealed override void SetupContent()
        {
            SetStaticDefaults();
        }

        public void GenerateOnThread(GenerationProgress progress = null, GameConfiguration config = null)
        {
            if (WorldGen.gen)
            {
                Generate(progress, config);
                return;
            }

            this.progress = progress;
            this.config = config;
            generating = true;
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                var me = (Generator)obj;
                try
                {
                    me.Generate();
                }
                catch (Exception ex)
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        me.Mod.Logger.Error($"{me.Name} failed when conducting generation...");
                        me.Mod.Logger.Error($"{ex.Message}\n{ex.StackTrace}");
                    });
                }
                me.generating = false;
                me.progress = null;
                me.config = null;
            }, this);
        }
        public void Generate(GenerationProgress progress = null, GameConfiguration config = null)
        {
            this.progress = progress;
            this.config = config;
            generating = true;
            try
            {
                Generate();
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"{Name} failed when conducting generation...");
                Mod.Logger.Error($"{ex.Message}\n{ex.StackTrace}");
                if (WorldGen.gen)
                {
                    throw ex;
                }
            }
            generating = false;
            this.progress = null;
            this.config = null;
        }

        protected abstract void Generate();

        protected void SetText(string text)
        {
            if (progress != null)
            {
                progress.Message = text;
            }
        }
        protected void SetProgress(float progress)
        {
            if (this.progress != null)
            {
                this.progress.CurrentPassWeight = progress;
            }
        }
    }
}
