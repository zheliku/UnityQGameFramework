//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityQGameFramework.Core.FileSystem;
using System;
using System.IO;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 安卓文件系统流。
    /// </summary>
    public sealed class AndroidFileSystemStream : FileSystemStream
    {
        private static readonly string SplitFlag = "!/assets/";
        private static readonly int SplitFlagLength = SplitFlag.Length;
        private static readonly AndroidJavaObject s_AssetManager = null;
        private static readonly IntPtr s_InternalReadMethodId = IntPtr.Zero;
        private static readonly jvalue[] s_InternalReadArgs = null;

        private readonly AndroidJavaObject m_FileStream;
        private readonly IntPtr m_FileStreamRawObject;

        /// <summary>
        /// 缓存 Android 资源管理器和流读取接口。
        /// </summary>
        static AndroidFileSystemStream()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            if (unityPlayer == null)
            {
                throw new FrameworkException("Unity player is invalid.");
            }

            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (currentActivity == null)
            {
                throw new FrameworkException("Current activity is invalid.");
            }

            AndroidJavaObject assetManager = currentActivity.Call<AndroidJavaObject>("getAssets");
            if (assetManager == null)
            {
                throw new FrameworkException("Asset manager is invalid.");
            }

            s_AssetManager = assetManager;

            IntPtr inputStreamClassPtr = AndroidJNI.FindClass("java/io/InputStream");
            s_InternalReadMethodId = AndroidJNIHelper.GetMethodID(inputStreamClassPtr, "read", "([BII)I");
            s_InternalReadArgs = new jvalue[3];

            AndroidJNI.DeleteLocalRef(inputStreamClassPtr);
            currentActivity.Dispose();
            unityPlayer.Dispose();
        }

        /// <summary>
        /// 初始化安卓文件系统流的新实例。
        /// </summary>
        /// <param name="fullPath">要加载的文件系统的完整路径。</param>
        /// <param name="access">要加载的文件系统的访问方式。</param>
        /// <param name="createNew">是否创建新的文件系统流。</param>
        public AndroidFileSystemStream(string fullPath, FileSystemAccess access, bool createNew)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new FrameworkException("Full path is invalid.");
            }

            if (access != FileSystemAccess.Read)
            {
                throw new FrameworkException(Utility.Text.Format("'{0}' is not supported in AndroidFileSystemStream.", access));
            }

            if (createNew)
            {
                throw new FrameworkException("Create new is not supported in AndroidFileSystemStream.");
            }

            int position = fullPath.LastIndexOf(SplitFlag, StringComparison.Ordinal);
            if (position < 0)
            {
                throw new FrameworkException("Can not find split flag in full path.");
            }

            string fileName = fullPath.Substring(position + SplitFlagLength);
            m_FileStream = InternalOpen(fileName);
            if (m_FileStream == null)
            {
                throw new FrameworkException(Utility.Text.Format("Open file '{0}' from Android asset manager failure.", fullPath));
            }

            m_FileStreamRawObject = m_FileStream.GetRawObject();
        }

        /// <summary>
        /// 获取或设置文件系统流位置。
        /// </summary>
        protected override long Position
        {
            get
            {
                throw new FrameworkException("Get position is not supported in AndroidFileSystemStream.");
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// 获取文件系统流长度。
        /// </summary>
        protected override long Length
        {
            get
            {
                return InternalAvailable();
            }
        }

        /// <summary>
        /// 设置文件系统流长度。
        /// </summary>
        /// <param name="length">要设置的文件系统流的长度。</param>
        protected override void SetLength(long length)
        {
            throw new FrameworkException("SetLength is not supported in AndroidFileSystemStream.");
        }

        /// <summary>
        /// 定位文件系统流位置。
        /// </summary>
        /// <param name="offset">要定位的文件系统流位置的偏移。</param>
        /// <param name="origin">要定位的文件系统流位置的方式。</param>
        protected override void Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.End)
            {
                Seek(Length + offset, SeekOrigin.Begin);
                return;
            }

            if (origin == SeekOrigin.Begin)
            {
                InternalReset();
            }

            while (offset > 0)
            {
                long skip = InternalSkip(offset);
                if (skip < 0)
                {
                    return;
                }

                offset -= skip;
            }
        }

        /// <summary>
        /// 从文件系统流中读取一个字节。
        /// </summary>
        /// <returns>读取的字节，若已经到达文件结尾，则返回 -1。</returns>
        protected override int ReadByte()
        {
            return InternalRead();
        }

        /// <summary>
        /// 从文件系统流中读取二进制流。
        /// </summary>
        /// <param name="buffer">存储读取文件内容的二进制流。</param>
        /// <param name="startIndex">存储读取文件内容的二进制流的起始位置。</param>
        /// <param name="length">存储读取文件内容的二进制流的长度。</param>
        /// <returns>实际读取了多少字节。</returns>
        protected override int Read(byte[] buffer, int startIndex, int length)
        {
            sbyte[] result = null;
            int bytesRead = InternalRead(length, out result);
            Buffer.BlockCopy(result, 0, buffer, startIndex, bytesRead);
            return bytesRead;
        }

        /// <summary>
        /// 向文件系统流中写入一个字节。
        /// </summary>
        /// <param name="value">要写入的字节。</param>
        protected override void WriteByte(byte value)
        {
            throw new FrameworkException("WriteByte is not supported in AndroidFileSystemStream.");
        }

        /// <summary>
        /// 向文件系统流中写入二进制流。
        /// </summary>
        /// <param name="buffer">存储写入文件内容的二进制流。</param>
        /// <param name="startIndex">存储写入文件内容的二进制流的起始位置。</param>
        /// <param name="length">存储写入文件内容的二进制流的长度。</param>
        protected override void Write(byte[] buffer, int startIndex, int length)
        {
            throw new FrameworkException("Write is not supported in AndroidFileSystemStream.");
        }

        /// <summary>
        /// 将文件系统流立刻更新到存储介质中。
        /// </summary>
        protected override void Flush()
        {
            throw new FrameworkException("Flush is not supported in AndroidFileSystemStream.");
        }

        /// <summary>
        /// 关闭文件系统流。
        /// </summary>
        protected override void Close()
        {
            InternalClose();
            m_FileStream.Dispose();
        }

        /// <summary>
        /// 打开 Android 内置资源流。
        /// </summary>
        /// <param name="fileName">相对内置资源目录的文件名称。</param>
        /// <returns>打开的 Java 输入流。</returns>
        private AndroidJavaObject InternalOpen(string fileName)
        {
            return s_AssetManager.Call<AndroidJavaObject>("open", fileName);
        }

        /// <summary>
        /// 获取当前流无需阻塞即可读取的字节数。
        /// </summary>
        /// <returns>可读取字节数。</returns>
        private int InternalAvailable()
        {
            return m_FileStream.Call<int>("available");
        }

        /// <summary>
        /// 关闭 Android 输入流。
        /// </summary>
        private void InternalClose()
        {
            m_FileStream.Call("close");
        }

        /// <summary>
        /// 从 Android 输入流读取数据。
        /// </summary>
        /// <returns>读取的字节；到达流末尾时返回 -1。</returns>
        private int InternalRead()
        {
            return m_FileStream.Call<int>("read");
        }

        /// <summary>
        /// 从 Android 输入流读取数据。
        /// </summary>
        /// <param name="length">最多读取的字节数。</param>
        /// <param name="result">输出读取缓冲区。</param>
        /// <returns>实际写入输出缓冲区的字节数。</returns>
        private int InternalRead(int length, out sbyte[] result)
        {
            IntPtr resultPtr = AndroidJNI.NewSByteArray(length);
            int offset = 0;
            int bytesLeft = length;
            while (bytesLeft > 0)
            {
                s_InternalReadArgs[0] = new jvalue() { l = resultPtr };
                s_InternalReadArgs[1] = new jvalue() { i = offset };
                s_InternalReadArgs[2] = new jvalue() { i = bytesLeft };
                int bytesRead = AndroidJNI.CallIntMethod(m_FileStreamRawObject, s_InternalReadMethodId, s_InternalReadArgs);
                if (bytesRead <= 0)
                {
                    break;
                }

                offset += bytesRead;
                bytesLeft -= bytesRead;
            }

            result = AndroidJNI.FromSByteArray(resultPtr);
            AndroidJNI.DeleteLocalRef(resultPtr);
            return offset;
        }

        /// <summary>
        /// 将 Android 输入流重置到标记位置。
        /// </summary>
        private void InternalReset()
        {
            m_FileStream.Call("reset");
        }

        /// <summary>
        /// 跳过输入流中的指定字节数。
        /// </summary>
        /// <param name="offset">希望跳过的字节数。</param>
        /// <returns>实际跳过的字节数。</returns>
        private long InternalSkip(long offset)
        {
            return m_FileStream.Call<long>("skip", offset);
        }
    }
}
