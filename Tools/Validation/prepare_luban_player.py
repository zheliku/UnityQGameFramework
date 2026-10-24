"""Prepare a fresh UPM tarball consumer for the Luban standalone Player check."""
import argparse
import json
from pathlib import Path
import shutil


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--package', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--unity-version', default='6000.3.24f1')
    args = parser.parse_args()
    package, output = args.package.resolve(), args.output.resolve()
    if (root / 'Artifacts~').resolve() not in output.parents or output.exists():
        parser.error('Output must be a new directory below Artifacts~.')
    if not package.is_file() or package.suffix != '.tgz':
        parser.error('Supply the staged UPM .tgz from Tools/Packaging/prepare_upm.py.')
    (output / 'Packages').mkdir(parents=True)
    (output / 'Packages/manifest.json').write_text(json.dumps({'dependencies': {
        'com.zheliku.unityqgameframework': 'file:' + package.as_posix(),
    }}), encoding='utf-8')
    (output / 'ProjectSettings').mkdir()
    (output / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: ' + args.unity_version + '\n', encoding='utf-8')
    runtime = output / 'Assets/LubanValidation'
    editor = output / 'Assets/Editor'
    runtime.mkdir(parents=True)
    editor.mkdir()
    workspace = root / 'Assets/Examples/02_StarForce/UnityQGameFramework/Config'
    for folder in ('Generated', 'Data', 'Extensions'):
        shutil.copytree(workspace / folder, runtime / folder)
        shutil.copy2(workspace / (folder + '.meta'), runtime / (folder + '.meta'))
    # Intentionally omit Excel/schema/Profile: a Player must require only generated output.
    shutil.copy2(Path(__file__).with_name('LubanRuntimeProbe.cs'), runtime / 'LubanRuntimeProbe.cs')
    config = root / 'Assets/Examples/02_StarForce/GameMain/Configs/DefaultConfig.toml'
    shutil.copy2(config, runtime / 'GameConfig.toml')
    shutil.copy2(config.with_suffix('.toml.meta'), runtime / 'GameConfig.toml.meta')
    shutil.copy2(Path(__file__).with_name('LubanPlayerValidation.cs'), editor / 'LubanPlayerValidation.cs')
    (runtime / 'Luban.Validation.Runtime.asmdef').write_text(json.dumps({
        'name': 'Luban.Validation.Runtime',
        'references': ['UnityQGameFramework.Core', 'UnityQGameFramework.Runtime', 'UnityQGameFramework.Luban'],
    }), encoding='utf-8')
    print(json.dumps({'project': str(output), 'executeMethod': 'Luban.Validation.LubanPlayerValidation.Run'}, indent=2))


if __name__ == '__main__':
    main()
