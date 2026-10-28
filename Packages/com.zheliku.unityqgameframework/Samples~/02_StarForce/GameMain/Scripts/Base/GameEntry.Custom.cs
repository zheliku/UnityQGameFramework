//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 游戏入口。
    /// </summary>
    public partial class GameEntry : MonoBehaviour
    {
        /// <summary>
        /// 获取内置启动数据组件。
        /// </summary>
        public static BuiltinDataComponent BuiltinData
        {
            get; private set;
        }

        /// <summary>
        /// 获取血条组件。
        /// </summary>
        public static HPBarComponent HPBar
        {
            get; private set;
        }

        private static void InitCustomComponents()
        {
            BuiltinData = UnityQGameFramework.Runtime.GameEntry.GetComponent<BuiltinDataComponent>();
            HPBar = UnityQGameFramework.Runtime.GameEntry.GetComponent<HPBarComponent>();
        }
    }
}
