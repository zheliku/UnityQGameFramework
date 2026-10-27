//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using ProcedureOwner = UnityQGameFramework.Core.Fsm.IFsm<UnityQGameFramework.Core.Procedure.IProcedureManager>;

namespace StarForce
{
    /// <summary>
    /// 启动画面流程。
    /// </summary>
    public class ProcedureSplash : ProcedureBase
    {
        /// <summary>
        /// 更新当前流程。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            // 模拟、离线与联机模式均先初始化资源包，不再由业务判断旧资源模式。
            ChangeState<ProcedureInitResources>(procedureOwner);
        }
    }
}
