using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using QueensWrath.Content.Items;

namespace QueensWrath.Content.NPCs
{
    
    public class QueenPiece : ModNPC
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
            NPC.width = 84;
            NPC.height = 200;
            NPC.damage = 200;
            NPC.defense = 50;
            NPC.lifeMax = 250000;
            NPC.value = 1000000f;
            NPC.knockBackResist = 0f;
            NPC.boss = true; // Marks this NPC as a boss
            Music = MusicID.QueenSlime; // Set the music to Queen slime for now
            //NPC.noGravity = true; // The queen piece should not be affected by gravity
            NPC.noTileCollide = true; // The queen piece should not collide with tiles
            NPC.netAlways = true; // Ensure the NPC is always synced over the network



        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Drop the weapons item when the queen is defeated
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BishopStaff>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SiegeBastion>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RookRectifier>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NightOfKnightsItem>(), 1));

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