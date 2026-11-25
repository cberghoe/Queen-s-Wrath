using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using QueensWrath.Content.NPCs;

namespace QueensWrath.Content.Projectiles
{
	public class KingThrownPawn : ModProjectile
	{
		private const float HomingDelay = 35f;
		private const float HomingDuration = 90f;
		private const float HomingAcceleration = 0.12f;
		private const float MaximumSpeed = 11f;
		private static Asset<Texture2D> pawnTexture;

		public override void Load()
		{
			pawnTexture = ModContent.Request<Texture2D>("QueensWrath/Content/Projectiles/PawnOrbit");
		}

		public override void Unload()
		{
			pawnTexture = null;
		}

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 10;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = 1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 360;
			Projectile.light = 0.35f;
			Projectile.alpha = 20;
		}

		public override void AI()
		{
			Projectile.localAI[0]++;

			if (Projectile.localAI[0] <= HomingDelay)
			{
				Projectile.velocity *= 0.985f;
			}
			else if (Projectile.localAI[0] <= HomingDelay + HomingDuration)
			{
				int targetIndex = (int)Projectile.ai[0];
				if (targetIndex >= 0 && targetIndex < Main.maxPlayers)
				{
					Player target = Main.player[targetIndex];
					if (target.active && !target.dead)
					{
						Vector2 toTarget = target.Center - Projectile.Center;
						if (toTarget.LengthSquared() > 1f)
						{
							Vector2 desiredVelocity = toTarget.SafeNormalize(Vector2.Zero) * MaximumSpeed;
							Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, HomingAcceleration);
						}
					}
				}
			}

			Projectile.rotation += Projectile.ai[1] + Projectile.velocity.X * 0.008f;

			if (!Main.dedServ)
			{
				Dust dust = Dust.NewDustPerfect(
					Projectile.Center - Projectile.velocity * 0.3f,
					DustID.SilverCoin,
					-Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.6f, 0.6f),
					0,
					Color.White,
					Main.rand.NextFloat(0.9f, 1.3f));
				dust.noGravity = true;

				Lighting.AddLight(Projectile.Center, 0.7f, 0.75f, 1f);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (pawnTexture == null || !pawnTexture.IsLoaded)
				return false;

			Texture2D texture = pawnTexture.Value;
			Vector2 origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
			Vector2 drawPosition = Projectile.Center - Main.screenPosition;

			for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
			{
				float fade = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
				Vector2 oldPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
				Color trailColor = new Color(225, 230, 255, 0) * (fade * 0.55f);
				Main.EntitySpriteDraw(texture, oldPosition, null, trailColor, Projectile.oldRot[i], origin, Projectile.scale, SpriteEffects.None, 0);
			}

			Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info)
		{
			Projectile.Kill();
		}

		public override void OnKill(int timeLeft)
		{
			int encodedPawnAndKing = (int)Projectile.ai[2];
			int pawnIndex = encodedPawnAndKing / 1000;
			int kingIndex = encodedPawnAndKing % 1000;
			if (kingIndex >= 0 && kingIndex < Main.maxNPCs)
			{
				NPC king = Main.npc[kingIndex];
				if (king.active && king.ModNPC is KingPiece kingPiece)
					kingPiece.RestoreOrbitPawn(pawnIndex);
			}

			if (!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.45f, Pitch = -0.15f }, Projectile.Center);

				for (int i = 0; i < 12; i++)
				{
					Dust dust = Dust.NewDustDirect(
						Projectile.position,
						Projectile.width,
						Projectile.height,
						DustID.SilverCoin,
						0f,
						0f,
						100,
						Color.White,
						Main.rand.NextFloat(0.9f, 1.4f));
					dust.velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
					dust.noGravity = true;
				}
			}
		}
	}
}
