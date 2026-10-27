using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.NPCs
{
	public class BossTestNPC : ModNPC
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Guide;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 26;
			AnimationType = NPCID.Guide;
		}

		public override void SetDefaults()
		{
			NPC.width = 18;
			NPC.height = 40;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.lifeMax = 100;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.5f;
			NPC.aiStyle = NPCAIStyleID.Passive;
			NPC.friendly = false;
		}

		public override void OnKill()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				Main.dayTime = false;
				Main.time = 0;
				NPC.SpawnOnPlayer(Player.FindClosest(NPC.position, NPC.width, NPC.height), NPCID.EyeofCthulhu);
			}
		}
	}
}
