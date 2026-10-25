"""Check the distributable package, Unity metadata, binary payloads and Git state."""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import re
import subprocess
import zipfile
from sync_samples import sample_differences, sample_groups


def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), *args], text=True, encoding='utf-8').splitlines()


def file_digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--require-committed', action='store_true')
    args = parser.parse_args()
    package = root / 'Packages/com.zheliku.unityqgameframework'
    manifest = json.loads((package / 'package.json').read_text(encoding='utf-8-sig'))
    errors = []
    unitask = json.loads((root / 'Tools/Packaging/unitask-source.json').read_text(encoding='utf-8'))
    plugin = package / unitask['destination']
    for name, digest in unitask['files'].items():
        path = plugin / name
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != digest:
            errors.append('Bundled UniTask differs from its pinned upstream source: ' + name)
    if not (plugin / 'LICENSE.txt').is_file():
        errors.append('Bundled UniTask license is missing.')
    yooasset = json.loads((root / 'Tools/Packaging/yooasset-source.json').read_text(encoding='utf-8-sig'))
    yooasset_root = package / yooasset['destination']
    for name, digest in yooasset['files'].items():
        path = yooasset_root / name
        accepted = {digest}
        if name in yooasset.get('unityImportHashes', {}):
            accepted.add(yooasset['unityImportHashes'][name])
        if not path.is_file() or file_digest(path) not in accepted:
            errors.append('Bundled YooAsset differs from its pinned upstream source: ' + name)
    if yooasset.get('version') != '3.0.6' or not (yooasset_root / 'LICENSE.txt').is_file():
        errors.append('Bundled YooAsset version or license is missing.')
    if manifest['name'] != package.name:
        errors.append('Package name does not match its directory.')
    optional_tools = {'com.annulusgames.alchemy', 'com.borodar.rainbow-folders'}
    if optional_tools.intersection(manifest.get('dependencies', {})):
        errors.append('Optional editor tools must not be required framework dependencies.')
    version_source = (package / 'Runtime/Core/Base/Version/Version.cs').read_text(encoding='utf-8-sig')
    runtime_version = re.search(r'FrameworkVersionString\s*=\s*"([^"]+)"', version_source)
    if runtime_version is None or runtime_version[1] != manifest['version']:
        errors.append('Framework display version does not match package.json.')
    changelog = package / 'CHANGELOG.md'
    releases = re.findall(r'^## \[([0-9][^\]]*)\]', changelog.read_text(encoding='utf-8-sig'), re.M) if changelog.is_file() else []
    if not releases or releases[0] != manifest['version']:
        errors.append('The latest CHANGELOG.md version does not match package.json.')
    layout = json.loads((root / 'Tools/Documentation/examples-layout.json').read_text(encoding='utf-8-sig'))
    groups = sample_groups(root)
    expected_samples = {entry['folder'] for entry in groups}
    registered_samples = {sample['path'].removeprefix('Samples~/') for sample in manifest['samples']}
    if registered_samples != expected_samples:
        errors.append('Register the Sample groups defined in sample-groups.json.')
    actual_samples = {path.name for path in (package / 'Samples~').iterdir() if path.is_dir()}
    if actual_samples != registered_samples:
        errors.append('Samples~ contains missing or unregistered directories: ' + str(actual_samples ^ registered_samples))
    for sample_name in sorted(registered_samples):
        stale_sample = sample_differences(root, sample_name, package / 'Samples~' / sample_name)
        if stale_sample:
            errors.append(sample_name + ' sample mirror is stale: ' + ', '.join(stale_sample[:10]))
    examples = root / 'Assets/Examples'
    if {path.name for path in examples.iterdir() if path.is_dir()} != registered_samples:
        errors.append('Assets/Examples and Samples~ must have the same three root directories.')
    if any(entry.get('source') != entry['folder'] for entry in groups):
        errors.append('Editable Sample source names must match their published directory names.')
    readme_roots = (examples, package / 'Samples~')
    expected_readmes = (
        {'README.md', '00_PointPointPoint/README.md', '01_Modules/README.md', '02_StarForce/README.md'},
        {entry['folder'] + '/README.md' for entry in groups},
    )
    for scope, expected in zip(readme_roots, expected_readmes):
        actual = {path.relative_to(scope).as_posix() for path in scope.rglob('README.md')}
        if actual != expected:
            errors.append('Example README entry points differ: ' + str(sorted(actual ^ expected)))
    # Unity serializes Chinese labels as Unicode escapes; inspect decoded text too.
    example_text_files = 0
    for scope in (examples, package / 'Samples~'):
        for path in scope.rglob('*'):
            if not path.is_file() or path.suffix not in {'.cs', '.md', '.json', '.unity', '.prefab', '.asset', '.toml', '.txt', '.uxml', '.uss'}:
                continue
            example_text_files += 1
            content = path.read_text(encoding='utf-8-sig')
            decoded = re.sub(r'\\u([0-9a-fA-F]{4})', lambda match: chr(int(match[1], 16)), content)
            if re.search('课|教[程学]', decoded) or re.search(r'\b(?:tutorial|lessons?)\b', decoded, re.I):
                errors.append('Use 示例 in example text, including serialized UI: ' + path.relative_to(root).as_posix())
    module_root = examples / '01_Modules'
    common = module_root / 'Common'
    shared_prefab = common / 'Prefabs/QGF.prefab'
    if not shared_prefab.is_file() or len(list(common.rglob('*.prefab'))) != 1:
        errors.append('Module examples must share Common/Prefabs/QGF.prefab.')
    module_assembly = module_root / 'UnityQGameFramework.Examples.Modules.asmdef'
    if not module_assembly.is_file():
        errors.append('Module Sample needs one root assembly: 01_Modules')
    if not (common / 'Prefabs/QGF.prefab').is_file() or len(list(common.rglob('*.prefab'))) != 1:
        errors.append('Module Sample must contain one Common/Prefabs/QGF.prefab.')
    example_cases = sorted(path for path in module_root.rglob('*')
                           if path.is_dir() and 'Art' not in path.relative_to(module_root).parts
                           and list(path.glob('*.unity')))
    if (module_root / 'Framework.prefab').exists() or (module_root / 'UI/UISprites').exists():
        errors.append('Module framework and common UI copies must be removed from 01_Modules.')
    if any((examples / name).exists() for name in ('ModuleTutorials', 'ModuleExamples', 'StarForce')):
        errors.append('Old unnumbered sample directories remain.')
    point_root = examples / '00_PointPointPoint/GameMain'
    for relative in ('Configs/Game.toml', 'Prefabs/QGF.prefab', 'Scenes/PointPointPoint.unity',
                     'Scripts/UnityQGameFramework.PointPointPoint.asmdef', 'UI/UIForms/PointGameView.prefab'):
        if not (point_root / relative).is_file():
            errors.append('The PointPointPoint GameMain layout is incomplete: ' + relative)
    for case in example_cases:
        scenes = list(case.glob('*.unity'))
        if len(scenes) != 1:
            errors.append('An example needs one main scene: ' + case.relative_to(root).as_posix())
            continue
        script = case / (scenes[0].stem + '.cs')
        if not script.is_file() or not re.search(r'class\s+' + re.escape(script.stem) + r'\s*:\s*MonoBehaviour\b', script.read_text(encoding='utf-8-sig')):
            errors.append('An example must use an ordinary MonoBehaviour: ' + script.relative_to(root).as_posix())
    for script in module_root.rglob('*.cs'):
        content = script.read_text(encoding='utf-8-sig')
        assembly = next((path for parent in script.parents if parent != examples
                         for path in parent.glob('*.asmdef')), None)
        editor_only_assembly = assembly is not None and 'Editor' in json.loads(
            assembly.read_text(encoding='utf-8-sig')).get('includePlatforms', [])
        editor_fixture = re.search(r'\bUnityEditor(?:\.|\b)|\bAssetDatabase\b|\bResourceBuildService\b|\bResourceBuildProfile\b', content)
        if editor_fixture and not content.lstrip().startswith('#if UNITY_EDITOR') and not editor_only_assembly:
            errors.append('An example using Editor APIs or fixtures needs an Editor-only guard: ' + script.relative_to(root).as_posix())
        if re.search(r'\bAwaitable\b', content):
            errors.append('Unity 2022.3 examples must use UniTask instead of Awaitable: ' + script.relative_to(root).as_posix())
        if 'using Cysharp.Threading.Tasks;' in content:
            if assembly is None or 'UniTask' not in json.loads(assembly.read_text(encoding='utf-8-sig')).get('references', []):
                errors.append('An example using UniTask needs an explicit assembly reference: ' + str(script.relative_to(root)))
        if re.search(r'\bvoid\s+Print\s*\(', script.read_text(encoding='utf-8-sig')):
            errors.append('Example output must be direct, without a Print wrapper: ' + script.relative_to(root).as_posix())
        text = script.read_text(encoding='utf-8-sig')
        # Luban-generated tables and hand-written DataTable adapters need IEnumerator
        # for the common enumeration contract; other examples should use UniTask.
        allows_table_enumerator = 'Generated' in script.parts or 'DataTable<' in text
        if re.search(r'\b(?:TutorialLesson|TutorialAssets|TutorialFiles)\b|\bStartCoroutine\s*\(', text) or (
                re.search(r'\bIEnumerator\b', text) and not allows_table_enumerator):
            errors.append('An obsolete shared example helper or coroutine remains: ' + script.relative_to(root).as_posix())
    guid_paths = defaultdict(list)
    asset_count = 0
    for scope in (package, root / 'Assets'):
        for path in scope.rglob('*'):
            relative = path.relative_to(scope)
            if any(part.startswith('.') or part.endswith('~') for part in relative.parts):
                continue
            if path.suffix == '.meta':
                if not path.with_suffix('').exists():
                    errors.append('Orphan metadata: ' + str(path.relative_to(root)))
                match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(encoding='utf-8-sig'), re.M)
                if not match:
                    errors.append('Invalid metadata GUID: ' + str(path.relative_to(root)))
                else:
                    guid_paths[match[1]].append(str(path.relative_to(root)))
            else:
                asset_count += 1
                if not Path(str(path) + '.meta').is_file():
                    errors.append('Missing metadata: ' + str(path.relative_to(root)))
    for paths in guid_paths.values():
        if len(paths) > 1:
            errors.append('Duplicate GUID: ' + ', '.join(paths))
    for dll in package.rglob('*.dll'):
        if dll.read_bytes()[:2] != b'MZ':
            errors.append('DLL is not a binary payload (possible LFS pointer): ' + str(dll.relative_to(root)))
    payloads = [p for p in package.rglob('*') if p.is_file() and p.name != '.gitattributes']
    package_files = [p.relative_to(root).as_posix() for p in payloads]
    for path in payloads:
        with path.open('rb') as stream:
            if stream.read(128).startswith(b'version https://git-lfs.github.com/spec/v1'):
                errors.append('Package contains an LFS pointer: ' + path.relative_to(root).as_posix())
    attributes = subprocess.check_output(
        ['git', '-C', str(root), 'check-attr', '-z', '--stdin', 'filter'],
        input=('\0'.join(package_files) + '\0').encode('utf-8')).decode('utf-8').split('\0')
    for index in range(0, len(attributes) - 1, 3):
        if attributes[index + 2] == 'lfs':
            errors.append('Package file still uses the LFS filter: ' + attributes[index])
    committed_pointers = subprocess.run(
        ['git', '-C', str(root), 'grep', '-a', '-l', '^version https://git-lfs[.]github[.]com/spec/v1$',
         'HEAD', '--', package.relative_to(root).as_posix()],
        capture_output=True, text=True, encoding='utf-8')
    if committed_pointers.returncode not in (0, 1):
        raise RuntimeError('Could not inspect committed package payloads: ' + committed_pointers.stderr)
    pointer_paths = committed_pointers.stdout.splitlines()
    if args.require_committed and pointer_paths:
        errors.append('Committed package contains LFS pointers; commit the real assets: ' + ', '.join(pointer_paths))
    starter = package / 'Editor/Luban/Starter~'
    conf = json.loads((starter / 'luban.conf').read_text(encoding='utf-8-sig'))
    for schema in conf['schemaFiles']:
        if not (starter / schema['fileName']).exists():
            errors.append('Missing starter schema: ' + schema['fileName'])
    workbooks = list(starter.rglob('*.xlsx')) + list((root / 'Assets').rglob('*.xlsx'))
    for workbook in workbooks:
        try:
            with zipfile.ZipFile(workbook) as archive:
                if archive.testzip() is not None or 'xl/workbook.xml' not in archive.namelist():
                    raise ValueError('Invalid OOXML archive')
        except (ValueError, zipfile.BadZipFile) as error:
            errors.append(str(workbook.relative_to(root)) + ': ' + str(error))
    package_path = package.relative_to(root).as_posix()
    changed = git(root, 'diff', '--name-only', 'HEAD', '--', package_path)
    untracked = git(root, 'ls-files', '--others', '--exclude-standard', '--', package_path)
    if args.require_committed and (changed or untracked):
        errors.append('Package has unpublished working-tree changes; commit the reviewed release first.')
    report = {
        'passed': not errors,
        'package': manifest['name'], 'version': manifest['version'],
        'unityMinimum': manifest['unity'], 'assetsChecked': asset_count,
        'exampleTextFilesChecked': example_text_files,
        'examplesChecked': len(example_cases),
        'uniTaskVersion': unitask['version'], 'uniTaskUpstreamFilesChecked': len(unitask['files']),
        'xlsxArchivesChecked': len(workbooks),
        'packagePayloadsChecked': len(payloads),
        'committedLfsPointers': pointer_paths,
        'head': git(root, 'rev-parse', 'HEAD')[0],
        'packageFilesChangedSinceHead': len(changed), 'untrackedPackageFiles': untracked,
        'samplesRegisteredInSource': bool(manifest.get('samples')),
        'errors': errors,
    }
    text = json.dumps(report, ensure_ascii=False, indent=2)
    if args.output:
        output = args.output.resolve()
        if (root / 'Artifacts~').resolve() not in output.parents:
            parser.error('Report output must be below Artifacts~.')
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(text + '\n', encoding='utf-8')
    print(text)
    raise SystemExit(0 if report['passed'] else 1)


if __name__ == '__main__':
    main()
