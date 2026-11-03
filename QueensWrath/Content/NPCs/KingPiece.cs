using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using QueensWrath.Content.Items;

namespace QueensWrath.Content.NPCs
{
    public class KingPiece : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // Register as a boss so it gets a boss bar and priority in the Bestiary
            
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;

        }

        public override void SetDefaults()
        {
            NPC.width = 39;
            NPC.height = 79;
            NPC.damage = 40;
            NPC.defense = 10;
            NPC.lifeMax = 2000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 10000f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = 1; // Default slime AI style
            AIType = NPCID.KingSlime; // Mimic King Slime AI type if available, or write custom state logic
            NPC.boss = true; // Marks this NPC as a boss
            Music = MusicID.Boss1;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PawnPwner>(), 1));
        }

        public override void AI()
        {
            // Custom AI logic goes here (e.g., jumping patterns, minion spawns, or teleportation)

            Player target = Main.player[NPC.target];
            if (target.dead || !target.active)
            {
                // Float away if player dies
                NPC.velocity.Y -= 0.04f;
                NPC.EncourageDespawn(10);
                return;
            }

            base.AI();
        }
    }
}