"""Check the distributable package, Unity metadata, binary payloads and Git state."""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import re
import subprocess
import zipfile
from sync_samples import sample_differences


def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), *args], text=True, encoding='utf-8').splitlines()


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
    expected_samples = {'00_PointPointPoint', '22_StarForce'} | {entry['folder'] for entry in layout}
    registered_samples = {sample['path'].removeprefix('Samples~/') for sample in manifest['samples']}
    if registered_samples != expected_samples:
        errors.append('Each numbered example must have its own Sample registration.')
    actual_samples = {path.name for path in (package / 'Samples~').iterdir() if path.is_dir()}
    if actual_samples != registered_samples:
        errors.append('Samples~ contains missing or unregistered directories: ' + str(actual_samples ^ registered_samples))
    for sample_name in sorted(registered_samples):
        stale_sample = sample_differences(root, sample_name, package / 'Samples~' / sample_name)
        if stale_sample:
            errors.append(sample_name + ' sample mirror is stale: ' + ', '.join(stale_sample[:10]))
    examples = root / 'Assets/Examples'
    readme_roots = (examples, package / 'Samples~')
    expected_readmes = (
        {'README.md', '00_PointPointPoint/README.md', '22_StarForce/README.md'},
        {'00_PointPointPoint/README.md', '22_StarForce/README.md'},
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
    module_roots = [examples / entry['folder'] for entry in layout]
    common = examples / 'Common'
    shared_prefab = common / 'Prefabs/QGF.prefab'
    if not shared_prefab.is_file() or len(list(common.rglob('*.prefab'))) != 1:
        errors.append('Module examples must share Common/Prefabs/QGF.prefab.')
    for entry, module in zip(layout, module_roots):
        assembly_meta = module / (entry['assembly'] + '.asmdef.meta')
        sample_assembly = package / 'Samples~' / entry['folder'] / (entry['assembly'] + '.asmdef')
        if not assembly_meta.is_file() or not sample_assembly.is_file():
            errors.append('Module Sample anchor assembly is missing: ' + entry['folder'])
            continue
        match = re.search(r'^guid: ([0-9a-f]{32})$', assembly_meta.read_text(encoding='utf-8-sig'), re.M)
        if match is None or match[1] != entry['anchorGuid']:
            errors.append('Module Sample anchor does not match its assembly GUID: ' + entry['folder'])
    sample_guid_owners = {}
    source_common_guids = set()
    for meta in common.rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(encoding='utf-8-sig'), re.M)
        if match:
            source_common_guids.add(match[1])
    for entry in layout:
        sample_common = package / 'Samples~' / entry['folder'] / 'Common'
        if not (sample_common / 'Prefabs/QGF.prefab').is_file():
            errors.append('Module Sample is missing its QGF prefab: ' + entry['folder'])
            continue
        sample_guids = set()
        for meta in sample_common.rglob('*.meta'):
            match = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(encoding='utf-8-sig'), re.M)
            if match:
                previous = sample_guid_owners.get(match[1])
                if previous:
                    errors.append('Module Samples share a Common GUID: ' + previous + ' and ' + entry['folder'])
                sample_guid_owners[match[1]] = entry['folder']
                sample_guids.add(match[1])
        if sample_guids.intersection(source_common_guids):
            errors.append('Module Sample retained development Common GUIDs: ' + entry['folder'])
        for path in (package / 'Samples~' / entry['folder']).rglob('*'):
            if not path.is_file() or path.suffix not in {'.asset', '.prefab', '.unity', '.mat', '.controller', '.meta'}:
                continue
            content = path.read_text(encoding='utf-8-sig', errors='ignore')
            if any(guid in content for guid in source_common_guids):
                errors.append('Module Sample has a stale Common reference: ' + path.relative_to(root).as_posix())
                break
    example_cases = sorted(path for module in module_roots for path in module.glob('[0-9][0-9]_*') if path.is_dir())
    for entry, module in zip(layout, module_roots):
        if not (module / (entry['assembly'] + '.asmdef')).is_file():
            errors.append('A module needs its own assembly: ' + entry['folder'])
        if (module / 'Framework.prefab').exists() or (module / 'UI/UISprites').exists():
            errors.append('Module framework and common UI copies must be removed: ' + entry['folder'])
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
    for script in (script for module in module_roots for script in module.rglob('*.cs')):
        content = script.read_text(encoding='utf-8-sig')
        assembly = next((path for parent in script.parents if parent != examples
                         for path in parent.glob('*.asmdef')), None)
        editor_only_assembly = assembly is not None and 'Editor' in json.loads(
            assembly.read_text(encoding='utf-8-sig')).get('includePlatforms', [])
        editor_fixture = re.search(r'\bUnityEditor(?:\.|\b)|\bResourceExamplePaths\b|\bResourceFixtureManifest\b', content)
        if editor_fixture and not content.lstrip().startswith('#if UNITY_EDITOR') and not editor_only_assembly:
            errors.append('An example using Editor APIs or fixtures needs an Editor-only guard: ' + script.relative_to(root).as_posix())
        if re.search(r'\bAwaitable\b', content):
            errors.append('Unity 2022.3 examples must use UniTask instead of Awaitable: ' + script.relative_to(root).as_posix())
        if 'using Cysharp.Threading.Tasks;' in content:
            if assembly is None or 'UniTask' not in json.loads(assembly.read_text(encoding='utf-8-sig')).get('references', []):
                errors.append('An example using UniTask needs an explicit assembly reference: ' + str(script.relative_to(root)))
        if re.search(r'\bvoid\s+Print\s*\(', script.read_text(encoding='utf-8-sig')):
            errors.append('Example output must be direct, without a Print wrapper: ' + script.relative_to(root).as_posix())
        if re.search(r'\b(?:TutorialLesson|TutorialAssets|TutorialFiles|IEnumerator)\b|\bStartCoroutine\s*\(', script.read_text(encoding='utf-8-sig')):
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
