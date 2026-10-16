//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Net.Sockets;

namespace UnityQGameFramework.Core.Network
{
    internal sealed partial class NetworkManager : FrameworkModule, INetworkManager
    {
        private sealed class ConnectState
        {
            public ConnectState(Socket socket, object userData)
            {
                Socket = socket;
                UserData = userData;
            }

            public Socket Socket { get; }

            public object UserData { get; }
        }
    }
}
