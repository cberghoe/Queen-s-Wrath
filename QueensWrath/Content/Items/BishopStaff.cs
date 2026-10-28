using Microsoft.Xna.Framework;
using QueensWrath.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Items
{
	public class BishopStaff : ModItem
	{
		private const float SpawnDistance = 380f;

		private static readonly Vector2[] DiagonalUnits = new Vector2[]
		{
			new Vector2(-0.70710678f, -0.70710678f),
			new Vector2(0.70710678f, -0.70710678f),
			new Vector2(-0.70710678f, 0.70710678f),
			new Vector2(0.70710678f, 0.70710678f)
		};

		public override void SetStaticDefaults()
		{
			Item.staff[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.damage = 850;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 65;
			Item.width = 48;
			Item.height = 48;
			Item.useTime = 34;
			Item.useAnimation = 34;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.knockBack = 8.5f;
			Item.value = Item.sellPrice(gold: 20);
			Item.rare = ItemRarityID.Red;
			Item.UseSound = SoundID.Item71 with { Volume = 0.85f, Pitch = 0.2f, MaxInstances = 2 };
			Item.autoReuse = false;
			Item.shoot = ModContent.ProjectileType<BishopPiece>();
			Item.shootSpeed = 26f;
		}

		// IMPORTANT: Returning false prevents default projectile spawn and handles custom diagonal convergence
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			Vector2 target = Main.MouseWorld;

			for (int i = 0; i < DiagonalUnits.Length; i++)
			{
				Vector2 dir = DiagonalUnits[i];
				Vector2 spawnPos = target + dir * SpawnDistance;
				Vector2 speed = -dir * Item.shootSpeed;

				// IMPORTANT: Passes target distance in ai[0] for convergence check and corner index in ai[1]
				Projectile.NewProjectile(source, spawnPos, speed, type, damage / 2, knockback, player.whoAmI, SpawnDistance, i);
			}

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
