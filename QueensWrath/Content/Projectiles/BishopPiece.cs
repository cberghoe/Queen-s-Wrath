using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Projectiles
{
	public class BishopPiece : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			// IMPORTANT!!!
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			// IMPORTANT!!!
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 52;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			// IMPORTANT!!!
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 28;
			// IMPORTANT!!!
			Projectile.usesLocalNPCImmunity = true;
			// IMPORTANT!!!
			Projectile.localNPCHitCooldown = 15;
		}

		public override void AI()
		{
			Lighting.AddLight(Projectile.Center, 0.7f, 0.6f, 0.3f);

			Dust trail = Dust.NewDustPerfect(Projectile.Center, DustID.Enchanted_Gold, -Projectile.velocity * 0.15f, 0, default, 1.1f);
			trail.noGravity = true;

			Projectile.rotation = Projectile.velocity.X * 0.01f;

			// IMPORTANT!!!
			Projectile.localAI[0] += Projectile.velocity.Length();
			// IMPORTANT!!!
			float targetDistance = Projectile.ai[0];

			// IMPORTANT!!!
			if (Projectile.localAI[1] == 0 && Projectile.localAI[0] >= targetDistance)
			{
				Projectile.localAI[1] = 1;

				// IMPORTANT!!!
				if ((int)Projectile.ai[1] == 0)
				{
					TriggerCrossEffect(Projectile.Center);
				}
			}
		}

		private void TriggerCrossEffect(Vector2 center)
		{
			SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f, Pitch = 0.2f }, center);
			SoundEngine.PlaySound(SoundID.Item105 with { Volume = 0.8f }, center);

			for (int i = 0; i < 4; i++)
			{
				Vector2 dir = (MathHelper.PiOver4 + i * MathHelper.PiOver2).ToRotationVector2();
				for (int k = 1; k <= 12; k++)
				{
					Dust d = Dust.NewDustPerfect(center + dir * (k * 10f), DustID.GoldFlame, dir * 3f, 0, Color.Gold, 1.3f);
					d.noGravity = true;
				}
			}

			for (int k = 0; k < 18; k++)
			{
				Dust d = Dust.NewDustPerfect(center, DustID.SpectreStaff, Main.rand.NextVector2Circular(6f, 6f), 0, Color.White, 1.2f);
				d.noGravity = true;
			}
		}

		// IMPORTANT!!!
		public override Color? GetAlpha(Color lightColor)
		{
			return Color.White;
		}
	}
}
