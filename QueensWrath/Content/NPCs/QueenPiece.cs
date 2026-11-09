using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using QueensWrath.Content.Items;

namespace QueensWrath.Content.NPCs
{
    public class QueenPiece : ModNPC
    {
        // Aliases for NPC.ai[] state variables
        public ref float State => ref NPC.ai[0];
        public ref float Timer => ref NPC.ai[1];
        public ref float SideDirection => ref NPC.ai[2];

        // Movement Phase Enum matching Empress of Light's flight sequence
        public enum AIState
        {
            HoverAbove,     // Smooth hover to top-left / top-right of player
            AlignDash,      // Horizontal line-up prior to dash
            Dash,           // High-speed predictive dash past player
            ArcSweep        // Wide sweeping curve around player
        }

        public override void SetStaticDefaults()
        {
            // Register as a boss so it gets priority in the Bestiary
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 84;
            NPC.height = 200;
            NPC.damage = 200;
            NPC.defense = 50;
            NPC.lifeMax = 250000;
            NPC.value = 1000000f;
            NPC.knockBackResist = 0f;
            NPC.boss = true;             // Enables the Boss Health Bar
            NPC.aiStyle = -1;            // Custom AI logic (disables vanilla AI override)
            Music = MusicID.QueenSlime;
            NPC.noGravity = true;        // Required for smooth floating flight
            NPC.noTileCollide = true;    // Allows passing through terrain
            NPC.netAlways = true;        // Syncs AI state accurately over multiplayer
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Drop weapons item when the queen is defeated
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BishopStaff>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SiegeBastion>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RookRectifier>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NightOfKnightsItem>(), 1));
        }

        public override void AI()
        {
            // Acquire / validate target player
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
            {
                NPC.TargetClosest(true);
            }

            Player target = Main.player[NPC.target];

            // Float upward and despawn if player dies or leaves world
            if (!target.active || target.dead)
            {
                NPC.velocity.Y -= 0.05f;
                NPC.EncourageDespawn(10);
                return;
            }

            // Face the direction of the player
            NPC.spriteDirection = NPC.direction = target.Center.X > NPC.Center.X ? 1 : -1;

            // Initialize side direction multiplier on first tick
            if (SideDirection == 0f) 
                SideDirection = 1f;

            Timer++;

            switch ((AIState)State)
            {
                case AIState.HoverAbove:
                    // Smooth hover ~260px above and 250px offset to the player's side
                    Vector2 hoverTarget = target.Center + new Vector2(250f * SideDirection, -260f);
                    SmoothMoveTo(hoverTarget, maxSpeed: 18f, inertia: 14f);

                    if (Timer >= 180) // 3 seconds hover duration
                    {
                        Timer = 0;
                        State = (float)AIState.AlignDash;
                    }
                    break;

                case AIState.AlignDash:
                    // Rapidly realign horizontally to player height prior to dash
                    float side = (NPC.Center.X < target.Center.X) ? -1f : 1f;
                    Vector2 prepPos = target.Center + new Vector2(side * 450f, -20f);
                    
                    SmoothMoveTo(prepPos, maxSpeed: 28f, inertia: 8f);

                    if (Timer >= 40) // 0.66 second telegraph alignment
                    {
                        Timer = 0;
                        // Predict future player position based on movement vector
                        Vector2 predictedTarget = target.Center + (target.velocity * 12f);
                        Vector2 dashDir = (predictedTarget - NPC.Center).SafeNormalize(Vector2.UnitX);
                        
                        // Execute high-speed dash burst
                        NPC.velocity = dashDir * 34f;
                        State = (float)AIState.Dash;
                    }
                    break;

                case AIState.Dash:
                    // Apply EoL exponential friction to slow down post-dash
                    NPC.velocity *= 0.95f;

                    if (Timer >= 35) 
                    {
                        Timer = 0;
                        SideDirection *= -1f; // Alternate side for next flight phase
                        State = (float)AIState.ArcSweep;
                    }
                    break;

                case AIState.ArcSweep:
                    // Sweeping arc motion over the player
                    float progress = Timer / 120f; // 2 second arc duration
                    float angle = MathHelper.Pi * progress;
                    
                    Vector2 arcOffset = new Vector2(
                        (float)Math.Cos(angle) * 350f * -SideDirection,
                        -(float)Math.Sin(angle) * 200f - 100f
                    );

                    Vector2 arcTarget = target.Center + arcOffset;
                    SmoothMoveTo(arcTarget, maxSpeed: 22f, inertia: 10f);

                    if (Timer >= 120)
                    {
                        Timer = 0;
                        State = (float)AIState.HoverAbove;
                    }
                    break;
            }
        }

        /// <summary>
        /// Exponential acceleration calculation mimicking Empress of Light movement.
        /// </summary>
        private void SmoothMoveTo(Vector2 targetPos, float maxSpeed, float inertia)
        {
            Vector2 desiredVelocity = targetPos - NPC.Center;
            float distance = desiredVelocity.Length();

            if (distance > 0f)
            {
                desiredVelocity.Normalize();
                float speed = Math.Min(distance * 0.12f, maxSpeed);
                NPC.velocity = (NPC.velocity * (inertia - 1f) + desiredVelocity * speed) / inertia;
            }
        }
    }
}