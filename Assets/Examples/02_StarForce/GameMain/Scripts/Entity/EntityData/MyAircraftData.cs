//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using UnityEngine;

namespace StarForce
{
    [Serializable]
    public class MyAircraftData : AircraftData
    {
        [SerializeField]
        private string m_Name = null;

        public MyAircraftData(int entityId, int typeId)
            : base(entityId, typeId, CampType.Player)
        {
        }

        /// <summary>
        /// 角色名称。
        /// </summary>
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }
    }
}
