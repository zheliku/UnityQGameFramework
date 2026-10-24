"""Prepare a clean Unity consumer using a real UPM Git dependency.

Accepts remote HTTPS URLs and unpublished git+file snapshots. It never changes
the framework checkout's index, commits, branches or remotes.
"""
import argparse
import json
from pathlib import Path
import shutil


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--url', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--unity-version', default='6000.3.24f1')
    parser.add_argument('--mode', choices=('foundation', 'luban'), default='luban')
    args = parser.parse_args()
    output = args.output.resolve()
    if (root / 'Artifacts~').resolve() not in output.parents or output.exists():
        parser.error('Output must be a new directory below Artifacts~.')
    if '?path=/Packages/com.zheliku.unityqgameframework' not in args.url:
        parser.error('Use a Git URL selecting the framework package subdirectory.')
    (output / 'Packages').mkdir(parents=True)
    (output / 'Packages/manifest.json').write_text(json.dumps({'dependencies': {
        'com.zheliku.unityqgameframework': args.url,
    }}, indent=2), encoding='utf-8')
    (output / 'ProjectSettings').mkdir()
    (output / 'ProjectSettings/ProjectVersion.txt').write_text(
        'm_EditorVersion: ' + args.unity_version + '\n', encoding='utf-8')
    runtime = output / 'Assets/Validation'
    editor = output / 'Assets/Editor'
    runtime.mkdir(parents=True)
    editor.mkdir()
    tests = Path(__file__).parent
    if args.mode == 'foundation':
        shutil.copy2(tests / 'RuntimeProbe.cs', runtime / 'RuntimeProbe.cs')
        shutil.copy2(tests / 'FoundationValidation.cs', editor / 'FoundationValidation.cs')
        references = ['UnityQGameFramework.Core', 'UnityQGameFramework.Runtime']
        entry = 'Foundation.Validation.FoundationValidation.Run'
    else:
        workspace = root / 'Assets/Examples/02_StarForce/UnityQGameFramework/Config'
        for folder in ('Generated', 'Data', 'Extensions'):
            shutil.copytree(workspace / folder, runtime / folder)
            shutil.copy2(workspace / (folder + '.meta'), runtime / (folder + '.meta'))
        # Keep the same paths expected by the standalone probe.
        runtime.rename(output / 'Assets/LubanValidation')
        runtime = output / 'Assets/LubanValidation'
        config = root / 'Assets/Examples/02_StarForce/GameMain/Configs/DefaultConfig.toml'
        shutil.copy2(config, runtime / 'GameConfig.toml')
        shutil.copy2(config.with_suffix('.toml.meta'), runtime / 'GameConfig.toml.meta')
        for name in ('LubanPlayerValidation.cs', 'GitLubanInstallValidation.cs'):
            shutil.copy2(tests / name, editor / name)
        shutil.copy2(tests / 'LubanRuntimeProbe.cs', runtime / 'LubanRuntimeProbe.cs')
        references = ['UnityQGameFramework.Core', 'UnityQGameFramework.Runtime', 'UnityQGameFramework.Luban']
        entry = 'GitLubanInstallValidation.Run'
    (runtime / 'Validation.Runtime.asmdef').write_text(json.dumps({
        'name': 'Foundation.Validation.Runtime' if args.mode == 'foundation' else 'Luban.Validation.Runtime',
        'references': references,
    }, indent=2), encoding='utf-8')
    print(json.dumps({'project': str(output), 'url': args.url, 'executeMethod': entry}, indent=2))


if __name__ == '__main__':
    main()
