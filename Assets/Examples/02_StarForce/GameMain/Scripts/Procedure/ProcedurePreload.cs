//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Resource;
using UnityEngine;
using UnityQGameFramework.Runtime;
using ProcedureOwner = UnityQGameFramework.Core.Fsm.IFsm<UnityQGameFramework.Core.Procedure.IProcedureManager>;

namespace StarForce
{
    /// <summary>
    /// 预加载配置、数据表和本地化字典。
    /// </summary>
    public class ProcedurePreload : ProcedureBase
    {
        private const string DataTableCatalogAddress = "423d814ae05f8594785f98c1e967ebac";

        private readonly Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();
        // 同轮并行加载失败时合并重试，避免连续弹出多个对话框。
        private readonly Dictionary<string, Action> m_RetryActions = new Dictionary<string, Action>();
        private bool m_FailureDialogOpen;
        private CancellationTokenSource m_Cancellation;

        /// <summary>
        /// 进入状态时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            // 先订阅结果，再发起读取；完成事件中的 UserData 用来识别本流程的请求。
            GameEntry.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);

            m_LoadedFlag.Clear();
            m_RetryActions.Clear();
            m_FailureDialogOpen = false;
            m_Cancellation = new CancellationTokenSource();

            PreloadResources();
        }

        /// <summary>
        /// 离开状态时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        /// <param name="isShutdown">是否是关闭状态机时触发。</param>
        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);
            m_RetryActions.Clear();
            if (m_Cancellation != null)
            {
                if (!m_Cancellation.IsCancellationRequested)
                {
                    m_Cancellation.Cancel();
                }
                m_Cancellation.Dispose();
                m_Cancellation = null;
            }

            base.OnLeave(procedureOwner, isShutdown);
        }

        /// <summary>
        /// 更新当前流程。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            foreach (KeyValuePair<string, bool> loadedFlag in m_LoadedFlag)
            {
                if (!loadedFlag.Value)
                {
                    return;
                }
            }

            procedureOwner.SetData<VarInt32>("NextSceneId", GameEntry.Config.GetInt("Scene.Menu"));
            ChangeState<ProcedureChangeScene>(procedureOwner);
        }

        private void PreloadResources()
        {
            // Config 读取游戏参数 TOML，成功后可用 GetInt 等接口按键取值。
            LoadConfig("DefaultConfig");

            // Catalog 引用生成的 bytes；Tables 负责反序列化并登记生成表。
            // Catalog 走资源包加载，解析完成后归还资产，再通知表集已就绪。
            LoadDataTableAsync().Forget();

            // 字典按当前 Language 选择，与游戏参数分开加载。
            LoadDictionary("Default");

            // 字体加载完成后交给 UGuiForm，之后创建的界面会使用它。
            LoadFontAsync("MainFont").Forget();
        }

        private void LoadConfig(string configName)
        {
            string configAssetName = AssetUtility.GetConfigAsset(configName);
            m_LoadedFlag.Add(configAssetName, false);
            GameEntry.Config.ReadData(configAssetName, this);
        }

        private async UniTask LoadDataTableAsync()
        {
            m_LoadedFlag["StarForce.Tables"] = false;
            CancellationToken cancellationToken = m_Cancellation.Token;
            try
            {
                // 任务成功表示整个 Catalog 已解析并登记；取消不会登记迟到的表集。
                await GameEntry.DataTable.LoadDataTablesAsync<Tables>(
                    DataTableCatalogAddress,
                    this,
                    cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                m_LoadedFlag["StarForce.Tables"] = true;
                m_RetryActions.Remove("StarForce.Tables");
                Log.Info("Load StarForce data tables OK.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Log.Error("Can not load StarForce data tables with error message '{0}'.", exception.Message);
                ShowLoadFailure("StarForce.Tables", exception.Message, () => LoadDataTableAsync().Forget());
            }
        }

        private void LoadDictionary(string dictionaryName)
        {
            string dictionaryAssetName = AssetUtility.GetDictionaryAsset(dictionaryName);
            m_LoadedFlag.Add(dictionaryAssetName, false);
            GameEntry.Localization.ReadData(dictionaryAssetName, this);
        }

        private async UniTask LoadFontAsync(string fontName)
        {
            string loadKey = Utility.Text.Format("Font.{0}", fontName);
            m_LoadedFlag[loadKey] = false;
            CancellationToken cancellationToken = m_Cancellation.Token;
            Font font = null;
            try
            {
                font = await GameEntry.Resource.LoadAssetAsync<Font>(
                    AssetUtility.GetFontAsset(fontName),
                    priority: Constant.AssetPriority.FontAsset,
                    cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                // 主字体供后续界面共用，借用保留到框架关闭；交接前失败则在 finally 归还。
                UGuiForm.SetMainFont(font);
                font = null;
                m_LoadedFlag[loadKey] = true;
                m_RetryActions.Remove(loadKey);
                Log.Info("Load font '{0}' OK.", fontName);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Log.Error("Can not load font '{0}' with error message '{1}'.", fontName, exception.Message);
                ShowLoadFailure(loadKey, exception.Message, () => LoadFontAsync(fontName).Forget());
            }
            finally
            {
                if (font != null)
                {
                    GameEntry.Resource.UnloadAsset(font);
                }
            }
        }

        private void OnLoadConfigSuccess(object sender, GameEventArgs e)
        {
            LoadConfigSuccessEventArgs ne = (LoadConfigSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            m_LoadedFlag[ne.ConfigAssetName] = true;
            m_RetryActions.Remove(ne.ConfigAssetName);
            Log.Info("Load config '{0}' OK.", ne.ConfigAssetName);
        }

        private void OnLoadConfigFailure(object sender, GameEventArgs e)
        {
            LoadConfigFailureEventArgs ne = (LoadConfigFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            string configAssetName = ne.ConfigAssetName;
            string errorMessage = ne.ErrorMessage;
            Log.Error("Can not load config '{0}' with error message '{1}'.", configAssetName, errorMessage);
            ShowLoadFailure(configAssetName, errorMessage, () => GameEntry.Config.ReadData(configAssetName, this));
        }

        private void OnLoadDictionarySuccess(object sender, GameEventArgs e)
        {
            LoadDictionarySuccessEventArgs ne = (LoadDictionarySuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            m_LoadedFlag[ne.DictionaryAssetName] = true;
            m_RetryActions.Remove(ne.DictionaryAssetName);
            Log.Info("Load dictionary '{0}' OK.", ne.DictionaryAssetName);
        }

        private void OnLoadDictionaryFailure(object sender, GameEventArgs e)
        {
            LoadDictionaryFailureEventArgs ne = (LoadDictionaryFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            string dictionaryAssetName = ne.DictionaryAssetName;
            string errorMessage = ne.ErrorMessage;
            Log.Error("Can not load dictionary '{0}' with error message '{1}'.", dictionaryAssetName, errorMessage);
            ShowLoadFailure(dictionaryAssetName, errorMessage, () => GameEntry.Localization.ReadData(dictionaryAssetName, this));
        }

        private void ShowLoadFailure(string loadKey, string errorMessage, Action retryAction)
        {
            m_RetryActions[loadKey] = retryAction;
            if (m_FailureDialogOpen)
            {
                return;
            }

            m_FailureDialogOpen = true;
            // 预载失败时不再尝试加载业务 Dialog，避免提示窗口依赖同一批故障资源。
            GameEntry.BuiltinData.OpenDialog(new DialogParams
            {
                Mode = 2,
                Title = "资源加载失败",
                Message = Utility.Text.Format("{0}\n{1}", loadKey, errorMessage),
                ConfirmText = "重试",
                OnClickConfirm = userData => RetryFailedLoads(),
                CancelText = "退出",
                OnClickCancel = userData => UnityQGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit)
            });
        }

        private void RetryFailedLoads()
        {
            m_FailureDialogOpen = false;
            List<KeyValuePair<string, Action>> retryActions = new List<KeyValuePair<string, Action>>(m_RetryActions);
            m_RetryActions.Clear();

            foreach (KeyValuePair<string, Action> retryAction in retryActions)
            {
                m_LoadedFlag[retryAction.Key] = false;
                retryAction.Value();
            }
        }
    }
}
