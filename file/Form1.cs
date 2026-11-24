using giaodien;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace giaodien
{
    public partial class Form1 : Form
    {
        private int soTrungCuTrenPLC = -1;
        private int soKhayCuTrenPLC = 0;
        private float phaChuyenDongTrung = 0f; // 0..1 giữa 2 quả; 0..2 trong 2s dừng băng tải chuyển khay
        private int hieuUngVoiDangTha = 0;     // 0: tắt, 1: Vòi 1 vừa thả, 2: Vòi 2 vừa thả
        private int demTickHieuUngVoi = 0;
        private int khoiLuongLoadcellCuT2 = -1;
        private int demTickDongPhanT2 = 0;
        private int phaDongPhanT2 = 0;

        // 2 dây chuyền phân chuyển động đồng bộ tốc độ
        private int khoiPhanX1 = 0;
        private int khoiPhanX2 = 190;
        private int viTriConLanPhan1 = 0;
        private int viTriConLanPhan2 = 9;



        private Panel pnlMiniTram2;
        private Panel pnlMangAn;
        private Panel pnlMangNuoc;
        private Panel pnlTienIchChuong;
        private Label lblBannerT1;
        private Label lblHeaderTrung;
        private Label lblHeaderPhan;
        private Label lblHeaderTienIch;
        private Label lblThongSoTram1;
        private Label lblLichLamViecT1;
        private Button btnCapPhanT1;
        private Button btnStartTram2_T1; // Nút Start Trạm 2 (%M0.3)
        private Button btnStopTram2_T1;  // Nút Stop Trạm 2 (%M1.6)
        private bool dangDocPLC1 = false;
        private bool isThuTrungOn = false;
        private bool isKeoCamOn = false;
        private bool isGatCamOn = false;
        private bool dangBamThemTrung = false;

        // Biến animation cho quạt thông gió (Q0.2) & sóng nhiệt sưởi (Q0.1)
        private float gocQuayQuat = 0f;
        private float phaSongNhiet = 0f;

        // Trạng thái mô phỏng cụm trượt thanh ray máng ăn & máng nước
        // 0.0f = chạm thành trong bên trái, 1.0f = chạm thành trong bên phải (mép xả)
        private float viTriTruotAn = 0f;
        private float viTriTruotNuoc = 0f;
        private float viTriBatDauCam = 0f;   // Giữ đúng mép trái lớp cám đã rải, tránh tự lấp đầy khi dừng giữa chừng
        private float viTriBatDauNuoc = 0f;
        private float mucCamSach = 0.75f;
        private float mucCamCu = 0f;
        private float mucNuocSach = 0.80f;
        private float mucNuocDong = 0f;
        private float mucRacAn = 0f;         // Mức thức ăn thừa tích trong hộp hứng thấp bên phải
        private float mucRacNuoc = 0f;       // Mức nước thải tích trong hộp hứng thấp bên phải
        private bool dangChuanBiCapAn = false;
        private bool dangChuanBiCapNuoc = false;
        private bool dangChuanBiDonAn = false;
        private bool dangChuanBiDonNuoc = false;
        private bool prevGatCam = false;
        private bool prevKeoCam = false;
        private int phaHatRoi = 0;

        // Hiệu ứng cạn dần & chuyển hóa sau 10s khi giữ nút Test (%M1.2 = 1)
        private bool dangTieuThuTest = false;
        private float tienTrinhTieuThu = 0f;

        private System.Windows.Forms.Timer timerAnimation;
        private System.Windows.Forms.Timer timerChuTrinhT1;

        public Form1()
        {
            InitializeComponent();
            ToiUuGiaoDienForm1();

            BatDoubleBuffered(pnlBangTaiTrung);
            BatDoubleBuffered(pnlBangTaiPhan);

            pnlBangTaiTrung.Paint += PnlBangTaiTrung_Paint;
            pnlBangTaiPhan.Paint += PnlBangTaiPhan_Paint;
            if (pnlMiniTram2 != null)
            {
                BatDoubleBuffered(pnlMiniTram2);
                pnlMiniTram2.Paint += PnlMiniTram2_Paint;
            }
            if (pnlMangAn != null)
            {
                BatDoubleBuffered(pnlMangAn);
                pnlMangAn.Paint += PnlMangAn_Paint;
            }
            if (pnlMangNuoc != null)
            {
                BatDoubleBuffered(pnlMangNuoc);
                pnlMangNuoc.Paint += PnlMangNuoc_Paint;
            }
            if (pnlTienIchChuong != null)
            {
                BatDoubleBuffered(pnlTienIchChuong);
                pnlTienIchChuong.Paint += PnlTienIchChuong_Paint;
            }
            this.FormClosing += (s, e) =>
            {
                timerAnimation?.Stop();
                timerChuTrinhT1?.Stop();
            };

            if (btnThemTrung != null) btnThemTrung.Click += BtnThemTrung_Click;
            if (btnThuTrungTayT1 != null) btnThuTrungTayT1.Click += BtnThuTrungTayT1_Click;
            if (btnStartT1 != null) btnStartT1.Click += BtnStartT1_Click;
            if (btnStopT1 != null) btnStopT1.Click += BtnStopT1_Click;
            if (btnGiaLapLoiU != null) btnGiaLapLoiU.Click += BtnGiaLapLoiU_Click;
            if (btnResetLoiT1 != null) btnResetLoiT1.Click += BtnResetLoiT1_Click;

            if (btnCheDoAutoT1 != null) btnCheDoAutoT1.Click += (s, e) => DatCheDoManual(false);
            if (btnCheDoManualT1 != null) btnCheDoManualT1.Click += (s, e) => DatCheDoManual(true);
            if (btnMoPhongKTO011 != null) btnMoPhongKTO011.Click += BtnMoPhongKTO011_Click;
            if (btnTestChuKyT1 != null) btnTestChuKyT1.Click += BtnTestChuKyT1_Click;
            if (btnResetSanLuongT1 != null) btnResetSanLuongT1.Click += BtnResetSanLuongT1_Click;

            // 2 nút Thêm cám/nước và Dọn cám/nước dạng bấm Toggle (set bit)
            if (btnKeoCamTayT1 != null) btnKeoCamTayT1.Click += BtnKeoCamTayT1_Click;
            if (btnGatCamTayT1 != null) btnGatCamTayT1.Click += BtnGatCamTayT1_Click;

            if (PlcDataShared.DaKetNoiPLC)
                PlcDataShared.DocDuLieuPLC1();

            soKhayCuTrenPLC = PlcDataShared.TongSoKhayTrung;
            khoiLuongLoadcellCuT2 = PlcDataShared.KhoiLuong_Loadcell;

            timerAnimation = new System.Windows.Forms.Timer();
            timerAnimation.Interval = 30;
            timerAnimation.Tick += TimerAnimation_Tick;
            timerAnimation.Start();

            timerChuTrinhT1 = new System.Windows.Forms.Timer();
            timerChuTrinhT1.Interval = 300;
            timerChuTrinhT1.Tick += TimerChuTrinhT1_Tick;
            timerChuTrinhT1.Start();

            CapNhatGiaoDienT1();
        }

        // 1. Sự kiện Toggle bấm 1 lần BẬT, bấm lần nữa TẮT cho nút CHẠY BĂNG TẢI CẤP TRỨNG (%M1.5)
        private void BtnThuTrungTayT1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            if (!PlcDataShared.M_CheDo_Manual)
            {
                MessageBox.Show("Nút Chạy băng tải cấp trứng chỉ hoạt động ở chế độ MANUAL!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                MessageBox.Show("Hệ thống đang khóa bởi lỗi (%M10.2)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isThuTrungOn = !isThuTrungOn;
            PlcDataShared.GhiBitPLC1(1, 5, isThuTrungOn); // %M1.5 M_Nut_ThuTrung_Tay
            CapNhatGiaoDienNutThuTrung();
        }

        private void CapNhatGiaoDienNutThuTrung()
        {
            if (btnThuTrungTayT1 == null) return;
            if (isThuTrungOn)
            {
                btnThuTrungTayT1.Text = "BTải TRỨNG: BẬT";
                btnThuTrungTayT1.BackColor = Color.FromArgb(0, 150, 80);
                btnThuTrungTayT1.ForeColor = Color.White;
            }
            else
            {
                btnThuTrungTayT1.Text = "CHẠY BTải TRỨNG";
                btnThuTrungTayT1.BackColor = Color.FromArgb(45, 50, 62);
                btnThuTrungTayT1.ForeColor = Color.White;
            }
        }

        // Toggle nút Thêm cám/nước (%M1.3 -> kích Q0.6)
        private void BtnKeoCamTayT1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            if (!PlcDataShared.M_CheDo_Manual)
            {
                MessageBox.Show("Vui lòng chuyển sang CHẾ ĐỘ TAY (MANUAL) trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                MessageBox.Show("Hệ thống đang khóa bởi lỗi (%M10.2)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isKeoCamOn = !isKeoCamOn;
            if (isKeoCamOn && isGatCamOn)
            {
                isGatCamOn = false;
                PlcDataShared.GhiBitPLC1(0, 6, false);
            }
            PlcDataShared.GhiBitPLC1(1, 3, isKeoCamOn); // %M1.3 M_Nut_KeoCam_Tay
            CapNhatGiaoDienNutAnUong();
        }

        // Toggle nút Dọn cám/nước (%M0.6 -> kích Q0.0)
        private void BtnGatCamTayT1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            if (!PlcDataShared.M_CheDo_Manual)
            {
                MessageBox.Show("Vui lòng chuyển sang CHẾ ĐỘ TAY (MANUAL) trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                MessageBox.Show("Hệ thống đang khóa bởi lỗi (%M10.2)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isGatCamOn = !isGatCamOn;
            if (isGatCamOn && isKeoCamOn)
            {
                isKeoCamOn = false;
                PlcDataShared.GhiBitPLC1(1, 3, false);
            }
            PlcDataShared.GhiBitPLC1(0, 6, isGatCamOn); // %M0.6 M_Nut_GatCam_Tay
            CapNhatGiaoDienNutAnUong();
        }

        private void CapNhatGiaoDienNutAnUong()
        {
            if (btnKeoCamTayT1 != null)
            {
                btnKeoCamTayT1.Text = isKeoCamOn ? "Thêm cám: BẬT" : "Thêm cám/nước";
                btnKeoCamTayT1.BackColor = isKeoCamOn ? Color.FromArgb(0, 150, 80) : Color.FromArgb(37, 99, 235);
                btnKeoCamTayT1.ForeColor = Color.White;
            }
            if (btnGatCamTayT1 != null)
            {
                btnGatCamTayT1.Text = isGatCamOn ? "Dọn cám: BẬT" : "Dọn cám/nước";
                btnGatCamTayT1.BackColor = isGatCamOn ? Color.FromArgb(217, 119, 6) : Color.FromArgb(37, 99, 235);
                btnGatCamTayT1.ForeColor = Color.White;
            }
        }

        // 2. Sự kiện nhấn nháy sáng vàng cho nút THÊM TRỨNG (TAY) (%M0.4)
        private async void BtnThemTrung_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            if (PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                MessageBox.Show("Hệ thống đang khóa bởi lỗi (%M10.2)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (dangBamThemTrung) return;

            dangBamThemTrung = true;
            try
            {
                btnThemTrung.BackColor = Color.FromArgb(230, 170, 0);
                btnThemTrung.ForeColor = Color.Black;

                PlcDataShared.GhiBitPLC1(0, 4, true); // %M0.4 = 1
                await Task.Delay(200);
                PlcDataShared.GhiBitPLC1(0, 4, false); // %M0.4 = 0
            }
            finally
            {
                btnThemTrung.BackColor = Color.FromArgb(45, 50, 62);
                btnThemTrung.ForeColor = Color.White;
                dangBamThemTrung = false;
            }
        }

        private void ApDungStyleButton(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Button btn)
                {
                    btn.UseVisualStyleBackColor = false;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(30, 41, 59);
                    btn.Cursor = Cursors.Hand;
                    btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                    btn.BackColor = Color.FromArgb(51, 65, 85);
                    btn.ForeColor = Color.White;
                }
                if (c.HasChildren)
                    ApDungStyleButton(c);
            }
        }

        private void TaoNutResetDon(Control parent, string designName, string text, int x, int y, int width, Action onClick)
        {
            Button btn = new Button
            {
                Name = designName,
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 32),
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                BackColor = Color.FromArgb(88, 28, 135),
                ForeColor = Color.White
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            parent.Controls.Add(btn);
            btn.BringToFront();
        }

        private void ToiUuGiaoDienForm1()
        {
            Color bgMain = Color.FromArgb(24, 25, 28);     // #18191c
            Color bgPanel = Color.FromArgb(34, 36, 40);    // #222428
            Color bgCard = Color.FromArgb(43, 46, 51);     // #2b2e33
            Color fgText = Color.FromArgb(241, 243, 245);  // #f1f3f5
            Color fgMuted = Color.FromArgb(173, 181, 189); // #adb5bd

            this.Size = new Size(1460, 880);
            this.MinimumSize = new Size(1280, 780);
            this.BackColor = bgMain;
            ApDungStyleButton(this);

            // Ẩn toàn bộ các control cũ gây trùng lặp / thừa thanh ghi MW
            if (btnBatTatPlc2T1 != null) btnBatTatPlc2T1.Visible = false;
            if (lblSoKhay != null) lblSoKhay.Visible = false;
            if (lblTrangThaiThietBiT1 != null) lblTrangThaiThietBiT1.Visible = false;
            if (lblHienThiMW_PLC1 != null) lblHienThiMW_PLC1.Visible = false;

            foreach (Control c in this.Controls)
            {
                if (c is Button oldBtn && (oldBtn.Name.StartsWith("btnResetMW") || oldBtn.Name.StartsWith("btnResetAllMW")))
                    oldBtn.Visible = false;
            }

            // ================= 1. THANH TOOLBAR TRÊN CÙNG =================
            Panel pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = bgPanel
            };
            this.Controls.Add(pnlToolbar);

            int btnY = 9, btnH = 36;
            if (btnQuayLai != null)
            {
                btnQuayLai.Text = "← Quay lại";
                btnQuayLai.Size = new Size(100, btnH);
                btnQuayLai.Location = new Point(12, btnY);
                btnQuayLai.BackColor = Color.FromArgb(55, 65, 81);
                pnlToolbar.Controls.Add(btnQuayLai);
            }
            if (btnStartT1 != null)
            {
                btnStartT1.Text = "▶ START";
                btnStartT1.Size = new Size(95, btnH);
                btnStartT1.Location = new Point(120, btnY);
                btnStartT1.BackColor = Color.FromArgb(16, 185, 129);
                pnlToolbar.Controls.Add(btnStartT1);
            }
            if (btnStopT1 != null)
            {
                btnStopT1.Text = "■ STOP";
                btnStopT1.Size = new Size(95, btnH);
                btnStopT1.Location = new Point(223, btnY);
                btnStopT1.BackColor = Color.FromArgb(220, 38, 38);
                pnlToolbar.Controls.Add(btnStopT1);
            }
            if (btnCheDoAutoT1 != null)
            {
                btnCheDoAutoT1.Text = "AUTO";
                btnCheDoAutoT1.Size = new Size(85, btnH);
                btnCheDoAutoT1.Location = new Point(332, btnY);
                pnlToolbar.Controls.Add(btnCheDoAutoT1);
            }
            if (btnCheDoManualT1 != null)
            {
                btnCheDoManualT1.Text = "MANUAL";
                btnCheDoManualT1.Size = new Size(95, btnH);
                btnCheDoManualT1.Location = new Point(423, btnY);
                pnlToolbar.Controls.Add(btnCheDoManualT1);
            }
            if (btnGiaLapLoiU != null)
            {
                btnGiaLapLoiU.Text = "Kích lỗi kẹt T1 (%M0.7)";
                btnGiaLapLoiU.Size = new Size(165, btnH);
                btnGiaLapLoiU.Location = new Point(532, btnY);
                btnGiaLapLoiU.BackColor = Color.FromArgb(185, 28, 28);
                pnlToolbar.Controls.Add(btnGiaLapLoiU);
            }
            if (btnResetLoiT1 != null)
            {
                btnResetLoiT1.Text = "Tắt lỗi / Reset";
                btnResetLoiT1.Size = new Size(125, btnH);
                btnResetLoiT1.Location = new Point(705, btnY);
                btnResetLoiT1.BackColor = Color.FromArgb(13, 148, 136);
                pnlToolbar.Controls.Add(btnResetLoiT1);
            }
            if (lblDongHoPLC1 != null)
            {
                lblDongHoPLC1.UseMnemonic = false;
                lblDongHoPLC1.AutoSize = true;
                lblDongHoPLC1.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lblDongHoPLC1.ForeColor = fgText;
                lblDongHoPLC1.Location = new Point(pnlToolbar.Width - 235, 17);
                lblDongHoPLC1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                pnlToolbar.Controls.Add(lblDongHoPLC1);
            }

            // ================= 2. CỘT PHẢI: THU NHỎ TRẠM 2 & RESET MW =================
            Panel pnlRightCol = new Panel
            {
                Dock = DockStyle.Right,
                Width = 380,
                BackColor = bgPanel,
                AutoScroll = true
            };
            this.Controls.Add(pnlRightCol);

            Label lblTitleT2 = new Label
            {
                Text = "TRẠM 2: Ủ HOAI - SẤY - ĐÓNG BAO (MINI)",
                UseMnemonic = false,
                ForeColor = fgMuted,
                Font = new Font("Segoe UI", 8.9f, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(356, 22),
                Location = new Point(10, 8)
            };
            pnlRightCol.Controls.Add(lblTitleT2);

            // 2 nút Start / Stop Trạm 2 trỏ thẳng vào biến %M0.3 và %M1.6 của PLC 1
            btnStartTram2_T1 = new Button
            {
                Name = "btnStartTram2_T1",
                Text = "▶ START T2 (%M0.3)",
                Location = new Point(18, 34),
                Size = new Size(164, 32),
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White
            };
            btnStartTram2_T1.FlatAppearance.BorderSize = 0;
            btnStartTram2_T1.Click += BtnStartTram2_T1_Click;
            pnlRightCol.Controls.Add(btnStartTram2_T1);

            btnStopTram2_T1 = new Button
            {
                Name = "btnStopTram2_T1",
                Text = "■ STOP T2 (%M1.6)",
                Location = new Point(194, 34),
                Size = new Size(164, 32),
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White
            };
            btnStopTram2_T1.FlatAppearance.BorderSize = 0;
            btnStopTram2_T1.Click += BtnStopTram2_T1_Click;
            pnlRightCol.Controls.Add(btnStopTram2_T1);

            pnlMiniTram2 = new Panel
            {
                Location = new Point(10, 72),
                Size = new Size(356, 252),
                BackColor = Color.FromArgb(17, 18, 20),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRightCol.Controls.Add(pnlMiniTram2);

            Panel pnlCardPutGet = new Panel
            {
                Location = new Point(10, 332),
                Size = new Size(356, 208),
                BackColor = bgCard
            };
            pnlRightCol.Controls.Add(pnlCardPutGet);

            Label lblTitlePG = new Label
            {
                Text = "GIAO TIẾP PUT/GET PLC 1 ↔ PLC 2",
                UseMnemonic = false,
                ForeColor = fgMuted,
                Font = new Font("Segoe UI", 8.4f, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(340, 20),
                Location = new Point(8, 6)
            };
            pnlCardPutGet.Controls.Add(lblTitlePG);

            if (lblThongTinGui != null)
            {
                lblThongTinGui.Visible = true;
                lblThongTinGui.UseMnemonic = false;
                lblThongTinGui.AutoSize = true;
                lblThongTinGui.MaximumSize = new Size(328, 0);
                lblThongTinGui.ForeColor = Color.FromArgb(147, 197, 253);
                lblThongTinGui.Font = new Font("Segoe UI", 8.4f, FontStyle.Regular);
                lblThongTinGui.Location = new Point(14, 30);
                pnlCardPutGet.Controls.Add(lblThongTinGui);
            }
            if (lblPhanHoiPlc2 != null)
            {
                lblPhanHoiPlc2.Visible = true;
                lblPhanHoiPlc2.UseMnemonic = false;
                lblPhanHoiPlc2.AutoSize = true;
                lblPhanHoiPlc2.MaximumSize = new Size(328, 0);
                lblPhanHoiPlc2.ForeColor = Color.FromArgb(253, 224, 71);
                lblPhanHoiPlc2.Font = new Font("Segoe UI", 8.4f, FontStyle.Bold);
                lblPhanHoiPlc2.Location = new Point(14, 108);
                pnlCardPutGet.Controls.Add(lblPhanHoiPlc2);
            }
            if (lblTrangThaiPlc2T1 != null)
            {
                lblTrangThaiPlc2T1.Visible = true;
                lblTrangThaiPlc2T1.UseMnemonic = false;
                lblTrangThaiPlc2T1.AutoSize = false;
                lblTrangThaiPlc2T1.TextAlign = ContentAlignment.MiddleCenter;
                lblTrangThaiPlc2T1.Size = new Size(340, 22);
                lblTrangThaiPlc2T1.Font = new Font("Segoe UI", 8.6f, FontStyle.Bold);
                lblTrangThaiPlc2T1.Location = new Point(8, 178);
                pnlCardPutGet.Controls.Add(lblTrangThaiPlc2T1);
            }

            Panel pnlCardReset = new Panel
            {
                Location = new Point(10, 548),
                Size = new Size(356, 166),
                BackColor = bgCard
            };
            pnlRightCol.Controls.Add(pnlCardReset);

            Label lblTitleRst = new Label
            {
                Text = "RESET THANH GHI MW PLC 1",
                UseMnemonic = false,
                ForeColor = fgMuted,
                Font = new Font("Segoe UI", 8.4f, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(340, 20),
                Location = new Point(8, 8)
            };
            pnlCardReset.Controls.Add(lblTitleRst);

            TaoNutResetDon(pnlCardReset, "btnResetMW20_T1", "MW20 (Trứng=0)", 12, 32, 162, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.GhiMW_PLC1(20, 0);
                PlcDataShared.DocDuLieuPLC1();
                CapNhatGiaoDienT1();
            });
            TaoNutResetDon(pnlCardReset, "btnResetMW22_T1", "MW22 (Khay=0)", 182, 32, 162, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.GhiMW_PLC1(22, 0);
                soKhayCuTrenPLC = 0;
                PlcDataShared.DocDuLieuPLC1();
                CapNhatGiaoDienT1();
            });
            TaoNutResetDon(pnlCardReset, "btnResetMW24_T1", "MW24 (Trong khay=0)", 12, 72, 162, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.GhiMW_PLC1(24, 0);
                soTrungCuTrenPLC = -1;
                phaChuyenDongTrung = 0f;
                PlcDataShared.DocDuLieuPLC1();
                pnlBangTaiTrung.Invalidate();
                CapNhatGiaoDienT1();
            });
            TaoNutResetDon(pnlCardReset, "btnResetAllMW_T1", "Reset ALL MW1", 182, 72, 162, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.XungBitPLC1(1, 1);
                PlcDataShared.GhiMW_PLC1(20, 0);
                PlcDataShared.GhiMW_PLC1(22, 0);
                PlcDataShared.GhiMW_PLC1(24, 0);
                soKhayCuTrenPLC = 0;
                soTrungCuTrenPLC = -1;
                phaChuyenDongTrung = 0f;
                PlcDataShared.DocDuLieuPLC1();
                pnlBangTaiTrung.Invalidate();
                CapNhatGiaoDienT1();
            });

            if (btnResetSanLuongT1 != null)
            {
                btnResetSanLuongT1.Visible = true;
                btnResetSanLuongT1.Text = "Reset sản lượng (%M1.1)";
                btnResetSanLuongT1.BackColor = Color.FromArgb(79, 70, 229);
                btnResetSanLuongT1.Size = new Size(332, 34);
                btnResetSanLuongT1.Location = new Point(12, 114);
                pnlCardReset.Controls.Add(btnResetSanLuongT1);
            }

            // ================= 3. VÙNG CHÍNH BÊN TRÁI: TRẠM 1 =================
            Panel pnlMainArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgMain,
                Padding = new Padding(12)
            };
            this.Controls.Add(pnlMainArea);

            pnlToolbar.SendToBack();
            pnlMainArea.BringToFront();

            lblBannerT1 = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                UseMnemonic = false,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = bgCard,
                ForeColor = Color.White,
                Text = "TRẠM 1: CHUỒNG NUÔI, THU TRỨNG & TÁCH PHÂN"
            };
            pnlMainArea.Controls.Add(lblBannerT1);

            Panel pnlControlDeck = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = Color.FromArgb(95, 106, 124) // Slate dịu
            };
            pnlMainArea.Controls.Add(pnlControlDeck);

            if (btnThemTrung != null)
            {
                btnThemTrung.Text = "THÊM TRỨNG (TAY)";
                btnThemTrung.BackColor = Color.FromArgb(45, 50, 62);
                btnThemTrung.ForeColor = Color.White;
                btnThemTrung.Size = new Size(152, 34);
                btnThemTrung.Location = new Point(174, 48);
                pnlControlDeck.Controls.Add(btnThemTrung);
            }
            if (btnMoPhongKTO011 != null)
            {
                btnMoPhongKTO011.Size = new Size(175, 34);
                btnMoPhongKTO011.Location = new Point(174, 8);
                pnlControlDeck.Controls.Add(btnMoPhongKTO011);
            }
            if (btnTestChuKyT1 != null)
            {
                btnTestChuKyT1.Size = new Size(215, 34);
                btnTestChuKyT1.Location = new Point(357, 8);
                pnlControlDeck.Controls.Add(btnTestChuKyT1);
            }

            btnCapPhanT1 = new Button
            {
                Name = "btnCapPhanT1",
                Text = "BT PHÂN (TAY): TẮT",
                Size = new Size(195, 34),
                Location = new Point(628, 48),
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 12, 0)
            };
            btnCapPhanT1.FlatAppearance.BorderSize = 1;
            btnCapPhanT1.FlatAppearance.BorderColor = Color.FromArgb(100, 116, 139);
            btnCapPhanT1.Click += BtnCapPhanT1_Click;
            btnCapPhanT1.Paint += BtnCapPhanT1_Paint;
            pnlControlDeck.Controls.Add(btnCapPhanT1);

            if (btnThuTrungTayT1 != null)
            {
                btnThuTrungTayT1.Text = "CHẠY BTải TRỨNG";
                btnThuTrungTayT1.BackColor = Color.FromArgb(45, 50, 62);
                btnThuTrungTayT1.ForeColor = Color.White;
                btnThuTrungTayT1.Size = new Size(158, 34);
                btnThuTrungTayT1.Location = new Point(10, 48);
                pnlControlDeck.Controls.Add(btnThuTrungTayT1);
            }
            if (btnKeoCamTayT1 != null)
            {
                btnKeoCamTayT1.Text = "Thêm cám/nước";
                btnKeoCamTayT1.BackColor = Color.FromArgb(37, 99, 235);
                btnKeoCamTayT1.Size = new Size(142, 34);
                btnKeoCamTayT1.Location = new Point(332, 48);
                pnlControlDeck.Controls.Add(btnKeoCamTayT1);
            }
            if (btnGatCamTayT1 != null)
            {
                btnGatCamTayT1.Text = "Dọn cám/nước";
                btnGatCamTayT1.BackColor = Color.FromArgb(37, 99, 235);
                btnGatCamTayT1.Size = new Size(142, 34);
                btnGatCamTayT1.Location = new Point(480, 48);
                pnlControlDeck.Controls.Add(btnGatCamTayT1);
            }

            // ================= VÙNG ĐÁY: TRÁI (TRẠNG THÁI + 2 CỘT THÔNG SỐ & LỊCH) | PHẢI (MÁNG ĂN + MÁNG NƯỚC) =================
            TableLayoutPanel tblBottomDeck = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 216,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = bgMain,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tblBottomDeck.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f)); // Trái: Dòng trạng thái hệ thống + 2 cột thông số & lịch
            tblBottomDeck.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f)); // Phải: Máng thức ăn & Máng nước
            tblBottomDeck.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            pnlMainArea.Controls.Add(tblBottomDeck);

            // Khối trái: Hàng 1 = Thanh trạng thái hệ thống | Hàng 2 = 2 cột (Thông số thống kê | Lịch vận hành tiếp theo)
            TableLayoutPanel pnlBottomInfoCard = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = bgPanel,
                Padding = new Padding(6, 5, 6, 5),
                Margin = new Padding(0, 0, 6, 0)
            };
            pnlBottomInfoCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlBottomInfoCard.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            pnlBottomInfoCard.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblBottomDeck.Controls.Add(pnlBottomInfoCard, 0, 0);

            if (lblCanhBaoLoiT1 != null)
            {
                lblCanhBaoLoiT1.Visible = true;
                lblCanhBaoLoiT1.UseMnemonic = false;
                lblCanhBaoLoiT1.AutoSize = false;
                lblCanhBaoLoiT1.Dock = DockStyle.Fill;
                lblCanhBaoLoiT1.TextAlign = ContentAlignment.MiddleLeft;
                lblCanhBaoLoiT1.Padding = new Padding(8, 0, 8, 0);
                lblCanhBaoLoiT1.Margin = new Padding(0, 0, 0, 4);
                lblCanhBaoLoiT1.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
                pnlBottomInfoCard.Controls.Add(lblCanhBaoLoiT1, 0, 0);
            }

            TableLayoutPanel tblInfo2Cols = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tblInfo2Cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblInfo2Cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblInfo2Cols.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            pnlBottomInfoCard.Controls.Add(tblInfo2Cols, 0, 1);

            // --- CỘT 1: THÔNG SỐ THỐNG KÊ TRẠM 1 ---
            TableLayoutPanel pnlColThongSo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.FromArgb(26, 28, 33),
                Padding = new Padding(10, 5, 8, 5),
                Margin = new Padding(0, 0, 3, 0)
            };
            pnlColThongSo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlColThongSo.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            pnlColThongSo.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblInfo2Cols.Controls.Add(pnlColThongSo, 0, 0);

            Label lblHeaderThongSo = new Label
            {
                Text = "THÔNG SỐ THỐNG KÊ TRẠM 1",
                UseMnemonic = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(147, 197, 253),
                Font = new Font("Segoe UI", 8.3f, FontStyle.Bold)
            };
            pnlColThongSo.Controls.Add(lblHeaderThongSo, 0, 0);

            lblThongSoTram1 = new Label
            {
                Name = "lblThongSoTram1",
                UseMnemonic = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(4, 2, 0, 0),
                ForeColor = fgText,
                Font = new Font("Segoe UI", 8.4f, FontStyle.Regular)
            };
            pnlColThongSo.Controls.Add(lblThongSoTram1, 0, 1);

            // --- CỘT 2: LỊCH VẬN HÀNH TIẾP THEO ---
            TableLayoutPanel pnlColLich = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.FromArgb(26, 28, 33),
                Padding = new Padding(10, 5, 8, 5),
                Margin = new Padding(3, 0, 0, 0)
            };
            pnlColLich.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pnlColLich.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            pnlColLich.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblInfo2Cols.Controls.Add(pnlColLich, 1, 0);

            Label lblHeaderLich = new Label
            {
                Text = "THỜI GIAN CHỜ CHU KỲ TIẾP THEO",
                UseMnemonic = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(253, 224, 71),
                Font = new Font("Segoe UI", 8.3f, FontStyle.Bold)
            };
            pnlColLich.Controls.Add(lblHeaderLich, 0, 0);

            lblLichLamViecT1 = new Label
            {
                Name = "lblLichLamViecT1",
                UseMnemonic = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(4, 2, 0, 0),
                ForeColor = fgText,
                Font = new Font("Segoe UI", 8.4f, FontStyle.Regular)
            };
            pnlColLich.Controls.Add(lblLichLamViecT1, 0, 1);

            // Cột bên phải chia đôi theo chiều dọc: trên = Máng thức ăn, dưới = Máng nước
            TableLayoutPanel tblMangAnNuoc = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = bgMain,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tblMangAnNuoc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblMangAnNuoc.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblMangAnNuoc.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblBottomDeck.Controls.Add(tblMangAnNuoc, 1, 0);

            pnlMangAn = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(17, 18, 20),
                Margin = new Padding(0, 0, 0, 3),
                BorderStyle = BorderStyle.FixedSingle
            };
            tblMangAnNuoc.Controls.Add(pnlMangAn, 0, 0);

            pnlMangNuoc = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(17, 18, 20),
                Margin = new Padding(0, 3, 0, 0),
                BorderStyle = BorderStyle.FixedSingle
            };
            tblMangAnNuoc.Controls.Add(pnlMangNuoc, 0, 1);

            // ================= 4. KHU VỰC MÔ PHỎNG TRUNG TÂM =================
            TableLayoutPanel tblSim = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = bgMain,
                Padding = new Padding(0, 6, 0, 6),
                Margin = new Padding(0)
            };
            tblSim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblSim.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            tblSim.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));
            tblSim.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            tblSim.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            pnlMainArea.Controls.Add(tblSim);

            lblBannerT1.SendToBack();
            pnlControlDeck.BringToFront();
            tblSim.BringToFront();

            lblHeaderTrung = new Label
            {
                Dock = DockStyle.Fill,
                UseMnemonic = false,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(125, 211, 252),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(0),
                Text = "Băng tải đóng khay trứng: Đang chờ khung giờ (10h00 & 17h00)"
            };
            tblSim.Controls.Add(lblHeaderTrung, 0, 0);

            if (pnlBangTaiTrung != null)
            {
                pnlBangTaiTrung.Dock = DockStyle.Fill;
                pnlBangTaiTrung.Margin = new Padding(0);
                pnlBangTaiTrung.BackColor = Color.FromArgb(17, 18, 20);
                tblSim.Controls.Add(pnlBangTaiTrung, 0, 1);
            }

            // --- HÀNG 2: THANH TIÊU ĐỀ CHIA ĐÔI 50% - 50% (TRÁI: PHÂN | PHẢI: ĐIỆN - SƯỞI - QUẠT) ---
            TableLayoutPanel tblHeaderBottomSim = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0, 4, 0, 0)
            };
            tblHeaderBottomSim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblHeaderBottomSim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblHeaderBottomSim.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblSim.Controls.Add(tblHeaderBottomSim, 0, 2);

            Panel pnlSepPhan = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 4, 0),
                BackColor = Color.FromArgb(46, 38, 34)
            };
            pnlSepPhan.Paint += (s, e) =>
            {
                using (Pen pTop = new Pen(Color.FromArgb(217, 119, 6), 2))
                    e.Graphics.DrawLine(pTop, 0, 1, pnlSepPhan.Width, 1);
            };
            lblHeaderPhan = new Label
            {
                Dock = DockStyle.Fill,
                UseMnemonic = false,
                BackColor = Color.Transparent,
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 2, 8, 0),
                Text = "Băng tải phân thô (2 dây chuyền): Đang chờ chu kỳ kéo"
            };
            pnlSepPhan.Controls.Add(lblHeaderPhan);
            tblHeaderBottomSim.Controls.Add(pnlSepPhan, 0, 0);

            Panel pnlSepTienIch = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 0, 0),
                BackColor = Color.FromArgb(30, 41, 59)
            };
            pnlSepTienIch.Paint += (s, e) =>
            {
                using (Pen pTop = new Pen(Color.FromArgb(56, 189, 248), 2))
                    e.Graphics.DrawLine(pTop, 0, 1, pnlSepTienIch.Width, 1);
            };
            lblHeaderTienIch = new Label
            {
                Dock = DockStyle.Fill,
                UseMnemonic = false,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(186, 230, 253),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 2, 8, 0),
                Text = "Tiện ích chuồng: Điện & Đèn (Q0.3) | Sưởi (Q0.1) | Quạt (Q0.2)"
            };
            pnlSepTienIch.Controls.Add(lblHeaderTienIch);
            tblHeaderBottomSim.Controls.Add(pnlSepTienIch, 1, 0);

            // --- HÀNG 3: NỘI DUNG MÔ PHỎNG CHIA ĐÔI 50% - 50% ---
            TableLayoutPanel tblBottomSimSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = bgMain,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tblBottomSimSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblBottomSimSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblBottomSimSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblSim.Controls.Add(tblBottomSimSplit, 0, 3);

            if (pnlBangTaiPhan != null)
            {
                pnlBangTaiPhan.Dock = DockStyle.Fill;
                pnlBangTaiPhan.Margin = new Padding(0, 0, 4, 0);
                pnlBangTaiPhan.BackColor = Color.FromArgb(17, 18, 20);
                tblBottomSimSplit.Controls.Add(pnlBangTaiPhan, 0, 0);
            }

            pnlTienIchChuong = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 0, 0),
                BackColor = Color.FromArgb(17, 18, 20),
                BorderStyle = BorderStyle.FixedSingle
            };
            tblBottomSimSplit.Controls.Add(pnlTienIchChuong, 1, 0);
        }

        // Bật Trạm 2 từ PLC 1: nhả bit dừng %M1.6 và giữ bit chạy %M0.3 = 1
        private void BtnStartTram2_T1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            PlcDataShared.GhiBitPLC1(1, 6, false); // %M1.6 hmi_stop_plc2 = 0
            PlcDataShared.GhiBitPLC1(0, 3, true);  // %M0.3 M_HMI_BatPLC2 = 1
        }

        // Tắt Trạm 2 từ PLC 1: ngắt %M0.3 và bật bit dừng %M1.6 = 1
        private void BtnStopTram2_T1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            PlcDataShared.GhiBitPLC1(0, 3, false); // %M0.3 M_HMI_BatPLC2 = 0
            PlcDataShared.GhiBitPLC1(1, 6, true);  // %M1.6 hmi_stop_plc2 = 1
        }

        private void BtnCapPhanT1_Paint(object sender, PaintEventArgs e)
        {
            if (btnCapPhanT1 == null) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool dangChay = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && (PlcDataShared.Q_BangTaiPhan || PlcDataShared.guidi_1_DangCapPhan == 1);
            Color ledColor = dangChay ? Color.FromArgb(74, 222, 128) : Color.FromArgb(148, 163, 184);

            int ledSize = 11;
            int ledX = 12;
            int ledY = (btnCapPhanT1.Height - ledSize) / 2;

            if (dangChay)
            {
                using (SolidBrush halo = new SolidBrush(Color.FromArgb(75, 34, 197, 94)))
                    g.FillEllipse(halo, ledX - 4, ledY - 4, ledSize + 8, ledSize + 8);
            }
            using (SolidBrush brLed = new SolidBrush(ledColor))
                g.FillEllipse(brLed, ledX, ledY, ledSize, ledSize);
        }

        private void BtnCapPhanT1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            if (!PlcDataShared.M_CheDo_Manual)
            {
                MessageBox.Show("Nút này chỉ hoạt động ở chế độ MANUAL!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                MessageBox.Show("Nút đang bị khóa bởi lỗi (%M10.2)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool trangThaiMoi = !PlcDataShared.Q_BangTaiPhan;
            PlcDataShared.GhiBitPLC1(0, 5, trangThaiMoi); // %M0.5 M_Nut_CapPhan_Tay
        }

        private bool KiemTraKetNoi()
        {
            if (!PlcDataShared.DaKetNoiPLC)
            {
                MessageBox.Show("Chưa kết nối PLC! Vui lòng quay lại màn hình chính và bấm 'KẾT NỐI 2 PLC'.", "Chưa kết nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!PlcDataShared.PLC1_DangRun)
            {
                MessageBox.Show("CPU PLC 1 trên TIA Portal / PLCSIM đang STOP! Hãy chuyển sang RUN trước.", "CPU đang STOP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void BatDoubleBuffered(Panel pnl)
        {
            if (pnl == null) return;
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, pnl, new object[] { true });
        }

        private void DatCheDoManual(bool isManual)
        {
            if (!KiemTraKetNoi()) return;
            if (!isManual)
            {
                if (isThuTrungOn)
                {
                    isThuTrungOn = false;
                    PlcDataShared.GhiBitPLC1(1, 5, false);
                    CapNhatGiaoDienNutThuTrung();
                }
                if (isKeoCamOn || isGatCamOn)
                {
                    isKeoCamOn = false;
                    isGatCamOn = false;
                    PlcDataShared.GhiBitPLC1(1, 3, false);
                    PlcDataShared.GhiBitPLC1(0, 6, false);
                    CapNhatGiaoDienNutAnUong();
                }
            }
            PlcDataShared.GhiBitPLC1(0, 2, isManual); // %M0.2 M_HMI_Manual
        }

        private void BtnMoPhongKTO011_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            PlcDataShared.GhiBitPLC1(1, 4, !PlcDataShared.M_HMI_KTO011); // %M1.4 M_HMI_KTO011
        }

        private void BtnStartT1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            if (PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                MessageBox.Show("Đang khóa do lỗi kẹt quy trình (%M10.2)! Hãy bấm 'Tắt lỗi / Reset' trước.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            PlcDataShared.XungBitPLC1(0, 0); // %M0.0 M_HMI_Start
        }

        private void BtnStopT1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            PlcDataShared.XungBitPLC1(0, 1); // %M0.1 M_HMI_Stop
        }

        private void BtnTestChuKyT1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            PlcDataShared.GhiBitPLC1(1, 2, !PlcDataShared.M_TestChuKy_Active); // %M1.2 M_HMI_Test
        }

        private void BtnResetSanLuongT1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            PlcDataShared.XungBitPLC1(1, 1); // %M1.1 M_HMI_ResetSanLuong
            PlcDataShared.GhiMW_PLC1(20, 0);
            PlcDataShared.GhiMW_PLC1(22, 0);
            PlcDataShared.GhiMW_PLC1(24, 0);
            soKhayCuTrenPLC = 0;
            soTrungCuTrenPLC = -1;
            phaChuyenDongTrung = 0f;
        }

        private void BtnGiaLapLoiU_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            PlcDataShared.GhiBitPLC1(0, 7, !PlcDataShared.M_Sim_Loi_T1_Active); // %M0.7 M_Sim_LoiKetU
        }

        private void BtnResetLoiT1_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            isThuTrungOn = false;
            isKeoCamOn = false;
            isGatCamOn = false;
            dangTieuThuTest = false;
            tienTrinhTieuThu = 0f;
            dangChuanBiCapAn = false;
            dangChuanBiCapNuoc = false;
            dangChuanBiDonAn = false;
            dangChuanBiDonNuoc = false;
            CapNhatGiaoDienNutThuTrung();
            CapNhatGiaoDienNutAnUong();

            PlcDataShared.GhiBitPLC1(0, 7, false);
            PlcDataShared.GhiBitPLC1(0, 5, false);
            PlcDataShared.GhiBitPLC1(1, 5, false);
            PlcDataShared.GhiBitPLC1(1, 3, false);
            PlcDataShared.GhiBitPLC1(0, 6, false);
            PlcDataShared.XungBitPLC1(1, 0);
        }

        private async void TimerChuTrinhT1_Tick(object sender, EventArgs e)
        {
            if (PlcDataShared.DaKetNoiPLC && !dangDocPLC1)
            {
                dangDocPLC1 = true;
                try
                {
                    await Task.Run(() =>
                    {
                        PlcDataShared.DocDuLieuPLC1();
                        PlcDataShared.DocDuLieuPLC2();
                    });
                }
                catch { }
                finally
                {
                    dangDocPLC1 = false;
                }

                if (PlcDataShared.PLC1_DangRun)
                {
                    int curTrung = PlcDataShared.SoTrung_TrongKhay;
                    int curKhay = PlcDataShared.TongSoKhayTrung;

                    if (soTrungCuTrenPLC == -1)
                    {
                        soTrungCuTrenPLC = curTrung;
                        soKhayCuTrenPLC = curKhay;
                    }
                    else if (curTrung != soTrungCuTrenPLC || curKhay != soKhayCuTrenPLC)
                    {
                        // Reset pha mượt mỗi khi PLC đếm thêm 1 quả hoặc vừa reset về khay mới (0)
                        phaChuyenDongTrung = 0f;

                        if (curTrung > 0 && curTrung <= 12)
                        {
                            // Quả lẻ (1,3,5,7,9,11) -> Vòi 1 (hàng trên); Quả chẵn (2,4,6,8,10,12) -> Vòi 2 (hàng dưới)
                            hieuUngVoiDangTha = (curTrung % 2 == 1) ? 1 : 2;
                            demTickHieuUngVoi = 8; // Nháy hiệu ứng thả ~240ms
                        }

                        soTrungCuTrenPLC = curTrung;
                        soKhayCuTrenPLC = curKhay;
                    }
                }
            }

            pnlBangTaiTrung.Invalidate();
            pnlBangTaiPhan.Invalidate();
            pnlMiniTram2?.Invalidate();
            pnlMangAn?.Invalidate();
            pnlMangNuoc?.Invalidate();
            pnlTienIchChuong?.Invalidate();
            CapNhatGiaoDienT1();
        }

        private void LayGioPhutHienTaiPLC(out int gio, out int phut)
        {
            string chuoiTG = PlcDataShared.LayChuoiThoiGianPLC() ?? "";
            Match m = Regex.Match(chuoiTG, @"\b(\d{1,2}):(\d{2})");
            if (m.Success
                && int.TryParse(m.Groups[1].Value, out int h)
                && int.TryParse(m.Groups[2].Value, out int min)
                && h >= 0 && h <= 23 && min >= 0 && min <= 59)
            {
                gio = h;
                phut = min;
                return;
            }
            DateTime now = DateTime.Now;
            gio = now.Hour;
            phut = now.Minute;
        }

        private string DinhDangGioPhut(int tongPhut)
        {
            if (tongPhut < 0) tongPhut = 0;
            int soGioCon = tongPhut / 60;
            int soPhutCon = tongPhut % 60;
            return soPhutCon == 0
                ? $"{soGioCon} giờ"
                : $"{soGioCon}h{soPhutCon:D2}p";
        }

        private string DinhDangThoiGianChoLich(int gioHienTai, int phutHienTai, int[] danhSachGioChay)
        {
            bool dangTrongKhungGio = false;
            int gioKeTiep = -1;

            foreach (int g in danhSachGioChay)
            {
                if (g == gioHienTai)
                    dangTrongKhungGio = true;
                if (g > gioHienTai && gioKeTiep == -1)
                    gioKeTiep = g;
            }

            int tongPhutCho;
            int mocGioHienThi;
            if (gioKeTiep != -1)
            {
                mocGioHienThi = gioKeTiep;
                tongPhutCho = (gioKeTiep - gioHienTai) * 60 - phutHienTai;
            }
            else
            {
                mocGioHienThi = danhSachGioChay[0];
                tongPhutCho = (24 - gioHienTai + mocGioHienThi) * 60 - phutHienTai;
            }

            if (tongPhutCho < 0) tongPhutCho = 0;
            int soGioCon = tongPhutCho / 60;
            int soPhutCon = tongPhutCho % 60;

            string chuoiDemNguoc = soPhutCon == 0
                ? $"{soGioCon} giờ"
                : $"{soGioCon}h{soPhutCon:D2}p";

            if (dangTrongKhungGio)
                return $"Đang chạy ({gioHienTai:D2}h) • Tiếp: {chuoiDemNguoc}";

            return $"Còn {chuoiDemNguoc} (lúc {mocGioHienThi:D2}h00)";
        }

        // Tính lịch quạt thông gió (Q0.2) theo thương phép chia %MW32 (MOD 10) mùa đông và <= 45p ôn hòa
        private string DinhDangLichQuat(int gioHienTai, int phutHienTai, bool laMuaDong)
        {
            if (laMuaDong)
            {
                // Network 12: %MW32 = now.MINUTE MOD 10; bật quạt khi %MW32 >= 8 (phút x8 và x9 mỗi 10 phút)
                int du10 = phutHienTai % 10;
                if (du10 >= 8)
                {
                    int phutCho = (10 - du10) + 8;
                    int tongPhutTiep = gioHienTai * 60 + phutHienTai + phutCho;
                    int gioTiep = (tongPhutTiep / 60) % 24;
                    int phutTiep = tongPhutTiep % 60;
                    return $"Đang chạy (Mùa đông: {phutHienTai:D2}p) • Tiếp: {phutCho}p ({gioTiep:D2}h{phutTiep:D2})";
                }
                else
                {
                    int phutCho = 8 - du10;
                    int phutTiep = phutHienTai + phutCho;
                    return $"Còn {phutCho}p (lúc {gioHienTai:D2}h{phutTiep:D2} - Mùa đông)";
                }
            }
            else
            {
                // Network 12 (Ôn hòa): bật quạt khi now.MINUTE <= 45
                int gioTiep = (gioHienTai + 1) % 24;
                int phutCho = 60 - phutHienTai;
                if (phutHienTai <= 45)
                    return $" Run (0–45p) • Tiếp: {phutCho}p ({gioTiep:D2}h00)";

                return $"Còn {phutCho}p (lúc {gioTiep:D2}h00)";
            }
        }

        // Tính lịch đèn chiếu sáng (Q0.3) theo Network 12 PLC 1
        private string DinhDangLichDen(int gioHienTai, int phutHienTai, bool laMuaDong)
        {
            if (laMuaDong)
            {
                // Mùa đông: 4 <= HOUR <= 7 (04h-08h) và 18 <= HOUR <= 19 (18h-20h)
                if (gioHienTai >= 4 && gioHienTai <= 7)
                {
                    int tongPhut = (18 - gioHienTai) * 60 - phutHienTai;
                    return $"Đang bật (04h–08h) • Tiếp: {DinhDangGioPhut(tongPhut)} (18h00)";
                }
                if (gioHienTai >= 18 && gioHienTai <= 19)
                {
                    int tongPhut = (24 - gioHienTai + 4) * 60 - phutHienTai;
                    return $"Đang bật (18h–20h) • Tiếp: {DinhDangGioPhut(tongPhut)} (04h00)";
                }
                if (gioHienTai < 4)
                {
                    int tongPhut = (4 - gioHienTai) * 60 - phutHienTai;
                    return $"Còn {DinhDangGioPhut(tongPhut)} (lúc 04h00)";
                }
                if (gioHienTai >= 8 && gioHienTai < 18)
                {
                    int tongPhut = (18 - gioHienTai) * 60 - phutHienTai;
                    return $"Còn {DinhDangGioPhut(tongPhut)} (lúc 18h00)";
                }
                int tongPhutSang = (24 - gioHienTai + 4) * 60 - phutHienTai;
                return $"Còn {DinhDangGioPhut(tongPhutSang)} (lúc 04h00)";
            }
            else
            {
                // Ôn hòa: 4 <= HOUR <= 5 (04h-06h) và HOUR >= 17 (17h-24h)
                if (gioHienTai >= 4 && gioHienTai <= 5)
                {
                    int tongPhut = (17 - gioHienTai) * 60 - phutHienTai;
                    return $"Đang bật (04h–06h) • Tiếp: {DinhDangGioPhut(tongPhut)} (17h00)";
                }
                if (gioHienTai >= 17)
                {
                    int tongPhut = (24 - gioHienTai + 4) * 60 - phutHienTai;
                    return $"Đang bật (17h–24h) • Tiếp: {DinhDangGioPhut(tongPhut)} (04h00)";
                }
                if (gioHienTai < 4)
                {
                    int tongPhut = (4 - gioHienTai) * 60 - phutHienTai;
                    return $"Còn {DinhDangGioPhut(tongPhut)} (lúc 04h00)";
                }
                int tongPhutToi = (17 - gioHienTai) * 60 - phutHienTai;
                return $"Còn {DinhDangGioPhut(tongPhutToi)} (lúc 17h00)";
            }
        }


        private void CapNhatGiaoDienT1()
        {
            if (lblDongHoPLC1 != null)
                lblDongHoPLC1.Text = $"Bây giờ là: {PlcDataShared.LayChuoiThoiGianPLC()}";

            bool onlineVaRun = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun;
            bool heThongDangChay = onlineVaRun && PlcDataShared.M_HeThong_Run;

            string trangThaiAnUong = !onlineVaRun ? "Chưa hoạt động"
                                   : PlcDataShared.Q_GatCam_XaNuoc ? "Gạt cám & xả nước (Q0.0)"
                                   : PlcDataShared.Q_KeoCam_CapNuoc ? "Cấp cám & nước (Q0.6)"
                                   : dangTieuThuTest ? "Mô phỏng gà ăn/uống"
                                   : "Chờ giờ (6h, 10h, 14h, 18h)";
            string trangThaiSuoi = (heThongDangChay && PlcDataShared.Q_HeThongSuoi) ? "BẬT (Q0.1)" : "TẮT";
            string trangThaiQuat = (heThongDangChay && PlcDataShared.Q_QuatThongGio) ? "BẬT (Q0.2)" : "TẮT";
            string trangThaiDen = (heThongDangChay && PlcDataShared.Q_DenChieuSang) ? "BẬT (Q0.3)" : "TẮT";

            if (lblThongSoTram1 != null)
            {
                lblThongSoTram1.Text =
                    $"  • Tổng trứng đã thu: {PlcDataShared.TongTrung_DaThu} quả\n" +
                    $"  • Số khay đã đóng: {PlcDataShared.TongSoKhayTrung} khay\n" +
                    $"  • Khay hiện tại: {PlcDataShared.SoTrung_TrongKhay}/12 quả\n" +
                    $"  • Máng ăn & uống: {trangThaiAnUong}\n" +
                    $"  • Hệ thống sưởi (Q0.1): {trangThaiSuoi}\n" +
                    $"  • Quạt thông gió (Q0.2): {trangThaiQuat}\n" +
                    $"  • Đèn chiếu sáng (Q0.3): {trangThaiDen}";
            }

            if (lblHeaderTienIch != null)
            {
                if (!heThongDangChay)
                {
                    lblHeaderTienIch.Text = "Tiện ích chuồng: Mất nguồn tổng (Đang dừng)";
                    lblHeaderTienIch.ForeColor = Color.Silver;
                }
                else
                {
                    lblHeaderTienIch.Text = $"Tiện ích chuồng: Đèn {(PlcDataShared.Q_DenChieuSang ? "ON" : "OFF")} | Sưởi {(PlcDataShared.Q_HeThongSuoi ? "ON" : "OFF")} | Quạt {(PlcDataShared.Q_QuatThongGio ? "ON" : "OFF")}";
                    lblHeaderTienIch.ForeColor = Color.FromArgb(186, 230, 253);
                }
            }

            if (lblLichLamViecT1 != null)
            {
                LayGioPhutHienTaiPLC(out int gioHienTai, out int phutHienTai);
                bool laMuaDong = PlcDataShared.M_KTO011_Active;

                // Quy đổi giờ sang ngày hôm sau nếu >= 24h
                int ChuanHoaGio(int gio) => gio >= 24 ? gio - 24 : gio;

                string TinhChuKyTheoMoc(int[] danhSachGioChay)
                {
                    bool dangTrongKhungGio = false;
                    int mocGioKe = -1;

                    foreach (int g in danhSachGioChay)
                    {
                        if (g == gioHienTai)
                            dangTrongKhungGio = true;
                        if (g > gioHienTai && mocGioKe == -1)
                            mocGioKe = g;
                    }

                    if (mocGioKe == -1)
                        mocGioKe = danhSachGioChay[0] + 24;

                    int tongPhutCho = (mocGioKe - gioHienTai) * 60 - phutHienTai;
                    int gioHienThi = ChuanHoaGio(mocGioKe);
                    string demNguoc = DinhDangGioPhut(tongPhutCho);

                    return dangTrongKhungGio
                        ? $"RUN ({gioHienTai:D2}h) • CK.Tiếp: {demNguoc} ({gioHienThi:D2}h)"
                        : $"Còn {demNguoc} (lúc {gioHienThi:D2}h)";
                }

                // 1. Chuyển phân -> T2 (mỗi 2h: 0h, 2h, ..., 22h -> sau 22h tự trừ 24 về 00h00)
                string dongPhan = TinhChuKyTheoMoc(new[] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22 });

                // 2. Thu trứng (10h & 17h -> sau 17h tự trừ 24 về 10h00 ngày mai)
                string dongTrung = TinhChuKyTheoMoc(new[] { 10, 17 });

                // 3. Cấp cám & nước (6h, 10h, 14h, 18h -> sau 18h tự trừ 24 về 06h00 ngày mai)
                string dongCam = TinhChuKyTheoMoc(new[] { 6, 10, 14, 18 });

                // 4. Quạt thông gió (nếu sang ngày mới >= 24h tự trừ 24 về 00h)
                string dongQuat;
                if (laMuaDong)
                {
                    int du10 = phutHienTai % 10;
                    if (du10 >= 8)
                    {
                        int phutCho = (10 - du10) + 8;
                        int tongPhutTiep = gioHienTai * 60 + phutHienTai + phutCho;
                        int gioTiep = ChuanHoaGio(tongPhutTiep / 60);
                        int phutTiep = tongPhutTiep % 60;
                        dongQuat = $"Đang chạy (Mùa đông: {phutHienTai:D2}p) • Tiếp: {phutCho}p ({gioTiep:D2}h{phutTiep:D2})";
                    }
                    else
                    {
                        int phutCho = 8 - du10;
                        int phutTiep = phutHienTai + phutCho;
                        dongQuat = $"Còn {phutCho}p (lúc {gioHienTai:D2}h{phutTiep:D2} - Mùa đông)";
                    }
                }
                else
                {
                    int gioTiep = ChuanHoaGio(gioHienTai + 1);
                    int phutCho = 60 - phutHienTai;
                    dongQuat = (phutHienTai <= 45)
                        ? $" Run (0–45p) • Tiếp: {phutCho}p ({gioTiep:D2}h)"
                        : $"Còn {phutCho}p (lúc {gioTiep:D2}h00)";
                }

                // 5. Hệ thống sưởi
                string dongSuoi = laMuaDong ? "Đang chạy (Rét đậm) • Tiếp: Mùa đông" : "Mùa đông (khi nhiệt độ thấp)";

                // 6. Đèn chiếu sáng (không còn 24h, chu kỳ qua ngày mới tự trừ 24 về 04h00)
                string dongDen;
                if (laMuaDong)
                {
                    if (gioHienTai >= 4 && gioHienTai <= 7)
                    {
                        int tongPhut = (18 - gioHienTai) * 60 - phutHienTai;
                        dongDen = $"Đang bật (04h–08h) • Tiếp: {DinhDangGioPhut(tongPhut)} (lúc 18h00)";
                    }
                    else if (gioHienTai >= 18 && gioHienTai <= 19)
                    {
                        int tongPhut = (28 - gioHienTai) * 60 - phutHienTai;
                        dongDen = $"Đang bật (18h–20h) • Tiếp: {DinhDangGioPhut(tongPhut)} (lúc {ChuanHoaGio(28):D2}h00)";
                    }
                    else if (gioHienTai < 4)
                    {
                        int tongPhut = (4 - gioHienTai) * 60 - phutHienTai;
                        dongDen = $"Còn {DinhDangGioPhut(tongPhut)} (lúc 04h00)";
                    }
                    else if (gioHienTai < 18)
                    {
                        int tongPhut = (18 - gioHienTai) * 60 - phutHienTai;
                        dongDen = $"Còn {DinhDangGioPhut(tongPhut)} (lúc 18h00)";
                    }
                    else
                    {
                        int tongPhut = (28 - gioHienTai) * 60 - phutHienTai;
                        dongDen = $"Còn {DinhDangGioPhut(tongPhut)} (lúc {ChuanHoaGio(28):D2}h00)";
                    }
                }
                else
                {
                    if (gioHienTai >= 4 && gioHienTai <= 5)
                    {
                        int tongPhut = (17 - gioHienTai) * 60 - phutHienTai;
                        dongDen = $"Đang bật (04h–06h) • Tiếp: {DinhDangGioPhut(tongPhut)} (lúc 17h00)";
                    }
                    else if (gioHienTai >= 17)
                    {
                        int tongPhut = (28 - gioHienTai) * 60 - phutHienTai;
                        dongDen = $"Đang bật (17h–00h) • Tiếp: {DinhDangGioPhut(tongPhut)} (lúc {ChuanHoaGio(28):D2}h00)";
                    }
                    else if (gioHienTai < 4)
                    {
                        int tongPhut = (4 - gioHienTai) * 60 - phutHienTai;
                        dongDen = $"Còn {DinhDangGioPhut(tongPhut)} (lúc 04h00)";
                    }
                    else
                    {
                        int tongPhut = (17 - gioHienTai) * 60 - phutHienTai;
                        dongDen = $"Còn {DinhDangGioPhut(tongPhut)} (lúc 17h00)";
                    }
                }

                lblLichLamViecT1.Text =
                    $"  • Băng tải phân: {dongPhan}\n" +
                    $"  • Thu trứng (10h & 17h): {dongTrung}\n" +
                    $"  • Cám / nước: {dongCam}\n" +
                    $"  • Quạt thông gió: {dongQuat}\n" +
                    $"  • Hệ thống sưởi: {dongSuoi}\n" +
                    $"  • Đèn điện: {dongDen}";
            }

            if (!PlcDataShared.DaKetNoiPLC)
            {
                lblCanhBaoLoiT1.Text = "Hệ thống: CHƯA KẾT NỐI C# VỚI 2 PLC — Dữ liệu đang tạm dừng cập nhật.";
                lblCanhBaoLoiT1.BackColor = Color.FromArgb(71, 85, 105);
                lblCanhBaoLoiT1.ForeColor = Color.White;
                if (lblBannerT1 != null)
                {
                    lblBannerT1.Text = "TRẠM 1: CHƯA KẾT NỐI C# VỚI 2 PLC";
                    lblBannerT1.BackColor = Color.FromArgb(71, 85, 105);
                }
                return;
            }

            if (!PlcDataShared.PLC1_DangRun)
            {
                lblCanhBaoLoiT1.Text = "Hệ thống: CPU PLC 1 TRÊN TIA PORTAL / PLCSIM ĐANG STOP — Hãy chuyển sang RUN.";
                lblCanhBaoLoiT1.BackColor = Color.FromArgb(180, 83, 9);
                lblCanhBaoLoiT1.ForeColor = Color.White;
                if (lblBannerT1 != null)
                {
                    lblBannerT1.Text = "TRẠM 1: CPU PLC 1 ĐANG STOP TRÊN TIA / PLCSIM";
                    lblBannerT1.BackColor = Color.FromArgb(180, 83, 9);
                }
                return;
            }

            if (btnCapPhanT1 != null)
            {
                bool dangCap = PlcDataShared.Q_BangTaiPhan || PlcDataShared.guidi_1_DangCapPhan == 1;
                btnCapPhanT1.Text = dangCap ? "CẤP PHÂN TAY: BẬT" : "CẤP PHÂN TAY: TẮT";
                btnCapPhanT1.BackColor = dangCap ? Color.FromArgb(21, 128, 61) : Color.FromArgb(51, 65, 85);
                btnCapPhanT1.FlatAppearance.BorderColor = dangCap ? Color.FromArgb(74, 222, 128) : Color.FromArgb(100, 116, 139);
                btnCapPhanT1.Invalidate();
            }

            if (btnCheDoAutoT1 != null)
            {
                btnCheDoAutoT1.UseVisualStyleBackColor = false;
                btnCheDoAutoT1.BackColor = !PlcDataShared.M_CheDo_Manual ? Color.FromArgb(22, 163, 74) : Color.FromArgb(148, 163, 184);
                btnCheDoAutoT1.ForeColor = !PlcDataShared.M_CheDo_Manual ? Color.White : Color.FromArgb(15, 23, 42);
            }
            if (btnCheDoManualT1 != null)
            {
                btnCheDoManualT1.UseVisualStyleBackColor = false;
                btnCheDoManualT1.BackColor = PlcDataShared.M_CheDo_Manual ? Color.FromArgb(234, 88, 12) : Color.FromArgb(148, 163, 184);
                btnCheDoManualT1.ForeColor = PlcDataShared.M_CheDo_Manual ? Color.White : Color.FromArgb(15, 23, 42);
            }

            if (btnGiaLapLoiU != null)
            {
                btnGiaLapLoiU.UseVisualStyleBackColor = false;
                btnGiaLapLoiU.Text = PlcDataShared.M_Sim_Loi_T1_Active
                    ? "ĐANG KÍCH %M0.7 = 1"
                    : "Kích lỗi kẹt T1 (%M0.7)";
                btnGiaLapLoiU.BackColor = PlcDataShared.M_Sim_Loi_T1_Active ? Color.DarkOrange : Color.FromArgb(185, 28, 28);
                btnGiaLapLoiU.ForeColor = Color.White;
            }

            if (btnMoPhongKTO011 != null)
            {
                btnMoPhongKTO011.UseVisualStyleBackColor = false;
                btnMoPhongKTO011.Text = PlcDataShared.M_KTO011_Active
                    ? "rét đậm (Bật Sưởi)"
                    : "Nhiệt độ: Ôn hoà";
                btnMoPhongKTO011.BackColor = PlcDataShared.M_KTO011_Active ? Color.FromArgb(220, 38, 38) : Color.FromArgb(51, 65, 85);
                btnMoPhongKTO011.ForeColor = Color.White;
            }

            if (btnStartTram2_T1 != null && btnStopTram2_T1 != null)
            {
                bool t2DangBat = (PlcDataShared.guidi_0_BatPlc2TuXa == 1);
                btnStartTram2_T1.BackColor = t2DangBat ? Color.FromArgb(22, 163, 74) : Color.FromArgb(51, 65, 85);
                btnStopTram2_T1.BackColor = !t2DangBat ? Color.FromArgb(220, 38, 38) : Color.FromArgb(51, 65, 85);
            }

            if (lblTrangThaiPlc2T1 != null)
            {
                lblTrangThaiPlc2T1.Text = PlcDataShared.guidi_0_BatPlc2TuXa == 1
                    ? "Trạng thái PLC 2: ĐANG BẬT (guidi[0] = 1)"
                    : "Trạng thái PLC 2: ĐANG TẮT (guidi[0] = 0)";
                lblTrangThaiPlc2T1.ForeColor = PlcDataShared.guidi_0_BatPlc2TuXa == 1 ? Color.FromArgb(52, 211, 153) : Color.FromArgb(248, 113, 113);
            }

            if (btnTestChuKyT1 != null)
            {
                btnTestChuKyT1.UseVisualStyleBackColor = false;
                btnTestChuKyT1.Size = new Size(215, btnTestChuKyT1.Height);
                btnTestChuKyT1.Text = PlcDataShared.M_TestChuKy_Active
                    ? "CHẠY THỬ: ĐANG BẬT"
                    : "CHẠY THỬ TẤT CẢ";
                btnTestChuKyT1.BackColor = PlcDataShared.M_TestChuKy_Active ? Color.FromArgb(0, 150, 80) : Color.FromArgb(71, 85, 105);
                btnTestChuKyT1.ForeColor = Color.White;
            }

            lblThongTinGui.Text =
                "Gửi sang PLC 2:\n" +
                $"  • guidi[0] (Bật T2): {PlcDataShared.guidi_0_BatPlc2TuXa}\n" +
                $"  • guidi[1] (Cấp phân): {PlcDataShared.guidi_1_DangCapPhan}\n" +
                $"  • guidi[2] (Khối lượng): {PlcDataShared.guidi_2_KhoiLuongGui} kg";

            lblPhanHoiPlc2.Text = PlcDataShared.guiden_plc1_0_BaoDay == 1
                ? "Nhận từ PLC 2:\n  • nhantu_plc2[0] = 1\n  • Trạng thái: HẦM ĐẦY (NGỪNG NHẬN)"
                : "Nhận từ PLC 2:\n  • nhantu_plc2[0] = 0\n  • Trạng thái: Sẵn sàng nhận phân";

            if (lblHeaderTrung != null)
            {
                if (PlcDataShared.M_Loi_KetQuyTrinhU_T1)
                {
                    lblHeaderTrung.Text = "Băng tải đóng khay trứng: KHÓA DO LỖI KẸT (%M10.2)";
                    lblHeaderTrung.ForeColor = Color.OrangeRed;
                }
                else if (PlcDataShared.SoTrung_TrongKhay >= 12)
                {
                    lblHeaderTrung.Text = $"Băng tải đóng khay trứng: DỪNG 2s CHUYỂN KHAY ĐẦY (12/12) → KHAY MỚI (Tổng: {PlcDataShared.TongSoKhayTrung} khay)";
                    lblHeaderTrung.ForeColor = Color.FromArgb(253, 224, 71);
                }
                else if (PlcDataShared.Q_BangTaiTrung)
                {
                    lblHeaderTrung.Text = $"Băng tải đóng khay trứng: ĐANG CHẠY ({PlcDataShared.SoTrung_TrongKhay}/12 quả trong khay)";
                    lblHeaderTrung.ForeColor = Color.LightGreen;
                }
                else
                {
                    lblHeaderTrung.Text = $"Băng tải đóng khay trứng: Đang chờ khung giờ (10h00 & 17h00) — Khay hiện tại: {PlcDataShared.SoTrung_TrongKhay}/12 quả";
                    lblHeaderTrung.ForeColor = Color.FromArgb(125, 211, 252);
                }
            }

            if (lblHeaderPhan != null)
            {
                bool dangKeoPhan = PlcDataShared.Q_BangTaiPhan || PlcDataShared.guidi_1_DangCapPhan == 1;
                if (PlcDataShared.M_Loi_KetQuyTrinhU_T1)
                {
                    lblHeaderPhan.Text = "Băng tải phân thô: KẸT BĂNG TẢI (%M10.2)";
                    lblHeaderPhan.ForeColor = Color.OrangeRed;
                }
                else if (dangKeoPhan)
                {
                    lblHeaderPhan.Text = "Băng tải phân thô: ĐANG KÉO SANG TRẠM 2 → (50kg)";
                    lblHeaderPhan.ForeColor = Color.LightGreen;
                }
                else
                {
                    lblHeaderPhan.Text = "Băng tải phân thô (2 dây chuyền): Đang chờ chu kỳ kéo";
                    lblHeaderPhan.ForeColor = Color.WhiteSmoke;
                }
            }

            if (PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                string nguyenNhan = PlcDataShared.M_Sim_Loi_T1_Active
                    ? "Kích giả lập %M0.7"
                    : "Tràn phân khi hầm Trạm 2 đầy (nhantu_plc2[0]=1 & guidi[1]=1)";
                lblCanhBaoLoiT1.Text = $"Hệ thống: LỖI KẸT QUY TRÌNH (%M10.2=1) [{nguyenNhan}] — Đã khóa băng tải! Bấm 'Tắt lỗi / Reset'.";
                lblCanhBaoLoiT1.BackColor = Color.Red;
                lblCanhBaoLoiT1.ForeColor = Color.White;
            }
            else if (PlcDataShared.M_Sim_Loi_T1_Active || (PlcDataShared.guiden_plc1_0_BaoDay == 1 && PlcDataShared.guidi_1_DangCapPhan == 1))
            {
                lblCanhBaoLoiT1.Text = "Hệ thống: CẢNH BÁO SỚM (Network 11) — Chờ Timer PLC đếm 3 giây để kích hoạt %M10.2...";
                lblCanhBaoLoiT1.BackColor = Color.DarkOrange;
                lblCanhBaoLoiT1.ForeColor = Color.White;
            }
            else
            {
                string cheDo = PlcDataShared.M_CheDo_Manual ? "CHẾ ĐỘ TAY (MANUAL)" : "CHẾ ĐỘ TỰ ĐỘNG (AUTO)";
                string trangThaiChuKy = (PlcDataShared.Q_BangTaiTrung || PlcDataShared.SoTrung_TrongKhay >= 12)
                    ? "ĐANG THU TRỨNG"
                    : "CHỜ KHUNG GIỜ (10h00 & 17h00)";
                lblCanhBaoLoiT1.Text = PlcDataShared.M_HeThong_Run
                    ? $"Hệ thống: ĐANG CHẠY [{cheDo}] — {trangThaiChuKy}"
                    : $"Hệ thống: ĐANG DỪNG [{cheDo}]";
                lblCanhBaoLoiT1.BackColor = Color.FromArgb(24, 38, 32);
                lblCanhBaoLoiT1.ForeColor = Color.FromArgb(52, 211, 153);
                if (lblBannerT1 != null)
                {
                    lblBannerT1.Text = PlcDataShared.M_HeThong_Run
                        ? $"TRẠM 1: ĐANG CHẠY [{cheDo}] — {trangThaiChuKy}"
                        : $"TRẠM 1: ĐANG DỪNG [{cheDo}]";
                    lblBannerT1.BackColor = PlcDataShared.M_HeThong_Run ? Color.FromArgb(21, 128, 61) : Color.FromArgb(51, 65, 85);
                }
            }
        }


        private void TimerAnimation_Tick(object sender, EventArgs e)
        {
            if (PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && !PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                // Timer interval = 30ms -> 1 giây = 33.33 tick (0.03f/tick)
                float buocPha = 0.03f;

                if (PlcDataShared.SoTrung_TrongKhay >= 12)
                {
                    // Lúc này PLC đã ngắt %Q0.5 = 0 trong 2 giây (TON T#2s) để xuất khay đầy sang phải & kéo khay rỗng từ trái vào tâm Vòi 1
                    phaChuyenDongTrung = Math.Min(2.0f, phaChuyenDongTrung + buocPha);
                    pnlBangTaiTrung.Invalidate();
                }
                else if (PlcDataShared.Q_BangTaiTrung)
                {
                    // Băng tải chính đang chạy (%Q0.5 = 1)
                    if (PlcDataShared.SoTrung_TrongKhay > 0)
                    {
                        phaChuyenDongTrung = Math.Min(1.0f, phaChuyenDongTrung + buocPha);
                    }
                    pnlBangTaiTrung.Invalidate();
                }

                if (demTickHieuUngVoi > 0)
                {
                    demTickHieuUngVoi--;
                    pnlBangTaiTrung.Invalidate();
                }
                else
                {
                    hieuUngVoiDangTha = 0;
                }
                if (PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC2_DangRun)
                {
                    int curLoadcell = PlcDataShared.KhoiLuong_Loadcell;
                    if (khoiLuongLoadcellCuT2 == -1)
                    {
                        khoiLuongLoadcellCuT2 = curLoadcell;
                    }
                    else if (curLoadcell != khoiLuongLoadcellCuT2)
                    {
                        if (curLoadcell > 0)
                        {
                            demTickDongPhanT2 = 15; // ~450ms mỗi lần nhảy số kg (1/25 -> 2/25 -> ... -> 25/25)
                            phaDongPhanT2 = 0;
                        }
                        else
                        {
                            demTickDongPhanT2 = 0;  // Về 0/25 thì ngắt ngay
                        }
                        khoiLuongLoadcellCuT2 = curLoadcell;
                        pnlMiniTram2?.Invalidate();
                    }
                }
                else
                {
                    demTickDongPhanT2 = 0;
                }

                if (demTickDongPhanT2 > 0)
                {
                    demTickDongPhanT2--;
                    phaDongPhanT2 = (phaDongPhanT2 + 3) % 14;
                    pnlMiniTram2?.Invalidate();
                }
            }

            if (PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.M_HeThong_Run
                && (PlcDataShared.Q_BangTaiPhan || PlcDataShared.guidi_1_DangCapPhan == 1)
                && !PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                khoiPhanX1 = (khoiPhanX1 + 5) % 720;
                viTriConLanPhan1 = (viTriConLanPhan1 + 5) % 18;

                khoiPhanX2 = (khoiPhanX2 + 5) % 720;
                viTriConLanPhan2 = (viTriConLanPhan2 + 5) % 18;

                pnlBangTaiPhan.Invalidate();
            }

            // Chuyển động cụm trượt thanh ray trên máng ăn & máng nước
            // Tốc độ 0.0072f/tick (~4.15s cho toàn hành trình) đảm bảo thanh gạt chạm tận cùng thành máng trước khi timer 5s PLC ngắt
            const float buocTruot5Giay = 0.0072f;
            const float buocChuanBiNhanh = 0.028f;
            const float buocTieuThu5_5Giay = 0.0055f;

            bool dangDon = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.Q_GatCam_XaNuoc;   // Q0.0 (5s)
            bool dangCap = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.Q_KeoCam_CapNuoc;  // Q0.6 (5s)
            bool dangTestFull = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.M_TestChuKy_Active; // %M1.2

            if (dangDon && !prevGatCam)
            {
                dangTieuThuTest = false;
                tienTrinhTieuThu = 0f;
                dangChuanBiCapAn = false;
                dangChuanBiCapNuoc = false;

                // Nếu thanh gạt đang nằm xa mép trái (> 0.02f), cho chạy mượt về 0f rồi mới gạt chứ không nhảy cóc
                dangChuanBiDonAn = viTriTruotAn > 0.02f;
                dangChuanBiDonNuoc = viTriTruotNuoc > 0.02f;

                if (mucCamSach > 0.02f)
                {
                    mucCamCu = Math.Max(mucCamCu, 1f);
                    mucCamSach = 0f;
                }
                if (mucNuocSach > 0.02f)
                {
                    mucNuocDong = Math.Max(mucNuocDong, 1f);
                    mucNuocSach = 0f;
                }
            }

            if (dangCap && !prevKeoCam)
            {
                dangTieuThuTest = false;
                tienTrinhTieuThu = 0f;
                dangChuanBiDonAn = false;
                dangChuanBiDonNuoc = false;

                // Nếu thanh gạt chưa ở mép phải (< 0.98f), lùi mượt về mép phải trước khi rải cám/nước
                dangChuanBiCapAn = viTriTruotAn < 0.98f;
                dangChuanBiCapNuoc = viTriTruotNuoc < 0.98f;

                mucCamCu = 0f;
                mucNuocDong = 0f;
                mucRacAn = 0f;
                mucRacNuoc = 0f;
                if (!dangChuanBiCapAn)
                {
                    viTriBatDauCam = viTriTruotAn;
                    mucCamSach = 0.02f;
                }
                if (!dangChuanBiCapNuoc)
                {
                    viTriBatDauNuoc = viTriTruotNuoc;
                    mucNuocSach = 0.04f;
                }
            }

            if (dangDon)
            {
                dangTieuThuTest = false;

                if (dangChuanBiDonAn)
                {
                    viTriTruotAn = Math.Max(0f, viTriTruotAn - buocChuanBiNhanh);
                    if (viTriTruotAn <= 0f) dangChuanBiDonAn = false;
                }
                else
                {
                    viTriTruotAn = Math.Min(1f, viTriTruotAn + buocTruot5Giay);
                    if (mucCamCu > 0.01f)
                        mucRacAn = Math.Min(1f, Math.Max(mucRacAn, viTriTruotAn));
                    mucCamCu = Math.Max(0f, 1f - viTriTruotAn);
                }

                if (dangChuanBiDonNuoc)
                {
                    viTriTruotNuoc = Math.Max(0f, viTriTruotNuoc - buocChuanBiNhanh);
                    if (viTriTruotNuoc <= 0f) dangChuanBiDonNuoc = false;
                }
                else
                {
                    viTriTruotNuoc = Math.Min(1f, viTriTruotNuoc + buocTruot5Giay);
                    if (mucNuocDong > 0.01f)
                        mucRacNuoc = Math.Min(1f, Math.Max(mucRacNuoc, viTriTruotNuoc));
                    mucNuocDong = Math.Max(0f, 1f - viTriTruotNuoc);
                }

                phaHatRoi = (phaHatRoi + 2) % 24;
                pnlMangAn?.Invalidate();
                pnlMangNuoc?.Invalidate();
            }
            else if (dangCap)
            {
                dangTieuThuTest = false;

                if (dangChuanBiCapAn)
                {
                    viTriTruotAn = Math.Min(1f, viTriTruotAn + buocChuanBiNhanh);
                    if (viTriTruotAn >= 1f)
                    {
                        dangChuanBiCapAn = false;
                        viTriBatDauCam = 1f;
                        mucCamSach = 0.02f;
                    }
                }
                else
                {
                    viTriTruotAn = Math.Max(0f, viTriTruotAn - buocTruot5Giay);
                    viTriBatDauCam = viTriTruotAn;
                    mucCamSach = Math.Min(1f, 1f - viTriTruotAn);
                }

                if (dangChuanBiCapNuoc)
                {
                    viTriTruotNuoc = Math.Min(1f, viTriTruotNuoc + buocChuanBiNhanh);
                    if (viTriTruotNuoc >= 1f)
                    {
                        dangChuanBiCapNuoc = false;
                        viTriBatDauNuoc = 1f;
                        mucNuocSach = 0.04f;
                    }
                }
                else
                {
                    viTriTruotNuoc = Math.Max(0f, viTriTruotNuoc - buocTruot5Giay);
                    viTriBatDauNuoc = viTriTruotNuoc;
                    mucNuocSach = Math.Min(1f, 0.04f + (1f - viTriTruotNuoc) * 0.96f);
                }

                phaHatRoi = (phaHatRoi + 2) % 24;
                pnlMangAn?.Invalidate();
                pnlMangNuoc?.Invalidate();
            }
            else
            {
                if (dangTestFull)
                {
                    if (prevKeoCam)
                    {
                        dangTieuThuTest = true;
                        tienTrinhTieuThu = 0f;
                        mucCamSach = 1f;
                        mucNuocSach = 1f;
                    }

                    if (dangTieuThuTest)
                    {
                        // Nếu thanh gạt còn dư vài % cuối hành trình lúc Q0.6 vừa ngắt, cho trượt nốt về tận cùng 0f
                        if (viTriTruotAn > 0f)
                        {
                            viTriTruotAn = Math.Max(0f, viTriTruotAn - buocTruot5Giay);
                            viTriBatDauCam = viTriTruotAn;
                        }
                        if (viTriTruotNuoc > 0f)
                        {
                            viTriTruotNuoc = Math.Max(0f, viTriTruotNuoc - buocTruot5Giay);
                            viTriBatDauNuoc = viTriTruotNuoc;
                        }

                        tienTrinhTieuThu = Math.Min(1f, tienTrinhTieuThu + buocTieuThu5_5Giay);
                        phaHatRoi = (phaHatRoi + 1) % 24;

                        if (tienTrinhTieuThu >= 1f)
                        {
                            dangTieuThuTest = false;
                            mucCamSach = 0f;
                            mucCamCu = 1f;
                            mucNuocSach = 0f;
                            mucNuocDong = 1f;
                        }

                        pnlMangAn?.Invalidate();
                        pnlMangNuoc?.Invalidate();
                    }
                }
                else if (dangTieuThuTest)
                {
                    dangTieuThuTest = false;
                }
            }

            // Animation quạt thông gió (Q0.2) và sóng nhiệt sưởi (Q0.1)
            bool onlineVaRunAnim = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.M_HeThong_Run;
            if (onlineVaRunAnim && PlcDataShared.Q_QuatThongGio)
            {
                gocQuayQuat = (gocQuayQuat + 18f) % 360f;
            }
            if (onlineVaRunAnim && PlcDataShared.Q_HeThongSuoi)
            {
                phaSongNhiet = (phaSongNhiet + 1.4f) % 24f;
            }
            if (onlineVaRunAnim && (PlcDataShared.Q_QuatThongGio || PlcDataShared.Q_HeThongSuoi))
            {
                pnlTienIchChuong?.Invalidate();
            }

            prevGatCam = dangDon;
            prevKeoCam = dangCap;
        }

        private static Color LerpColor(Color c1, Color c2, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            int a = (int)(c1.A + (c2.A - c1.A) * t);
            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int g = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);
            return Color.FromArgb(a, r, g, b);
        }

        private void VeConBo(Graphics g, int x, int y, Color mauThan)
        {
            using (SolidBrush brBo = new SolidBrush(mauThan))
            using (Pen penChan = new Pen(mauThan, 1f))
            {
                g.FillEllipse(brBo, x, y, 6, 4);
                g.DrawLine(penChan, x, y + 1, x - 3, y - 1);
                g.DrawLine(penChan, x, y + 3, x - 3, y + 5);
                g.DrawLine(penChan, x + 2, y, x + 1, y - 2);
                g.DrawLine(penChan, x + 4, y + 4, x + 5, y + 6);
            }
        }

        private void VeCumTruotThanhRay(
            Graphics g,
            int troughX,
            int troughW,
            int railY,
            int troughBottomY,
            float viTriNorm,
            bool laCheDoDon,
            bool laCheDoCap,
            bool laMangAn,
            int mucMatNuocY = 0)
        {
            int innerLeftX = troughX + 2;
            int innerRightX = troughX + troughW - 2;
            int innerTravelW = Math.Max(10, innerRightX - innerLeftX);

            // Lưỡi gạt đi tận cùng từ sát thành trong bên trái (0f) đến sát thành trong bên phải (1f)
            int bladeX = innerLeftX + (int)Math.Round(viTriNorm * innerTravelW);

            int blockW = 20;
            int blockH = 10;
            int blockX = bladeX - blockW / 2;
            int blockY = railY - blockH / 2;

            // Thanh ray kéo dài đủ đỡ con trượt khi lưỡi gạt áp sát 2 thành máng
            int railLeftX = innerLeftX - blockW / 2 - 2;
            int railRightX = innerRightX + blockW / 2 + 2;
            using (Pen penRail = new Pen(Color.FromArgb(148, 163, 184), 2.5f))
            {
                g.DrawLine(penRail, railLeftX, railY, railRightX, railY);
            }
            g.FillRectangle(Brushes.Gray, railLeftX - 2, railY - 3, 4, 7);
            g.FillRectangle(Brushes.Gray, railRightX - 2, railY - 3, 4, 7);

            Color mauBlock = laCheDoDon ? Color.FromArgb(234, 88, 12)
                           : laCheDoCap ? Color.FromArgb(16, 185, 129)
                           : Color.FromArgb(71, 85, 105);

            using (SolidBrush brBlock = new SolidBrush(mauBlock))
            using (Pen penBlock = new Pen(Color.WhiteSmoke, 1f))
            {
                g.FillRectangle(brBlock, blockX, blockY, blockW, blockH);
                g.DrawRectangle(penBlock, blockX, blockY, blockW, blockH);
            }

            g.FillEllipse(Brushes.Silver, blockX + 3, railY - 2, 4, 4);
            g.FillEllipse(Brushes.Silver, blockX + blockW - 7, railY - 2, 4, 4);

            using (Pen penArm = new Pen(Color.LightGray, 2f))
            {
                g.DrawLine(penArm, bladeX, blockY + blockH, bladeX, troughBottomY - 2);
            }

            if (laCheDoDon)
            {
                using (SolidBrush brBlade = new SolidBrush(Color.Orange))
                {
                    Point[] bladePts =
                    {
                        new Point(bladeX - 1, troughBottomY - 12),
                        new Point(bladeX + 4, troughBottomY - 2),
                        new Point(bladeX - 1, troughBottomY - 2)
                    };
                    g.FillPolygon(brBlade, bladePts);
                }
            }
            else if (laCheDoCap && ((laMangAn && !dangChuanBiCapAn && viTriTruotAn > 0.01f) || (!laMangAn && !dangChuanBiCapNuoc && viTriTruotNuoc > 0.01f)))
            {
                if (laMangAn)
                {
                    int sprayX = Math.Max(innerLeftX, bladeX - 7);
                    using (SolidBrush brHat = new SolidBrush(Color.FromArgb(250, 204, 21)))
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            int dy = ((phaHatRoi + k * 6) % 14);
                            int dx = (k % 2) * 3 - (dy / 5);
                            g.FillEllipse(brHat, sprayX + dx, blockY + blockH + dy, 3, 3);
                        }
                    }
                }
                else
                {
                    int streamBottomY = (mucMatNuocY > blockY + blockH + 2 && mucMatNuocY < troughBottomY)
                        ? mucMatNuocY
                        : troughBottomY - 3;
                    using (Pen penStream = new Pen(Color.FromArgb(56, 189, 248), 2.5f))
                    using (Pen penSplash = new Pen(Color.FromArgb(186, 230, 253), 1.2f))
                    {
                        g.DrawLine(penStream, bladeX, blockY + blockH, bladeX, streamBottomY);
                        g.DrawArc(penSplash, bladeX - 5, streamBottomY - 2, 10, 4, 0, 180);
                    }
                }
            }
        }

        private void PnlMangAn_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = Math.Max(180, pnlMangAn.Width);
            int ph = Math.Max(55, pnlMangAn.Height);

            bool dangDon = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.Q_GatCam_XaNuoc;
            bool dangCap = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.Q_KeoCam_CapNuoc;

            using (Font fTitle = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (Font fStatus = new Font("Segoe UI", 7.6f, FontStyle.Bold))
            {
                g.DrawString("MÁNG THỨC ĂN", fTitle, Brushes.WhiteSmoke, 6, 3);

                string stText = dangDon ? "GẠT DỌN →"
                              : dangCap ? "← CẤP CÁM"
                              : dangTieuThuTest ? "ĐANG CẠN..."
                              : (mucCamSach > 0.1f ? "Đầy cám" : (mucCamCu > 0.1f ? "Cần dọn cám" : "Đã dọn sạch"));
                Brush brSt = dangDon ? Brushes.Orange
                           : dangCap ? Brushes.LightGreen
                           : dangTieuThuTest ? Brushes.Khaki
                           : (mucCamSach > 0.1f ? Brushes.Gold : (mucCamCu > 0.1f ? Brushes.SandyBrown : Brushes.Silver));
                SizeF sz = g.MeasureString(stText, fStatus);
                g.DrawString(stText, fStatus, brSt, pw - sz.Width - 6, 3);
            }

            int troughX = 12;
            int chuteW = 16;
            int troughW = pw - troughX - chuteW - 10;
            int railY = 23;
            int troughY = 34;
            int troughH = Math.Max(18, ph - troughY - 6);
            int troughBottomY = troughY + troughH;

            int innerLeftX = troughX + 2;
            int innerRightX = troughX + troughW - 2;
            int innerTravelW = Math.Max(10, innerRightX - innerLeftX);
            int bladeScreenX = innerLeftX + (int)Math.Round(viTriTruotAn * innerTravelW);

            // Hộp hứng thức ăn thừa thu nhỏ & hạ thấp xuống ngang đáy máng
            int chuteX = troughX + troughW + 2;
            int chuteH = Math.Max(10, troughH / 2);
            int chuteY = troughBottomY - chuteH + 2;

            using (SolidBrush brTrough = new SolidBrush(Color.FromArgb(30, 34, 42)))
            using (Pen penTrough = new Pen(Color.FromArgb(100, 116, 139), 1.4f))
            {
                g.FillRectangle(brTrough, troughX, troughY, troughW, troughH);
                g.DrawRectangle(penTrough, troughX, troughY, troughW, troughH);
            }

            // Vẽ hộp hứng thấp đằng sau máng + phần thức ăn thừa tích tụ bên trong
            using (SolidBrush brChute = new SolidBrush(Color.FromArgb(45, 40, 36)))
            {
                g.FillRectangle(brChute, chuteX, chuteY, chuteW, chuteH);
                if (mucRacAn > 0.02f)
                {
                    int hWastePile = Math.Max(2, (int)((chuteH - 3) * mucRacAn));
                    using (SolidBrush brWastePile = new SolidBrush(Color.FromArgb(120, 83, 45)))
                    {
                        g.FillRectangle(brWastePile, chuteX + 2, chuteY + chuteH - hWastePile - 1, chuteW - 4, hWastePile);
                    }
                }
                g.DrawRectangle(Pens.DimGray, chuteX, chuteY, chuteW, chuteH);
            }

            // Khe thoát thức ăn thừa ở mép dưới thành phải máng dẫn sang hộp hứng
            using (Pen penSlot = new Pen(Color.FromArgb(30, 34, 42), 2f))
            {
                g.DrawLine(penSlot, troughX + troughW, chuteY + 2, troughX + troughW, troughBottomY - 1);
            }

            int feedStartX = innerLeftX + (int)Math.Round(viTriBatDauCam * innerTravelW);

            if (mucCamCu > 0.02f)
            {
                int startX = (dangDon && !dangChuanBiDonAn)
                    ? Math.Max(feedStartX, bladeScreenX)
                    : feedStartX;
                int wDirty = innerRightX - startX;
                if (wDirty > 1)
                {
                    int hDirty = Math.Max(4, (int)((troughH - 4) * 0.28f));
                    using (SolidBrush brDirty = new SolidBrush(Color.FromArgb(120, 83, 45)))
                    {
                        g.FillRectangle(brDirty, startX, troughBottomY - hDirty - 1, wDirty, hDirty);
                    }
                    int stepBug = Math.Max(28, wDirty / 3);
                    for (int bx = startX + 6; bx < startX + wDirty - 8; bx += stepBug)
                    {
                        VeConBo(g, bx, troughBottomY - hDirty - 3, Color.FromArgb(62, 28, 12));
                    }
                }
                if (dangDon && !dangChuanBiDonAn)
                {
                    using (SolidBrush brWaste = new SolidBrush(Color.FromArgb(120, 83, 45)))
                    {
                        int dropRange = Math.Max(4, chuteH - 5);
                        g.FillEllipse(brWaste, chuteX + 4, chuteY + 1 + (phaHatRoi % dropRange), 3, 3);
                        g.FillEllipse(brWaste, chuteX + 9, chuteY + 2 + ((phaHatRoi + 3) % dropRange), 3, 3);
                    }
                }
            }

            if (dangTieuThuTest)
            {
                float t = tienTrinhTieuThu;
                int startX = feedStartX;
                int wTotal = Math.Max(0, innerRightX - startX);
                if (wTotal > 1)
                {
                    float ratioH = 0.72f * (1f - t) + 0.28f * t;
                    int hCur = Math.Max(4, (int)((troughH - 4) * ratioH));

                    Color colFeed = LerpColor(Color.FromArgb(217, 155, 22), Color.FromArgb(120, 83, 45), t);
                    using (SolidBrush brCur = new SolidBrush(colFeed))
                    {
                        g.FillRectangle(brCur, startX, troughBottomY - hCur - 1, wTotal, hCur);
                    }

                    if (t < 0.65f)
                    {
                        int alphaGrain = (int)(255 * (1f - t / 0.65f));
                        using (SolidBrush brGrain = new SolidBrush(Color.FromArgb(alphaGrain, 253, 224, 71)))
                        {
                            for (int gx = startX + 4; gx < startX + wTotal - 4; gx += 10)
                            {
                                g.FillEllipse(brGrain, gx, troughBottomY - hCur + 1, 3, 2);
                            }
                        }
                    }

                    if (t >= 0.60f)
                    {
                        int alphaBug = (int)(255 * Math.Min(1f, (t - 0.60f) / 0.35f));
                        Color colBug = Color.FromArgb(alphaBug, 62, 28, 12);
                        int stepBug = Math.Max(28, wTotal / 3);
                        for (int bx = startX + 6; bx < startX + wTotal - 8; bx += stepBug)
                        {
                            VeConBo(g, bx, troughBottomY - hCur - 3, colBug);
                        }
                    }
                }
            }
            else if (mucCamSach > 0.02f)
            {
                int hFresh = Math.Max(6, (int)((troughH - 4) * 0.72f));
                using (SolidBrush brFresh = new SolidBrush(Color.FromArgb(217, 155, 22)))
                using (SolidBrush brGrain = new SolidBrush(Color.FromArgb(253, 224, 71)))
                {
                    if (dangCap && !dangChuanBiCapAn && viTriTruotAn > 0.02f)
                    {
                        int pileStartX = Math.Max(innerLeftX, bladeScreenX - 10);
                        int pileW = bladeScreenX - pileStartX;
                        if (pileW > 2)
                        {
                            int pileH = Math.Min(troughH - 3, hFresh + 3);
                            Point[] pilePts =
                            {
                                new Point(pileStartX, troughBottomY - 1),
                                new Point(bladeScreenX, troughBottomY - pileH - 1),
                                new Point(bladeScreenX, troughBottomY - 1)
                            };
                            g.FillPolygon(brFresh, pilePts);
                        }
                    }

                    int startX = feedStartX;
                    int wFresh = innerRightX - startX;
                    if (wFresh > 1)
                    {
                        g.FillRectangle(brFresh, startX, troughBottomY - hFresh - 1, wFresh, hFresh);
                        for (int gx = startX + 4; gx < startX + wFresh - 4; gx += 9)
                        {
                            g.FillEllipse(brGrain, gx, troughBottomY - hFresh + 1, 3, 2);
                            g.FillEllipse(brGrain, gx + 4, troughBottomY - hFresh / 2, 3, 2);
                        }
                    }
                }
            }

            VeCumTruotThanhRay(g, troughX, troughW, railY, troughBottomY, viTriTruotAn, dangDon && !dangChuanBiDonAn, dangCap && !dangChuanBiCapAn, laMangAn: true);
        }

        private void PnlMangNuoc_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = Math.Max(180, pnlMangNuoc.Width);
            int ph = Math.Max(55, pnlMangNuoc.Height);

            bool dangDon = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.Q_GatCam_XaNuoc;
            bool dangCap = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.Q_KeoCam_CapNuoc;

            using (Font fTitle = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (Font fStatus = new Font("Segoe UI", 7.6f, FontStyle.Bold))
            {
                g.DrawString("MÁNG NƯỚC", fTitle, Brushes.WhiteSmoke, 6, 3);

                string stText = dangDon ? "XẢ NƯỚC →"
                              : dangCap ? "← CẤP NƯỚC"
                              : dangTieuThuTest ? "ĐANG CẠN..."
                              : (mucNuocSach > 0.1f ? "Nước sạch" : (mucNuocDong > 0.1f ? "Cần xả nước" : "Đã xả cạn"));
                Brush brSt = dangDon ? Brushes.Orange
                           : dangCap ? Brushes.DeepSkyBlue
                           : dangTieuThuTest ? Brushes.LightSteelBlue
                           : (mucNuocSach > 0.1f ? Brushes.LightSkyBlue : (mucNuocDong > 0.1f ? Brushes.DarkKhaki : Brushes.Silver));
                SizeF sz = g.MeasureString(stText, fStatus);
                g.DrawString(stText, fStatus, brSt, pw - sz.Width - 6, 3);
            }

            int troughX = 12;
            int chuteW = 16;
            int troughW = pw - troughX - chuteW - 10;
            int railY = 23;
            int troughY = 34;
            int troughH = Math.Max(18, ph - troughY - 6);
            int troughBottomY = troughY + troughH;

            int innerLeftX = troughX + 2;
            int innerRightX = troughX + troughW - 2;
            int innerTravelW = Math.Max(10, innerRightX - innerLeftX);
            int bladeScreenX = innerLeftX + (int)Math.Round(viTriTruotNuoc * innerTravelW);

            // Hộp/hố thu nước xả thu nhỏ & hạ thấp xuống ngang đáy máng
            int chuteX = troughX + troughW + 2;
            int chuteH = Math.Max(10, troughH / 2);
            int chuteY = troughBottomY - chuteH + 2;

            using (SolidBrush brTrough = new SolidBrush(Color.FromArgb(24, 32, 44)))
            using (Pen penTrough = new Pen(Color.FromArgb(100, 116, 139), 1.4f))
            {
                g.FillRectangle(brTrough, troughX, troughY, troughW, troughH);
                g.DrawRectangle(penTrough, troughX, troughY, troughW, troughH);
            }

            using (SolidBrush brPipe = new SolidBrush(Color.FromArgb(32, 42, 54)))
            {
                g.FillRectangle(brPipe, chuteX, chuteY, chuteW, chuteH);
                if (mucRacNuoc > 0.02f)
                {
                    int hDrainWater = Math.Max(2, (int)((chuteH - 3) * mucRacNuoc));
                    using (SolidBrush brDrain = new SolidBrush(Color.FromArgb(113, 128, 74)))
                    {
                        g.FillRectangle(brDrain, chuteX + 2, chuteY + chuteH - hDrainWater - 1, chuteW - 4, hDrainWater);
                    }
                }
                g.DrawRectangle(Pens.DimGray, chuteX, chuteY, chuteW, chuteH);
            }

            using (Pen penSlot = new Pen(Color.FromArgb(24, 32, 44), 2f))
            {
                g.DrawLine(penSlot, troughX + troughW, chuteY + 2, troughX + troughW, troughBottomY - 1);
            }

            int mucMatNuocY = troughBottomY - 2;

            if (mucNuocDong > 0.02f)
            {
                int startX = (dangDon && !dangChuanBiDonNuoc)
                    ? Math.Max(innerLeftX, bladeScreenX)
                    : innerLeftX;
                int wDirty = innerRightX - startX;
                if (wDirty > 1)
                {
                    int hDirty = Math.Max(4, (int)((troughH - 4) * 0.26f));
                    using (SolidBrush brStagnant = new SolidBrush(Color.FromArgb(113, 128, 74)))
                    {
                        g.FillRectangle(brStagnant, startX, troughBottomY - hDirty - 1, wDirty, hDirty);
                    }
                    int stepBug = Math.Max(30, wDirty / 3);
                    for (int bx = startX + 5; bx < startX + wDirty - 8; bx += stepBug)
                    {
                        VeConBo(g, bx, troughBottomY - hDirty - 2, Color.FromArgb(54, 46, 20));
                    }
                }
                if (dangDon && !dangChuanBiDonNuoc)
                {
                    using (Pen penStream = new Pen(Color.FromArgb(132, 148, 86), 2f))
                    {
                        g.DrawLine(penStream, chuteX + 2, chuteY + 3, chuteX + chuteW / 2, chuteY + chuteH - 2);
                    }
                }
            }

            if (dangTieuThuTest)
            {
                float t = tienTrinhTieuThu;
                int startX = innerLeftX;
                int wTotal = innerRightX - startX;
                float ratioH = 0.80f * (1f - t) + 0.26f * t;
                int hCur = Math.Max(4, (int)((troughH - 4) * ratioH));
                int waterTop = troughBottomY - hCur - 1;
                mucMatNuocY = waterTop;

                Color colWater = LerpColor(Color.FromArgb(175, 14, 165, 233), Color.FromArgb(255, 113, 128, 74), t);
                using (SolidBrush brWater = new SolidBrush(colWater))
                {
                    g.FillRectangle(brWater, startX, waterTop, wTotal, hCur);
                }

                if (t < 0.6f)
                {
                    int alphaWave = (int)(220 * (1f - t / 0.6f));
                    using (Pen penWave = new Pen(Color.FromArgb(alphaWave, 186, 230, 253), 1.2f))
                    {
                        for (int wx = startX; wx < startX + wTotal - 6; wx += 14)
                        {
                            int segW = Math.Min(14, (startX + wTotal) - wx);
                            g.DrawLine(penWave, wx, waterTop, wx + segW, waterTop + 1);
                        }
                    }
                }

                if (t >= 0.60f)
                {
                    int alphaBug = (int)(255 * Math.Min(1f, (t - 0.60f) / 0.35f));
                    Color colBug = Color.FromArgb(alphaBug, 54, 46, 20);
                    int stepBug = Math.Max(30, wTotal / 3);
                    for (int bx = startX + 5; bx < startX + wTotal - 8; bx += stepBug)
                    {
                        VeConBo(g, bx, troughBottomY - hCur - 2, colBug);
                    }
                }
            }
            else if (mucNuocSach > 0.01f)
            {
                int startX = innerLeftX;
                int wClean = innerRightX - startX;
                int maxWaterH = Math.Max(6, (int)((troughH - 4) * 0.80f));
                int hClean = Math.Max(2, (int)(maxWaterH * mucNuocSach));
                int waterTop = troughBottomY - hClean - 1;
                mucMatNuocY = waterTop;

                using (SolidBrush brWater = new SolidBrush(Color.FromArgb(175, 14, 165, 233)))
                using (Pen penWave = new Pen(Color.FromArgb(186, 230, 253), 1.3f))
                {
                    g.FillRectangle(brWater, startX, waterTop, wClean, hClean);

                    for (int wx = startX; wx < startX + wClean - 6; wx += 14)
                    {
                        int segW = Math.Min(14, (startX + wClean) - wx);
                        int shift = ((wx + phaHatRoi) % 28 < 14) ? 0 : 1;
                        g.DrawLine(penWave, wx, waterTop + shift, wx + segW, waterTop + 1 - shift);
                    }
                }
            }

            VeCumTruotThanhRay(g, troughX, troughW, railY, troughBottomY, viTriTruotNuoc, dangDon && !dangChuanBiDonNuoc, dangCap && !dangChuanBiCapNuoc, laMangAn: false, mucMatNuocY: mucMatNuocY);
        }

        private void VeMotKhayTrung(Graphics g, int x, int y, int oRong, int oCao, int soTrung, string nhanKhay, bool laKhayChinh)
        {
            int soCot = 6, soHang = 2;
            int khayWidth = oRong * soCot + 14;
            int khayHeight = oCao * soHang + 14;

            Rectangle rectKhay = new Rectangle(x, y, khayWidth, khayHeight);
            Color bgKhay = laKhayChinh ? Color.FromArgb(200, 224, 242, 254) : Color.FromArgb(145, 203, 213, 225);
            Color borderKhay = laKhayChinh ? Color.DeepSkyBlue : Color.SlateGray;

            using (SolidBrush brushKhay = new SolidBrush(bgKhay))
            using (Pen penKhay = new Pen(borderKhay, laKhayChinh ? 2.2f : 1.5f))
            using (Font fTag = new Font("Segoe UI", 7.8f, FontStyle.Bold))
            {
                g.FillRectangle(brushKhay, rectKhay);
                g.DrawRectangle(penKhay, rectKhay);
                g.DrawString(nhanKhay, fTag, laKhayChinh ? Brushes.LightSkyBlue : Brushes.Silver, x, y - 16);

                for (int hang = 0; hang < soHang; hang++)
                {
                    for (int cot = 0; cot < soCot; cot++)
                    {
                        int eggX = x + 7 + cot * oRong;
                        int eggY = y + 7 + hang * oCao;
                        int eggSize = oRong - 6;

                        g.DrawEllipse(Pens.DimGray, eggX, eggY, eggSize, eggSize);

                        // Thứ tự đặt từ PHẢI qua TRÁI theo từng cột:
                        // Cột 5 (phải cùng): Quả 1 (trên), Quả 2 (dưới)
                        // Cột 4: Quả 3 (trên), Quả 4 (dưới) ... Cột 0 (trái cùng): Quả 11 (trên), Quả 12 (dưới)
                        int thuTuQua = (5 - cot) * 2 + hang + 1;

                        if (thuTuQua <= soTrung)
                        {
                            using (SolidBrush brushTrung = new SolidBrush(Color.SandyBrown))
                            using (Pen penTrung = new Pen(Color.Chocolate, 1.8f))
                            {
                                g.FillEllipse(brushTrung, eggX, eggY, eggSize, eggSize);
                                g.DrawEllipse(penTrung, eggX, eggY, eggSize, eggSize);
                            }
                        }
                    }
                }
            }
        }

        private void PnlBangTaiTrung_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = Math.Max(500, pnlBangTaiTrung.Width);
            int ph = Math.Max(125, pnlBangTaiTrung.Height);

            int oRong = 35, oCao = 35;
            int khayWidth = oRong * 6 + 14;  // 224px
            int khayHeight = oCao * 2 + 14;  // 84px

            // Khoảng cách bước khay = 7 cột (6 cột khay + 1 cột khe hở = 245px = 14 nửa-cột)
            int buocNhayKhay = oRong * 7;

            int khayY = Math.Max(36, (ph - khayHeight) / 2 + 12);
            int btX = 12;
            int btW = pw - 24;
            int btY = khayY + khayHeight + 2;
            int btH = 11;

            // Tọa độ X cố định của 2 vòi đặt trứng (lệch chéo nhau 0.5 cột = 17.5px)
            int voi1X = Math.Max(khayWidth + 30, pw / 2 + 45); // Vòi 1: đặt hàng trên
            int voi2X = voi1X + (oRong / 2);                   // Vòi 2: đặt hàng dưới (lệch chéo sang phải)

            int soTrung = Math.Max(0, Math.Min(12, PlcDataShared.SoTrung_TrongKhay));

            // Băng tải chạy từ TRÁI -> PHẢI (đơn vị: nửa-cột = 0.5 * oRong):
            // - soTrung == 0: Cột 5 khay mới nằm sẵn tại tâm Vòi 1 (buocDaTroi = 0)
            // - soTrung == 1..11: dịch đều sang phải 0.5 cột/giây từ 0 đến 11
            // - soTrung >= 12: dừng băng tải chính 2s (%Q0.5 = 0), đẩy khay đầy sang phải 1.5 cột (11 -> 14)
            //   để kéo Cột 5 của khay rỗng kế tiếp bên TRÁI (-14 -> 0) vào đúng tâm Vòi 1 -> không giật khi reset 12 -> 0
            float buocDaTroi;
            if (soTrung == 0)
                buocDaTroi = 0f;
            else if (soTrung >= 12)
                buocDaTroi = 11.0f + 1.5f * Math.Min(2.0f, phaChuyenDongTrung);
            else
                buocDaTroi = (soTrung - 1) + Math.Min(1.0f, phaChuyenDongTrung);

            int activeKhayX = (int)Math.Round(voi1X - (7 + 5 * oRong + oRong / 2.0) + buocDaTroi * (oRong / 2.0));

            // Vẽ thân băng tải và con lăn chuyển động đồng bộ theo activeKhayX (trái -> phải)
            g.FillRectangle(Brushes.DimGray, btX, btY, btW, btH);
            g.DrawRectangle(Pens.Black, btX, btY, btW, btH);
            int offsetRoller = ((activeKhayX % 22) + 22) % 22;
            for (int cx = btX + 8 + offsetRoller; cx < btX + btW - 8; cx += 22)
            {
                g.FillEllipse(Brushes.Silver, cx, btY + 2, 7, 7);
            }

            // Vẽ chuỗi các khay trên băng tải (chạy từ TRÁI -> PHẢI):
            // k < 0: các khay rỗng đang chờ phía sau (bên TRÁI: x < activeKhayX)
            // k = 0: khay hiện tại đang đóng
            // k > 0: các khay đã đóng đầy phía trước (bên PHẢI: x > activeKhayX)
            for (int k = -3; k <= 3; k++)
            {
                int x = activeKhayX + k * buocNhayKhay;
                if (x + khayWidth < -10 || x > pw + 10) continue;

                if (k == 0)
                {
                    string nhan = soTrung >= 12
                        ? "KHAY ĐÃ ĐÓNG (12/12)"
                        : $"Đã có ({soTrung}/12) quả";
                    VeMotKhayTrung(g, x, khayY, oRong, oCao, soTrung, nhan, true);
                }
                else if (k < 0)
                {
                    VeMotKhayTrung(g, x, khayY, oRong, oCao, 0, "KHAY RỖNG (0/12)", false);
                }
                else
                {
                    VeMotKhayTrung(g, x, khayY, oRong, oCao, 12, "KHAY ĐÃ ĐÓNG (12/12)", false);
                }
            }

            // Vẽ 2 đầu đặt trứng cố định phía trên băng tải (Vòi 1 hàng trên, Vòi 2 hàng dưới lệch chéo)
            int dauVoi1Y = khayY + 7 + oCao / 2;
            int dauVoi2Y = khayY + 7 + oCao + oCao / 2;

            using (Pen penThanhDo = new Pen(Color.FromArgb(148, 163, 184), 2f))
            using (Pen penNetDut = new Pen(Color.FromArgb(110, 56, 189, 248), 1.3f) { DashStyle = DashStyle.Dot })
            using (Font fVoi = new Font("Segoe UI", 7.3f, FontStyle.Bold))
            {
                // Thanh gá chung phía trên
                g.DrawLine(penThanhDo, voi1X - 18, 3, voi2X + 18, 3);

                // Vòi 1 (Hàng trên)
                g.DrawLine(penThanhDo, voi1X, 3, voi1X, 10);
                g.DrawLine(penNetDut, voi1X, 20, voi1X, dauVoi1Y);
                if (hieuUngVoiDangTha == 1)
                {
                    using (SolidBrush brV1 = new SolidBrush(Color.FromArgb(74, 222, 128)))
                        g.FillEllipse(brV1, voi1X - 5, 10, 10, 10);
                }
                g.DrawEllipse(penThanhDo, voi1X - 5, 10, 10, 10);
                g.DrawString("V1", fVoi, Brushes.LightSkyBlue, voi1X - 22, 5);

                // Vòi 2 (Hàng dưới - lệch chéo sang phải)
                g.DrawLine(penThanhDo, voi2X, 3, voi2X, 10);
                g.DrawLine(penNetDut, voi2X, 20, voi2X, dauVoi2Y);
                if (hieuUngVoiDangTha == 2)
                {
                    using (SolidBrush brV2 = new SolidBrush(Color.FromArgb(74, 222, 128)))
                        g.FillEllipse(brV2, voi2X - 5, 10, 10, 10);
                }
                g.DrawEllipse(penThanhDo, voi2X - 5, 10, 10, 10);
                g.DrawString("V2", fVoi, Brushes.Gold, voi2X + 7, 5);
            }
        }

        private void VeMotDayChuyenPhan(
            Graphics g,
            string tenDayChuyen,
            int btX,
            int btY,
            int btW,
            int btH,
            int rollerSpacing,
            int viTriConLan,
            int khoiPhanX,
            int[] offsetCum,
            int[] rongCum,
            int[] caoCum,
            Color mauChinhColor,
            Color mauPhuColor)
        {
            using (Font fLine = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(tenDayChuyen, fLine);
                float centerX = btX + (btW - textSize.Width) / 2f;
                g.DrawString(tenDayChuyen, fLine, Brushes.Silver, centerX, btY - 48);
            }

            g.FillRectangle(Brushes.DimGray, btX, btY, btW, btH);
            g.DrawRectangle(Pens.Black, btX, btY, btW, btH);
            for (int cx = btX + 8 + viTriConLan; cx < btX + btW - 10; cx += rollerSpacing)
            {
                g.FillEllipse(Brushes.Silver, cx, btY + 2, 8, 8);
            }

            using (SolidBrush mauChinh = new SolidBrush(mauChinhColor))
            using (SolidBrush mauPhu = new SolidBrush(mauPhuColor))
            {
                int chuKy = 720;
                int soVongLap = (btW / chuKy) + 2;

                for (int v = -1; v < soVongLap; v++)
                {
                    for (int i = 0; i < offsetCum.Length; i++)
                    {
                        int x = btX + v * chuKy + ((khoiPhanX + offsetCum[i]) % chuKy);
                        int w = rongCum[i];
                        int h = caoCum[i];

                        if (x + w < btX || x > btX + btW) continue;

                        int y = btY - h + 3;
                        g.FillEllipse(mauChinh, x, y, w, h);
                        g.FillEllipse(mauPhu, x + 4, y + 3, Math.Max(6, w - 10), Math.Max(5, h - 6));
                        g.DrawEllipse(Pens.Black, x, y, w, h);
                    }
                }
            }
        }

        private void PnlBangTaiPhan_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = Math.Max(260, pnlBangTaiPhan.Width);
            int ph = Math.Max(130, pnlBangTaiPhan.Height);

            int btX = 10;
            int chuteW = 40;
            int btW = pw - btX - chuteW - 8;
            int btH = 12;

            int btY1 = Math.Max(40, ph * 34 / 100);
            int btY2 = Math.Max(btY1 + 54, ph * 78 / 100);

            int[] offsetCum1 = { 0, 52, 110, 195, 250, 325, 415, 480, 565, 635 };
            int[] rongCum1 = { 42, 46, 38, 48, 40, 44, 46, 39, 45, 41 };
            int[] caoCum1 = { 21, 24, 19, 25, 20, 22, 24, 19, 23, 21 };

            int[] offsetCum2 = { 18, 55, 92, 145, 182, 230, 275, 335, 378, 430, 485, 540, 595, 655 };
            int[] rongCum2 = { 30, 34, 28, 36, 31, 33, 29, 35, 32, 30, 36, 29, 34, 31 };
            int[] caoCum2 = { 16, 18, 15, 19, 16, 17, 15, 18, 17, 15, 19, 16, 18, 16 };

            VeMotDayChuyenPhan(
                g,
                "CỤM DÂY CHUYỀN PHÂN 1",
                btX, btY1, btW, btH,
                18, viTriConLanPhan1, khoiPhanX1,
                offsetCum1, rongCum1, caoCum1,
                Color.FromArgb(101, 67, 56),
                Color.FromArgb(62, 39, 35));

            int midY = (btY1 + btY2 - 35) / 2 + 8;
            using (Pen pMid = new Pen(Color.FromArgb(45, 55, 72), 1) { DashStyle = DashStyle.Dot })
                g.DrawLine(pMid, btX, midY, btX + btW, midY);

            VeMotDayChuyenPhan(
                g,
                "CỤM DÂY CHUYỀN PHÂN 2",
                btX, btY2, btW, btH,
                18, viTriConLanPhan2, khoiPhanX2,
                offsetCum2, rongCum2, caoCum2,
                Color.FromArgb(120, 78, 58),
                Color.FromArgb(78, 52, 46));

            int chuteX = btX + btW + 4;
            int chuteY = Math.Max(14, btY1 - 24);
            int chuteH = (btY2 + btH) - chuteY + 6;
            using (SolidBrush brChute = new SolidBrush(Color.FromArgb(30, 58, 138)))
            using (Pen penChute = new Pen(Color.SteelBlue, 1.8f))
            using (Font fChute = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                g.FillRectangle(brChute, chuteX, chuteY, chuteW, chuteH);
                g.DrawRectangle(penChute, chuteX, chuteY, chuteW, chuteH);
                g.DrawString("GOM\n→ T2", fChute, Brushes.White, chuteX + 3, chuteY + chuteH / 2 - 14);
            }
        }

        private void PnlTienIchChuong_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = Math.Max(280, pnlTienIchChuong.Width);
            int ph = Math.Max(130, pnlTienIchChuong.Height);

            bool onlineVaRun = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC1_DangRun && PlcDataShared.M_HeThong_Run;
            bool suoiOn = onlineVaRun && PlcDataShared.Q_HeThongSuoi;
            bool quatOn = onlineVaRun && PlcDataShared.Q_QuatThongGio;
            bool denOn = onlineVaRun && PlcDataShared.Q_DenChieuSang;

            // 1. Khung mặt cắt ngang chuồng nuôi
            int houseX = 8;
            int houseY = 10;
            int houseW = pw - 16;
            int houseH = ph - 16;

            using (SolidBrush brBgChuong = new SolidBrush(denOn ? Color.FromArgb(30, 31, 28) : Color.FromArgb(22, 24, 29)))
            using (Pen penKhungChuong = new Pen(Color.FromArgb(71, 85, 105), 1.5f))
            {
                g.FillRectangle(brBgChuong, houseX, houseY, houseW, houseH);
                g.DrawRectangle(penKhungChuong, houseX, houseY, houseW, houseH);
            }

            // 2. Quạt thông gió vách phải (Q0.2)
            int fanSize = Math.Max(56, Math.Min(86, houseH - 28));
            int fanX = houseX + houseW - fanSize - 24;
            int fanY = houseY + (houseH - fanSize) / 2 + 6;

            // 3. Tủ điện & Nguồn tổng góc trên trái
            // int elecX = houseX + 6;
            // int elecY = houseY + 6;
            // int elecW = Math.Max(84, Math.Min(102, pw / 4));
            // int elecH = Math.Max(42, (houseH - 16) / 2 - 2);

            // using (SolidBrush brCabinet = new SolidBrush(Color.FromArgb(30, 41, 59)))
            // using (Pen penCabinet = new Pen(onlineVaRun ? Color.FromArgb(52, 211, 153) : Color.FromArgb(100, 116, 139), 1.4f))
            // using (Font fCabTitle = new Font("Segoe UI", 7.3f, FontStyle.Bold))
            // using (Font fCabSub = new Font("Segoe UI", 7.1f, FontStyle.Bold))
            // {
            //     g.FillRectangle(brCabinet, elecX, elecY, elecW, elecH);
            //     g.DrawRectangle(penCabinet, elecX, elecY, elecW, elecH);

            //     g.DrawString("TỦ ĐIỆN", fCabTitle, Brushes.WhiteSmoke, elecX + 5, elecY + 3);

            //     Color ledMain = onlineVaRun ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113);
            //     using (SolidBrush brLed = new SolidBrush(ledMain))
            //         g.FillEllipse(brLed, elecX + elecW - 14, elecY + 5, 8, 8);

            //     string txtNguon = onlineVaRun ? "NGUỒN: ON" : "NGUỒN: OFF";
            //     g.DrawString(txtNguon, fCabSub, onlineVaRun ? Brushes.LightGreen : Brushes.Salmon, elecX + 5, elecY + 17);

            //     // 3 chấm LED tải Q0.1 (Sưởi) - Q0.2 (Quạt) - Q0.3 (Đèn)
            //     int ledY = elecY + elecH - 11;
            //     g.FillEllipse(suoiOn ? Brushes.OrangeRed : Brushes.DimGray, elecX + 8, ledY, 6, 6);
            //     g.FillEllipse(quatOn ? Brushes.LimeGreen : Brushes.DimGray, elecX + 34, ledY, 6, 6);
            //     g.FillEllipse(denOn ? Brushes.Gold : Brushes.DimGray, elecX + 60, ledY, 6, 6);
            // }

            //3.1 Đường dây trục nguồn trên trần nối từ tủ điện sang đèn & quạt
            int busY = houseY + 6;
            // int busEndX = fanX + fanSize / 2;
            // using (Pen penBus = new Pen(onlineVaRun ? Color.FromArgb(245, 158, 11) : Color.FromArgb(71, 85, 105), 2f))
            // {
            //     g.DrawLine(penBus, elecX + elecW, busY, busEndX, busY);
            //     g.DrawLine(penBus, busEndX, busY, busEndX, fanY);
            // }

            // 4. Hệ thống Chiếu sáng trên trần (Q0.3)
            int lightLeft = houseX + 10;                          // Sửa elecX + elecW + 8 -> houseX + 10
            int lightRight = fanX - 8;
            int lightSpan = Math.Max(40, lightRight - lightLeft);
            int soDen = lightSpan >= 180 ? 4 : 3;                 // Tăng số lượng lên 3-4 đèn

            using (Font fDen = new Font("Segoe UI", 7.3f, FontStyle.Bold))
            {
                string lblDen = denOn ? "ĐÈN (Q0.3): BẬT" : "ĐÈN (Q0.3): TẮT";
                g.DrawString(lblDen, fDen, denOn ? Brushes.Gold : Brushes.Silver, lightLeft + 105, busY + 35);

                for (int i = 0; i < soDen; i++)
                {
                    int lx = lightLeft + (lightSpan * (i + 1)) / (soDen + 1); // Sửa / 3 -> / (soDen + 1) để không tràn sang quạt
                    int lampTopY = busY + 17;
                    int lampBotY = lampTopY + 8;

                    using (Pen penDrop = new Pen(denOn ? Color.Gold : Color.Gray, 1.5f))
                        g.DrawLine(penDrop, lx, busY, lx, lampTopY);

                    if (denOn)
                    {
                        int coneBottomY = houseY + houseH - 3;
                        int coneHalfW = Math.Min(42, lightSpan / (soDen * 2) + 12);
                        Point[] conePts =
                        {
                            new Point(lx - 7, lampBotY),
                            new Point(lx + 7, lampBotY),
                            new Point(lx + coneHalfW, coneBottomY),
                            new Point(lx - coneHalfW, coneBottomY)
                        };
                        using (LinearGradientBrush brCone = new LinearGradientBrush(
                            new Point(lx, lampBotY),
                            new Point(lx, coneBottomY),
                            Color.FromArgb(95, 253, 224, 71),
                            Color.FromArgb(5, 253, 224, 71)))
                        {
                            g.FillPolygon(brCone, conePts);
                        }
                    }

                    Point[] shadePts =
                    {
                        new Point(lx - 5, lampTopY),
                        new Point(lx + 5, lampTopY),
                        new Point(lx + 10, lampBotY),
                        new Point(lx - 10, lampBotY)
                    };
                    g.FillPolygon(Brushes.SlateGray, shadePts);
                    using (SolidBrush brBulb = new SolidBrush(denOn ? Color.FromArgb(254, 240, 138) : Color.FromArgb(71, 85, 105)))
                        g.FillEllipse(brBulb, lx - 4, lampBotY - 2, 8, 6);
                }
            }

            // 5. Hệ thống Sưởi góc dưới trái (Q0.1)
            int heatW = 102;                                      // Thu nhỏ bề ngang (cũ: 110..156)
            int heatH = 44;                                       // Thu nhỏ chiều cao
            int heatX = houseX + 6;
            int heatY = houseY + houseH - heatH - 5;

            if (suoiOn)
            {
                using (SolidBrush brWarmHalo = new SolidBrush(Color.FromArgb(45, 249, 115, 22)))
                    g.FillRectangle(brWarmHalo, heatX - 2, heatY - 4, heatW + 6, heatH + 6);
            }

            using (SolidBrush brHeater = new SolidBrush(Color.FromArgb(30, 34, 42)))
            using (Pen penHeater = new Pen(suoiOn ? Color.FromArgb(249, 115, 22) : Color.FromArgb(100, 116, 139), 1.5f))
            using (Font fHeat = new Font("Segoe UI", 7.3f, FontStyle.Bold))
            {
                g.FillRectangle(brHeater, heatX, heatY, heatW, heatH);
                g.DrawRectangle(penHeater, heatX, heatY, heatW, heatH);

                string txtSuoi = suoiOn ? "SƯỞI (Q0.1): BẬT" : "SƯỞI (Q0.1): TẮT";
                g.DrawString(txtSuoi, fHeat, suoiOn ? Brushes.Orange : Brushes.Silver, heatX + 5, heatY + 3);

                int coilLeft = heatX + 8;
                int coilRight = heatX + heatW - 8;
                int firstCoilY = heatY + 20;
                int coilGap = Math.Max(6, (heatH - 25) / 3);

                using (Pen penCoil = new Pen(suoiOn ? Color.FromArgb(251, 146, 60) : Color.FromArgb(71, 85, 105), 3f))
                using (Pen penCore = new Pen(suoiOn ? Color.Gold : Color.FromArgb(100, 116, 139), 1.2f))
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int cy = firstCoilY + c * coilGap;
                        g.DrawLine(penCoil, coilLeft, cy, coilRight, cy);
                        g.DrawLine(penCore, coilLeft + 2, cy, coilRight - 2, cy);
                    }
                }

                if (suoiOn)
                {
                    using (Pen penWave = new Pen(Color.FromArgb(200, 253, 186, 116), 1.5f))
                    {
                        for (int w = 0; w < 3; w++)
                        {
                            int wx = heatX + 24 + w * ((heatW - 48) / 2);
                            int shift = (int)((phaSongNhiet + w * 6) % 10);
                            int wyTop = heatY - 8 - (shift / 3);
                            g.DrawBezier(penWave,
                                wx, heatY + 14,
                                wx - 5, heatY + 6,
                                wx + 5, heatY - 1,
                                wx, wyTop);
                        }
                    }
                }
            }

            // 6. Quạt thông gió công nghiệp bên vách phải (Q0.2)
            using (Font fFan = new Font("Segoe UI", 7.3f, FontStyle.Bold))
            {
                string txtQuat = quatOn ? "QUẠT (Q0.2): BẬT" : "QUẠT (Q0.2): TẮT";
                SizeF szFan = g.MeasureString(txtQuat, fFan);
                g.DrawString(txtQuat, fFan, quatOn ? Brushes.LightGreen : Brushes.Silver,
                    fanX + (fanSize - szFan.Width) / 2f, fanY - 15);
            }

            using (SolidBrush brFanBox = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (Pen penFanBox = new Pen(quatOn ? Color.FromArgb(34, 197, 94) : Color.FromArgb(100, 116, 139), 1.6f))
            {
                g.FillRectangle(brFanBox, fanX, fanY, fanSize, fanSize);
                g.DrawRectangle(penFanBox, fanX, fanY, fanSize, fanSize);
            }

            int cx = fanX + fanSize / 2;
            int cyFan = fanY + fanSize / 2;
            int rOuter = fanSize / 2 - 5;

            using (SolidBrush brHole = new SolidBrush(Color.FromArgb(15, 23, 42)))
            using (Pen penRing = new Pen(Color.FromArgb(148, 163, 184), 1.4f))
            {
                g.FillEllipse(brHole, cx - rOuter, cyFan - rOuter, rOuter * 2, rOuter * 2);
                g.DrawEllipse(penRing, cx - rOuter, cyFan - rOuter, rOuter * 2, rOuter * 2);
            }

            GraphicsState state = g.Save();
            g.TranslateTransform(cx, cyFan);
            g.RotateTransform(gocQuayQuat);

            using (SolidBrush brBlade = new SolidBrush(quatOn ? Color.FromArgb(56, 189, 248) : Color.FromArgb(100, 116, 139)))
            using (Pen penBlade = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                int bladeLen = rOuter - 4;
                int bladeW = Math.Max(8, rOuter / 3);
                for (int b = 0; b < 4; b++)
                {
                    g.RotateTransform(90f);
                    Rectangle rectBlade = new Rectangle(3, -bladeW / 2, bladeLen - 3, bladeW);
                    g.FillEllipse(brBlade, rectBlade);
                    g.DrawEllipse(penBlade, rectBlade);
                }
            }
            g.Restore(state);

            g.FillEllipse(Brushes.Silver, cx - 6, cyFan - 6, 12, 12);
            g.DrawEllipse(Pens.Black, cx - 6, cyFan - 6, 12, 12);

            // Cửa chớp thông gió & luồng gió hút ra bên phải
            int louverX = fanX + fanSize + 3;
            int louverW = Math.Max(14, (houseX + houseW) - louverX - 2);
            using (Pen penLouver = new Pen(quatOn ? Color.FromArgb(74, 222, 128) : Color.FromArgb(100, 116, 139), 1.5f))
            {
                for (int k = 0; k < 4; k++)
                {
                    int ly = fanY + 10 + k * ((fanSize - 20) / 3);
                    g.DrawLine(penLouver, louverX, ly, louverX + louverW, quatOn ? ly - 3 : ly);
                }
            }

            if (quatOn)
            {
                int shiftWind = (int)(gocQuayQuat / 30f) % 12;
                using (Pen penWind = new Pen(Color.FromArgb(180, 125, 211, 252), 1.6f))
                {
                    for (int w = 0; w < 3; w++)
                    {
                        int wy = fanY + 14 + w * ((fanSize - 28) / 2);
                        int wx1 = fanX - 18 + shiftWind;
                        int wx2 = wx1 + 12;
                        g.DrawLine(penWind, wx1, wy, wx2, wy);
                        g.DrawLine(penWind, wx2 - 3, wy - 3, wx2, wy);
                        g.DrawLine(penWind, wx2 - 3, wy + 3, wx2, wy);
                    }
                }
            }
        }

        // Vẽ mô phỏng mini Trạm 2 theo giao diện Form 2 mới (2 khu vực: Bể ủ & sấy tích hợp + Cân & đóng bao 25kg)
        private void PnlMiniTram2_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pw = Math.Max(320, pnlMiniTram2.Width);
            int ph = Math.Max(220, pnlMiniTram2.Height);

            int cd = 0;
            if (!PlcDataShared.DaKetNoiPLC || !PlcDataShared.PLC2_DangRun || !PlcDataShared.M_T2_ChoPhepChay || PlcDataShared.M_Loi_KetQuyTrinhU_T2)
                cd = -1;
            else if (PlcDataShared.TrangThai_CongDoan_T2 > 0)
                cd = PlcDataShared.TrangThai_CongDoan_T2;

            bool dangCapPhan = PlcDataShared.DaKetNoiPLC && PlcDataShared.PLC2_DangRun && PlcDataShared.guidi_1_DangCapPhan == 1 && cd >= 0;
            bool dangU = (cd == 1);
            bool dangSay = (cd == 2);
            bool dangDongBao = (cd == 3);

            using (Font fTop = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (Font fTitle = new Font("Segoe UI", 7.8f, FontStyle.Bold))
            using (Font fVal = new Font("Segoe UI", 7.8f, FontStyle.Regular))
            using (Font fSmall = new Font("Segoe UI", 7.2f, FontStyle.Bold))
            using (SolidBrush brBoxBg = new SolidBrush(Color.FromArgb(24, 27, 34)))
            using (StringFormat sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                // Dòng trạng thái tổng quát Trạm 2 căn giữa trên cùng
                string stT2 = !PlcDataShared.DaKetNoiPLC ? "TRẠM 2: OFFLINE"
                            : !PlcDataShared.PLC2_DangRun ? "TRẠM 2: CPU STOP TRÊN TIA"
                            : PlcDataShared.M_Loi_KetQuyTrinhU_T2 ? "TRẠM 2: LỖI KẸT (%M20.5 = 1)"
                            : cd == -1 ? "TRẠM 2: ĐANG TẮT"
                            : cd == 0 ? "TRẠM 2: SẴN SÀNG / NẠP PHÂN"
                            : cd == 1 ? "TRẠM 2: ĐANG Ủ HOAI (Q0.0/Q0.1)"
                            : cd == 2 ? "TRẠM 2: ĐANG SẤY KHÔ (Q0.2)"
                            : "TRẠM 2: ĐANG ĐÓNG BAO (Q0.3)";
                Brush brSt = PlcDataShared.M_Loi_KetQuyTrinhU_T2 ? Brushes.OrangeRed : (cd >= 0 ? Brushes.LightSkyBlue : Brushes.Gray);
                g.DrawString(stT2, fTop, brSt, new RectangleF(6, 5, pw - 12, 20), sfCenter);

                // Chia 2 khu vực giống Form 2 mới: Trái = Bể ủ & sấy tích hợp | Phải = Cân & đóng bao 25kg
                int margin = 10;
                int gap = 12;
                int hamW = (pw - margin * 2 - gap) * 56 / 100;
                int dongBaoX = margin + hamW + gap;
                int dongBaoW = pw - dongBaoX - margin;

                int topBoxY = 66;
                int boxH = ph - topBoxY - 12;
                int hamX = margin;
                int tamHamX = hamX + hamW / 2;

                // ================= 1. BỂ Ủ HOAI & SẤY TÍCH HỢP =================
                g.DrawString("1. BỂ Ủ & SẤY TÍCH HỢP", fTitle, Brushes.WhiteSmoke, new RectangleF(hamX, 28, hamW, 18), sfCenter);

                // Ống cấp phân góc trên trái bể
                int ongCapX = hamX + 20;
                g.FillRectangle(Brushes.SteelBlue, 4, 49, ongCapX + 4, 6);
                g.FillRectangle(Brushes.LightSteelBlue, ongCapX - 5, 54, 10, 8);

                // Động cơ khuấy mini trên nắp bể
                int motorW = 24, motorH = 14;
                g.FillRectangle(dangU ? Brushes.MediumSpringGreen : Brushes.SlateGray, tamHamX - motorW / 2, topBoxY - motorH - 2, motorW, motorH);
                g.DrawRectangle(Pens.WhiteSmoke, tamHamX - motorW / 2, topBoxY - motorH - 2, motorW, motorH);

                // Bình vi sinh mini góc trên phải bể
                int vsW = 28, vsH = 15;
                int vsX = hamX + hamW - vsW - 8;
                int vsY = topBoxY - vsH - 3;
                g.FillRectangle(dangU ? Brushes.SeaGreen : Brushes.DarkSlateGray, vsX, vsY, vsW, vsH);
                g.DrawRectangle(Pens.LightSkyBlue, vsX, vsY, vsW, vsH);
                g.DrawString("VS", fSmall, Brushes.White, new RectangleF(vsX, vsY, vsW, vsH), sfCenter);

                // Thân bể tích hợp
                g.FillRectangle(brBoxBg, hamX, topBoxY, hamW, boxH);
                Color vienBe = PlcDataShared.M_Loi_KetQuyTrinhU_T2 ? Color.Red
                             : dangU ? Color.LimeGreen
                             : dangSay ? Color.Orange
                             : Color.SlateGray;
                g.DrawRectangle(new Pen(vienBe, 2f), hamX, topBoxY, hamW, boxH);

                // Khối phân hữu cơ bên trong bể
                int dayBeY = topBoxY + boxH - 8;
                int caoPhanMax = dayBeY - (topBoxY + 12);
                int caoPhan = (int)(caoPhanMax * (Math.Min(PlcDataShared.KhoiLuong_TrongHam, 100) / 100.0));
                if (caoPhan > 0)
                {
                    using (SolidBrush brPhan = new SolidBrush(dangSay ? Color.FromArgb(154, 82, 26) : Color.FromArgb(128, 74, 24)))
                        g.FillRectangle(brPhan, hamX + 8, dayBeY - caoPhan, hamW - 16, caoPhan);
                }

                // Trục & 2 tầng cánh khuấy trong bể
                using (Pen penTruc = new Pen(dangU ? Color.FromArgb(52, 211, 153) : Color.Silver, 2.5f))
                {
                    g.DrawLine(penTruc, tamHamX, topBoxY + 2, tamHamX, dayBeY - 4);
                    int yCanh1 = topBoxY + boxH * 38 / 100;
                    int yCanh2 = topBoxY + boxH * 68 / 100;
                    int rCanh = Math.Max(22, hamW / 4);
                    g.DrawLine(penTruc, tamHamX - rCanh, yCanh1, tamHamX + rCanh, yCanh1);
                    g.DrawLine(penTruc, tamHamX - rCanh, yCanh2, tamHamX + rCanh, yCanh2);
                }

                // Thanh gia nhiệt 2 vách & đáy bể
                using (SolidBrush brHeat = new SolidBrush(dangSay ? Color.FromArgb(234, 88, 12) : Color.FromArgb(51, 65, 85)))
                {
                    g.FillRectangle(brHeat, hamX + 2, topBoxY + 10, 5, dayBeY - topBoxY - 10);
                    g.FillRectangle(brHeat, hamX + hamW - 7, topBoxY + 10, 5, dayBeY - topBoxY - 10);
                    g.FillRectangle(brHeat, hamX + 2, dayBeY, hamW - 4, 6);
                }

                if (dangCapPhan)
                {
                    g.FillEllipse(Brushes.Peru, ongCapX - 3, topBoxY + 8, 6, 6);
                    g.FillEllipse(Brushes.SandyBrown, ongCapX - 3, topBoxY + 20, 6, 6);
                }

                // Thẻ thông số căn giữa trong bể
                int cardW = hamW - 22;
                int cardH = 44;
                int cardX = tamHamX - cardW / 2;
                int cardY = topBoxY + (boxH - cardH) / 2;
                using (SolidBrush brCardInfo = new SolidBrush(Color.FromArgb(215, 15, 23, 42)))
                using (Pen penCardInfo = new Pen(Color.FromArgb(148, 163, 184), 1f))
                {
                    g.FillRectangle(brCardInfo, cardX, cardY, cardW, cardH);
                    g.DrawRectangle(penCardInfo, cardX, cardY, cardW, cardH);
                }
                g.DrawString($"{PlcDataShared.KhoiLuong_TrongHam} / 100 kg", fTitle, Brushes.White, new RectangleF(cardX, cardY + 3, cardW, 18), sfCenter);
                g.DrawString($"T:{PlcDataShared.NhietDo_UHoai}°C | Ẩm:{PlcDataShared.DoAm_Say}%", fVal, Brushes.Gold, new RectangleF(cardX, cardY + 22, cardW, 18), sfCenter);

                // ================= 2. CÂN LOADCELL & ĐÓNG BAO 25KG =================
                g.DrawString("2. ĐÓNG BAO 25KG", fTitle, Brushes.WhiteSmoke, new RectangleF(dongBaoX, 28, dongBaoW, 18), sfCenter);
                g.FillRectangle(brBoxBg, dongBaoX, topBoxY, dongBaoW, boxH);
                g.DrawRectangle(new Pen(dangDongBao ? Color.DeepSkyBlue : Color.SlateGray, 2f), dongBaoX, topBoxY, dongBaoW, boxH);

                int tamBaoX = dongBaoX + dongBaoW / 2;




                // Phễu xả phân hạ thấp xuống 10px (topBoxY + 18) cùng điểm bắt đầu dòng chảy
                int pheuTopY = topBoxY + 18;
                int pheuBotY = pheuTopY + 18;
                Point[] pheuPts =
                {
                    new Point(tamBaoX - 24, pheuTopY),
                    new Point(tamBaoX + 24, pheuTopY),
                    new Point(tamBaoX + 8, pheuBotY),
                    new Point(tamBaoX - 8, pheuBotY)
                };
                g.FillPolygon(Brushes.SteelBlue, pheuPts);
                g.DrawPolygon(Pens.LightSkyBlue, pheuPts);

                // Băng tải con lăn mini
                int btY = topBoxY + boxH - 46;
                //int btX = dongBaoX + 8;
                //int btW = dongBaoW - 16;
                //g.FillRectangle(Brushes.DimGray, btX, btY, btW, 10);
                //g.DrawRectangle(new Pen(dangDongBao ? Color.LightGreen : Color.Silver, 1f), btX, btY, btW, 10);
                //for (int cx = btX + 6; cx < btX + btW - 6; cx += 12)
                //{
                //    g.FillEllipse(Brushes.WhiteSmoke, cx, btY + 2, 6, 6);
                //}

                // Bao phân 3D mini trên băng tải
                int bagW = 42, bagH = 52;
                int bagX = tamBaoX - bagW / 2;
                int bagY = btY - bagH - 2;

                // Dòng phân rơi đứt quãng từng cụm chỉ xuất hiện khi biến Loadcell thay đổi
                if (demTickDongPhanT2 > 0)
                {
                    using (SolidBrush brDong = new SolidBrush(Color.FromArgb(146, 84, 24)))
                    using (SolidBrush brHat = new SolidBrush(Color.FromArgb(180, 110, 42)))
                    {
                        int startDropY = pheuBotY + 2 + (phaDongPhanT2 % 10);
                        for (int dy = startDropY; dy < bagY - 2; dy += 12)
                        {
                            int hCum = Math.Min(6, (bagY - 1) - dy);
                            if (hCum > 1)
                            {
                                g.FillEllipse(brDong, tamBaoX - 4, dy, 8, hCum);
                                g.FillEllipse(brHat, tamBaoX - 2, dy + 1, 4, Math.Max(1, hCum - 2));
                            }
                        }
                    }
                }





                double tyLeBao = Math.Min(1.0, Math.Max(0.0, PlcDataShared.KhoiLuong_Loadcell / 25.0));
                int caoBao = (int)(bagH * tyLeBao);

                g.FillRectangle(new SolidBrush(Color.FromArgb(236, 239, 241)), bagX, bagY, bagW, bagH);
                if (caoBao > 0)
                {
                    using (SolidBrush brBao = new SolidBrush(Color.FromArgb(120, 78, 58)))
                        g.FillRectangle(brBao, bagX + 1, bagY + bagH - caoBao, bagW - 2, caoBao);
                }
                g.DrawRectangle(new Pen(Color.FromArgb(69, 90, 100), 1.5f), bagX, bagY, bagW, bagH);

                // Thông số cân & số bao căn giữa phía dưới băng tải
                g.DrawString($"{PlcDataShared.KhoiLuong_Loadcell} / 25 kg", fVal, Brushes.White, new RectangleF(dongBaoX + 2, btY + 12, dongBaoW - 4, 16), sfCenter);
                g.DrawString($"Đã đóng: {PlcDataShared.SoBao_DaDong} bao", fTitle, Brushes.LightGreen, new RectangleF(dongBaoX + 2, btY + 28, dongBaoW - 4, 16), sfCenter);
            }
        }

        private void btnQuayLai_Click(object sender, EventArgs e)
        {
            timerAnimation.Stop();
            timerChuTrinhT1.Stop();

            Form mainForm = Application.OpenForms["FormMain"];
            if (mainForm != null)
                mainForm.Show();
            else
                new FormMain().Show();

            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //this.Text = $"Size ban đầu: {this.Width}, {this.Height}";
            ////    1260x740
            this.Size = new Size(1560, 950);
            this.CenterToScreen();

        }
    }
}