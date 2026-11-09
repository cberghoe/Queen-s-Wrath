using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using QueensWrath.Content.Items;

namespace QueensWrath.Content.NPCs
{
    public class QueenPiece : ModNPC
    {
        public override string Texture => "QueensWrath/Content/NPCs/frame1";

        // Animation frames
        private static Asset<Texture2D> frame1Texture;
        private static Asset<Texture2D> frame2Texture;
        private float animTimer;

        // Load textures
        public override void Load()
        {
            frame1Texture = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/frame1");
            frame2Texture = ModContent.Request<Texture2D>("QueensWrath/Content/NPCs/queen_frame_2");
        }

        // Unload textures
        public override void Unload()
        {
            frame1Texture = null;
            frame2Texture = null;
        }

        // Static defaults
        public override void SetStaticDefaults()
        {
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
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
        }

        // Draw NPC
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (frame1Texture == null || !frame1Texture.IsLoaded || frame2Texture == null || !frame2Texture.IsLoaded)
            {
                return false;
            }

            // Alternate frames
            bool useFrame2 = (Main.GameUpdateCount / 12) % 2 == 1;
            Texture2D tex = useFrame2 ? frame2Texture.Value : frame1Texture.Value;

            bool isFacingRight = NPC.spriteDirection == 1;
            SpriteEffects effects = isFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
            Vector2 drawCenter = NPC.Center - screenPos;

            float bob = MathF.Sin(animTimer * 2f) * 3f;
            Vector2 bobOffset = new Vector2(0f, bob);
            float sway = MathF.Sin(animTimer * 1.8f) * 0.04f;
            float rot = NPC.rotation + (isFacingRight ? sway : -sway);

            // Texture scale
            float drawScale = NPC.scale * (208f / tex.Height);

            spriteBatch.Draw(tex, drawCenter + bobOffset, null, drawColor, rot, origin, drawScale, effects, 0f);
            return false;
        }
    }
}
