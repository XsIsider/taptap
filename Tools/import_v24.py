"""Read original V2.4 workbooks; export typed Unity JSON and schema. Never edit source sheets."""
import datetime
import argparse
import json
from pathlib import Path
import openpyxl
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'SourceData/V24TwoFloors'
OUT = ROOT / 'Assets/Castle/Resources/Castle/V24'
CODE = ROOT / 'Assets/Castle/Scripts/V24'

def pascal(value):
    return ''.join(part[:1].upper() + part[1:] for part in value.split('_'))

def convert(value, kind):
    if kind.endswith('[]'):
        return [convert(v, kind[:-2]) for v in str(value).split('|') if v] if value is not None else []
    if kind == 'int': return int(value or 0)
    if kind == 'float': return float(value or 0)
    if kind == 'bool': return str(value).lower() in ('true', '1')
    if isinstance(value, datetime.datetime): return value.strftime('%Y-%m-%d %H:%M:%S')
    return str(value) if value is not None else ''

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=SOURCE)
    parser.add_argument('--output', type=Path, default=OUT)
    parser.add_argument('--code-output', type=Path, default=CODE)
    args = parser.parse_args()
    source, output, code_output = args.source, args.output, args.code_output
    output.mkdir(parents=True, exist_ok=True)
    code_output.mkdir(parents=True, exist_ok=True)
    result = {}; classes = []
    for path in sorted((source / '配置表').glob('*.xlsx')):
        name = 'PlotChoice' if path.name.startswith('Plot_Choice') else path.name.split('_')[0]
        sheet = openpyxl.load_workbook(path, data_only=True).active
        rows = list(sheet.values)
        columns = [(i, pascal(key), rows[1][i]) for i, key in enumerate(rows[0]) if key and not key.startswith('##')]
        data = []
        for row in rows[5:]:
            if str(row[0] or '').startswith('##') or not row[1]: continue
            data.append({key: convert(row[i], kind) for i, key, kind in columns})
        if len({r['Id'] for r in data}) != len(data): raise ValueError('Duplicate '+name)
        result[name] = data
        fields = []
        for _, key, kind in columns:
            typ = kind if kind in ('int','float','bool','int[]','string[]') else 'string'
            default = ' = Array.Empty<'+typ[:-2]+'>()' if typ.endswith('[]') else ' = ""' if typ=='string' else ''
            fields.append(f'        public {typ} {key}{default};')
        classes.append('    [Serializable]\n    public sealed class V24'+name+'\n    {\n'+'\n'.join(fields)+'\n    }')
    binding = json.loads((source / 'Unity绑定_2.4测试.json').read_text(encoding='utf-8-sig'))
    def normalize(value):
        if isinstance(value, dict): return {pascal(k):normalize(v) for k,v in value.items()}
        if isinstance(value, list): return [normalize(v) for v in value]
        return value
    for key in ['GameSettings','DeviceConfig','MapConfig','HotspotBinding']: result[key] = normalize(binding[key])
    # 新包的四日流程与名称揭晓绑定必须一并导出，不能静默丢弃后沿用旧逻辑。
    for key in ['ControlRoomPolicy', 'DialogueActionConfig', 'SleepPolicy', 'CutsceneConfig', 'FinaleConfig', 'RoomDisplayConfig']:
        if key in binding:
            result[key] = normalize(binding[key])
    if 'SleepPolicy' in binding:
        result['SleepPolicy']['Requirements'] = [dict(Day=int(day), All=value.get('all', []), Any=value.get('any', [])) for day, value in binding['SleepPolicy']['required_before_sleep'].items()]
    result['InvitationEntry'] = binding['场景组件绑定'][0]['entry']
    result['TutorialTape'] = binding['场景组件绑定'][1]['entry']
    # Fail before writing generated output when a future table update breaks references.
    indexes = {name: {row['Id'] for row in rows} for name, rows in result.items() if isinstance(rows, list) and rows and isinstance(rows[0], dict) and 'Id' in rows[0]}
    errors = []
    def ref(table, value, where):
        if value and value not in indexes[table]:
            errors.append(f'{where}: missing {table}:{value}')
    for table in ['Character', 'Room', 'Entry', 'Plot', 'PlotChoice', 'Event', 'Record', 'Puzzle']:
        if any(row['Id'] <= 0 for row in result[table]):
            errors.append(f'{table}: ID must be positive')
    for row in result['Entry']:
        for owner in row['Owner']: ref('Character', owner, f"Entry {row['Id']}")
        ref('Record', row['Record'], f"Entry {row['Id']}")
    for row in result['Event']:
        for field, target in [('Room','Room'), ('UseEntry','Entry'), ('Record','Record'), ('NextEvent','Event'), ('PlanB','Event')]:
            ref(target, row[field], f"Event {row['Id']}.{field}")
        for reward in row['Reward']: ref('Entry', reward, f"Event {row['Id']}.Reward")
    for row in result['Plot']:
        ref('Character', row['SpeakerId'], f"Plot {row['Id']}")
        if len(row['LinkText']) != len(row['LinkTarget']): errors.append(f"Plot {row['Id']}: link counts differ")
        for text in row['LinkText']:
            if row['Text'].count(text) != 1: errors.append(f"Plot {row['Id']}: ambiguous link {text}")
        for link in row['LinkTarget']:
            kind, target = link.split(':')
            ref({'entry':'Entry','room':'Room','character':'Character','action':'DialogueActionConfig'}[kind], int(target), f"Plot {row['Id']}")
    for row in result['Record']:
        ref('DeviceConfig', row['Device'], f"Record {row['Id']}")
        lines = sorted((line for line in result['Plot'] if line['Group'] == row['Plot']), key=lambda line: line['Order'])
        if not lines or any(line['At'] >= row['Duration'] for line in lines): errors.append(f"Record {row['Id']}: invalid timed lines")
    if errors: raise ValueError('\n'.join(errors))
    (output/'Content.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    (code_output/'V24Rows.cs').write_text('// Generated from the supplied workbook schema by Tools/import_v24.py.\nusing System;\nnamespace Castle.V24\n{\n'+'\n'.join(classes)+'\n}\n',encoding='utf-8')
    print({k:len(v) for k,v in result.items() if isinstance(v,list)})
if __name__ == '__main__': main()
