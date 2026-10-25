"""Stage the framework and its editable Assets example as a distributable UPM package.

The source tree is never modified. The staged tree keeps the repository's package
path so a future release branch/tag can use the same Git URL with a revision.
"""

import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
from sync_samples import sample_differences, sample_sources, sample_groups


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output",
        type=Path,
        help="New output directory under Artifacts~/; existing directories are refused",
    )
    parser.add_argument('--require-committed', action='store_true',
                        help='Reject unpublished package changes before staging a release')
    parser.add_argument('--external-dependency', choices=['unitask', 'yooasset'], action='append', default=[],
                        help='Exclude a bundled plugin; the consumer must install its official package')
    args = parser.parse_args()
    package_source = root / "Packages/com.zheliku.unityqgameframework"
    example_source = root / "Assets/Examples/02_StarForce"
    manifest = json.loads((package_source / "package.json").read_text(encoding="utf-8-sig"))
    artifacts_root = (root / "Artifacts~").resolve()
    output = (
        args.output.resolve()
        if args.output
        else artifacts_root / "UPM" / (manifest["version"] + ("-external-" + "-".join(sorted(set(args.external_dependency))) if args.external_dependency else ""))
    )
    if artifacts_root not in output.parents:
        parser.error("Output must be a new directory below this repository's Artifacts~/")
    if output.exists():
        parser.error(f"Output already exists; choose another --output directory: {output}")
    if not (example_source / "StarForce Launcher.unity").is_file():
        parser.error("The StarForce development example is missing")
    if any(path.name == ".git" for path in example_source.rglob("*")):
        parser.error("The example must not contain an upstream Git checkout")

    check = [sys.executable, str(Path(__file__).with_name('check_package.py'))]
    if args.require_committed:
        check.append('--require-committed')
    validation = subprocess.run(check, check=True, stdout=subprocess.PIPE, text=True, encoding='utf-8')
    check_result = json.loads(validation.stdout)

    plugins = {
        'unitask': {
            'directory': 'UniTask',
            'name': 'com.cysharp.unitask',
            'version': '2.5.11',
            'url': 'https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11',
        },
        'yooasset': {
            'directory': 'YooAsset',
            'name': 'com.tuyoogame.yooasset',
            'version': '3.0.6',
            'url': 'https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset#3.0.6',
        },
    }
    external = [plugins[name] for name in sorted(set(args.external_dependency))]

    def ignore_plugins(directory, names):
        if Path(directory).resolve() != (package_source / 'Runtime/Plugins').resolve():
            return []
        excluded = set()
        for plugin in external:
            excluded.add(plugin['directory'])
            excluded.add(plugin['directory'] + '.meta')
        return excluded.intersection(names)

    # Samples~ is already present in the Git package; reject a stale mirror.
    staged_package = output / "Packages" / manifest["name"]
    shutil.copytree(package_source, staged_package, ignore=ignore_plugins)
    sample_files = {}
    for sample in manifest['samples']:
        name = sample['path'].removeprefix('Samples~/')
        if sample_differences(root, name, staged_package / 'Samples~' / name):
            raise RuntimeError('Staged sample differs: ' + name)
        sample_files[name] = len(sample_sources(root, name))
    (staged_package / "package.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    if external:
        readme = staged_package / 'README.md'
        external_note = '\n本分发包排除了内置的 ' + '、'.join(plugin['directory'] for plugin in external) + '；安装前请在项目中安装对应的官方包。\n'
        readme.write_text(readme.read_text(encoding='utf-8-sig') + external_note, encoding='utf-8')
        (output / 'external-dependencies.json').write_text(
            json.dumps({plugin['name']: plugin['url'] for plugin in external}, indent=2) + '\n', encoding='utf-8'
        )
    archive = output / f"{manifest['name']}-{manifest['version']}.tgz"
    with tarfile.open(archive, "w:gz") as stream:
        stream.add(staged_package, arcname="package")
    # Reopen the real deliverable to verify the sample registration and entry scene.
    with tarfile.open(archive, "r:gz") as stream:
        exported_manifest = json.load(stream.extractfile("package/package.json"))
        registered = {sample['path'] for sample in exported_manifest['samples']}
        expected = {'Samples~/' + entry['folder'] for entry in sample_groups(root)}
        assert registered == expected
        stream.getmember("package/Samples~/02_StarForce/StarForce Launcher.unity")
        stream.getmember("package/Samples~/02_StarForce/GameMain.meta")
        stream.getmember("package/Samples~/01_Modules/Common/Prefabs/QGF.prefab")
        stream.getmember("package/Samples~/01_Modules/02_Config/01_ReadValues/ReadConfigValues.unity")
        stream.getmember("package/Samples~/00_PointPointPoint/GameMain/Scenes/PointPointPoint.unity")
        names = stream.getnames()
        for plugin in external:
            prefix = 'package/Runtime/Plugins/' + plugin['directory']
            if any(name == prefix or name == prefix + '.meta' or name.startswith(prefix + '/') for name in names):
                raise RuntimeError('External dependency was bundled: ' + plugin['name'])
    print(
        json.dumps(
            {
                "package": str(staged_package),
                "archive": str(archive),
                "sampleFilesVerified": sample_files,
                "sampleBytesPreserved": True,
                "sourceCheckPassed": check_result['passed'],
                "externalDependencies": external,
                "published": False,
            },
            ensure_ascii=False,
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
