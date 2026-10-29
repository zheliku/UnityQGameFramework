using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityQGameFramework.Core;

/// <summary>
/// 检查资源管理器的后端所有权与关闭边界。
/// </summary>
public static class ResourceModernizationValidation
{
    /// <summary>
    /// 在不创建 Unity 对象或修改场景的情况下执行资源契约检查。
    /// </summary>
    /// <returns>检查结果与已验证的行为。</returns>
    public static object Run()
    {
        var checks = new List<string>();
        Assembly core = typeof(IResourceManager).Assembly;
        Require(!core.GetReferencedAssemblies().Any(assembly => assembly.Name.StartsWith("UnityEngine", StringComparison.Ordinal)),
            "The Core resource contract remains independent of UnityEngine.");
        checks.Add("Core remains engine-independent");
        string[] removedTypes =
        {
            "ResourceMode", "IResourceBackendHost", "PackageVersionListSerializer", "UpdatableVersionListSerializer",
            "IResourceHelper", "ILoadResourceAgentHelper", "IResourceGroup", "IResourceGroupCollection"
        };
        foreach (string name in removedTypes)
        {
            Require(core.GetType("UnityQGameFramework.Core." + name) == null, "A legacy resource type remains: " + name);
        }
        checks.Add("Legacy resource modes, containers, helpers and patch contracts are removed");

        Type managerType = core.GetType("UnityQGameFramework.Core.ResourceManager", true);
        var manager = (IResourceManager)Activator.CreateInstance(managerType);
        int resourcePriority = (int)managerType.GetProperty("Priority", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
        Type poolType = core.GetType("UnityQGameFramework.Core.ObjectPoolManager", true);
        int poolPriority = (int)poolType.GetProperty("Priority", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Activator.CreateInstance(poolType));
        Require(resourcePriority > poolPriority, "The resource backend must remain available while pooled entities and UI return their assets.");
        checks.Add("Pooled objects return their resource borrows before the backend shuts down");
        var backend = new OwnedBackend();
        RequireRejected(() => manager.HasAsset("Cube"));
        RequireRejected(() => manager.SetResourceBackend(null));
        manager.SetResourceBackend(backend);
        RequireRejected(() => manager.SetResourceBackend(new OwnedBackend()));
        checks.Add("An unset, null or replaced backend is rejected");

        object target = null;
        var callbacks = new LoadAssetCallbacks((name, asset, duration, userData) => target = asset);
        manager.LoadAsset("Cube", callbacks);
        manager.LoadAsset("Cube", callbacks);
        manager.UnloadAsset(target);
        Require(backend.Borrows == 1, "Returning one borrow must keep an independent borrow alive.");
        manager.UnloadAsset(target);
        RequireRejected(() => manager.UnloadAsset(target));
        checks.Add("Independent borrows are returned once and cannot be returned twice");

        MethodInfo update = managerType.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo shutdown = managerType.GetMethod("Shutdown", BindingFlags.NonPublic | BindingFlags.Instance);
        update.Invoke(manager, new object[] { 0.25f, 0.5f });
        Require(backend.Updates == 1, "The manager must update its configured backend.");
        shutdown.Invoke(manager, null);
        shutdown.Invoke(manager, null);
        update.Invoke(manager, new object[] { 0.25f, 0.5f });
        Require(backend.Shutdowns == 1 && backend.Updates == 1, "Shutdown must run once and prevent later updates.");
        RequireRejected(() => manager.HasAsset("Cube"));
        RequireRejected(() => manager.LoadAsset("Cube", callbacks));
        RequireRejected(() => manager.SetResourceBackend(new OwnedBackend()));
        checks.Add("Shutdown is idempotent and a stale manager cannot restart a resource service");
        return new
        {
            passed = true,
            checks = checks.ToArray()
        };
    }

    /// <summary>
    /// 检查指定条件。
    /// </summary>
    /// <param name="condition">必须成立的条件。</param>
    /// <param name="message">条件不成立时的错误信息。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 检查操作是否被资源契约拒绝。
    /// </summary>
    /// <param name="action">应当抛出框架异常的操作。</param>
    private static void RequireRejected(Action action)
    {
        try
        {
            action();
        }
        catch (GameFrameworkException)
        {
            return;
        }
        throw new InvalidOperationException("The resource operation should have been rejected.");
    }

    /// <summary>
    /// 提供可独立计数的纯 C# 资源借用，用于检查管理器生命周期。
    /// </summary>
    private sealed class OwnedBackend : IResourceBackend
    {
        private readonly object m_Target = new object();

        /// <summary>
        /// 获取尚未归还的借用数量。
        /// </summary>
        internal int Borrows
        {
            get; private set;
        }

        /// <summary>
        /// 获取已执行的轮询次数。
        /// </summary>
        internal int Updates
        {
            get; private set;
        }

        /// <summary>
        /// 获取已执行的关闭次数。
        /// </summary>
        internal int Shutdowns
        {
            get; private set;
        }

        /// <summary>
        /// 获取本检查不使用的只读目录。
        /// </summary>
        public string ReadOnlyPath { get; } = string.Empty;

        /// <summary>
        /// 获取本检查不使用的缓存目录。
        /// </summary>
        public string ReadWritePath { get; } = string.Empty;

        /// <summary>
        /// 获取检查资源的数量。
        /// </summary>
        public int AssetCount { get; } = 1;

        /// <summary>
        /// 获取即时完成后剩余的请求数量。
        /// </summary>
        public int LoadingCount { get; } = 0;

        /// <summary>
        /// 检查唯一的测试资产。
        /// </summary>
        /// <param name="assetName">资产地址。</param>
        /// <returns>地址是否存在。</returns>
        public HasAssetResult HasAsset(string assetName)
        {
            return assetName == "Cube" ? HasAssetResult.Exists : HasAssetResult.NotExist;
        }

        /// <summary>
        /// 建立一次独立借用并立即通知完成。
        /// </summary>
        /// <param name="assetName">资产地址。</param>
        /// <param name="assetType">本检查不使用的资产类型。</param>
        /// <param name="priority">本检查不使用的加载优先级。</param>
        /// <param name="callbacks">接收借用的加载回调。</param>
        /// <param name="userData">原样传回的用户数据。</param>
        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks callbacks, object userData)
        {
            Borrows++;
            callbacks.LoadAssetSuccessCallback(assetName, m_Target, 0f, userData);
        }

        /// <summary>
        /// 归还一次借用。
        /// </summary>
        /// <param name="asset">本后端借出的资产。</param>
        public void UnloadAsset(object asset)
        {
            if (!ReferenceEquals(asset, m_Target) || Borrows == 0)
            {
                throw new GameFrameworkException("The asset has no outstanding borrow.");
            }
            Borrows--;
        }

        /// <summary>
        /// 拒绝本检查没有实现的场景加载。
        /// </summary>
        /// <param name="sceneAssetName">场景地址。</param>
        /// <param name="priority">加载优先级。</param>
        /// <param name="callbacks">加载回调。</param>
        /// <param name="userData">用户数据。</param>
        public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 拒绝本检查没有实现的场景卸载。
        /// </summary>
        /// <param name="sceneAssetName">场景地址。</param>
        /// <param name="callbacks">卸载回调。</param>
        /// <param name="userData">用户数据。</param>
        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 拒绝本检查没有实现的文件读取。
        /// </summary>
        /// <param name="assetName">文件地址。</param>
        /// <param name="callbacks">读取回调。</param>
        /// <param name="userData">用户数据。</param>
        public void LoadBinary(string assetName, LoadBinaryCallbacks callbacks, object userData)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 记录资源后端的轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间。</param>
        /// <param name="realElapseSeconds">真实流逝时间。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            Updates++;
        }

        /// <summary>
        /// 记录关闭并释放本后端尚未归还的借用。
        /// </summary>
        public void Shutdown()
        {
            Shutdowns++;
            Borrows = 0;
        }
    }
}
