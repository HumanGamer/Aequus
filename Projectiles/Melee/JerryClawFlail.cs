﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AQMod.Projectiles.Melee
{
    public class JerryClawFlail : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 28;
            projectile.height = 28;
            projectile.friendly = true;
            projectile.penetrate = -1;
            projectile.melee = true;
        }

        public override void AI()
        {
            Player player = Main.player[projectile.owner];
            if (!player.active || player.dead)
            {
                projectile.Kill();
                return;
            }
            player.itemAnimation = 10;
            player.itemTime = 10;
            Vector2 difference = player.MountedCenter - projectile.Center;
            float length = difference.Length();
            if (projectile.ai[0] == 0f)
            {
                float maxLength = 180f;
                projectile.tileCollide = true;
                if (length > maxLength)
                {
                    projectile.ai[0] = 1f;
                    projectile.netUpdate = true;
                }
                else if (!player.channel)
                {
                    if (projectile.velocity.Y < 0f)
                        projectile.velocity.Y *= 0.9f;
                    projectile.velocity.Y += 1f;
                    projectile.velocity.X *= 0.9f;
                }
            }
            else if (projectile.ai[0] == 1f)
            {
                projectile.ai[1] += 0.01f;
                if (projectile.ai[1] >= 1)
                {
                    projectile.Kill();
                }
                projectile.tileCollide = false;
                float speed = Math.Max((Main.player[projectile.owner].Center - projectile.Center).Length() / 4f, 22f);
                projectile.velocity = Vector2.Lerp(projectile.velocity, Vector2.Normalize(Main.player[projectile.owner].Center - projectile.Center) * speed, Math.Max(1f - (Main.player[projectile.owner].Center - projectile.Center).Length() / 50f, projectile.ai[1]));
                if ((projectile.Center - Main.player[projectile.owner].Center).Length() < 36f)
                {
                    projectile.Kill();
                }
            }
            if (projectile.active)
            {
                int direction = projectile.Center.X > player.Center.X ? 1 : -1;
                player.ChangeDir(direction);
                projectile.direction = direction;
                projectile.spriteDirection = -direction;
            }
            projectile.rotation = difference.ToRotation() - MathHelper.PiOver2;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool hitEffect = false;
            if (oldVelocity.X != projectile.velocity.X)
            {
                if (Math.Abs(oldVelocity.X) > 4f)
                    hitEffect = true;
                projectile.position.X += projectile.velocity.X;
                projectile.velocity.X = -oldVelocity.X * 0.2f;
            }
            if (oldVelocity.Y != projectile.velocity.Y)
            {
                if (Math.Abs(oldVelocity.Y) > 4f)
                    hitEffect = true;
                projectile.position.Y += projectile.velocity.Y;
                projectile.velocity.Y = -oldVelocity.Y * 0.2f;
            }
            projectile.ai[0] = 1f;
            if (hitEffect)
            {
                projectile.netUpdate = true;
                Collision.HitTiles(projectile.position, projectile.velocity, projectile.width, projectile.height);
                Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y);
            }
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Player player = Main.player[projectile.owner];
            Vector2 center = projectile.Center + new Vector2(0f, 10f).RotatedBy(projectile.rotation);
            Vector2 playerCenter = player.MountedCenter;
            var chain = ModContent.GetTexture(this.GetPath("_Chain"));
            int height = chain.Height - 2;
            var velo = Vector2.Normalize(center + new Vector2(0f, height * 4f) - playerCenter) * height;
            var position = playerCenter;
            var origin = new Vector2(chain.Width / 2f, chain.Height / 2f);
            for (int i = 0; i < 50; i++)
            {
                Main.spriteBatch.Draw(chain, position - Main.screenPosition, null, Lighting.GetColor((int)(position.X / 16), (int)(position.Y / 16f)), velo.ToRotation() + MathHelper.PiOver2, origin, 1f, SpriteEffects.None, 0f);
                velo = Vector2.Normalize(Vector2.Lerp(velo, center - position, 0.01f + MathHelper.Clamp(1f - Vector2.Distance(center, position) / 100f, 0f, 0.99f))) * height;
                position += velo;
                float gravity = MathHelper.Clamp(1f - Vector2.Distance(center, position) / 100f, 0f, 1f);
                velo.Y += gravity * 3f;
                velo.Normalize();
                velo *= height;
                if (Vector2.Distance(position, center) <= height)
                    break;
            }
            return true;
        }
    }
}
