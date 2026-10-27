using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Projectiles
{
    public class SiegeBastionProjectile : ModProjectile
    {
        private static readonly Vector2[] CrossDirections = new Vector2[] {
            -Vector2.UnitY,
            Vector2.UnitY,
            -Vector2.UnitX,
            Vector2.UnitX
        };

        private static readonly SoundStyle DetonateExplosionSound = SoundID.Item14 with {
            Volume = 0.9f,
            Pitch = -0.2f,
            MaxInstances = 3,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest
        };

        private static readonly SoundStyle DetonateChimeSound = SoundID.Item105 with {
            Volume = 0.7f,
            MaxInstances = 3,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest
        };

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            // IMPORTANT: TrailingMode 2 records old positions and rotations for afterimages
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 24;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 7;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.light = 0.8f;
            Projectile.alpha = 0;
            Projectile.scale = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.extraUpdates = 1;
        }

        public override void AI() {
            // IMPORTANT: Sprite points UP by default; +Pi/2 rotates it to align with velocity
            if (Projectile.velocity != Vector2.Zero) {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }

            // IMPORTANT: Swapping width and height on horizontal flight aligns the AABB hitbox with the rotated sprite
            if (Projectile.localAI[1] == 0f) {
                Projectile.localAI[1] = 1f;
                if (Math.Abs(Projectile.velocity.X) > Math.Abs(Projectile.velocity.Y)) {
                    int oldW = Projectile.width;
                    int oldH = Projectile.height;
                    Projectile.width = oldH;
                    Projectile.height = oldW;
                    Projectile.position.X -= (Projectile.width - oldW) / 2f;
                    Projectile.position.Y -= (Projectile.height - oldH) / 2f;
                }
            }

            for (int i = 0; i < 2; i++) {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * Main.rand.NextFloat(0.5f),
                    DustID.LunarOre,
                    -Projectile.velocity * 0.05f + Main.rand.NextVector2Circular(1f, 1f),
                    0, default, 1.1f);
                d.noGravity = true;
            }

            if (Main.rand.NextBool(3)) {
                Dust g = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.Granite,
                    Vector2.Zero, 0, default, 0.8f);
                g.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.4f, 0.9f, 0.7f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Detonate();
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft) {
            Detonate();
        }

        private void Detonate() {
            if (Projectile.ai[0] == 1f) return;
            Projectile.ai[0] = 1f;

            SoundEngine.PlaySound(DetonateExplosionSound, Projectile.Center);
            SoundEngine.PlaySound(DetonateChimeSound, Projectile.Center);

            for (int i = 0; i < 30; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.LunarOre,
                    Main.rand.NextVector2Circular(8f, 8f), 0, default, 1.5f);
                d.noGravity = true;
            }
            for (int i = 0; i < 15; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Granite,
                    Main.rand.NextVector2Circular(6f, 6f), 0, default, 1.3f);
                d.noGravity = true;
            }

            if (Main.myPlayer == Projectile.owner) {
                int shockwaveType = ModContent.ProjectileType<SiegeBastionShockwave>();
                int shockwaveDamage = (int)(Projectile.damage * 0.5f);
                float shockwaveSpeed = 14f;

                for (int i = 0; i < CrossDirections.Length; i++) {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        CrossDirections[i] * shockwaveSpeed,
                        shockwaveType,
                        shockwaveDamage,
                        Projectile.knockBack * 0.5f,
                        Projectile.owner
                    );
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            Vector2 halfSize = new Vector2(Projectile.width * 0.5f, Projectile.height * 0.5f);

            for (int k = Projectile.oldPos.Length - 1; k > 0; k--) {
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + halfSize + new Vector2(0f, Projectile.gfxOffY);
                Color color = new Color(100, 255, 200, 160) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length) * 0.5f;
                float rot = (Projectile.oldRot != null && k < Projectile.oldRot.Length) ? Projectile.oldRot[k] : Projectile.rotation;
                Main.EntitySpriteDraw(texture, drawPos, null, color, rot, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }

            Dust rookDust = Dust.NewDustDirect(
                new Vector2(Projectile.position.X + 3f, Projectile.position.Y + 3f) - Projectile.velocity * 0.5f,
                Projectile.width - 6, Projectile.height - 6, DustID.LunarOre, 0f, 0f, 100);
            rookDust.scale *= 1.5f + Main.rand.Next(10) * 0.1f;
            rookDust.velocity *= 0.2f;
            rookDust.noGravity = true;

            return true;
        }

        public override Color? GetAlpha(Color lightColor) {
            return Color.White;
        }
    }
}
