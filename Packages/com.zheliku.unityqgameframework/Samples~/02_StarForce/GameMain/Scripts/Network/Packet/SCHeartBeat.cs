//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using ProtoBuf;
using System;

namespace StarForce
{
    /// <summary>
    /// 服务端心跳消息。
    /// </summary>
    [Serializable, ProtoContract(Name = @"SCHeartBeat")]
    public class SCHeartBeat : SCPacketBase
    {
        /// <summary>
        /// 服务端心跳消息。
        /// </summary>
        public SCHeartBeat()
        {
        }

        /// <summary>
        /// 获取当前实例或消息的标识。
        /// </summary>
        public override int Id
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// 重置数据，供引用池复用。
        /// </summary>
        public override void Clear()
        {
        }
    }
}
