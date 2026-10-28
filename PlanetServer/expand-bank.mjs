// One-time, idempotent additions so every difficulty has five questions per subject.
import { readFileSync, writeFileSync } from 'node:fs';
const path = new URL('./questions.json', import.meta.url);
const bank = JSON.parse(readFileSync(path, 'utf8'));
const additions = [
['Physics','Dễ','Dụng cụ nào dùng để đo nhiệt độ?',['Nhiệt kế','Cân','Thước','Lực kế'],0],
['Physics','Dễ','Đơn vị đo khối lượng trong hệ SI là gì?',['Mét','Giây','Kilôgam','Newton'],2],
['Physics','Trung bình','2500 g bằng bao nhiêu kg?',['25','2,5','0,25','250'],1],
['Physics','Trung bình','Lực nào giữ vật trên mặt đất?',['Lực hút của Trái Đất','Lực đẩy của nước','Lực đàn hồi','Lực kéo của dây'],0],
['Physics','Trung bình','Lò xo bị kéo giãn tạo ra lực gì?',['Lực ma sát','Lực đàn hồi','Lực từ','Lực đẩy Archimedes'],1],
['Physics','Khó','Một vật có trọng lượng gần 50 N trên Trái Đất. Khối lượng của vật gần bao nhiêu?',['0,5 kg','50 kg','5 kg','500 kg'],2],
['Physics','Khó','Khi phanh xe đạp, dạng năng lượng nào tăng lên ở má phanh?',['Nhiệt năng','Quang năng','Hóa năng','Năng lượng hạt nhân'],0],
['Physics','Khó','Kéo vật trượt đều trên sàn ngang, lực kéo cân bằng với lực nào theo phương ngang?',['Trọng lực','Lực ma sát trượt','Lực nâng của sàn','Lực hút nam châm'],1],
['Chemistry','Dễ','Nước lỏng chuyển thành hơi nước gọi là gì?',['Đông đặc','Ngưng tụ','Hóa hơi','Nóng chảy'],2],
['Chemistry','Dễ','Chất nào tan tốt trong nước ở điều kiện thường?',['Cát','Muối ăn','Dầu ăn','Sỏi'],1],
['Chemistry','Trung bình','Hơi nước chuyển thành giọt nước gọi là gì?',['Ngưng tụ','Nóng chảy','Đông đặc','Sôi'],0],
['Chemistry','Trung bình','Vật liệu nào sau đây dẫn điện tốt?',['Nhựa','Cao su','Đồng','Thủy tinh'],2],
['Chemistry','Trung bình','Không khí là gì?',['Một chất tinh khiết','Hỗn hợp nhiều chất khí','Chỉ có oxygen','Chỉ có nitrogen'],1],
['Chemistry','Khó','Tách mạt sắt khỏi hỗn hợp mạt sắt và cát bằng cách nào?',['Dùng nam châm','Lọc qua giấy','Cô cạn','Để lắng trong dầu'],0],
['Chemistry','Khó','Tách dầu ăn ra khỏi nước bằng dụng cụ nào phù hợp nhất?',['Phễu chiết','Nam châm','Rây khô','Cân'],0],
['Chemistry','Khó','Vì sao không dùng nước để dập đám cháy xăng?',['Xăng nổi trên nước và có thể lan rộng','Nước làm xăng đông đặc','Xăng tan hoàn toàn trong nước','Nước biến thành xăng'],0],
['Biology','Dễ','Dụng cụ nào dùng quan sát tế bào nhỏ?',['Kính hiển vi','Nhiệt kế','Ống nhòm','Cân'],0],
['Biology','Dễ','Cơ quan nào giúp cá hô hấp trong nước?',['Phổi','Mang','Vây','Đuôi'],1],
['Biology','Trung bình','Nhóm động vật nào có lông vũ?',['Cá','Bò sát','Chim','Lưỡng cư'],2],
['Biology','Trung bình','Nấm không được xếp vào giới Thực vật chủ yếu vì sao?',['Không có diệp lục để quang hợp','Không có tế bào','Không cần dinh dưỡng','Không sinh sản'],0],
['Biology','Trung bình','Cấu trúc nào kiểm soát chất đi vào và ra khỏi tế bào?',['Màng tế bào','Nhân','Lục lạp','Nhiễm sắc thể'],0],
['Biology','Khó','Tập hợp các tế bào có cấu tạo giống nhau, cùng thực hiện một chức năng gọi là gì?',['Mô','Cơ quan','Cơ thể','Quần xã'],0],
['Biology','Khó','Vì sao virus phải sống trong tế bào vật chủ để nhân lên?',['Virus chưa có cấu tạo tế bào hoàn chỉnh','Virus có nhiều lục lạp','Virus tự quang hợp','Virus là động vật đa bào'],0],
['Biology','Khó','Trong chuỗi cỏ → châu chấu → ếch → rắn, sinh vật sản xuất là gì?',['Ếch','Rắn','Châu chấu','Cỏ'],3]
];
for (const [subject,difficulty,text,options,correct] of additions)
  if (!bank.some(q => q.text === text)) bank.push({subject,difficulty,text,options,correct});
writeFileSync(path, JSON.stringify(bank, null, 2) + '\n');
