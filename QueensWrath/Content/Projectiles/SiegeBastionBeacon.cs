using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Projectiles
{
    public class SiegeBastionBeacon : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 28;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            // IMPORTANT: tileCollide must be false so the beacon does not prematurely get destroyed by blocks
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.9f;
            Projectile.alpha = 0;
            Projectile.scale = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI() {
            Projectile.rotation = 0f;

            // IMPORTANT: Stops at max distance (45 tiles = 720 px) and hovers so it remains within castling range
            Projectile.localAI[0] += Projectile.velocity.Length();
            if (Projectile.localAI[0] >= 45f * 16f) {
                Projectile.velocity *= 0.88f;
                if (Projectile.velocity.Length() < 0.2f) {
                    Projectile.velocity = Vector2.Zero;
                }
            }

            if (Main.rand.NextBool(2)) {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.LunarOre,
                    -Projectile.velocity * 0.2f,
                    0, default, 1.3f);
                d.noGravity = true;
            }

            if (Main.rand.NextBool(3)) {
                Dust g = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.Granite,
                    Vector2.Zero, 0, default, 1.1f);
                g.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.4f, 0.9f, 0.7f);

            Player player = Main.player[Projectile.owner];
            if (player.active && !player.dead && Main.rand.NextBool(4)) {
                Vector2 dirToPlayer = player.Center - Projectile.Center;
                float dist = dirToPlayer.Length();
                if (dist > 30f) {
                    float step = Main.rand.NextFloat(0.1f, 0.9f);
                    Vector2 pPos = Projectile.Center + dirToPlayer * step;
                    Dust linkDust = Dust.NewDustPerfect(pPos, DustID.Vortex, Vector2.Zero, 0, default, 0.7f);
                    linkDust.noGravity = true;
                }
            }
        }

        public override void OnKill(int timeLeft) {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.85f, Pitch = -0.2f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item105 with { Volume = 0.7f }, Projectile.Center);

            for (int i = 0; i < 25; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.LunarOre,
                    Main.rand.NextVector2Circular(7f, 7f), 0, default, 1.4f);
                d.noGravity = true;
            }
            for (int i = 0; i < 15; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Granite,
                    Main.rand.NextVector2Circular(5f, 5f), 0, default, 1.2f);
                d.noGravity = true;
            }

            if (Main.myPlayer == Projectile.owner) {
                int shockwaveType = ModContent.ProjectileType<SiegeBastionShockwave>();
                int shockwaveDamage = (int)(Projectile.damage * 0.5f);
                float shockwaveSpeed = 13f;

                Vector2[] directions = new Vector2[] {
                    new Vector2(0f, -1f),
                    new Vector2(0f, 1f),
                    new Vector2(-1f, 0f),
                    new Vector2(1f, 0f)
                };

                foreach (Vector2 dir in directions) {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        dir * shockwaveSpeed,
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
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + halfSize;
                Color color = new Color(80, 240, 200, 140) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length) * 0.5f;
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }

            Player player = Main.player[Projectile.owner];
            if (player.active && !player.dead) {
                Vector2 start = Projectile.Center - Main.screenPosition;
                Vector2 end = player.Center - Main.screenPosition;
                Vector2 diff = end - start;
                float length = diff.Length();
                if (length > 0f) {
                    float rotation = (float)Math.Atan2(diff.Y, diff.X);
                    Texture2D pixel = TextureAssets.MagicPixel.Value;
                    Color beamColor = new Color(100, 255, 200, 40) * 0.4f;
                    Main.EntitySpriteDraw(pixel, start, new Rectangle(0, 0, 1, 1), beamColor, rotation, Vector2.Zero, new Vector2(length, 2f), SpriteEffects.None, 0);
                }
            }

            return true;
        }

        public override Color? GetAlpha(Color lightColor) {
            return new Color(180, 255, 230, 200);
        }
    }
}
