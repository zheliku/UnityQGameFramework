"""Check the example API map against an explicit Unity reflection inventory.

The report separates source locations, documented configuration and pending work.
It does not establish that a source call compiled, ran, or produced usable UI.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / 'Packages/com.zheliku.unityqgameframework'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inventory', type=Path, required=True,
                        help='Full entries exported by AuditExampleApi.Run, not the CLI summary')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    inventory = read(args.inventory)
    if 'entries' not in inventory:
        parser.error('Expected the full reflection export containing entries.')
    if inventory.get('schemaVersion', 0) < 2:
        parser.error('Run AuditExampleApi.Run again; the current inventory must include declared events and inherited interfaces.')
    contracts = {entry['type']: entry for entry in inventory['entries']}
    coverage = read(ROOT / 'Tools/Documentation/example-api-coverage.json')
    errors = []
    rows = []
    seen_types = set()
    source_texts = {}
    source_call_audit = dict(mapped=0, found=0, classified=0, missing=[])
    overload_groups = {}

    def check_locations(record, owner):
        for location in record.get('examples', []) + record.get('relatedExamples', []):
            if not (ROOT / location).is_dir():
                errors.append(f'{owner}: missing source folder {location}')
        documents = record.get('documents', [])
        if 'documentation' in record:
            documents = [*documents, record['documentation']]
        for location in [*documents, *record.get('sources', [])]:
            path = PACKAGE / location if location.startswith('Documentation~/') else ROOT / location
            if not path.is_file():
                errors.append(f'{owner}: missing document or source {location}')

    def source_for(locations):
        sources = []
        for location in locations:
            if location not in source_texts:
                folder = ROOT / location
                source_texts[location] = '\n'.join(
                    path.read_text(encoding='utf-8-sig')
                    for path in folder.rglob('*.cs')
                ) if folder.is_dir() else ''
            sources.append(source_texts[location])
        source = '\n'.join(sources)
        source = re.sub(r'/\*.*?\*/', '', source, flags=re.S)
        source = re.sub(r'(?<!:)//[^\r\n]*', '', source)
        return source

    def method_name(signature):
        name = signature.split('(', 1)[0].rsplit(' ', 1)[-1]
        return name.split('[', 1)[0]

    def has_source_call(member, kind):
        locations = member.get('examples', []) + member.get('relatedExamples', [])
        source = source_for(locations)
        if kind == 'methods':
            name = method_name(member['signature'])
            pattern = rf'\.\s*{re.escape(name)}\s*(?:<[^>;\r\n]*>)?\s*\('
        else:
            name = member['name']
            pattern = rf'\.\s*{re.escape(name)}\b'
        return re.search(pattern, source) is not None

    for mapped in coverage['types']:
        name = mapped['type']
        check_locations(mapped, name)
        if name in seen_types:
            errors.append('Duplicate type: ' + name)
        seen_types.add(name)
        if name not in contracts:
            errors.append('Type missing from inventory: ' + name)
            continue
        contract = contracts[name]
        row = dict(type=name, layer=contract['layer'], interfaces=contract['interfaces'])
        for kind, key in [('methods', 'signature'), ('properties', 'name'), ('events', 'name')]:
            pending_key = {'methods': 'pendingMethods', 'properties': 'pendingProperties', 'events': 'pendingEvents'}[kind]
            declared = {member[key] for member in contract[kind]}
            current = mapped.get(kind, [])
            pending = mapped.get(pending_key, [])
            seen_members = set()
            for member in current + pending:
                identity = member[key]
                if identity in seen_members:
                    errors.append(f'{name}: duplicate {identity}')
                seen_members.add(identity)
                if identity not in declared:
                    errors.append(f'{name}: unknown declared member {identity}')
                has_example = member.get('examples') or member.get('relatedExamples')
                has_documented_reason = (member.get('classification') and member.get('reason')
                    and (member.get('documents') or member.get('documentation')))
                if contract['layer'] == 'Unity' and not (has_example or has_documented_reason):
                    errors.append(f'{name}.{identity}: add an example or document why it is configuration-only')
                if contract['layer'] == 'Unity' and has_example:
                    source_call_audit['mapped'] += 1
                    if kind == 'methods':
                        method = method_name(identity)
                        group = overload_groups.setdefault((name, method), dict(signatures=set(), locations=set()))
                        group['signatures'].add(identity)
                        group['locations'].update(member.get('examples', []))
                        group['locations'].update(member.get('relatedExamples', []))
                    if has_source_call(member, kind):
                        source_call_audit['found'] += 1
                    elif has_documented_reason:
                        source_call_audit['classified'] += 1
                    else:
                        missing_call = f'{name}.{identity}'
                        source_call_audit['missing'].append(missing_call)
                        errors.append(f'{missing_call}: mapped example has no source call; classify its lifecycle/configuration use if intentional')
                check_locations(member, f'{name}.{identity}')
            row[kind] = dict(declared=len(declared),
                exampleLocations=sum(bool(member.get('examples') or member.get('relatedExamples')) for member in current),
                classified=sum(bool(member.get('classification')) for member in current),
                pending=len(pending),
                unaccounted=sorted(declared - seen_members),
                sourceOnly=[member[key] for member in current if 'Source call added' in member.get('verification', '')])
        rows.append(row)

    overload_call_audit = dict(groups=0, signatures=0, callSites=0, shortfalls=[])
    for (type_name, method), group in overload_groups.items():
        if len(group['signatures']) < 2:
            continue
        source = source_for(sorted(group['locations']))
        pattern = rf'\.\s*{re.escape(method)}\s*(?:<[^>;\r\n]*>)?\s*\('
        call_sites = len(re.findall(pattern, source))
        overload_call_audit['groups'] += 1
        overload_call_audit['signatures'] += len(group['signatures'])
        overload_call_audit['callSites'] += call_sites
        if call_sites < len(group['signatures']):
            shortfall = dict(type=type_name, method=method, signatures=len(group['signatures']), callSites=call_sites)
            overload_call_audit['shortfalls'].append(shortfall)
            errors.append(f'{type_name}.{method}: {call_sites} source call sites for {len(group["signatures"])} mapped overloads')

    unmapped = [dict(type=name, layer=entry['layer'], methods=len(entry['methods']), properties=len(entry['properties']), events=len(entry['events']), interfaces=entry['interfaces'])
                for name, entry in contracts.items() if name not in seen_types]
    result = dict(mappingIsConsistent=not errors,
        scope='Exact declared signatures and source/doc paths, plus lexical method and property checks for mapped Unity APIs. Overload checks compare mapped signatures with source call-site counts. These checks are not semantic overload resolution, compilation, execution, rendered UI or full-goal completion evidence.',
        inventory=str(args.inventory), mappedTypes=len(rows),
        runtimeComponents=[row for row in rows if row['layer'] == 'Unity'],
        coreTypes=[row for row in rows if row['layer'] == 'Core'],
        runtimeSourceCallAudit=source_call_audit,
        runtimeOverloadCallAudit=overload_call_audit,
        unmappedTypes=unmapped, declaredEvents=sum(len(entry['events']) for entry in contracts.values()),
        unmappedEvents=sum(len(entry['events']) for name, entry in contracts.items() if name not in seen_types),
        errors=errors)
    output = args.output.resolve()
    artifacts = (ROOT / 'Artifacts~').resolve()
    if artifacts not in output.parents:
        parser.error('Output must be under this repository\'s Artifacts~ directory.')
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(mappingIsConsistent=result['mappingIsConsistent'], mappedTypes=len(rows),
        runtimeComponents=len(result['runtimeComponents']), unaccountedRuntimeMethods=sum(len(row['methods']['unaccounted']) for row in result['runtimeComponents']),
        unaccountedRuntimeProperties=sum(len(row['properties']['unaccounted']) for row in result['runtimeComponents']),
        unaccountedRuntimeEvents=sum(len(row['events']['unaccounted']) for row in result['runtimeComponents']),
        unaccountedMappedCoreEvents=sum(len(row['events']['unaccounted']) for row in result['coreTypes']),
        runtimeApiCallsMapped=source_call_audit['mapped'], runtimeApiCallsFound=source_call_audit['found'],
        runtimeApiCallsClassified=source_call_audit['classified'], runtimeApiCallsMissing=len(source_call_audit['missing']),
        overloadedMethodGroups=overload_call_audit['groups'], overloadedSignatures=overload_call_audit['signatures'],
        overloadCallSites=overload_call_audit['callSites'], overloadShortfalls=len(overload_call_audit['shortfalls']),
        declaredEvents=result['declaredEvents'],
        unmappedEvents=result['unmappedEvents'],
        unmappedTypes=len(unmapped), errors=errors), ensure_ascii=False, indent=2))
    raise SystemExit(0 if not errors else 1)


if __name__ == '__main__':
    main()
