/* Shared pure rules for the reviewer workspace. */
(function (root) {
  'use strict';
  const roles = ['Chuyên gia phản biện', 'Phản biện viên', 'Người phản biện', 'Phản biện', 'Reviewer'];
  const recommendations = ['Chấp nhận đăng', 'Chỉnh sửa nhỏ', 'Chỉnh sửa lớn và phản biện lại', 'Từ chối đăng'];
  const scoreKeys = ['diemTinhMoi', 'diemPhuongPhap', 'diemKetQua', 'diemTrinhBay'];
  const reviewFields = [
    ['reviewTitle', 'Tiêu đề bài báo'],
    ['reviewNovelty', 'Những điểm mới trong nội dung'],
    ['reviewArgument', 'Vấn đề đặt ra, luận cứ, luận điểm và kết luận'],
    ['reviewStructure', 'Bố cục'],
    ['reviewFigures', 'Trình bày hình vẽ, biểu đồ'],
    ['reviewGrammar', 'Văn phạm'],
    ['reviewContentChanges', 'Vấn đề cần chỉnh sửa hoặc bổ sung về nội dung'],
    ['reviewFormatChanges', 'Vấn đề cần chỉnh sửa hoặc bổ sung về hình thức']
  ];
  function packReview(data) {
    if (!reviewFields.some(([key]) => String(data[key] || '').trim())) return '';
    return reviewFields.map(([key,label]) => `${label}:\n${String(data[key] || '').trim()}`).join('\n\n');
  }
  function reviewTextLength(data) { return reviewFields.reduce((sum,[key]) => sum + String(data[key] || '').trim().length, 0); }
  function unpackReview(text) {
    const source=String(text || ''); const found=reviewFields.map(([key,label])=>({key,marker:`${label}:\n`,at:source.indexOf(`${label}:\n`)})).filter(x=>x.at>=0); const result={};
    if(!found.length) { result.reviewArgument=source; return result; }
    found.forEach((item,i)=>{const start=item.at+item.marker.length;const next=found[i+1]?.at;const end=next==null?source.length:source.lastIndexOf('\n\n',next);result[item.key]=source.slice(start,end<start?next:end).trim();});
    return result;
  }
  function hasRole(user) { return !!user && Array.isArray(user.vaiTros) && user.vaiTros.some(r => roles.includes(r)); }
  function completed(a) { return a.daDanhGia === true || a.trangThai === 'Đã đánh giá'; }
  function inactive(a) { return completed(a) || ['Từ chối phản biện', 'Đã hủy', 'Đã huỷ'].includes(a.trangThai); }
  function canEvaluate(a) { return !inactive(a) && ['Đồng ý phản biện', 'Đang đánh giá'].includes(a.trangThai); }
  function canDownload(a) { return ['Đồng ý phản biện', 'Đang đánh giá', 'Đã đánh giá'].includes(a.trangThai); }
  function parseDate(value) {
    if (!value) return NaN;
    // SQL datetime has no zone: the journal's wall-clock timezone is Vietnam.
    const normalized = /^\d{4}-\d\d-\d\d$/.test(value) ? value + 'T23:59:59+07:00' :
      /^\d{4}-\d\d-\d\dT\d\d:\d\d/.test(value) && !/(Z|[+-]\d\d:\d\d)$/i.test(value) ? value + '+07:00' : value;
    return Date.parse(normalized);
  }
  function due(a) { return parseDate(a.trangThai === 'Chờ phản hồi' ? (a.hanPhanHoi || a.hanHoanThanh) : a.hanHoanThanh); }
  function deadline(a, now = Date.now()) {
    if (inactive(a)) return 'closed'; const t = due(a); if (!Number.isFinite(t)) return 'none';
    return t < now ? 'overdue' : t <= now + 3 * 86400000 ? 'due' : 'normal';
  }
  function validate(data) {
    if (!Number.isInteger(data.maPhanCong) || data.maPhanCong < 1) return 'Mã phân công không hợp lệ.';
    const anyScore=scoreKeys.some(k=>String(data[k]??'').trim()!==''), allScores=scoreKeys.every(k=>String(data[k]??'').trim()!=='');
    if (anyScore !== allScores || (allScores && scoreKeys.some(k => {
      const value = Number(data[k]);
      return !Number.isFinite(value) || value < 0 || value > 10 || Math.abs(value * 10 - Math.round(value * 10)) > 1e-8;
    }))) return 'Nếu nhập điểm theo phiếu cũ, cần nhập đủ bốn tiêu chí từ 0 đến 10.';
    const narrative=reviewFields.some(([key])=>String(data[key]||'').trim())?packReview(data):String(data.nhanXetChoTacGia||'').trim();
    if (reviewFields.some(([key])=>String(data[key]||'').trim()) ? reviewTextLength(data) < 10 : narrative.length < 10) return 'Vui lòng điền nhận xét theo các mục của biểu mẫu.';
    if (narrative.length > 20000 || (data.nhanXetBaoMat || '').length > 20000) return 'Nhận xét tối đa 20.000 ký tự.';
    if (!recommendations.includes(data.kienNghi)) return 'Vui lòng chọn kiến nghị hợp lệ.';
    return '';
  }
  function total(data) { return scoreKeys.every(k=>data[k]!=null&&String(data[k]).trim()!=='') ? Math.round(scoreKeys.reduce((sum,k) => sum + Number(data[k]),0) / 4 * 10) / 10 : null; }
  function filter(items, { view='active', search='', status='all', sort='deadline', draftIds=[] }={}, now=Date.now()) {
    const q = search.trim().toLocaleLowerCase('vi');
    return items.filter(a => (view !== 'active' || !inactive(a)) && (view !== 'history' || completed(a)))
      .filter(a => !q || `${a.tieuDeBaiBao} ${a.chuyenNganh} ${a.maBaiBao} ${a.maPhanCong}`.toLocaleLowerCase('vi').includes(q))
      .filter(a => status==='all' || (status==='pending' && a.trangThai==='Chờ phản hồi') ||
        (status==='draft' && draftIds.includes(a.maPhanCong)) || (['overdue','due'].includes(status) && deadline(a,now)===status))
      .sort((a,b) => sort==='title' ? String(a.tieuDeBaiBao).localeCompare(String(b.tieuDeBaiBao),'vi') :
        sort==='newest' ? (parseDate(b.ngayPhanCong)||0)-(parseDate(a.ngayPhanCong)||0) :
        (Number.isFinite(due(a))?due(a):Infinity)-(Number.isFinite(due(b))?due(b):Infinity) || a.maPhanCong-b.maPhanCong);
  }
  const api = {hasRole,completed,inactive,canEvaluate,canDownload,parseDate,due,deadline,validate,total,filter,scoreKeys,recommendations,reviewFields,packReview,unpackReview,reviewTextLength};
  if (typeof module === 'object' && module.exports) module.exports = api; else root.ReviewerCore = api;
})(typeof globalThis !== 'undefined' ? globalThis : this);
