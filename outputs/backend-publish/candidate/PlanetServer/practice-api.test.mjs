import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

function fixture(main='Code.gs') {
  const headers=['ThucHanhID','MonID','ChuongID','TenChuong','ThuTu','TenBai','MucTieu','LoaiMoPhong','DungCu','CauHoiDuDoan','HuongDanTomTat','KetQuaMongDoi','CauHoiKetLuan','DapAnThamKhao','GoiYAI','CauHinhJSON','AnhDaiDien','AnhHuongDan','ThoiLuongPhut','TrangThai','AnToan'];
  const lessons=[headers];
  const steps=[['BuocID','ThucHanhID','ThuTu','TenBuoc','ThaoTacHocSinh','HienTuongQuanSat','DieuKienHoanThanh','PhanHoiKhiSai']];
  for (const subject of ['PHY','CHM']) for(let c=1;c<=5;c++) for(let n=1;n<=3;n++) {
    const chapter='CH_'+subject+'_'+String(c).padStart(2,'0');
    const id='TH_'+subject+'_'+String(c).padStart(2,'0')+'_'+String(n).padStart(2,'0');
    lessons.push([id,subject==='PHY'?'Vật lý':'Hóa học',chapter,'Chương '+c,n,'Bài '+n,'Mục tiêu','DoChieuDai','Thước','Dự đoán?','Thao tác','Kết quả','Vì sao?','Giải thích','Hãy quan sát','{"answer":8}',
      'https://drive.google.com/file/d/cover-file-id/view?usp=sharing','https://drive.google.com/file/d/guide-file-id/view',8,'Đã duyệt','Mô phỏng ảo']);
    for(let step=3;step>=1;step--)steps.push([id+'_B'+step,id,step,'Bước '+step,'Kéo vật','Quan sát','Đúng vị trí','Thử lại']);
  }
  const data={ThucHanh:lessons,BuocThucHanh:steps};
  const context=vm.createContext({SpreadsheetApp:{openById:()=>({getSheetByName:name=>data[name]?{getDataRange:()=>({getValues:()=>data[name]})}:null})},
    ContentService:{MimeType:{JSON:'json'},createTextOutput:text=>({setMimeType:()=>JSON.parse(text)})}});
  vm.runInContext(fs.readFileSync(new URL('../AppsScript/'+main,import.meta.url),'utf8'),context);
  vm.runInContext(fs.readFileSync(new URL('../AppsScript/ThucHanh.gs',import.meta.url),'utf8'),context);
  return {data,context,get:parameter=>context.doGet({parameter})};
}

for(const main of ['Code.gs','Ma-da-sua.gs'])test(`${main} practice routes read all ten chapters independently and preserve other GET routes`,()=>{
  const {get,context,data}=fixture(main);
  const before=JSON.stringify(data);
  for(const subject of ['PHY','CHM'])for(let n=1;n<=5;n++) {
    const chapter='CH_'+subject+'_'+String(n).padStart(2,'0');
    const result=get({action:'practiceList',chapterId:chapter});
    assert.equal(result.success,true);assert.equal(result.count,3);
    assert.ok(result.lessons.every(l=>l.chapterId===chapter));
    assert.deepEqual(result.lessons.map(l=>l.order),[1,2,3]);
  }
  context.getHistory=()=>({unchanged:'history'}); assert.equal(get({action:'history'}).unchanged,'history');
  context.planetGetProgressRank_=()=>({unchanged:'rank'}); assert.equal(get({action:'getProgressRank'}).unchanged,'rank');
  context.getEssayQuestions=()=>({unchanged:'essay'}); assert.equal(get({action:'essayQuestions'}).unchanged,'essay');
  context.getTheory=()=>({unchanged:'theory'}); assert.equal(get({chapterId:'CH_PHY_01'}).unchanged,'theory');
  assert.equal(JSON.stringify(data),before);
});

test('unapproved lessons cannot be accessed by list or by guessed ID',()=>{
  const {get,data}=fixture(); data.ThucHanh[1][19]='Chờ duyệt';data.ThucHanh[2][19]='Ẩn';
  assert.equal(get({action:'practiceList',chapterId:'CH_PHY_01'}).count,1);
  assert.equal(get({action:'practiceDetail',lessonId:'TH_PHY_01_01'}).success,false);
  assert.equal(get({action:'practiceDetail',lessonId:'TH_PHY_01_02'}).success,false);
  data.ThucHanh[3][19]='Chờ duyệt';const empty=get({action:'practiceList',chapterId:'CH_PHY_01'});
  assert.equal(empty.success,true);assert.equal(empty.count,0);assert.match(empty.message,/Đã duyệt/);
});

test('detail joins only the selected lesson, orders steps and supplies Unity-compatible config and images',()=>{
  const {get}=fixture();const result=get({action:'practiceDetail',lessonId:'TH_PHY_01_01'});
  assert.equal(result.success,true);assert.equal(result.stepCount,3);
  assert.deepEqual(result.steps.map(s=>s.order),[1,2,3]);
  assert.ok(result.steps.every(s=>s.id.startsWith('TH_PHY_01_01_B')));
  assert.equal(result.lesson.config.answer,8);assert.equal(JSON.parse(result.lesson.configJson).answer,8);
  assert.equal(result.lesson.imageFileId,'cover-file-id');assert.equal(result.lesson.imagesConfigured,true);
  assert.equal(result.lesson.imageUrl,'https://drive.google.com/uc?export=download&id=cover-file-id');
});

test('invalid subjects, chapters, duplicate IDs and malformed scoped config report explicit failures',()=>{
  const {get,data}=fixture();
  assert.equal(get({action:'practiceList'}).success,false);
  assert.equal(get({action:'practiceList',chapterId:'CH_PHY_01',subject:'Hóa học'}).success,false);
  assert.equal(get({action:'practiceList',chapterId:'CH_PHY_01',subject:'Vật lí'}).success,true);
  data.ThucHanh[1][15]='{bad json';
  assert.match(get({action:'practiceList',chapterId:'CH_PHY_01'}).message,/CauHinhJSON/);
  assert.equal(get({action:'practiceList',chapterId:'CH_CHM_01'}).success,true);
  data.ThucHanh[1][15]='{}';data.ThucHanh.push([...data.ThucHanh[1]]);
  assert.match(get({action:'practiceList',chapterId:'CH_PHY_01'}).message,/Trùng ThucHanhID/);
});

test('missing headers or steps and duplicate step order are not silently accepted',()=>{
  const {get,data}=fixture();data.BuocThucHanh[1][2]=1;
  assert.match(get({action:'practiceDetail',lessonId:'TH_PHY_01_01'}).message,/ThuTu/);
  data.BuocThucHanh=data.BuocThucHanh.filter((r,i)=>i===0||r[1]!=='TH_PHY_01_01');
  assert.match(get({action:'practiceDetail',lessonId:'TH_PHY_01_01'}).message,/chưa có bước/);
  data.ThucHanh[0][15]='ConfigWrong';
  assert.match(get({action:'practiceList',chapterId:'CH_PHY_01'}).message,/thiếu cột CauHinhJSON/);
});

test('image URL conversion preserves resource keys, refuses local paths and does not fetch or change permissions',()=>{
  const {context}=fixture();
  const drive=context.practiceImage_('https://drive.google.com/file/d/test-file/view?resourcekey=key123&usp=sharing');
  assert.equal(drive.url,'https://drive.google.com/uc?export=download&id=test-file&resourcekey=key123');
  assert.equal(context.practiceImage_('images/example.png').ready,false);
  assert.equal(context.practiceImage_('javascript:alert(1)').url,'');
  assert.equal(context.practiceImage_('https://example.com/image.png').url,'https://example.com/image.png');
  assert.equal(context.practiceImage_('https://drive.google.com/drive/folders/example').ready,false);
});
