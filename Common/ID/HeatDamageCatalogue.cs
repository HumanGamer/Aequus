﻿using System.Collections.Generic;
using Terraria.ID;

namespace Aequus.Common.ID
{
    public sealed class HeatDamageCatalogue : CatalogueBase
    {
        public static HashSet<int> HeatNPC { get; private set; }
        public static HashSet<int> HeatProjectile { get; private set; }

        public override void SetupVanillaEntries()
        {
            HeatNPC = new HashSet<int>()
            {
                NPCID.Lavabat,
                NPCID.LavaSlime,
                NPCID.FireImp,
                NPCID.MeteorHead,
                NPCID.HellArmoredBones,
                NPCID.HellArmoredBonesMace,
                NPCID.HellArmoredBonesSpikeShield,
                NPCID.HellArmoredBonesSword,
                NPCID.BlazingWheel,
            };

            HeatProjectile = new HashSet<int>()
            {
                ProjectileID.CultistBossFireBall,
                ProjectileID.CultistBossFireBallClone,
                ProjectileID.EyeFire,
                ProjectileID.GreekFire1,
                ProjectileID.GreekFire2,
                ProjectileID.GreekFire3,
            };
        }
    }
}
