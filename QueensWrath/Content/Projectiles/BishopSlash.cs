using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Projectiles
{
	public class BishopSlash : ModProjectile
	{
		private static readonly Vector2 DrawOrigin = new Vector2(128f, 128f);

		public override void SetStaticDefaults()
		{
			// IMPORTANT: TrailingMode 2 records old positions and rotations for slash movement
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 110;
			Projectile.height = 110;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 22;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			if (Projectile.velocity != Vector2.Zero)
			{
				Projectile.rotation = Projectile.velocity.ToRotation();
			}

			if (Main.rand.NextBool(2))
			{
				Vector2 arcOffset = Main.rand.NextVector2Circular(35f, 35f);
				Dust d = Dust.NewDustPerfect(
					Projectile.Center + arcOffset,
					DustID.GoldFlame,
					-Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(2f, 2f),
					0, Color.Gold, 1.4f);
				d.noGravity = true;
			}

			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(
					Projectile.Center + Main.rand.NextVector2Circular(25f, 25f),
					DustID.Enchanted_Gold,
					-Projectile.velocity * 0.1f,
					0, Color.White, 1.2f);
				d.noGravity = true;
			}

			Lighting.AddLight(Projectile.Center, 0.95f, 0.75f, 0.25f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 drawPos = Projectile.Center - Main.screenPosition;

			float progress = (22 - Projectile.timeLeft) / 22f;
			float scale = MathHelper.Lerp(0.85f, 1.45f, progress);
			float alpha = (float)Math.Sin(progress * Math.PI);

			// IMPORTANT: Must restore default AlphaBlend SpriteBatch state after Additive blending
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.Additive,
				SamplerState.LinearClamp,
				DepthStencilState.None,
				RasterizerState.CullNone,
				null,
				Main.GameViewMatrix.TransformationMatrix
			);

			Color outerColor = new Color(255, 140, 30, 200) * (alpha * 0.8f);
			Main.EntitySpriteDraw(texture, drawPos, null, outerColor, Projectile.rotation, DrawOrigin, scale * 1.15f, SpriteEffects.None, 0);

			Color midColor = new Color(255, 220, 70, 230) * (alpha * 0.95f);
			Main.EntitySpriteDraw(texture, drawPos, null, midColor, Projectile.rotation, DrawOrigin, scale, SpriteEffects.None, 0);

			Color coreColor = new Color(255, 255, 255, 255) * (alpha * 0.9f);
			Main.EntitySpriteDraw(texture, drawPos, null, coreColor, Projectile.rotation, DrawOrigin, scale * 0.88f, SpriteEffects.None, 0);

			// IMPORTANT: Must restore default AlphaBlend SpriteBatch state after Additive blending
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				Main.DefaultSamplerState,
				DepthStencilState.None,
				RasterizerState.CullNone,
				null,
				Main.GameViewMatrix.TransformationMatrix
			);

			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			float dist = Vector2.Distance(target.Center, Projectile.Center);
			if (dist <= 75f)
			{
				modifiers.SetCrit();
				modifiers.SourceDamage *= 1.4f;
			}
		}
	}
}
