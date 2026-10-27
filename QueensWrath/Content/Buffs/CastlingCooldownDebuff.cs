using Terraria;
using Terraria.ModLoader;

namespace QueensWrath.Content.Buffs
{
    public class CastlingCooldownDebuff : ModBuff
    {
        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
        }
    }
}
