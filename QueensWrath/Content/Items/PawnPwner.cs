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
			Item.DefaultToSpear(ModContent.ProjectileType<Projectiles.PawnLance>(), 1f, 24);

			Item.DamageType = DamageClass.MeleeNoSpeed; // We need to use MeleeNoSpeed here so that attack speed doesn't effect our held projectile.

			Item.SetWeaponValues(100, 12f, 0); // A special method that sets the damage, knockback, and bonus critical strike chance.

			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(0, 6);

			Item.channel = true; // Channel is important for our projectile.

			// This will make sure our projectile completely disappears on hurt.
			// It's not enough just to stop the channel, as the lance can still deal damage while being stowed
			// If two players charge at each other, the first one to hit should cancel the other's lance
			Item.StopAnimationOnHurt = true;

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
