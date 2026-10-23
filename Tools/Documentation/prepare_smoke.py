"""Combine extracted examples with the real Luban Item fixture and smoke entry."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
output = root / 'Artifacts~/Documentation'
files = sorted((output / 'ItemFixture/Code').glob('*.cs'))
if not files:
    raise SystemExit('Generate the example Item fixture with topModule UnityQGameFramework.Config first.')
parts = []
usings = set()
for path in files:
    text = path.read_text(encoding='utf-8-sig')
    if 'namespace UnityQGameFramework.Config' not in text:
        raise SystemExit(f'{path}: regenerate with topModule UnityQGameFramework.Config to match the documentation.')
    usings.update(line.strip() for line in re.findall(r'^using .*?;\s*$', text, re.M))
    body = re.sub(r'^using .*?;\s*$', '', text, flags=re.M)
    parts.append(body)
# Generated types and the partial extension retain their real namespace.
parts.insert(0, (output / 'CompleteExamples.cs').read_text(encoding='utf-8'))
parts.insert(0, '\n'.join(sorted(usings)))
parts.append((output / 'ItemExtension.cs').read_text(encoding='utf-8'))
parts.append((root / 'Tools/Documentation/ExamplesSmoke.cs').read_text(encoding='utf-8'))
(output / 'RuntimeExamples.cs').write_text('\n'.join(parts), encoding='utf-8')
