const fs=require('node:fs'),path=require('node:path');
const text=fs.readFileSync('Assets/Scenes/HomePage.unity','utf8');
const docs=[...text.matchAll(/^--- !u!(\d+) &(\d+)( stripped)?\r?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>({type:m[1],id:m[2],stripped:!!m[3],body:m[4]}));
const map=new Map(docs.map(x=>[x.id,x]));
function chain(id){const result=[];for(let n=0;n<12&&id&&id!=='0';n++){const d=map.get(id);if(!d){result.push({id,unresolved:true});break;}const go=map.get(d.body.match(/m_GameObject: \{fileID: (\d+)\}/)?.[1]);result.push({transform:id,gameObject:go?.id,name:go?.body.match(/m_Name: ([^\r\n]+)/)?.[1],active:go?.body.match(/m_IsActive: (\d)/)?.[1]});id=d.body.match(/m_Father: \{fileID: (\d+)\}/)?.[1];}return result;}
const results=[];
for(const d of docs.filter(d=>d.type==='1'&&/m_Name: (txtFullname|txtRole)\r?\n/.test(d.body))){const rt=docs.find(x=>['224','4'].includes(x.type)&&x.body.includes('m_GameObject: {fileID: '+d.id+'}'));const comps=docs.filter(x=>x.type==='114'&&x.body.includes('m_GameObject: {fileID: '+d.id+'}'));results.push({object:d.id,name:d.body.match(/m_Name: ([^\r\n]+)/)?.[1],chain:chain(rt?.id),components:comps.map(x=>({class:x.body.match(/m_EditorClassIdentifier: ([^\r\n]+)/)?.[1],text:x.body.match(/m_Text: ([^\r\n]+)/)?.[1]}))});}
fs.writeFileSync(path.join(__dirname,'profile-bindings.json'),JSON.stringify(results,null,2));console.log(JSON.stringify(results));
