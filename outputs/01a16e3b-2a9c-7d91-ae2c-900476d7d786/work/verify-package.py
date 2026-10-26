from pathlib import Path
import csv, json, zipfile, collections, xml.etree.ElementTree as ET
from PIL import Image

base = Path(__file__).resolve().parent.parent
root = base / 'thuc-hanh'
book = root / 'ThucHanh_KHTN6_Li_Hoa_30Bai.xlsx'
with (root / 'ThucHanh.csv').open(encoding='utf-8-sig', newline='') as f:
    lessons = list(csv.DictReader(f))
with (root / 'BuocThucHanh.csv').open(encoding='utf-8-sig', newline='') as f:
    steps = list(csv.DictReader(f))
assert len(lessons) == 30 and len(steps) == 90
assert len({r['ThucHanhID'] for r in lessons}) == 30
counts = collections.Counter(r['ChuongID'] for r in lessons)
assert len(counts) == 10 and set(counts.values()) == {3}, counts
ids = {r['ThucHanhID'] for r in lessons}
assert set(r['ThucHanhID'] for r in steps) == ids
assert set(collections.Counter(r['ThucHanhID'] for r in steps).values()) == {3}
for row in lessons:
    assert all(str(value).strip() for value in row.values()), row['ThucHanhID']
    json.loads(row['CauHinhJSON'])
    for field in ['AnhDaiDien', 'AnhHuongDan']:
        with Image.open(root / row[field]) as image:
            assert image.size == (900, 560)
            image.verify()
assert len(list((root/'images').glob('*.png'))) == 60
with zipfile.ZipFile(book) as z:
    media = [n for n in z.namelist() if n.startswith('xl/media/') and n.endswith('.png')]
    assert len(media) == 60, len(media)
    ns = {'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
    workbook = ET.fromstring(z.read('xl/workbook.xml'))
    assert [s.attrib['name'] for s in workbook.findall('s:sheets/s:sheet',ns)] == ['ThucHanh','BuocThucHanh','HinhAnh','HuongDan']
    for i,expected in [(1,31),(2,91),(3,31)]:
        tree=ET.fromstring(z.read(f'xl/worksheets/sheet{i}.xml'))
        assert len(tree.findall('s:sheetData/s:row',ns)) == expected
    assert not any(b'<c t="e"' in z.read(n) for n in z.namelist() if n.startswith('xl/worksheets/') and n.endswith('.xml'))
package=base/'ThucHanh_KHTN6_30Bai_KemAnh.zip'
with zipfile.ZipFile(package,'w',zipfile.ZIP_DEFLATED) as z:
    for name in [book.name,'ThucHanh.csv','BuocThucHanh.csv','manifest.json','README.txt']:
        z.write(root/name,name)
    for image in sorted((root/'images').glob('*.png')):
        z.write(image,'images/'+image.name)
with zipfile.ZipFile(package) as z:
    assert len(z.namelist()) == 65
    assert z.testzip() is None
print(json.dumps({'lessons':len(lessons),'chapters':dict(counts),'steps':len(steps),'embedded_images':len(media),'zip_files':65,'workbook_bytes':book.stat().st_size,'package_bytes':package.stat().st_size},ensure_ascii=False))
