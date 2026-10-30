//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Globalization;

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 以标准语言代码标识语言；项目可定义自己的静态语言项。
    /// </summary>
    public readonly struct Language : IEquatable<Language>
    {
        private readonly string m_Code;

        /// <summary>
        /// 未指定。
        /// </summary>
        public static readonly Language Unspecified = default;

        /// <summary>
        /// 南非荷兰语。
        /// </summary>
        public static readonly Language Afrikaans = new Language("af");

        /// <summary>
        /// 阿尔巴尼亚语。
        /// </summary>
        public static readonly Language Albanian = new Language("sq");

        /// <summary>
        /// 阿拉伯语。
        /// </summary>
        public static readonly Language Arabic = new Language("ar");

        /// <summary>
        /// 巴斯克语。
        /// </summary>
        public static readonly Language Basque = new Language("eu");

        /// <summary>
        /// 白俄罗斯语。
        /// </summary>
        public static readonly Language Belarusian = new Language("be");

        /// <summary>
        /// 保加利亚语。
        /// </summary>
        public static readonly Language Bulgarian = new Language("bg");

        /// <summary>
        /// 加泰罗尼亚语。
        /// </summary>
        public static readonly Language Catalan = new Language("ca");

        /// <summary>
        /// 简体中文。
        /// </summary>
        public static readonly Language ChineseSimplified = new Language("zh-Hans");

        /// <summary>
        /// 繁体中文。
        /// </summary>
        public static readonly Language ChineseTraditional = new Language("zh-Hant");

        /// <summary>
        /// 克罗地亚语。
        /// </summary>
        public static readonly Language Croatian = new Language("hr");

        /// <summary>
        /// 捷克语。
        /// </summary>
        public static readonly Language Czech = new Language("cs");

        /// <summary>
        /// 丹麦语。
        /// </summary>
        public static readonly Language Danish = new Language("da");

        /// <summary>
        /// 荷兰语。
        /// </summary>
        public static readonly Language Dutch = new Language("nl");

        /// <summary>
        /// 英语。
        /// </summary>
        public static readonly Language English = new Language("en");

        /// <summary>
        /// 爱沙尼亚语。
        /// </summary>
        public static readonly Language Estonian = new Language("et");

        /// <summary>
        /// 法罗语。
        /// </summary>
        public static readonly Language Faroese = new Language("fo");

        /// <summary>
        /// 芬兰语。
        /// </summary>
        public static readonly Language Finnish = new Language("fi");

        /// <summary>
        /// 法语。
        /// </summary>
        public static readonly Language French = new Language("fr");

        /// <summary>
        /// 格鲁吉亚语。
        /// </summary>
        public static readonly Language Georgian = new Language("ka");

        /// <summary>
        /// 德语。
        /// </summary>
        public static readonly Language German = new Language("de");

        /// <summary>
        /// 希腊语。
        /// </summary>
        public static readonly Language Greek = new Language("el");

        /// <summary>
        /// 希伯来语。
        /// </summary>
        public static readonly Language Hebrew = new Language("he");

        /// <summary>
        /// 匈牙利语。
        /// </summary>
        public static readonly Language Hungarian = new Language("hu");

        /// <summary>
        /// 冰岛语。
        /// </summary>
        public static readonly Language Icelandic = new Language("is");

        /// <summary>
        /// 印尼语。
        /// </summary>
        public static readonly Language Indonesian = new Language("id");

        /// <summary>
        /// 意大利语。
        /// </summary>
        public static readonly Language Italian = new Language("it");

        /// <summary>
        /// 日语。
        /// </summary>
        public static readonly Language Japanese = new Language("ja");

        /// <summary>
        /// 韩语。
        /// </summary>
        public static readonly Language Korean = new Language("ko");

        /// <summary>
        /// 拉脱维亚语。
        /// </summary>
        public static readonly Language Latvian = new Language("lv");

        /// <summary>
        /// 立陶宛语。
        /// </summary>
        public static readonly Language Lithuanian = new Language("lt");

        /// <summary>
        /// 马其顿语。
        /// </summary>
        public static readonly Language Macedonian = new Language("mk");

        /// <summary>
        /// 马拉雅拉姆语。
        /// </summary>
        public static readonly Language Malayalam = new Language("ml");

        /// <summary>
        /// 挪威语。
        /// </summary>
        public static readonly Language Norwegian = new Language("nb");

        /// <summary>
        /// 波斯语。
        /// </summary>
        public static readonly Language Persian = new Language("fa");

        /// <summary>
        /// 波兰语。
        /// </summary>
        public static readonly Language Polish = new Language("pl");

        /// <summary>
        /// 巴西葡萄牙语。
        /// </summary>
        public static readonly Language PortugueseBrazil = new Language("pt-BR");

        /// <summary>
        /// 葡萄牙语。
        /// </summary>
        public static readonly Language PortuguesePortugal = new Language("pt-PT");

        /// <summary>
        /// 罗马尼亚语。
        /// </summary>
        public static readonly Language Romanian = new Language("ro");

        /// <summary>
        /// 俄语。
        /// </summary>
        public static readonly Language Russian = new Language("ru");

        /// <summary>
        /// 塞尔维亚克罗地亚语。
        /// </summary>
        public static readonly Language SerboCroatian = new Language("hr");

        /// <summary>
        /// 塞尔维亚西里尔语。
        /// </summary>
        public static readonly Language SerbianCyrillic = new Language("sr-Cyrl");

        /// <summary>
        /// 塞尔维亚拉丁语。
        /// </summary>
        public static readonly Language SerbianLatin = new Language("sr-Latn");

        /// <summary>
        /// 斯洛伐克语。
        /// </summary>
        public static readonly Language Slovak = new Language("sk");

        /// <summary>
        /// 斯洛文尼亚语。
        /// </summary>
        public static readonly Language Slovenian = new Language("sl");

        /// <summary>
        /// 西班牙语。
        /// </summary>
        public static readonly Language Spanish = new Language("es");

        /// <summary>
        /// 瑞典语。
        /// </summary>
        public static readonly Language Swedish = new Language("sv");

        /// <summary>
        /// 泰语。
        /// </summary>
        public static readonly Language Thai = new Language("th");

        /// <summary>
        /// 土耳其语。
        /// </summary>
        public static readonly Language Turkish = new Language("tr");

        /// <summary>
        /// 乌克兰语。
        /// </summary>
        public static readonly Language Ukrainian = new Language("uk");

        /// <summary>
        /// 越南语。
        /// </summary>
        public static readonly Language Vietnamese = new Language("vi");

        /// <summary>
        /// 美国英语。
        /// </summary>
        public static readonly Language EnglishUnitedStates = new Language("en-US");

        /// <summary>
        /// 英国英语。
        /// </summary>
        public static readonly Language EnglishUnitedKingdom = new Language("en-GB");

        /// <summary>
        /// 加拿大法语。
        /// </summary>
        public static readonly Language FrenchCanada = new Language("fr-CA");

        /// <summary>
        /// 拉丁美洲西班牙语。
        /// </summary>
        public static readonly Language SpanishLatinAmerica = new Language("es-419");

        /// <summary>
        /// 创建已经规范化的语言值。
        /// </summary>
        /// <param name="code">已验证的标准语言代码。</param>
        private Language(string code)
        {
            m_Code = code;
        }

        /// <summary>
        /// 获取标准语言代码；未指定时为空字符串。
        /// </summary>
        public string Code
        {
            get
            {
                return m_Code ?? string.Empty;
            }
        }

        /// <summary>
        /// 获取区域父语言；没有父语言时返回 Unspecified。
        /// </summary>
        public Language Parent
        {
            get
            {
                if (this == Unspecified)
                {
                    return Unspecified;
                }
                string code = CultureInfo.GetCultureInfo(Code).Parent.Name;
                return code.Length == 0 ? Unspecified : new Language(code);
            }
        }

        /// <summary>
        /// 从外部配置或项目语言声明创建语言值。
        /// </summary>
        /// <param name="code">CultureInfo 支持的标准代码，如 en-US；不接受空值或下划线。</param>
        /// <returns>已校验并规范化的语言；无效代码抛出异常。</returns>
        public static Language FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.IndexOf('_') >= 0)
            {
                throw new ArgumentException("Use a standard language code such as en-US.", nameof(code));
            }
            string canonical = CultureInfo.GetCultureInfo(code).Name;
            if (canonical.Length == 0)
            {
                throw new ArgumentException("The invariant culture is not a language.", nameof(code));
            }
            return new Language(canonical);
        }

        /// <summary>
        /// 尝试读取外部保存的标准语言代码。
        /// </summary>
        /// <param name="code">要读取的语言代码。</param>
        /// <param name="language">读取成功的语言；失败时为 Unspecified。</param>
        /// <returns>代码是否有效。</returns>
        public static bool TryParse(string code, out Language language)
        {
            try
            {
                language = FromCode(code);
                return true;
            }
            catch (ArgumentException)
            {
                language = Unspecified;
                return false;
            }
        }

        /// <summary>
        /// 判断两个语言值是否相同。
        /// </summary>
        /// <param name="other">要比较的语言。</param>
        /// <returns>标准语言代码是否相同。</returns>
        public bool Equals(Language other)
        {
            return string.Equals(Code, other.Code, StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断对象是否为同一语言值。
        /// </summary>
        /// <param name="obj">要比较的对象。</param>
        /// <returns>对象是否表示同一语言。</returns>
        public override bool Equals(object obj)
        {
            return obj is Language other && Equals(other);
        }

        /// <summary>
        /// 获取按标准代码计算的哈希值。
        /// </summary>
        /// <returns>语言的哈希值。</returns>
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Code);
        }

        /// <summary>
        /// 获取适合保存或显示的标准代码。
        /// </summary>
        /// <returns>语言代码；未指定时为空字符串。</returns>
        public override string ToString()
        {
            return Code;
        }

        /// <summary>
        /// 比较语言值是否相等。
        /// </summary>
        /// <param name="left">左侧语言。</param>
        /// <param name="right">右侧语言。</param>
        /// <returns>两个语言是否相等。</returns>
        public static bool operator ==(Language left, Language right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 比较语言值是否不同。
        /// </summary>
        /// <param name="left">左侧语言。</param>
        /// <param name="right">右侧语言。</param>
        /// <returns>两个语言是否不同。</returns>
        public static bool operator !=(Language left, Language right)
        {
            return !left.Equals(right);
        }
    }
}
