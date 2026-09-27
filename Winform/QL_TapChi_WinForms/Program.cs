using System;
using System.Windows.Forms;
using QL_TapChi_WinForms.Services;
using QL_TapChi_WinForms.UI.Forms;

namespace QL_TapChi_WinForms
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            // Nếu truyền tham số --main thì vào thẳng bàn làm việc Biên tập
            if (args.Length > 0 && (args[0] == "--main" || args[0] == "-m"))
            {
                Application.Run(new FrmLogin());
                return;
            }

            // Nếu truyền tham số --admin thì vào thẳng bàn làm việc Quản trị
            if (args.Length > 0 && (args[0] == "--admin" || args[0] == "-a"))
            {
                Application.Run(new FrmLogin());
                return;
            }

            // Nếu truyền tham số --render-audit thì tự động render các view để tự kiểm tra chất lượng giao diện
            if (args.Length > 0 && args[0] == "--render-audit")
            {
                RunVisualAudit();
                return;
            }

            // Mặc định khởi động màn hình Đăng nhập
            Application.Run(new FrmLogin());
        }

        private static void RunVisualAudit()
        {
            string outDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "huit-journal-ui-audit");
            System.IO.Directory.CreateDirectory(outDir);
            var auditEmail = Environment.GetEnvironmentVariable("HUIT_AUDIT_EMAIL");
            var auditPassword = Environment.GetEnvironmentVariable("HUIT_AUDIT_PASSWORD");
            if (string.IsNullOrWhiteSpace(auditEmail) || string.IsNullOrEmpty(auditPassword) || !AuthService.Login(auditEmail, auditPassword))
                throw new InvalidOperationException("Cần HUIT_AUDIT_EMAIL/HUIT_AUDIT_PASSWORD của tài khoản tòa soạn để chạy --render-audit.");

            using var frm = new FrmMain();
            frm.Size = new System.Drawing.Size(1180, 720);
            frm.StartPosition = FormStartPosition.Manual;
            frm.Location = new System.Drawing.Point(-2000, -2000); // Offscreen
            frm.Show();

            void SaveView(string viewKey, string filename)
            {
                frm.NavigateTo(viewKey);
                Application.DoEvents();
                System.Threading.Thread.Sleep(200);
                using var bmp = new System.Drawing.Bitmap(frm.Width, frm.Height);
                frm.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, frm.Width, frm.Height));
                string path = System.IO.Path.Combine(outDir, filename);
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine($"[AUDIT] Saved: {filename}");
            }

            Console.WriteLine("[AUDIT] --- VIETNAMESE PASS ---");
            LanguageService.SetLanguage("vi");
            SaveView("Dashboard", "audit_view_dashboard.png");
            SaveView("BaiBao", "audit_view_baibao.png");
            SaveView("PhanBien", "audit_view_phanbien.png");
            SaveView("SoTapChi", "audit_view_sotapchi.png");

            // Verify sorting on SoTapChi
            try
            {
                var grids = GetAllControls(frm).OfType<DataGridView>().ToList();
                var dgv = grids.FirstOrDefault(g => g.Columns.Contains("Ma"));
                if (dgv != null && dgv.Columns["Ma"] != null)
                {
                    dgv.Sort(dgv.Columns["Ma"], System.ComponentModel.ListSortDirection.Ascending);
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(200);
                    using var bmpSort = new System.Drawing.Bitmap(frm.Width, frm.Height);
                    frm.DrawToBitmap(bmpSort, new System.Drawing.Rectangle(0, 0, frm.Width, frm.Height));
                    bmpSort.Save(System.IO.Path.Combine(outDir, "audit_view_sotapchi_sorted.png"), System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"[AUDIT] Saved: audit_view_sotapchi_sorted.png (First row Ma={dgv.Rows[0].Cells["Ma"].Value})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Sort test: {ex.Message}");
            }

            SaveView("ChuyenNganh", "audit_view_chuyennganh.png");
            SaveView("SaoLuu", "audit_view_saoluu.png");
            SaveView("NguoiDung", "audit_view_nguoidung.png");
            SaveView("ThongKe", "audit_view_thongke.png");
            SaveView("AdminDashboard", "audit_view_admindash.png");

            Console.WriteLine("[AUDIT] --- ENGLISH PASS ---");
            LanguageService.SetLanguage("en");
            SaveView("Dashboard", "audit_view_en_dashboard.png");
            SaveView("BaiBao", "audit_view_en_baibao.png");
            SaveView("PhanBien", "audit_view_en_phanbien.png");
            SaveView("SoTapChi", "audit_view_en_sotapchi.png");
            SaveView("ChuyenNganh", "audit_view_en_chuyennganh.png");
            SaveView("SaoLuu", "audit_view_en_saoluu.png");
            SaveView("NguoiDung", "audit_view_en_nguoidung.png");
            SaveView("ThongKe", "audit_view_en_thongke.png");
            SaveView("AdminDashboard", "audit_view_en_admindash.png");

            // Detail form
            try
            {
                var list = BaiBaoService.GetAllBaiBao();
                if (list.Count > 0)
                {
                    using var frmDetail = new FrmChiTietBaiBao(list[0].MaBaiBao);
                    frmDetail.Size = new System.Drawing.Size(1050, 780);
                    frmDetail.StartPosition = FormStartPosition.Manual;
                    frmDetail.Location = new System.Drawing.Point(-2000, -2000);
                    frmDetail.Show();
                    Application.DoEvents();
                    using var bmpDetail = new System.Drawing.Bitmap(frmDetail.Width, frmDetail.Height);
                    frmDetail.DrawToBitmap(bmpDetail, new System.Drawing.Rectangle(0, 0, frmDetail.Width, frmDetail.Height));
                    bmpDetail.Save(System.IO.Path.Combine(outDir, "audit_view_chitiet.png"), System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine("[AUDIT] Saved: audit_view_chitiet.png");
                    frmDetail.Close();

                    // Audit FrmPhanCongPhanBien with COI
                    using var frmPhanCong = new FrmPhanCongPhanBien(list[0].MaBaiBao);
                    frmPhanCong.StartPosition = FormStartPosition.Manual;
                    frmPhanCong.Location = new System.Drawing.Point(-2000, -2000);
                    frmPhanCong.Show();
                    Application.DoEvents();
                    using var bmpPC = new System.Drawing.Bitmap(frmPhanCong.Width, frmPhanCong.Height);
                    frmPhanCong.DrawToBitmap(bmpPC, new System.Drawing.Rectangle(0, 0, frmPhanCong.Width, frmPhanCong.Height));
                    bmpPC.Save(System.IO.Path.Combine(outDir, "audit_view_phancong.png"), System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine("[AUDIT] Saved: audit_view_phancong.png");
                    frmPhanCong.Close();

                    // Audit FrmKiemTraDaoVanDialog
                    decimal tyLe = BaiBaoService.KiemTraDaoVan(list[0].MaBaiBao, out string kl);
                    using var frmDaoVan = new FrmKiemTraDaoVanDialog(list[0], tyLe, kl);
                    frmDaoVan.StartPosition = FormStartPosition.Manual;
                    frmDaoVan.Location = new System.Drawing.Point(-2000, -2000);
                    frmDaoVan.Show();
                    Application.DoEvents();
                    using var bmpDV = new System.Drawing.Bitmap(frmDaoVan.Width, frmDaoVan.Height);
                    frmDaoVan.DrawToBitmap(bmpDV, new System.Drawing.Rectangle(0, 0, frmDaoVan.Width, frmDaoVan.Height));
                    bmpDV.Save(System.IO.Path.Combine(outDir, "audit_view_daovan.png"), System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine("[AUDIT] Saved: audit_view_daovan.png");
                    frmDaoVan.Close();

                    // Audit FrmNguoiDungDialog
                    using var frmUserDlg = new FrmNguoiDungDialog();
                    frmUserDlg.StartPosition = FormStartPosition.Manual;
                    frmUserDlg.Location = new System.Drawing.Point(-2000, -2000);
                    frmUserDlg.Show();
                    Application.DoEvents();
                    using var bmpUD = new System.Drawing.Bitmap(frmUserDlg.Width, frmUserDlg.Height);
                    frmUserDlg.DrawToBitmap(bmpUD, new System.Drawing.Rectangle(0, 0, frmUserDlg.Width, frmUserDlg.Height));
                    bmpUD.Save(System.IO.Path.Combine(outDir, "audit_view_userdlg.png"), System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine("[AUDIT] Saved: audit_view_userdlg.png");
                    frmUserDlg.Close();

                    // Audit FrmLogin
                    using var frmLog = new FrmLogin();
                    frmLog.StartPosition = FormStartPosition.Manual;
                    frmLog.Location = new System.Drawing.Point(-2000, -2000);
                    frmLog.Show();
                    Application.DoEvents();
                    using var bmpLog = new System.Drawing.Bitmap(frmLog.Width, frmLog.Height);
                    frmLog.DrawToBitmap(bmpLog, new System.Drawing.Rectangle(0, 0, frmLog.Width, frmLog.Height));
                    bmpLog.Save(System.IO.Path.Combine(outDir, "audit_view_login.png"), System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine("[AUDIT] Saved: audit_view_login.png");
                    frmLog.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIT ERROR] Detail/Dialogs: {ex.Message}");
            }

            // Restore default Vietnamese
            LanguageService.SetLanguage("vi");
            frm.Close();
            Console.WriteLine("[AUDIT] Complete!");
        }

        private static System.Collections.Generic.IEnumerable<Control> GetAllControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var sub in GetAllControls(c))
                    yield return sub;
            }
        }
    }
}
