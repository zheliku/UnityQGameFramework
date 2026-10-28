using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace Foundation.Validation
{
    public sealed class ValidationProcedure : ProcedureBase
    {
        public static bool Started;
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            Started = true;
        }
    }

    public sealed class RuntimeProbe : MonoBehaviour
    {
        private static int s_Errors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureErrors()
        {
            s_Errors = 0;
            ValidationProcedure.Started = false;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    s_Errors++;
            };
        }

        private IEnumerator Start()
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!ValidationProcedure.Started && Time.realtimeSinceStartup < deadline)
                yield return null;
            for (int i = 0; i < 5; i++)
                yield return null;
            string failure = null;
            try
            {
                if (!ValidationProcedure.Started)
                    throw new Exception("Procedure did not start in a headless Player");
                if (GameEntry.GetComponent<BaseComponent>() == null)
                    throw new Exception("Runtime component lookup failed");
                var eventComponent = GameEntry.GetComponent<EventComponent>();
                if (eventComponent == null || !ReferenceEquals(eventComponent, GameEntry.GetComponent("EventComponent")))
                    throw new Exception("String and typed component lookups disagree");
                if (!ReferenceEquals(GameFrameworkEntry.GetModule<IEventManager>(), GameFrameworkEntry.GetModule<IEventManager>()))
                    throw new Exception("Module lookup returned multiple instances");
                if (s_Errors != 0)
                    throw new Exception("Player logged " + s_Errors + " errors");
            }
            catch (Exception exception) { failure = exception.ToString(); }

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-foundation-result")
                    continue;
                File.WriteAllText(args[i + 1], JsonUtility.ToJson(new PlayerResult
                {
                    success = failure == null,
                    unityVersion = Application.unityVersion,
                    frameworkVersion = UnityQGameFramework.Core.Version.GameFrameworkVersion,
                    procedureStarted = ValidationProcedure.Started,
                    errors = s_Errors,
                    failure = failure,
                }, true));
            }
            Application.Quit(failure == null ? 0 : 1);
        }

        [Serializable]
        private sealed class PlayerResult
        {
            public bool success;
            public string unityVersion;
            public string frameworkVersion;
            public bool procedureStarted;
            public int errors;
            public string failure;
        }
    }
}
