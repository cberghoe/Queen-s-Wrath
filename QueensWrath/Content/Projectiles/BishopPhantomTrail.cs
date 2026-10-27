using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace QueensWrath.Content.Projectiles
{
	public class BishopPhantomTrail : ModProjectile
	{
		public override string Texture => "QueensWrath/Content/Projectiles/BishopPiece";

		public const int LifeTime = 26;

		public override void SetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 52;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = LifeTime;
			Projectile.aiStyle = -1;
			Projectile.netImportant = false;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Projectile.rotation = Projectile.ai[0];
			}

			Projectile.scale += 0.006f;

			float progress = Projectile.timeLeft / (float)LifeTime;
			Lighting.AddLight(Projectile.Center, 0.75f * progress, 0.55f * progress, 0.2f * progress);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D bishopTex = TextureAssets.Projectile[Type].Value;
			Vector2 origin = new Vector2(bishopTex.Width * 0.5f, bishopTex.Height * 0.5f);
			Vector2 drawPos = Projectile.Center - Main.screenPosition;

			float progress = Projectile.timeLeft / (float)LifeTime;
			float alpha = progress * progress;

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

			Color glowColor = new Color(255, 170, 40, 200) * (alpha * 0.7f);
			Main.EntitySpriteDraw(bishopTex, drawPos, null, glowColor, Projectile.rotation, origin, Projectile.scale * 1.15f, SpriteEffects.None, 0);

			Color coreColor = new Color(255, 240, 160, 220) * (alpha * 0.85f);
			Main.EntitySpriteDraw(bishopTex, drawPos, null, coreColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

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
	}
}
