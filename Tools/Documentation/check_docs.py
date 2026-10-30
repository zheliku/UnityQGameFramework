"""Check local documentation links and extract complete C# examples for Unity CLI.

Run from the repository root. No third-party Python packages are required.
The generated C# is compiled with Unity CLI run_script --dry_run; it is never
imported into Assets. Schema-dependent partial extensions are reported separately.
"""
from __future__ import annotations

import json
import re
from pathlib import Path
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / "Packages/com.zheliku.unityqgameframework"
DOCS = PACKAGE / "Documentation~"
OUTPUT = ROOT / "Artifacts~/Documentation"
MODULE_DOCUMENTS = {
    "Base": "base-utility.md", "Config": "config.md", "DataNode": "data-node.md",
    "DataTable": "data-table.md", "Debugger": "debugger.md", "Download": "download.md",
    "Entity": "entity.md", "Event": "event.md", "FileSystem": "file-system.md",
    "Fsm": "fsm.md", "Localization": "localization.md", "Network": "network.md",
    "ObjectPool": "object-pool.md", "Procedure": "procedure.md",
    "ReferencePool": "reference-pool.md", "Resource": "resource.md", "Scene": "scene.md",
    "Setting": "setting.md", "Sound": "sound.md", "UI": "ui.md",
    "Utility": "base-utility.md", "Variable": "data-node.md", "WebRequest": "web-request.md",
}


def slug(text: str) -> str:
    text = re.sub(r"<[^>]+>", "", text).replace("`", "")
    text = text.strip().lower()
    return "".join(c for c in text if c.isalnum() or c in " _-").replace(" ", "-")


def blocks(text: str):
    current = None
    for number, line in enumerate(text.splitlines(), 1):
        match = re.match(r"^(`{3,}|~{3,})([a-zA-Z0-9_-]*)\s*$", line)
        if current is None and match:
            current = [match[1], match[2], number, []]
        elif current is not None and line.strip() == current[0]:
            yield current[1], current[2], "\n".join(current[3]) + "\n"
            current = None
        elif current is not None:
            current[3].append(line)
    if current is not None:
        raise ValueError(f"Unclosed code fence at line {current[2]}")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    paths = [ROOT / "README.md", PACKAGE / "README.md", *sorted((ROOT / 'Assets/Examples').rglob('README.md')), *sorted(DOCS.rglob("*.md")),
             *sorted((PACKAGE / 'Samples~').rglob('*.md'))]
    errors = []
    checked_links = 0
    examples = []
    partials = []
    coverage = {}
    for directory in sorted((PACKAGE / "Runtime/Unity").iterdir()):
        if not directory.is_dir():
            continue
        chapter = MODULE_DOCUMENTS.get(directory.name)
        if chapter is None or not (DOCS / "Modules" / chapter).is_file():
            errors.append(f"Runtime/Unity/{directory.name}: no module example")
        else:
            coverage[directory.name] = "Modules/" + chapter
    source = ["// Generated from documentation. Do not edit. Compile only.\n"]
    for chapter, path in enumerate(paths):
        if b'\r\r\n' in path.read_bytes():
            errors.append(f'{path.relative_to(ROOT).as_posix()}: doubled carriage returns')
        content = path.read_text(encoding="utf-8-sig")
        relative = path.relative_to(ROOT).as_posix()
        try:
            code_blocks = list(blocks(content))
        except ValueError as exception:
            errors.append(f"{relative}: {exception}")
            continue
        prose = content
        for language, line, code in code_blocks:
            prose = prose.replace(code, "")
            declarations = re.sub(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"', '', code, flags=re.S)
            if language != "csharp" or not re.search(r"\b(?:class|struct|interface)\s+\w+", declarations):
                continue
            record = {"file": relative, "line": line,
                      "types": re.findall(r"\b(?:class|struct|interface)\s+(\w+)", code)}
            # This partial extends the DRItem generated after the Price schema step.
            # Validate it together with that actual generated source, not a fake DTO.
            if path.name == "02-data-tables.md" and "partial class DRItem" in code:
                record["fixture"] = "Item schema with Price"
                partials.append(record)
                (OUTPUT / "ItemExtension.cs").write_text(code, encoding="utf-8")
                continue
            if path.name == "01-getting-started.md" and "using UnityQGameFramework.Config;" in code:
                record["fixture"] = "Default generated UnityQGameFramework.Config workspace"
                partials.append(record)
                (OUTPUT / "GameStartup.cs").write_text(code, encoding="utf-8")
                continue
            examples.append(record)
            source.append(f'namespace QgfDocsValidation.Chapter{chapter}\n{{\n'
                          f'#line {line + 1} "{relative}"\n{code}\n'
                          '#line default\n}\n')
        for target in re.findall(r"!?\[[^\]\n]*\]\(([^)\n]+)\)", prose):
            target = target.strip().strip("<>")
            if urlsplit(target).scheme or target.startswith("//"):
                continue
            filename, _, anchor = unquote(target).partition("#")
            linked = (path.parent / filename).resolve() if filename else path
            checked_links += 1
            if not linked.exists():
                errors.append(f"{relative}: missing link {target}")
                continue
            if anchor and linked.suffix == ".md":
                headings = [slug(m[1]) for m in re.finditer(
                    r"^#{1,6}\s+(.+?)\s*#*\s*$",
                    linked.read_text(encoding="utf-8-sig"), re.M)]
                if anchor not in headings:
                    errors.append(f"{relative}: missing anchor {target}")
    (OUTPUT / "CompleteExamples.cs").write_text("\n".join(source), encoding="utf-8")
    report = {"passed": not errors, "markdownFiles": len(paths),
              "localLinks": checked_links, "completeExamples": len(examples),
              "runtimeDirectoryCoverage": coverage,
              "examples": examples, "schemaDependentExamples": partials, "errors": errors}
    (OUTPUT / "docs-check.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in report.items()
                      if k not in ("examples", "runtimeDirectoryCoverage")},
                     ensure_ascii=False, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
