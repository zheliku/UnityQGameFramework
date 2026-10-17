using System;
namespace StarForce
{
    public sealed partial class DRAircraft
    {
        public int WeaponIdCount => 3;
        public int ArmorIdCount => 3;
        public int GetWeaponId(int id) => GetWeaponIdAt(id);
        public int GetArmorId(int id) => GetArmorIdAt(id);
        public int GetWeaponIdAt(int index) => index switch
        {
            0 => WeaponId0, 1 => WeaponId1, 2 => WeaponId2,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        public int GetArmorIdAt(int index) => index switch
        {
            0 => ArmorId0, 1 => ArmorId1, 2 => ArmorId2,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }
}
