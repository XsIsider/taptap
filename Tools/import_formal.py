"""Read the delivered formal workbooks and companion policies without changing sources."""
import argparse
import json
from pathlib import Path
import openpyxl
from import_v24 import convert, pascal

ROOT = Path(__file__).resolve().parents[1]


def normalize(value):
    if isinstance(value, dict):
        return {pascal(key): normalize(item) for key, item in value.items()}
    if isinstance(value, list):
        return [normalize(item) for item in value]
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=ROOT / 'SourceData/Formal20261020')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    args.output = args.output or args.source / 'GeneratedContent.json'
    result = {'Formal': True}
    for path in sorted((args.source / '配置表').glob('*.xlsx')):
        name = 'PlotChoice' if path.name.startswith('Plot_Choice') else path.name.split('_')[0]
        workbook = openpyxl.load_workbook(path, read_only=True, data_only=True)
        rows = list(workbook.active.values)
        columns = [(i, pascal(key), rows[1][i]) for i, key in enumerate(rows[0]) if key and not key.startswith('##')]
        result[name] = [{key: convert(row[i], kind) for i, key, kind in columns}
                        for row in rows[5:] if not str(row[0] or '').startswith('##') and row[1]]
        workbook.close()
        assert len({row['Id'] for row in result[name]}) == len(result[name]), name
    binding = json.loads((args.source / 'Unity绑定_正式剧情.json').read_text(encoding='utf-8-sig'))
    result.update(normalize(binding))
    result['PlotMeta'] = [dict(Id=int(key), **normalize(value)) for key, value in binding['PlotMeta'].items()]
    result['HotspotBinding'] = []
    result['InvitationEntry'] = 300001
    result['TutorialTape'] = next(row['Id'] for row in result['Entry'] if row['Record'] == 620001)
    result['GameSettings'].update(ArrivalRoom=100002, TapeTutorialEvent=0, IntroSoundPath=720003)
    for action in result['DialogueActionConfig']:
        action.update(Start='00:00', End='24:00', Need=[], ReturnRoom=100002)
    for day in result['ControlRoomPolicy']['Days']:
        day['MapEntry'] = day['Mode'] == 'normal'
    result['FinaleConfig'].update(Action=1500002, RequiredEntries=[], RequiredGoals=[], MustExistRecords=[])
    result['SleepPolicy'].update(LastDay=4, Wake='10:00', Requirements=[], Day3Ghost=dict(Day=3, Event=920076, OnceGoal=1920076, Start='23:00'))
    # 人工确认覆盖包内的播放限制；原始 JSON 保持不动。
    for gate in result['PlaybackGates']:
        if gate.get('Event') == 920007:
            gate['AllowEarlyExit'] = True
    result['ConversationGroups'] = [
        dict(Id='banquet', AfterEvent=920033, Events=[920034, 920035, 920036, 920037, 920038, 920039], Ordered=[920034, 920035], Clocks=['18:45', '18:50'], Day=2),
        dict(Id='report', AfterEvent=920059, RequiredEvent=920058, Events=[920060, 920061, 920062, 920063, 920064], Ordered=[920060, 920061, 920062], Clocks=['11:10', '11:20', '11:30'], Day=3)]
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({key: len(result[key]) for key in ('Character', 'Room', 'Plot', 'PlotChoice', 'Event', 'Record', 'Puzzle', 'Entry')}, ensure_ascii=False))


if __name__ == '__main__':
    main()
