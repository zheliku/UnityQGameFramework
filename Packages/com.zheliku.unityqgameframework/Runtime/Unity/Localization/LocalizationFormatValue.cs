using System;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 保存 Inspector 中声明的命名格式值，动态值由业务代码更新。
    /// </summary>
    [Serializable]
    public sealed class LocalizationFormatValue
    {
        [SerializeField] private string m_Name;
        [SerializeField] private LocalizationFormatValueType m_Type;
        [SerializeField] private string m_Text;
        [SerializeField] private int m_Integer;
        [SerializeField] private double m_Number;
        [SerializeField] private bool m_Boolean;

        /// <summary>
        /// 获取翻译中的占位符名称，不含大括号。
        /// </summary>
        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        /// <summary>
        /// 读取配置的强类型值。
        /// </summary>
        /// <returns>字符串、整数、小数或布尔值。</returns>
        public object GetValue()
        {
            switch (m_Type)
            {
                case LocalizationFormatValueType.Integer:
                    return m_Integer;
                case LocalizationFormatValueType.Number:
                    return m_Number;
                case LocalizationFormatValueType.Boolean:
                    return m_Boolean;
                default:
                    return m_Text;
            }
        }
    }

    /// <summary>
    /// 指定 Inspector 格式值的类型。
    /// </summary>
    public enum LocalizationFormatValueType
    {
        /// <summary>
        /// 文本。
        /// </summary>
        Text,

        /// <summary>
        /// 整数。
        /// </summary>
        Integer,

        /// <summary>
        /// 小数。
        /// </summary>
        Number,

        /// <summary>
        /// 布尔值。
        /// </summary>
        Boolean
    }

    /// <summary>
    /// 指定翻译键缺失时的文本显示方式。
    /// </summary>
    public enum MissingTranslationBehavior
    {
        /// <summary>
        /// 显示模块返回的缺键诊断。
        /// </summary>
        ShowKey,

        /// <summary>
        /// 显示预制体原始文本。
        /// </summary>
        KeepOriginal,

        /// <summary>
        /// 清空文本。
        /// </summary>
        ClearText
    }
}
