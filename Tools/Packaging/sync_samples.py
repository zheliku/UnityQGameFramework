"""Mirror editable examples into the Git-installable UPM samples."""
import argparse
import json
from pathlib import Path
import re
import shutil
import uuid

SAMPLE_GUID_NAMESPACE = uuid.uuid5(uuid.NAMESPACE_URL, 'https://github.com/zheliku/UnityQGameFramework/samples')


def differences(source, target):
    originals = {p.relative_to(source): p for p in source.rglob('*') if p.is_file()}
    copies = {p.relative_to(target): p for p in target.rglob('*') if p.is_file()}
    return sorted(str(p) for p in originals.keys() | copies.keys()
                  if p not in originals or p not in copies or originals[p].read_bytes() != copies[p].read_bytes())


def sample_sources(root, name):
    examples = root / 'Assets/Examples'
    if name in {'00_PointPointPoint', '22_StarForce'}:
        source = examples / name
        if not source.is_dir() or not any(source.rglob('*.unity')):
            raise ValueError(name + ' source scenes are missing.')
        return {p.relative_to(source): p.read_bytes() for p in source.rglob('*') if p.is_file()}

    layout = json.loads((root / 'Tools/Documentation/examples-layout.json').read_text(encoding='utf-8-sig'))
    module = next((entry for entry in layout if entry['folder'] == name), None)
    if module is None:
        raise ValueError('Unknown sample: ' + name)
    source = examples / name
    if not source.is_dir() or not any(source.rglob('*.unity')):
        raise ValueError('Module source scenes are missing: ' + name)

    common = examples / 'Common'
    files = {p.relative_to(source): p.read_bytes() for p in source.rglob('*') if p.is_file()}
    files.update({Path('Common') / p.relative_to(common): p.read_bytes() for p in common.rglob('*') if p.is_file()})
    files[Path('Common.meta')] = (examples / 'Common.meta').read_bytes()
    if not module.get('responsiveLayout'):
        for filename in ('ExampleSceneLayout.cs', 'ExampleSceneLayout.cs.meta'):
            files.pop(Path('Common') / filename, None)

    guid_map = {}
    for relative in [Path('Common.meta')] + [p for p in files if p.parts[0] == 'Common' and p.suffix == '.meta']:
        content = files[relative].decode('utf-8-sig')
        match = re.search(r'^guid: ([0-9a-f]{32})\r?$', content, re.M)
        if match:
            old_guid = match[1]
            guid_map[old_guid] = uuid.uuid5(SAMPLE_GUID_NAMESPACE, name + '/' + old_guid).hex

    result = {}
    for relative, content in files.items():
        try:
            text = content.decode('utf-8')
        except UnicodeDecodeError:
            result[relative] = content
            continue
        for old_guid, new_guid in guid_map.items():
            text = text.replace(old_guid, new_guid)
        # 公共显示脚本随各模块独立编译，避免同时导入多个 Sample 时出现同名类型。
        common_namespace = 'UnityQGameFramework.Examples.Presentation'
        sample_namespace = common_namespace + '.' + module['module']
        text = text.replace(common_namespace, sample_namespace)
        text = text.replace('Assembly-CSharp::' + sample_namespace, module['assembly'] + '::' + sample_namespace)
        result[relative] = text.encode('utf-8')
    return result


def sample_differences(root, name, target):
    originals = sample_sources(root, name)
    copies = {p.relative_to(target): p for p in target.rglob('*') if p.is_file()}
    return sorted(str(p) for p in originals.keys() | copies.keys()
                  if p not in originals or p not in copies or originals[p] != copies[p].read_bytes())


def main():
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    package = root / 'Packages/com.zheliku.unityqgameframework'
    samples_root = package / 'Samples~'
    manifest = json.loads((package / 'package.json').read_text(encoding='utf-8-sig'))
    for sample in manifest['samples']:
        name = sample['path'].removeprefix('Samples~/')
        if '/' in name or '\\' in name or name in ('', '.', '..'):
            parser.error('Invalid sample path: ' + sample['path'])
        target = samples_root / name
        originals = sample_sources(root, name)
        changed = sample_differences(root, name, target)
        if args.check:
            if changed:
                parser.error('Run sync_samples.py before publishing. Stale ' + name + ' files: ' + ', '.join(changed[:10]))
        elif changed:
            expected_parent = samples_root.resolve()
            if root.resolve() not in target.resolve().parents or target.resolve().parent != expected_parent:
                raise RuntimeError('Invalid sample destination')
            if target.exists():
                shutil.rmtree(target)
            for relative, content in originals.items():
                destination = target / relative
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(content)
        print(f'{name} sample verified: {len(originals)} files')

    registered = {sample['path'].removeprefix('Samples~/') for sample in manifest['samples']}
    for stale in {path.name for path in samples_root.iterdir() if path.is_dir()} - registered:
        obsolete = samples_root / stale
        if stale not in {'01_Modules', '99_StarForce'} or obsolete.resolve().parent != samples_root.resolve():
            parser.error('Unregistered Sample directory: ' + stale)
        if args.check:
            parser.error('Remove obsolete Sample directory: ' + stale)
        shutil.rmtree(obsolete)
    for obsolete in (samples_root / 'README.md', samples_root / 'README.md.meta'):
        if obsolete.exists():
            if args.check:
                parser.error('Remove obsolete Samples~ landing page: ' + obsolete.name)
            obsolete.unlink()


if __name__ == '__main__':
    main()
