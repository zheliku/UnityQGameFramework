"""Migrate saved sample UI to TMP while retaining component IDs and references."""
from pathlib import Path
import json
import re
import math

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'Assets/Examples'
TEXT = '5f7201a12d95ffc409449d95f23cf332'
TMP_TEXT = 'f4688fdb7df04437aeb418b961361dc5'
INPUT = 'd199490a83bb2b844b9695cbf13b01ef'
TMP_INPUT = '2da0c512f12947e489f739169773d7ca'
DROPDOWN = '0d0b652f32a2cc243917e4028fa0f046'
TMP_DROPDOWN = '7b743370ac3e4ec2a1668f5455a8ef8a'
LAYOUT = 'e94d780b998856ef9a74337d62377873'
OLD_LOCALIZE = 'fda707c591da4f6ba0610a093b2e1795'
LOCALIZE = 'e1ee7bf3b56841379ea43696e2178278'


def field(text, name, default=None):
    match = re.search(r'^  ' + re.escape(name) + r':(.*(?:\n(?!  \w|---).*)*)', text, re.M)
    return match[1].strip() if match else default


def guid(path):
    return re.search(r'^guid: (\w+)', Path(str(path) + '.meta').read_text(), re.M)[1]


def set_field(block, name, value):
    return re.sub(r'(^  ' + re.escape(name) + r':).*$', lambda match: match[1] + ' ' + value, block, flags=re.M)


def bake_layout(blocks, layout):
    """Save the intended wide layout using native anchors and camera framing."""
    by_id = {re.search(r'&(-?\d+)', block)[1]: index for index, block in enumerate(blocks)
             if re.match(r'^--- !u!', block)}
    for name, minimum, maximum in [('controls', '{x: 0.025, y: 0.05}', '{x: 0.475, y: 0.95}'),
                                   ('legend', '{x: 0.54, y: 0.72}', '{x: 0.97, y: 0.94}')]:
        reference = re.search(r'^  ' + name + r': \{fileID: (-?\d+)\}', layout, re.M)
        if reference and reference[1] != '0':
            index = by_id[reference[1]]
            for field_name, value in [('m_AnchorMin', minimum), ('m_AnchorMax', maximum),
                                      ('m_AnchoredPosition', '{x: 0, y: 0}'), ('m_SizeDelta', '{x: 0, y: 0}')]:
                blocks[index] = set_field(blocks[index], field_name, value)
    camera = re.search(r'^  sceneCamera: \{fileID: (-?\d+)\}', layout, re.M)
    if camera and camera[1] != '0':
        camera_index = by_id[camera[1]]
        camera_block = blocks[camera_index]
        camera_object = field(camera_block, 'm_GameObject')
        transform_index = next(index for index, block in enumerate(blocks)
                               if block.startswith('--- !u!4 ') and field(block, 'm_GameObject') == camera_object)
        # CanvasScaler uses a 960 x 640 reference. Expand keeps the whole composition visible.
        half_height = max(float(field(camera_block, 'orthographic size', '6')), 3 / .88, 4.5 / (.44 * 1.5))
        blocks[camera_index] = set_field(camera_block, 'orthographic size', str(half_height))
        position = '{x: ' + str(3.5 - .25 * 2 * half_height * 1.5) + ', y: 0, z: -10}'
        blocks[transform_index] = set_field(blocks[transform_index], 'm_LocalPosition', position)


def migrate_text(block, template, font, material):
    result = template
    for name in ['m_ObjectHideFlags', 'm_CorrespondingSourceObject', 'm_PrefabInstance', 'm_PrefabAsset',
                 'm_GameObject', 'm_Enabled', 'm_Color', 'm_RaycastTarget', 'm_Maskable']:
        value = field(block, name)
        if value is not None:
            result = re.sub(r'(^  ' + name + r':).*$', lambda match: match[1] + ' ' + value, result, flags=re.M)
    result = re.sub(r'^  m_text:.*$', lambda _: '  m_text: ' + field(block, 'm_Text', '""'), result, flags=re.M)
    font_data = block.split('  m_FontData:\n', 1)[1].split('  m_Text:', 1)[0]
    def font_value(name, default):
        match = re.search(r'^    ' + name + r': (.+)', font_data, re.M)
        return match[1] if match else default
    alignment = int(font_value('m_Alignment', '0'))
    values = {
        'm_fontAsset': font, 'm_sharedMaterial': material,
        'm_fontColor': field(block, 'm_Color', '{r: 1, g: 1, b: 1, a: 1}'),
        'm_fontSize': font_value('m_FontSize', '20'), 'm_fontSizeBase': font_value('m_FontSize', '20'),
        'm_enableAutoSizing': font_value('m_BestFit', '0'),
        'm_fontSizeMin': font_value('m_MinSize', '10'), 'm_fontSizeMax': font_value('m_MaxSize', '40'),
        'm_fontStyle': font_value('m_FontStyle', '0'),
        'm_HorizontalAlignment': str([1, 2, 4][alignment % 3]),
        'm_VerticalAlignment': str([256, 512, 1024][alignment // 3]),
        'm_enableWordWrapping': '0' if font_value('m_HorizontalOverflow', '0') == '1' else '1',
        'm_TextWrappingMode': '0' if font_value('m_HorizontalOverflow', '0') == '1' else '1',
        'm_isRichText': font_value('m_RichText', '1'),
        'm_lineSpacing': '0',
    }
    for name, value in values.items():
        pattern = r'^  ' + name + r':.*$'
        if re.search(pattern, result, re.M):
            result = re.sub(pattern, lambda _: '  ' + name + ': ' + value, result, flags=re.M)
        else:
            result += '  ' + name + ': ' + value + '\n'
    return result


def main():
    prototype = (ROOT / 'Artifacts~/TmpMigration/Template.prefab').read_text(encoding='utf-8-sig')
    template = next(block.split('\n', 1)[1] for block in re.split(r'(?=^--- !u!)', prototype, flags=re.M)
                    if 'guid: ' + TMP_TEXT in block)
    fonts = {
        '00_PointPointPoint': 'GameMain/Fonts/DefaultFont.asset',
        '01_Modules': '10_Localization/Common/ChineseFont.asset',
        '02_StarForce': 'GameMain/Fonts/MainFont.asset',
    }
    changed = []
    for sample, relative in fonts.items():
        asset = ASSETS / sample / relative
        font_guid = guid(asset)
        text = asset.read_text(encoding='utf-8-sig')
        material_id = re.search(r'^  (?:m_Material|material): \{fileID: (-?\d+)\}', text, re.M)[1]
        font = '{fileID: 11400000, guid: ' + font_guid + ', type: 2}'
        material = '{fileID: ' + material_id + ', guid: ' + font_guid + ', type: 2}'
        for path in (ASSETS / sample).rglob('*'):
            if path.suffix not in {'.unity', '.prefab'}:
                continue
            original = path.read_text(encoding='utf-8-sig')
            blocks = re.split(r'(?=^--- !u!)', original, flags=re.M)
            removed = []
            layouts = [block for block in blocks if 'guid: ' + LAYOUT in block]
            for layout in layouts:
                bake_layout(blocks, layout)
            for index, block in enumerate(blocks):
                if 'guid: ' + TEXT in block:
                    if ' stripped\n' in block.split('MonoBehaviour:', 1)[0]:
                        blocks[index] = block.replace(TEXT, TMP_TEXT)
                        continue
                    header = block.split('\n', 1)[0]
                    blocks[index] = header + '\n' + migrate_text(block, template, font, material)
                elif 'guid: ' + INPUT in block:
                    blocks[index] = block.replace(INPUT, TMP_INPUT)
                    if ' stripped\n' in block.split('MonoBehaviour:', 1)[0]:
                        continue
                    game_object = field(block, 'm_GameObject')
                    rect = next(re.search(r'&(-?\d+)', item)[1] for item in blocks
                                if item.startswith('--- !u!224 ') and field(item, 'm_GameObject') == game_object)
                    blocks[index] += '  m_TextViewport: {fileID: ' + rect + '}\n'
                elif 'guid: ' + DROPDOWN in block:
                    blocks[index] = block.replace(DROPDOWN, TMP_DROPDOWN)
                elif 'guid: ' + LAYOUT in block:
                    removed.append(re.search(r'&(-?\d+)', block)[1])
                    blocks[index] = ''
                elif 'm_ReferenceResolution:' in block:
                    blocks[index] = set_field(block, 'm_ScreenMatchMode', '1')
            migrated = ''.join(blocks).replace(OLD_LOCALIZE, LOCALIZE)
            for component in removed:
                migrated = re.sub(r'^  - component: \{fileID: ' + component + r'\}\n', '', migrated, flags=re.M)
            if migrated != original:
                path.write_text(migrated, encoding='utf-8')
                changed.append(path.relative_to(ROOT).as_posix())
    report = ROOT / 'Artifacts~/TmpMigration/serialized-migration.json'
    report.write_text(json.dumps(changed, indent=2) + '\n', encoding='utf-8')
    print(f'Migrated {len(changed)} scenes and prefabs.')


if __name__ == '__main__':
    main()
