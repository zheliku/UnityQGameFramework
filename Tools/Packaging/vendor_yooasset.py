"""Import the pinned YooAsset source without its game samples."""

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def unity_import_hashes(package):
    # These two upstream meta files contain only format and GUID. Unity adds these default importers.
    folder = 'Editor/BundleBuilder/VisualViewers/InstantAssetBuildPipeline/'
    importers = {
        'InstantAssetBuildPipelineViewer.cs.meta':
            'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n'
            '  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',
        'InstantAssetBuildPipelineViewer.uxml.meta':
            'ScriptedImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n  serializedVersion: 2\n'
            '  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
            '  script: {fileID: 13804, guid: 0000000000000000e000000000000000, type: 0}\n',
    }
    result = {}
    for name, importer in importers.items():
        relative = folder + name
        lines = (package / relative).read_text(encoding='utf-8-sig').splitlines()
        if len(lines) != 2:
            raise ValueError('Expected upstream metadata with only format and GUID: ' + relative)
        imported = ('\n'.join(lines) + '\n' + importer).encode('utf-8')
        result[relative] = hashlib.sha256(imported).hexdigest()
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="YooAsset repository at tag 3.0.6")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    source = args.source.resolve()
    expected = "3b4cfb36cc2e81b9558ab74d65fee2cf2eac2804"
    revision = subprocess.check_output(
        ["git", "-C", str(source), "rev-parse", "HEAD"], text=True
    ).strip()
    if revision != expected:
        parser.error("Expected YooAsset 3.0.6 at " + expected)
    modified = subprocess.check_output(
        ['git', '-C', str(source), 'status', '--porcelain', '--', 'Assets/YooAsset'], text=True
    )
    if modified.strip():
        parser.error('The upstream YooAsset source has local changes; import from a clean checkout')
    package = source / "Assets/YooAsset"
    destination = root / "Packages/com.zheliku.unityqgameframework/Runtime/Plugins/YooAsset"
    if destination.exists():
        parser.error("Destination already exists; do not overwrite local changes")
    hashes = {}
    for path in sorted(package.rglob("*")):
        relative = path.relative_to(package)
        if not path.is_file() or "Samples~" in relative.parts:
            continue
        if relative.as_posix() == "Samples~.meta":
            continue
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)
        hashes[relative.as_posix()] = digest(path)
    shutil.copy2(source / "LICENSE", destination / "LICENSE.txt")
    lock = {
        "name": "YooAsset",
        "version": "3.0.6",
        "upstream": "https://github.com/tuyoogame/YooAsset",
        "tag": "3.0.6",
        "commit": revision,
        "sourceDirectory": "Assets/YooAsset",
        "destination": "Runtime/Plugins/YooAsset",
        "license": "Apache-2.0",
        "files": hashes,
        "unityImportHashes": unity_import_hashes(package),
    }
    (root / "Tools/Packaging/yooasset-source.json").write_text(
        json.dumps(lock, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps({"destination": str(destination), "upstreamFiles": len(hashes)}))


if __name__ == "__main__":
    main()
