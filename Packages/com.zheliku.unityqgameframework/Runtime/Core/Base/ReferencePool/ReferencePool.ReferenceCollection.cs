//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace UnityQGameFramework.Core
{
    public static partial class ReferencePool
    {
        /// <summary>
        /// 初始化指定类型的引用集合。
        /// </summary>
        private sealed class ReferenceCollection
        {
            private readonly Queue<IReference> m_References;

            /// <summary>
            /// 初始化指定类型的引用集合。
            /// </summary>
            /// <param name="referenceType">引用对象类型。</param>
            public ReferenceCollection(Type referenceType)
            {
                m_References = new Queue<IReference>();
                ReferenceType = referenceType;
                UsingReferenceCount = 0;
                AcquireReferenceCount = 0;
                ReleaseReferenceCount = 0;
                AddReferenceCount = 0;
                RemoveReferenceCount = 0;
            }

            /// <summary>
            /// 获取引用对象类型。
            /// </summary>
            public Type ReferenceType
            {
                get;
            }

            /// <summary>
            /// 获取闲置引用数量。
            /// </summary>
            public int UnusedReferenceCount
            {
                get
                {
                    return m_References.Count;
                }
            }

            /// <summary>
            /// 获取正在使用的引用数量。
            /// </summary>
            public int UsingReferenceCount
            {
                get; private set;
            }

            /// <summary>
            /// 获取累计获取引用次数。
            /// </summary>
            public int AcquireReferenceCount
            {
                get; private set;
            }

            /// <summary>
            /// 获取累计归还引用次数。
            /// </summary>
            public int ReleaseReferenceCount
            {
                get; private set;
            }

            /// <summary>
            /// 获取累计创建引用数量。
            /// </summary>
            public int AddReferenceCount
            {
                get; private set;
            }

            /// <summary>
            /// 获取累计移除引用数量。
            /// </summary>
            public int RemoveReferenceCount
            {
                get; private set;
            }

            /// <summary>
            /// 获取引用，集合为空时创建新引用。
            /// </summary>
            /// <typeparam name="T">引用对象类型。</typeparam>
            /// <returns>可使用的引用对象。</returns>
            public T Acquire<T>() where T : class, IReference, new()
            {
                if (typeof(T) != ReferenceType)
                {
                    throw new GameFrameworkException("Type is invalid.");
                }

                UsingReferenceCount++;
                AcquireReferenceCount++;
                lock (m_References)
                {
                    if (m_References.Count > 0)
                    {
                        return (T)m_References.Dequeue();
                    }
                }

                AddReferenceCount++;
                return new T();
            }

            /// <summary>
            /// 获取引用，集合为空时创建新引用。
            /// </summary>
            /// <returns>可使用的引用对象。</returns>
            public IReference Acquire()
            {
                UsingReferenceCount++;
                AcquireReferenceCount++;
                lock (m_References)
                {
                    if (m_References.Count > 0)
                    {
                        return m_References.Dequeue();
                    }
                }

                AddReferenceCount++;
                return (IReference)Activator.CreateInstance(ReferenceType);
            }

            /// <summary>
            /// 清理引用并归还到集合。
            /// </summary>
            /// <param name="reference">要归还的引用对象。</param>
            public void Release(IReference reference)
            {
                reference.Clear();
                lock (m_References)
                {
                    if (EnableStrictCheck && m_References.Contains(reference))
                    {
                        throw new GameFrameworkException("The reference has been released.");
                    }

                    m_References.Enqueue(reference);
                }

                ReleaseReferenceCount++;
                UsingReferenceCount--;
            }

            /// <summary>
            /// 向集合预先添加引用对象。
            /// </summary>
            /// <typeparam name="T">引用对象类型。</typeparam>
            /// <param name="count">要创建的引用数量。</param>
            public void Add<T>(int count) where T : class, IReference, new()
            {
                if (typeof(T) != ReferenceType)
                {
                    throw new GameFrameworkException("Type is invalid.");
                }

                lock (m_References)
                {
                    AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        m_References.Enqueue(new T());
                    }
                }
            }

            /// <summary>
            /// 向集合预先添加引用对象。
            /// </summary>
            /// <param name="count">要创建的引用数量。</param>
            public void Add(int count)
            {
                lock (m_References)
                {
                    AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        m_References.Enqueue((IReference)Activator.CreateInstance(ReferenceType));
                    }
                }
            }

            /// <summary>
            /// 从集合移除闲置引用。
            /// </summary>
            /// <param name="count">要移除的引用数量。</param>
            public void Remove(int count)
            {
                lock (m_References)
                {
                    if (count > m_References.Count)
                    {
                        count = m_References.Count;
                    }

                    RemoveReferenceCount += count;
                    while (count-- > 0)
                    {
                        m_References.Dequeue();
                    }
                }
            }

            /// <summary>
            /// 移除集合中的所有闲置引用。
            /// </summary>
            public void RemoveAll()
            {
                lock (m_References)
                {
                    RemoveReferenceCount += m_References.Count;
                    m_References.Clear();
                }
            }
        }
    }
}
