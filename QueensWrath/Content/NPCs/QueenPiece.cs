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
    public enum ChessPieceType { Bishop, Knight, Queen, Rook }

    public enum QueenAttackState { Idle, SpinningWheel, PieceAscending, PieceAttacking, PieceReturning }

    public class QueenPiece : ModNPC
    {
        public override string Texture => "QueensWrath/Content/NPCs/QueenPiece";

        // Halo settings
        private const int HaloPieceCount = 8;
        private const float RadiusX = 150f;
        private const float RadiusY = 50f;
        private const float TiltAngle = -0.12f;
        private const float HaloPieceHeight = 44f;
        private const float ElevatedPieceHeight = 115f;

        // Attack timings
        public const int AttackCooldownTicks = 240;
        public const int SpinDurationTicks = 90;
        public const int AscendDurationTicks = 50;
        public const int DefaultAttackDurationTicks = 180;
        public const int QueenAttackDurationTicks = 520;
        public const int ReturnDurationTicks = 35;

        private static readonly ChessPieceType[] HaloPieceSlots =
        {
            ChessPieceType.Bishop, ChessPieceType.Knight, ChessPieceType.Queen, ChessPieceType.Rook,
            ChessPieceType.Bishop, ChessPieceType.Knight, ChessPieceType.Queen, ChessPieceType.Rook
        };

        // Knight attack state
        private static readonly Vector2[] KnightOffsets =
        {
            new Vector2( 2f,  1f), new Vector2( 2f, -1f),
            new Vector2(-2f,  1f), new Vector2(-2f, -1f),
            new Vector2( 1f,  2f), new Vector2( 1f, -2f),
            new Vector2(-1f,  2f), new Vector2(-1f, -2f)
        };

        private int knightStep;
        private int knightStepTimer;
        private Vector2 knightAnchorPos;
        private Vector2 knightCornerPos;
        private Vector2 knightEndPos;

       // Queen asterisk attack state
        private const int AsteriskTrailLength = 16;

        private struct AsteriskPiece
        {
            public bool IsBishop;
            public Vector2 Direction;
            public float Length;
            public Vector2 CurrentPos;
            public Vector2 EndPos;
            public Vector2[] Trail;
            public int TrailCount;
        }

        private AsteriskPiece[] queenAsteriskPieces;
        private Vector2 queenAttackCenter;
        private int queenBeamHitCooldown;


        // Textures
        private static Asset<Texture2D> texBishop;
        private static Asset<Texture2D> texKnight;
        private static Asset<Texture2D> texRook;
        private static Asset<Texture2D> texQueen;
        private static Asset<Texture2D> glowTexture;

        // Timers
        private float animTimer;
        private float haloOrbitTimer;
        private float spinAngleOffset;
        private float lastTickAngle;
        private int attackCooldownTimer = AttackCooldownTicks;

        // Network sync
        public QueenAttackState CurrentState
        {
            get => (QueenAttackState)(int)NPC.ai[0];
            set => NPC.ai[0] = (float)value;
        }

        public ref float StateTimer => ref NPC.ai[1];

        public ChessPieceType SelectedPiece
        {
            get => (ChessPieceType)(int)NPC.ai[2];
            set => NPC.ai[2] = (float)value;
        }

        public int SelectedHaloIndex
        {
            get => (int)NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        public int AttackTimer { get; private set; }

        // Asset loading
        public override void Load()
        {
            texBishop = ModContent.Request<Texture2D>("QueensWrath/Content/Projectiles/BishopPiece");
            texKnight = ModContent.Request<Texture2D>("QueensWrath/Content/Projectiles/Minions/White_knight");
            texRook = ModContent.Request<Texture2D>("QueensWrath/Content/Projectiles/RookProjectile");
            texQueen = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/QueenPiece");
            glowTexture = TextureAssets.Extra[ExtrasID.SharpTears];
        }

        // Asset unloading
        public override void Unload()
        {
            texBishop = null;
            texKnight = null;
            texRook = null;
            texQueen = null;
            glowTexture = null;
        }

        // Static defaults
        public override void SetStaticDefaults()
        {
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
        }

        // NPC defaults
        public override void SetDefaults()
        {
            NPC.width = 110;
            NPC.height = 208;
            NPC.damage = 200;
            NPC.defense = 50;
            NPC.lifeMax = 250000;
            NPC.value = 1000000f;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            Music = MusicID.QueenSlime;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
        }

        // Loot table
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BishopStaff>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SiegeBastion>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RookRectifier>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NightOfKnightsItem>(), 1));
        }

        // AI update
        public override void AI()
        {
            animTimer += 0.035f;

            // Target tracking
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest(true);

            Player target = Main.player[NPC.target];
            if (target.dead || !target.active)
            {
                NPC.velocity.Y -= 0.35f;
                NPC.EncourageDespawn(10);
                return;
            }

            // Movement logic
            UpdateMovement(target);

            // Halo physics
            UpdateHaloOrbit();

            // Attack machine
            UpdateAttackStateMachine(target);

            // Ambient dust
            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                Vector2 dustPos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                Dust d = Dust.NewDustPerfect(dustPos, DustID.Smoke, new Vector2(0f, -0.6f), 120, Color.White, 0.85f);
                d.noGravity = true;
            }
        }

        // Movement update
        private void UpdateMovement(Player target)
        {
            bool isKnightDashing = CurrentState == QueenAttackState.PieceAttacking &&
                                   SelectedPiece == ChessPieceType.Knight;

            if (isKnightDashing)
            {
                NPC.spriteDirection = NPC.velocity.X != 0f
                    ? Math.Sign(NPC.velocity.X)
                    : (target.Center.X >= NPC.Center.X ? 1 : -1);
                NPC.rotation = NPC.velocity.X * 0.015f;
                return;
            }

            float hoverOffsetY = CurrentState switch
            {
                QueenAttackState.SpinningWheel => -200f,
                QueenAttackState.PieceAscending => -220f,
                _ => -240f
            };

            Vector2 targetPos = target.Center + new Vector2(0f, hoverOffsetY);
            Vector2 toTarget = targetPos - NPC.Center;
            float distance = toTarget.Length();

            float maxSpeed = (CurrentState == QueenAttackState.SpinningWheel) ? 4.5f : 8.5f;
            float inertia = (CurrentState == QueenAttackState.SpinningWheel) ? 30f : 24f;

            if (distance > 20f)
                NPC.velocity = (NPC.velocity * (inertia - 1f) + toTarget * (maxSpeed / distance)) / inertia;

            NPC.rotation = NPC.velocity.X * 0.015f;
            NPC.spriteDirection = target.Center.X >= NPC.Center.X ? 1 : -1;
        }

        // Halo rotation
        private void UpdateHaloOrbit()
        {
            if (CurrentState == QueenAttackState.SpinningWheel)
            {
                float spinProgress = Math.Clamp(StateTimer / SpinDurationTicks, 0f, 1f);
                spinAngleOffset += MathHelper.Lerp(0.38f, 0.035f, MathF.Pow(spinProgress, 2.2f));

                float tickInterval = MathHelper.TwoPi / HaloPieceCount;
                if (MathF.Abs(spinAngleOffset - lastTickAngle) >= tickInterval)
                {
                    lastTickAngle = spinAngleOffset;
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f, Pitch = MathHelper.Lerp(0.2f, -0.2f, spinProgress) }, NPC.Center);
                }
            }
            else
            {
                haloOrbitTimer += 0.025f;
            }
        }

        private int GetCurrentAttackDuration() => SelectedPiece == ChessPieceType.Queen
            ? QueenAttackDurationTicks
            : DefaultAttackDurationTicks;

        // State machine
        private void UpdateAttackStateMachine(Player target)
        {
            StateTimer++;

            switch (CurrentState)
            {
                case QueenAttackState.Idle:
                    if (--attackCooldownTimer <= 0)
                        TriggerPieceRouletteAttack();
                    break;

                case QueenAttackState.SpinningWheel:
                    if (!Main.dedServ && Main.rand.NextBool(2))
                    {
                        Vector2 p = GetHaloPiecePositionRaw(Main.rand.NextFloat(MathHelper.TwoPi));
                        Dust.NewDustPerfect(p, DustID.SilverCoin, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, Color.White, 1.1f).noGravity = true;
                    }

                    if (StateTimer >= SpinDurationTicks)
                    {
                        CurrentState = QueenAttackState.PieceAscending;
                        StateTimer = 0f;
                        NPC.netUpdate = true;
                        if (!Main.dedServ)
                            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.9f, Pitch = 0.15f }, NPC.Center);
                    }
                    break;

                case QueenAttackState.PieceAscending:
                    if (!Main.dedServ)
                    {
                        Vector2 overheadPos = GetActivePieceWorldPosition();
                        Dust.NewDustPerfect(overheadPos + Main.rand.NextVector2Circular(18f, 18f), DustID.Enchanted_Pink, new Vector2(0f, -1.8f), 120, Color.White, 1.25f).noGravity = true;
                    }

                    if (StateTimer >= AscendDurationTicks)
                    {
                        CurrentState = QueenAttackState.PieceAttacking;
                        StateTimer = 0f;
                        AttackTimer = 0;
                        NPC.netUpdate = true;
                        if (!Main.dedServ)
                            SoundEngine.PlaySound(SoundID.Item105 with { Volume = 0.85f, Pitch = 0.2f }, NPC.Center);
                    }
                    break;

                case QueenAttackState.PieceAttacking:
                    AttackTimer++;
                    UpdatePieceAttack(target);
                    if (AttackTimer >= GetCurrentAttackDuration())
                        EndPieceAttack();
                    break;

                case QueenAttackState.PieceReturning:
                    if (StateTimer >= ReturnDurationTicks)
                    {
                        CurrentState = QueenAttackState.Idle;
                        StateTimer = 0f;
                        attackCooldownTimer = AttackCooldownTicks;
                        NPC.netUpdate = true;
                    }
                    break;
            }
        }

        // Trigger roulette
        public void TriggerPieceRouletteAttack(ChessPieceType? forcedPiece = null)
        {
            if (CurrentState != QueenAttackState.Idle)
                return;

            CurrentState = QueenAttackState.SpinningWheel;
            StateTimer = 0f;
            spinAngleOffset = 0f;
            lastTickAngle = 0f;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (forcedPiece.HasValue)
                {
                    SelectedPiece = forcedPiece.Value;
                    SelectedHaloIndex = Math.Max(0, Array.IndexOf(HaloPieceSlots, SelectedPiece));
                }
                else
                {
                    SelectedHaloIndex = Main.rand.Next(HaloPieceCount);
                    SelectedPiece = HaloPieceSlots[SelectedHaloIndex];
                }
                NPC.netUpdate = true;
            }

            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.8f, Pitch = -0.1f }, NPC.Center);
        }

        // End attack
        public void EndPieceAttack()
        {
            knightStep = 0;
            knightStepTimer = 0;
            queenAsteriskPieces = null;
            queenBeamHitCooldown = 0;

            if (CurrentState == QueenAttackState.PieceAttacking)
            {
                CurrentState = QueenAttackState.PieceReturning;
                StateTimer = 0f;
                NPC.netUpdate = true;
            }
        }

        // Attack dispatcher
        private void UpdatePieceAttack(Player target)
        {
            switch (SelectedPiece)
            {
                case ChessPieceType.Bishop: ExecuteBishopAttack(target); break;
                case ChessPieceType.Knight: ExecuteKnightAttack(target); break;
                case ChessPieceType.Queen:  ExecuteQueenAttack(target);  break;
                case ChessPieceType.Rook:   ExecuteRookAttack(target);   break;
            }
        }

        // Bishop attack
        public virtual void ExecuteBishopAttack(Player target)
        {
            if (AttackTimer >= 150)
                EndPieceAttack();
        }

        // Knight attack
        public virtual void ExecuteKnightAttack(Player target)
        {
            const float stepSize = 260f;
            const int telegraphTicks = 20;
            const int leg1Ticks = 28;
            const int leg2Ticks = 20;
            const int recoveryTicks = 12;
            const float leg1Speed = 24f;
            const float leg2Speed = 24f;

            if (AttackTimer == 1)
            {
                knightStep = 0;
                knightStepTimer = 0;
            }

            switch (knightStep)
            {
                case 0:
                    if (knightStepTimer == 0)
                    {
                        knightAnchorPos = target.Center;
                        knightEndPos = knightAnchorPos;

                        Vector2 chosenOffset = KnightOffsets[Main.rand.Next(KnightOffsets.Length)];
                        Vector2 startPos = knightAnchorPos + chosenOffset * stepSize;

                        bool xIsLongLeg = MathF.Abs(chosenOffset.X) > MathF.Abs(chosenOffset.Y);
                        knightCornerPos = xIsLongLeg
                            ? new Vector2(knightAnchorPos.X, startPos.Y)
                            : new Vector2(startPos.X, knightAnchorPos.Y);

                        NPC.Center = startPos;
                        NPC.velocity = Vector2.Zero;
                        NPC.netUpdate = true;
                    }

                    knightStepTimer++;
                    if (knightStepTimer >= telegraphTicks)
                    {
                        knightStep = 1;
                        knightStepTimer = 0;
                    }
                    break;

                case 1:
                    {
                        Vector2 toCorner = knightCornerPos - NPC.Center;
                        if (toCorner.Length() <= leg1Speed)
                        {
                            NPC.Center = knightCornerPos;
                            NPC.velocity = Vector2.Zero;
                            knightStep = 2;
                            knightStepTimer = 0;
                            break;
                        }

                        NPC.velocity = NPC.Center.DirectionTo(knightCornerPos) * leg1Speed;

                        knightStepTimer++;
                        if (knightStepTimer >= leg1Ticks)
                        {
                            knightStep = 2;
                            knightStepTimer = 0;
                        }
                    }
                    break;

                case 2:
                    {
                        Vector2 toEnd = knightEndPos - NPC.Center;
                        if (toEnd.Length() <= leg2Speed)
                        {
                            NPC.Center = knightEndPos;
                            NPC.velocity = Vector2.Zero;
                            knightStep = 3;
                            knightStepTimer = 0;
                            break;
                        }

                        NPC.velocity = NPC.Center.DirectionTo(knightEndPos) * leg2Speed;

                        knightStepTimer++;
                        if (knightStepTimer >= leg2Ticks)
                        {
                            knightStep = 3;
                            knightStepTimer = 0;
                        }
                    }
                    break;

                case 3:
                    NPC.velocity *= 0.82f;
                    knightStepTimer++;

                    if (knightStepTimer >= recoveryTicks)
                    {
                        knightStep = 0;
                        knightStepTimer = 0;

                        if (AttackTimer >= 110)
                            EndPieceAttack();
                    }
                    break;
            }

            if (AttackTimer >= 150)
                EndPieceAttack();
        }

        // Queen attack
        public virtual void ExecuteQueenAttack(Player target)
        {
            const float laneLength = 1250f;
            const float cycleSpeed = 0.095f;
            const float trailHitboxWidth = 26f;

            if (queenBeamHitCooldown > 0)
                queenBeamHitCooldown--;

            if (AttackTimer == 1)
            {
                queenAttackCenter = target.Center;
                queenAsteriskPieces = new AsteriskPiece[8];

                for (int i = 0; i < 8; i++)
                {
                    float angle = i * MathHelper.PiOver4;
                    bool isDiagonal = (i % 2) == 1;
                    Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

                    Vector2[] trail = new Vector2[AsteriskTrailLength];
                    trail[0] = queenAttackCenter;

                    queenAsteriskPieces[i] = new AsteriskPiece
                    {
                        IsBishop = isDiagonal,
                        Direction = dir,
                        Length = laneLength,
                        CurrentPos = queenAttackCenter,
                        EndPos = queenAttackCenter + dir * laneLength,
                        Trail = trail,
                        TrailCount = 1
                    };
                }
            }

            if (queenAsteriskPieces != null)
            {
                float pingPong = 0.5f - 0.5f * MathF.Cos(AttackTimer * cycleSpeed * MathF.PI);

                for (int i = 0; i < queenAsteriskPieces.Length; i++)
                {
                    AsteriskPiece piece = queenAsteriskPieces[i];
                    piece.EndPos = queenAttackCenter + piece.Direction * piece.Length;
                    piece.CurrentPos = Vector2.Lerp(queenAttackCenter, piece.EndPos, pingPong);

                    if (piece.Trail == null)
                    {
                        piece.Trail = new Vector2[AsteriskTrailLength];
                        piece.TrailCount = 0;
                    }

                    int nextCount = Math.Min(piece.TrailCount + 1, AsteriskTrailLength);
                    for (int t = nextCount - 1; t > 0; t--)
                        piece.Trail[t] = piece.Trail[t - 1];

                    piece.Trail[0] = piece.CurrentPos;
                    piece.TrailCount = nextCount;

                    queenAsteriskPieces[i] = piece;

                    for (int t = 0; t < piece.TrailCount - 1; t++)
                    {
                        float progress = 1f - (float)t / (piece.TrailCount - 1);
                        float segmentStrength = MathHelper.Lerp(0.35f, 1f, progress);
                        float scaledWidth = MathHelper.Lerp(trailHitboxWidth * 0.45f, trailHitboxWidth, progress);

                        CheckQueenBeamCollision(target, piece.Trail[t], piece.Trail[t + 1], scaledWidth, segmentStrength);
                        if (queenBeamHitCooldown > 0)
                            break;
                    }
                }
            }

            if (AttackTimer >= QueenAttackDurationTicks)
                EndPieceAttack();
        }

        private void CheckQueenBeamCollision(Player target, Vector2 beamStart, Vector2 beamEnd, float beamWidth, float damageScale = 1f)
        {
            if (queenBeamHitCooldown > 0 || target.dead || !target.active)
                return;

            // Skip tiny/degenerate segments
            if (Vector2.DistanceSquared(beamStart, beamEnd) < 1f)
                return;

            float collisionPoint = 0f;
            bool hit = Collision.CheckAABBvLineCollision(
                target.Hitbox.TopLeft(),
                target.Hitbox.Size(),
                beamStart,
                beamEnd,
                beamWidth,
                ref collisionPoint
            );

            if (!hit)
                return;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int hitDirection = target.Center.X >= beamStart.X ? 1 : -1;
                int baseDamage = NPC.damage / 2;
                int finalDamage = Math.Max(1, (int)(baseDamage * damageScale));

                target.Hurt(
                    PlayerDeathReason.ByNPC(NPC.whoAmI),
                    finalDamage,
                    hitDirection,
                    pvp: false,
                    quiet: false
                );
            }

            queenBeamHitCooldown = 14;
        }

        // Rook attack
        public virtual void ExecuteRookAttack(Player target)
        {
            if (AttackTimer >= 150)
                EndPieceAttack();
        }

        // Halo piece position
        private Vector2 GetHaloPiecePosition(int slotIndex)
        {
            float angle = (haloOrbitTimer + spinAngleOffset) + slotIndex * (MathHelper.TwoPi / HaloPieceCount);
            return GetHaloPiecePositionRaw(angle);
        }

        // Ellipse calculation
        private Vector2 GetHaloPiecePositionRaw(float angle)
        {
            float breath = MathF.Sin(animTimer * 1.5f);
            float rx = RadiusX + breath * 8f;
            float ry = RadiusY + breath * 3f;

            float cos = MathF.Cos(TiltAngle);
            float sin = MathF.Sin(TiltAngle);

            float rawX = MathF.Cos(angle) * rx;
            float rawY = MathF.Sin(angle) * ry;

            return NPC.Center + new Vector2(0f, 10f) + new Vector2(rawX * cos - rawY * sin, rawX * sin + rawY * cos);
        }

        // Active piece position
        public Vector2 GetActivePieceWorldPosition()
        {
            Vector2 haloSlot = GetHaloPiecePosition(SelectedHaloIndex);
            Vector2 overhead = NPC.Center + new Vector2(0f, -150f);

            return CurrentState switch
            {
                QueenAttackState.PieceAscending => Vector2.Lerp(haloSlot, overhead, MathHelper.SmoothStep(0f, 1f, Math.Clamp(StateTimer / AscendDurationTicks, 0f, 1f))),
                QueenAttackState.PieceAttacking => overhead + new Vector2(0f, MathF.Sin(animTimer * 2f) * 6f),
                QueenAttackState.PieceReturning => Vector2.Lerp(overhead, haloSlot, MathHelper.SmoothStep(0f, 1f, Math.Clamp(StateTimer / ReturnDurationTicks, 0f, 1f))),
                _ => haloSlot
            };
        }

        // Texture getter
        private static Texture2D GetPieceTexture(ChessPieceType type) => type switch
        {
            ChessPieceType.Bishop => texBishop?.Value,
            ChessPieceType.Knight => texKnight?.Value,
            ChessPieceType.Queen  => texQueen?.Value,
            ChessPieceType.Rook   => texRook?.Value,
            _ => null
        };

        // PreDraw rendering
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[Type].Value;
            if (tex == null) return false;

            bool isFacingRight = NPC.spriteDirection == 1;
            SpriteEffects effects = isFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);

            // Floating motion
            float floatX = MathF.Sin(animTimer * 1.4f) * 6f;
            float floatY = MathF.Cos(animTimer * 2.8f) * 9f;
            Vector2 drawCenter = NPC.Center - screenPos + new Vector2(floatX, floatY);

            // Squash and stretch
            float breathe = MathF.Sin(animTimer * 2.2f);
            float baseScale = NPC.scale * (208f / tex.Height);
            Vector2 scaleVec = new Vector2(1f - breathe * 0.035f, 1f + breathe * 0.035f) * baseScale;

            // Tilt and sway
            float tilt = MathHelper.Clamp(NPC.velocity.X * 0.02f, -0.15f, 0.15f);
            float sway = MathF.Sin(animTimer * 1.8f) * 0.03f;
            float rot = NPC.rotation + tilt + (isFacingRight ? sway : -sway);

            // Back halo pieces
            DrawChessHalo(spriteBatch, screenPos, drawColor, isFront: false);

            // Shadow trail
            if (NPC.velocity.LengthSquared() > 0.8f)
            {
                for (int i = NPC.oldPos.Length - 1; i > 0; i--)
                {
                    if (NPC.oldPos[i] == Vector2.Zero) continue;
                    float factor = (float)(NPC.oldPos.Length - i) / NPC.oldPos.Length;
                    Vector2 oldPos = NPC.oldPos[i] + new Vector2(NPC.width * 0.5f, NPC.height * 0.5f) - screenPos + new Vector2(floatX, floatY);

                    Color shadowColor = (drawColor * 0.4f) * (factor * 0.35f);
                    shadowColor.A = (byte)(180 * factor * 0.35f);
                    spriteBatch.Draw(tex, oldPos, null, shadowColor, rot, origin, scaleVec, effects, 0f);
                }
            }

            // Queen sprite
            spriteBatch.Draw(tex, drawCenter, null, drawColor, rot, origin, scaleVec, effects, 0f);

            // Front halo pieces
            DrawChessHalo(spriteBatch, screenPos, drawColor, isFront: true);

            // Elevated piece
            DrawElevatedActivePiece(spriteBatch, screenPos, drawColor);

            // Queen asterisk beams & pieces
            if (CurrentState == QueenAttackState.PieceAttacking && SelectedPiece == ChessPieceType.Queen)
                DrawQueenAsteriskAttack(spriteBatch, screenPos, drawColor);

            return false;
        }

        private void DrawQueenAsteriskAttack(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (queenAsteriskPieces == null)
                return;

            Texture2D trailTex = glowTexture?.Value;

            if (trailTex != null)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

                Vector2 origin = new Vector2(0f, trailTex.Height * 0.5f);

                for (int i = 0; i < queenAsteriskPieces.Length; i++)
                {
                    AsteriskPiece piece = queenAsteriskPieces[i];
                    if (piece.Trail == null || piece.TrailCount <= 1)
                        continue;

                    Color baseTint = piece.IsBishop
                        ? new Color(220, 120, 255)
                        : new Color(110, 220, 255);

                    for (int t = 0; t < piece.TrailCount - 1; t++)
                    {
                        float progress = 1f - (float)t / (piece.TrailCount - 1);
                        Vector2 p1 = piece.Trail[t] - screenPos;
                        Vector2 p2 = piece.Trail[t + 1] - screenPos;
                        Vector2 seg = p2 - p1;
                        float segLen = seg.Length();

                        if (segLen <= 0.5f)
                            continue;

                        float angle = seg.ToRotation();
                        Vector2 dir = seg / segLen;
                        Vector2 drawStart = p1 - dir * 8f; // Slight overlap into the previous segment
                        float scaleX = (segLen + 16f) / trailTex.Width;
                        float scaleY = MathHelper.Lerp(0.28f, 0.85f, progress);
                        Color segColor = baseTint * (progress * 0.85f);

                        spriteBatch.Draw(trailTex, drawStart, null, segColor, angle, origin, new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);
                    }
                }

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }

            for (int i = 0; i < queenAsteriskPieces.Length; i++)
            {
                AsteriskPiece piece = queenAsteriskPieces[i];
                ChessPieceType pieceType = piece.IsBishop ? ChessPieceType.Bishop : ChessPieceType.Rook;
                Texture2D pieceTex = GetPieceTexture(pieceType);
                if (pieceTex == null)
                    continue;

                Vector2 drawPos = piece.CurrentPos - screenPos;
                Vector2 pieceOrigin = new Vector2(pieceTex.Width * 0.5f, pieceTex.Height * 0.5f);
                float scale = HaloPieceHeight / pieceTex.Height;
                float rot = MathF.Sin(animTimer * 2f + i) * 0.08f;

                spriteBatch.Draw(pieceTex, drawPos, null, drawColor, rot, pieceOrigin, scale, SpriteEffects.None, 0f);
            }
        }

        // Halo drawing
        private void DrawChessHalo(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, bool isFront)
        {
            Texture2D glowTex = glowTexture?.Value;
            Vector2 glowOrigin = glowTex != null ? new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f) : Vector2.Zero;

            for (int i = 0; i < HaloPieceCount; i++)
            {
                bool isElevated = (i == SelectedHaloIndex) &&
                    (CurrentState == QueenAttackState.PieceAscending ||
                     CurrentState == QueenAttackState.PieceAttacking ||
                     CurrentState == QueenAttackState.PieceReturning);

                if (isElevated) continue;

                float slotAngle = (haloOrbitTimer + spinAngleOffset) + i * (MathHelper.TwoPi / HaloPieceCount);
                float depthSin = MathF.Sin(slotAngle);
                if ((depthSin >= 0f) != isFront) continue;

                Texture2D pieceTex = GetPieceTexture(HaloPieceSlots[i]);
                if (pieceTex == null) continue;

                Vector2 drawPos = GetHaloPiecePosition(i) - screenPos;
                float depthScale = MathHelper.Lerp(0.82f, 1.15f, (depthSin + 1f) * 0.5f);
                float scale = (HaloPieceHeight / pieceTex.Height) * depthScale;
                Vector2 origin = new Vector2(pieceTex.Width * 0.5f, pieceTex.Height * 0.5f);

                if (glowTex != null)
                {
                    Color haloGlow = new Color(220, 230, 255, 0) * (0.35f + 0.15f * depthSin);
                    spriteBatch.Draw(glowTex, drawPos, null, haloGlow, 0f, glowOrigin, 0.45f * depthScale, SpriteEffects.None, 0f);
                }

                Color pieceColor = drawColor * MathHelper.Lerp(0.70f, 1f, (depthSin + 1f) * 0.5f);
                spriteBatch.Draw(pieceTex, drawPos, null, pieceColor, MathF.Sin(animTimer + i) * 0.08f, origin, scale, SpriteEffects.None, 0f);
            }
        }

        // Elevated piece drawing
        private void DrawElevatedActivePiece(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (CurrentState != QueenAttackState.PieceAscending &&
                CurrentState != QueenAttackState.PieceAttacking &&
                CurrentState != QueenAttackState.PieceReturning)
                return;

            Texture2D pieceTex = GetPieceTexture(SelectedPiece);
            if (pieceTex == null) return;

            Vector2 drawPos = GetActivePieceWorldPosition() - screenPos;
            Vector2 origin = new Vector2(pieceTex.Width * 0.5f, pieceTex.Height * 0.5f);

            float normalScale = HaloPieceHeight / pieceTex.Height;
            float maxScale = ElevatedPieceHeight / pieceTex.Height;

            float currentScale = CurrentState switch
            {
                QueenAttackState.PieceAscending => MathHelper.Lerp(normalScale, maxScale, MathHelper.SmoothStep(0f, 1f, Math.Clamp(StateTimer / AscendDurationTicks, 0f, 1f))),
                QueenAttackState.PieceAttacking => maxScale * (1f + MathF.Sin(animTimer * 3f) * 0.05f),
                QueenAttackState.PieceReturning => MathHelper.Lerp(maxScale, normalScale, MathHelper.SmoothStep(0f, 1f, Math.Clamp(StateTimer / ReturnDurationTicks, 0f, 1f))),
                _ => normalScale
            };

            Texture2D glowTex = glowTexture?.Value;
            if (glowTex != null)
            {
                Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);
                Color auraColor = new Color(255, 255, 255, 0) * (0.65f + (0.5f + 0.5f * MathF.Sin(animTimer * 4f)) * 0.25f);
                spriteBatch.Draw(glowTex, drawPos, null, auraColor, animTimer * 0.5f, glowOrigin, currentScale * 2.2f, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(pieceTex, drawPos, null, Color.White, MathF.Sin(animTimer * 1.5f) * 0.06f, origin, currentScale, SpriteEffects.None, 0f);
        }
    }
}