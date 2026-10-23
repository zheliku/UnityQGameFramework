//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace StarForce
{
    [StructLayout(LayoutKind.Auto)]
    public struct ImpactData
    {
        public ImpactData(CampType camp, int hp, int attack, int defense)
        {
            Camp = camp;
            HP = hp;
            Attack = attack;
            Defense = defense;
        }

        public CampType Camp { get; }

        public int HP { get; }

        public int Attack { get; }

        public int Defense { get; }
    }
}
