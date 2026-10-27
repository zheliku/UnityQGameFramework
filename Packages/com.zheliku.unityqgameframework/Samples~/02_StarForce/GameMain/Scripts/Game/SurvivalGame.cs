//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 持续生成小行星的生存玩法。
    /// </summary>
    public class SurvivalGame : GameBase
    {
        private float m_ElapseSeconds = 0f;

        /// <summary>
        /// 获取当前游戏模式。
        /// </summary>
        public override GameMode GameMode
        {
            get
            {
                return GameMode.Survival;
            }
        }

        /// <summary>
        /// 初始化下载代理。
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            // ProcedureMain 会复用 Game 实例，每局重置出怪计时。
            m_ElapseSeconds = 0f;
        }

        /// <summary>
        /// 所有游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            base.Update(elapseSeconds, realElapseSeconds);
            if (GameOver)
            {
                return;
            }

            m_ElapseSeconds += elapseSeconds;
            if (m_ElapseSeconds >= 1f)
            {
                m_ElapseSeconds = 0f;
                // 使用 Luban 的实际记录选类型；Id 可以不连续，也不必从 60000 开始。
                var asteroids = GameEntry.DataTable.GetDataTable<TbAsteroid>().GetAllDataRows();
                if (asteroids.Length == 0)
                {
                    return;
                }

                DRAsteroid asteroid = asteroids[Utility.Random.GetRandom(asteroids.Length)];
                float randomPositionX = SceneBackground.EnemySpawnBoundary.bounds.min.x
                    + SceneBackground.EnemySpawnBoundary.bounds.size.x
                    * (float)Utility.Random.GetRandomDouble();
                float randomPositionZ = SceneBackground.EnemySpawnBoundary.bounds.min.z
                    + SceneBackground.EnemySpawnBoundary.bounds.size.z
                    * (float)Utility.Random.GetRandomDouble();
                // entityId 标识本次出生的实例，asteroid.Id 标识配置表记录。
                GameEntry.Entity.ShowAsteroid(new AsteroidData(
                    GameEntry.Entity.GenerateSerialId(),
                    asteroid.Id)
                {
                    Position = new Vector3(randomPositionX, 0f, randomPositionZ),
                });
            }
        }
    }
}
