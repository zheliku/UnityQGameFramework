"""Mirror the editable StarForce example into the Git-installable UPM sample."""
import argparse
from pathlib import Path
import shutil


def differences(source, target):
    originals = {p.relative_to(source): p for p in source.rglob('*') if p.is_file()}
    copies = {p.relative_to(target): p for p in target.rglob('*') if p.is_file()}
    return sorted(str(p) for p in originals.keys() | copies.keys()
                  if p not in originals or p not in copies or originals[p].read_bytes() != copies[p].read_bytes())


def main():
    root = Path(__file__).resolve().parents[2]
    source = root / 'Assets/Examples/StarForce'
    target = root / 'Packages/com.zheliku.unityqgameframework/Samples~/StarForce'
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    if not (source / 'StarForce Launcher.unity').is_file():
        parser.error('StarForce source is missing.')
    changed = differences(source, target)
    if args.check:
        if changed:
            parser.error('Run sync_samples.py before publishing. Stale sample files: ' + ', '.join(changed[:10]))
    elif changed:
        # This destination is a generated mirror; only this fixed package subdirectory is replaced.
        expected_parent = (root / 'Packages/com.zheliku.unityqgameframework/Samples~').resolve()
        if root.resolve() not in target.resolve().parents or target.resolve().parent != expected_parent:
            raise RuntimeError('Invalid sample destination')
        if target.exists():
            shutil.rmtree(target)
        shutil.copytree(source, target)
    print(f'StarForce sample verified: {sum(p.is_file() for p in source.rglob("*"))} files')


if __name__ == '__main__':
    main()
