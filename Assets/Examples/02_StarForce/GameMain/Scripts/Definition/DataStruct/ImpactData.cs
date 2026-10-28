//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace StarForce
{
    /// <summary>
    /// 碰撞结算使用的阵营、生命值、攻击和防御数据。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ImpactData
    {
        /// <summary>
        /// 碰撞结算使用的阵营、生命值、攻击和防御数据。
        /// </summary>
        /// <param name="camp">实体阵营。</param>
        /// <param name="hp">当前生命值。</param>
        /// <param name="attack">攻击力。</param>
        /// <param name="defense">防御力。</param>
        public ImpactData(CampType camp, int hp, int attack, int defense)
        {
            Camp = camp;
            HP = hp;
            Attack = attack;
            Defense = defense;
        }

        /// <summary>
        /// 获取所属阵营。
        /// </summary>
        public CampType Camp
        {
            get;
        }

        /// <summary>
        /// 获取当前生命值。
        /// </summary>
        public int HP
        {
            get;
        }

        /// <summary>
        /// 获取攻击力。
        /// </summary>
        public int Attack
        {
            get;
        }

        /// <summary>
        /// 获取防御力。
        /// </summary>
        public int Defense
        {
            get;
        }
    }
}
