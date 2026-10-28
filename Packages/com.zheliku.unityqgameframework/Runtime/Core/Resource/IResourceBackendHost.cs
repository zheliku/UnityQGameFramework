namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 可接入替代资源后端的资源管理器。
    /// </summary>
    public interface IResourceBackendHost
    {
        /// <summary>
        /// 设置资源后端。
        /// </summary>
        /// <param name="resourceBackend">资源后端。</param>
        void SetResourceBackend(IResourceBackend resourceBackend);
    }
}
