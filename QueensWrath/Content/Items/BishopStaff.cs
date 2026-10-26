using Microsoft.Xna.Framework;
using QueensWrath.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Items
{
	public class BishopStaff : ModItem
	{
		public override void SetStaticDefaults()
		{
			// IMPORTANT!!!
			Item.staff[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.damage = 260;
			Item.DamageType = DamageClass.Magic;
			// IMPORTANT!!!
			Item.mana = 10;
			Item.width = 48;
			Item.height = 48;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.knockBack = 6f;
			Item.value = Item.sellPrice(gold: 15);
			Item.rare = ItemRarityID.Red;
			Item.UseSound = SoundID.Item43;
			Item.autoReuse = true;
			// IMPORTANT!!!
			Item.shoot = ModContent.ProjectileType<BishopPiece>();
			Item.shootSpeed = 24f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			// IMPORTANT!!!
			Vector2 target = Main.MouseWorld;
			const float distance = 260f;

			// 4 diagonal corners around target (NW, NE, SW, SE)
			Vector2[] offsets = new Vector2[]
			{
				new Vector2(-distance, -distance),
				new Vector2(distance, -distance),
				new Vector2(-distance, distance),
				new Vector2(distance, distance)
			};

			for (int i = 0; i < 4; i++)
			{
				Vector2 spawnPos = target + offsets[i];
				Vector2 speed = (target - spawnPos).SafeNormalize(Vector2.Zero) * Item.shootSpeed;

				// IMPORTANT!!!
				Projectile.NewProjectile(source, spawnPos, speed, type, damage, knockback, player.whoAmI, offsets[i].Length(), i);
			}

			// IMPORTANT!!!
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.FragmentNebula, 18)
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient(ItemID.MarbleBlock, 25)
				.AddTile(TileID.LunarCraftingStation)
				.Register();

			CreateRecipe()
				.AddIngredient(ItemID.DirtBlock, 10)
				.AddTile(TileID.WorkBenches)
				.Register();
		}
	}
}
