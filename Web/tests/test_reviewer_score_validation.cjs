const assert = require('node:assert/strict');
const C = require('../reviewer-core.js');

const response = {
  maPhanCong: 12,
  reviewTitle: 'Tiêu đề phù hợp với nội dung nghiên cứu.',
  reviewNovelty: 'Bài có đóng góp mới ở cách tiếp cận.',
  reviewArgument: 'Cần giải thích rõ luận cứ và căn cứ cho kết luận.',
  reviewStructure: 'Bố cục hợp lý.',
  reviewFigures: 'Hình 2 cần tăng độ phân giải.',
  reviewGrammar: 'Cần hiệu đính một số câu dài.',
  reviewContentChanges: 'Bổ sung đối chiếu với nghiên cứu gần đây.',
  reviewFormatChanges: 'Chuẩn hóa chú thích hình.',
  nhanXetBaoMat: '',
  kienNghi: 'Chỉnh sửa nhỏ'
};

assert.equal(C.validate(response), '', 'Teacher form must submit without numeric scores');
assert.equal(C.total(response), null, 'The new form must not calculate a numeric total');
const packed = C.packReview(response);
for (const [, label] of C.reviewFields) assert.ok(packed.includes(`${label}:\n`), `Missing rubric section: ${label}`);
const unpacked = C.unpackReview(packed);
for (const [key] of C.reviewFields) assert.equal(unpacked[key], response[key]);
assert.match(C.validate({...response, reviewTitle:'',reviewNovelty:'',reviewArgument:'',reviewStructure:'',reviewFigures:'',reviewGrammar:'',reviewContentChanges:'',reviewFormatChanges:''}), /nhận xét/i);
assert.ok(C.validate({...response, reviewArgument:'', diemTinhMoi:'8'}), 'Partial legacy scores must be rejected');

const oldReview = {...response, reviewTitle:'',reviewNovelty:'',reviewArgument:'',reviewStructure:'',reviewFigures:'',reviewGrammar:'',reviewContentChanges:'',reviewFormatChanges:'',nhanXetChoTacGia:'Nhận xét cũ của phiếu trước.'};
assert.equal(C.unpackReview(oldReview.nhanXetChoTacGia).reviewArgument, oldReview.nhanXetChoTacGia, 'Existing free-text drafts remain readable');
console.log('PASS: teacher review rubric, no-score submission, legacy score validation, and old draft compatibility');
