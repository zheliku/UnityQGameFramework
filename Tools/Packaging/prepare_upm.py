"""Stage the framework and its editable Assets example as a distributable UPM package.

The source tree is never modified. The staged tree keeps the repository's package
path so a future release branch/tag can use the same Git URL with a revision.
"""

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile


def verify_copy(source, destination):
    source_files = {
        path.relative_to(source): path
        for path in source.rglob("*")
        if path.is_file()
    }
    destination_files = {
        path.relative_to(destination): path
        for path in destination.rglob("*")
        if path.is_file()
    }
    if source_files.keys() != destination_files.keys():
        raise RuntimeError("Sample staging changed the set of files")
    for relative, path in source_files.items():
        if hashlib.sha256(path.read_bytes()).digest() != hashlib.sha256(
            destination_files[relative].read_bytes()
        ).digest():
            raise RuntimeError(f"Sample staging changed file bytes: {relative}")
    return len(source_files)


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
    args = parser.parse_args()
    package_source = root / "Packages/com.zheliku.unityqgameframework"
    example_source = root / "Assets/Examples/StarForce"
    manifest = json.loads((package_source / "package.json").read_text(encoding="utf-8-sig"))
    artifacts_root = (root / "Artifacts~").resolve()
    output = (
        args.output.resolve()
        if args.output
        else artifacts_root / "UPM" / manifest["version"]
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
    subprocess.run(check, check=True)

    # Samples~ is already present in the Git package; reject a stale mirror.
    staged_package = output / "Packages" / manifest["name"]
    shutil.copytree(package_source, staged_package)
    staged_sample = staged_package / "Samples~/StarForce"
    sample_files = verify_copy(example_source, staged_sample)
    (staged_package / "package.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    archive = output / f"{manifest['name']}-{manifest['version']}.tgz"
    with tarfile.open(archive, "w:gz") as stream:
        stream.add(staged_package, arcname="package")
    # Reopen the real deliverable to verify the sample registration and entry scene.
    with tarfile.open(archive, "r:gz") as stream:
        exported_manifest = json.load(stream.extractfile("package/package.json"))
        assert exported_manifest["samples"][0]["path"] == "Samples~/StarForce"
        stream.getmember("package/Samples~/StarForce/StarForce Launcher.unity")
        stream.getmember("package/Samples~/StarForce/GameMain.meta")
    print(
        json.dumps(
            {
                "package": str(staged_package),
                "archive": str(archive),
                "sampleFilesVerified": sample_files,
                "sampleBytesPreserved": True,
                "published": False,
            },
            ensure_ascii=False,
            indent=2,
        )
    )


if __name__ == "__main__":
    main()
