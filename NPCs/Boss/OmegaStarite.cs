﻿using Aequus.Buffs.Debuffs;
using Aequus.Common;
using Aequus.Common.Configuration;
using Aequus.Common.ItemDrops;
using Aequus.Common.Utilities;
using Aequus.Content.CrossMod;
using Aequus.Content.Invasions;
using Aequus.Effects;
using Aequus.Effects.Prims;
using Aequus.Items.Consumables.TreasureBags;
using Aequus.Items.Misc;
using Aequus.Items.Misc.Energies;
using Aequus.Items.Misc.Pets;
using Aequus.Items.Placeable;
using Aequus.Items.Weapons.Ranged;
using Aequus.Particles.Dusts;
using Aequus.Projectiles.Boss;
using Aequus.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace Aequus.NPCs.Boss
{
    [AutoloadBossHead()]
    public class OmegaStarite : AequusBoss
    {
        public const float Circumference = 120;
        public const float Radius = Circumference / 2f;
        private const float DEATH_TIME = MathHelper.PiOver4 * 134;

        public const int InnerRingSegmentCount = 5;
        public const float InnerRingScale = 1f;

        public const int OuterRingSegmentCount = 8;
        public const float OuterRingScale_Normal = 1.1f;
        public const float OuterRingSegment_Expert = 1.2f;
        public const float OuterRingCircumferenceMultiplier_Normal = 1.5f;
        public const float OuterRingCircumferenceMultiplier_Expert = 1.75f;

        public class Ring
        {
            public readonly byte amountOfSegments;
            public readonly float rotationOrbLoop;
            public readonly Vector3[] CachedPositions;
            public readonly Rectangle[] CachedHitboxes;

            public float pitch;
            public float roll;
            public float yaw;
            public float radiusFromOrigin;

            public float OriginalRadiusFromOrigin { get; private set; }
            public float Scale { get; private set; }

            public Vector3 rotationVelocity;

            public Ring(int amount, float radiusFromOrigin, float scale)
            {
                amountOfSegments = (byte)amount;
                rotationOrbLoop = MathHelper.TwoPi / amountOfSegments;
                OriginalRadiusFromOrigin = radiusFromOrigin;
                this.radiusFromOrigin = OriginalRadiusFromOrigin;
                Scale = scale;
                CachedPositions = new Vector3[amountOfSegments];
                CachedHitboxes = new Rectangle[amountOfSegments];
            }

            /// <summary>
            /// Creates a Ring through a net package
            /// </summary>
            /// <param name="reader"></param>
            public Ring(BinaryReader reader)
            {
                amountOfSegments = reader.ReadByte();
                rotationOrbLoop = MathHelper.TwoPi / amountOfSegments;
                OriginalRadiusFromOrigin = reader.ReadSingle();
                radiusFromOrigin = OriginalRadiusFromOrigin;
                Scale = reader.ReadSingle();
                CachedPositions = new Vector3[amountOfSegments];
                CachedHitboxes = new Rectangle[amountOfSegments];
            }

            public static Ring[] FromNetPackage(BinaryReader reader)
            {
                byte amount = reader.ReadByte();
                var rings = new Ring[amount];
                for (byte i = 0; i < amount; i++)
                {
                    rings[i] = new Ring(reader);
                }
                return rings;
            }

            public void Update(Vector2 origin)
            {
                pitch += rotationVelocity.X;
                roll += rotationVelocity.Y;
                yaw = (yaw + rotationVelocity.Z) % rotationOrbLoop;
                int i = 0;
                for (float r = 0f; i < amountOfSegments; r += rotationOrbLoop)
                {
                    CachedPositions[i] = Vector3.Transform(new Vector3(radiusFromOrigin, 0f, 0f), Matrix.CreateFromYawPitchRoll(pitch, roll, r + yaw)) + new Vector3(origin, 0f);
                    CachedHitboxes[i] = Utils.CenteredRectangle(new Vector2(CachedPositions[i].X, CachedPositions[i].Y), new Vector2(50f, 50f) * Scale);
                    i++;
                }
            }

            public void MultScale(float scale)
            {
                OriginalRadiusFromOrigin *= scale;
                radiusFromOrigin *= scale;
                Scale *= scale;
            }

            public static void SendNetPackage(BinaryWriter writer, Ring[] rings)
            {
                for (byte i = 0; i < rings.Length; i++)
                {
                    rings[i].SendNetPackage(writer);
                }
            }

            public void SendNetPackage(BinaryWriter writer)
            {
                writer.Write(pitch);
                writer.Write(roll);
                writer.Write(yaw);
            }

            public void RecieveNetPackage(BinaryReader reader)
            {
                pitch = reader.ReadSingle();
                roll = reader.ReadSingle();
                yaw = reader.ReadSingle();
            }
        }

        public static HashSet<int> StarResistCatalogue { get; private set; }
        public static HashSet<int> StarResistEasterEggCatalogue { get; private set; }

        private LegacyPrimRenderer prim;

        public Ring[] rings;
        public float starDamageMultiplier;
        private byte _hitShake;

        public override void Load()
        {
            StarResistEasterEggCatalogue = new HashSet<int>()
            {
                ProjectileID.FallingStar,
                ProjectileID.StarCannonStar,
                ProjectileID.SuperStar,
            };
            StarResistCatalogue = new HashSet<int>(StarResistEasterEggCatalogue)
            {
                ProjectileID.StarCloakStar,
                ProjectileID.Starfury,
                ProjectileID.StarVeilStar,
                ProjectileID.StarWrath,
                ProjectileID.BeeCloakStar,
                ProjectileID.HallowStar,
                ProjectileID.ManaCloakStar,
                ProjectileID.SuperStarSlash,
            };
        }

        public override void Unload()
        {
            StarResistEasterEggCatalogue?.Clear();
            StarResistEasterEggCatalogue = null;
        }

        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailingMode[NPC.type] = 7;
            NPCID.Sets.TrailCacheLength[NPC.type] = 60;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Position = new Vector2(0f, 2f),
            });
            NPCID.Sets.DebuffImmunitySets[NPC.type] = new NPCDebuffImmunityData() { ImmuneToAllBuffsThatAreNotWhips = true, };
            Main.npcFrameCount[NPC.type] = 14;
        }

        public override void SetDefaults()
        {
            NPC.width = 120;
            NPC.height = 120;
            NPC.lifeMax = 9500;
            NPC.damage = 50;
            NPC.defense = 18;
            NPC.DeathSound = SoundID.NPCDeath55;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.value = Item.buyPrice(gold: 18);
            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.noTileCollide = true;
            NPC.trapImmune = true;
            NPC.lavaImmune = true;

            starDamageMultiplier = 0.8f;

            if (Main.getGoodWorld)
            {
                NPC.scale *= 0.5f;
                starDamageMultiplier *= 0.5f;
            }

            if (AprilFools.CheckAprilFools())
            {
                NPC.GivenName = "Omega Starite, Living Galaxy the Omega Being";
            }
            //if (!Glimmer.IsGlimmerEventCurrentlyActive())
            //if (AQMod.UseAssets)
            //{
            //    music = GetMusic().GetMusicID();
            //    musicPriority = MusicPriority.BossMedium;
            //}
        }

        public override Color? GetAlpha(Color drawColor)
        {
            return new Color(255, 255, 255, 240);
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            if (Main.expertMode)
            {
                starDamageMultiplier *= 0.8f;
            }
            NPC.lifeMax = (int)(NPC.lifeMax * 0.8f) + 4000 * numPlayers;
            //if (AQMod.calamityMod.IsActive)
            //{
            //    NPC.lifeMax = (int)(NPC.lifeMax * 2f);
            //    NPC.damage *= 2;
            //    NPC.defense *= 2;
            //}
        }

        public const int PHASE_HYPER_STARITE_PART2_ALT = 8;
        public const int PHASE_OMEGA_LASER_PART0 = 7;
        public const int PHASE_OMEGA_LASER = 6;
        public const int PHASE_STAR_BULLETS = 5;
        public const int PHASE_ASSAULT_PLAYER = 4;
        public const int PHASE_HYPER_STARITE_PART2 = 3;
        public const int PHASE_HYPER_STARITE_PART1 = 2;
        public const int PHASE_HYPER_STARITE_PART0 = 1;
        public const int PHASE_INIT = 0;
        public const int PHASE_DEAD = -1;
        public const int PHASE_NOVA = -2;
        public const int PHASE_GOODBYE = -3;

        private bool PlrCheck()
        {
            NPC.TargetClosest(faceTarget: false);
            NPC.netUpdate = true;
            if (Main.player[NPC.target].dead)
            {
                NPC.ai[0] = PHASE_GOODBYE;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.ai[3] = 0f;
                NPC.localAI[0] = 0f;
                NPC.localAI[1] = 0f;
                NPC.localAI[2] = 0f;
                NPC.localAI[3] = 0f;
                return false;
            }
            return true;
        }

        public void Initialize(bool bestiaryDummy = false)
        {
            if (!bestiaryDummy)
                NPC.TargetClosest(faceTarget: false);
            else if (!Main.getGoodWorld)
                NPC.scale *= 0.5f;
            var center = NPC.Center;
            rings = new Ring[2];
            if (Main.expertMode)
            {
                rings[0] = new Ring(InnerRingSegmentCount, Circumference, InnerRingScale);
                if (Main.getGoodWorld)
                {
                    rings[1] = new Ring(OuterRingSegmentCount, Circumference * OuterRingCircumferenceMultiplier_Expert, OuterRingSegment_Expert);
                    Array.Resize(ref rings, 3);
                    rings[2] = new Ring(13, Circumference * 2.5f, 1.45f);
                }
                else
                {
                    rings[1] = new Ring(OuterRingSegmentCount, Circumference * OuterRingCircumferenceMultiplier_Expert, OuterRingSegment_Expert);
                }
            }
            else
            {
                rings[0] = new Ring(InnerRingSegmentCount, Circumference * 0.75f, InnerRingScale);
                rings[1] = new Ring(OuterRingSegmentCount, Circumference * OuterRingCircumferenceMultiplier_Normal, OuterRingScale_Normal);
            }
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i].MultScale(NPC.scale);
                rings[i].Update(center);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && !bestiaryDummy)
            {
                int damage = Main.expertMode ? 25 : 30;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<OmegaStariteProj>(), damage, 1f, Main.myPlayer, NPC.whoAmI);
            }
        }

        public override void AI()
        {
            if (Main.dayTime)
            {
                NPC.life = -1;
                Glimmer.omegaStarite = -1;
                NPC.HitEffect();
                SoundEngine.PlaySound(SoundID.Dig, NPC.Center);
                NPC.active = false;
                return;
            }
            Glimmer.omegaStarite = (short)NPC.whoAmI;
            //Main.NewText(NPC.ai[0]);
            Vector2 center = NPC.Center;
            Player player = Main.player[NPC.target];
            var plrCenter = player.Center;
            float speed = NPC.velocity.Length();
            if (ROR2ArtifactsSupport.ROR2Artifacts.Enabled)
            {
                var parent = ROR2ArtifactsSupport.GetParent(NPC);
                if (parent != null && !parent.dontTakeDamage && (int)parent.ai[0] == (int)NPC.ai[0])
                {
                    NPC.ai[0] = PHASE_ASSAULT_PLAYER;
                }
            }
            switch ((int)NPC.ai[0])
            {
                default:
                    {
                        LerpToDefaultRotationVelocity();
                        NPC.Center = plrCenter + new Vector2(0f, -Circumference * 2f);
                    }
                    break;

                case PHASE_OMEGA_LASER_PART0:
                    {
                        if (NPC.ai[1] == 0f)
                        {
                            CullRingRotations();
                        }
                        NPC.ai[1] += 0.0002f;
                        bool allRingsSet = true;

                        rings[0].rotationVelocity *= 0.95f;

                        rings[0].pitch = rings[0].pitch.AngleLerp(MathHelper.PiOver2, 0.025f);
                        rings[0].roll = rings[0].roll.AngleLerp(-MathHelper.PiOver2, 0.025f);

                        if (!rings[0].pitch.CloseEnough(MathHelper.PiOver2, 0.314f) || !rings[0].roll.CloseEnough(-MathHelper.PiOver2, 0.314f))
                        {
                            allRingsSet = false;
                        }
                        for (int i = 1; i < rings.Length; i++)
                        {
                            rings[i].rotationVelocity *= 0.95f;

                            rings[i].pitch = rings[i].pitch.AngleLerp(0f, 0.025f);
                            rings[i].roll = rings[i].roll.AngleLerp(0f, 0.025f);
                            if (allRingsSet && (!rings[i].pitch.CloseEnough(0f, 0.314f) || !rings[i].roll.CloseEnough(0f, 0.314f)))
                            {
                                allRingsSet = false;
                            }
                        }

                        if (NPC.ai[1] > 0.0314f)
                        {
                            if (allRingsSet)
                            {
                                NPC.velocity = Vector2.Normalize(plrCenter - center) * NPC.velocity.Length();
                                rings[0].pitch = MathHelper.PiOver2;
                                rings[0].roll = -MathHelper.PiOver2;
                                for (int i = 1; i < rings.Length; i++)
                                {
                                    rings[i].pitch = 0f;
                                    rings[i].roll = 0f;
                                }
                                if (PlrCheck())
                                {
                                    NPC.ai[0] = PHASE_OMEGA_LASER;
                                    NPC.ai[1] = 0f;
                                    NPC.ai[3] = 3f + (1f - NPC.life / (float)NPC.lifeMax) * 1.5f;
                                }
                            }
                        }
                        else
                        {
                            rings[0].yaw += 0.0314f - NPC.ai[1];
                            for (int i = 1; i < rings.Length; i++)
                            {
                                rings[i].yaw += 0.0157f - NPC.ai[1] * 0.5f;
                            }
                        }
                    }
                    break;

                case PHASE_OMEGA_LASER:
                    {
                        NPC.ai[2]++;
                        if (NPC.ai[2] > 1200f)
                        {
                            if (NPC.ai[1] > 0.0314)
                            {
                                NPC.ai[1] -= 0.0005f;
                            }
                            else
                            {
                                NPC.ai[1] = 0.0314f;
                            }
                            rings[0].yaw += NPC.ai[1];
                            rings[1].yaw += NPC.ai[1] * 0.5f;
                            bool ringsSet = false;
                            if (rings[1].radiusFromOrigin > rings[1].OriginalRadiusFromOrigin)
                            {
                                rings[1].radiusFromOrigin -= MathHelper.PiOver2 * 3f;
                                NPC.localAI[0]++;
                                if (Main.getGoodWorld)
                                {
                                    bool shot = false;
                                    for (int i = 0; i < rings.Length; i++)
                                    {
                                        shot |= ShootProjsFromRing(endingPhase: true, rings[i]);
                                    }
                                    if (shot)
                                    {
                                        SoundID.DD2_DarkMageHealImpact?.PlaySound(NPC.Center);
                                        NPC.localAI[0] = 0f;
                                    }
                                }
                                else if (Vector2.Distance(plrCenter, center) > rings[0].radiusFromOrigin)
                                {
                                    if (ShootProjsFromRing(endingPhase: true, rings[1]))
                                    {
                                        SoundID.DD2_DarkMageHealImpact?.PlaySound(NPC.Center);
                                        NPC.localAI[0] = 0f;
                                    }
                                }
                            }
                            else
                            {
                                ringsSet = true;
                            }
                            for (int i = 2; i < rings.Length; i++)
                            {
                                rings[i].radiusFromOrigin -= MathHelper.PiOver2 * 3f;
                                if (rings[i].radiusFromOrigin > rings[i].OriginalRadiusFromOrigin)
                                {
                                    ringsSet = false;
                                }
                            }
                            if (ringsSet)
                            {
                                ResetRingsRadiusFromOrigin();
                                if (PlrCheck())
                                {
                                    var choices = new List<int>
                                    {
                                        PHASE_ASSAULT_PLAYER,
                                        PHASE_STAR_BULLETS,
                                    };
                                    NPC.ai[0] = choices[Main.rand.Next(choices.Count)];
                                    NPC.ai[1] = 0f;
                                    NPC.ai[2] = 0f;
                                    NPC.ai[3] = 0f;
                                    NPC.localAI[0] = 0f;
                                    NPC.localAI[1] = 0f;
                                    NPC.localAI[2] = 0f;
                                }
                            }
                        }
                        else if ((center - plrCenter).Length() > 1800f)
                        {
                            NPC.ai[2] = 1200f;
                            rings[0].yaw += NPC.ai[1];
                            for (int i = 1; i < rings.Length; i++)
                            {
                                rings[i].yaw += NPC.ai[1] * 0.5f;
                            }
                        }
                        else
                        {
                            if (NPC.ai[1] >= 0.0628f)
                            {
                                NPC.ai[1] = 0.0628f;
                            }
                            else
                            {
                                NPC.ai[1] += 0.0002f;
                            }
                            rings[0].yaw += NPC.ai[1];
                            for (int i = 1; i < rings.Length; i++)
                            {
                                rings[i].yaw += NPC.ai[1] * 0.5f;
                                rings[i].radiusFromOrigin = MathHelper.Lerp(rings[i].radiusFromOrigin, rings[i].OriginalRadiusFromOrigin * (NPC.ai[3] + i), 0.025f);
                            }
                            if (NPC.ai[2] > 100f)
                            {
                                if (NPC.localAI[1] == 0f)
                                {
                                    if (PlrCheck())
                                    {
                                        NPC.localAI[1] = 1f;
                                        SoundID.DD2_EtherianPortalOpen?.PlaySound(NPC.Center);
                                        if (Main.netMode != NetmodeID.Server)
                                        {
                                            EffectsSystem.Shake.Set(12f);
                                        }
                                        int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), center, new Vector2(0f, 0f), ModContent.ProjectileType<OmegaStariteDeathray>(), 100, 1f, Main.myPlayer, NPC.whoAmI);
                                        Main.projectile[p].scale = Main.getGoodWorld ? 1f : 0.75f;
                                        if (Main.getGoodWorld)
                                        {
                                            p = Projectile.NewProjectile(NPC.GetSource_FromAI(), center, new Vector2(0f, 0f), ModContent.ProjectileType<OmegaStariteDeathray>(), 100, 1f, Main.myPlayer, NPC.whoAmI);
                                            ((OmegaStariteDeathray)Main.projectile[p].ModProjectile).rotationOffset = MathHelper.Pi;
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (rings[0].roll > MathHelper.PiOver2 * 6f)
                                {
                                    NPC.localAI[2] -= Main.expertMode ? 0.001f : 0.00045f;
                                }
                                else
                                {
                                    NPC.localAI[2] += Main.expertMode ? 0.00015f : 0.000085f;
                                }
                                NPC.localAI[0]++;
                                if (Main.getGoodWorld)
                                {
                                    bool shot = false;
                                    for (int i = 1; i < rings.Length; i++)
                                    {
                                        shot |= ShootProjsFromRing(endingPhase: false, rings[i]);
                                    }
                                    if (shot)
                                    {
                                        SoundID.DD2_DarkMageHealImpact?.PlaySound(NPC.Center);
                                        NPC.localAI[0] = 0f;
                                    }
                                }
                                else if (Vector2.Distance(plrCenter, center) > rings[0].radiusFromOrigin)
                                {
                                    if (ShootProjsFromRing(endingPhase: false, rings[1]))
                                    {
                                        SoundID.DD2_DarkMageHealImpact?.PlaySound(NPC.Center);
                                        NPC.localAI[0] = 0f;
                                    }
                                }
                                rings[0].roll += NPC.localAI[2];
                                if (NPC.soundDelay <= 0)
                                {
                                    NPC.soundDelay = 60;
                                    SoundID.DD2_EtherianPortalIdleLoop?.PlaySound(NPC.Center);
                                }
                                if (NPC.soundDelay > 0)
                                    NPC.soundDelay--;
                                if (rings[0].roll > MathHelper.PiOver2 * 7f)
                                {
                                    NPC.soundDelay = 0;
                                    SoundID.DD2_EtherianPortalOpen?.PlaySound(NPC.Center);
                                    NPC.ai[2] = 1200f;
                                    rings[0].roll = -MathHelper.PiOver2;
                                }
                            }
                            else
                            {
                                const int width = (int)(Circumference * 2f);
                                const int height = 900;
                                Vector2 dustPos = center + new Vector2(-width / 2f, 0f);
                                Dust.NewDust(dustPos, width, height, ModContent.DustType<MonoDust>(), 0f, 0f, 0, Glimmer.CosmicEnergyColor, 2f);
                                Dust.NewDust(dustPos, width, height, ModContent.DustType<MonoDust>(), 0f, 0f, 0, Glimmer.CosmicEnergyColor, 2f);
                                Dust.NewDust(dustPos, width, height, ModContent.DustType<MonoDust>(), 0f, 0f, 0, Glimmer.CosmicEnergyColor, 2f);
                            }
                        }
                    }
                    break;

                case PHASE_STAR_BULLETS:
                    {
                        LerpToDefaultRotationVelocity();

                        NPC.ai[1]++;

                        if (NPC.ai[2] == 0f)
                        {
                            if (Main.expertMode)
                            {
                                NPC.ai[2] = 18f;
                                NPC.ai[3] = 96f;
                            }
                            else
                            {
                                NPC.ai[2] = 7.65f;
                                NPC.ai[3] = 192f;
                            }
                        }

                        if (NPC.ai[1] % NPC.ai[3] == 0f)
                        {
                            if (PlrCheck())
                            {
                                SoundHelper.Play(SoundType.Sound, "starbullets", NPC.Center, 0.3f, 0.5f);
                                //Main.PlaySound(SoundID.Item125);
                                int type = ModContent.ProjectileType<OmegaStariteBullet>();
                                float speed2 = Main.expertMode ? 12.5f : 5.5f;
                                int damage = 30;
                                if (Main.expertMode)
                                    damage = 20;
                                float rot = MathHelper.TwoPi / (Main.getGoodWorld ? 10f : 5f);
                                for (int i = 0; i < (Main.getGoodWorld ? 3 : 2); i++)
                                {
                                    for (float f = 0f; f < MathHelper.TwoPi; f += rot)
                                    {
                                        var v = f.ToRotationVector2();
                                        int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), center + v * Radius, v * speed2, type, damage, 1f, player.whoAmI, -60f, speed2);
                                        Main.projectile[p].timeLeft += 120;
                                    }
                                    speed2 *= 1.2f;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        float distance = (center - plrCenter).Length();
                        if (distance > Circumference * 3.75f)
                        {
                            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Normalize(plrCenter - center) * NPC.ai[2], 0.02f);
                        }
                        else if (distance < Circumference * 2.25f)
                        {
                            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Normalize(center - plrCenter) * NPC.ai[2], 0.02f);
                        }
                        else
                        {
                            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Normalize(plrCenter - center).RotatedBy(MathHelper.PiOver2) * NPC.ai[2], 0.02f);
                        }

                        if (NPC.ai[1] > 480f)
                        {
                            NPC.ai[0] = PHASE_HYPER_STARITE_PART0;
                            NPC.ai[1] = 0f;
                            NPC.ai[2] = 0f;
                            NPC.ai[3] = 0f;
                        }
                    }
                    break;

                case PHASE_ASSAULT_PLAYER:
                    {
                        LerpToDefaultRotationVelocity();

                        if (NPC.ai[1] < 0f)
                        {
                            NPC.ai[1]++;
                            if (NPC.ai[2] == 0f)
                            {
                                if (PlrCheck())
                                {
                                    NPC.ai[2] = Main.expertMode ? 18f : 6f;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Normalize(plrCenter - center) * NPC.ai[2], 0.02f);
                        }
                        else
                        {
                            if (!PlrCheck())
                                break;
                            if (NPC.ai[1] == 0f)
                            {
                                SoundID.DD2_BetsyWindAttack?.PlaySound(NPC.Center);
                                NPC.ai[1] = plrCenter.X + player.velocity.X * 20f;
                                NPC.ai[2] = plrCenter.Y + player.velocity.Y * 20f;
                            }
                            if ((center - new Vector2(NPC.ai[1], NPC.ai[2])).Length() < Circumference)
                            {
                                NPC.ai[3]++;
                                if (NPC.ai[3] > 5)
                                {
                                    NPC.ai[0] = PHASE_HYPER_STARITE_PART0;
                                    NPC.ai[1] = 0f;
                                    NPC.ai[3] = 0f;
                                }
                                else
                                {
                                    NPC.ai[1] = -NPC.ai[3] * 16;
                                    if (Main.getGoodWorld || Vector2.Distance(plrCenter, center) > 120f)
                                    {
                                        if (Main.netMode != NetmodeID.MultiplayerClient)
                                        {
                                            float lifePercent = NPC.life / (float)NPC.lifeMax;
                                            if (Main.getGoodWorld || (Main.expertMode && lifePercent < 0.75f) || lifePercent < 0.6f)
                                            {
                                                SoundID.DD2_DarkMageHealImpact?.PlaySound(NPC.Center);
                                                int type = ModContent.ProjectileType<OmegaStariteBullet>();
                                                float speed2 = Main.expertMode ? 12.5f : 5.5f;
                                                int damage = 30;
                                                if (Main.expertMode)
                                                    damage = 20;
                                                float rot = MathHelper.TwoPi / (Main.getGoodWorld ? 10f : 5f) + 0.01f;
                                                for (float f = 0f; f < MathHelper.TwoPi; f += rot)
                                                {
                                                    var v = f.ToRotationVector2();
                                                    int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), center + v * Radius, v * speed2, type, damage, 1f, player.whoAmI, -60f, speed2);
                                                    Main.projectile[p].timeLeft += 120;
                                                }
                                            }
                                        }
                                    }
                                    NPC.netUpdate = true;
                                }
                                NPC.ai[2] = 0f;
                            }
                            else
                            {
                                NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Normalize(new Vector2(NPC.ai[1], NPC.ai[2]) - center) * 20f, 0.025f);
                            }
                        }
                    }
                    break;

                case PHASE_HYPER_STARITE_PART2_ALT:
                case PHASE_HYPER_STARITE_PART2:
                    {
                        NPC.ai[2]++;
                        if (NPC.ai[2] > 300f)
                        {
                            if (NPC.ai[1] > 0.0314)
                            {
                                NPC.ai[1] -= 0.0005f;
                            }
                            else
                            {
                                NPC.ai[1] = 0.0314f;
                            }
                            rings[0].yaw += NPC.ai[1];
                            for (int i = 1; i < rings.Length; i++)
                            {
                                rings[i].yaw += NPC.ai[1] * 0.5f;
                            }

                            PullInRingsTransition();
                        }
                        else if ((center - plrCenter).Length() > 1800f)
                        {
                            NPC.ai[2] = 300f;
                            rings[0].yaw += NPC.ai[1];
                            for (int i = 1; i < rings.Length; i++)
                            {
                                rings[i].yaw += NPC.ai[1] * 0.5f;
                            }
                        }
                        else
                        {
                            if (NPC.ai[1] >= 0.0628f)
                            {
                                NPC.ai[1] = 0.0628f;
                            }
                            else
                            {
                                NPC.ai[1] += 0.0002f;
                            }
                            rings[0].yaw += NPC.ai[1];
                            rings[0].radiusFromOrigin = MathHelper.Lerp(rings[0].radiusFromOrigin, rings[0].OriginalRadiusFromOrigin * NPC.ai[3], 0.025f);
                            for (int i = 1; i < rings.Length; i++)
                            {
                                rings[i].yaw += NPC.ai[1] * 0.5f;
                                rings[i].radiusFromOrigin = MathHelper.Lerp(rings[i].radiusFromOrigin, rings[i].OriginalRadiusFromOrigin * (NPC.ai[3] + i), 0.025f);
                            }
                            if (NPC.ai[2] > 100f)
                            {
                                NPC.localAI[0]++;
                                if (Main.getGoodWorld)
                                {
                                    bool shot = false;
                                    for (int i = 0; i < rings.Length; i++)
                                    {
                                        shot |= ShootProjsFromRing(endingPhase: false, rings[i]);
                                    }
                                    if (shot)
                                    {
                                        SoundID.DD2_DarkMageHealImpact?.PlaySound(NPC.Center);
                                        NPC.localAI[0] = 0f;
                                    }
                                }
                                else if (Vector2.Distance(plrCenter, center) > rings[0].radiusFromOrigin)
                                {
                                    if (ShootProjsFromRing(endingPhase: false, rings[1]))
                                    {
                                        SoundID.DD2_DarkMageHealImpact?.PlaySound(NPC.Center);
                                        NPC.localAI[0] = 0f;
                                    }
                                }
                            }
                        }
                    }
                    break;

                case PHASE_HYPER_STARITE_PART1:
                    {
                        if (NPC.ai[1] == 0f)
                        {
                            rings[0].pitch %= MathHelper.Pi;
                            rings[0].roll %= MathHelper.Pi;
                            rings[1].pitch %= MathHelper.Pi;
                            rings[1].roll %= MathHelper.Pi;
                        }
                        NPC.ai[1] += 0.0002f;

                        bool allRingsSet = true;
                        for (int i = 0; i < rings.Length; i++)
                        {
                            rings[i].rotationVelocity *= 0.95f;
                            rings[i].pitch = rings[i].pitch.AngleLerp(0f, 0.025f);
                            rings[i].roll = rings[i].roll.AngleLerp(0f, 0.025f);
                            if (allRingsSet && (!rings[i].pitch.CloseEnough(0f, 0.314f) || !rings[i].roll.CloseEnough(0f, 0.314f)))
                            {
                                allRingsSet = false;
                            }
                        }
                        if (NPC.ai[1] > 0.0314f)
                        {
                            if (allRingsSet)
                            {
                                NPC.velocity = Vector2.Normalize(plrCenter - center) * NPC.velocity.Length();
                                for (int i = 0; i < rings.Length; i++)
                                {
                                    rings[i].pitch = 0f;
                                    rings[i].roll = 0f;
                                }
                                if (PlrCheck())
                                {
                                    NPC.ai[0] = Main.rand.NextBool() ? PHASE_HYPER_STARITE_PART2 : PHASE_HYPER_STARITE_PART2_ALT;
                                    NPC.ai[1] = 0f;
                                    NPC.ai[3] = 3f + (1f - NPC.life / (float)NPC.lifeMax) * 1.5f;
                                }
                            }
                        }
                        else
                        {
                            rings[0].yaw += 0.0314f - NPC.ai[1];
                            for (int i = 1; i < rings.Length; i++)
                            {
                                rings[i].yaw += 0.0157f - NPC.ai[1] * 0.5f;
                            }
                        }
                    }
                    break;

                case PHASE_HYPER_STARITE_PART0:
                    {
                        LerpToDefaultRotationVelocity();
                        if (NPC.ai[1] == 0f)
                        {
                            if (PlrCheck())
                            {
                                SoundID.DD2_BetsyWindAttack?.PlaySound(NPC.Center);
                                NPC.ai[1] = plrCenter.X + player.velocity.X * 20f;
                                NPC.ai[2] = plrCenter.Y + player.velocity.Y * 20f;
                                NPC.netUpdate = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if ((center - new Vector2(NPC.ai[1], NPC.ai[2])).Length() < Circumference)
                        {
                            if (NPC.velocity.Length() < 2f)
                            {
                                ResetRingsRadiusFromOrigin();
                                if (PlrCheck())
                                {
                                    NPC.velocity *= 0.1f;
                                    if (NPC.life / (float)NPC.lifeMax < 0.5f)
                                    {
                                        NPC.ai[0] = PHASE_OMEGA_LASER_PART0;
                                    }
                                    else
                                    {
                                        NPC.ai[0] = PHASE_HYPER_STARITE_PART1;
                                    }
                                    NPC.ai[1] = 0f;
                                    NPC.ai[2] = 0f;
                                }
                            }
                            else
                            {
                                NPC.velocity *= 0.925f;
                            }
                        }
                        else
                        {
                            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Normalize(new Vector2(NPC.ai[1], NPC.ai[2]) - center) * 30f, 0.025f);
                        }
                    }
                    break;

                case PHASE_INIT:
                    {
                        var choices = new List<int>
                        {
                            PHASE_ASSAULT_PLAYER,
                            PHASE_HYPER_STARITE_PART0,
                        };
                        NPC.ai[0] = choices[Main.rand.Next(choices.Count)];
                        Initialize();
                        NPC.netUpdate = true;
                    }
                    break;

                case PHASE_DEAD:
                    {
                        for (int i = 0; i < rings.Length; i++)
                        {
                            rings[i].rotationVelocity *= 0f;
                        }
                        NPC.ai[1] += 0.5f;
                        if (NPC.ai[1] > DEATH_TIME * 1.314f)
                        {
                            NPC.life = -33333;
                            NPC.HitEffect();
                            NPC.checkDead();
                        }
                    }
                    break;

                case PHASE_NOVA:
                    {
                        if (NPC.ai[1] == 0f)
                        {
                            int target = NPC.target;
                            Initialize();
                            NPC.netUpdate = true;
                            NPC.target = target;
                            NPC.ai[2] = plrCenter.Y - Circumference * 2.5f;
                        }
                        LerpToDefaultRotationVelocity();
                        if (center.Y > NPC.ai[2])
                        {
                            int[] choices = new int[] { PHASE_HYPER_STARITE_PART0, PHASE_ASSAULT_PLAYER };
                            NPC.ai[0] = choices[Main.rand.Next(choices.Length)];
                            NPC.ai[1] = 0f;
                            NPC.ai[2] = 0f;
                            NPC.netUpdate = true;
                        }
                        else
                        {
                            //if (Main.netMode != NetmodeID.Server) //Could not get this to work!
                            //{
                            //    int id = mod.GetSoundSlot(SoundType.Item, "Sounds/Item/OmegaStarite/novaspawn");
                            //    float length = Vector2.Distance(NPC.Center, Main.player[NPC.target].Center);
                            //    if (!_playedSpawnSound)
                            //    {
                            //        Main.soundInstanceItem[id].Stop();
                            //        Main.soundInstanceItem[id] = Main.soundItem[id].CreateInstance();
                            //    }
                            //    if (length > 1000f)
                            //    {
                            //        Main.soundInstanceItem[id].Volume = 0.1f;
                            //    }
                            //    else
                            //    {
                            //        Main.soundInstanceItem[id].Volume = 1f - length / 1250f;
                            //    }
                            //    if (!_playedSpawnSound)
                            //    {
                            //        Main.soundInstanceItem[id].Play();
                            //        _playedSpawnSound = true;
                            //    }
                            //    Main.soundInstanceItem[id].Pitch = 0f;
                            //}
                            float fallSpeed = Main.getGoodWorld ? 56f : 36f;
                            NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Normalize(new Vector2(center.X, NPC.ai[2]) - center) * fallSpeed, 0.025f);
                        }
                    }
                    break;

                case PHASE_GOODBYE:
                    {
                        if (NPC.timeLeft > 120)
                            NPC.timeLeft = 120;
                        NPC.velocity.X *= 0.975f;
                        NPC.velocity.Y -= 0.2f;

                        rings[0].yaw += 0.0314f;
                        rings[0].roll += 0.0157f;
                        rings[0].pitch += 0.01f;
                        rings[1].yaw += 0.0157f;
                        rings[1].roll += 0.0314f;
                        rings[1].pitch += 0.011f;
                    }
                    break;
            }
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i].Update(center + NPC.velocity);
            }
            if (NPC.ai[0] != -1)
            {
                int chance = 10 - (int)speed;
                if (chance < 2 || Main.rand.NextBool(chance))
                {
                    if (speed < 2f)
                    {
                        var spawnPos = new Vector2(Radius, 0f);
                        int d = Dust.NewDust(center + spawnPos.RotatedBy(Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi)), 2, 2, 15);
                        Main.dust[d].velocity = Vector2.Normalize(spawnPos - center) * speed * 0.25f;
                    }
                    else
                    {
                        var spawnPos = new Vector2(Radius, 0f).RotatedBy(NPC.velocity.ToRotation() - MathHelper.Pi);
                        int d = Dust.NewDust(NPC.Center + spawnPos.RotatedBy(Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4)), 2, 2, 15);
                        Main.dust[d].velocity = -NPC.velocity * 0.25f;
                    }
                }
                if (Main.rand.NextBool(30))
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 58);
                    Main.dust[d].velocity.X = Main.rand.NextFloat(-4f, 4f);
                    Main.dust[d].velocity.Y = Main.rand.NextFloat(-4f, 4f);
                }
                if (Main.rand.NextBool(30))
                {
                    int g = Gore.NewGore(new EntitySource_HitEffect(NPC), NPC.position + new Vector2(Main.rand.Next(NPC.width - 4), Main.rand.Next(NPC.height - 4)), new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f)), 16 + Main.rand.Next(2));
                    Main.gore[g].scale *= 0.6f;
                }
            }
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            Lighting.AddLight(NPC.Center, new Vector3(1.2f, 1.2f, 2.2f));
            for (int i = 0; i < rings.Length; i++)
            {
                for (int j = 0; j < rings[i].amountOfSegments; j++)
                {
                    Lighting.AddLight(new Vector2(rings[i].CachedPositions[i].X, rings[i].CachedPositions[i].Y), new Vector3(0.4f, 0.4f, 1f));
                }
            }
        }

        private void LerpToDefaultRotationVelocity()
        {
            rings[0].rotationVelocity = Vector3.Lerp(rings[0].rotationVelocity, new Vector3(0.01f, 0.0157f, 0.0314f), 0.1f);
            rings[1].rotationVelocity = Vector3.Lerp(rings[1].rotationVelocity, new Vector3(0.011f, 0.0314f, 0.0157f), 0.1f);
            if (rings.Length > 2)
            {
                rings[2].rotationVelocity = Vector3.Lerp(rings[2].rotationVelocity, new Vector3(0.012f, 0.0186f, 0.0214f), 0.1f);
            }
        }

        private bool ShootProjsFromRing(bool endingPhase, Ring ring)
        {
            int delay = Main.expertMode ? 12 : 60;
            if (!endingPhase && Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 1000f)
            {
                delay /= 2;
            }
            if (NPC.localAI[0] > delay)
            {
                if (Main.getGoodWorld || (NPC.life / (float)NPC.lifeMax) < 0.75f)
                {
                    float speed = 7.5f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        var diff = new Vector2(ring.CachedPositions[0].X, ring.CachedPositions[0].Y) - NPC.Center;
                        var shootDir = Vector2.Normalize(diff).RotatedBy(MathHelper.PiOver2) * speed;
                        int type = ModContent.ProjectileType<OmegaStariteBullet>();
                        int damage = 25;
                        if (Main.expertMode)
                            damage = 18;
                        for (int i = 0; i < ring.amountOfSegments; i++)
                        {
                            float rot = ring.rotationOrbLoop * i;
                            var position = NPC.Center + diff.RotatedBy(rot);
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), position, shootDir.RotatedBy(rot), type, damage, 1f, Main.myPlayer);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private void CullRingRotations()
        {
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i].pitch %= MathHelper.TwoPi;
                rings[i].roll %= MathHelper.TwoPi;
            }
        }

        private void ResetRingsRadiusFromOrigin()
        {
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i].radiusFromOrigin = rings[i].OriginalRadiusFromOrigin;
            }
        }

        private void PullInRingsTransition()
        {
            bool allRingsSet = true;
            float[] transitionSpeed = new float[rings.Length];
            transitionSpeed[0] = MathHelper.Pi;
            for (int i = 1; i < rings.Length; i++)
            {
                transitionSpeed[i] = MathHelper.PiOver2 * (3f + 2.5f * i);
            }
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i].radiusFromOrigin > rings[i].OriginalRadiusFromOrigin)
                {
                    rings[i].radiusFromOrigin -= transitionSpeed[i];
                    allRingsSet = false;
                }
            }

            if (allRingsSet && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < rings.Length; i++)
                {
                    rings[i].radiusFromOrigin = rings[i].OriginalRadiusFromOrigin;
                }
                if (PlrCheck())
                {
                    var choices = new List<int>
                    {
                        PHASE_ASSAULT_PLAYER,
                    };
                    if (NPC.life / (float)NPC.lifeMax < (Main.expertMode ? 0.5f : 0.33f))
                        choices.Add(PHASE_STAR_BULLETS);
                    if (choices.Count == 1)
                    {
                        NPC.ai[0] = choices[0];
                    }
                    else
                    {
                        NPC.ai[0] = choices[Main.rand.Next(choices.Count)];
                    }
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.ai[3] = 0f;
                    NPC.localAI[1] = 0f;
                    NPC.netUpdate = true;
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.ai[0] != -1)
            {
                NPC.frameCounter++;
                if (NPC.frameCounter >= 6)
                {
                    NPC.frameCounter = 0;
                    NPC.frame.Y += frameHeight;
                    if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                        NPC.frame.Y = 0;
                }
            }
        }

        public override bool CheckDead()
        {
            if (NPC.ai[0] == -1f)
            {
                NPC.lifeMax = -33333;
                return true;
            }
            //NPC.GetGlobalNPC<NoHitting>().preventNoHitCheck = true;
            NPC.ai[0] = -1f;
            NPC.ai[1] = 0f;
            NPC.ai[2] = 0f;
            NPC.ai[3] = 0f;
            NPC.velocity = new Vector2(0f, 0f);
            NPC.dontTakeDamage = true;
            NPC.life = NPC.lifeMax;
            return false;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            if (NPC.life == -33333)
            {
                //if (NoHitting.HasBeenNoHit(npc, Main.myPlayer))
                //{
                //    NoHitting.PlayNoHitJingle(NPC.Center);
                //}
                var center = NPC.Center;
                for (int k = 0; k < 60; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 58, NPC.velocity.X * 0.1f, NPC.velocity.Y * 0.1f, 150, default(Color), 0.8f);
                }
                for (float f = 0f; f < 1f; f += 0.02f)
                {
                    Dust.NewDustPerfect(NPC.Center, ModContent.DustType<MonoSparkleDust>(), Vector2.UnitY.RotatedBy(f * ((float)Math.PI * 2f) + Main.rand.NextFloat() * 0.5f) * (4f + Main.rand.NextFloat() * 4f), 150, Color.CornflowerBlue).noGravity = true;
                }
                for (float f = 0f; f < 1f; f += 0.05f)
                {
                    Dust.NewDustPerfect(NPC.Center, ModContent.DustType<MonoSparkleDust>(), Vector2.UnitY.RotatedBy(f * ((float)Math.PI * 2f) + Main.rand.NextFloat() * 0.5f) * (2f + Main.rand.NextFloat() * 3f), 150, Color.Gold).noGravity = true;
                }
                ScreenCulling.SetFluff();
                if (ScreenCulling.OnScreenWorld(NPC.getRect()))
                {
                    for (int k = 0; k < 7; k++)
                    {
                        Gore.NewGore(new EntitySource_HitEffect(NPC), NPC.Center, Main.rand.NextVector2CircularEdge(0.5f, 0.5f) * NPC.velocity.Length(), Utils.SelectRandom(Main.rand, 16, 17, 17, 17, 17, 17, 17, 17));
                    }
                }
                for (int i = 0; i < rings.Length; i++)
                {
                    for (int j = 0; j < rings[i].amountOfSegments; j++)
                    {
                        for (int k = 0; k < 30; k++)
                        {
                            Dust.NewDust(rings[i].CachedHitboxes[j].TopLeft(), rings[i].CachedHitboxes[j].Width, rings[i].CachedHitboxes[j].Height, 58, NPC.velocity.X * 0.1f, NPC.velocity.Y * 0.1f, 150, default(Color), 0.8f);
                        }
                        for (float f = 0f; f < 1f; f += 0.125f)
                        {
                            Dust.NewDustPerfect(rings[i].CachedHitboxes[j].Center.ToVector2(), ModContent.DustType<MonoSparkleDust>(), Vector2.UnitY.RotatedBy(f * ((float)Math.PI * 2f) + Main.rand.NextFloat() * 0.5f) * (4f + Main.rand.NextFloat() * 4f), 150, Color.CornflowerBlue).noGravity = true;
                        }
                        for (float f = 0f; f < 1f; f += 0.25f)
                        {
                            Dust.NewDustPerfect(rings[i].CachedHitboxes[j].Center.ToVector2(), ModContent.DustType<MonoSparkleDust>(), Vector2.UnitY.RotatedBy(f * ((float)Math.PI * 2f) + Main.rand.NextFloat() * 0.5f) * (2f + Main.rand.NextFloat() * 3f), 150, Color.Gold).noGravity = true;
                        }
                        if (ScreenCulling.OnScreenWorld(rings[i].CachedHitboxes[j]))
                        {
                            for (int k = 0; k < 7; k++)
                            {
                                Gore.NewGore(new EntitySource_HitEffect(NPC), NPC.Center, Main.rand.NextVector2CircularEdge(0.5f, 0.5f) * NPC.velocity.Length(), Utils.SelectRandom(Main.rand, 16, 17, 17, 17, 17, 17, 17, 17));
                            }
                        }
                    }
                }
            }
            else if (NPC.life <= 0)
            {
                SoundHelper.Play(SoundType.Sound, "omegastaritehit" + Main.rand.Next(3), NPC.Center, 0.6f);
                //if (skipDeathTimer > 0)
                //{
                //    if (NoHitting.HasBeenNoHit(npc, Main.myPlayer))
                //    {
                //        NoHitting.PlayNoHitJingle(NPC.Center);
                //    }
                //    AQGraphics.SetCullPadding();
                //    for (int i = 0; i < rings.Length; i++)
                //    {
                //        for (int j = 0; j < rings[i].amountOfSegments; j++)
                //        {
                //            for (int k = 0; k < 30; k++)
                //            {
                //                Dust.NewDust(rings[i].CachedHitboxes[j].TopLeft(), rings[i].CachedHitboxes[j].Width, rings[i].CachedHitboxes[j].Height, 58, NPC.velocity.X * 0.1f, NPC.velocity.Y * 0.1f, 150, default(Color), 0.8f);
                //            }
                //            for (float f = 0f; f < 1f; f += 0.125f)
                //            {
                //                Dust.NewDustPerfect(rings[i].CachedHitboxes[j].Center.ToVector2(), ModContent.DustType<MonoSparkleDust>(), Vector2.UnitY.RotatedBy(f * ((float)Math.PI * 2f) + Main.rand.NextFloat() * 0.5f) * (4f + Main.rand.NextFloat() * 4f), 150, Color.CornflowerBlue).noGravity = true;
                //            }
                //            for (float f = 0f; f < 1f; f += 0.25f)
                //            {
                //                Dust.NewDustPerfect(rings[i].CachedHitboxes[j].Center.ToVector2(), ModContent.DustType<MonoSparkleDust>(), Vector2.UnitY.RotatedBy(f * ((float)Math.PI * 2f) + Main.rand.NextFloat() * 0.5f) * (2f + Main.rand.NextFloat() * 3f), 150, Color.Gold).noGravity = true;
                //            }
                //            if (AQGraphics.Cull_WorldPosition(rings[i].CachedHitboxes[j]))
                //            {
                //                for (int k = 0; k < 7; k++)
                //                {
                //                    Gore.NewGore(NPC.Center, Main.rand.NextVector2CircularEdge(0.5f, 0.5f) * 12f, Utils.SelectRandom(Main.rand, 16, 17, 17, 17, 17, 17, 17, 17));
                //                }
                //            }
                //        }
                //    }
                //}
                for (int k = 0; k < 60; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 58, NPC.velocity.X * 0.1f, NPC.velocity.Y * 0.1f, 150, default(Color), 0.8f);
                }
                for (float f = 0f; f < 1f; f += 0.02f)
                {
                    Dust.NewDustPerfect(NPC.Center, ModContent.DustType<MonoSparkleDust>(), Vector2.UnitY.RotatedBy(f * ((float)Math.PI * 2f) + Main.rand.NextFloat() * 0.5f) * (4f + Main.rand.NextFloat() * 4f), 150, Color.CornflowerBlue).noGravity = true;
                }
                for (float f = 0f; f < 1f; f += 0.05f)
                {
                    Dust.NewDustPerfect(NPC.Center, ModContent.DustType<MonoSparkleDust>(), Vector2.UnitY.RotatedBy(f * ((float)Math.PI * 2f) + Main.rand.NextFloat() * 0.5f) * (2f + Main.rand.NextFloat() * 3f), 150, Color.Gold).noGravity = true;
                }
                ScreenCulling.SetFluff();
                if (ScreenCulling.OnScreenWorld(NPC.getRect()))
                {
                    for (int k = 0; k < 7; k++)
                    {
                        Gore.NewGore(new EntitySource_HitEffect(NPC), NPC.Center, Main.rand.NextVector2CircularEdge(0.5f, 0.5f) * 6f, Utils.SelectRandom(Main.rand, 16, 17, 17, 17, 17, 17, 17, 17));
                    }
                }

            }
            else
            {
                SoundHelper.Play(SoundType.Sound, "omegastaritehit" + Main.rand.Next(3), NPC.Center, 0.6f);
                byte shake = (byte)MathHelper.Clamp((int)(damage / 8), 4, 10);
                if (shake > _hitShake)
                {
                    _hitShake = shake;
                }
                float x = NPC.velocity.X.Abs() * hitDirection;
                if (Main.rand.NextBool())
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 58);
                    Main.dust[d].velocity.X += x;
                    Main.dust[d].velocity.Y = Main.rand.NextFloat(2f, 6f);
                }
                if (Main.rand.NextBool(7))
                    Gore.NewGore(new EntitySource_HitEffect(NPC), NPC.Center, new Vector2(Main.rand.NextFloat(-4f, 4f) + x * 0.75f, Main.rand.NextFloat(-4f, 4f)), 16 + Main.rand.Next(2));
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.expertMode)
            {
                if (Main.rand.NextBool())
                    target.AddBuff(ModContent.BuffType<BlueFire>(), 120);
                if (Main.rand.NextBool())
                    target.AddBuff(BuffID.Blackout, 360);
            }
            else
            {
                if (Main.rand.NextBool())
                    target.AddBuff(BuffID.OnFire, 120);
                if (Main.rand.NextBool())
                    target.AddBuff(BuffID.Darkness, 120);
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return NPC.ai[0] != -1;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            scale = 1.5f;
            return null;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,

                new FlavorTextBestiaryInfoElement("Mods.Aequus.Bestiary.OmegaStarite")
            });
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                if ((int)NPC.ai[0] == 0)
                {
                    Initialize(bestiaryDummy: true);
                    NPC.ai[0]++;
                }
                LerpToDefaultRotationVelocity();
                for (int i = 0; i < rings.Length; i++)
                {
                    rings[i].Update(NPC.Center);
                }
                NPC.scale = 0.5f;
            }
            if (rings == null)
            {
                return false;
            }
            var viewPos = NPC.IsABestiaryIconDummy ? NPC.Center : new Vector2(screenPos.X + Main.screenWidth / 2f, screenPos.Y + Main.screenHeight / 2f);
            drawColor *= 5f;
            if (drawColor.R < 80)
                drawColor.R = 80;
            if (drawColor.G < 80)
                drawColor.G = 80;
            if (drawColor.B < 80)
                drawColor.B = 80;
            var drawPos = NPC.Center - screenPos;
            drawPos.X = (int)drawPos.X;
            drawPos.Y = (int)drawPos.Y;
            if (NPC.IsABestiaryIconDummy)
            {
                drawPos.Y += 2f;
            }
            var positions = new List<Vector4>();
            for (int i = 0; i < rings.Length; i++)
            {
                for (int j = 0; j < rings[i].amountOfSegments; j++)
                {
                    positions.Add(new Vector4((int)rings[i].CachedPositions[j].X, (int)rings[i].CachedPositions[j].Y, (int)rings[i].CachedPositions[j].Z, rings[i].Scale));
                }
            }
            float intensity = 1f;

            if ((int)NPC.ai[0] == -1)
            {
                intensity += NPC.ai[1] / 20;
                if (NPC.CountNPCS(Type) == 1)
                {
                    ModContent.GetInstance<GameCamera>().SetTarget("Omega Starite", NPC.Center, CameraPriority.NPCDefeat, 12f, 60);
                }

                FlashScene.Flash.Set(NPC.Center, Math.Min(Math.Max(intensity - 1f, 0f) * 0.6f, 4f));
                EffectsSystem.Shake.Set(intensity * 2f);

                int range = (int)intensity + 4;
                drawPos += new Vector2(Main.rand.Next(-range, range), Main.rand.Next(-range, range));
                for (int i = 0; i < positions.Count; i++)
                {
                    positions[i] += new Vector4(Main.rand.Next(-range, range), Main.rand.Next(-range, range), Main.rand.Next(-range, range), 0f);
                }
            }
            else if (_hitShake > 0)
            {
                drawPos += new Vector2(Main.rand.Next(-_hitShake, _hitShake), Main.rand.Next(-_hitShake, _hitShake));
                _hitShake--;
            }
            positions.Sort((o, o2) => -o.Z.CompareTo(o2.Z));
            Main.instance.LoadProjectile(ModContent.ProjectileType<OmegaStariteProj>());
            var omegiteTexture = TextureAssets.Projectile[ModContent.ProjectileType<OmegaStariteProj>()].Value;
            var omegiteFrame = new Rectangle(0, 0, omegiteTexture.Width, omegiteTexture.Height);
            var omegiteOrigin = omegiteFrame.Size() / 2f;
            float xOff = (float)(Math.Sin(Main.GlobalTimeWrappedHourly * 3f) + 1f);
            var clr3 = new Color(50, 50, 50, 0) * intensity;
            float deathSpotlightScale = 0f;
            if (intensity > 3f)
                deathSpotlightScale = NPC.scale * (intensity - 2.1f) * ((float)Math.Sin(NPC.ai[1] * 0.1f) + 1f) / 2f;
            var spotlight = Aequus.MyTex("Assets/Bloom");
            var spotlightOrig = spotlight.Size() / 2f;
            Color spotlightColor = new Color(100, 100, 255, 0);
            var drawOmegite = new List<Aequus.LegacyDrawMethod>();
            if (ClientConfiguration.Instance.HighQuality)
            {
                drawOmegite.Add(delegate (Texture2D texture1, Vector2 position, Rectangle? frame1, Color color, float scale, Vector2 origin1, float rotation, SpriteEffects effects, float layerDepth)
                {
                    spriteBatch.Draw(spotlight, position, null, spotlightColor, rotation, spotlightOrig, scale * 1.33f, SpriteEffects.None, 0f);
                });
            }
            drawOmegite.Add(delegate (Texture2D texture1, Vector2 position, Rectangle? frame1, Color color, float scale, Vector2 origin1, float rotation, SpriteEffects effects, float layerDepth)
            {
                spriteBatch.Draw(omegiteTexture, position, omegiteFrame, drawColor, rotation, origin1, scale, SpriteEffects.None, 0f);
            });
            if (intensity >= 1f)
            {
                drawOmegite.Add(delegate (Texture2D texture1, Vector2 position, Rectangle? frame1, Color color, float scale, Vector2 origin1, float rotation, SpriteEffects effects, float layerDepth)
                {
                    for (int j = 0; j < intensity; j++)
                    {
                        spriteBatch.Draw(omegiteTexture, position + new Vector2(2f + xOff * 2f * j, 0f), omegiteFrame, clr3, rotation, origin1, scale, SpriteEffects.None, 0f);
                        spriteBatch.Draw(omegiteTexture, position + new Vector2(2f - xOff * 2f * j, 0f), omegiteFrame, clr3, rotation, origin1, scale, SpriteEffects.None, 0f);
                    }
                });
            }
            if (intensity > 3f)
            {
                float omegiteDeathDrawScale = deathSpotlightScale * 0.5f;
                drawOmegite.Add(delegate (Texture2D texture1, Vector2 position, Rectangle? frame1, Color color, float scale, Vector2 origin1, float rotation, SpriteEffects effects, float layerDepth)
                {
                    spriteBatch.Draw(spotlight, position, null, drawColor, rotation, spotlightOrig, scale * omegiteDeathDrawScale, SpriteEffects.None, 0f);
                    spriteBatch.Draw(spotlight, position, null, spotlightColor, rotation, spotlightOrig, scale * omegiteDeathDrawScale * 2, SpriteEffects.None, 0f);
                });
            }
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].Z > 0f)
                {
                    var drawPosition = PerspectiveHelper.GetParralaxPosition(new Vector2(positions[i].X, positions[i].Y), positions[i].Z * 0.00728f, viewPos) - screenPos;
                    var drawScale = PerspectiveHelper.GetParralaxScale(positions[i].W, positions[i].Z * 0.0314f);
                    foreach (var draw in drawOmegite)
                    {
                        draw.Invoke(
                            omegiteTexture,
                            drawPosition,
                            omegiteFrame,
                            drawColor,
                            drawScale,
                            omegiteOrigin,
                            NPC.rotation,
                            SpriteEffects.None,
                            0f);
                    }
                    positions.RemoveAt(i);
                    i--;
                }
            }
            var texture = TextureAssets.Npc[NPC.type].Value;
            var offset = new Vector2(NPC.width / 2f, NPC.height / 2f);
            Vector2 origin = NPC.frame.Size() / 2f;
            float mult = 1f / NPCID.Sets.TrailCacheLength[NPC.type];
            var clr = drawColor * 0.25f;
            for (int i = 0; i < intensity; i++)
            {
                spriteBatch.Draw(spotlight, drawPos, null, spotlightColor, NPC.rotation, spotlightOrig, NPC.scale * 2.5f + i, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(spotlight, drawPos, null, spotlightColor * (1f - (intensity - (int)intensity)), NPC.rotation, spotlightOrig, NPC.scale * 2.5f + ((int)intensity + 1), SpriteEffects.None, 0f);

            if (!NPC.IsABestiaryIconDummy)
            {
                if ((NPC.position - NPC.oldPos[1]).Length() > 0.01f)
                {
                    if (prim == null)
                    {
                        float radius = Circumference / 2f;
                        prim = new LegacyPrimRenderer(Aequus.MyTex("Assets/Effects/Prims/ThinLine"), LegacyPrimRenderer.DefaultPass, (p) => new Vector2(radius - p * radius), (p) => new Color(35, 85, 255, 0) * (1f - p), drawOffset: NPC.Size / 2f);
                    }
                    prim.Draw(NPC.oldPos);
                }
                else
                {
                    NPC.oldPos[0] = new Vector2(0f, 0f);
                }
            }

            spriteBatch.Draw(texture, drawPos, NPC.frame, drawColor, NPC.rotation, origin, NPC.scale, SpriteEffects.None, 0f);
            for (int j = 0; j < intensity; j++)
            {
                spriteBatch.Draw(texture, drawPos + new Vector2(2f + xOff * 2f * j, 0f), NPC.frame, clr3, NPC.rotation, origin, NPC.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(texture, drawPos - new Vector2(2f + xOff * 2f * j, 0f), NPC.frame, clr3, NPC.rotation, origin, NPC.scale, SpriteEffects.None, 0f);
            }
            for (int i = 0; i < positions.Count; i++)
            {
                var drawPosition = PerspectiveHelper.GetParralaxPosition(new Vector2(positions[i].X, positions[i].Y), positions[i].Z * 0.00728f, viewPos) - screenPos;
                var drawScale = PerspectiveHelper.GetParralaxScale(positions[i].W, positions[i].Z * 0.0314f);
                foreach (var draw in drawOmegite)
                {
                    draw.Invoke(
                        omegiteTexture,
                        drawPosition,
                        omegiteFrame,
                        drawColor,
                        drawScale,
                        omegiteOrigin,
                        NPC.rotation,
                        SpriteEffects.None,
                        0f);
                }
            }
            if (intensity > 3f)
            {
                float intensity2 = intensity - 2f;
                if (NPC.ai[1] > DEATH_TIME)
                {
                    float scale = (NPC.ai[1] - DEATH_TIME) * 0.2f;
                    scale *= scale;
                    Main.spriteBatch.Draw(spotlight, drawPos, null, new Color(120, 120, 120, 0) * intensity2, NPC.rotation, spotlightOrig, scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(spotlight, drawPos, null, spotlightColor * intensity2, NPC.rotation, spotlightOrig, scale * 2.15f, SpriteEffects.None, 0f);
                }
                else
                {
                    Main.spriteBatch.Draw(spotlight, drawPos, null, new Color(120, 120, 120, 0) * intensity2, NPC.rotation, spotlightOrig, deathSpotlightScale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(spotlight, drawPos, null, spotlightColor * intensity2, NPC.rotation, spotlightOrig, deathSpotlightScale * 2f, SpriteEffects.None, 0f);
                }
            }
            return false;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (StarResistCatalogue.Contains(projectile.type))
            {
                damage = (int)(damage * starDamageMultiplier);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            if (StarResistEasterEggCatalogue.Contains(projectile.type))
            {
                if (damage > 800 * starDamageMultiplier)
                {
                    var starVelocity = projectile.velocity * -1.2f;
                    for (int i = 0; i < 8; i++)
                    {
                        int p2 = Projectile.NewProjectile(NPC.GetSource_OnHurt(projectile), projectile.Center, starVelocity.RotatedBy(MathHelper.PiOver4 * i), ModContent.ProjectileType<RainbowStarofHyperApocalypse>(), damage, knockback);
                        Main.projectile[p2].timeLeft = 240;
                    }
                }
                else
                {
                    int p = Projectile.NewProjectile(NPC.GetSource_OnHurt(projectile), projectile.Center, projectile.velocity * -1.2f, ModContent.ProjectileType<RainbowStarofHyperApocalypse>(), damage, knockback);
                    Main.projectile[p].timeLeft = 240;
                }
                projectile.active = false;
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.HealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new GuaranteedDropWhenBeatenFlawlessly(ModContent.ItemType<OmegaStariteTrophy>(), 10));
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<OmegaStariteBag>()));
            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<OmegaStariteRelic>()));
            npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<DragonBall>(), 4));
            npcLoot.Add(new FlawlessDrop(ModContent.ItemType<Origin>()));
            var normalOnly = new LeadingConditionRule(new Conditions.NotExpert());
            normalOnly.OnSuccess(ItemDropRule.OneFromOptions(1, ModContent.ItemType<Raygun>()));
            normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<LightMatter>(), 1, 14, 20));
            normalOnly.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CosmicEnergy>(), 1, 3, 6));
            npcLoot.Add(normalOnly);
        }

        public override void OnKill()
        {
            WorldFlags.MarkAsDefeated(ref WorldFlags.downedOmegaStarite);
            //Glimmer.deactivationDelay = 275;
            //var noHitManager = NPC.GetGlobalNPC<NoHitting>();
            //bool anyoneNoHit = false;
            //for (int i = 0; i < Main.maxPlayers; i++)
            //{
            //    if (NoHitting.HasBeenNoHit(npc, i))
            //    {
            //        anyoneNoHit = true;
            //        AQItem.DropInstancedItem(i, NPC.getRect(), ModContent.ItemType<AStrangeIdea>());
            //    }
            //}
            //if (anyoneNoHit || Main.rand.NextBool(10))
            //    Item.NewItem(NPC.getRect(), ModContent.ItemType<OmegaStariteTrophy>());

            //if (Main.expertMode)
            //{
            //    NPC.DropBossBags();
            //    if (Main.netMode == NetmodeID.Server)
            //    {
            //        int item = Item.NewItem(NPC.getRect(), ModContent.ItemType<DragonBall>(), 1, noBroadcast: true);
            //        Main.itemLockoutTime[item] = 54000;
            //        for (int i = 0; i < 255; i++)
            //        {
            //            var plr = Main.player[i];
            //            if (plr.active && NPC.playerInteraction[i] && (!noHitManager.damagedPlayers[i] || Main.rand.NextBool(4)))
            //            {
            //                NetMessage.SendData(MessageID.InstancedItem, i, -1, null, item);
            //            }
            //        }
            //        Main.item[item].active = false;
            //    }
            //    else if (Main.netMode == NetmodeID.SinglePlayer)
            //    {
            //        Item.NewItem(NPC.getRect(), ModContent.ItemType<DragonBall>());
            //    }
            //}
            //else
            //{
            //    var rect = NPC.getRect();
            //    if (Main.rand.NextBool(3))
            //        Item.NewItem(rect, ModContent.ItemType<CosmicTelescope>());
            //    if (Main.rand.NextBool(7))
            //        Item.NewItem(rect, ModContent.ItemType<OmegaStariteMask>());
            //    int[] choices = new int[]
            //    {
            //        ModContent.ItemType<MagicWand>(),
            //        ModContent.ItemType<Raygun>(),
            //    };
            //    Item.NewItem(rect, choices[Main.rand.Next(choices.Length)]);
            //    Item.NewItem(rect, ItemID.FallenStar, Main.rand.NextVRand(15, 20));
            //    Item.NewItem(rect, ModContent.ItemType<SaintsFlow>(), Main.rand.NextVRand(1, 3));
            //    Item.NewItem(rect, ModContent.ItemType<CosmicEnergy>(), Main.rand.NextVRand(6, 10));
            //    Item.NewItem(rect, ModContent.ItemType<LightMatter>(), Main.rand.NextVRand(10, 18));
            //}
            //WorldDefeats.DownedStarite = true;
            //WorldDefeats.DownedGlimmer = true;
            //if (Glimmer.IsGlimmerEventCurrentlyActive())
            //{
            //    switch (Main.rand.Next(3))
            //    {
            //        default:
            //            {
            //                NPC.DropItemInstanced(NPC.position, new Vector2(NPC.width, NPC.height), ModContent.ItemType<EnchantedDye>());
            //            }
            //            break;

            //        case 1:
            //            {
            //                NPC.DropItemInstanced(NPC.position, new Vector2(NPC.width, NPC.height), ModContent.ItemType<RainbowOutlineDye>());
            //            }
            //            break;

            //        case 2:
            //            {
            //                NPC.DropItemInstanced(NPC.position, new Vector2(NPC.width, NPC.height), ModContent.ItemType<DiscoDye>());
            //            }
            //            break;
            //    }

            //    if (Main.netMode == NetmodeID.SinglePlayer)
            //    {
            //        for (int i = 0; i < Main.maxPlayers; i++)
            //        {
            //            var plr = Main.player[i];
            //            if (plr.active && NPC.playerInteraction[i])
            //            {
            //                WorldDefeats.ObtainedUltimateSword = true;
            //                int p = Projectile.NewProjectile(new EntitySource_Parent(NPC), NPC.Center, new Vector2(Main.rand.NextFloat(2f, 6f) * (Main.rand.NextBool() ? -1f : 1f), -18f), ModContent.ProjectileType<UltimateSwordDrop>(), 0, 0f, i, ModContent.ItemType<UltimateSword>());
            //                Main.projectile[p].netUpdate = true;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        int item = Item.NewItem(new EntitySource_Parent(NPC), NPC.getRect(), ModContent.ItemType<UltimateSword>(), 1, noBroadcast: true);
            //        Main.itemLockoutTime[item] = 54000;
            //        for (int i = 0; i < 255; i++)
            //        {
            //            var plr = Main.player[i];
            //            if (plr.active && NPC.playerInteraction[i])
            //            {
            //                NetMessage.SendData(MessageID.InstancedItem, i, -1, null, item);
            //            }
            //        }
            //        Main.item[item].active = false;
            //    }
            //}
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(rings.Length);
            for (int i = 0; i < rings.Length; i++)
            {
                writer.Write(rings[i].pitch);
                writer.Write(rings[i].roll);
                writer.Write(rings[i].yaw);
                rings[i].SendNetPackage(writer);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int amt = reader.ReadInt32();
            for (int i = 0; i < rings.Length; i++)
            {
                rings[1].pitch = reader.ReadSingle();
                rings[1].roll = reader.ReadSingle();
                rings[1].yaw = reader.ReadSingle();
                rings[1].RecieveNetPackage(reader);
            }
        }

        public bool IsUltimateRayActive()
        {
            return NPC.ai[0] == PHASE_OMEGA_LASER && NPC.ai[2] < 1200f;
        }
    }
}