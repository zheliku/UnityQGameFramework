using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在支持版本的隔离工程复核本地化、资源生命周期和设置可靠性。
/// </summary>
public static class RunModernizationBatch
{
    /// <summary>
    /// 验证 StarForce 四种语言启动后退出隔离编辑器。
    /// </summary>
    public static async void RunStarForce()
    {
        try
        {
            await LocalizationSampleValidation.RunStarForce();
            File.WriteAllText("Artifacts~/starforce-language-final.txt", Application.unityVersion + "\npassed");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            File.WriteAllText("Artifacts~/starforce-language-final.txt", exception.ToString());
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// 执行复核并保存报告，完成后退出批处理编辑器。
    /// </summary>
    public static async void Run()
    {
        var report = new Report { unity = Application.unityVersion };
        try
        {
            if (!Application.dataPath.Contains("Artifacts~"))
            {
                throw new InvalidOperationException("Use an isolated validation project.");
            }
            report.core = LocalizationCoreValidation.Run();
            report.build = LocalizationBuildValidation.Run();
            report.setting = SettingPersistenceValidation.Run();
            await LocalizationIntegrationValidation.Run();
            await System.Threading.Tasks.Task.Delay(300);
            await LocalizationSampleValidation.Run();
            report.passed = true;
        }
        catch (Exception exception)
        {
            report.error = exception.ToString();
        }
        Directory.CreateDirectory("Artifacts~");
        File.WriteAllText("Artifacts~/modernization-final.json", JsonUtility.ToJson(report, true));
        EditorApplication.Exit(report.passed ? 0 : 1);
    }

    /// <summary>
    /// 保存本次复核实际执行的检查和失败原因。
    /// </summary>
    [Serializable]
    private sealed class Report
    {
        /// <summary>
        /// 是否通过全部检查。
        /// </summary>
        public bool passed;

        /// <summary>
        /// 实际 Unity 版本。
        /// </summary>
        public string unity;

        /// <summary>
        /// Core 检查。
        /// </summary>
        public string[] core;

        /// <summary>
        /// 构建检查。
        /// </summary>
        public string[] build;

        /// <summary>
        /// 设置持久化检查。
        /// </summary>
        public string[] setting;

        /// <summary>
        /// 失败原因。
        /// </summary>
        public string error;
    }
}
