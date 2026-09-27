using System;
using System.Collections.Generic;

namespace QL_TapChi_WinForms.Services
{
    public static class LanguageService
    {
        public static string CurrentLanguage { get; private set; } = "vi"; // "vi" or "en"

        public static event Action? OnLanguageChanged;

        private static readonly Dictionary<string, string> Vi = new()
        {
            // App branding & Header
            ["AppTitle"] = "Tạp chí Khoa học và Công nghệ",
            ["AppSubtitle"] = "Hệ thống Quản lý Tòa soạn & Xuất bản",
            ["RoleAdmin"] = "Quản trị hệ thống",
            ["RoleEditor"] = "Ban biên tập",
            ["SwitchToEditor"] = "Sang Biên tập →",
            ["SwitchToAdmin"] = "Sang Quản trị →",
            ["Logout"] = "Đăng xuất",
            ["LogoutConfirm"] = "Bạn có chắc chắn muốn đăng xuất khỏi hệ thống?",
            ["LogoutTitle"] = "Xác nhận đăng xuất",
            ["Language"] = "Ngôn ngữ",

            // Sidebar - Admin
            ["Nav_AdminDashboard"] = "Tổng quan Quản trị",
            ["Nav_NguoiDung"] = "Người dùng & Phân quyền",
            ["Nav_ChuyenNganh"] = "Danh mục Chuyên ngành",
            ["Nav_SaoLuu"] = "Sao lưu & Phục hồi CSDL",
            ["Nav_ThongKe"] = "Thống kê Dữ liệu",
            ["Nav_BaiBaoTraCuu"] = "Tra cứu Bản thảo",

            // Sidebar - Editor
            ["Nav_Dashboard"] = "Tổng quan Biên tập",
            ["Nav_BaiBao"] = "Quản lý Bản thảo",
            ["Nav_PhanBien"] = "Phân công Phản biện",
            ["Nav_SoTapChi"] = "Số Tạp chí & Xuất bản",
            ["Nav_ThongKeBaoCao"] = "Thống kê & Báo cáo",

            // Common actions
            ["Add"] = "Thêm mới",
            ["Edit"] = "Chỉnh sửa",
            ["Delete"] = "Xóa",
            ["Save"] = "Lưu thay đổi",
            ["Cancel"] = "Hủy bỏ",
            ["Refresh"] = "Làm mới",
            ["Search"] = "Tìm kiếm:",
            ["Status"] = "Trạng thái:",
            ["Action"] = "Thao tác",

            // Admin Dashboard
            ["AdminDash_Eyebrow"] = "QUẢN TRỊ HỆ THỐNG",
            ["AdminDash_Title"] = "Bảng điều khiển Quản trị hệ thống",
            ["AdminDash_Subtitle"] = "Giám sát hoạt động vận hành tòa soạn, phân quyền người dùng và an toàn dữ liệu học thuật.",
            ["AdminDash_CardUsers"] = "Tài khoản hệ thống",
            ["AdminDash_CardUsersSub"] = "Người dùng đã đăng ký",
            ["AdminDash_CardReviewers"] = "Chuyên gia phản biện",
            ["AdminDash_CardReviewersSub"] = "Hội đồng thẩm định",
            ["AdminDash_CardDB"] = "Cơ sở dữ liệu",
            ["AdminDash_CardDBVal"] = "Sẵn sàng",
            ["AdminDash_CardDBSub"] = "SQL Server trực tuyến",
            ["AdminDash_CardAudit"] = "Nhật ký hệ thống",
            ["AdminDash_CardAuditSub"] = "Lịch sử Audit Trail",
            ["AdminDash_RolesTitle"] = "Vai trò người dùng",
            ["AdminDash_ColRole"] = "Vai trò hệ thống",
            ["AdminDash_ColCount"] = "Số lượng",
            ["AdminDash_AuditTitle"] = "Nhật ký thao tác gần đây",
            ["AdminDash_ColTime"] = "Thời gian",
            ["AdminDash_ColUser"] = "Người dùng",
            ["AdminDash_ColAction"] = "Nội dung thao tác",
            ["AdminDash_BtnManage"] = "Quản lý →",
            ["AdminDash_BtnBackup"] = "Chi tiết →",

            // Editorial Dashboard
            ["Desk_Eyebrow"] = "BÀN LÀM VIỆC TÒA SOẠN",
            ["Desk_Title"] = "Bàn làm việc Tòa soạn & Tiến độ bản thảo",
            ["Desk_Subtitle"] = "Theo dõi luồng xử lý bản thảo qua các giai đoạn thẩm định và xuất bản khoa học.",
            ["Desk_CardSubmissions"] = "Tổng bài nộp",
            ["Desk_CardSubmissionsSub"] = "Tất cả bản thảo",
            ["Desk_CardReview"] = "Đang phản biện",
            ["Desk_CardReviewSub"] = "Thẩm định chuyên môn",
            ["Desk_CardRevision"] = "Chờ chỉnh sửa",
            ["Desk_CardRevisionSub"] = "Tác giả đang bổ sung",
            ["Desk_CardPublished"] = "Đã xuất bản",
            ["Desk_CardPublishedSub"] = "Đã cấp số & DOI",
            ["Desk_RecentTitle"] = "Danh sách bản thảo mới cập nhật cần xử lý",
            ["Desk_BtnViewAll"] = "Xem tất cả bản thảo →",

            // Users
            ["Users_Title"] = "Quản lý người dùng & Phân quyền hệ thống",
            ["Users_SearchPlaceholder"] = "Tên, email, đơn vị, ORCID...",
            ["Users_RoleFilter"] = "Vai trò:",
            ["Users_BtnAdd"] = "+ Thêm người dùng",
            ["Users_BtnEdit"] = "Sửa & Phân quyền",
            ["Users_BtnToggle"] = "Khóa / Mở tài khoản",
            ["Users_BtnDelete"] = "Xóa tài khoản",
            ["Col_Id"] = "Mã",
            ["Col_FullName"] = "Họ và tên",
            ["Col_Degree"] = "Học vị",
            ["Col_Email"] = "Email",
            ["Col_Affiliation"] = "Đơn vị công tác",
            ["Col_Role"] = "Vai trò",
            ["Col_Status"] = "Trạng thái",

            // Manuscripts (BaiBao)
            ["Articles_Title"] = "Quản lý bản thảo bài báo",
            ["Articles_SearchPlaceholder"] = "Tiêu đề, tác giả, từ khóa...",
            ["Articles_CategoryFilter"] = "Chuyên ngành:",
            ["Articles_BtnAdd"] = "+ Tiếp nhận bài",
            ["Articles_BtnDetail"] = "Xem chi tiết & Thẩm định →",
            ["Articles_BtnEdit"] = "Sửa thông tin",
            ["Articles_BtnDelete"] = "Rút hồ sơ",
            ["Col_ArticleCode"] = "Mã bài",
            ["Col_ArticleTitle"] = "Tiêu đề bài báo",
            ["Col_Field"] = "Chuyên ngành",
            ["Col_LeadAuthor"] = "Tác giả",
            ["Col_Stage"] = "Trạng thái",
            ["Col_Reviews"] = "Phản biện",
            ["Col_UpdatedDate"] = "Cập nhật",

            // Peer Review
            ["Reviews_Title"] = "Quản lý phân công & Đánh giá phản biện",
            ["Reviews_FilterStatus"] = "Lọc trạng thái:",
            ["Reviews_BtnAssign"] = "+ Giao phản biện",
            ["Reviews_BtnInvite3"] = "Mời PB thứ 3",
            ["Reviews_BtnScore"] = "Xem đánh giá",
            ["Reviews_BtnRemind"] = "Nhắc hạn",
            ["Reviews_BtnExtend"] = "Gia hạn",
            ["Reviews_BtnCancel"] = "Hủy phân công",
            ["Col_ReviewCode"] = "Mã",
            ["Col_ReviewerName"] = "Chuyên gia PB",
            ["Col_Round"] = "Vòng",
            ["Col_Deadline"] = "Hạn đánh giá",
            ["Col_Score"] = "Điểm",

            // Issues
            ["Issues_Title"] = "Quản lý các số báo & Tập san xuất bản",
            ["Issues_BtnAdd"] = "+ Thêm số tạp chí",
            ["Issues_BtnEdit"] = "Sửa số báo",
            ["Issues_BtnPublish"] = "Phát hành trực tuyến",
            ["Issues_BtnDelete"] = "Xóa số báo",
            ["Issues_BtnAddArticle"] = "+ Thêm bài vào số",
            ["Issues_BtnRemoveArticle"] = "Gỡ khỏi số",
            ["Issues_BtnEditPage"] = "Sửa Trang & DOI",
            ["Issues_BtnViewDetail"] = "Xem chi tiết →",
            ["Issues_EmptyNotice"] = "Số tạp chí này chưa có bài báo nào. Hãy bấm nút [+ Thêm bài vào số] ở trên để đưa bài vào tập san.",
            ["Issues_ColArticlesCount"] = "Số bài",
            ["Col_IssueName"] = "Tên số tạp chí",
            ["Col_Volume"] = "Tập",
            ["Col_Number"] = "Số",
            ["Col_Year"] = "Năm",
            ["Col_PublishDate"] = "Phát hành",

            // Fields / ChuyenNganh
            ["Fields_Title"] = "Quản lý danh mục chuyên ngành nghiên cứu",
            ["Fields_BtnAdd"] = "+ Thêm chuyên ngành",
            ["Fields_BtnDelete"] = "Xóa chuyên ngành",
            ["Fields_BtnSave"] = "Lưu thay đổi",
            ["Fields_BtnClear"] = "Xóa trắng",
            ["Col_FieldCode"] = "Mã",
            ["Col_FieldName"] = "Tên chuyên ngành",
            ["Col_FieldDesc"] = "Mô tả",
            ["Col_ArticleCount"] = "Số bài",

            // Backup & Restore
            ["Backup_Title"] = "Sao lưu & Phục hồi cơ sở dữ liệu",
            ["Backup_Section1"] = "1. Sao lưu dự phòng cơ sở dữ liệu",
            ["Backup_Desc1"] = "Tạo tệp sao lưu toàn vẹn (.bak) chứa bài báo, phản biện, tài khoản và audit trail.",
            ["Backup_FolderLabel"] = "Thư mục lưu trữ bản sao lưu (.bak):",
            ["Backup_BtnBrowse"] = "Duyệt thư mục...",
            ["Backup_BtnRun"] = "Thực hiện Sao lưu",
            ["Backup_Section2"] = "2. Phục hồi dữ liệu từ tệp bản sao",
            ["Backup_Desc2"] = "Khôi phục nguyên trạng cơ sở dữ liệu từ tệp .bak (Thao tác sẽ ghi đè dữ liệu hiện tại).",
            ["Backup_FileLabel"] = "Tệp bản sao lưu (.bak) cần phục hồi:",
            ["Backup_BtnBrowseFile"] = "Duyệt tệp .bak...",
            ["Backup_BtnRestore"] = "Thực hiện Phục hồi",
            ["Backup_LogLabel"] = "Nhật ký thao tác cơ sở dữ liệu:",

            // Reports / ThongKe
            ["Reports_Title"] = "Thống kê hoạt động xuất bản & Báo cáo học thuật",
            ["Reports_BtnExportArticles"] = "Xuất DS Bài báo (CSV/Excel)",
            ["Reports_BtnExportSummary"] = "Xuất Thống kê Tổng hợp",
            ["Reports_SectionStatus"] = "1. Phân bổ bài báo theo tiến độ & trạng thái",
            ["Reports_SectionField"] = "2. Phân bổ theo chuyên ngành khoa học",
            ["Col_StageName"] = "Giai đoạn / Trạng thái",

            // Common additional keys
            ["Role_All"] = "Tất cả",
            ["Role_Admin"] = "Quản trị hệ thống",
            ["Role_Editor"] = "Ban biên tập",
            ["Role_Author"] = "Tác giả",
            ["Role_Reviewer"] = "Chuyên gia phản biện",
            ["Role_Reader"] = "Độc giả",
            ["Status_Active"] = "Hoạt động",
            ["Status_Locked"] = "Bị khóa",
            ["Col_Orcid"] = "ORCID",
            ["Col_Pages"] = "Trang",
            ["Col_DOI"] = "Mã DOI",
            ["Col_Recommendation"] = "Kiến nghị",
            ["Backup_PlaceholderFile"] = "Chọn đường dẫn tệp .bak cần phục hồi...",
            ["Fields_DetailTitle"] = "Chi tiết chuyên ngành",
            ["Fields_CodeAuto"] = "Mã số: [Tự động sinh khi tạo mới]",
            ["Fields_NameLabel"] = "Tên chuyên ngành (*):",
            ["Fields_DescLabel"] = "Mô tả / Phạm vi nghiên cứu:",
            ["Fields_NoticeText"] = "Chuyên ngành là cơ sở chuẩn hóa phân loại bài báo và điều phối phản biện. Hệ thống không cho xóa ngành đã có bài báo.",
            ["Issues_ArticlesInSelected"] = "Bài báo thuộc số đã chọn",
            ["Issues_ArticlesInSelectedParam"] = "Bản thảo: {0}",
            ["Col_Quantity"] = "Số lượng"
        };

        private static readonly Dictionary<string, string> En = new()
        {
            // App branding & Header
            ["AppTitle"] = "Journal of Science and Technology",
            ["AppSubtitle"] = "Editorial & Publishing Management System",
            ["RoleAdmin"] = "System Administration",
            ["RoleEditor"] = "Editorial Board",
            ["SwitchToEditor"] = "To Editor →",
            ["SwitchToAdmin"] = "To Admin →",
            ["Logout"] = "Logout",
            ["LogoutConfirm"] = "Are you sure you want to log out of the system?",
            ["LogoutTitle"] = "Confirm Logout",
            ["Language"] = "Language",

            // Sidebar - Admin
            ["Nav_AdminDashboard"] = "Admin Dashboard",
            ["Nav_NguoiDung"] = "Users & Permissions",
            ["Nav_ChuyenNganh"] = "Research Fields",
            ["Nav_SaoLuu"] = "Database Backup & Restore",
            ["Nav_ThongKe"] = "Data & Reports",
            ["Nav_BaiBaoTraCuu"] = "Search Manuscripts",

            // Sidebar - Editor
            ["Nav_Dashboard"] = "Editorial Dashboard",
            ["Nav_BaiBao"] = "Manuscript Management",
            ["Nav_PhanBien"] = "Peer Review Management",
            ["Nav_SoTapChi"] = "Issues & Publishing",
            ["Nav_ThongKeBaoCao"] = "Reports & Analytics",

            // Common actions
            ["Add"] = "Add New",
            ["Edit"] = "Edit",
            ["Delete"] = "Delete",
            ["Save"] = "Save Changes",
            ["Cancel"] = "Cancel",
            ["Refresh"] = "Refresh",
            ["Search"] = "Search:",
            ["Status"] = "Status:",
            ["Action"] = "Action",

            // Admin Dashboard
            ["AdminDash_Eyebrow"] = "SYSTEM ADMINISTRATION",
            ["AdminDash_Title"] = "System Administration Dashboard",
            ["AdminDash_Subtitle"] = "Monitor editorial operations, user permissions, and scholarly data security.",
            ["AdminDash_CardUsers"] = "System Accounts",
            ["AdminDash_CardUsersSub"] = "Registered users",
            ["AdminDash_CardReviewers"] = "Peer Reviewers",
            ["AdminDash_CardReviewersSub"] = "Review board members",
            ["AdminDash_CardDB"] = "Database",
            ["AdminDash_CardDBVal"] = "Online",
            ["AdminDash_CardDBSub"] = "SQL Server connected",
            ["AdminDash_CardAudit"] = "System Logs",
            ["AdminDash_CardAuditSub"] = "Audit trail entries",
            ["AdminDash_RolesTitle"] = "User Roles",
            ["AdminDash_ColRole"] = "System Role",
            ["AdminDash_ColCount"] = "Quantity",
            ["AdminDash_AuditTitle"] = "Recent Activity Log",
            ["AdminDash_ColTime"] = "Timestamp",
            ["AdminDash_ColUser"] = "Performed By",
            ["AdminDash_ColAction"] = "Action Details",
            ["AdminDash_BtnManage"] = "Manage →",
            ["AdminDash_BtnBackup"] = "Details →",

            // Editorial Dashboard
            ["Desk_Eyebrow"] = "EDITORIAL DESK",
            ["Desk_Title"] = "Editorial Workspace & Manuscript Progress",
            ["Desk_Subtitle"] = "Track scholarly manuscripts through peer review and publishing stages.",
            ["Desk_CardSubmissions"] = "Submissions",
            ["Desk_CardSubmissionsSub"] = "Total manuscripts",
            ["Desk_CardReview"] = "Under Review",
            ["Desk_CardReviewSub"] = "Peer review in progress",
            ["Desk_CardRevision"] = "Revisions",
            ["Desk_CardRevisionSub"] = "Author revising",
            ["Desk_CardPublished"] = "Published",
            ["Desk_CardPublishedSub"] = "Volume & DOI assigned",
            ["Desk_RecentTitle"] = "Recent Manuscripts Pending Action",
            ["Desk_BtnViewAll"] = "View All Manuscripts →",

            // Users
            ["Users_Title"] = "User Management & Role Permissions",
            ["Users_SearchPlaceholder"] = "Name, email, affiliation, ORCID...",
            ["Users_RoleFilter"] = "Role:",
            ["Users_BtnAdd"] = "+ Add User",
            ["Users_BtnEdit"] = "Edit & Permissions",
            ["Users_BtnToggle"] = "Lock / Unlock",
            ["Users_BtnDelete"] = "Delete Account",
            ["Col_Id"] = "ID",
            ["Col_FullName"] = "Full Name",
            ["Col_Degree"] = "Degree",
            ["Col_Email"] = "Email",
            ["Col_Affiliation"] = "Affiliation",
            ["Col_Role"] = "Role",
            ["Col_Status"] = "Status",

            // Manuscripts (BaiBao)
            ["Articles_Title"] = "Manuscript Management",
            ["Articles_SearchPlaceholder"] = "Title, author, keywords...",
            ["Articles_CategoryFilter"] = "Field:",
            ["Articles_BtnAdd"] = "+ Submit",
            ["Articles_BtnDetail"] = "Review & Details →",
            ["Articles_BtnEdit"] = "Edit",
            ["Articles_BtnDelete"] = "Withdraw",
            ["Col_ArticleCode"] = "Code",
            ["Col_ArticleTitle"] = "Article Title",
            ["Col_Field"] = "Research Field",
            ["Col_LeadAuthor"] = "Author",
            ["Col_Stage"] = "Status",
            ["Col_Reviews"] = "Reviews",
            ["Col_UpdatedDate"] = "Updated",

            // Peer Review
            ["Reviews_Title"] = "Reviewer Assignment & Evaluation",
            ["Reviews_FilterStatus"] = "Filter Status:",
            ["Reviews_BtnAssign"] = "+ Assign Reviewer",
            ["Reviews_BtnInvite3"] = "Invite 3rd",
            ["Reviews_BtnScore"] = "View Evaluation",
            ["Reviews_BtnRemind"] = "Remind",
            ["Reviews_BtnExtend"] = "Extend",
            ["Reviews_BtnCancel"] = "Cancel Review",
            ["Col_ReviewCode"] = "ID",
            ["Col_ReviewerName"] = "Reviewer",
            ["Col_Round"] = "Round",
            ["Col_Deadline"] = "Deadline",
            ["Col_Score"] = "Score",

            // Issues
            ["Issues_Title"] = "Journal Issues & Publication Management",
            ["Issues_BtnAdd"] = "+ Add Issue",
            ["Issues_BtnEdit"] = "Edit Issue",
            ["Issues_BtnPublish"] = "Publish Online",
            ["Issues_BtnDelete"] = "Delete Issue",
            ["Issues_BtnAddArticle"] = "+ Add Article",
            ["Issues_BtnRemoveArticle"] = "Remove",
            ["Issues_BtnEditPage"] = "Edit Pages & DOI",
            ["Issues_BtnViewDetail"] = "Details →",
            ["Issues_EmptyNotice"] = "This issue has no articles yet. Click [+ Add Article] above to assign manuscripts.",
            ["Issues_ColArticlesCount"] = "Articles",
            ["Col_IssueName"] = "Issue Title",
            ["Col_Volume"] = "Vol.",
            ["Col_Number"] = "No.",
            ["Col_Year"] = "Year",
            ["Col_PublishDate"] = "Publish Date",

            // Fields / ChuyenNganh
            ["Fields_Title"] = "Research Fields Management",
            ["Fields_BtnAdd"] = "+ Add Field",
            ["Fields_BtnDelete"] = "Delete Field",
            ["Fields_BtnSave"] = "Save Changes",
            ["Fields_BtnClear"] = "Clear Form",
            ["Col_FieldCode"] = "Code",
            ["Col_FieldName"] = "Field Name",
            ["Col_FieldDesc"] = "Description",
            ["Col_ArticleCount"] = "Articles",

            // Backup & Restore
            ["Backup_Title"] = "Database Backup & Restoration",
            ["Backup_Section1"] = "1. Database Backup",
            ["Backup_Desc1"] = "Create full backup (.bak) containing articles, reviews, accounts, and audit logs.",
            ["Backup_FolderLabel"] = "Backup destination folder (.bak):",
            ["Backup_BtnBrowse"] = "Browse Folder...",
            ["Backup_BtnRun"] = "Run Backup",
            ["Backup_Section2"] = "2. Restore from Backup File",
            ["Backup_Desc2"] = "Restore database state from .bak file (This operation will overwrite current data).",
            ["Backup_FileLabel"] = "Backup file (.bak) to restore:",
            ["Backup_BtnBrowseFile"] = "Browse .bak File...",
            ["Backup_BtnRestore"] = "Run Restore",
            ["Backup_LogLabel"] = "Database Operation Log:",

            // Reports / ThongKe
            ["Reports_Title"] = "Publishing Statistics & Academic Reports",
            ["Reports_BtnExportArticles"] = "Export Manuscripts (CSV/Excel)",
            ["Reports_BtnExportSummary"] = "Export Summary Report",
            ["Reports_SectionStatus"] = "1. Manuscripts by Review Stage & Status",
            ["Reports_SectionField"] = "2. Manuscripts by Research Field",
            ["Col_StageName"] = "Stage / Status",

            // Roles
            ["Role_Admin"] = "System Admin",
            ["Role_Editor"] = "Editorial Board",
            ["Role_Author"] = "Author",
            ["Role_Reviewer"] = "Peer Reviewer",
            ["Role_Reader"] = "Reader",
            ["Role_All"] = "All",

            // Status & Misc
            ["Status_Active"] = "Active",
            ["Status_Locked"] = "Locked",
            ["Col_Orcid"] = "ORCID",
            ["Col_Pages"] = "Pages",
            ["Col_DOI"] = "DOI",
            ["Col_Recommendation"] = "Decision",
            ["Backup_PlaceholderFile"] = "Select path of .bak file to restore...",
            ["Fields_DetailTitle"] = "Research Field Details",
            ["Fields_CodeAuto"] = "Code: [Auto-generated on creation]",
            ["Fields_NameLabel"] = "Field Name (*):",
            ["Fields_DescLabel"] = "Description / Scope:",
            ["Fields_NoticeText"] = "Research field classifies scholarly articles and coordinates peer reviewers. Fields with linked articles cannot be deleted.",
            ["Issues_ArticlesInSelected"] = "Articles in selected issue",
            ["Issues_ArticlesInSelectedParam"] = "Articles: {0}",
            ["Col_Quantity"] = "Quantity"
        };

        public static void SetLanguage(string lang)
        {
            if (lang != "vi" && lang != "en") return;
            if (CurrentLanguage == lang) return;

            CurrentLanguage = lang;
            OnLanguageChanged?.Invoke();
        }

        public static string Get(string key)
        {
            var dict = CurrentLanguage == "en" ? En : Vi;
            if (dict.TryGetValue(key, out var val)) return val;
            if (Vi.TryGetValue(key, out var fallback)) return fallback;
            return key;
        }

        public static string TranslateRole(string role)
        {
            if (CurrentLanguage == "vi") return role;
            return role switch
            {
                "Quản trị hệ thống" => Get("Role_Admin"),
                "Ban biên tập" => Get("Role_Editor"),
                "Tác giả" => Get("Role_Author"),
                "Chuyên gia phản biện" => Get("Role_Reviewer"),
                "Độc giả" => Get("Role_Reader"),
                "Tất cả" => Get("Role_All"),
                _ => role
            };
        }

        public static string TranslateStatus(string status)
        {
            if (CurrentLanguage == "vi") return status;
            return status switch
            {
                "Chờ sơ duyệt" => "Initial Review",
                "Đang phản biện" => "Under Review",
                "Chờ chỉnh sửa" => "Revisions",
                "Chờ sửa hình thức" => "Format Revision",
                "Chờ quyết định" => "Decision Pending",
                "Đã chấp nhận" => "Accepted",
                "Đang chế bản" => "Copyediting",
                "Sẵn sàng xuất bản" => "Ready to Publish",
                "Đã xuất bản" => "Published",
                "Từ chối" => "Rejected",
                "Hoạt động" => "Active",
                "Bị khóa" => "Locked",
                "Chờ phản hồi" => "Pending Response",
                "Đồng ý phản biện" => "Review Accepted",
                "Đang đánh giá" => "In Progress",
                "Đã đánh giá" => "Completed",
                "Từ chối phản biện" => "Review Declined",
                "Quá hạn" => "Overdue",
                "Đã hủy" => "Cancelled",
                "Đang biên tập" => "In Editorial",
                "Đã phát hành" => "Published",
                "Đang lên số" => "In Preparation",
                _ => status
            };
        }

        public static string TranslateRecommendation(string? rec)
        {
            if (string.IsNullOrWhiteSpace(rec)) return "-";
            if (CurrentLanguage == "vi") return rec;
            return rec switch
            {
                "Chấp nhận đăng" => "Accept",
                "Chỉnh sửa nhỏ" => "Minor Rev.",
                "Chỉnh sửa lớn" => "Major Rev.",
                "Từ chối" => "Reject",
                _ => rec
            };
        }

        public static string TranslateRound(int round)
        {
            return CurrentLanguage == "en" ? $"Round {round}" : $"Vòng {round}";
        }
    }
}
