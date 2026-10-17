"""Normalize SpreadsheetML element prefixes for VS Code XLSX Diff compatibility.

The installed fastxlsx reader expects unprefixed workbook/sheet/row tags. Both
representations are valid OOXML; this operation changes namespace spelling only.
"""
import argparse
from pathlib import Path
import re
import zipfile
import xml.etree.ElementTree as ET


MAIN = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'


def signature(data):
    return [(node.tag, sorted(node.attrib.items()), node.text, node.tail)
            for node in ET.fromstring(data).iter()]


def normalize(path):
    path = Path(path).resolve()
    workspace = Path(__file__).resolve().parents[2]
    if workspace not in path.parents or path.suffix.lower() != '.xlsx':
        raise ValueError('Only .xlsx files inside this workspace may be normalized.')
    changes = 0
    with zipfile.ZipFile(path) as source:
        archive_comment = source.comment
        entries = []
        for info in source.infolist():
            data = source.read(info.filename)
            if info.filename.endswith('.xml') and ET.fromstring(data).tag.startswith('{' + MAIN + '}'):
                text = data.decode('utf-8-sig')
                prefixes = re.findall(r'xmlns:([A-Za-z_][\w.-]*)=["\']' + re.escape(MAIN) + r'["\']', text)
                if prefixes:
                    opening_end = text.index('>') if not text.startswith('<?xml') else text.index('>', text.index('?>') + 2)
                    opening = text[:opening_end]
                    if not re.search(r'\sxmlns=["\']' + re.escape(MAIN) + r'["\']', opening):
                        if re.search(r'\sxmlns=["\']', opening):
                            raise ValueError('Unexpected default namespace in ' + info.filename)
                        insertion = opening_end - 1 if text[opening_end - 1] == '/' else opening_end
                        text = text[:insertion] + ' xmlns="' + MAIN + '"' + text[insertion:]
                    for prefix in prefixes:
                        text = re.sub(r'<(/?)' + re.escape(prefix) + r':', r'<\1', text)
                    normalized = text.encode('utf-8')
                    if signature(data) != signature(normalized):
                        raise ValueError('Namespace normalization changed XML meaning: ' + info.filename)
                    if data != normalized:
                        data = normalized
                        changes += 1
            entries.append((info, data))
    if changes:
        temporary = path.with_suffix('.xlsx.qgf-normalizing')
        try:
            with zipfile.ZipFile(temporary, 'w') as destination:
                destination.comment = archive_comment
                for info, data in entries:
                    destination.writestr(info, data)
            with zipfile.ZipFile(temporary) as check:
                if check.testzip() is not None:
                    raise ValueError('Normalized archive failed CRC validation.')
            temporary.replace(path)
        finally:
            if temporary.exists():
                temporary.unlink()
    return changes


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('files', type=Path, nargs='+')
    args = parser.parse_args()
    for file in args.files:
        print(f'{file}: normalized {normalize(file)} XML parts')
