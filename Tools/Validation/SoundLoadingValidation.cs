using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Core.Sound;

// 定点注入资源完成回调，检查取消与失败分支的等待状态和引用池所有权。
public static class SoundLoadingValidation
{
    private sealed class GroupHelper : ISoundGroupHelper { }

    public static object Run()
    {
        if (Application.isPlaying || !Application.dataPath.Contains("Artifacts~"))
            throw new Exception("Use the isolated editor outside Play Mode.");
        var type = typeof(ISoundManager).Assembly.GetType("UnityQGameFramework.Core.Sound.SoundManager", true);
        var infoType = type.GetNestedType("PlaySoundInfo", BindingFlags.NonPublic);
        var create = infoType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        var failure = type.GetMethod("LoadAssetFailureCallback", BindingFlags.Instance | BindingFlags.NonPublic);
        int checks = 0;
        foreach (bool cancelled in new[] { false, true })
        foreach (bool handled in new[] { false, true })
        {
            var manager = (ISoundManager)Activator.CreateInstance(type, true);
            manager.AddSoundGroup("Test", false, false, 1, new GroupHelper());
            var loading = (List<int>)type.GetField("m_SoundsBeingLoaded", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            int infoBefore = Using(infoType), paramsBefore = Using(typeof(PlaySoundParams));
            var parameters = PlaySoundParams.Create();
            var info = create.Invoke(null, new object[] { 1, manager.GetSoundGroup("Test"), parameters, null });
            loading.Add(1);
            bool notified = false;
            if (handled) manager.PlaySoundFailure += (sender, args) => notified = true;
            if (cancelled)
            {
                manager.StopAllLoadingSounds();
                Require(manager.GetAllLoadingSoundSerialIds().Length == 0 && !manager.IsLoadingSound(1), "Cancellation clears queries");
                checks++;
            }
            bool threw = false;
            try { failure.Invoke(manager, new object[] { "Assets/Missing.wav", LoadResourceStatus.NotExist, "Test failure", info }); }
            catch (TargetInvocationException exception) when (exception.InnerException is FrameworkException) { threw = true; }
            Require(threw == (!cancelled && !handled), "Only an unhandled failure throws");
            Require(notified == (!cancelled && handled), "Cancelled failures do not notify");
            Require(!manager.IsLoadingSound(1), "Failure leaves no pending request");
            Require(Using(infoType) == infoBefore && Using(typeof(PlaySoundParams)) == paramsBefore, "Failure returns context and parameters exactly once");
            checks += 4;
        }
        return new { passed = true, cases = 4, assertions = checks };
    }

    private static int Using(Type type) => ReferencePool.GetAllReferencePoolInfos()
        .Where(info => info.Type == type).Select(info => info.UsingReferenceCount).DefaultIfEmpty().Single();
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
