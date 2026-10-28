using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Projectiles
{
	public class BishopPiece : ModProjectile
	{
		private static Asset<Texture2D> TrailTexture;

		private static readonly SoundStyle DetonateExplosionSound = SoundID.Item14 with {
			Volume = 0.9f,
			Pitch = 0.1f,
			MaxInstances = 2,
			SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest
		};

		private static readonly SoundStyle DetonateChimeSound = SoundID.Item105 with {
			Volume = 0.85f,
			Pitch = -0.1f,
			MaxInstances = 2,
			SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest
		};

		public override void Load()
		{
			TrailTexture = ModContent.Request<Texture2D>("QueensWrath/Content/Projectiles/BishopTrail");
		}

		public override void Unload()
		{
			TrailTexture = null;
		}

		public override void SetStaticDefaults()
		{
			// IMPORTANT: TrailingMode 2 records old positions and rotations for afterimages and ribbon trail
			ProjectileID.Sets.TrailCacheLength[Type] = 16;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 52;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 45;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		public override void AI()
		{
			Lighting.AddLight(Projectile.Center, 0.95f, 0.8f, 0.35f);

			Projectile.rotation = Projectile.velocity.X * 0.015f;

			Vector2 prevCenter = Projectile.Center - Projectile.velocity;
			for (float f = 0f; f <= 1f; f += 0.25f)
			{
				Vector2 p = Vector2.Lerp(prevCenter, Projectile.Center, f);

				Dust d = Dust.NewDustPerfect(
					p,
					DustID.GoldFlame,
					-Projectile.velocity * 0.06f + Main.rand.NextVector2Circular(1f, 1f),
					0,
					Color.Gold,
					1.45f
				);
				d.noGravity = true;

				if (Main.rand.NextBool(3))
				{
					Dust spark = Dust.NewDustPerfect(
						p,
						DustID.SpectreStaff,
						Main.rand.NextVector2Circular(0.8f, 0.8f),
						0,
						Color.White,
						1.15f
					);
					spark.noGravity = true;
				}
			}

			int flightFrame = (int)Projectile.localAI[2];
			Projectile.localAI[2]++;

			if (Main.myPlayer == Projectile.owner && flightFrame % 3 == 0)
			{
				Projectile.NewProjectile(
					Projectile.GetSource_FromThis(),
					Projectile.Center,
					Vector2.Zero,
					ModContent.ProjectileType<BishopPhantomTrail>(),
					0,
					0f,
					Projectile.owner,
					Projectile.rotation
				);
			}

			Projectile.localAI[0] += Projectile.velocity.Length();
			float targetDistance = Projectile.ai[0];

			if (Projectile.localAI[1] == 0f && Projectile.localAI[0] >= targetDistance - 20f)
			{
				Projectile.localAI[1] = 1f;

				if (Main.myPlayer == Projectile.owner)
				{
					Vector2 slashDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
					Vector2 slashSpeed = slashDir * 12f;
					int slashType = ModContent.ProjectileType<BishopSlash>();

					Projectile.NewProjectile(
						Projectile.GetSource_FromThis(),
						Projectile.Center,
						slashSpeed,
						slashType,
						Projectile.damage,
						Projectile.knockBack,
						Projectile.owner
					);
				}

				// IMPORTANT: Only one bishop triggers the central explosion and camera punch to prevent duplicate stacking
				if ((int)Projectile.ai[1] == 0)
				{
					TriggerCenterImpact(Projectile.Center);
				}

				Projectile.Kill();
			}
		}

		private void TriggerCenterImpact(Vector2 center)
		{
			SoundEngine.PlaySound(DetonateExplosionSound, center);
			SoundEngine.PlaySound(DetonateChimeSound, center);

			if (Main.myPlayer == Projectile.owner)
			{
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(
					center,
					Main.rand.NextVector2Unit(),
					7f,
					18f,
					16,
					1500f,
					"QueensWrath:BishopConvergence"
				));
			}

			for (int k = 0; k < 35; k++)
			{
				Dust d = Dust.NewDustPerfect(center, DustID.GoldFlame, Main.rand.NextVector2Circular(8f, 8f), 0, Color.Gold, 1.6f);
				d.noGravity = true;
			}

			for (int k = 0; k < 25; k++)
			{
				Dust d = Dust.NewDustPerfect(center, DustID.SpectreStaff, Main.rand.NextVector2Circular(6f, 6f), 0, Color.White, 1.4f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D bishopTex = TextureAssets.Projectile[Type].Value;
			Vector2 bishopOrigin = new Vector2(bishopTex.Width * 0.5f, bishopTex.Height * 0.5f);

			Texture2D trailTex = TrailTexture?.Value;
			if (trailTex == null) return false;

			Vector2 trailOrigin = new Vector2(0f, trailTex.Height * 0.5f);

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

			for (int k = 0; k < Projectile.oldPos.Length - 1; k++)
			{
				Vector2 p0 = Projectile.oldPos[k];
				Vector2 p1 = Projectile.oldPos[k + 1];
				if (p0 == Vector2.Zero || p1 == Vector2.Zero) continue;

				Vector2 diff = p1 - p0;
				float dist = diff.Length();
				if (dist <= 0.1f) continue;

				float rot = diff.ToRotation();
				float progress = 1f - (k / (float)Projectile.oldPos.Length);
				float widthScale = (0.5f + 0.5f * progress) * 1.1f;

				Vector2 drawPos = p0 - Main.screenPosition + bishopOrigin;
				Color trailColor = new Color(255, 200, 60, 200) * (progress * 0.85f);
				Main.EntitySpriteDraw(trailTex, drawPos, null, trailColor, rot, trailOrigin, new Vector2(dist / trailTex.Width, widthScale), SpriteEffects.None, 0);

				Color innerColor = new Color(255, 255, 220, 255) * (progress * 0.95f);
				Main.EntitySpriteDraw(trailTex, drawPos, null, innerColor, rot, trailOrigin, new Vector2(dist / trailTex.Width, widthScale * 0.5f), SpriteEffects.None, 0);
			}

			for (int k = Projectile.oldPos.Length - 1; k >= 0; k -= 2)
			{
				if (Projectile.oldPos[k] == Vector2.Zero) continue;
				float trailAlpha = (Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length;
				Vector2 drawPos = Projectile.oldPos[k] - Main.screenPosition + bishopOrigin;
				float rot = (k < Projectile.oldRot.Length) ? Projectile.oldRot[k] : Projectile.rotation;

				Color ghostColor = new Color(255, 180, 50, 180) * (trailAlpha * 0.55f);
				Main.EntitySpriteDraw(bishopTex, drawPos, null, ghostColor, rot, bishopOrigin, Projectile.scale * (0.85f + 0.15f * trailAlpha), SpriteEffects.None, 0);
			}

			Vector2 activeDrawPos = Projectile.Center - Main.screenPosition;
			Color glowColor = new Color(255, 220, 100, 200) * 0.75f;
			Main.EntitySpriteDraw(bishopTex, activeDrawPos, null, glowColor, Projectile.rotation, bishopOrigin, Projectile.scale * 1.15f, SpriteEffects.None, 0);

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

			Main.EntitySpriteDraw(bishopTex, activeDrawPos, null, Color.White, Projectile.rotation, bishopOrigin, Projectile.scale, SpriteEffects.None, 0);

			return false;
		}

		public override Color? GetAlpha(Color lightColor)
		{
			return Color.White;
		}
	}
}
