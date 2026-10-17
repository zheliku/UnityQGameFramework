"""Check the distributable package, Unity metadata, binary payloads and Git state."""
import argparse
from collections import defaultdict
import json
from pathlib import Path
import re
import subprocess
import zipfile


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
    if manifest['name'] != package.name:
        errors.append('Package name does not match its directory.')
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
        'xlsxArchivesChecked': len(workbooks),
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
