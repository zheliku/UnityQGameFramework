"""按当前 QGF 路径排列官方原文，生成可在 VS Code 查看逐文件 diff 的独立 Git 仓库。"""
import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import subprocess
import urllib.request
import zipfile


ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / "Packages/com.zheliku.unityqgameframework"
OUTPUT = ROOT / "Artifacts~/UgfComparison"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def git(directory, *arguments):
    return subprocess.check_output(
        ["git", "-c", "core.autocrlf=false", "-c", "core.excludesFile=", "-C", str(directory), *arguments],
        encoding="utf-8", errors="strict", stderr=subprocess.PIPE)


def upstream_sources():
    """固定提交和归档摘要；缓存存在时不访问网络。"""
    locks = json.loads(Path(__file__).with_name("upstream.json").read_text(encoding="utf-8"))
    sources = {}
    for repository, lock in locks.items():
        archive = OUTPUT / "Upstream" / (repository + ".zip")
        if not archive.exists():
            archive.parent.mkdir(parents=True, exist_ok=True)
            url = f"https://codeload.github.com/EllanJiang/{repository}/zip/{lock['commit']}"
            with urllib.request.urlopen(url, timeout=90) as response:
                content = response.read()
            if digest(content) != lock["archiveSha256"]:
                raise RuntimeError("Upstream archive checksum mismatch: " + repository)
            archive.write_bytes(content)
        if digest(archive.read_bytes()) != lock["archiveSha256"]:
            raise RuntimeError("Cached upstream archive checksum mismatch: " + repository)
        with zipfile.ZipFile(archive) as zipped:
            sources[repository] = {
                item.filename.split("/", 1)[1]: zipped.read(item)
                for item in zipped.infolist() if not item.is_dir()
            }
    return locks, sources


def guid(content):
    match = re.search(rb"(?m)^guid: ([a-f0-9]{32})\s*$", content)
    return match[1].decode("ascii") if match else None


def official_path(repository, path):
    """只改变放置位置和文件名，绝不改写基线内容。"""
    if repository == "GameFramework":
        if not path.startswith("GameFramework/") or path.endswith((".csproj", ".csproj.user")):
            return None
        if path == "GameFramework/DataTable/DataTableManager.DataTable.cs":
            return "Runtime/Core/DataTable/DataTable.cs"
        return "Runtime/Core/" + path.removeprefix("GameFramework/").replace("GameFramework", "Framework")
    if path.startswith("Scripts/Runtime/"):
        return "Runtime/Unity/" + path.removeprefix("Scripts/Runtime/").replace("GameFramework", "Framework")
    if path.startswith("Scripts/Editor/"):
        return "Editor/" + path.removeprefix("Scripts/Editor/").replace("GameFramework", "Framework")
    if path.startswith("Libraries/"):
        return "Runtime/Plugins/" + path.removeprefix("Libraries/")
    if path.startswith("GameFramework.prefab"):
        return "Runtime/Prefabs/" + path.replace("GameFramework", "UnityQGameFramework", 1)
    if path == "Scripts/Runtime.meta":
        return "Runtime/Unity.meta"
    if path == "Scripts/Editor.meta":
        return "Editor.meta"
    if path == "Libraries.meta":
        return "Runtime/Plugins.meta"
    return path


def normalized_text(content):
    """统计统一改名造成的差异；Git diff 仍始终使用原文。"""
    try:
        text = content.decode("utf-8-sig").replace("\r\n", "\n")
    except UnicodeDecodeError:
        return None
    for old, new in (
        ("UnityQGameFramework.Core", "GameFramework"),
        ("UnityQGameFramework.Runtime", "UnityGameFramework.Runtime"),
        ("UnityQGameFramework.Editor", "UnityGameFramework.Editor"),
        ("Unity Q Game Framework", "Game Framework"),
    ):
        text = text.replace(old, new)
    text = re.sub(r"\bFramework(?=Action|Entry|EventArgs|Exception|Func|LinkedList|Module|MultiDictionary|Serializer|Log)", "GameFramework", text)
    return text


def prepare_review(refresh):
    review = OUTPUT / "Review"
    if review.exists():
        marker = review / ".git/qgf-review.json"
        if not refresh or not marker.is_file():
            raise RuntimeError("Review already exists. Use --refresh for a previously generated QGF review.")
        # 刷新前完整保留旧对照仓库及用户在其中写下的笔记。
        history = OUTPUT / "History" / (datetime.now().strftime("%Y%m%d-%H%M%S-%f") + ".zip")
        if review.resolve().parent != OUTPUT.resolve() or history.resolve().parent.parent != OUTPUT.resolve():
            raise RuntimeError("Review path escaped its output directory.")
        history.parent.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(history, "w", compression=zipfile.ZIP_DEFLATED) as backup:
            for path in review.rglob("*"):
                if path.is_file():
                    backup.write(path, path.relative_to(review).as_posix())
        with zipfile.ZipFile(history) as backup:
            if backup.testzip() is not None:
                raise RuntimeError("Review backup verification failed.")
        # 保留 Git 对象与索引，不移动 Windows 上可能只读或被编辑器占用的 .git 目录。
        for path in review.rglob("*"):
            if path.is_file() and path.relative_to(review).parts[0] != ".git":
                if review.resolve() not in path.resolve().parents:
                    raise RuntimeError("Invalid previous snapshot path: " + str(path))
                path.unlink()
    review.mkdir(parents=True, exist_ok=True)
    git(review, "init", "--initial-branch=ugf-baseline")
    git(review, "config", "user.name", "QGF Review")
    git(review, "config", "user.email", "review@localhost")
    git(review, "config", "core.autocrlf", "false")
    git(review, "config", "core.quotepath", "false")
    git(review, "config", "core.excludesFile", "")
    git(review, "config", "core.filemode", "false")
    return review


def write_files(root, files):
    for path, content in files.items():
        target = root / path
        if root.resolve() not in target.resolve().parents:
            raise RuntimeError("Invalid generated path: " + path)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(content)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--refresh", action="store_true", help="Back up and regenerate an existing review.")
    args = parser.parse_args()
    locks, sources = upstream_sources()
    current = {path.relative_to(PACKAGE).as_posix(): path.read_bytes()
               for path in PACKAGE.rglob("*") if path.is_file()}
    current_guids = {}
    for path, content in current.items():
        if path.endswith(".meta") and guid(content):
            current_guids.setdefault(guid(content), []).append(path)

    baseline = {}
    origins = {}
    for repository, files in sources.items():
        for path, content in files.items():
            destination = official_path(repository, path)
            if destination is None:
                continue
            method = "path"
            meta = content if path.endswith(".meta") else files.get(path + ".meta", b"")
            matches = current_guids.get(guid(meta), [])
            if len(matches) == 1:
                destination = matches[0] if path.endswith(".meta") else matches[0].removesuffix(".meta")
                method = "guid"
            if destination in baseline:
                raise RuntimeError("Duplicate upstream mapping: " + destination)
            baseline[destination] = content
            origins[destination] = {"repository": repository, "commit": locks[repository]["commit"],
                                    "upstreamPath": path, "mapping": method}

    review = prepare_review(args.refresh)
    write_files(review, baseline)
    git(review, "add", "--all", "--force")
    git(review, "-c", "commit.gpgsign=false", "commit", "--no-verify", "--allow-empty", "--quiet", "-m",
        "Official UGF sources: original contents aligned to QGF paths")
    # 只移除刚生成的基线文件，新增文件由当前包快照写入。
    for path in baseline.keys() - current.keys():
        target = review / path
        if review.resolve() not in target.resolve().parents:
            raise RuntimeError("Invalid obsolete path: " + path)
        target.unlink()
    write_files(review, current)

    entries = []
    for path in sorted(baseline.keys() | current.keys()):
        before, after = baseline.get(path), current.get(path)
        status = "added" if before is None else "deleted" if after is None else "unchanged" if before == after else "modified"
        normalized = before is not None and after is not None and normalized_text(before) is not None and normalized_text(before) == normalized_text(after)
        entries.append({"path": path, "status": status, "renameOnly": normalized and status == "modified",
                        "beforeSha256": digest(before) if before is not None else None,
                        "afterSha256": digest(after) if after is not None else None,
                        **origins.get(path, {})})
    report = {"generatedAt": datetime.now(timezone.utc).isoformat(), "sourcePackage": str(PACKAGE),
              "sourceHead": git(ROOT, "rev-parse", "HEAD").strip(), "upstream": locks,
              "scope": "Complete distributable package, including source, metadata, plugins, documentation and Samples. Binary files use hashes.",
              "counts": dict(Counter(entry["status"] for entry in entries)),
              "entries": entries}
    (OUTPUT / "file-map.json").write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    (review / ".git/qgf-review.json").write_text(json.dumps({"generated": True, "counts": report["counts"]}), encoding="utf-8")
    workspace = {"folders": [{"name": "UGF → QGF (review copy)", "path": "Review"}],
                 "settings": {"git.openRepositoryInParentFolders": "never", "git.statusLimit": 100000,
                              "git.untrackedChanges": "mixed", "diffEditor.ignoreTrimWhitespace": False,
                              "files.exclude": {"**/*.meta": True}, "editor.renderWhitespace": "selection",
                              "scm.defaultViewMode": "tree"}}
    (OUTPUT / "UGF-QGF.code-workspace").write_text(json.dumps(workspace, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    (OUTPUT / "README.md").write_text(
        "# UGF → QGF review\n\nOpen `UGF-QGF.code-workspace` in VS Code and select Source Control (Ctrl+Shift+G). "
        "Click an M file to compare official UGF on the left with current QGF on the right. "
        "U files are additions; D files have no current counterpart. Unchanged files have no status.\n\n"
        "The baseline contains exact official file bytes, placed at corresponding current QGF paths. "
        "Renamed and moved counterparts therefore appear as M, rather than D/U. "
        "Original paths, matching methods and hashes are in `file-map.json`. "
        "Metadata is hidden in Explorer only; it remains in Git and the inventory. "
        "Vendor sources and Samples are included as additions because UGF did not include them.\n\n"
        "This is a generated copy. Make implementation changes in the main project. "
        "Run `python Tools/Review/compare_ugf.py --refresh` there to update this snapshot. "
        "Previous review repositories are preserved under `History/`. "
        "No commits or index changes are made in the main repository.\n", encoding="utf-8")
    # 验证每个基线和当前快照的内容与完整覆盖范围。
    tracked = set(git(review, "ls-tree", "-r", "--name-only", "HEAD").splitlines())
    if tracked != baseline.keys():
        raise RuntimeError("Baseline coverage differs from the mapping.")
    for path, content in current.items():
        if (review / path).read_bytes() != content:
            raise RuntimeError("Review snapshot mismatch: " + path)
    print(json.dumps({"review": str(review), "workspace": str(OUTPUT / "UGF-QGF.code-workspace"),
                      "counts": report["counts"], "mappedFiles": len(baseline), "currentFiles": len(current)}, indent=2))


if __name__ == "__main__":
    main()
