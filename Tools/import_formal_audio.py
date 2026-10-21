"""Import delivered sound archives by exact clip basename; never overwrite differing assets."""
import hashlib
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    clips = []
    for filename, bus in [('CV.zip', 'CV'), ('音效包.zip', 'SFX')]:
        with zipfile.ZipFile(Path('C:/Users/kk/Downloads') / filename) as archive:
            for entry in archive.infolist():
                if entry.is_dir():
                    continue
                name = entry.filename
                if not entry.flag_bits & 0x800:
                    try:
                        name = name.encode('cp437').decode('gbk')
                    except (UnicodeError, LookupError):
                        pass
                source = Path(name)
                if source.suffix.lower() not in ('.wav', '.mp3', '.ogg') or source.name.startswith('._'):
                    continue
                target = ROOT / 'Assets/Castle/Art/V24/audio' / bus / source.name
                data = archive.read(entry)
                if target.exists() and target.read_bytes() != data:
                    raise ValueError(f'Different existing audio: {target}')
                target.parent.mkdir(parents=True, exist_ok=True)
                if not target.exists():
                    target.write_bytes(data)
                clips.append(dict(Key=source.stem, Bus=bus, Path=target.relative_to(ROOT).as_posix(), Sha256=hashlib.sha256(data).hexdigest()))
    keys = [clip['Key'] for clip in clips]
    if len(keys) != len(set(keys)):
        raise ValueError('Duplicate audio basename')
    content = json.loads((ROOT / 'SourceData/Formal20261019/GeneratedContent.json').read_text(encoding='utf-8'))
    missing = sorted({plot['Sfx'] for plot in content['Plot'] if plot['Sfx']} - set(keys))
    report = dict(Clips=clips, Missing=missing)
    (ROOT / 'SourceData/Formal20261019/AudioImport.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'Imported {len(clips)} clips; missing keys: {missing}')
    if missing:
        raise ValueError('Unresolved plot audio')


if __name__ == '__main__':
    main()
