﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Aequus.Projectiles.Misc.Bobbers
{
    public class NimrodBobber : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.BobberWooden);
            DrawOriginOffsetY = -8;
        }

        public override void PostAI()
        {
            if (Projectile.wet)
            {
                Projectile.extraUpdates = 0;
            }
            else
            {
                Projectile.extraUpdates = 2;
            }

            int mainBobber = -1;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].bobber && Main.projectile[i].owner == Projectile.owner && Main.projectile[i].type == ModContent.ProjectileType<NimrodCloudBobber>())
                {
                    mainBobber = i;
                    break;
                }
            }

            Projectile.rotation = 0f;
            if ((int)Projectile.ai[0] < 1)
            {
                if (mainBobber == -1)
                {
                    Projectile.ai[0] = 1f;
                    Projectile.Center = Main.player[Projectile.owner].Center;
                }
                if (Main.raining)
                {
                    if (Projectile.ai[1] < -10)
                    {
                        Projectile.ai[1] += 5;
                    }
                }
            }
            else if ((int)Projectile.ai[0] == 1)
            {
                if (mainBobber != -1)
                {
                    Projectile.velocity = Vector2.Normalize(Main.projectile[mainBobber].Center - Projectile.Center).UnNaN() * 12f;
                    (Main.projectile[mainBobber].ModProjectile as NimrodCloudBobber).gotoPosition = new Vector2(-2f);
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return true;
        }

        public override bool PreDrawExtras()
        {
            var player = Main.player[Projectile.owner];
            if (!Projectile.bobber || player.inventory[player.selectedItem].holdStyle <= 0)
                return false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].bobber && Main.projectile[i].owner == Projectile.owner && Main.projectile[i].type == ModContent.ProjectileType<NimrodCloudBobber>())
                {
                    AequusHelpers.DrawFishingLine(player, Projectile.position, Projectile.width / 2, Projectile.height, Projectile.velocity, Projectile.localAI[0], Main.projectile[i].Center + new Vector2(Main.projectile[i].width / -2f, 0f),
                        getLighting: (v, c) => new Color(0, 172, 255, 200));
                    break;
                }
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var texture = TextureAssets.Projectile[Type].Value;
            var frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class NimrodCloudBobber : ModProjectile
    {
        public override string Texture => Aequus.VanillaTexture + "Projectile_" + ProjectileID.RainCloudRaining;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = Main.projFrames[ProjectileID.RainCloudRaining];
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.BobberWooden);
        }

        public Vector2 gotoPosition = new Vector2(0f, 0f);

        public override bool PreAI()
        {
            if (Main.player[Projectile.owner].inventory[Main.player[Projectile.owner].selectedItem].fishingPole == 0 || Main.player[Projectile.owner].CCed || Main.player[Projectile.owner].noItems
               || Main.player[Projectile.owner].pulley || Main.player[Projectile.owner].dead || Projectile.wet)
            {
                Projectile.Kill();
            }

            if ((int)gotoPosition.X == -2f)
            {
                Projectile.frame = 0;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.Normalize(Main.player[Projectile.owner].Center - Projectile.Center) * 12f, 0.6f);
                Projectile.timeLeft = 10;
                if ((Projectile.Center - Main.player[Projectile.owner].Center).Length() < 10f)
                {
                    Projectile.Kill();
                }
                Projectile.rotation += 0.1f;
                return false;
            }

            if ((int)gotoPosition.X == -1f)
            {
                Projectile.frame = 1;
                Projectile.velocity *= 0.9f;
                Projectile.timeLeft = 20;
                if ((Main.player[Projectile.owner].Center - Projectile.Center).Length() > 3000f)
                {
                    Projectile.ai[0] = 1f;
                    gotoPosition.X = -2f;
                    Projectile.netUpdate = true;
                }
                if (Main.myPlayer == Projectile.owner)
                {
                    if ((int)gotoPosition.Y != -20)
                    {
                        if (Projectile.velocity.Length() < 1f)
                        {
                            gotoPosition.Y = -20;
                            SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
                            var s = Main.player[Projectile.owner].GetSource_ItemUse(Main.player[Projectile.owner].HeldItem);
                            Projectile.NewProjectile(s, Projectile.Center, new Vector2(0f, 1f), ModContent.ProjectileType<NimrodBobber>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                            Projectile.NewProjectile(s, Projectile.Center + new Vector2(20f, 0f), new Vector2(1f, 1f), ModContent.ProjectileType<NimrodBobber>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                            Projectile.NewProjectile(s, Projectile.Center + new Vector2(-20f, 0f), new Vector2(-1f, 1f), ModContent.ProjectileType<NimrodBobber>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                        }
                    }
                    else
                    {
                        bool shouldKill = true;
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (Main.projectile[i].active && Main.projectile[i].bobber && Main.projectile[i].owner == Projectile.owner && Main.projectile[i].type == ModContent.ProjectileType<NimrodBobber>())
                            {
                                shouldKill = false;
                            }
                        }
                        if (shouldKill)
                        {
                            Projectile.ai[0] = 1f;
                            gotoPosition.X = -2f;
                            Projectile.netUpdate = true;
                        }
                    }
                    if (Main.player[Projectile.owner].controlUseItem)
                    {
                        Projectile.ai[0] = 1f;
                        gotoPosition.X = -2f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.rotation = 0f;
                return false;
            }
            Projectile.timeLeft = 10;
            if (Main.myPlayer == Projectile.owner && gotoPosition == new Vector2(0f, 0f))
            {
                gotoPosition = Main.MouseWorld + new Vector2(0f, -20f);
                Projectile.netUpdate = true;
            }
            if ((Projectile.Center - gotoPosition).Length() < 10f)
            {
                gotoPosition = new Vector2(-1f, -1f);
                Projectile.velocity *= 0.1f;
                Projectile.netUpdate = true;
            }
            else
            {
                Projectile.velocity = Vector2.Normalize(gotoPosition - Projectile.Center) * Main.player[Projectile.owner].HeldItem.shootSpeed;
                Projectile.rotation += 0.1f;
            }
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(gotoPosition.X);
            writer.Write(gotoPosition.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            gotoPosition.X = reader.ReadSingle();
            gotoPosition.Y = reader.ReadSingle();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            gotoPosition = new Vector2(-2f, -2f);
            return true;
        }

        public override bool PreDrawExtras()
        {
            var player = Main.player[Projectile.owner];
            if (!Projectile.bobber || player.inventory[player.selectedItem].holdStyle <= 0)
                return false;
            AequusHelpers.DrawFishingLine(player, Projectile.position, Projectile.width / 2, Projectile.height, Projectile.velocity, Projectile.localAI[0], Main.player[Projectile.owner].Center + new Vector2(50f * Main.player[Projectile.owner].direction, -38f), getLighting: (v, c) => new Color(0, 172, 255, 200));
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var drawCoordinates = Projectile.Center - Main.screenPosition;
            if (Projectile.frame == 0)
            {
                Main.instance.LoadProjectile(ProjectileID.RainCloudMoving);
                var texture = TextureAssets.Projectile[ProjectileID.RainCloudMoving].Value;
                var frame = texture.Frame(1, 4, 0, (int)Main.GameUpdateCount / 8 % 4);
                Main.spriteBatch.Draw(texture, drawCoordinates, frame, lightColor, Projectile.rotation, frame.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            }
            else
            {
                var texture = TextureAssets.Projectile[Type].Value;
                var frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, (int)Main.GameUpdateCount / 8 % Main.projFrames[Projectile.type]);
                Main.spriteBatch.Draw(texture, drawCoordinates, frame, lightColor, Projectile.rotation, frame.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}