using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class AttachedCubeLogic : EntityLogic
    {
        public Transform Socket { get; private set; }
        public int AttachCalls { get; private set; }
        public int DetachCalls { get; private set; }
        public string LastData { get; private set; } = "无";

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            Socket = new GameObject("Socket").transform;
            Socket.SetParent(CachedTransform, false);
            Socket.localPosition = Vector3.right * 1.8f;
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            CachedTransform.position = (Vector3)userData;
            CachedTransform.rotation = Quaternion.identity;
            CachedTransform.localScale = Vector3.one * (Entity.Id == 10 ? 0.75f : 0.35f);
            AttachCalls = DetachCalls = 0;
            LastData = "无";
            // 用实例属性区分父子颜色，保持共享材质不变。
            var color = new MaterialPropertyBlock();
            color.SetColor("_BaseColor", Entity.Id == 10 ? new Color(0.12f, 0.65f, 1f) : new Color(0.3f, 0.9f, 0.4f));
            GetComponent<Renderer>().SetPropertyBlock(color);
        }

        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData); // 基类将 Transform 挂到目标节点。
            CachedTransform.localPosition = parentTransform == parentEntity.CachedTransform ? Vector3.right * 1.8f : Vector3.zero;
            CachedTransform.localRotation = Quaternion.identity;
            CachedTransform.localScale = Vector3.one * 0.45f;
            AttachCalls++;
            LastData = userData as string ?? "无";
        }

        protected override void OnDetachFrom(EntityLogic parentEntity, object userData)
        {
            base.OnDetachFrom(parentEntity, userData); // 恢复到实体组下，保留当前世界位置。
            DetachCalls++;
            LastData = userData as string ?? "无";
        }

        protected override void OnUpdate(float elapsed, float realElapsed)
        {
            base.OnUpdate(elapsed, realElapsed);
            if (Entity.Id == 10) CachedTransform.Rotate(0, 0, 45f * elapsed);
        }
    }
}
