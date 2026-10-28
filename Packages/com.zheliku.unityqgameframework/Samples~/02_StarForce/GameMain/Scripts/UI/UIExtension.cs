//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 按配置表打开界面并提供 UGUI 动画。
    /// </summary>
    public static class UIExtension
    {
        /// <summary>
        /// 按真实时间改变界面透明度。
        /// </summary>
        /// <param name="canvasGroup">需要改变透明度的界面组。</param>
        /// <param name="alpha">目标透明度。</param>
        /// <param name="duration">动画时长，以秒为单位。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>透明度动画任务。</returns>
        public static async UniTask FadeToAlpha(this CanvasGroup canvasGroup, float alpha, float duration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float time = 0f;
            float originalAlpha = canvasGroup.alpha;
            while (time < duration)
            {
                // 界面淡入淡出使用真实时间，暂停游戏时仍能关闭对话框。
                time += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(originalAlpha, alpha, time / duration);
                await UniTask.NextFrame(cancellationToken);
            }

            canvasGroup.alpha = alpha;
        }

        /// <summary>
        /// 按真实时间平滑改变滑动条数值。
        /// </summary>
        /// <param name="slider">需要改变显示值的滑动条。</param>
        /// <param name="value">目标值。</param>
        /// <param name="duration">动画时长，以秒为单位。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>数值动画任务。</returns>
        public static async UniTask SmoothValue(this Slider slider, float value, float duration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float time = 0f;
            float originalValue = slider.value;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                slider.value = Mathf.Lerp(originalValue, value, time / duration);
                await UniTask.NextFrame(cancellationToken);
            }

            slider.value = value;
        }

        /// <summary>
        /// 根据配置编号检查界面是否已打开。
        /// </summary>
        /// <param name="uiComponent">界面组件。</param>
        /// <param name="uiFormId">界面配置编号。</param>
        /// <param name="uiGroupName">界面组名称，为空时查询全部组。</param>
        /// <returns>存在相应界面时返回真，否则返回假。</returns>
        public static bool HasUIForm(this UIComponent uiComponent, UIFormId uiFormId, string uiGroupName = null)
        {
            return uiComponent.HasUIForm((int)uiFormId, uiGroupName);
        }

        /// <summary>
        /// 根据配置编号检查界面是否已打开。
        /// </summary>
        /// <param name="uiComponent">界面组件。</param>
        /// <param name="uiFormId">界面配置编号。</param>
        /// <param name="uiGroupName">界面组名称，为空时查询全部组。</param>
        /// <returns>存在相应界面时返回真，否则返回假。</returns>
        public static bool HasUIForm(this UIComponent uiComponent, int uiFormId, string uiGroupName = null)
        {
            var dtUIForm = GameEntry.DataTable.GetDataTable<TbUIForm>();
            DRUIForm drUIForm = dtUIForm.GetDataRow(uiFormId);
            if (drUIForm == null)
            {
                return false;
            }

            string assetName = AssetUtility.GetUIFormAsset(drUIForm.AssetName);
            if (string.IsNullOrEmpty(uiGroupName))
            {
                return uiComponent.HasUIForm(assetName);
            }

            IUIGroup uiGroup = uiComponent.GetUIGroup(uiGroupName);
            if (uiGroup == null)
            {
                return false;
            }

            return uiGroup.HasUIForm(assetName);
        }

        /// <summary>
        /// 根据配置编号取得已打开的界面。
        /// </summary>
        /// <param name="uiComponent">界面组件。</param>
        /// <param name="uiFormId">界面配置编号。</param>
        /// <param name="uiGroupName">界面组名称，为空时查询全部组。</param>
        /// <returns>对应游戏界面，未打开或配置不存在时返回空。</returns>
        public static UGuiForm GetUIForm(this UIComponent uiComponent, UIFormId uiFormId, string uiGroupName = null)
        {
            return uiComponent.GetUIForm((int)uiFormId, uiGroupName);
        }

        /// <summary>
        /// 根据配置编号取得已打开的界面。
        /// </summary>
        /// <param name="uiComponent">界面组件。</param>
        /// <param name="uiFormId">界面配置编号。</param>
        /// <param name="uiGroupName">界面组名称，为空时查询全部组。</param>
        /// <returns>对应游戏界面，未打开或配置不存在时返回空。</returns>
        public static UGuiForm GetUIForm(this UIComponent uiComponent, int uiFormId, string uiGroupName = null)
        {
            var dtUIForm = GameEntry.DataTable.GetDataTable<TbUIForm>();
            DRUIForm drUIForm = dtUIForm.GetDataRow(uiFormId);
            if (drUIForm == null)
            {
                return null;
            }

            string assetName = AssetUtility.GetUIFormAsset(drUIForm.AssetName);
            UIForm uiForm = null;
            if (string.IsNullOrEmpty(uiGroupName))
            {
                uiForm = uiComponent.GetUIForm(assetName);
                if (uiForm == null)
                {
                    return null;
                }

                return (UGuiForm)uiForm.Logic;
            }

            IUIGroup uiGroup = uiComponent.GetUIGroup(uiGroupName);
            if (uiGroup == null)
            {
                return null;
            }

            uiForm = (UIForm)uiGroup.GetUIForm(assetName);
            if (uiForm == null)
            {
                return null;
            }

            return (UGuiForm)uiForm.Logic;
        }

        /// <summary>
        /// 关闭指定游戏界面。
        /// </summary>
        /// <param name="uiComponent">界面组件。</param>
        /// <param name="uiForm">要关闭的游戏界面。</param>
        public static void CloseUIForm(this UIComponent uiComponent, UGuiForm uiForm)
        {
            uiComponent.CloseUIForm(uiForm.UIForm);
        }

        /// <summary>
        /// 按配置表打开界面。
        /// </summary>
        /// <param name="uiComponent">界面组件。</param>
        /// <param name="uiFormId">界面配置编号。</param>
        /// <param name="userData">业务自定义数据。</param>
        /// <returns>打开请求序号，配置无效或不允许重复打开时返回空。</returns>
        public static int? OpenUIForm(this UIComponent uiComponent, UIFormId uiFormId, object userData = null)
        {
            return uiComponent.OpenUIForm((int)uiFormId, userData);
        }

        /// <summary>
        /// 按配置表打开界面。
        /// </summary>
        /// <param name="uiComponent">界面组件。</param>
        /// <param name="uiFormId">界面配置编号。</param>
        /// <param name="userData">业务自定义数据。</param>
        /// <returns>打开请求序号，配置无效或不允许重复打开时返回空。</returns>
        public static int? OpenUIForm(this UIComponent uiComponent, int uiFormId, object userData = null)
        {
            var dtUIForm = GameEntry.DataTable.GetDataTable<TbUIForm>();
            DRUIForm drUIForm = dtUIForm.GetDataRow(uiFormId);
            if (drUIForm == null)
            {
                Log.Warning("Can not load UI form '{0}' from data table.", uiFormId.ToString());
                return null;
            }

            string assetName = AssetUtility.GetUIFormAsset(drUIForm.AssetName);
            if (!drUIForm.AllowMultiInstance)
            {
                if (uiComponent.IsLoadingUIForm(assetName))
                {
                    return null;
                }

                if (uiComponent.HasUIForm(assetName))
                {
                    return null;
                }
            }

            return uiComponent.OpenUIForm(assetName, drUIForm.UIGroupName, Constant.AssetPriority.UIFormAsset, drUIForm.PauseCoveredUIForm, userData);
        }

        /// <summary>
        /// 显示指定内容和按钮回调的对话框。
        /// </summary>
        /// <param name="uiComponent">界面组件。</param>
        /// <param name="dialogParams">对话框内容与回调参数。</param>
        public static void OpenDialog(this UIComponent uiComponent, DialogParams dialogParams)
        {
            // 更新前还没有 UI 表，用随场景发布的窗口；预加载后交给 UI 模块。
            if (GameEntry.DataTable.HasDataTable<TbUIForm>())
            {
                uiComponent.OpenUIForm(UIFormId.DialogForm, dialogParams);
            }
            else
            {
                GameEntry.BuiltinData.OpenDialog(dialogParams);
            }
        }
    }
}
