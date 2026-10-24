"""Copy editable Assets examples into the Git-installable UPM Samples."""
import argparse
import json
from pathlib import Path
import re
import shutil


def sample_groups(root):
    return json.loads((root / 'Tools/Documentation/sample-groups.json').read_text(encoding='utf-8-sig'))


def sample_sources(root, name):
    examples = root / 'Assets/Examples'
    group = next((item for item in sample_groups(root) if item['folder'] == name), None)
    if group is None:
        raise ValueError('Unknown sample: ' + name)
    source_name = group.get('source')
    if not source_name:
        raise ValueError('Sample must declare a source directory: ' + name)
    source = examples / source_name
    if not source.is_dir():
        raise FileNotFoundError('Missing editable Sample source: ' + str(source))
    return {path.relative_to(source): path.read_bytes()
            for path in source.rglob('*') if path.is_file()}


def sample_differences(root, name, target):
    originals = sample_sources(root, name)
    copies = {path.relative_to(target): path for path in target.rglob('*') if path.is_file()}
    return sorted(str(path) for path in originals.keys() | copies.keys()
                  if path not in originals or path not in copies or originals[path] != copies[path].read_bytes())


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
        if obsolete.resolve().parent != samples_root.resolve() or not re.match(r'^\d{2}_', stale):
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
