const fs=require('node:fs'), path=require('node:path');
const wanted=/Assembly-CSharp::(HomeMenuController|HomeHistoryController|HistoryManager|RankManager|LoginController|GoogleSheetAPI)$/;
const files=[];
function walk(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())walk(p);else if(p.endsWith('.prefab.meta'))files.push(p);}}
walk('Assets');
const prefabs=new Map(files.map(f=>[fs.readFileSync(f,'utf8').match(/^guid: (.+)$/m)?.[1],f.slice(0,-5)]));
function docs(file){const text=fs.readFileSync(file,'utf8');return Array.from(text.matchAll(/^--- !u!(\d+) &(\d+)( stripped)?\r?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm),m=>({id:m[2],type:m[1],stripped:!!m[3],body:m[4],file,line:text.slice(0,m.index).split('\n').length}));}
const scene=docs(process.argv[2] || 'Assets/Scenes/HomePage.unity');
const result=[];
function summarize(items,instance){
  for(const d of items){
    const cls=d.body.match(/m_EditorClassIdentifier: (.+)/)?.[1];
    if(!cls||!wanted.test(cls)||d.stripped)continue;
    const goId=d.body.match(/m_GameObject: \{fileID: (\d+)\}/)?.[1];
    const go=items.find(x=>x.id===goId);
    const overrides=instance?Array.from(instance.body.matchAll(/- target: \{fileID: (\d+), guid: [^\n]+\}\r?\n\s+propertyPath: ([^\r\n]+)\r?\n\s+value:([^\r\n]*)\r?\n\s+objectReference: ([^\r\n]+)/g)).filter(m=>m[1]===d.id).map(m=>({property:m[2],value:m[3].trim(),reference:m[4]})):[];
    result.push({class:cls.split('::')[1],object:go?.body.match(/m_Name: ([^\r\n]+)/)?.[1],file:d.file,line:d.line,id:d.id,instance:instance?.id,active:go?.body.match(/m_IsActive: (\d)/)?.[1],fields:d.body.split(/\r?\n/).filter(l=>/^  [a-zA-Z]/.test(l)&&!/^  m_|^  Mono/.test(l)).join('\n'),overrides});
  }
}
summarize(scene);
for(const instance of scene.filter(d=>d.type==='1001')){
  const guid=instance.body.match(/m_SourcePrefab: \{fileID: \d+, guid: (\w+)/)?.[1];
  const file=prefabs.get(guid);if(file)summarize(docs(file),instance);
}
fs.writeFileSync(path.join(__dirname,process.argv[3] || 'account-scene-results.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
