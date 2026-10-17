using UnityEngine;
using UnityEngine.EventSystems;

namespace StarForce
{
    /// <summary>Reads UI input without requiring named axes or changing the active input backend.</summary>
    public sealed class StarForceInput : BaseInput
    {
        protected override void Awake()
        {
            base.Awake();
            GetComponent<StandaloneInputModule>().inputOverride = this;
        }

#if ENABLE_INPUT_SYSTEM
        private static UnityEngine.InputSystem.Mouse Mouse => UnityEngine.InputSystem.Mouse.current;
        private static UnityEngine.InputSystem.Controls.ButtonControl Button(int button) => Mouse == null ? null : button == 0 ? Mouse.leftButton : button == 1 ? Mouse.rightButton : Mouse.middleButton;
        public override bool mousePresent => Mouse != null;
        public override Vector2 mousePosition => Mouse?.position.ReadValue() ?? Vector2.zero;
        public override Vector2 mouseScrollDelta => Mouse?.scroll.ReadValue() ?? Vector2.zero;
        public override bool GetMouseButton(int button) => Button(button)?.isPressed ?? false;
        public override bool GetMouseButtonDown(int button) => Button(button)?.wasPressedThisFrame ?? false;
        public override bool GetMouseButtonUp(int button) => Button(button)?.wasReleasedThisFrame ?? false;
        public override bool GetButtonDown(string name)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (name == "Submit" ? keyboard.enterKey.wasPressedThisFrame : name == "Cancel" && keyboard.escapeKey.wasPressedThisFrame);
        }
#else
        public override bool mousePresent => Input.mousePresent;
        public override Vector2 mousePosition => Input.mousePosition;
        public override Vector2 mouseScrollDelta => Input.mouseScrollDelta;
        public override bool GetMouseButton(int button) => Input.GetMouseButton(button);
        public override bool GetMouseButtonDown(int button) => Input.GetMouseButtonDown(button);
        public override bool GetMouseButtonUp(int button) => Input.GetMouseButtonUp(button);
        public override bool GetButtonDown(string name) => name == "Submit" ? Input.GetKeyDown(KeyCode.Return) : name == "Cancel" && Input.GetKeyDown(KeyCode.Escape);
#endif
        public override float GetAxisRaw(string axisName) => 0f;
        public override bool touchSupported => false;
        public override int touchCount => 0;
    }
}
