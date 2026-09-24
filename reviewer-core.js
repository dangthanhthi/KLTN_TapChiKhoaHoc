/* Shared pure rules for the reviewer workspace; no implicit demo fallback. */
(function (root) {
  'use strict';
  const roles = ['Chuyên gia phản biện', 'Phản biện viên', 'Người phản biện', 'Phản biện', 'Reviewer'];
  const recommendations = ['Chấp nhận đăng', 'Chỉnh sửa nhỏ', 'Chỉnh sửa lớn và phản biện lại', 'Từ chối đăng'];
  const scoreKeys = ['diemTinhMoi', 'diemPhuongPhap', 'diemKetQua', 'diemTrinhBay'];
  function hasRole(user) { return !!user && Array.isArray(user.vaiTros) && user.vaiTros.some(r => roles.includes(r)); }
  function completed(a) { return a.daDanhGia === true || a.trangThai === 'Đã đánh giá'; }
  function inactive(a) { return completed(a) || ['Từ chối phản biện', 'Đã hủy', 'Đã huỷ'].includes(a.trangThai); }
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
    if (scoreKeys.some(k => data[k] === '' || data[k] == null || !Number.isFinite(Number(data[k])) || Number(data[k]) < 0 || Number(data[k]) > 10)) return 'Nhập đầy đủ 4 điểm từ 0 đến 10.';
    if (!data.nhanXetChoTacGia?.trim()) return 'Vui lòng nhập nhận xét gửi tác giả.';
    if ((data.nhanXetChoTacGia || '').length > 20000 || (data.nhanXetBaoMat || '').length > 20000) return 'Nhận xét tối đa 20.000 ký tự.';
    if (!recommendations.includes(data.kienNghi)) return 'Vui lòng chọn kiến nghị hợp lệ.';
    return '';
  }
  function total(data) { return Math.round(scoreKeys.reduce((sum,k) => sum + Number(data[k]),0) / 4 * 10) / 10; }
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
  const api = {hasRole,completed,inactive,parseDate,due,deadline,validate,total,filter,scoreKeys,recommendations};
  if (typeof module === 'object' && module.exports) module.exports = api; else root.ReviewerCore = api;
})(typeof globalThis !== 'undefined' ? globalThis : this);
