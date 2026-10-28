"""按照仓库 .editorconfig 格式化自有 C#，排除第三方及生成代码。"""
import argparse
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / "Packages/com.zheliku.unityqgameframework"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Verify formatting without writing files.")
    args = parser.parse_args()
    includes = [PACKAGE / "Runtime/Core", PACKAGE / "Runtime/Unity", PACKAGE / "Editor",
                ROOT / "Assets", ROOT / "Tools"]
    for directory in includes:
        excludes = []
        for path in directory.rglob("*"):
            if path.is_dir() and path.name in {"Plugins", "Generated", "bin", "obj", "__pycache__"}:
                excludes.append(path)
        command = ["dotnet", "format", "whitespace", str(directory), "--folder", "--verbosity", "minimal"]
        if excludes:
            command.extend(["--exclude", *(str(path) for path in sorted(excludes))])
        if args.check:
            command.append("--verify-no-changes")
        result = subprocess.run(command, cwd=ROOT)
        if result.returncode != 0:
            return result.returncode
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
