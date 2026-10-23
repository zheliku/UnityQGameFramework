using System;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Debugger;

public static class DebuggerLifecycleValidation
{
    public static object Run()
    {
        var debugger = FrameworkEntry.GetModule<IDebuggerManager>();
        try
        {
            debugger.ActiveWindow = true;
            var first = new Window();
            var selected = new Window();
            debugger.RegisterDebuggerWindow("First", first);
            debugger.RegisterDebuggerWindow("Selected", selected);
            debugger.SelectDebuggerWindow("Selected");
            Require(debugger.UnregisterDebuggerWindow("First"), "Remove earlier sibling");
            Require(ReferenceEquals(debugger.DebuggerWindowRoot.SelectedWindow, selected), "Keep selected sibling");
            FrameworkEntry.Update(0.1f, 0.1f);
            Require(selected.Updates == 1, "Selected window still updates");
            Require(debugger.UnregisterDebuggerWindow("Selected"), "Remove last window");
            Require(selected.Stops == 1, "Shutdown exactly once");
            Require(!debugger.UnregisterDebuggerWindow("Selected"), "Repeated removal returns false");
            FrameworkEntry.Update(0.1f, 0.1f);
            debugger.DebuggerWindowRoot.OnEnter();
            debugger.DebuggerWindowRoot.OnLeave();
            debugger.DebuggerWindowRoot.SelectedIndex = -1;
            Require(debugger.DebuggerWindowRoot.SelectedWindow == null, "Negative selection is empty");
            return new { passed = true, emptyGroupUpdate = true, selectionPreserved = true, repeatedRemoval = true };
        }
        finally { FrameworkEntry.Shutdown(); }
    }
    private static void Require(bool value, string step) { if (!value) throw new Exception(step); }
    private sealed class Window : IDebuggerWindow
    {
        public int Updates, Stops;
        public void Initialize(params object[] args) { }
        public void Shutdown() => Stops++;
        public void OnEnter() { }
        public void OnLeave() { }
        public void OnUpdate(float elapsed, float realElapsed) => Updates++;
        public void OnDraw() { }
    }
}
