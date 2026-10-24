namespace UnityQGameFramework.Editor
{
    // 第一个 GUID 保留旧版 Framework prefab 锚点；第二个是模块程序集锚点。
    internal static class ExampleSampleCatalog
    {
        internal const string LegacyAssemblyGuid = "c1267f68364fa7f42927a4906957275c";
        internal const string ModulesReadmeGuid = "cb9c8b026fa34ec0b0669df5f42f9ad6";
        internal const string PointAssemblyGuid = "57bf2c65eab596540b0dddc0f6926f32";
        internal static readonly string[] GroupAssemblyGuids =
        {
            "26a4a9304f1b571a97885965a89aaed1", // 当前模块示例根目录。
            "4570994c42b45dacaf2f2b87567597e9", // 旧版基础示例，保留卸载识别。
            "9b65e720872b5001b41335f95c015d16", // 旧版进阶示例，保留卸载识别。
        };
        internal static readonly string[][] Modules =
        {
            new[] { "Base", "922138476a5f4e989d722ca4769a2c2b", "451fb7eed1a048a39d433c5f2e2a84f9" },
            new[] { "Config", "4872be88a273436598469cf36dc992a9", "74c628515104487ab46e8c195343f494" },
            new[] { "DataNode", "5889460eee9841a3b527e9451854287c", "0569d91e17474a50bcbc4cf9ce610a8d" },
            new[] { "Event", "2a81dcc3695e4019a2fee8a7985a1fd0", "6d6ccb3a40994ce797ceea27fc7b9b00" },
            new[] { "Fsm", "7ef79c649b4c4a9da688b29277b76ff4", "b12fb1fa2df742f791a917fed60ac0b9" },
            new[] { "Procedure", "8b2fd079e69b4920a94c4c94a5fdcb71", "316e0f0a61164a63aa9ffb0b1de5130e" },
            new[] { "ReferencePool", "d237017054e84150bb43729cebdfca36", "6db43065adb6494ea8b51f0f3d7ac386" },
            new[] { "ObjectPool", "33d614ac20384ad19c414172b0c68bf0", "875fad970bfa4f7db24353c475ddcda3" },
            new[] { "Setting", "092791a935c74b8789d8aff9807c18b0", "636842cd577841a587a4696ab4a69cb9" },
            new[] { "Localization", "eccb26c7f86b4b3cac250631b303ef9d", "546e1e2e8f2a49bdaeb03ad61edce340" },
            new[] { "DataTable", "4a6c974e3fa145db9b3e8b6080701a6c", "9cb40d2fa7d54547ad62f12dee11fa0a" },
            new[] { "Resource", "4a69c805f624450ea83cb5eacbd16343", "c2ad608804cc4202bd615677e0af4fbb" },
            new[] { "Entity", "7b6808f770464b67b61c1e79bdeeb6c2", "ae5b69fc6e6c420eb8a94c2f3fabd1b8" },
            new[] { "UI", "c0e4f82bb625466e99d0799092e53e33", "cad20e62ad474103ab805e69424a0958" },
            new[] { "Sound", "c57808fe5030475da2eda36a853f3ee0", "bead710ed85a4e19b16e4e6815fc7e14" },
            new[] { "Scene", "3eb9191cd1f94bbc8eeac63f6f2915c2", "bdd615d3777d46c487aeeb6777122b02" },
            new[] { "FileSystem", "d5aca376a6c247af8b1100bbbb925b61", "5cd2c46a3d5844cc9bdfa2598b06fce5" },
            new[] { "WebRequest", "4fd7360f898b428ead7073433006b610", "ac8a73123ee145a584d0f448bb0c22ac" },
            new[] { "Download", "8ced14a34866463b8d71772fc3c86bea", "e1db92c5a1f04414ab9307b6836aacd1" },
            new[] { "Network", "d814ae877df54f8da66a2aec8744f08d", "6a874a2080ef4446bbe06ca26f489ab9" },
            new[] { "Debugger", "75dd7e3a7cb345d7ab74e1972ad27d8e", "c79b243b74ca44668be4b228ba13159f" },
        };
    }
}
