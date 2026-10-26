import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import sharp from 'sharp';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';

const out=path.resolve(import.meta.dirname,'../thuc-hanh');
await fs.mkdir(path.join(out,'images'),{recursive:true});
await fs.mkdir(path.join(out,'../font-cache'),{recursive:true});
const fontConfig=path.join(out,'../fontconfig.xml');
await fs.writeFile(fontConfig,`<?xml version="1.0"?><!DOCTYPE fontconfig SYSTEM "fonts.dtd"><fontconfig><dir>C:/Windows/Fonts</dir><cachedir>${path.join(out,'../font-cache').replaceAll('\\','/')}</cachedir></fontconfig>`);
process.env.FONTCONFIG_FILE=fontConfig;
const chapters={PHY:['Mở đầu và Các phép đo','Lực và Tác dụng của lực','Lực ma sát và Lực cản','Năng lượng và sự truyền năng lượng','Trái Đất và Bầu trời'],CHM:['An toàn trong phòng thực hành','Các thể của chất và Sự chuyển thể','Oxygen và Không khí','Vật liệu, nhiên liệu, nguyên liệu, lương thực - thực phẩm','Hỗn hợp, dung dịch và Phương pháp tách chất']};
const src={forces:'https://phet.colorado.edu/en/simulations/forces-and-motion-basics',energy:'https://phet.colorado.edu/en/simulations/energy-forms-and-changes',states:'https://phet.colorado.edu/en/simulations/states-of-matter-basics',moon:'https://science.nasa.gov/moon/moon-phases/',day:'https://nightsky.jpl.nasa.gov/media/documents/resources/MoonRotate.pdf',mix:'https://edu.rsc.org/experiments/separating-sand-and-salt-by-filtering-and-evaporation/386.article',air:'https://scied.ucar.edu/learning-zone/atmosphere/what-is-atmosphere',safety:'https://www.acs.org/education/policies/middle-and-high-school-chemistry/safety.html'};
const lessons=[];
function add(mon,ch,n,title,type,goal,tools,predict,steps,expect,question,answer,config,source=''){
 const id=`TH_${mon}_${String(ch).padStart(2,'0')}_${String(n).padStart(2,'0')}`;
 lessons.push({id,mon,ch,n,title,type,goal,tools,predict,steps,expect,question,answer,config,source});
}
// All actions below describe virtual controls, not instructions for a physical lab.
add('PHY',1,1,'Đo chiều dài khi vật không đặt ở vạch 0','DoChieuDai','Đặt thước song song với vật; tính chiều dài từ hai số đọc.','Thước 0–15 cm, chiếc bút, hai mốc kéo thả','Đầu bút ở 2 cm, cuối bút ở 10 cm: bút dài bao nhiêu?',[
 ['Đặt thước','Kéo thước song song với bút, đưa hai mốc đến hai đầu bút.','Hai mốc khớp hai đầu vật.','Thước song song; mốc đầu 2 cm, mốc cuối 10 cm.','Đặt thước sát và song song với bút.'],
 ['Đọc hai đầu','Đọc số 2 cm và 10 cm trên thước ảo.','Vị trí đầu và cuối khác nhau.','Ghi đúng hai số đọc.','Chưa thể dùng riêng số ở đầu cuối làm chiều dài.'],
 ['Tính chiều dài','Nhập chiều dài và chọn đơn vị cm.','10 − 2 = 8 cm.','Nhập 8 cm.','Lấy số đọc cuối trừ số đọc đầu.']
 ],'Bút dài 8 cm.','Vì sao không thể kết luận bút dài 10 cm?','10 cm là vị trí cuối trên thước; chiều dài là 10 − 2 = 8 cm.',{startCm:2,endCm:10,answer:8,unit:'cm',tolerance:0.1});
add('PHY',1,2,'Cân khối lượng bằng cân thăng bằng','CanKhoiLuong','Xác định khối lượng vật qua tổng khối lượng quả cân khi cân bằng.','Cân hai đĩa, vật mẫu, quả cân 50 g và 100 g','Hai đĩa cân ngang nhau thì ta biết điều gì?',[
 ['Đặt vật','Kéo vật mẫu lên đĩa trái.','Đĩa trái hạ xuống.','Vật nằm trên đĩa trái.','Đặt vật trọn trên đĩa cân.'],
 ['Chọn quả cân','Thêm quả 100 g và 50 g vào đĩa phải.','Đòn cân nằm ngang.','Tổng quả cân bằng 150 g.','So sánh độ cao hai đĩa để điều chỉnh.'],
 ['Đọc kết quả','Cộng khối lượng hai quả cân và nhập kết quả.','Vật có khối lượng 150 g.','Nhập 150 g.','Cộng tất cả quả cân trên đĩa phải.']
 ],'Vật mẫu có khối lượng 150 g.','Cân bằng cho biết gì về khối lượng hai bên?','Trong mô hình cân hai đĩa lý tưởng, khối lượng hai bên bằng nhau.',{massG:150,weightsG:[50,100],answer:150,unit:'g'});
add('PHY',1,3,'Đo thể tích hòn đá bằng nước dâng','DoTheTich','Suy ra thể tích vật không thấm nước từ độ tăng thể tích nước.','Ống đong 100 mL, nước, hòn đá ảo','Nước tăng từ 40 mL lên 70 mL: hòn đá chiếm bao nhiêu thể tích?',[
 ['Đọc mức đầu','Đặt mắt ảo ngang mực nước và đọc mức ban đầu.','Mức nước ban đầu 40 mL.','Ghi 40 mL.','Đọc ngang mực nước.'],
 ['Đặt đá vào','Kéo đá chìm hoàn toàn trong ống đong ảo.','Mực nước lên 70 mL, không tràn.','Đá chìm hoàn toàn; ghi 70 mL.','Đá cần chìm hoàn toàn, không có bọt khí bám.'],
 ['Tính thể tích','Lấy mức sau trừ mức trước.','70 − 40 = 30 mL.','Nhập 30 mL hoặc 30 cm³.','1 mL bằng 1 cm³.']
 ],'Hòn đá có thể tích 30 cm³.','Mực nước sau có phải thể tích riêng của đá không?','Không; thể tích đá bằng phần nước tăng thêm: 70 − 40 = 30 mL = 30 cm³.',{beforeMl:40,afterMl:70,answer:30,unit:'cm3'});
add('PHY',2,1,'Lực làm lò xo biến dạng','LucBienDang','Quan sát lò xo biến dạng khi lực kéo thay đổi trong giới hạn mô hình.','Lò xo ảo, tay kéo, thước','Khi tăng lực kéo vừa phải, độ dài lò xo thay đổi thế nào?',[
 ['Quan sát ban đầu','Đưa lực kéo về 0 bằng thanh trượt.','Lò xo ở chiều dài ban đầu.','Lực bằng 0.','Ghi chiều dài trước khi kéo.'],
 ['Tăng lực','Chuyển thanh trượt qua ba mức lực nhỏ.','Lò xo dài hơn khi lực kéo tăng.','Thử đủ ba mức và ghi chiều dài.','Giữ cùng một lò xo để so sánh.'],
 ['Thả lực','Đưa lực về 0 và chọn nhận xét.','Lò xo trở lại gần chiều dài ban đầu trong mô hình đàn hồi.','Chọn lực gây biến dạng.','Đây là biến dạng đàn hồi trong giới hạn mô phỏng.']
 ],'Lực kéo có thể làm lò xo biến dạng.', 'Thí nghiệm này cho thấy tác dụng nào của lực?','Lực có thể làm vật biến dạng; khi bỏ lực, lò xo trong giới hạn đàn hồi trở về hình dạng ban đầu.',{forceLevelsN:[0,1,2],elasticRange:true},src.forces);
add('PHY',2,2,'Hai lực đẩy ngược chiều lên xe','HaiLucNguocChieu','So sánh chuyển động từ trạng thái nghỉ khi hai lực cân bằng hoặc không cân bằng.','Xe ảo, hai mũi tên lực, đường không ma sát','Hai lực 10 N ngược chiều tác dụng lên xe đang nghỉ: xe có tự chuyển động không?',[
 ['Cân bằng lực','Đặt lực trái và phải cùng 10 N.','Xe ban đầu đứng yên vẫn đứng yên.','Hai lực bằng nhau, ngược chiều.','So cả độ lớn và chiều của hai lực.'],
 ['Tạo chênh lệch','Đổi lực sang phải thành 20 N, giữ lực còn lại 10 N.','Xe tăng tốc sang phải.','Lực phải lớn hơn lực trái.','Theo dõi sự thay đổi tốc độ, không chỉ vị trí.'],
 ['So sánh','Chọn nhận xét đúng cho hai tình huống.','Lực tổng hợp bằng 0 hoặc 10 N sang phải.','Phân biệt đúng hai trường hợp.','Mô hình này bỏ qua ma sát.']
 ],'Hai lực không cân bằng làm thay đổi chuyển động của xe.','Xe đang nghỉ với hai lực cân bằng có tăng tốc không?','Không trong mô hình này; tổng lực bằng 0. Khi lực phải lớn hơn, xe tăng tốc sang phải.',{leftN:10,rightLevelsN:[10,20],friction:false,initialSpeed:0},src.forces);
add('PHY',2,3,'Khối lượng và lực hút của Trái Đất','TrongLuc','Phân biệt đơn vị khối lượng và lực; đọc số chỉ lực kế ảo.','Lực kế 0–5 N, vật 100 g và 200 g','Trên cùng một nơi của Trái Đất, vật nặng gấp đôi có trọng lượng thế nào?',[
 ['Treo vật thứ nhất','Chọn vật 100 g trên lực kế ảo.','Lực kế chỉ khoảng 1 N.','Đọc 1 N.','Lực kế đo lực bằng N.'],
 ['Đổi vật','Thay bằng vật 200 g ở cùng vị trí.','Lực kế chỉ khoảng 2 N.','Đọc 2 N.','Đừng nhầm g với N.'],
 ['Kết luận','Ghép 100 g với 1 N và 200 g với 2 N.','Khối lượng và trọng lượng là hai đại lượng khác nhau.','Ghép đúng cả hai cặp.','Các số là gần đúng với g ≈ 10 N/kg.']
 ],'Ở cùng nơi, vật 200 g có trọng lượng xấp xỉ gấp đôi vật 100 g.','Vì sao không ghi số chỉ lực kế là 200 g?','Lực kế đo lực bằng N, còn g là đơn vị khối lượng.',{massesG:[100,200],gravityNPerKg:10,weightsN:[1,2]},src.forces);
add('PHY',3,1,'Xe trượt trên ba mặt đường','MaSatBeMat','So sánh quãng đường trượt khi giữ cùng vận tốc ban đầu.','Xe, mặt kính, gỗ và thảm trong mô hình','Xe sẽ dừng sớm nhất trên mặt nào?',[
 ['Giữ điều kiện','Đặt cùng xe và cùng vận tốc đầu cho ba lượt.','Chỉ thay đổi loại mặt đường.','Cùng khối lượng và vận tốc đầu.','So sánh cần giữ các yếu tố khác như nhau.'],
 ['Thử ba mặt','Bấm thả xe trên từng bề mặt.','Trong cấu hình này, xe dừng sớm nhất trên thảm.','Chạy đủ ba lượt.','Quan sát quãng đường đến lúc dừng.'],
 ['Xếp thứ tự','Xếp lực cản ma sát từ nhỏ đến lớn.','Kính, gỗ, thảm theo cấu hình mô phỏng.','Xếp đúng thứ tự đã quan sát.','Thứ tự này áp dụng cho vật liệu đã cấu hình, không phải mọi bề mặt thực tế.']
 ],'Ma sát lớn hơn làm xe dừng sau quãng đường ngắn hơn trong cùng điều kiện ban đầu.','Vì sao phải dùng cùng vận tốc ban đầu?','Để thay đổi quãng đường chủ yếu phản ánh sự khác nhau của ma sát.',{surfaces:['kinh','go','tham'],initialSpeed:2,frictionLevels:[0.1,0.3,0.6]},src.forces);
add('PHY',3,2,'Hình dạng và lực cản của nước','LucCanChatLong','Quan sát lực cản khác nhau khi các hình được kéo với cùng tốc độ.','Bể nước ảo, ba vật cùng diện tích cản lớn nhất, lực kế','Vật thuôn có cần lực kéo nhỏ hơn vật mặt tù không?',[
 ['Thiết lập','Chọn cùng tốc độ kéo và độ sâu trong nước.','Các điều kiện so sánh được cố định.','Cùng tốc độ và diện tích cản lớn nhất.','Không đồng thời đổi tốc độ và hình dạng.'],
 ['Kéo từng vật','Chọn vật mặt tù, hình cầu rồi hình thuôn.','Lực kế hiển thị ba lực cản trong mô hình.','Ghi đủ ba số chỉ.','So lực kéo khi vật đã chuyển động đều.'],
 ['Chọn hình','Chọn hình cần lực kéo nhỏ nhất.','Vật thuôn có lực cản nhỏ nhất trong cấu hình.','Chọn vật thuôn.','Hình dạng có ảnh hưởng đến lực cản.']
 ],'Hình thuôn giảm lực cản trong mô hình so sánh.','Vì sao phải kéo các vật với cùng tốc độ?','Vì tốc độ cũng ảnh hưởng lực cản; giữ tốc độ giúp so sánh ảnh hưởng của hình dạng.',{speed:1,shapes:['mat_tu','cau','thuon'],dragN:[3,2,1]},src.forces);
add('PHY',3,3,'Ma sát có ích hay có hại','MaSatDoiSong','Nhận biết tác dụng của ma sát trong các tình huống đời sống.','Thẻ giày, phanh xe, bản lề và dầu bôi trơn ảo','Có phải mọi ma sát đều cần loại bỏ?',[
 ['Quan sát tình huống','Mở các thẻ đế giày, phanh xe và bản lề.','Ma sát giúp bám đường, hãm xe hoặc gây khó chuyển động.','Xem đủ ba thẻ.','Xét mục đích của từng thiết bị.'],
 ['Chọn giải pháp','Ghép tăng độ bám với đế giày, tăng lực hãm với phanh, bôi trơn với bản lề.','Hiệu quả mô phỏng thay đổi.','Ghép đúng ba cặp.','Không bôi trơn bộ phận cần độ bám như má phanh.'],
 ['Giải thích','Chọn có ích hoặc có hại theo từng mục đích.','Cùng một hiện tượng có thể mang tác dụng khác nhau.','Phân loại đúng ba tình huống.','Ma sát không luôn có hại.']
 ],'Cần tăng ma sát ở nơi cần bám và giảm ma sát ở nơi cần trượt dễ.','Vì sao đế giày có rãnh nhưng bản lề thường được bôi trơn?','Giày cần độ bám; bản lề cần giảm lực cản khi quay.',{situations:['de_giay','phanh_xe','ban_le'],actions:['tang','tang','giam']},src.forces);
add('PHY',4,1,'Từ tay quay đến đèn sáng','ChuyenHoaNangLuong','Theo dõi chuỗi chuyển hóa năng lượng trong máy phát điện ảo.','Tay quay, máy phát, đèn LED, mũi tên năng lượng','Ngừng quay thì đèn trong mạch không có bộ tích trữ sẽ thế nào?',[
 ['Nối mô hình','Nối tay quay, máy phát và đèn bằng các cổng ảo.','Một chuỗi truyền năng lượng được tạo.','Nối đúng ba thiết bị.','Theo dõi từ nguồn đến nơi nhận.'],
 ['Quay tay','Tăng thanh trượt tốc độ quay.','Đèn sáng theo cấu hình máy phát.','Quay ở hai mức tốc độ.','Năng lượng đến từ thao tác quay.'],
 ['Dừng và giải thích','Dừng quay và ghép tên các dạng năng lượng.','Đèn tắt khi ngừng cấp năng lượng trong mô hình.','Ghép cơ năng → điện năng → ánh sáng và nhiệt.','Đèn không biến toàn bộ điện năng thành ánh sáng.']
 ],'Cơ năng chuyển thành điện năng rồi thành ánh sáng và nhiệt.','Đèn có tự tạo năng lượng không?','Không; đèn nhận điện năng, phát ánh sáng và tỏa nhiệt.',{storage:false,chain:['co_nang','dien_nang','anh_sang_va_nhiet']},src.energy);
add('PHY',4,2,'Giữ ấm cốc nước bằng vật cách nhiệt','TruyenNhiet','So sánh nhiệt độ sau cùng thời gian khi chỉ thay vật bọc cốc.','Hai cốc ảo, nước ấm, lớp bọc cách nhiệt, nhiệt kế','Cốc được bọc sẽ nguội nhanh hay chậm hơn?',[
 ['Đặt điều kiện','Chọn hai cốc giống nhau cùng lượng nước ở 60 °C.','Nhiệt độ đầu và môi trường như nhau.','Hai cốc cùng nhiệt độ đầu và thể tích.','Chỉ thay lớp bọc ngoài.'],
 ['Chạy thời gian','Bọc cốc B bằng lớp cách nhiệt rồi chạy đồng hồ ảo.','Cốc B giữ nhiệt tốt hơn trong mô hình.','Chạy cùng khoảng thời gian.','Không mở thêm nguồn nhiệt.'],
 ['Đọc nhiệt độ','So sánh nhiệt kế của hai cốc.','Cốc A 40 °C, cốc B 50 °C trong dữ liệu minh họa.','Chọn cốc B và nêu cách nhiệt làm chậm truyền nhiệt.','Lớp bọc không tự sinh nhiệt.']
 ],'Lớp cách nhiệt làm chậm sự truyền năng lượng ra môi trường.','Lớp bọc giữ ấm bằng cách tạo thêm nhiệt hay giảm truyền nhiệt?','Giảm tốc độ truyền nhiệt; không tự tạo năng lượng.',{initialC:60,afterC:[40,50],roomC:25,illustrativeData:true},src.energy);
add('PHY',4,3,'Chọn thiết bị ít hao phí năng lượng','NangLuongHaoPhi','So sánh năng lượng có ích và hao phí trong hai mô hình chiếu sáng.','Hai đèn ảo, sơ đồ dòng năng lượng','Hai đèn nhận cùng năng lượng có nhất thiết sáng như nhau?',[
 ['Cấp năng lượng','Cấp cùng 100 đơn vị năng lượng cho hai đèn mẫu.','Đầu vào bằng nhau.','Hai đầu vào cùng 100.','Đây là đơn vị minh họa, không phải số đo một loại đèn cụ thể.'],
 ['Quan sát đầu ra','Đọc phần ánh sáng có ích và nhiệt hao phí.','A: 20 sáng, 80 nhiệt; B: 60 sáng, 40 nhiệt.','Đọc đúng hai sơ đồ.','Tổng đầu ra mỗi đèn bằng đầu vào.'],
 ['Lựa chọn','Chọn đèn có phần năng lượng có ích lớn hơn.','Đèn B có ít hao phí hơn cho mục đích chiếu sáng.','Chọn B và giải thích.','Nhiệt được xem là hao phí đối với mục đích chiếu sáng.']
 ],'Đèn B dùng phần năng lượng có ích lớn hơn trong cấu hình giả lập.','Vì sao cần xác định mục đích trước khi nói năng lượng hao phí?','Một dạng năng lượng có thể có ích cho mục đích này nhưng không có ích cho mục đích khác.',{input:100,useful:[20,60],waste:[80,40],illustrativeData:true},src.energy);
add('PHY',5,1,'Vì sao có ngày và đêm','NgayDem','Liên hệ ngày đêm với sự tự quay của Trái Đất.','Mặt Trời, Trái Đất, điểm đánh dấu vị trí','Nếu điểm đánh dấu quay sang phía khuất sáng thì là ngày hay đêm?',[
 ['Chọn vị trí','Đánh dấu một điểm trên Trái Đất mô phỏng.','Một nửa Trái Đất được chiếu sáng.','Có điểm đánh dấu.','Nguồn sáng giữ cố định.'],
 ['Quay Trái Đất','Kéo thanh góc tự quay một vòng.','Điểm lần lượt đi qua vùng sáng và tối.','Điểm đi qua cả hai vùng.','Mô hình không dùng chuyển động của Mặt Trời quanh Trái Đất.'],
 ['Ghép thời điểm','Ghép vùng sáng với ngày và vùng tối với đêm.','Ngày đêm luân phiên theo sự tự quay.','Ghép đúng hai vùng.','Mô hình đơn giản bỏ qua chạng vạng.']
 ],'Sự tự quay của Trái Đất làm ngày đêm luân phiên.','Có phải ban đêm Mặt Trời ngừng phát sáng?','Không; vị trí quan sát quay sang phần không được Mặt Trời chiếu trực tiếp.',{sunFixed:true,rotationDegrees:[0,90,180,270,360],notToScale:true},src.day);
add('PHY',5,2,'Quan sát các pha Mặt Trăng','PhaMatTrang','Nhận biết pha Trăng do góc nhìn phần được Mặt Trời chiếu sáng.','Mặt Trời, Trái Đất, Mặt Trăng, cửa sổ góc nhìn','Pha Trăng thông thường có phải do bóng Trái Đất che lên Trăng?',[
 ['Đặt góc nhìn','Bật góc nhìn từ Trái Đất.','Mặt Trăng nhận ánh sáng Mặt Trời.','Chọn góc nhìn từ Trái Đất.','Mặt Trăng không tự phát sáng.'],
 ['Đổi vị trí','Di chuyển Trăng quanh quỹ đạo ảo.','Phần sáng nhìn thấy thay đổi.','Xem ít nhất bốn vị trí.','Không đồng nhất pha Trăng với nguyệt thực.'],
 ['Ghép pha','Ghép ảnh trăng mới, bán nguyệt và trăng tròn.','Tên pha khớp phần sáng nhìn thấy.','Ghép đúng ba ảnh.','Mô hình không mô tả nguyệt thực và không theo tỉ lệ khoảng cách.']
 ],'Các pha Trăng phụ thuộc vị trí tương đối và phần sáng nhìn từ Trái Đất.','Pha Trăng thông thường có phải là nguyệt thực không?','Không; pha Trăng là sự thay đổi phần nửa sáng nhìn thấy, còn nguyệt thực là hiện tượng khác.',{phases:['trang_moi','ban_nguyet','trang_tron'],eclipse:false,notToScale:true},src.moon);
add('PHY',5,3,'Sắp xếp các hành tinh trong Hệ Mặt Trời','HeMatTroi','Xếp thứ tự tám hành tinh từ gần Mặt Trời ra xa.','Thẻ tám hành tinh, các vị trí quỹ đạo','Trái Đất đứng thứ mấy tính từ Mặt Trời?',[
 ['Nhận diện','Mở tên trên tám thẻ hành tinh.','Tên được ghép với hình minh họa.','Xem đủ tám thẻ.','Kích thước hình và khoảng cách không theo tỉ lệ.'],
 ['Sắp thứ tự','Kéo thẻ vào vị trí từ gần đến xa Mặt Trời.','Thứ tự các thẻ được kiểm tra.','Thủy, Kim, Trái Đất, Hỏa, Mộc, Thổ, Thiên Vương, Hải Vương.','Không dùng kích thước hình để suy thứ tự.'],
 ['Xác định vị trí','Chọn Trái Đất và hai hành tinh lân cận theo thứ tự quỹ đạo.','Trái Đất là hành tinh thứ ba.','Chọn thứ ba, giữa Kim và Hỏa.','Mặt Trời là một ngôi sao, không phải một hành tinh.']
 ],'Trái Đất đứng thứ ba tính từ Mặt Trời.','Trong thứ tự quỹ đạo, hành tinh nào nằm ngay trước và sau Trái Đất?','Sao Kim nằm trước và sao Hỏa nằm sau Trái Đất theo thứ tự từ Mặt Trời ra ngoài.',{order:['Thuy','Kim','TraiDat','Hoa','Moc','Tho','ThienVuong','HaiVuong'],notToScale:true},'https://science.nasa.gov/solar-system/planets/');

add('CHM',1,1,'Chọn trang bị trước khi vào phòng thực hành','TrangBiBaoHo','Nhận biết trang bị và trang phục phù hợp trước khi thao tác.','Tủ đồ ảo, kính bảo hộ, áo choàng, giày kín, dây buộc tóc','Kính thời trang có thay thế kính bảo hộ không?',[
 ['Quan sát nhiệm vụ','Đọc yêu cầu của phòng thực hành ảo.','Các yêu cầu về mắt, quần áo và tóc được hiển thị.','Mở bảng yêu cầu.','Đọc nhiệm vụ trước khi chọn đồ.'],
 ['Chọn trang bị','Kéo kính bảo hộ, áo choàng, giày kín và buộc tóc dài cho nhân vật.','Nhân vật được trang bị phù hợp.','Chọn đủ các mục theo yêu cầu nhiệm vụ.','Không chọn dép hở hoặc kính thời trang thay kính bảo hộ.'],
 ['Kiểm tra','Bấm kiểm tra trước khi vào bàn thực hành.','Hệ thống báo các mục còn thiếu.','Không còn mục thiếu.','Trang bị không thay thế hướng dẫn của giáo viên.']
 ],'Nhân vật hoàn tất các yêu cầu trang bị của nhiệm vụ.','Vì sao không chỉ đeo kính mà bỏ qua trang phục?','Các trang bị bảo vệ những bộ phận khác nhau; cần đáp ứng đầy đủ yêu cầu của hoạt động.',{required:['kinh_bao_ho','ao_choang','giay_kin','toc_gon']},src.safety);
add('CHM',1,2,'Đọc nhãn cảnh báo trước khi chọn thao tác','NhanCanhBao','Đọc tên cảnh báo và chọn cách ứng xử phù hợp trong mô phỏng.','Ba nhãn minh họa: dễ cháy, ăn mòn, độc; bảng hành vi','Có nên nhận biết hóa chất bằng cách nếm thử không?',[
 ['Đọc nhãn','Mở ba thẻ cảnh báo có tên rõ ràng.','Tên nguy cơ hiện dưới mỗi hình minh họa.','Đọc đủ ba tên.','Hình trong bộ dữ liệu là minh họa, không thay bảng nhãn chuẩn của phòng học.'],
 ['Ghép hành vi','Ghép tránh nguồn lửa, tránh tiếp xúc trực tiếp và không nếm/ngửi trực tiếp.','Hành vi phù hợp được ghép với nguy cơ.','Ghép đúng ba cặp.','Không thử bằng hóa chất thật.'],
 ['Giải thích','Chọn lý do phải đọc nhãn trước khi sử dụng.','Nhận biết nguy cơ để tuân thủ hướng dẫn.','Chọn đọc nhãn và hỏi giáo viên khi chưa rõ.','Không suy đoán tính an toàn qua màu hoặc mùi.']
 ],'Học sinh đọc cảnh báo và chọn đúng hành vi tương ứng.','Nếu không hiểu nhãn, em nên làm gì?','Dừng thao tác và hỏi giáo viên; không tự nếm, ngửi hay chạm để thử.',{labels:['de_chay','an_mon','doc'],virtualOnly:true},src.safety);
add('CHM',1,3,'Tìm lỗi trên bàn thực hành','TimLoiAnToan','Phát hiện hành vi chưa phù hợp và chọn cách báo hoặc sửa trong mô phỏng.','Bàn ảo, cốc không nhãn, đồ ăn, chai không đậy, thẻ báo giáo viên','Đồ ăn có được đặt trên bàn thực hành không?',[
 ['Quan sát bàn','Mở góc nhìn toàn bàn trong phòng ảo.','Ba điểm cần chú ý xuất hiện.','Quan sát cốc không nhãn, đồ ăn và chai không đậy.','Không kéo hóa chất thật để xử lý.'],
 ['Đánh dấu lỗi','Bấm vào ba điểm cần xử lý.','Mỗi lỗi có thẻ giải thích.','Đánh dấu đúng ba lỗi.','Không chọn vật an toàn chỉ để tăng số lỗi.'],
 ['Chọn cách xử lý','Chọn bỏ đồ ăn ra khu vực phù hợp và báo giáo viên về đồ chứa chưa rõ.','Bàn mô phỏng chuyển về trạng thái phù hợp.','Chọn đúng hành vi ở cả ba thẻ.','Không tự đổ hoặc trộn chất không rõ.']
 ],'Đồ ăn được tách khỏi bàn; các nguy cơ chưa rõ được báo giáo viên.','Vì sao không tự đổ chất trong cốc không có nhãn?','Không biết chất gì và nguy cơ nào; cần hỏi người phụ trách trước.',{errors:['do_an','coc_khong_nhan','chai_khong_day'],virtualOnly:true},src.safety);
add('CHM',2,1,'Ba thể của chất qua mô hình hạt','BaTheCuaChat','So sánh hình dạng, thể tích và cách sắp xếp hạt trong mô hình đơn giản.','Ba bình mô phỏng rắn, lỏng, khí','Chất lỏng có giữ hình dạng riêng như chất rắn không?',[
 ['Xem ba mô hình','Chọn lần lượt rắn, lỏng và khí.','Hạt được bố trí gần cố định, gần nhưng linh động hoặc cách xa.','Xem đủ ba mô hình.','Hạt là ký hiệu phóng đại, không phải ảnh chụp.'],
 ['Đổi bình chứa','Đổi hình bình chứa trên màn hình.','Chất lỏng đổi hình theo phần bình chứa; khí chiếm không gian bình.','Thử đổi bình cho lỏng và khí.','Mô hình giữ lượng chất không đổi.'],
 ['Ghép tính chất','Ghép mỗi thể với mô tả hình dạng và thể tích.','Mỗi thẻ tính chất khớp một thể.','Ghép đúng ba thẻ.','Không đồng nhất hình dạng của bình với hình dạng riêng của chất.']
 ],'Chất rắn giữ hình dạng; chất lỏng đổi hình theo bình; chất khí lan ra không gian bình.','Vì sao nước lỏng đổi hình khi rót sang cốc khác?','Chất lỏng không có hình dạng riêng, nên nhận hình dạng phần bình chứa nó.',{states:['ran','long','khi'],particleModel:true},src.states);
add('CHM',2,2,'Theo dõi nước đá nóng chảy','NongChay','Nhận biết chuyển từ rắn sang lỏng và đoạn nhiệt độ gần như không đổi.','Nước đá tinh khiết ảo, nguồn nhiệt ảo, nhiệt kế, đồ thị','Trong lúc đá tinh khiết đang tan, nhiệt độ có tăng liên tục không?',[
 ['Đọc nhiệt độ đầu','Quan sát nước đá ở −5 °C trên màn hình.','Nước đang ở thể rắn.','Ghi −5 °C và trạng thái rắn.','Các điều kiện là áp suất khí quyển thông thường.'],
 ['Cấp nhiệt ảo','Bấm chạy mô phỏng và theo dõi lúc đá tan.','Nhiệt độ tăng đến 0 °C, gần như giữ nguyên khi rắn và lỏng cùng tồn tại.','Đánh dấu đoạn nóng chảy.','Không thực hiện gia nhiệt thật theo hình minh họa.'],
 ['Đọc đồ thị','Chọn đoạn đá tan và đoạn nước lỏng ấm lên.','Sau khi tan hết, nhiệt độ nước lỏng tăng.','Chọn đúng hai đoạn.','Phần phẳng không có nghĩa là ngừng nhận năng lượng.']
 ],'Nước đá tinh khiết tan gần 0 °C ở điều kiện mô hình.','Vì sao vẫn cấp nhiệt mà nhiệt độ có đoạn gần như không đổi?','Năng lượng được dùng cho sự chuyển thể trong mô hình, không làm nhiệt độ tăng ở đoạn đó.',{startC:-5,meltingC:0,pressure:'1 atm',pureWater:true,virtualOnly:true},src.states);
add('CHM',2,3,'Phân biệt bay hơi và sôi','BayHoiVaSoi','Phân biệt bay hơi ở mặt thoáng với sôi trong toàn khối chất lỏng.','Hai bình nước ảo ở 40 °C và 100 °C, kính phóng đại','Nước có bay hơi khi chưa sôi không?',[
 ['Xem bình 40 °C','Chọn kính phóng đại trên mặt nước.','Một số hạt thoát khỏi mặt thoáng.','Nhận diện bay hơi ở mặt nước.','Không nhìn thấy từng phân tử bằng mắt thường; đây là mô hình.'],
 ['Xem bình đang sôi','Chọn bình ở khoảng 100 °C trong mô hình áp suất 1 atm.','Bọt hơi xuất hiện trong khối nước.','Nhận diện sôi.','Giá trị sôi phụ thuộc áp suất, ở đây đã cố định.'],
 ['So sánh','Ghép hai hiện tượng với mô tả.','Bay hơi có thể diễn ra dưới nhiệt độ sôi.','Ghép đúng cả hai mô tả.','Không kết luận bay hơi chỉ xảy ra khi sôi.']
 ],'Bay hơi có thể diễn ra khi nước chưa sôi; sôi có bọt hơi trong khối nước.','Khăn ướt khô ở nhiệt độ phòng nhờ hiện tượng nào?','Nhờ sự bay hơi của nước, không cần nước đạt nhiệt độ sôi.',{temperaturesC:[40,100],pressure:'1 atm',virtualOnly:true},src.states);
add('CHM',3,1,'Ghép thành phần không khí khô','ThanhPhanKhongKhi','Nhận biết tỉ lệ gần đúng của nitrogen và oxygen trong không khí khô.','Bảng 100 ô, thẻ nitrogen, oxygen và khí khác','Oxygen có chiếm phần lớn không khí không?',[
 ['Xem bảng 100 ô','Mở chú giải từng nhóm khí.','Mỗi ô đại diện khoảng 1% thể tích.','Hiểu đơn vị phần trăm thể tích.','Đây là không khí khô, tỉ lệ đã làm tròn.'],
 ['Phân bố khí','Gán 78 ô nitrogen, 21 ô oxygen, 1 ô khí khác.','Nitrogen chiếm nhiều ô nhất.','Đủ 100 ô với tỉ lệ 78/21/1.','Không dùng ô khí khác để mô tả toàn bộ hơi nước trong không khí ẩm.'],
 ['Kết luận','Chọn khí chiếm tỉ lệ lớn nhất.','Nitrogen là thành phần lớn nhất.','Chọn nitrogen, oxygen khoảng một phần năm.','Oxygen quan trọng nhưng không phải thành phần nhiều nhất.']
 ],'Không khí khô có khoảng 78% nitrogen, 21% oxygen và 1% khí khác.','Khí cần cho hô hấp có phải khí chiếm nhiều nhất trong không khí?','Không; oxygen cần cho hô hấp nhưng nitrogen mới chiếm tỉ lệ lớn nhất.',{nitrogenPercent:78,oxygenPercent:21,otherPercent:1,dryAir:true},src.air);
add('CHM',3,2,'Oxygen và sự cháy trong mô phỏng','OxygenSuChay','Nhận biết oxygen hỗ trợ sự cháy, không phải là nhiên liệu.','Ba buồng cháy ảo, chỉ báo oxygen, cùng mẫu nhiên liệu','Oxygen là nhiên liệu hay khí hỗ trợ sự cháy?',[
 ['Giữ mẫu so sánh','Chọn cùng mẫu nhiên liệu và cùng điều kiện kích hoạt ảo.','Các buồng chỉ khác mức oxygen.','Giữ nguyên mẫu nhiên liệu.','Chỉ dùng mô phỏng, không thao tác với oxygen hoặc lửa thật.'],
 ['Chạy ba buồng','Quan sát buồng không có oxygen, không khí và giàu oxygen.','Mô hình thể hiện mức duy trì cháy khác nhau.','Xem đủ ba buồng.','Không suy công thức tốc độ cháy từ hoạt ảnh minh họa.'],
 ['Giải thích','Ghép nhiên liệu với vai trò chất cháy, oxygen với vai trò hỗ trợ.','Vai trò hai thành phần được phân biệt.','Ghép đúng hai vai trò.','Oxygen làm tăng nguy cơ cháy; không thử ngoài đời.']
 ],'Oxygen hỗ trợ sự cháy; thiếu oxygen thì mẫu không duy trì cháy trong mô hình.','Vì sao không gọi oxygen là nhiên liệu?','Trong tình huống này mẫu là chất cháy, oxygen hỗ trợ quá trình cháy, không được dùng như tên nhiên liệu.',{oxygenLevels:['khong_co','khong_khi','giau_oxygen'],qualitative:true,virtualOnly:true},src.safety);
add('CHM',3,3,'Chọn hành động giảm ô nhiễm không khí','BaoVeKhongKhi','Nhận biết nguồn phát thải và hành động giảm ô nhiễm tại nguồn.','Bản đồ khu phố ảo, thẻ giao thông, đốt rác và vệ sinh','Trồng một cây có làm mất ngay mọi khói bụi không?',[
 ['Tìm nguồn','Đánh dấu xe nổ máy lâu, đốt rác và bụi xây dựng trong tranh.','Ba nguồn được tô sáng.','Xác định đủ ba nguồn.','Không đánh đồng mọi khí nhìn thấy với cùng mức nguy hại.'],
 ['Chọn hành động','Ghép giảm nổ máy không cần thiết, không đốt rác và kiểm soát bụi.','Mũi tên phát thải minh họa giảm.','Ghép đúng ba biện pháp tại nguồn.','Chỉ số trong tranh là minh họa, không phải dự báo chất lượng không khí.'],
 ['Đánh giá','Chọn nhận xét về giới hạn của giải pháp.','Không có một hành động đơn lẻ xử lý mọi ô nhiễm.','Chọn cần phối hợp nhiều biện pháp.','Cây xanh không thay thế việc giảm phát thải.']
 ],'Các hành động tại nguồn giúp giảm phát thải trong mô hình.','Vì sao không chỉ trồng cây mà vẫn cần giảm đốt rác?','Cần giảm chất ô nhiễm ngay tại nguồn; cây xanh không loại bỏ tức thì mọi chất ô nhiễm.',{sources:['xe_no_may','dot_rac','bui'],qualitative:true},src.air);
add('CHM',4,1,'Chọn vật liệu cho từng bộ phận','ChonVatLieu','Liên hệ lựa chọn vật liệu với tính chất và mục đích sử dụng.','Thẻ kim loại, nhựa chịu nhiệt, kính; nồi, tay cầm, cửa sổ ảo','Có nên chọn cùng một vật liệu cho thân nồi và tay cầm không?',[
 ['Xem yêu cầu','Mở yêu cầu dẫn nhiệt, cách nhiệt và truyền ánh sáng.','Mỗi bộ phận có mục đích riêng.','Đọc đủ ba yêu cầu.','Không chọn chỉ theo màu sắc.'],
 ['Ghép vật liệu','Chọn kim loại cho thân nồi, vật liệu cách nhiệt phù hợp cho tay cầm, kính cho cửa sổ.','Tính chất phù hợp được đánh dấu.','Ghép đúng ba mục đích.','Không phải mọi loại nhựa đều dùng được ở nhiệt độ cao.'],
 ['Giải thích','Chọn lý do cho từng lựa chọn.','Quyết định dựa trên tính chất và điều kiện sử dụng.','Nêu đúng dẫn nhiệt, hạn chế truyền nhiệt hoặc trong suốt.','Phần này không phải hướng dẫn chế tạo dụng cụ thật.']
 ],'Lựa chọn vật liệu phải xét tính chất và điều kiện sử dụng.','Vì sao thân nồi và tay cầm có thể dùng vật liệu khác nhau?','Thân nồi cần truyền nhiệt cho thức ăn; tay cầm cần hạn chế truyền nhiệt tới tay.',{parts:['than_noi','tay_cam','cua_so'],matches:['kim_loai','cach_nhiet_phu_hop','kinh']});
add('CHM',4,2,'Phân loại nhiên liệu và cách dùng tiết kiệm','NhienLieu','Phân biệt nhiên liệu với vật liệu không dùng làm nhiên liệu trong tình huống.','Thẻ củi, than, khí thiên nhiên, cát và kính; thiết bị ảo','Mọi vật liệu đều là nhiên liệu phải không?',[
 ['Đọc thẻ','Xem tên và công dụng được mô tả trên năm thẻ.','Các ví dụ nhiên liệu và vật liệu được phân biệt.','Xem đủ năm thẻ.','Không thử đốt vật liệu thật.'],
 ['Phân loại','Kéo củi, than và khí thiên nhiên vào nhóm nhiên liệu.','Cát và kính nằm ngoài nhóm nhiên liệu của bài.','Phân loại đúng năm thẻ.','Không xếp điện vào nhiên liệu trong bài phân loại này.'],
 ['Chọn tiết kiệm','Chọn tắt thiết bị khi không dùng và giảm thất thoát năng lượng.','Mức tiêu thụ minh họa giảm.','Chọn đúng hai hành động.','Hoạt ảnh không đại diện số liệu tiêu thụ thực.']
 ],'Nhiên liệu cung cấp năng lượng qua quá trình cháy trong các ví dụ của bài.','Vì sao cát và kính không nằm trong nhóm nhiên liệu của bài?','Chúng được dùng theo tính chất vật liệu, không phải để đốt cung cấp năng lượng trong các ví dụ này.',{fuels:['cui','than','khi_thien_nhien'],notFuels:['cat','kinh'],virtualOnly:true});
add('CHM',4,3,'Đọc nhãn và bảo quản lương thực thực phẩm','BaoQuanThucPham','Đọc thông tin bảo quản thay vì quyết định an toàn chỉ qua hình ảnh hoặc mùi.','Gói thực phẩm ảo có nhãn, tủ mát và kệ khô','Bao bì nhìn đẹp có đủ để kết luận thực phẩm còn dùng được không?',[
 ['Đọc nhãn','Mở ngày hạn dùng và điều kiện bảo quản trên ba gói mẫu.','Yêu cầu bảo quản mỗi gói được hiển thị.','Đọc đủ hạn dùng và điều kiện.','Không nếm thực phẩm nghi hỏng để kiểm tra.'],
 ['Chọn nơi','Đưa gói vào tủ mát hoặc kệ khô theo nhãn mẫu.','Vị trí khớp điều kiện bảo quản.','Ghép đúng ba nhãn với nơi tương ứng.','Nhiệt độ và điều kiện phải theo nhãn, không áp dụng chung cho mọi loại thực phẩm.'],
 ['Xử lý gói lỗi','Đánh dấu gói quá hạn hoặc hư bao bì và chọn báo người lớn.','Gói cần kiểm tra được tách riêng trong mô phỏng.','Không chọn nếm thử.','Ảnh minh họa không chứng nhận an toàn của thực phẩm thật.']
 ],'Đọc nhãn, bảo quản theo hướng dẫn và không dùng cách nếm để kiểm tra đồ nghi hỏng.','Vì sao không chỉ dựa vào hình ảnh để kết luận thực phẩm an toàn?','Ảnh không cho biết đủ thời gian, điều kiện bảo quản hay nguy cơ; cần kiểm tra thông tin và nhờ người phụ trách khi có nghi ngờ.',{items:['goi_kho','goi_can_mat','goi_qua_han'],labelDriven:true});
add('CHM',5,1,'Lọc hỗn hợp cát và nước','LocHonHop','Dùng mô hình lọc để tách chất rắn không tan khỏi chất lỏng.','Cốc nước cát ảo, phễu, giấy lọc, cốc nhận','Giấy lọc có giữ được muối đã hòa tan không?',[
 ['Lắp mô hình','Kéo giấy lọc vào phễu và đặt cốc nhận bên dưới.','Đường nước chảy qua phễu được tạo.','Có giấy lọc, phễu và cốc nhận đúng vị trí.','Cốc nhận không được đặt lệch khỏi đầu phễu.'],
 ['Cho hỗn hợp vào','Kéo cốc nước cát tới vị trí rót ảo.','Cát giữ trên giấy, nước đi qua.','Quan sát cả phần trên giấy và cốc nhận.','Đây là lọc chất rắn không tan, không phải khử trùng nước.'],
 ['Kết luận','Chọn phần giữ lại và phần đi qua.','Cát ở giấy lọc, nước ở cốc nhận.','Ghép đúng hai phần.','Nước sau lọc không được mặc định là nước uống.']
 ],'Giấy lọc giữ cát không tan; nước đi qua giấy lọc.','Có thể dùng giấy lọc thường để tách muối đã tan trong nước không?','Không trong mô hình này; muối hòa tan đi cùng nước qua giấy lọc.',{mixture:['cat','nuoc'],soluble:false,virtualOnly:true},src.mix);
add('CHM',5,2,'Thu muối từ dung dịch bằng bay hơi','BayHoiThuMuoi','Nhận biết bay hơi tách dung môi để thu chất rắn hòa tan.','Dung dịch muối ảo, khay, nguồn gia nhiệt ảo, đồng hồ','Khi nước bay hơi, muối có biến mất cùng nước không?',[
 ['Xác định hỗn hợp','Chọn dung dịch muối và kiểm tra giấy lọc ảo.','Giấy lọc không tách muối đã hòa tan.','Nhận ra cần phương pháp khác lọc.','Không thực hiện gia nhiệt thật theo mô phỏng.'],
 ['Chạy bay hơi','Bấm nguồn nhiệt ảo và tăng thời gian mô phỏng.','Lượng nước giảm; chất rắn xuất hiện.','Theo dõi nước và muối.','Mô hình đã giản lược tốc độ và hình dạng tinh thể.'],
 ['Kết luận','Chọn phần thu được và phần rời khỏi khay.','Muối còn trong khay; nước chuyển vào môi trường dưới dạng hơi.','Chọn muối và nước bay hơi.','Muốn thu lại nước cần thêm bước ngưng tụ; bài này chỉ thu muối.']
 ],'Bay hơi nước giúp thu muối còn lại trong mô hình.','Vì sao bay hơi phù hợp hơn lọc giấy để thu muối đã tan?','Muối hòa tan qua giấy lọc cùng nước, nhưng khi nước bay hơi thì muối còn lại.',{solute:'muoi',solvent:'nuoc',recover:'muoi',virtualOnly:true},src.mix);
add('CHM',5,3,'Chọn rây để tách gạo và bột','RayHonHop','Tách hỗn hợp chất rắn dựa trên kích thước hạt khác nhau.','Hỗn hợp gạo bột ảo, ba rây cỡ lỗ khác nhau, khay','Rây có lỗ lớn hơn hạt gạo có giữ được gạo không?',[
 ['So kích thước','Xem hạt bột, hạt gạo và ba cỡ lỗ rây phóng đại.','Hạt bột nhỏ hơn hạt gạo.','Phân biệt đúng hai cỡ hạt.','Kích thước hình đã phóng đại để dễ quan sát.'],
 ['Chọn rây','Chọn lỗ lớn hơn hạt bột nhưng nhỏ hơn hạt gạo.','Bột đi qua, gạo ở trên.','Chọn rây trung gian.','Lỗ quá nhỏ giữ cả hai, quá lớn cho cả hai đi qua.'],
 ['Kiểm tra hai phần','Ghép khay dưới với bột và phần trên với gạo.','Hai phần được tách trong mô hình lý tưởng.','Ghép đúng hai phần.','Mô hình giản lược hình dạng và sự bám dính của hạt.']
 ],'Chọn cỡ lỗ phù hợp giúp tách gạo và bột.','Tại sao không chọn rây có lỗ lớn nhất?','Nếu lỗ lớn hơn cả hạt gạo thì cả gạo và bột đều đi qua, không tách được.',{particleSizes:[1,5],sieveSizes:[0.5,3,8],idealized:true},src.mix);

assert.equal(lessons.length,30);
assert.equal(new Set(lessons.map(x=>x.id)).size,30);
for(const mon of ['PHY','CHM'])for(let ch=1;ch<=5;ch++)assert.equal(lessons.filter(x=>x.mon===mon&&x.ch===ch).length,3);

// Original scientific diagrams, deliberately schematic and not interactive assets.
const esc=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&apos;'}[c]));
const ink='#18354a',blue='#2789be',green='#3ba888',orange='#e9a43b';
const text=(x,y,s,size=22,color=ink,anchor='middle')=>`<text x="${x}" y="${y}" fill="${color}" font-family="Arial,DejaVu Sans,sans-serif" font-size="${size}" text-anchor="${anchor}">${esc(s)}</text>`;
const rect=(x,y,w,h,fill='#eef6fa',stroke=ink,r=10)=>`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}" fill="${fill}" stroke="${stroke}" stroke-width="3"/>`;
const circle=(x,y,r,fill=blue,stroke=ink)=>`<circle cx="${x}" cy="${y}" r="${r}" fill="${fill}" stroke="${stroke}" stroke-width="3"/>`;
const line=(x,y,x2,y2,color=ink,width=4)=>`<line x1="${x}" y1="${y}" x2="${x2}" y2="${y2}" stroke="${color}" stroke-width="${width}" stroke-linecap="round"/>`;
const arrow=(x,y,x2,y2,color=blue)=>{const length=Math.hypot(x2-x,y2-y),dx=(x2-x)/length,dy=(y2-y)/length;return line(x,y,x2,y2,color)+`<path d="M${x2-12*dx-8*dy},${y2-12*dy+8*dx} L${x2},${y2} L${x2-12*dx+8*dy},${y2-12*dy-8*dx}" fill="none" stroke="${color}" stroke-width="4"/>`;};
const wrap=(s,max=42)=>{let out=[],v='';for(const word of s.split(' ')){if((v+' '+word).trim().length>max&&v){out.push(v);v=word;}else v=(v+' '+word).trim();}if(v)out.push(v);return out;};
const block=(x,y,s,max=42,size=20,color=ink)=>wrap(s,max).map((v,i)=>text(x,y+i*(size+6),v,size,color)).join('');
const beaker=(x,y,label,color='#a9dded',level=55)=>rect(x,y,140,145,'#f7fcff')+rect(x+8,y+140-level*1.25,124,level*1.25,color,'none',2)+line(x+100,y+30,x+125,y+30)+line(x+100,y+55,x+125,y+55)+text(x+70,y+177,label,21);
const box=(x,y,title,sub)=>rect(x,y,210,155,'#edf5f9')+block(x+105,y+55,title,18,25)+block(x+105,y+115,sub,23,18);
function scene(l){
 const k=l.id.slice(3); let a='';
 switch(k){
 case 'PHY_01_01':
   a=rect(120,160,660,70,'#ffe0a0')+rect(208,75,317,35,'#f3ad4f')+`<path d="M525 75 L560 92 L525 110Z" fill="#dbcdb0" stroke="${ink}" stroke-width="3"/>`+text(450,37,'Bút: từ 2 cm đến 10 cm',25);
   for(let i=0;i<=15;i++){let x=120+i*44;a+=line(x,160,x,182+(i%5===0?10:0),ink,2)+text(x,215,i,17);} a+=text(450,280,'Chiều dài = 10 − 2 = 8 cm',25);break;
 case 'PHY_01_02': a=line(450,80,450,235)+line(240,90,660,90)+line(240,90,240,175)+line(660,90,660,175)+rect(175,175,130,16,'#a9d9e7')+rect(595,175,130,16,'#a9d9e7')+rect(205,115,70,60,blue)+rect(605,122,55,53,orange)+rect(663,138,45,37,orange)+text(450,52,'Hai đĩa cân ngang nhau',25)+text(240,235,'Vật mẫu',22)+text(660,235,'100 g + 50 g',22)+text(450,285,'Khối lượng vật: 150 g',24);break;
 case 'PHY_01_03':a=beaker(180,70,'40 mL','#a9dded',40)+beaker(580,70,'70 mL','#a9dded',70)+circle(650,185,23,'#7b919e')+arrow(365,140,525,140)+text(450,35,'Nước dâng khi đá chìm hoàn toàn',24)+text(450,295,'Thể tích đá: 70 − 40 = 30 cm³',25);break;
 case 'PHY_02_01':a=line(170,60,710,60)+rect(410,65,80,35,'#d8e7ed');for(let i=0;i<8;i++)a+=line(i%2?470:430,100+i*15,i%2?430:470,115+i*15,blue,5);a+=line(450,225,450,270,blue)+`<path d="M440 258 L450 272 L460 258" fill="none" stroke="${blue}" stroke-width="4"/>`+text(450,28,'Lực có thể làm vật biến dạng',24)+text(515,260,'Kéo xuống',22)+text(450,310,'Quan sát lò xo trước và sau khi kéo',23);break;
 case 'PHY_02_02':a=rect(350,110,200,85,'#acd9e8')+circle(390,210,22,'#6b8494')+circle(510,210,22,'#6b8494')+arrow(330,150,170,150,orange)+arrow(570,150,770,150,green)+text(220,115,'10 N',24)+text(690,115,'20 N',24)+line(90,240,810,240)+text(450,40,'Cùng xe, hai lực ngược chiều',24)+text(450,290,'Tổng lực: 10 N sang phải',25);break;
 case 'PHY_02_03':a=rect(200,45,110,160,'#edf6fa')+rect(590,45,110,160,'#edf6fa')+rect(225,210,60,45,blue)+rect(610,210,70,65,orange)+text(255,110,'1 N',27)+text(645,110,'2 N',27)+text(255,290,'100 g',22)+text(645,305,'200 g',22)+text(450,25,'Cùng nơi: g ≈ 10 N/kg',24);break;
 case 'PHY_03_01':for(let i=0;i<3;i++){let y=65+i*78;a+=line(230,y+40,785,y+40,[blue,green,orange][i],10)+rect([650,480,300][i],y-5,75,37,'#b8d9e6')+text(150,y+25,['Kính','Gỗ','Thảm'][i],23)+text(800,y+24,['xa','vừa','gần'][i],18);}a+=text(450,25,'Giữ cùng vận tốc ban đầu',24)+text(450,310,'So sánh quãng đường đến khi xe dừng',22);break;
 case 'PHY_03_02':a=rect(110,65,680,190,'#d2f0f7');a+=rect(160,125,60,65,orange)+circle(430,156,34,green)+`<ellipse cx="670" cy="156" rx="70" ry="24" fill="${blue}" stroke="${ink}" stroke-width="3"/>`;a+=text(450,30,'Cùng tốc độ kéo trong nước',24)+text(190,295,'Mặt tù: 3 N',22)+text(430,295,'Cầu: 2 N',22)+text(670,295,'Thuôn: 1 N',22);break;
 case 'PHY_03_03':a=box(70,70,'Đế giày','Tăng độ bám')+box(345,70,'Phanh xe','Tăng lực hãm')+box(620,70,'Bản lề','Giảm ma sát')+text(450,30,'Ma sát có ích hoặc có hại theo mục đích',24)+text(450,295,'Chọn giải pháp phù hợp cho từng bộ phận',23);break;
 case 'PHY_04_01':a=box(50,80,'Tay quay','Cơ năng')+box(345,80,'Máy phát','Điện năng')+box(640,80,'Đèn LED','Sáng và nhiệt')+arrow(270,155,330,155)+arrow(565,155,625,155)+text(450,35,'Theo dõi đường truyền năng lượng',24)+text(450,290,'Ngừng quay: ngừng cấp năng lượng',23);break;
 case 'PHY_04_02':a=beaker(190,80,'Cốc A: 40 °C')+rect(550,65,180,170,'#b5dac3')+beaker(570,80,'Cốc B: 50 °C')+text(450,32,'Cùng ban đầu 60 °C, cùng thời gian',24)+text(450,300,'Số liệu minh họa: lớp bọc làm chậm truyền nhiệt',22);break;
 case 'PHY_04_03':a=text(450,35,'Mỗi đèn nhận 100 đơn vị năng lượng',24);for(let i=0;i<2;i++){let y=100+i*110,p=[20,60][i];a+=text(140,y+28,'Đèn '+['A','B'][i],24)+rect(220,y,480,45,'#f4d7c1','none',3)+rect(220,y,480*p/100,45,'#f7c951','none',3)+text(450,y+78,`${p} ánh sáng + ${100-p} nhiệt`,22);}break;
 case 'PHY_05_01':a=circle(150,145,60,'#ffcc4e')+arrow(225,145,375,145,orange)+circle(570,145,95,'#2e78aa')+`<path d="M570 50 A95 95 0 0 1 570 240Z" fill="#183849"/>`+circle(520,130,9,'#f9e6a6')+text(510,272,'Ngày',24)+text(640,272,'Đêm',24)+text(450,25,'Trái Đất tự quay (không theo tỉ lệ)',24)+text(150,235,'Mặt Trời',22);break;
 case 'PHY_05_02':a=text(450,27,'Phần sáng nhìn từ Trái Đất thay đổi',24)+circle(200,150,63,'#233d52')+circle(450,150,63,'#233d52')+`<path d="M450 87 A63 63 0 0 1 450 213Z" fill="#f3df9e"/>`+circle(700,150,63,'#f3df9e')+text(200,260,'Trăng mới',23)+text(450,260,'Bán nguyệt',23)+text(700,260,'Trăng tròn',23)+text(450,305,'Pha Trăng thông thường không phải nguyệt thực',22);break;
 case 'PHY_05_03':a=text(450,30,'Thứ tự từ gần Mặt Trời ra xa',24)+circle(50,145,31,'#ffce58');['Thủy','Kim','Trái Đất','Hỏa','Mộc','Thổ','Thiên Vương','Hải Vương'].forEach((name,i)=>{a+=circle(150+i*93,145,[12,17,20,15,32,28,23,22][i],['#929ea9','#c49b56',blue,'#c97950','#c2a77b','#d5bd8a','#7fc8cb','#5486be'][i])+text(150+i*93,225,name,15);});a+=text(450,285,'Hình và khoảng cách không theo tỉ lệ',22);break;
 case 'CHM_01_01':a=text(450,30,'Chuẩn bị trang bị trước khi thao tác',24)+rect(100,110,75,55,'#b8e6f5')+rect(190,110,75,55,'#b8e6f5')+line(175,130,190,130)+line(80,120,100,125)+line(265,125,285,120)+text(185,245,'Kính bảo hộ',24)+`<path d="M400 80 L430 65 L470 65 L500 80 L530 150 L505 165 L490 135 V215 H410 V135 L395 165 L370 150Z" fill="#c5e2e8" stroke="${ink}" stroke-width="4"/>`+line(450,90,450,210)+text(450,245,'Áo choàng',24)+`<path d="M665 120 H715 V155 L765 175 Q780 190 755 200 H650 V185 L665 155Z" fill="#7ca7ba" stroke="${ink}" stroke-width="4"/>`+text(705,245,'Giày kín',24)+text(450,300,'Tóc dài cần được buộc gọn',23);break;
 case 'CHM_01_02':for(let i=0;i<3;i++){let x=190+i*260;a+=`<path d="M${x} 65 L${x+75} 140 L${x} 215 L${x-75} 140Z" fill="#fff" stroke="#cc5a42" stroke-width="5"/>`;if(i===0)a+=`<path d="M${x} 92 C${x+9} 123 ${x+42} 133 ${x+26} 169 C${x+10} 196 ${x-35} 179 ${x-28} 152 C${x-20} 164 ${x-14} 151 ${x-15} 137 C${x-10} 125 ${x+4} 117 ${x} 92Z" fill="#253b48"/>`;if(i===1)a+=line(x-35,115,x+5,140,ink,12)+`<path d="M${x+18} 143 Q${x+37} 169 ${x+20} 169 Q${x+5} 167 ${x+18} 143Z" fill="${ink}"/>`+line(x-38,185,x+40,185,ink,5);if(i===2)a+=circle(x,133,28,'#f1f4f5')+circle(x-11,130,7,ink)+circle(x+11,130,7,ink)+rect(x-16,154,32,17,'#f1f4f5')+line(x-35,183,x+35,166,ink,7)+line(x-35,166,x+35,183,ink,7);a+=text(x,268,['Dễ cháy','Ăn mòn','Độc'][i],25);}a+=text(450,27,'Nhãn minh họa: đọc tên nguy cơ trước khi chọn',22)+text(450,309,'Không thay thế bảng nhãn chuẩn tại phòng học',18);break;
 case 'CHM_01_03':a=rect(90,195,720,28,'#d7b08a')+beaker(155,45,'Không nhãn')+rect(430,100,100,90,'#f1c070')+text(480,150,'Đồ ăn',21)+rect(675,75,60,115,'#abdbe3')+text(450,25,'Tìm ba điểm cần xử lý trên bàn ảo',24)+text(705,260,'Chai chưa đậy',21)+text(450,310,'Báo giáo viên về đồ chứa không rõ',22);break;
 case 'CHM_02_01':for(let i=0;i<3;i++){let x=80+i*280;a+=rect(x,70,190,165,'#f1f8fc')+text(x+95,280,['Rắn','Lỏng','Khí'][i],25);for(let j=0;j<12;j++){let xx=x+25+(j%4)*43,yy=100+Math.floor(j/4)*45;if(i===1){xx+=j%2*7;yy=158+Math.floor(j/4)*25;}if(i===2){xx=x+20+((j*47)%150);yy=90+((j*31)%125);}a+=circle(xx,yy,9,[blue,green,orange][i]);}}a+=text(450,30,'Mô hình hạt phóng đại, không phải ảnh chụp',23);break;
 case 'CHM_02_02':a=rect(100,95,110,100,'#c5edfa')+beaker(640,80,'Nước lỏng')+arrow(240,145,590,145,orange)+text(450,108,'Cấp nhiệt ảo',23)+text(155,245,'Đá: −5 °C',23)+text(450,210,'Đang tan: khoảng 0 °C',23)+text(450,285,'Nước tinh khiết, áp suất 1 atm',22);break;
 case 'CHM_02_03':a=beaker(190,90,'40 °C: bay hơi')+beaker(580,90,'100 °C: sôi');for(let i=0;i<5;i++){a+=circle(220+i*20,65-(i%2)*22,5,blue);a+=circle(610+(i%3)*30,200-(i%4)*20,8,'#fff',blue);}a+=text(450,30,'Bay hơi có thể diễn ra khi nước chưa sôi',24)+text(450,310,'Điều kiện mô hình: áp suất 1 atm',20);break;
 case 'CHM_03_01':for(let i=0;i<100;i++){let x=110+(i%20)*34,y=75+Math.floor(i/20)*34;a+=rect(x,y,28,28,i<78?blue:i<99?orange:green,'none',4);}a+=text(450,30,'100 ô = 100% thể tích không khí khô',24)+text(450,285,'78% nitrogen · 21% oxygen · 1% khí khác',23);break;
 case 'CHM_03_02':a=box(70,70,'Không oxygen','Không duy trì cháy')+box(345,70,'Không khí','Mẫu có thể cháy')+box(620,70,'Giàu oxygen','Nguy cơ cháy tăng')+text(450,30,'So sánh định tính trong buồng cháy ảo',24)+text(450,300,'Chỉ mô phỏng; không thử với lửa hoặc oxygen thật',22);break;
 case 'CHM_03_03':a=box(70,70,'Giao thông','Giảm nổ máy lâu')+box(345,70,'Rác thải','Không đốt rác')+box(620,70,'Công trình','Kiểm soát bụi')+text(450,30,'Giảm phát thải tại nguồn',25)+text(450,300,'Không có một biện pháp xử lý mọi ô nhiễm',22);break;
 case 'CHM_04_01':a=box(70,70,'Thân nồi','Kim loại dẫn nhiệt')+box(345,70,'Tay cầm','Vật liệu cách nhiệt')+box(620,70,'Cửa sổ','Kính truyền sáng')+text(450,30,'Chọn vật liệu theo mục đích sử dụng',24)+text(450,300,'Không phải mọi nhựa đều chịu nhiệt như nhau',22);break;
 case 'CHM_04_02':a=box(70,70,'Củi, than','Nhiên liệu')+box(345,70,'Khí thiên nhiên','Nhiên liệu')+box(620,70,'Cát, kính','Không thuộc nhóm')+text(450,30,'Phân loại trong tình huống của bài',24)+text(450,300,'Chỉ thao tác trên thẻ ảo, không thử đốt',22);break;
 case 'CHM_04_03':a=box(70,70,'Gói khô','Theo nhãn: kệ khô')+box(345,70,'Gói cần mát','Theo nhãn: tủ mát')+box(620,70,'Gói quá hạn','Không nếm thử')+text(450,30,'Đọc hạn dùng và yêu cầu bảo quản',24)+text(450,300,'Hình minh họa không chứng nhận thực phẩm an toàn',21);break;
 case 'CHM_05_01':a=beaker(110,100,'Nước và cát')+`<path d="M380 75 H550 L485 165 V215 H455 V165Z" fill="#d8eaf0" stroke="${ink}" stroke-width="4"/>`+beaker(620,100,'Nước đi qua')+arrow(275,160,355,160)+arrow(530,230,600,230);for(let i=0;i<10;i++)a+=circle(410+(i%5)*23,90+Math.floor(i/5)*20,4,orange);a+=text(465,55,'Giấy lọc giữ cát',22)+text(450,310,'Lọc không tách muối đã hòa tan',22);break;
 case 'CHM_05_02':a=beaker(140,80,'Dung dịch muối')+arrow(310,155,510,155,orange)+rect(565,170,190,45,'#c4dce8');for(let i=0;i<9;i++)a+=rect(585+(i%5)*30,175+Math.floor(i/5)*15,10,10,'#f7f4e9','#b9a580',1);a+=text(660,255,'Muối còn lại',23)+text(450,100,'Nước bay hơi',24)+text(450,30,'Thu muối trong mô phỏng bay hơi',24)+text(450,310,'Không gia nhiệt thật theo hình minh họa',21);break;
 case 'CHM_05_03':a=rect(230,95,440,25,'#d4ba8e')+rect(230,240,440,28,'#bfdbe6');for(let i=0;i<12;i++){a+=rect(248+i*34,65,17,8,'#fff2d2','#b69763',3)+circle(250+i*33,170+(i%2)*25,4,'#e0ba7d');}a+=text(450,32,'Lỗ rây lớn hơn bột, nhỏ hơn gạo',24)+text(450,150,'Gạo giữ phía trên',21)+text(450,310,'Bột đi qua xuống khay',23);break;
 default: throw new Error('Missing diagram '+k);
 }
 return a;
}
function svg(l,guide){
 const accent=l.mon==='PHY'?blue:green;
 const caption=guide?'HƯỚNG DẪN THAO TÁC ẢO':'PHÒNG THỰC HÀNH ẢO';
 let s=`<svg xmlns="http://www.w3.org/2000/svg" width="900" height="560" viewBox="0 0 900 560"><rect width="900" height="560" fill="#f6fafc"/>${rect(15,15,870,530,'#fff','#d5e5ec',22)}${text(450,43,caption,16,accent)}${block(450,77,l.title,62,25)}<g transform="translate(0 ${guide?90:125}) ${guide?'translate(72 0) scale(.84)':''}">${scene(l)}</g>`;
 if(guide){l.steps.forEach((step,i)=>{let x=27+i*289;s+=rect(x,390,272,143,'#edf5f8','#d3e1e8',12)+circle(x+26,415,15,accent,accent)+text(x+26,421,i+1,17,'#fff')+block(x+137,449,step[0],23,18)+block(x+137,480,step[2],28,15);});}
 else s+=block(450,493,l.goal,76,18);
 s+='</svg>';return s;
}
for(const l of lessons)for(const guide of [false,true]){
 const filename=`${l.id}_${guide?'huongdan':'daidien'}.png`;
 await sharp(Buffer.from(svg(l,guide))).png().toFile(path.join(out,'images',filename));
}
for(const suffix of ['daidien','huongdan']){
 const tiles=await Promise.all(lessons.map(async(l,i)=>({input:await sharp(path.join(out,'images',`${l.id}_${suffix}.png`)).resize(320,199).toBuffer(),left:(i%5)*320,top:Math.floor(i/5)*199})));
 await sharp({create:{width:1600,height:1194,channels:3,background:'#f6fafc'}}).composite(tiles).png().toFile(path.join(out,`../${suffix}-contact.png`));
}

const wb=Workbook.create();
const main=wb.worksheets.add('ThucHanh'),steps=wb.worksheets.add('BuocThucHanh'),gallery=wb.worksheets.add('HinhAnh'),guide=wb.worksheets.add('HuongDan');
const headers=['ThucHanhID','MonID','ChuongID','TenChuong','ThuTu','TenBai','MucTieu','LoaiMoPhong','DungCu','CauHoiDuDoan','HuongDanTomTat','KetQuaMongDoi','CauHoiKetLuan','DapAnThamKhao','GoiYAI','CauHinhJSON','AnhDaiDien','AnhHuongDan','ThoiLuongPhut','TrangThai','AnToan'];
const mainRows=lessons.map(l=>[l.id,l.mon==='PHY'?'Vật lý':'Hóa học',`CH_${l.mon}_${String(l.ch).padStart(2,'0')}`,`Chương ${l.ch}: ${chapters[l.mon][l.ch-1]}`,l.n,l.title,l.goal,l.type,l.tools,l.predict,l.steps.map((s,i)=>`${i+1}. ${s[0]}`).join('\n'),l.expect,l.question,l.answer,'Gợi bằng câu hỏi về điều kiện so sánh và hiện tượng đã quan sát; không thay thao tác của học sinh. '+l.steps[1][4],JSON.stringify(l.config),`images/${l.id}_daidien.png`,`images/${l.id}_huongdan.png`,8,'Chờ duyệt',l.mon==='CHM'?'Chỉ thao tác trong phòng thực hành ảo. Không tự thực hiện với hóa chất, nguồn nhiệt hoặc lửa thật.':'Mô hình học tập đã giản lược; thông số và hình vẽ theo cấu hình từng bài.']);
main.getRange('A1:U31').values=[headers,...mainRows];
const stepHeaders=['BuocID','ThucHanhID','ThuTu','TenBuoc','ThaoTacHocSinh','HienTuongQuanSat','DieuKienHoanThanh','PhanHoiKhiSai'];
const stepRows=lessons.flatMap(l=>l.steps.map((s,i)=>[`${l.id}_B${i+1}`,l.id,i+1,...s]));
steps.getRange('A1:H91').values=[stepHeaders,...stepRows];
gallery.getRange('A1:H31').values=[['ThucHanhID','TenBai','AnhDaiDienXemTruoc','AnhHuongDanXemTruoc','TepAnhDaiDien','TepAnhHuongDan','URLDaiDienCongKhai','URLHuongDanCongKhai'],...lessons.map(l=>[l.id,l.title,'','',`images/${l.id}_daidien.png`,`images/${l.id}_huongdan.png`,'',''])];
for(let i=0;i<lessons.length;i++){
 const l=lessons[i];
 for(const [col,suffix] of [[2,'daidien'],[3,'huongdan']]){
  const bytes=await fs.readFile(path.join(out,'images',`${l.id}_${suffix}.png`));
  gallery.images.add({dataUrl:'data:image/png;base64,'+bytes.toString('base64'),anchor:{from:{row:i+1,col},extent:{widthPx:360,heightPx:224}}});
 }
}
const notes=[
 ['Thực hành ảo Lí và Hóa','30 bài, mỗi chương 3 bài; 5 chương Vật lí và 5 chương Hóa học.'],
 ['Phạm vi','Nội dung và ảnh cho phòng thực hành ảo. Chưa có code mô phỏng Unity trong gói này.'],
 ['Chương và mã chương','Theo danh mục câu hỏi test.csv và mã chương CH_PHY_01…05, CH_CHM_01…05 của dự án hiện tại.'],
 ['ThucHanh','Một dòng cho mỗi bài; giữ nguyên mã ID khi chỉnh sửa nội dung. MonID dùng Vật lý / Hóa học như dữ liệu câu hỏi hiện có.'],
 ['BuocThucHanh','Ba bước riêng cho mỗi bài, liên kết bằng ThucHanhID. Mỗi bước có thao tác, quan sát, điều kiện hoàn thành và phản hồi.'],
 ['HinhAnh','60 ảnh PNG riêng, gồm 30 ảnh đại diện và 30 ảnh hướng dẫn. Ảnh gốc 900 × 560 px nằm trong thư mục images.'],
 ['Đường dẫn ảnh','AnhDaiDien và AnhHuongDan hiện là đường dẫn tương đối tới ảnh kèm gói, KHÔNG phải URL công khai.'],
 ['Dùng ảnh từ Google Sheets','Đưa PNG lên nơi lưu trữ có URL ảnh công khai phù hợp. Điền hai cột URL trong HinhAnh, rồi thay đường dẫn ở ThucHanh bằng URL tương ứng. Chưa có bước tải ảnh lên dịch vụ ngoài.'],
 ['Nhập dữ liệu','Trong Google Sheets chọn Tệp > Nhập > Tải lên, chọn XLSX và chèn các trang tính mới; không thay toàn bộ cơ sở dữ liệu cũ. CSV UTF-8 trong gói dành cho nhập dữ liệu thuần nếu cần.'],
 ['Ảnh nhúng','Ảnh trong HinhAnh để xem và đối chiếu. Việc nhập Excel có thể thay đổi vị trí ảnh; dùng các PNG riêng làm bản gốc.'],
 ['Duyệt nội dung','TrangThai đang là Chờ duyệt. Giáo viên đối chiếu bài với kế hoạch giảng dạy trước khi chuyển sang Đã duyệt.'],
 ['CauHinhJSON','Thông số đề xuất cho mô phỏng tương lai. Mỗi LoaiMoPhong cần được xây trong Unity, không thể chạy chỉ bằng Sheet.'],
 ['An toàn','Mọi bước đều là thao tác ảo. Các bài có nhiệt, lửa, oxygen hoặc hóa chất không phải hướng dẫn làm thí nghiệm thật tại nhà.'],
 ['Nguồn ảnh','Toàn bộ ảnh sơ đồ được vẽ mới cho bộ nội dung này; không sao chép hình từ các tài liệu tham khảo.'],
 ['Giới hạn mô hình','Số liệu minh họa được ghi trong cấu hình. Các mô hình hạt, Hệ Mặt Trời và lực cản không phải hình đúng tỉ lệ hoặc dự đoán định lượng ngoài phạm vi bài.'],
 ['Tài liệu đối chiếu','Các liên kết sau dùng đối chiếu khái niệm; nội dung tiếng Việt và kịch bản thao tác được biên soạn riêng.'],
 ['Lực và ma sát',src.forces],['Năng lượng',src.energy],['Các thể của chất',src.states],['Pha Mặt Trăng',src.moon],['Ngày và đêm',src.day],['Hệ Mặt Trời','https://science.nasa.gov/solar-system/planets/'],['Tách hỗn hợp',src.mix],['Thành phần không khí',src.air],['An toàn giáo dục hóa học',src.safety]
];
guide.getRange(`A1:B${notes.length}`).values=notes;
function styleTable(sheet,lastCol,rows,widths,height){
 sheet.showGridLines=false;
 sheet.getRange(`A1:${lastCol}${rows}`).format.font={name:'Arial',size:11,color:'#243746'};
 sheet.getRange(`A1:${lastCol}${rows}`).format.verticalAlignment='top';
 sheet.getRange(`A1:${lastCol}${rows}`).format.wrapText=true;
 sheet.getRange(`A1:${lastCol}1`).format={fill:'#234b65',font:{name:'Arial',size:11,bold:true,color:'#ffffff'},rowHeight:42,verticalAlignment:'center',horizontalAlignment:'center',wrapText:true};
 sheet.getRange(`A2:${lastCol}${rows}`).format.rowHeight=height;
 widths.forEach((w,i)=>sheet.getRangeByIndexes(0,i,rows,1).format.columnWidth=w);
 for(let row=2;row<=rows;row++)if(row%2===0)sheet.getRange(`A${row}:${lastCol}${row}`).format.fill='#f0f5f8';
 sheet.freezePanes.freezeRows(1);
 sheet.tables.add(`A1:${lastCol}${rows}`,true,sheet.name+'Table');
}
styleTable(main,'U',31,[23,13,17,38,9,42,52,25,48,52,36,52,52,60,62,65,44,44,15,16,58],108);
main.freezePanes.freezeColumns(1); main.tabColor='#234b65';
main.getRange('E2:E31').setNumberFormat('0');main.getRange('S2:S31').setNumberFormat('0');
main.getRange('T2:T31').dataValidation={rule:{type:'list',values:['Chờ duyệt','Đã duyệt','Ẩn']}};
main.getRange('T2:T31').format.fill='#fff0c5';
styleTable(steps,'H',91,[28,23,9,26,54,54,54,58],92);steps.freezePanes.freezeColumns(2);
styleTable(gallery,'H',31,[23,42,52,52,44,44,55,55],176);
gallery.getRange('C1:D31').format.columnWidthPx=370;
gallery.getRange('A2:H31').format.rowHeightPx=234;
gallery.getRange('G2:H31').format.fill='#fff0c5';
guide.showGridLines=false;guide.getRange(`A1:B${notes.length}`).format.font={name:'Arial',size:11,color:'#243746'};guide.getRange(`A1:B${notes.length}`).format.wrapText=true;guide.getRange(`A1:B${notes.length}`).format.verticalAlignment='center';guide.getRange('A:A').format.columnWidth=30;guide.getRange('B:B').format.columnWidth=118;guide.getRange(`A1:B${notes.length}`).format.rowHeight=50;guide.getRange('A1:B1').format.font={name:'Arial',size:15,bold:true,color:'#234b65'};guide.tabColor='#8a98a3';
for(const row of [7,8,9,10,11,12,13,15])guide.getRange(`A${row}:B${row}`).format.rowHeight=68;
wb.recalculate();
console.log((await wb.inspect({kind:'table',range:'ThucHanh!A1:F4',include:'values',tableMaxRows:4,tableMaxCols:6,maxChars:1500})).ndjson);
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!',options:{useRegex:true,maxResults:20},maxChars:600})).ndjson);
const exported=await SpreadsheetFile.exportXlsx(wb);await exported.save(path.join(out,'ThucHanh_KHTN6_Li_Hoa_30Bai.xlsx'));
for(const [sheetName,range] of [['ThucHanh','A1:G4'],['BuocThucHanh','A1:F4'],['HinhAnh','A1:D3'],['HuongDan','A1:B9']]){
 try{const p=await wb.render({sheetName,range,scale:1,format:'png'});await fs.writeFile(path.join(out,`../${sheetName}-preview.png`),new Uint8Array(await p.arrayBuffer()));}catch(e){console.log('RENDER',sheetName,e.message);}
}
const csv=v=>'"'+String(v??'').replaceAll('"','""')+'"';
for(const [name,rows] of [['ThucHanh',[headers,...mainRows]],['BuocThucHanh',[stepHeaders,...stepRows]]])await fs.writeFile(path.join(out,name+'.csv'),'\ufeff'+rows.map(r=>r.map(csv).join(',')).join('\r\n'),'utf8');
await fs.writeFile(path.join(out,'README.txt'),notes.map(n=>n.join(': ')).join('\n\n'),'utf8');
await fs.writeFile(path.join(out,'manifest.json'),JSON.stringify({lessons:30,steps:90,pngImages:60,chapters:chapters,items:lessons.map(l=>({id:l.id,title:l.title,chapter:`CH_${l.mon}_${String(l.ch).padStart(2,'0')}`,cover:`images/${l.id}_daidien.png`,guide:`images/${l.id}_huongdan.png`}))},null,2));
console.log('COMPLETE',out);
