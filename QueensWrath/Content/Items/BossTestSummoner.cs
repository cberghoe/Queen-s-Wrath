using System;
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
			return !NPC.AnyNPCs(ModContent.NPCType<KingPiece>());
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer)
			{
				SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.7f, Pitch = 0.2f }, player.position);

				int dir = player.direction != 0 ? player.direction : 1;
				float spawnX = player.Center.X + dir * 140f;
				int tileX = Math.Clamp((int)(spawnX / 16f), 10, Main.maxTilesX - 10);
				int tileY = Math.Clamp((int)(player.Bottom.Y / 16f), 10, Main.maxTilesY - 10);

				while (tileY < Main.maxTilesY - 10 && (!Main.tile[tileX, tileY].HasTile || !Main.tileSolid[Main.tile[tileX, tileY].TileType] || Main.tile[tileX, tileY].IsActuated))
				{
					tileY++;
				}
				float spawnY = tileY * 16f - 96f;

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					NPC.NewNPC(player.GetSource_ItemUse(Item), (int)spawnX, (int)spawnY, ModContent.NPCType<KingPiece>());
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
				.AddIngredient(ItemID.DirtBlock)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.MudBlock)
				.Register();
		}
	}
}
