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

		private const float IntroDuration = 600f; // 10 seconds invulnerable intro & phase transition

		private enum BossState
		{
			Intro,              // Initial 10s ground stare (invulnerable, white/silver halo, standing sprite)
			FloatCombat,        // Floats and maneuvers towards player with pawn aura (float combat sprite)
			DashTelegraph,      // Floats into combat stance, telegraphs dash (float combat sprite)
			DashMid,            // High-speed forward thrust dash (dash mid sprite)
			DashEnd,            // Crouched recovery slide after dash (dash end sprite)
			Phase2Transition    // 10s ground stare at 20% HP (invulnerable, RED halo, injured standing sprite)
		}

		// Phase 1 Sprites (Start / Healthy)
		private static Asset<Texture2D> texStandingStart;
		private static Asset<Texture2D> texFloatStart;
		private static Asset<Texture2D> texDashMidStart;
		private static Asset<Texture2D> texDashEndStart;

		// Phase 2 Sprites (Injured <= 20% HP)
		private static Asset<Texture2D> texStandingInjured;
		private static Asset<Texture2D> texFloatInjured;
		private static Asset<Texture2D> texDashMidInjured;
		private static Asset<Texture2D> texDashEndInjured;

		private static Asset<Texture2D> pawnTexture;
		private static Asset<Texture2D> glowTexture;

		private float orbitTimer;
		private float introTimer;
		private float phase2Timer;
		private float stateTimer;
		private bool isPhase2;
		private bool hasTriggeredPhase2;
		private BossState currentState = BossState.Intro;

		public bool IsInvulnerable => currentState == BossState.Intro || currentState == BossState.Phase2Transition;

		public override void Load()
		{
			texStandingStart = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/king_standing_start");
			texFloatStart = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/king_fightFloat_start");
			texDashMidStart = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/king_dashattack_mid_startOfTheFight");
			texDashEndStart = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/king_dashattack_end");

			texStandingInjured = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/king_standing_injuredInTheFight");
			texFloatInjured = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/king_fightFloat_injuredInTheFight");
			texDashMidInjured = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/king_dashattack_mid_injuredInTheFight");
			texDashEndInjured = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/king_dashattack_end_injuredInTheFight");

			pawnTexture = ModContent.Request<Texture2D>("QueensWrath/Content/Projectiles/PawnOrbit");
			glowTexture = TextureAssets.Extra[ExtrasID.SharpTears];
		}

		public override void Unload()
		{
			texStandingStart = null;
			texFloatStart = null;
			texDashMidStart = null;
			texDashEndStart = null;

			texStandingInjured = null;
			texFloatInjured = null;
			texDashMidInjured = null;
			texDashEndInjured = null;

			pawnTexture = null;
			glowTexture = null;
		}

		public override void SetStaticDefaults()
		{
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.TrailCacheLength[Type] = 8;
			NPCID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			NPC.width = 96;
			NPC.height = 200;
			NPC.damage = 90;
			NPC.defense = 34;
			NPC.lifeMax = 80000; // Increased HP for full boss fight experience
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 500000f;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = -1;
			NPC.noGravity = false;
			NPC.noTileCollide = false;
			NPC.boss = true;
			Music = MusicID.Boss1;
		}

		public override bool? CanBeHitByItem(Player player, Item item) => IsInvulnerable ? false : null;
		public override bool? CanBeHitByProjectile(Projectile projectile) => IsInvulnerable ? false : null;
		public override bool CanBeHitByNPC(NPC attacker) => !IsInvulnerable;

		public override bool CheckDead()
		{
			if (!hasTriggeredPhase2)
			{
				NPC.life = (int)(NPC.lifeMax * 0.2f);
				TriggerPhase2();
				return false;
			}
			return true;
		}

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
		{
			if (currentState == BossState.DashMid)
			{
				modifiers.SourceDamage *= 1.35f;
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PawnPwner>(), 1));
		}

		// Spawn Queen boss upon defeat
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
				for (int i = 0; i < 50; i++)
				{
					Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, Color.White, 1.8f);
					dust.velocity = Main.rand.NextVector2Circular(7f, 7f);
					dust.noGravity = true;
				}
			}
		}

		private void SnapToGround()
		{
			int tileX = Math.Clamp((int)(NPC.Center.X / 16f), 10, Main.maxTilesX - 10);
			int tileY = Math.Clamp((int)(NPC.Bottom.Y / 16f), 10, Main.maxTilesY - 10);
			while (tileY < Main.maxTilesY - 10 && (!Main.tile[tileX, tileY].HasTile || !Main.tileSolid[Main.tile[tileX, tileY].TileType] || Main.tile[tileX, tileY].IsActuated))
			{
				tileY++;
			}
			NPC.Bottom = new Vector2(NPC.Center.X, tileY * 16f);
		}

		private void TriggerPhase2()
		{
			if (hasTriggeredPhase2)
				return;

			hasTriggeredPhase2 = true;
			currentState = BossState.Phase2Transition;
			phase2Timer = 0f;
			stateTimer = 0f;
			NPC.velocity = Vector2.Zero;
			NPC.rotation = 0f;
			NPC.dontTakeDamage = true;
			NPC.noGravity = false;
			NPC.noTileCollide = false;
			NPC.netUpdate = true;

			SnapToGround();

			if (!Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.Roar with { Volume = 1.0f, Pitch = -0.3f }, NPC.Center);
				for (int i = 0; i < 45; i++)
				{
					Vector2 vel = Main.rand.NextVector2Circular(7f, 7f);
					Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Blood, vel, 0, default, Main.rand.NextFloat(1.5f, 2.5f));
					d.noGravity = Main.rand.NextBool();
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
			bool shouldBleed = (isPhase2 || currentState == BossState.Phase2Transition) || Main.rand.NextBool(2);
			if (!Main.dedServ && shouldBleed)
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

				// In Phase 2, extra blood drips from robes and sword
				if (isPhase2 || currentState == BossState.Phase2Transition)
				{
					Vector2 randomBodyPos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
					Dust d = Dust.NewDustPerfect(randomBodyPos, DustID.Blood, new Vector2(0f, Main.rand.NextFloat(0.5f, 2.0f)), 0, default, Main.rand.NextFloat(0.8f, 1.2f));
					d.noGravity = false;
				}
			}

			// Initial ground snap on spawn
			if (introTimer == 0f && currentState == BossState.Intro)
			{
				SnapToGround();
			}

			// Check for 20% HP phase transition
			if (!hasTriggeredPhase2 && currentState != BossState.Intro && NPC.life <= (int)(NPC.lifeMax * 0.2f))
			{
				TriggerPhase2();
				return;
			}

			// ==========================================
			// STATE 0: Intro Phase (10s ground stare, white halo)
			// ==========================================
			if (currentState == BossState.Intro)
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
					currentState = BossState.FloatCombat;
					stateTimer = 0f;
					NPC.noGravity = true;
					NPC.netUpdate = true;

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

				return; // Skip combat during intro
			}

			// ==========================================
			// STATE 5: Phase 2 Transition (10s ground stare at 20% HP, RED halo)
			// ==========================================
			if (currentState == BossState.Phase2Transition)
			{
				phase2Timer++;
				NPC.dontTakeDamage = true;
				NPC.velocity = Vector2.Zero;
				NPC.rotation = 0f;

				// Lock HP at 20%
				NPC.life = (int)(NPC.lifeMax * 0.2f);

				bool isPlayerRight = target.Center.X >= NPC.Center.X;
				NPC.spriteDirection = isPlayerRight ? 1 : -1;
				NPC.direction = NPC.spriteDirection;

				// Ambient crimson sparks and blood around the RED halo
				if (!Main.dedServ && Main.rand.NextBool(2))
				{
					Vector2 haloWorld = NPC.Center + new Vector2(0f, -112f);
					Dust d = Dust.NewDustPerfect(haloWorld + new Vector2(Main.rand.NextFloat(-24f, 24f), Main.rand.NextFloat(-6f, 6f)), DustID.RedTorch, new Vector2(0f, -0.8f), 0, Color.Red, 1.3f);
					d.noGravity = true;

					if (Main.rand.NextBool(3))
					{
						Dust b = Dust.NewDustPerfect(haloWorld + new Vector2(Main.rand.NextFloat(-20f, 20f), 0f), DustID.Blood, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(1f, 3f)), 0, default, 1.2f);
						b.noGravity = false;
					}
				}

				// Phase 2 transition finish: red halo shatters, resumes combat in injured form
				if (phase2Timer >= IntroDuration)
				{
					NPC.dontTakeDamage = false;
					isPhase2 = true;
					currentState = BossState.FloatCombat;
					stateTimer = 0f;
					NPC.noGravity = true;
					NPC.netUpdate = true;

					if (!Main.dedServ)
					{
						SoundEngine.PlaySound(SoundID.Item29 with { Volume = 1.0f, Pitch = -0.3f }, NPC.Center);
						SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.9f }, NPC.Center);
						for (int i = 0; i < 45; i++)
						{
							Vector2 vel = Main.rand.NextVector2Circular(8f, 5f);
							Dust d = Dust.NewDustPerfect(NPC.Center + new Vector2(0f, -112f), DustID.RedTorch, vel, 0, Color.Red, 1.8f);
							d.noGravity = true;
							Dust b = Dust.NewDustPerfect(NPC.Center + new Vector2(0f, -112f), DustID.Blood, vel * 0.8f, 0, default, 1.5f);
							b.noGravity = false;
						}
					}
				}

				return; // Skip combat during phase 2 transition
			}

			// ==========================================
			// ACTIVE COMBAT LOGIC
			// ==========================================
			orbitTimer += 0.035f;

			// Monochromatic / crimson wisps around boss
			if (!Main.dedServ && Main.rand.NextBool(3))
			{
				Vector2 dustPos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
				int smokeDust = isPhase2 ? DustID.Blood : DustID.Smoke;
				Dust dust = Dust.NewDustDirect(dustPos, 4, 4, smokeDust, 0, -Main.rand.NextFloat(1.2f, 2.4f), 120, Color.White, Main.rand.NextFloat(0.9f, 1.3f));
				dust.noGravity = true;
				dust.velocity.X *= 0.25f;
			}

			// Dynamic orbit breathing for pawns
			float breath = MathF.Sin(orbitTimer * 1.5f);
			float dynamicRadiusX = BaseRadiusX + breath * 10f;
			float dynamicRadiusY = BaseRadiusY + breath * 3.5f;
			float dynamicTilt = BaseTiltAngle + MathF.Sin(orbitTimer * 0.8f) * 0.035f;

			float cosTilt = MathF.Cos(dynamicTilt);
			float sinTilt = MathF.Sin(dynamicTilt);

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

			// ==========================================
			// STATE MACHINE: ATTACK PATTERNS
			// ==========================================
			stateTimer++;

			switch (currentState)
			{
				case BossState.FloatCombat:
					NPC.noGravity = true;
					NPC.noTileCollide = false;

					// Always face player directly
					bool isTargetRight = target.Center.X >= NPC.Center.X;
					NPC.spriteDirection = isTargetRight ? 1 : -1;
					NPC.direction = NPC.spriteDirection;

					// Aerial hover navigation: glide smoothly near player with sinusoidal hover
					float bobbing = MathF.Sin(stateTimer * 0.05f) * 16f;
					float desiredSideOffset = isTargetRight ? -150f : 150f;
					Vector2 hoverTarget = target.Center + new Vector2(desiredSideOffset, -60f + bobbing);
					Vector2 toHover = hoverTarget - NPC.Center;
					float dist = toHover.Length();
					float maxSpeed = isPhase2 ? 7.5f : 5.5f;

					if (dist > 30f)
					{
						Vector2 desiredVel = Vector2.Normalize(toHover) * MathF.Min(dist * 0.08f, maxSpeed);
						NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVel, 0.1f);
					}
					else
					{
						NPC.velocity *= 0.92f;
					}

					NPC.rotation = NPC.velocity.X * 0.015f;

					// After floating combat maneuver, prepare dash attack
					int floatThreshold = isPhase2 ? 130 : 180;
					if (stateTimer >= floatThreshold)
					{
						currentState = BossState.DashTelegraph;
						stateTimer = 0f;
						NPC.netUpdate = true;
						if (!Main.dedServ)
						{
							SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
						}
					}
					break;

				case BossState.DashTelegraph:
					// Hover into combat ready float
					NPC.noGravity = true;
					NPC.noTileCollide = false;
					NPC.velocity.X *= 0.88f;
					NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, -2.2f, 0.08f);

					// Face player
					NPC.spriteDirection = target.Center.X >= NPC.Center.X ? 1 : -1;
					NPC.direction = NPC.spriteDirection;
					NPC.rotation = 0f;

					// Charging particles gathering at sword
					if (!Main.dedServ && Main.rand.NextBool(2))
					{
						Vector2 swordPos = NPC.Center + new Vector2(NPC.spriteDirection * 24f, -20f);
						int dustType = isPhase2 ? DustID.Blood : DustID.SilverCoin;
						Dust d = Dust.NewDustPerfect(swordPos + Main.rand.NextVector2Circular(16f, 16f), dustType, new Vector2(0f, -1.2f), 0, default, 1.2f);
						d.noGravity = true;
					}

					int telegraphMax = isPhase2 ? 34 : 45;
					if (stateTimer >= telegraphMax)
					{
						// Launch Dash!
						Vector2 toPlayer = target.Center - NPC.Center;
						float dirX = toPlayer.X >= 0 ? 1f : -1f;
						float dashSpeedX = dirX * (isPhase2 ? 22f : 17f);
						float dashSpeedY = Math.Clamp(toPlayer.Y * 0.035f, -6f, 6f);

						NPC.velocity = new Vector2(dashSpeedX, dashSpeedY);
						NPC.spriteDirection = dirX >= 0 ? 1 : -1;
						NPC.direction = NPC.spriteDirection;
						NPC.noTileCollide = true;

						currentState = BossState.DashMid;
						stateTimer = 0f;
						NPC.netUpdate = true;

						if (!Main.dedServ)
						{
							SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 1.0f, Pitch = -0.1f }, NPC.Center);
							for (int i = 0; i < 20; i++)
							{
								int dustType = isPhase2 ? DustID.Blood : DustID.Smoke;
								Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType, -dirX * 3f, 0f, 100, Color.White, 1.5f);
								d.noGravity = true;
							}
						}
					}
					break;

				case BossState.DashMid:
					NPC.noGravity = true;
					NPC.noTileCollide = true;
					NPC.rotation = NPC.velocity.Y * 0.015f;

					// Spawn dash motion trail
					if (!Main.dedServ)
					{
						for (int d = 0; d < 2; d++)
						{
							int dustType = isPhase2 ? DustID.Blood : DustID.Smoke;
							Dust dust = Dust.NewDustPerfect(
								NPC.Center + Main.rand.NextVector2Circular(18f, 24f) - NPC.velocity * 0.35f,
								dustType,
								-NPC.velocity * 0.12f,
								100,
								isPhase2 ? default : Color.White,
								Main.rand.NextFloat(1.2f, 1.8f)
							);
							dust.noGravity = true;
						}
					}

					int dashDuration = isPhase2 ? 26 : 24;
					if (stateTimer >= dashDuration)
					{
						currentState = BossState.DashEnd;
						stateTimer = 0f;
						NPC.noTileCollide = false;
						NPC.noGravity = true;
						NPC.netUpdate = true;

						if (!Main.dedServ)
						{
							SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = 0.1f }, NPC.Center);
						}
					}
					break;

				case BossState.DashEnd:
					// Crouched recovery slide with deceleration
					NPC.noGravity = true;
					NPC.noTileCollide = false;
					NPC.velocity *= 0.84f;
					NPC.rotation = MathHelper.Lerp(NPC.rotation, 0f, 0.15f);

					// Motion recovery wisps
					if (MathF.Abs(NPC.velocity.X) > 1.2f && !Main.dedServ && Main.rand.NextBool(2))
					{
						int dustType = isPhase2 ? DustID.Blood : DustID.Smoke;
						Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType, -NPC.velocity.X * 0.3f, -0.6f, 100, Color.White, 1.2f);
						dust.noGravity = true;
					}

					int recoveryDuration = isPhase2 ? 26 : 36;
					if (stateTimer >= recoveryDuration)
					{
						currentState = BossState.FloatCombat;
						stateTimer = 0f;
						NPC.netUpdate = true;
					}
					break;
			}
		}

		private Texture2D GetActiveTexture()
		{
			if (isPhase2 || currentState == BossState.Phase2Transition)
			{
				return currentState switch
				{
					BossState.Intro or BossState.Phase2Transition => texStandingInjured?.Value,
					BossState.DashMid => texDashMidInjured?.Value,
					BossState.DashEnd => texDashEndInjured?.Value,
					_ => texFloatInjured?.Value
				} ?? texStandingInjured?.Value;
			}
			else
			{
				return currentState switch
				{
					BossState.Intro => texStandingStart?.Value,
					BossState.DashMid => texDashMidStart?.Value,
					BossState.DashEnd => texDashEndStart?.Value,
					_ => texFloatStart?.Value
				} ?? texStandingStart?.Value;
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			Texture2D tex = GetActiveTexture();
			if (tex == null) return false;

			bool isFacingRight = NPC.spriteDirection == 1;
			SpriteEffects effects = isFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);

			// All textures are centered at NPC.Center for smooth transitions without vertical popping
			Vector2 drawCenter = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);

			// 1. Back pawn halo & body aura (only after invulnerable phases)
			if (!IsInvulnerable)
			{
				DrawKingBodyAura(spriteBatch, screenPos, tex, origin, drawCenter, effects);
				DrawPawnHalo(spriteBatch, screenPos, drawColor, isFront: false);
			}

			// 2. Dash motion after-image blur (during DashMid)
			if (currentState == BossState.DashMid)
			{
				DrawDashAfterimages(spriteBatch, screenPos, tex, origin, effects);
			}

			// 3. Main king sprite solid drawing
			spriteBatch.Draw(tex, drawCenter, null, drawColor, NPC.rotation, origin, NPC.scale, effects, 0f);

			// 4. Front pawn halo (only after invulnerable phases)
			if (!IsInvulnerable)
			{
				DrawPawnHalo(spriteBatch, screenPos, drawColor, isFront: true);
			}

			return false;
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (currentState == BossState.Intro)
			{
				DrawInvulnerableHalo(spriteBatch, screenPos, isRed: false);
			}
			else if (currentState == BossState.Phase2Transition)
			{
				DrawInvulnerableHalo(spriteBatch, screenPos, isRed: true);
			}
		}

		// Floating invulnerable halo above head (white/silver during intro, RED during Phase 2 transition)
		private void DrawInvulnerableHalo(SpriteBatch spriteBatch, Vector2 screenPos, bool isRed)
		{
			if (glowTexture == null || !glowTexture.IsLoaded)
				return;

			Texture2D glowTex = glowTexture.Value;
			Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);

			float activeTimer = isRed ? phase2Timer : introTimer;
			float pulse = 0.5f + 0.5f * MathF.Sin(activeTimer * 0.12f);
			float floatBob = MathF.Sin(activeTimer * 0.08f) * 3.5f;
			Vector2 haloPos = NPC.Center - screenPos + new Vector2(0f, -112f + floatBob);

			Color outerColor = isRed
				? new Color(255, 30, 40, 0) * (0.65f + pulse * 0.25f)
				: new Color(220, 225, 240, 0) * (0.55f + pulse * 0.25f);

			Color coreColor = isRed
				? new Color(255, 120, 120, 0) * (0.85f + pulse * 0.2f)
				: new Color(255, 255, 255, 0) * (0.8f + pulse * 0.2f);

			Color moteColor = isRed
				? new Color(255, 60, 70, 0) * (0.85f + 0.25f * MathF.Sin(activeTimer * 0.15f))
				: new Color(255, 255, 255, 0) * (0.75f + 0.25f * MathF.Sin(activeTimer * 0.15f));

			// Outer ethereal halo ellipse
			Vector2 outerScale = new Vector2(1.85f + pulse * 0.2f, 0.52f + pulse * 0.08f);
			spriteBatch.Draw(glowTex, haloPos, null, outerColor, 0f, glowOrigin, outerScale, SpriteEffects.None, 0f);

			// Core bright halo light
			Vector2 coreScale = new Vector2(1.25f, 0.34f);
			spriteBatch.Draw(glowTex, haloPos, null, coreColor, 0f, glowOrigin, coreScale, SpriteEffects.None, 0f);

			// 4 orbiting chess halo motes around the ring
			for (int i = 0; i < 4; i++)
			{
				float angle = activeTimer * 0.045f + i * MathHelper.PiOver2;
				Vector2 moteOffset = new Vector2(MathF.Cos(angle) * 32f, MathF.Sin(angle) * 9f);
				spriteBatch.Draw(glowTex, haloPos + moteOffset, null, moteColor, 0f, glowOrigin, 0.38f, SpriteEffects.None, 0f);
			}
		}

		// Body silhouette aura (monochromatic silver in Phase 1, crimson-tinted silver in Phase 2)
		private void DrawKingBodyAura(SpriteBatch spriteBatch, Vector2 screenPos, Texture2D kingTex, Vector2 kingOrigin, Vector2 drawCenter, SpriteEffects effects)
		{
			if (glowTexture == null || !glowTexture.IsLoaded || kingTex == null)
				return;

			Texture2D glowTex = glowTexture.Value;
			Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);

			// Radial core glow
			float pulse = MathF.Sin(orbitTimer * 2f);
			float glowScale = 2.6f + pulse * 0.25f;
			Color coreGlowColor = isPhase2
				? new Color(245, 120, 130, 0) * (0.35f + pulse * 0.1f)
				: new Color(230, 230, 240, 0) * (0.35f + pulse * 0.1f);

			spriteBatch.Draw(glowTex, drawCenter, null, coreGlowColor, orbitTimer * 0.2f, glowOrigin, glowScale, SpriteEffects.None, 0f);

			// Pulsating silhouette outline
			float auraDist = 4f + MathF.Sin(orbitTimer * 3.2f) * 2.5f;
			float auraAlpha = 0.28f + MathF.Sin(orbitTimer * 3.2f) * 0.12f;
			Color auraColor = isPhase2
				? new Color(255, 160, 160, 0) * auraAlpha
				: new Color(240, 240, 255, 0) * auraAlpha;

			const float auraAngleStep = MathHelper.TwoPi / 6f;
			float baseAuraAngle = orbitTimer * 0.5f;

			for (int a = 0; a < 6; a++)
			{
				float angle = a * auraAngleStep + baseAuraAngle;
				Vector2 auraOffset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * auraDist;
				spriteBatch.Draw(kingTex, drawCenter + auraOffset, null, auraColor, NPC.rotation, kingOrigin, NPC.scale, effects, 0f);
			}
		}

		// Dash motion after-image trail
		private void DrawDashAfterimages(SpriteBatch spriteBatch, Vector2 screenPos, Texture2D tex, Vector2 origin, SpriteEffects effects)
		{
			for (int i = 1; i < 6; i++)
			{
				Vector2 oldCenter = NPC.oldPos[i] + new Vector2(NPC.width * 0.5f, NPC.height * 0.5f) - screenPos;
				float alpha = (1f - (i / 6f)) * 0.35f;
				Color trailColor = isPhase2
					? new Color(220, 40, 50, 0) * alpha
					: new Color(230, 235, 255, 0) * alpha;

				spriteBatch.Draw(tex, oldCenter, null, trailColor, NPC.rotation, origin, NPC.scale, effects, 0f);
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
					Color trailColor = isPhase2
						? new Color(245, 120, 130, 0) * opacity
						: new Color(225, 230, 245, 0) * opacity;

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

				Color haloGlow = isPhase2
					? new Color(245, 120, 130, 0) * (sinVal < 0f ? 0.35f : 0.6f)
					: new Color(230, 235, 250, 0) * (sinVal < 0f ? 0.35f : 0.6f);

				spriteBatch.Draw(glowTex, drawPos, null, haloGlow, 0f, glowOrigin, scale * 0.75f, SpriteEffects.None, 0f);
				spriteBatch.Draw(pawnTex, drawPos, null, pawnColor, swayRotation, pawnOrigin, scale, SpriteEffects.None, 0f);
			}
		}
	}
}
