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

		private const float IntroDuration = 600f; // 10 seconds invulnerable intro stare

		private static Asset<Texture2D> kingTexture;
		private static Asset<Texture2D> pawnTexture;
		private static Asset<Texture2D> glowTexture;

		private float orbitTimer;
		private float introTimer;

		public bool IsInvulnerable => introTimer < IntroDuration;

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
			NPC.height = 200;
			NPC.damage = 50;
			NPC.defense = 14;
			NPC.lifeMax = 3500;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 10000f;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = -1;
			NPC.noGravity = false;
			NPC.noTileCollide = false;
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
					Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, Color.White, 1.8f);
					dust.velocity = Main.rand.NextVector2Circular(6f, 6f);
					dust.noGravity = true;
				}
			}
		}

		public override void AI()
		{
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

			// Bleed effect from clutching hand at midsection
			// Center offset for 168x208 sprite: X = +11.4f * spriteDirection, Y = -18.5f
			Vector2 woundPos = NPC.Center + new Vector2(NPC.spriteDirection * 11.4f, -18.5f);
			if (!Main.dedServ && Main.rand.NextBool(2))
			{
				Dust blood = Dust.NewDustPerfect(
					woundPos + Main.rand.NextVector2Circular(3f, 3f),
					DustID.Blood,
					new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(0.8f, 2.4f)),
					0,
					default,
					Main.rand.NextFloat(1.0f, 1.4f)
				);
				blood.noGravity = false;
			}

			// Initial ground snap on spawn
			if (introTimer == 0f)
			{
				int tileX = Math.Clamp((int)(NPC.Center.X / 16f), 10, Main.maxTilesX - 10);
				int tileY = Math.Clamp((int)(NPC.Bottom.Y / 16f), 10, Main.maxTilesY - 10);
				while (tileY < Main.maxTilesY - 10 && (!Main.tile[tileX, tileY].HasTile || !Main.tileSolid[Main.tile[tileX, tileY].TileType] || Main.tile[tileX, tileY].IsActuated))
				{
					tileY++;
				}
				NPC.Bottom = new Vector2(NPC.Center.X, tileY * 16f);
			}

			// Invulnerable intro phase: stay still on ground for 10 seconds, staring at player
			if (introTimer < IntroDuration)
			{
				introTimer++;
				NPC.dontTakeDamage = true;
				NPC.velocity.X = 0f;
				NPC.rotation = 0f;
				bool isPlayerRight = target.Center.X >= NPC.Center.X;
				NPC.spriteDirection = isPlayerRight ? 1 : -1;
				NPC.direction = NPC.spriteDirection;

				// Ambient silver motes around halo
				if (!Main.dedServ && Main.rand.NextBool(3))
				{
					Vector2 haloWorld = NPC.Center + new Vector2(0f, -112f);
					Dust d = Dust.NewDustPerfect(haloWorld + new Vector2(Main.rand.NextFloat(-22f, 22f), Main.rand.NextFloat(-6f, 6f)), DustID.SilverCoin, new Vector2(0f, -0.6f), 0, Color.White, 0.8f);
					d.noGravity = true;
				}

				// Intro finish: halo shatters, invulnerability drops
				if (introTimer >= IntroDuration)
				{
					NPC.dontTakeDamage = false;
					if (!Main.dedServ)
					{
						SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.85f, Pitch = 0.2f }, NPC.Center);
						for (int i = 0; i < 30; i++)
						{
							Vector2 vel = Main.rand.NextVector2Circular(6f, 3.5f);
							Dust d = Dust.NewDustPerfect(NPC.Center + new Vector2(0f, -112f), DustID.SilverCoin, vel, 0, Color.White, 1.4f);
							d.noGravity = true;
						}
					}
				}

				return; // Skip pawn attacks and movement during intro
			}

			// Post-intro: Active walking combat phase
			orbitTimer += 0.035f;

			// Always face player directly
			bool isTargetRight = target.Center.X >= NPC.Center.X;
			NPC.spriteDirection = isTargetRight ? 1 : -1;
			NPC.direction = NPC.spriteDirection;

			// Ground walking towards player
			float walkSpeed = 2.4f;
			float distToPlayerX = MathF.Abs(target.Center.X - NPC.Center.X);

			if (distToPlayerX > 35f)
			{
				float targetSpeedX = NPC.spriteDirection * walkSpeed;
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, targetSpeedX, 0.15f);
			}
			else
			{
				NPC.velocity.X *= 0.85f;
			}

			// Step up 1-tile bumps smoothly
			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);

			// Jump over walls / obstacles when walking against them
			if (NPC.collideX && (NPC.velocity.Y == 0f || NPC.collideY))
			{
				NPC.velocity.Y = -6.5f;
			}

			// Jump if player is on a higher ledge / platform
			if (target.Bottom.Y < NPC.Top.Y && NPC.velocity.Y == 0f && Main.rand.NextBool(50))
			{
				NPC.velocity.Y = -8f;
			}

			NPC.rotation = 0f;

			// Monochromatic ascending smoke & silver wisps
			if (!Main.dedServ && Main.rand.NextBool(2))
			{
				Vector2 dustPos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
				Dust dust = Dust.NewDustDirect(dustPos, 4, 4, DustID.Smoke, 0, -Main.rand.NextFloat(1.2f, 2.4f), 120, Color.White, Main.rand.NextFloat(0.9f, 1.3f));
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

			// Player collision damage from orbiting pawns
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
								Dust dust = Dust.NewDustDirect(pawnCenter - new Vector2(6, 6), 12, 12, DustID.SilverCoin, 0, 0, 100, Color.White, 1.2f);
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
			Texture2D tex = kingTexture?.Value ?? TextureAssets.Npc[Type].Value;
			if (tex == null) return false;

			bool isFacingRight = NPC.spriteDirection == 1;
			SpriteEffects effects = isFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
			Vector2 drawCenter = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);

			// 1. Back pawn halo & body aura (only after intro)
			if (!IsInvulnerable)
			{
				DrawKingBodyAura(spriteBatch, screenPos);
				DrawPawnHalo(spriteBatch, screenPos, drawColor, isFront: false);
			}

			// 2. Main king sprite solid drawing
			spriteBatch.Draw(tex, drawCenter, null, drawColor, NPC.rotation, origin, NPC.scale, effects, 0f);

			// 3. Front pawn halo (only after intro)
			if (!IsInvulnerable)
			{
				DrawPawnHalo(spriteBatch, screenPos, drawColor, isFront: true);
			}

			return false;
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (IsInvulnerable)
			{
				DrawInvulnerableHalo(spriteBatch, screenPos);
			}
		}

		// Floating invulnerable halo above head during intro
		private void DrawInvulnerableHalo(SpriteBatch spriteBatch, Vector2 screenPos)
		{
			if (glowTexture == null || !glowTexture.IsLoaded)
				return;

			Texture2D glowTex = glowTexture.Value;
			Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);

			float pulse = 0.5f + 0.5f * MathF.Sin(introTimer * 0.12f);
			float floatBob = MathF.Sin(introTimer * 0.08f) * 3.5f;
			Vector2 haloPos = NPC.Center - screenPos + new Vector2(0f, -112f + floatBob);

			// Outer ethereal silver halo ellipse
			Color outerColor = new Color(220, 225, 240, 0) * (0.55f + pulse * 0.25f);
			Vector2 outerScale = new Vector2(1.85f + pulse * 0.2f, 0.52f + pulse * 0.08f);
			spriteBatch.Draw(glowTex, haloPos, null, outerColor, 0f, glowOrigin, outerScale, SpriteEffects.None, 0f);

			// Core stark white halo light
			Color coreColor = new Color(255, 255, 255, 0) * (0.8f + pulse * 0.2f);
			Vector2 coreScale = new Vector2(1.25f, 0.34f);
			spriteBatch.Draw(glowTex, haloPos, null, coreColor, 0f, glowOrigin, coreScale, SpriteEffects.None, 0f);

			// 4 orbiting chess halo motes around the ring
			for (int i = 0; i < 4; i++)
			{
				float angle = introTimer * 0.045f + i * MathHelper.PiOver2;
				Vector2 moteOffset = new Vector2(MathF.Cos(angle) * 32f, MathF.Sin(angle) * 9f);
				Color moteColor = new Color(255, 255, 255, 0) * (0.75f + 0.25f * MathF.Sin(angle));
				spriteBatch.Draw(glowTex, haloPos + moteOffset, null, moteColor, 0f, glowOrigin, 0.35f, SpriteEffects.None, 0f);
			}
		}

		// Body silhouette aura (monochromatic silver / slate)
		private void DrawKingBodyAura(SpriteBatch spriteBatch, Vector2 screenPos)
		{
			if (kingTexture == null || !kingTexture.IsLoaded || glowTexture == null || !glowTexture.IsLoaded)
				return;

			Texture2D kingTex = kingTexture.Value;
			Vector2 kingOrigin = new Vector2(kingTex.Width * 0.5f, kingTex.Height * 0.5f);
			Vector2 drawCenter = NPC.Center - screenPos;

			Texture2D glowTex = glowTexture.Value;
			Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);

			// Radial core glow (silver-white)
			float pulse = MathF.Sin(orbitTimer * 2f);
			float glowScale = 2.6f + pulse * 0.25f;
			Color coreGlowColor = new Color(230, 230, 240, 0) * (0.35f + pulse * 0.1f);
			spriteBatch.Draw(glowTex, drawCenter, null, coreGlowColor, orbitTimer * 0.2f, glowOrigin, glowScale, SpriteEffects.None, 0f);

			// Pulsating silhouette outline
			float auraDist = 4f + MathF.Sin(orbitTimer * 3.2f) * 2.5f;
			float auraAlpha = 0.28f + MathF.Sin(orbitTimer * 3.2f) * 0.12f;
			Color auraColor = new Color(240, 240, 255, 0) * auraAlpha;

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

		// 3D pawn halo (monochromatic silver / chess black & white)
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
					float opacity = progress * 0.65f;

					float histScale = histSin < 0f
						? MathHelper.Lerp(1.0f, 0.85f, -histSin)
						: MathHelper.Lerp(1.0f, 1.1f, histSin);

					float trailScale = histScale * MathHelper.Lerp(0.35f, 0.85f, progress);
					Color trailColor = new Color(225, 230, 245, 0) * opacity;

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
					pawnColor = Color.Lerp(drawColor, Color.DarkSlateGray, 0.45f * depthFactor);
				}
				else
				{
					float depthFactor = sinVal;
					scale = MathHelper.Lerp(1.0f, 1.1f, depthFactor);
					pawnColor = drawColor;
				}

				float swayRotation = sinVal * 0.12f;

				Color haloGlow = new Color(230, 235, 250, 0) * (sinVal < 0f ? 0.35f : 0.6f);
				spriteBatch.Draw(glowTex, drawPos, null, haloGlow, 0f, glowOrigin, scale * 0.75f, SpriteEffects.None, 0f);

				spriteBatch.Draw(pawnTex, drawPos, null, pawnColor, swayRotation, pawnOrigin, scale, SpriteEffects.None, 0f);
			}
		}
	}
}
