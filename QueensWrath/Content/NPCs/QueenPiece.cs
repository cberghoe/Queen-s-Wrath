using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using QueensWrath.Content.Items;

namespace QueensWrath.Content.NPCs
{
    public class QueenPiece : ModNPC
    {
        public override string Texture => "QueensWrath/Content/NPCs/QueenPiece";

        private float animTimer;

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

        // Modify loot
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BishopStaff>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SiegeBastion>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RookRectifier>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NightOfKnightsItem>(), 1));
        }

        // Flight AI
        public override void AI()
        {
            animTimer += 0.035f;

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

            Vector2 targetPos = target.Center + new Vector2(0f, -240f);
            Vector2 toTarget = targetPos - NPC.Center;
            float distance = toTarget.Length();

            const float maxSpeed = 8.5f;
            const float inertia = 24f;

            if (distance > 20f)
            {
                toTarget *= maxSpeed / distance;
                NPC.velocity = (NPC.velocity * (inertia - 1f) + toTarget) / inertia;
            }

            NPC.rotation = NPC.velocity.X * 0.015f;
            NPC.spriteDirection = target.Center.X >= NPC.Center.X ? 1 : -1;

            // Ambient monochromatic chess particles
            if (Main.rand.NextBool(4))
            {
                Vector2 dustPos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                Dust d = Dust.NewDustPerfect(dustPos, DustID.Smoke, new Vector2(0f, -0.6f), 120, Color.White, 0.85f);
                d.noGravity = true;
            }
        }

        // Draw NPC with procedural animation (black & white chess aesthetic: silver aura, shadow/light ghost trails, squash & stretch)
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[Type].Value;
            if (tex == null)
            {
                return false;
            }

            bool isFacingRight = NPC.spriteDirection == 1;
            SpriteEffects effects = isFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);

            // 1. Organic figure-8 floating motion
            float floatX = MathF.Sin(animTimer * 1.4f) * 6f;
            float floatY = MathF.Cos(animTimer * 2.8f) * 9f;
            Vector2 drawCenter = NPC.Center - screenPos + new Vector2(floatX, floatY);

            // 2. Breathing / Squash & Stretch
            float breathe = MathF.Sin(animTimer * 2.2f);
            float baseScale = NPC.scale * (208f / tex.Height);
            Vector2 scaleVec = new Vector2(1f - breathe * 0.035f, 1f + breathe * 0.035f) * baseScale;

            // 3. Inertial tilt and gentle sway
            float tilt = MathHelper.Clamp(NPC.velocity.X * 0.02f, -0.15f, 0.15f);
            float sway = MathF.Sin(animTimer * 1.8f) * 0.03f;
            float rot = NPC.rotation + tilt + (isFacingRight ? sway : -sway);

            // 4. Subtle monochromatic motion shadow trails when moving
            if (NPC.velocity.LengthSquared() > 0.8f)
            {
                for (int i = NPC.oldPos.Length - 1; i > 0; i--)
                {
                    if (NPC.oldPos[i] == Vector2.Zero) continue;
                    float factor = (float)(NPC.oldPos.Length - i) / NPC.oldPos.Length;
                    Vector2 oldDrawPos = NPC.oldPos[i] + new Vector2(NPC.width * 0.5f, NPC.height * 0.5f) - screenPos + new Vector2(floatX, floatY);

                    // Smooth subtle dark shadow trail
                    Color shadowColor = (drawColor * 0.4f) * (factor * 0.35f);
                    shadowColor.A = (byte)(180 * factor * 0.35f);
                    spriteBatch.Draw(tex, oldDrawPos, null, shadowColor, rot, origin, scaleVec, effects, 0f);
                }
            }

            // 5. Draw main queen sprite solidly and cleanly
            spriteBatch.Draw(tex, drawCenter, null, drawColor, rot, origin, scaleVec, effects, 0f);

            return false;
        }
    }
}
