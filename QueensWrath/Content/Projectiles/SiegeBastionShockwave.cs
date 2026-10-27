using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Projectiles
{
    public class SiegeBastionShockwave : ModProjectile
    {
        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 20;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.6f;
            Projectile.alpha = 80;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI() {
            Projectile.velocity *= 1.04f;
            if (Projectile.velocity != Vector2.Zero) {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            for (int i = 0; i < 3; i++) {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * Main.rand.NextFloat(0.3f),
                    DustID.Vortex,
                    -Projectile.velocity * 0.1f,
                    0, default, 1.4f);
                d.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.3f, 0.8f, 0.6f);
        }

        public override Color? GetAlpha(Color lightColor) {
            return new Color(100, 255, 200, 150);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            Vector2 halfSize = new Vector2(Projectile.width * 0.5f, Projectile.height * 0.5f);

            for (int k = Projectile.oldPos.Length - 1; k > 0; k--) {
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + halfSize;
                Color color = new Color(100, 255, 200, 100) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length) * 0.4f;
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale * 0.9f, SpriteEffects.None, 0);
            }
            return true;
        }

        public override void OnKill(int timeLeft) {
            for (int i = 0; i < 10; i++) {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex,
                    Main.rand.NextVector2Circular(4f, 4f), 0, default, 1.2f);
                d.noGravity = true;
            }
        }
    }
}
