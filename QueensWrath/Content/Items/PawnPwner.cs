using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Items
{
	// This is a basic item template.
	// Please see tModLoader's ExampleMod for every other example:
	// https://github.com/tModLoader/tModLoader/tree/stable/ExampleMod
	public class PawnPwner : ModItem
	{
		// The Display Name and Tooltip of this item can be edited in the 'Localization/en-US_Mods.TestMod.hjson' file.
		public override void SetDefaults()
		{
			Item.damage = 15;
			Item.DamageType = DamageClass.MeleeNoSpeed;
			Item.width = 20;
			Item.height = 80;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 25;
			Item.value = Item.buyPrice(silver: 1);
			Item.rare = ItemRarityID.Blue;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;

		}

		public override bool MeleePrefix() => true;

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone){
			target.AddBuff(BuffID.Frostburn2, 300);

		}


		public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.DirtBlock, 10);
			recipe.AddTile(TileID.WorkBenches);
			recipe.Register();
		}
	}
}
