using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using QueensWrath.Content.Buffs;

namespace QueensWrath.Content.Projectiles.Minions
{
    public class NightOfKnights : ModProjectile
    {
        // Custom properties or helper getters for clarity
        private ref float State => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.ai[1];

        private ref float DirX => ref Projectile.localAI[0];
        private ref float DirY => ref Projectile.localAI[1];
        private ref float IsHorizontalFirst => ref Projectile.localAI[2];

        // Store the angle of the current dash so it can be offset for the next one
        private float dashAngle = 0f;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            //Projectile.scale = 0.1f;
        }

        public override bool MinionContactDamage() => true;

        public override void AI() {
            Player owner = Main.player[Projectile.owner];

            if (!CheckActive(owner))
                return;

            SearchForTargets(owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);

            if (foundTarget) {
                DashAttackBehavior(targetCenter);
            }
            else {
                IdleBehavior(owner);
            }

            Visuals();
        }

        private void DashAttackBehavior(Vector2 targetCenter) {
            Timer++;

            if (State == 0) {
                if (Timer == 1) {
                    // 1. Pick random Knight jump coordinates
                    DirX = Main.rand.NextBool() ? 1f : -1f;
                    DirY = Main.rand.NextBool() ? 1f : -1f;
                    IsHorizontalFirst = Main.rand.NextBool() ? 1f : 0f;

                    Vector2 startOffset = (IsHorizontalFirst == 1f)
                        ? new Vector2(DirX * 200f, DirY * 100f)
                        : new Vector2(DirX * 100f, DirY * 200f);

                    Vector2 startPosition = targetCenter + startOffset;

                    // 2. Spawn dust effects at the OLD position before disappearing
                    for (int i = 0; i < 12; i++) {
                        Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Enchanted_Gold, 0f, 0f, 150, default, 1.2f);
                    }

                    // 3. Teleport instantly
                    Projectile.Center = startPosition;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true; // Sync position across multiplayer clients

                    // 4. Spawn dust effects at the NEW position
                    for (int i = 0; i < 12; i++) {
                        Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Enchanted_Gold, 0f, 0f, 150, default, 1.2f);
                    }

                    // 5. Play teleport sound effect
                    SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
                }

                int teleportPause = 6;
                if (Timer >= teleportPause) {
                    State = 1;
                    Timer = 0;
                }
            }
            else if (State == 1) {
                float dashSpeed = 18f;

                Vector2 cornerOffset = (IsHorizontalFirst == 1f)
                    ? new Vector2(0f, DirY * 100f)
                    : new Vector2(DirX * 100f, 0f);

                Vector2 cornerPosition = targetCenter + cornerOffset;
                Vector2 toCorner = cornerPosition - Projectile.Center;

                if (toCorner.Length() > 20f && Timer < 15) {
                    toCorner.Normalize();
                    Projectile.velocity = toCorner * dashSpeed;
                } 
                else {
                    State = 2;
                    Timer = 0;

                    Vector2 turnDirection = (IsHorizontalFirst == 1f)
                        ? new Vector2(0f, -DirY)  
                        : new Vector2(-DirX, 0f); 

                    Projectile.velocity = turnDirection * dashSpeed;
                }
            }
            else if (State == 2) {
                int dashDuration = 10; // Ticks spent dashing past the enemy

                if (Timer >= dashDuration) {
                    // Attack complete: reset to setup state and pick a new Knight angle
                    State = 0;
                    Timer = 0;
                }
            }
        }

        private void IdleBehavior(Player owner) {
            // Reset attack states when idling
            State = 0;
            Timer = 0;

            int totalMinions = owner.ownedProjectileCounts[Type];
            if (totalMinions <= 0) {
                totalMinions = 1;
            }

            // Orbit smoothly around the player in a true circle (halo)
            float rotationSpeed = 0.035f;
            float baseAngle = (float)(Main.GameUpdateCount * rotationSpeed);
            float angle = baseAngle + (MathHelper.TwoPi / totalMinions) * Projectile.minionPos;

            float radius = 65f;
            Vector2 orbitCenter = owner.MountedCenter + new Vector2(0f, -10f);
            Vector2 idlePosition = orbitCenter + angle.ToRotationVector2() * radius;

            Vector2 toIdle = idlePosition - Projectile.Center;
            float distanceToIdle = toIdle.Length();

            if (distanceToIdle > 2000f) {
                Projectile.Center = idlePosition;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
                return;
            }

            if (distanceToIdle > 300f) {
                float speed = 24f;
                toIdle.Normalize();
                Projectile.velocity = (Projectile.velocity * 14f + toIdle * speed) / 15f;
            }
            else {
                Vector2 targetVelocity = toIdle * 0.25f + owner.velocity;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetVelocity, 0.35f);
            }
        }

        private bool CheckActive(Player owner) {
            if (owner.dead || !owner.active) {
                owner.ClearBuff(ModContent.BuffType<NightOfKnightsBuff>());
                return false;
            }
            if (owner.HasBuff(ModContent.BuffType<NightOfKnightsBuff>())) {
                Projectile.timeLeft = 2;
            }
            return true;
        }

        private void SearchForTargets(Player owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter) {
            distanceFromTarget = 800f;
            targetCenter = Projectile.position;
            foundTarget = false;

            if (owner.HasMinionAttackTargetNPC) {
                NPC npc = Main.npc[owner.MinionAttackTargetNPC];
                if (Vector2.Distance(npc.Center, Projectile.Center) < 2000f) {
                    distanceFromTarget = Vector2.Distance(npc.Center, Projectile.Center);
                    targetCenter = npc.Center;
                    foundTarget = true;
                }
            }

            if (!foundTarget) {
                foreach (var npc in Main.ActiveNPCs) {
                    if (npc.CanBeChasedBy()) {
                        float between = Vector2.Distance(npc.Center, Projectile.Center);
                        if (between < distanceFromTarget) {
                            distanceFromTarget = between;
                            targetCenter = npc.Center;
                            foundTarget = true;
                        }
                    }
                }
            }

            Projectile.friendly = foundTarget;
        }

        private void Visuals() {
            // Keep the knight minion upright without rotating
            Projectile.rotation = 0f;
            if (Projectile.velocity.X != 0f) {
                Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;
            }

            if (State == 1 || State == 2) {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center, 
                    DustID.Enchanted_Gold, 
                    -Projectile.velocity * 0.2f, // Pushes dust slightly backward
                    Alpha: 100, 
                    newColor: default, 
                    Scale: 1.2f
                );
                dust.noGravity = true;
            }
        }
    }
}
