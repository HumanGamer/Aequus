﻿using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Aequus.Common.Players
{
    public class SacrificingPlayer : ModPlayer
    {
        public struct LifeSacrifice
        {
            public int time;
            public int amtTaken;
            public bool physicallyHitPlayer = false;
            public PlayerDeathReason reason = null;

            public LifeSacrifice(int amtTaken, int time = 0, bool hitPlayer = false, PlayerDeathReason reason = null)
            {
                this.amtTaken = amtTaken;
                this.time = time;
                physicallyHitPlayer = hitPlayer;
                this.reason = reason;
            }
        }

        public List<LifeSacrifice> sacrifices;

        public override void Initialize()
        {
            sacrifices = new List<LifeSacrifice>();
        }

        public override void UpdateDead()
        {
            sacrifices.Clear();
        }

        public override void PostUpdateEquips()
        {
            for (int i = 0; i < sacrifices.Count; i++)
            {
                var s = sacrifices[i];
                s.time--;
                if (s.time <= 0)
                {
                    var reason = s.reason ?? PlayerDeathReason.ByOther(4);
                    if (s.physicallyHitPlayer)
                    {
                        Player.Hurt(reason, s.amtTaken, -Player.direction);
                    }
                    else
                    {
                        Player.statLife -= s.amtTaken;
                        if (Player.statLife <= 0)
                        {
                            Player.KillMe(reason, s.amtTaken, -Player.direction);
                        }
                    }
                    sacrifices.RemoveAt(i);
                    i--;
                    continue;
                }
                sacrifices[i] = s;
            }
        }

        public void SacrificeLife(int amt, int frames = 1, int separation = 1, bool hitPlayer = false, PlayerDeathReason reason = null)
        {
            if (amt < frames || frames < 2)
            {
                sacrifices.Add(new LifeSacrifice(amt, 0));
                return;
            }
            int lifeTaken = amt / frames;
            for (int i = 0; i < frames - 1; i++)
            {
                sacrifices.Add(new LifeSacrifice(lifeTaken, i * separation, hitPlayer, reason));
            }
            sacrifices.Add(new LifeSacrifice(lifeTaken + (amt - lifeTaken * frames), frames * separation, hitPlayer, reason));
        }

        public override void clientClone(ModPlayer clientClone)
        {
            var clone = (SacrificingPlayer)clientClone;
            clone.sacrifices = new List<LifeSacrifice>();
            foreach (var l in sacrifices)
            {
                clone.sacrifices.Add(l);
            }
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
        }
    }
}