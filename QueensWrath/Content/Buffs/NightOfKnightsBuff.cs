using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using QueensWrath.Content.Projectiles.Minions;

namespace QueensWrath.Content.Buffs
{
    public class NightOfKnightsBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;        // Prevents the buff from saving when exiting the world
            Main.buffNoTimeDisplay[Type] = true; // Hides the duration timer text on the buff icon

            // Shows the active minion counter beneath the buff icon
        }

        public override void Update(Player player, ref int buffIndex) {
            // Keep buff alive while player owns at least one NightOfKnights minion
            if (player.ownedProjectileCounts[ModContent.ProjectileType<NightOfKnights>()] > 0) {
                player.buffTime[buffIndex] = 18000;
            }
            else {
                // Despawn/remove buff when all minions are gone
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}