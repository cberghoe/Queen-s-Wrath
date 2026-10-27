using QueensWrath.Content.NPCs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Items
{
	public class BossTestSummoner : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Green;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = false;
		}

		public override bool CanUseItem(Player player)
		{
			return !NPC.AnyNPCs(ModContent.NPCType<BossTestNPC>());
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer)
			{
				SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.7f, Pitch = 0.2f }, player.position);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					NPC.NewNPC(player.GetSource_ItemUse(Item), (int)player.Center.X + player.direction * 60, (int)player.Center.Y, ModContent.NPCType<BossTestNPC>());
				}
				else
				{
					NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: ModContent.NPCType<BossTestNPC>());
				}
			}

			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.DirtBlock)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.MudBlock)
				.Register();
		}
	}
}
