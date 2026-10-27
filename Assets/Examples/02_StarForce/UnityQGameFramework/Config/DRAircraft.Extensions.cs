using System;

namespace StarForce
{
    /// <summary>
    /// 飞机配置记录的武器和装甲索引扩展。
    /// </summary>
    public sealed partial class DRAircraft
    {
        /// <summary>
        /// 获取武器配置槽位数。
        /// </summary>
        public int WeaponIdCount
        {
            get
            {
                return 3;
            }
        }

        /// <summary>
        /// 获取装甲配置槽位数。
        /// </summary>
        public int ArmorIdCount
        {
            get
            {
                return 3;
            }
        }

        /// <summary>
        /// 取得指定槽位的武器配置编号。
        /// </summary>
        /// <param name="id">从零开始的配置槽位索引。</param>
        /// <returns>对应武器配置编号。</returns>
        public int GetWeaponId(int id)
        {
            return GetWeaponIdAt(id);
        }

        /// <summary>
        /// 取得指定槽位的装甲配置编号。
        /// </summary>
        /// <param name="id">从零开始的配置槽位索引。</param>
        /// <returns>对应装甲配置编号。</returns>
        public int GetArmorId(int id)
        {
            return GetArmorIdAt(id);
        }

        /// <summary>
        /// 取得指定槽位的武器配置编号。
        /// </summary>
        /// <param name="index">从零开始的配置槽位索引。</param>
        /// <returns>对应武器配置编号。</returns>
        public int GetWeaponIdAt(int index)
        {
            return index switch
            {
                0 => WeaponId0,
                1 => WeaponId1,
                2 => WeaponId2,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        /// <summary>
        /// 取得指定槽位的装甲配置编号。
        /// </summary>
        /// <param name="index">从零开始的配置槽位索引。</param>
        /// <returns>对应装甲配置编号。</returns>
        public int GetArmorIdAt(int index)
        {
            return index switch
            {
                0 => ArmorId0,
                1 => ArmorId1,
                2 => ArmorId2,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }
    }
}
