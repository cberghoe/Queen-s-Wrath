using QueensWrath.Content.NPCs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Items
{
	public class LuminiteKingPiece : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 10;
			Item.height = 10;
            //Item.scale = 1f;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Green;
			Item.useAnimation = 30;
			Item.useTime = 50;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = false;
		}

		public override bool CanUseItem(Player player)
		{
			return !NPC.AnyNPCs(ModContent.NPCType<KingPiece>());
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer)
			{
				SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.7f, Pitch = 0.2f }, player.position);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					NPC.NewNPC(player.GetSource_ItemUse(Item), (int)player.Center.X + player.direction * 60, (int)player.Center.Y, ModContent.NPCType<KingPiece>());
				}
				else
				{
					NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: ModContent.NPCType<KingPiece>());
				}
			}

			return true;
		}
		
		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.LunarBar, 8)
                .AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
