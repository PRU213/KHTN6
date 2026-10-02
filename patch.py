import codecs

with codecs.open('D:/code game/KHTN6/Assets/Editor/Bean1Setup.cs', 'r', 'utf-8-sig') as f:
    content = f.read()

content = content.replace('objName == "d?t" || objName == "dat" || objName == "nen_dat" || (w > 75 && w < 700 && h > 55 && h < 250)',
                          'objName.Contains("\\u0111\\u1EA5t") || objName.Contains("dat") || objName.Contains("nen_dat") || (w > 75 && w < 700 && h > 55 && h < 250)')

content = content.replace('// Player (nhAAA-n diAA?n', '// Player')

# Mây 
if 'CloudMove cm =' not in content:
    content = content.replace('// CAA?ng (Portal)', 
'''// Mây (CloudMove)
            if (objName.Contains("m\\u00E2y") || objName.Contains("may"))
            {
                CloudMove cm = img.GetComponent<CloudMove>();
                if (cm == null) cm = img.gameObject.AddComponent<CloudMove>();
                EditorUtility.SetDirty(cm);
                continue;
            }

            // CAA?ng (Portal)''')

with codecs.open('D:/code game/KHTN6/Assets/Editor/Bean1Setup.cs', 'w', 'utf-8-sig') as f:
    f.write(content)
