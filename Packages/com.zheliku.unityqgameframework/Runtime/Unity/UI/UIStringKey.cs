//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 界面字符型主键。
    /// </summary>
    public sealed class UIStringKey : MonoBehaviour
    {
        [SerializeField]
        private string m_Key = null;

        /// <summary>
        /// 获取或设置主键。
        /// </summary>
        public string Key
        {
            get => m_Key ?? string.Empty;
            set => m_Key = value;
        }
    }
}
