﻿using Aequus.Projectiles.Misc.Drones;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Aequus.Content.DronePylons
{
    public abstract class DroneType : ModType, TagSerializable
    {
        public abstract int ProjectileType { get; }
        public virtual int ProjectileAmt => 1;
        public int Count
        {
            get
            {
                int numOwned = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<GunnerDrone>())
                    {
                        var gunner = Main.projectile[i].ModProjectile<GunnerDrone>();
                        if (gunner.pylonSpot == Location)
                        {
                            numOwned++;
                        }
                    }
                }
                return numOwned;
            }
        }

        public Point Location { get; internal set; }

        internal static Dictionary<string, DroneType> KeyToDroneType { get; private set; }

        protected sealed override void Register()
        {
            ModTypeLookup<DroneType>.Register(this);
            if (KeyToDroneType == null)
            {
                KeyToDroneType = new Dictionary<string, DroneType>();
            }
            KeyToDroneType.Add(FullName, this);
        }

        public virtual TagCompound SerializeData()
        {
            return new TagCompound()
            {
                ["Mod"] = Mod.Name,
                ["Name"] = Name,
            };
        }

        public static List<TagCompound> SerializeData(List<DroneType> drones)
        {
            if (drones == null || drones.Count == 0)
            {
                return null;
            }
            var l = new List<TagCompound>();
            foreach (var d in drones)
            {
                l.Add(d.SerializeData());
            }
            return l;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}