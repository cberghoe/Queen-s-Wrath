using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Items
{

	public class RookRectifier : ModItem
	{

        private int shotCount = 0; // Variable to keep track of which position to shoot from

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 42;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 10;
			Item.useAnimation = 10;
			Item.autoReuse = true;
			Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true; 
			Item.damage = 250;
			Item.knockBack = 6;
			Item.crit = 6;
			Item.value = Item.buyPrice(gold: 5);
			Item.rare = ItemRarityID.Pink;
			Item.UseSound = SoundID.Item70;
			Item.shoot = ModContent.ProjectileType<Projectiles.RookProjectile>(); 
			Item.shootSpeed = 8f; 
            //Item.useAmmo = AmmoID.Bullet;

		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 target = Main.screenPosition + new Vector2(Main.mouseX, Main.mouseY);
            
            // CODE TO SPAWN 4 ROOKS ONE AT A TIME
            if (shotCount < 1) {
                position = new Vector2(target.X, target.Y - 600f);
			    Vector2 heading = new Vector2(0f, 40f);
			    Projectile.NewProjectile(source, position, heading, type, damage * 2, knockback, player.whoAmI, 0f, 0f);
            } 
            else if (shotCount < 2) {
                position = new Vector2(target.X - 600f, target.Y);
			    Vector2 heading = new Vector2(40f, 0f);
			    Projectile.NewProjectile(source, position, heading, type, damage * 2, knockback, player.whoAmI, 0f, 0f);
            }
            else if (shotCount < 3) {
                position = new Vector2(target.X, target.Y + 600f);
			    Vector2 heading = new Vector2(0f, -40f);
			    Projectile.NewProjectile(source, position, heading, type, damage * 2, knockback, player.whoAmI, 0f, 0f);
            }
            else {
                position = new Vector2(target.X + 600f, target.Y);
			    Vector2 heading = new Vector2(-40f, 0f);
			    Projectile.NewProjectile(source, position, heading, type, damage * 2, knockback, player.whoAmI, 0f, 0f);
            }

            shotCount += 1;
            if (shotCount > 3) {
                shotCount = 0;
            }

            // CODE TO SPAWN 4 ROOKS AT ONCE
            /*
            for (int i = 0; i < 4; i++) {
                if (i < 1) {
                    position = new Vector2(target.X, target.Y - 600f);
				    Vector2 heading = new Vector2(0f, 40f);
				    Projectile.NewProjectile(source, position, heading, type, damage * 2, knockback, player.whoAmI, 0f, 0f);
                } 
                else if (i < 2) {
                    position = new Vector2(target.X - 600f, target.Y);
				    Vector2 heading = new Vector2(40f, 0f);
				    Projectile.NewProjectile(source, position, heading, type, damage * 2, knockback, player.whoAmI, 0f, 0f);
                }
                else if (i < 3) {
                    position = new Vector2(target.X, target.Y + 600f);
				    Vector2 heading = new Vector2(0f, -40f);
				    Projectile.NewProjectile(source, position, heading, type, damage * 2, knockback, player.whoAmI, 0f, 0f);
                }
                else {
                    position = new Vector2(target.X + 600f, target.Y);
				    Vector2 heading = new Vector2(-40f, 0f);
				    Projectile.NewProjectile(source, position, heading, type, damage * 2, knockback, player.whoAmI, 0f, 0f);
                }
			}
            */
            
            // CODE TO SPAWN 2 ROOKS AT SCREEN CEILING
            /*
			float ceilingLimit = target.Y;
			if (ceilingLimit > player.Center.Y - 200f) {
				ceilingLimit = player.Center.Y - 200f;
			}

			// Loop these functions 2 times.
			for (int i = 0; i < 2; i++) {
                position = new Vector2(target.X - 60f + (i * 120f), player.Center.Y - 600f);
				Vector2 heading = new Vector2(0f, 40f);
				Projectile.NewProjectile(source, position, heading, type, damage * 2, knockback, player.whoAmI, 0f, ceilingLimit);
			}
            */

			return false;
		}

		public override void AddRecipes() 
        {
			Recipe recipe = CreateRecipe();
				recipe.AddIngredient(ItemID.StoneBlock, 10);
				recipe.AddTile(TileID.WorkBenches);
				recipe.Register();
		}
	}
}
