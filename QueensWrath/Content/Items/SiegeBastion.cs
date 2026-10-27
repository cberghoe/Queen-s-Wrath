using Microsoft.Xna.Framework;
using System;
using QueensWrath.Content.Projectiles;
using QueensWrath.Content.Buffs;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace QueensWrath.Content.Items
{
    public class SiegeBastion : ModItem
    {
        public override void SetDefaults() {
            Item.damage = 280;
            Item.DamageType = DamageClass.Ranged;
            Item.crit = 14;
            Item.width = 48;
            Item.height = 48;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 9.5f;
            Item.value = Item.sellPrice(gold: 15);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item14;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<SiegeBastionProjectile>();
            Item.shootSpeed = 28f;
            // IMPORTANT: Switch to AmmoID.Rocket when ready to enable ammo consumption
            Item.useAmmo = AmmoID.None;
        }

        // IMPORTANT: Enables right-click functionality
        public override bool AltFunctionUse(Player player) {
            return true;
        }

        private Projectile FindActiveBeacon(Player player) {
            for (int i = 0; i < Main.maxProjectiles; i++) {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == ModContent.ProjectileType<SiegeBastionBeacon>()) {
                    return p;
                }
            }
            return null;
        }

        public override bool CanUseItem(Player player) {
            if (player.altFunctionUse == 2) {
                Projectile beacon = FindActiveBeacon(player);

                if (beacon == null) {
                    if (player.HasBuff<CastlingCooldownDebuff>()) {
                        return false;
                    }

                    Item.useStyle = ItemUseStyleID.Shoot;
                    Item.useTime = 16;
                    Item.useAnimation = 16;
                    Item.UseSound = SoundID.Item8;
                }
                else {
                    Item.useStyle = ItemUseStyleID.HoldUp;
                    Item.useTime = 14;
                    Item.useAnimation = 14;
                    Item.UseSound = SoundID.Item105;
                }
                Item.noUseGraphic = false;
            }
            else {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.useTime = 24;
                Item.useAnimation = 24;
                Item.UseSound = SoundID.Item14;
                Item.noUseGraphic = false;
            }

            return base.CanUseItem(player);
        }

        // IMPORTANT: Right-click logic must run in UseItem so it triggers reliably without relying on ammo or projectile hooks
        public override bool? UseItem(Player player) {
            if (player.altFunctionUse == 2) {
                Projectile beacon = FindActiveBeacon(player);

                if (beacon == null) {
                    Vector2 toMouse = Main.MouseWorld - player.Center;
                    Vector2 launchVelocity = toMouse.SafeNormalize(Vector2.UnitX) * 7.5f;
                    int damage = (int)player.GetTotalDamage(Item.DamageType).ApplyTo(Item.damage);
                    float knockback = player.GetTotalKnockback(Item.DamageType).ApplyTo(Item.knockBack);

                    Projectile.NewProjectile(
                        player.GetSource_ItemUse(Item),
                        player.Center,
                        launchVelocity,
                        ModContent.ProjectileType<SiegeBastionBeacon>(),
                        damage,
                        knockback,
                        player.whoAmI
                    );
                }
                else {
                    PerformCastlingSwap(player, beacon);
                }

                return true;
            }

            return base.UseItem(player);
        }

        public override bool CanConsumeAmmo(Item ammo, Player player) {
            if (player.altFunctionUse == 2) {
                return false;
            }
            return base.CanConsumeAmmo(ammo, player);
        }

        // IMPORTANT: Returning false prevents default projectile spawn and handles custom axis-snapped firing
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (player.altFunctionUse == 2) {
                return false;
            }

            Vector2 target = Main.MouseWorld;
            Vector2 toTarget = target - player.Center;

            Vector2 snappedDir;
            if (Math.Abs(toTarget.X) >= Math.Abs(toTarget.Y)) {
                snappedDir = new Vector2(toTarget.X >= 0 ? 1f : -1f, 0f);
            }
            else {
                snappedDir = new Vector2(0f, toTarget.Y >= 0 ? 1f : -1f);
            }

            Vector2 snappedVelocity = snappedDir * Item.shootSpeed;

            Projectile.NewProjectile(
                source,
                player.Center,
                snappedVelocity,
                ModContent.ProjectileType<SiegeBastionProjectile>(),
                damage,
                knockback,
                player.whoAmI
            );

            return false;
        }

        private void PerformCastlingSwap(Player player, Projectile beacon) {
            int damage = (int)player.GetTotalDamage(Item.DamageType).ApplyTo(Item.damage);
            float knockback = player.GetTotalKnockback(Item.DamageType).ApplyTo(Item.knockBack);

            Vector2 startPos = player.Center;
            Vector2 targetCenter = beacon.Center;
            Vector2 toDest = targetCenter - startPos;
            float dashLength = toDest.Length();

            // IMPORTANT: Prevents the player from teleporting inside solid blocks and suffocating
            Vector2 targetTopLeft = targetCenter - new Vector2(player.width / 2f, player.height / 2f);
            if (Collision.SolidCollision(targetTopLeft, player.width, player.height)) {
                for (float f = 1f; f >= 0.1f; f -= 0.05f) {
                    Vector2 testCenter = startPos + toDest * f;
                    Vector2 testTopLeft = testCenter - new Vector2(player.width / 2f, player.height / 2f);
                    if (!Collision.SolidCollision(testTopLeft, player.width, player.height)) {
                        targetCenter = testCenter;
                        targetTopLeft = testTopLeft;
                        toDest = targetCenter - startPos;
                        dashLength = toDest.Length();
                        break;
                    }
                }
            }

            if (dashLength > 0.001f) {
                Vector2 dashDir = Vector2.Normalize(toDest);

                for (int i = 0; i < Main.maxNPCs; i++) {
                    NPC npc = Main.npc[i];
                    if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;

                    Vector2 toNPC = npc.Center - startPos;
                    float dot = Vector2.Dot(toNPC, dashDir);
                    if (dot < 0f || dot > dashLength) continue;

                    Vector2 closestPoint = startPos + dashDir * dot;
                    float perpDist = Vector2.Distance(npc.Center, closestPoint);

                    if (perpDist <= 54f) {
                        int critDamage = Math.Max(560, damage * 2);
                        NPC.HitInfo hitInfo = new NPC.HitInfo {
                            Damage = critDamage,
                            Knockback = knockback,
                            HitDirection = (npc.Center.X >= startPos.X) ? 1 : -1,
                            Crit = true,
                            DamageType = DamageClass.Ranged,
                        };
                        npc.StrikeNPC(hitInfo);
                        if (Main.netMode != NetmodeID.SinglePlayer) {
                            NetMessage.SendStrikeNPC(npc, hitInfo);
                        }

                        if (!npc.boss) {
                            npc.target = 255;
                        }
                    }
                }
            }

            beacon.Kill();

            player.Teleport(targetTopLeft, 1);
            player.velocity = Vector2.Zero;
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, (float)player.whoAmI, targetTopLeft.X, targetTopLeft.Y, 1);
            }

            player.immune = true;
            player.immuneTime = 72;
            for (int i = 0; i < player.hurtCooldowns.Length; i++) {
                player.hurtCooldowns[i] = 72;
            }

            player.AddBuff(ModContent.BuffType<EntrenchedBuff>(), 240);
            player.AddBuff(ModContent.BuffType<CastlingCooldownDebuff>(), 360);

            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.9f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item105 with { Volume = 0.7f, Pitch = -0.3f }, player.Center);

            for (int i = 0; i < 24; i++) {
                Dust d = Dust.NewDustPerfect(startPos, DustID.Granite,
                    Main.rand.NextVector2Circular(6f, 6f), 0, default, 1.5f);
                d.noGravity = true;
            }

            int trailSteps = (int)(dashLength / 14f);
            if (dashLength > 0.001f) {
                Vector2 dashDir = Vector2.Normalize(toDest);
                for (int i = 0; i < trailSteps; i++) {
                    Vector2 p = startPos + dashDir * (i * 14f);
                    Dust d = Dust.NewDustPerfect(p, DustID.LunarOre,
                        Main.rand.NextVector2Circular(2.5f, 2.5f), 0, default, 1.2f);
                    d.noGravity = true;
                }
            }

            for (int i = 0; i < 30; i++) {
                Dust d = Dust.NewDustPerfect(player.Center, DustID.Vortex,
                    Main.rand.NextVector2Circular(7f, 7f), 0, default, 1.5f);
                d.noGravity = true;
            }
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ItemID.LunarBar, 16)
                .AddIngredient(ItemID.FragmentVortex, 20)
                .AddIngredient(ItemID.GraniteBlock, 30)
                .AddIngredient(ItemID.RocketLauncher, 1)
                .AddTile(TileID.LunarCraftingStation)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.DirtBlock, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
