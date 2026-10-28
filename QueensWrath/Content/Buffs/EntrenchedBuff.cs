using Terraria;
using Terraria.ModLoader;

namespace QueensWrath.Content.Buffs
{
    public class EntrenchedBuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.statDefense += 30;
        }
    }
}
