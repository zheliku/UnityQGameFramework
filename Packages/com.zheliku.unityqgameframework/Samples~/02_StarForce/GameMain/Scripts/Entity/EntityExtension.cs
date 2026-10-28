//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 根据配置表显示和操作游戏实体。
    /// </summary>
    public static class EntityExtension
    {
        // 关于 EntityId 的约定：
        // 0 为无效
        // 正值用于和服务器通信的实体（如玩家角色、NPC、怪等，服务器只产生正值）
        // 负值用于本地生成的临时实体（如特效、FakeObject等）
        private static int s_SerialId = 0;

        /// <summary>
        /// 取得框架实体对应的游戏逻辑。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <param name="entityId">实体实例编号。</param>
        /// <returns>游戏实体逻辑，不存在时返回空。</returns>
        public static Entity GetGameEntity(this EntityComponent entityComponent, int entityId)
        {
            UnityQGameFramework.Runtime.Entity entity = entityComponent.GetEntity(entityId);
            if (entity == null)
            {
                return null;
            }

            return (Entity)entity.Logic;
        }

        /// <summary>
        /// 隐藏游戏实体。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <param name="entity">要操作的实体。</param>
        public static void HideEntity(this EntityComponent entityComponent, Entity entity)
        {
            entityComponent.HideEntity(entity.Entity);
        }

        /// <summary>
        /// 将附属实体挂到指定所属实体上。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <param name="entity">要操作的实体。</param>
        /// <param name="ownerId">所属实体编号。</param>
        /// <param name="parentTransformPath">附加节点相对路径，留空时使用根节点。</param>
        /// <param name="userData">业务自定义数据。</param>
        public static void AttachEntity(
            this EntityComponent entityComponent,
            Entity entity,
            int ownerId,
            string parentTransformPath = null,
            object userData = null)
        {
            entityComponent.AttachEntity(entity.Entity, ownerId, parentTransformPath, userData);
        }

        /// <summary>
        /// 显示玩家飞机实体。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <param name="data">实体初始数据。</param>
        public static void ShowMyAircraft(this EntityComponent entityComponent, MyAircraftData data)
        {
            entityComponent.ShowEntity(typeof(MyAircraft), "Aircraft", Constant.AssetPriority.MyAircraftAsset, data);
        }

        /// <summary>
        /// 显示飞机实体。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <param name="data">实体初始数据。</param>
        public static void ShowAircraft(this EntityComponent entityComponent, AircraftData data)
        {
            entityComponent.ShowEntity(typeof(Aircraft), "Aircraft", Constant.AssetPriority.AircraftAsset, data);
        }

        /// <summary>
        /// 显示推进器实体。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <param name="data">实体初始数据。</param>
        public static void ShowThruster(this EntityComponent entityComponent, ThrusterData data)
        {
            entityComponent.ShowEntity(typeof(Thruster), "Thruster", Constant.AssetPriority.ThrusterAsset, data);
        }

        /// <summary>
        /// 显示武器实体。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <param name="data">实体初始数据。</param>
        public static void ShowWeapon(this EntityComponent entityComponent, WeaponData data)
        {
            entityComponent.ShowEntity(typeof(Weapon), "Weapon", Constant.AssetPriority.WeaponAsset, data);
        }

        /// <summary>
        /// 显示装甲实体。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <param name="data">实体初始数据。</param>
        public static void ShowArmor(this EntityComponent entityComponent, ArmorData data)
        {
            entityComponent.ShowEntity(typeof(Armor), "Armor", Constant.AssetPriority.ArmorAsset, data);
        }

        /// <summary>
        /// 显示子弹实体。
        /// </summary>
        /// <param name="entityCompoennt">资源所属的实体组件。</param>
        /// <param name="data">实体初始数据。</param>
        public static void ShowBullet(this EntityComponent entityCompoennt, BulletData data)
        {
            entityCompoennt.ShowEntity(typeof(Bullet), "Bullet", Constant.AssetPriority.BulletAsset, data);
        }

        /// <summary>
        /// 显示小行星实体。
        /// </summary>
        /// <param name="entityCompoennt">实体组件。</param>
        /// <param name="data">小行星初始数据。</param>
        public static void ShowAsteroid(this EntityComponent entityCompoennt, AsteroidData data)
        {
            entityCompoennt.ShowEntity(typeof(Asteroid), "Asteroid", Constant.AssetPriority.AsteroiAsset, data);
        }

        /// <summary>
        /// 显示特效实体。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <param name="data">实体初始数据。</param>
        public static void ShowEffect(this EntityComponent entityComponent, EffectData data)
        {
            entityComponent.ShowEntity(typeof(Effect), "Effect", Constant.AssetPriority.EffectAsset, data);
        }

        private static void ShowEntity(
            this EntityComponent entityComponent,
            Type logicType,
            string entityGroup,
            int priority,
            EntityData data)
        {
            if (data == null)
            {
                Log.Warning("Data is invalid.");
                return;
            }

            var dtEntity = GameEntry.DataTable.GetDataTable<TbEntity>();
            DREntity drEntity = dtEntity.GetDataRow(data.TypeId);
            if (drEntity == null)
            {
                Log.Warning("Can not load entity id '{0}' from data table.", data.TypeId.ToString());
                return;
            }

            entityComponent.ShowEntity(data.Id, logicType, AssetUtility.GetEntityAsset(drEntity.AssetName), entityGroup, priority, data);
        }

        /// <summary>
        /// 生成本地临时实体编号。
        /// </summary>
        /// <param name="entityComponent">资源所属的实体组件。</param>
        /// <returns>递减的负数实例编号。</returns>
        public static int GenerateSerialId(this EntityComponent entityComponent)
        {
            return --s_SerialId;
        }
    }
}
