using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using QueensWrath.Content.Items;

namespace QueensWrath.Content.NPCs
{
	public class KingPiece : ModNPC
	{
		private const int PawnCount = 6;
		private const float AngleStep = MathHelper.TwoPi / PawnCount;
		private const float BaseRadiusX = 145f;
		private const float BaseRadiusY = 38f;
		private const float BaseTiltAngle = -0.18f;
		private const int TrailSegments = 18;
		private const float TrailAngleStep = 0.035f;

		private static Asset<Texture2D> kingTexture;
		private static Asset<Texture2D> pawnTexture;
		private static Asset<Texture2D> glowTexture;

		private float orbitTimer;

		public override void Load()
		{
			kingTexture = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/KingPiece");
			pawnTexture = ModContent.Request<Texture2D>("QueensWrath/Content/Projectiles/PawnOrbit");
			glowTexture = TextureAssets.Extra[ExtrasID.SharpTears];
		}

		public override void Unload()
		{
			kingTexture = null;
			pawnTexture = null;
			glowTexture = null;
		}

		public override void SetStaticDefaults()
		{
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 96;
			NPC.height = 192;
			NPC.damage = 50;
			NPC.defense = 14;
			NPC.lifeMax = 3500;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 10000f;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = -1;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.boss = true;
			Music = MusicID.Boss1;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PawnPwner>(), 1));
		}

		// Spawn Queen boss
		public override void OnKill()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				int queenIndex = NPC.NewNPC(NPC.GetSource_Death(), (int)NPC.Center.X, (int)NPC.Center.Y - 50, ModContent.NPCType<QueenPiece>());
				if (queenIndex < Main.maxNPCs)
				{
					Main.npc[queenIndex].target = NPC.target;
					NetMessage.SendData(MessageID.SyncNPC, number: queenIndex);
				}
			}

			if (!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.9f }, NPC.Center);
				for (int i = 0; i < 40; i++)
				{
					Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 0f, 100, default, 1.8f);
					dust.velocity = Main.rand.NextVector2Circular(6f, 6f);
					dust.noGravity = true;
				}
			}
		}

		public override void AI()
		{
			orbitTimer += 0.035f;

			if (NPC.target < 0 || NPC.target >= Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
			{
				NPC.TargetClosest(true);
			}

			Player target = Main.player[NPC.target];
			if (target.dead || !target.active)
			{
				NPC.velocity.Y -= 0.35f;
				NPC.EncourageDespawn(10);
				return;
			}

			// Hover flight AI
			float hoverSwayX = MathF.Sin(orbitTimer * 0.45f) * 160f;
			float hoverBobY = MathF.Sin(orbitTimer * 1.1f) * 22f;
			Vector2 targetHoverPos = target.Center + new Vector2(hoverSwayX, -220f + hoverBobY);

			Vector2 toTarget = targetHoverPos - NPC.Center;
			float distance = toTarget.Length();

			const float maxSpeed = 7.5f;
			const float inertia = 26f;

			if (distance > 15f)
			{
				toTarget *= maxSpeed / distance;
				NPC.velocity = (NPC.velocity * (inertia - 1f) + toTarget) / inertia;
			}
			else
			{
				NPC.velocity *= 0.95f;
			}

			NPC.rotation = NPC.velocity.X * 0.015f;
			NPC.spriteDirection = target.Center.X >= NPC.Center.X ? 1 : -1;

			// Ascending energy wisps
			if (!Main.dedServ && Main.rand.NextBool(2))
			{
				Vector2 dustPos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
				Dust dust = Dust.NewDustDirect(dustPos, 4, 4, DustID.Shadowflame, 0, -Main.rand.NextFloat(1.2f, 2.4f), 120, default, Main.rand.NextFloat(0.9f, 1.3f));
				dust.noGravity = true;
				dust.velocity.X *= 0.25f;
			}

			// Dynamic orbit breathing
			float breath = MathF.Sin(orbitTimer * 1.5f);
			float dynamicRadiusX = BaseRadiusX + breath * 10f;
			float dynamicRadiusY = BaseRadiusY + breath * 3.5f;
			float dynamicTilt = BaseTiltAngle + MathF.Sin(orbitTimer * 0.8f) * 0.035f;

			float cosTilt = MathF.Cos(dynamicTilt);
			float sinTilt = MathF.Sin(dynamicTilt);

			// Precompute pawn positions
			Span<Vector2> pawnCenters = stackalloc Vector2[PawnCount];
			for (int i = 0; i < PawnCount; i++)
			{
				float baseAngle = orbitTimer + i * AngleStep;
				float rawX = MathF.Cos(baseAngle) * dynamicRadiusX;
				float rawY = MathF.Sin(baseAngle) * dynamicRadiusY;
				pawnCenters[i] = NPC.Center + new Vector2(rawX * cosTilt - rawY * sinTilt, rawX * sinTilt + rawY * cosTilt);
			}

			// Player collision damage
			float maxOrbitRadiusSq = (dynamicRadiusX + 50f) * (dynamicRadiusX + 50f);
			for (int p = 0; p < Main.maxPlayers; p++)
			{
				Player player = Main.player[p];
				if (!player.active || player.dead || player.immune || player.hurtCooldowns[0] > 0)
					continue;

				if (Vector2.DistanceSquared(player.Center, NPC.Center) > maxOrbitRadiusSq)
					continue;

				for (int i = 0; i < PawnCount; i++)
				{
					Vector2 pawnCenter = pawnCenters[i];
					if (player.Hitbox.Intersects(Utils.CenteredRectangle(pawnCenter, new Vector2(28f, 28f))))
					{
						int hitDirection = player.Center.X >= pawnCenter.X ? 1 : -1;
						player.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), NPC.damage / 2, hitDirection);
						if (!Main.dedServ)
						{
							SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.6f, Pitch = -0.2f }, pawnCenter);
						}
						break;
					}
				}
			}

			// Projectile shield defense
			const float pawnShieldRadiusSq = 24f * 24f;
			float shieldReachSq = (dynamicRadiusX + 30f) * (dynamicRadiusX + 30f);

			for (int j = 0; j < Main.maxProjectiles; j++)
			{
				Projectile proj = Main.projectile[j];
				if (!proj.active || !proj.friendly || proj.hostile || proj.damage <= 0)
					continue;

				if (Vector2.DistanceSquared(proj.Center, NPC.Center) > shieldReachSq)
					continue;

				for (int i = 0; i < PawnCount; i++)
				{
					Vector2 pawnCenter = pawnCenters[i];
					if (Vector2.DistanceSquared(proj.Center, pawnCenter) < pawnShieldRadiusSq)
					{
						proj.Kill();
						if (!Main.dedServ)
						{
							SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.6f, Pitch = 0.2f }, pawnCenter);
							for (int d = 0; d < 6; d++)
							{
								Dust dust = Dust.NewDustDirect(pawnCenter - new Vector2(6, 6), 12, 12, DustID.Shadowflame, 0, 0, 100, default, 1.2f);
								dust.velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
								dust.noGravity = true;
							}
						}
						break;
					}
				}
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			DrawKingBodyAura(spriteBatch, screenPos);
			DrawPawnHalo(spriteBatch, screenPos, drawColor, isFront: false);
			return true;
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			DrawPawnHalo(spriteBatch, screenPos, drawColor, isFront: true);
		}

		// Body silhouette aura
		private void DrawKingBodyAura(SpriteBatch spriteBatch, Vector2 screenPos)
		{
			if (kingTexture == null || !kingTexture.IsLoaded || glowTexture == null || !glowTexture.IsLoaded)
				return;

			Texture2D kingTex = kingTexture.Value;
			Vector2 kingOrigin = new Vector2(kingTex.Width * 0.5f, kingTex.Height * 0.5f);
			Vector2 drawCenter = NPC.Center - screenPos;

			Texture2D glowTex = glowTexture.Value;
			Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);

			// Radial core glow
			float pulse = MathF.Sin(orbitTimer * 2f);
			float glowScale = 2.6f + pulse * 0.25f;
			Color coreGlowColor = new Color(130, 35, 235, 0) * (0.4f + pulse * 0.1f);
			spriteBatch.Draw(glowTex, drawCenter, null, coreGlowColor, orbitTimer * 0.2f, glowOrigin, glowScale, SpriteEffects.None, 0f);

			// Pulsating silhouette outline
			float auraDist = 5f + MathF.Sin(orbitTimer * 3.2f) * 3f;
			float auraAlpha = 0.35f + MathF.Sin(orbitTimer * 3.2f) * 0.15f;
			Color auraColor = new Color(175, 55, 255, 0) * auraAlpha;

			SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

			const float auraAngleStep = MathHelper.TwoPi / 6f;
			float baseAuraAngle = orbitTimer * 0.5f;

			for (int a = 0; a < 6; a++)
			{
				float angle = a * auraAngleStep + baseAuraAngle;
				Vector2 auraOffset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * auraDist;
				spriteBatch.Draw(kingTex, drawCenter + auraOffset, null, auraColor, NPC.rotation, kingOrigin, NPC.scale, effects, 0f);
			}
		}

		// 3D pawn halo
		private void DrawPawnHalo(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, bool isFront)
		{
			if (pawnTexture == null || !pawnTexture.IsLoaded || glowTexture == null || !glowTexture.IsLoaded)
				return;

			Texture2D pawnTex = pawnTexture.Value;
			Vector2 pawnOrigin = new Vector2(pawnTex.Width * 0.5f, pawnTex.Height * 0.5f);

			Texture2D glowTex = glowTexture.Value;
			Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);

			float breath = MathF.Sin(orbitTimer * 1.5f);
			float dynamicRadiusX = BaseRadiusX + breath * 10f;
			float dynamicRadiusY = BaseRadiusY + breath * 3.5f;
			float dynamicTilt = BaseTiltAngle + MathF.Sin(orbitTimer * 0.8f) * 0.035f;

			float cosTilt = MathF.Cos(dynamicTilt);
			float sinTilt = MathF.Sin(dynamicTilt);

			for (int i = 0; i < PawnCount; i++)
			{
				float baseAngle = orbitTimer + i * AngleStep;

				// Trail segments
				for (int k = TrailSegments; k >= 1; k--)
				{
					float historicAngle = baseAngle - k * TrailAngleStep;
					float histSin = MathF.Sin(historicAngle);
					bool segmentIsFront = histSin >= 0f;

					if (segmentIsFront != isFront)
						continue;

					float rawHistX = MathF.Cos(historicAngle) * dynamicRadiusX;
					float rawHistY = histSin * dynamicRadiusY;
					Vector2 histOffset = new Vector2(rawHistX * cosTilt - rawHistY * sinTilt, rawHistX * sinTilt + rawHistY * cosTilt);
					Vector2 trailDrawPos = NPC.Center + histOffset - screenPos;

					float progress = 1f - (k / (float)TrailSegments);
					float opacity = progress * 0.75f;

					float histScale = histSin < 0f
						? MathHelper.Lerp(1.0f, 0.85f, -histSin)
						: MathHelper.Lerp(1.0f, 1.1f, histSin);

					float trailScale = histScale * MathHelper.Lerp(0.35f, 0.85f, progress);
					Color trailColor = new Color(150, 40, 255, 0) * opacity;

					spriteBatch.Draw(glowTex, trailDrawPos, null, trailColor, 0f, glowOrigin, trailScale, SpriteEffects.None, 0f);
				}

				// Pawn piece rendering
				float sinVal = MathF.Sin(baseAngle);
				bool pawnIsFront = sinVal >= 0f;

				if (pawnIsFront != isFront)
					continue;

				float rawX = MathF.Cos(baseAngle) * dynamicRadiusX;
				float rawY = sinVal * dynamicRadiusY;
				Vector2 offset = new Vector2(rawX * cosTilt - rawY * sinTilt, rawX * sinTilt + rawY * cosTilt);
				Vector2 drawPos = NPC.Center + offset - screenPos;

				float scale;
				Color pawnColor;

				if (sinVal < 0f)
				{
					float depthFactor = -sinVal;
					scale = MathHelper.Lerp(1.0f, 0.85f, depthFactor);
					pawnColor = Color.Lerp(drawColor, Color.Indigo, 0.4f * depthFactor);
				}
				else
				{
					float depthFactor = sinVal;
					scale = MathHelper.Lerp(1.0f, 1.1f, depthFactor);
					pawnColor = drawColor;
				}

				float swayRotation = sinVal * 0.12f;

				Color haloGlow = new Color(160, 50, 255, 0) * (sinVal < 0f ? 0.35f : 0.6f);
				spriteBatch.Draw(glowTex, drawPos, null, haloGlow, 0f, glowOrigin, scale * 0.75f, SpriteEffects.None, 0f);

				spriteBatch.Draw(pawnTex, drawPos, null, pawnColor, swayRotation, pawnOrigin, scale, SpriteEffects.None, 0f);
			}
		}
	}
}
