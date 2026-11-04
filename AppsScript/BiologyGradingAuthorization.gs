/**
 * Run once in the Apps Script editor as the SAME account that deploys the web app.
 * This does not create, edit or grade any pupil submission.
 * Keep existing manifest scopes; if explicit, append script.external_request only.
 */
function authorizeBiologyEssayGrading() {
  var response = UrlFetchApp.fetch('https://www.google.com/generate_204', {
    muteHttpExceptions: true,
    followRedirects: false
  });
  if (response.getResponseCode() !== 204 && response.getResponseCode() !== 200) {
    throw new Error('Chưa xác nhận được quyền kết nối dịch vụ ngoài. Mã HTTP ' + response.getResponseCode());
  }
  console.log('Đã xác nhận quyền script.external_request. Không thay đổi dữ liệu Sheet.');
}
