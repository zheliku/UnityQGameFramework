//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Net.Sockets;

namespace UnityQGameFramework.Core.Network
{
    internal sealed partial class NetworkManager : FrameworkModule, INetworkManager
    {
        /// <summary>
        /// 保存网络连接中的异步状态。
        /// </summary>
        private sealed class ConnectState
        {
            /// <summary>
            /// 初始化网络连接状态。
            /// </summary>
            /// <param name="socket">发起连接的套接字。</param>
            /// <param name="userData">用户自定义数据。</param>
            public ConnectState(Socket socket, object userData)
            {
                Socket = socket;
                UserData = userData;
            }

            /// <summary>
            /// 获取发起连接的套接字。
            /// </summary>
            public Socket Socket { get; }

            /// <summary>
            /// 获取用户自定义数据。
            /// </summary>
            public object UserData { get; }
        }
    }
}
