"""Create a clean, disposable framework consumer for a particular Unity version."""
import argparse
import json
from pathlib import Path
import shutil


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity-version', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    if (root / 'Artifacts~').resolve() not in output.parents:
        parser.error('Output must be below this checkout\'s Artifacts~/ directory')
    if output.exists():
        parser.error('Use a new output directory to avoid stale validation results')
    (output / 'Packages').mkdir(parents=True)
    shutil.copytree(root / 'Packages/com.zheliku.unityqgameframework',
                    output / 'Packages/com.zheliku.unityqgameframework')
    (output / 'Packages/manifest.json').write_text('{"dependencies":{}}\n', encoding='utf-8')
    (output / 'ProjectSettings').mkdir()
    (output / 'ProjectSettings/ProjectVersion.txt').write_text(
        f'm_EditorVersion: {args.unity_version}\n', encoding='utf-8')
    runtime = output / 'Assets/Validation'
    editor = output / 'Assets/Editor'
    runtime.mkdir(parents=True)
    editor.mkdir()
    shutil.copy2(Path(__file__).with_name('RuntimeProbe.cs'), runtime / 'RuntimeProbe.cs')
    shutil.copy2(Path(__file__).with_name('FoundationValidation.cs'), editor / 'FoundationValidation.cs')
    (runtime / 'Foundation.Validation.Runtime.asmdef').write_text(json.dumps({
        'name': 'Foundation.Validation.Runtime',
        'references': ['UnityQGameFramework.Core', 'UnityQGameFramework.Runtime'],
        'autoReferenced': True,
    }, indent=2), encoding='utf-8')
    print(json.dumps({'project': str(output), 'unityVersion': args.unity_version,
                      'executeMethod': 'Foundation.Validation.FoundationValidation.Run'}, indent=2))


if __name__ == '__main__':
    main()
