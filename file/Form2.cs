using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace giaodien
{
    public partial class Form2 : Form
    {
        private int soBaoCuTrenPLC = 0;
        private bool dangXuatBaoTrenBangTai = false;
        private int viTriBaoXuatX = 485;
        private int viTriConLan = 0;
        private System.Windows.Forms.Timer timerTram2;
        private Label lblCongDoanHienTai;
        private bool dangMoPhongCapPhanT2 = false;

        public Form2()
        {
            InitializeComponent();
            ChinhLaiBoCucForm2();

            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, pnlMoPhongTram2, new object[] { true });

            pnlMoPhongTram2.Paint += PnlMoPhongTram2_Paint;
            if (btnQuayLaiT2 != null) btnQuayLaiT2.Click += BtnQuayLaiT2_Click;
            if (btnGiaLapLoiT2 != null) btnGiaLapLoiT2.Click += BtnGiaLapLoiT2_Click;
            if (btnResetLoiT2 != null) btnResetLoiT2.Click += BtnResetLoiT2_Click;
            if (btnStartCucBoT2 != null) btnStartCucBoT2.Click += BtnStartCucBoT2_Click;
            if (btnStopCucBoT2 != null) btnStopCucBoT2.Click += BtnStopCucBoT2_Click;
            if (btnMoPhongCapPhanT2 != null) btnMoPhongCapPhanT2.Click += BtnMoPhongCapPhanT2_Click;
            this.FormClosing += Form2_FormClosing;

            if (PlcDataShared.DaKetNoiPLC)
                PlcDataShared.DocDuLieuPLC2();

            soBaoCuTrenPLC = PlcDataShared.SoBao_DaDong;

            timerTram2 = new System.Windows.Forms.Timer();
            timerTram2.Interval = 300;
            timerTram2.Tick += TimerTram2_Tick;
            timerTram2.Start();

            CapNhatGiaoDienT2();
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
                    btn.BringToFront();
                }
                if (c.HasChildren)
                    ApDungStyleButton(c);
            }
        }

        private void ChinhLaiBoCucForm2()
        {
            this.BackColor = Color.FromArgb(150, 125, 129);
            ApDungStyleButton(this);

            if (lblDongHoPLC2 != null)
            {
                lblDongHoPLC2.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                lblDongHoPLC2.ForeColor = Color.FromArgb(15, 23, 42);
            }

            lblCongDoanHienTai = new Label
            {
                AutoSize = false,
                Size = new Size(760, 34),
                Location = new Point(150, 42),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Text = "ĐANG KHỞI TẠO TRẠM 2..."
            };
            this.Controls.Add(lblCongDoanHienTai);
            lblCongDoanHienTai.BringToFront();

            if (btnQuayLaiT2 != null)
            {
                btnQuayLaiT2.Text = "← Quay lại";
                btnQuayLaiT2.Width = 110;
                btnQuayLaiT2.BackColor = Color.FromArgb(51, 65, 85);
            }

            int hangNutY = pnlMoPhongTram2 != null ? pnlMoPhongTram2.Bottom + 8 : 285;

            if (btnGiaLapLoiT2 != null)
            {
                btnGiaLapLoiT2.Text = "Kích lỗi T2 (%M21.2)";
                btnGiaLapLoiT2.Size = new Size(155, 30);
                btnGiaLapLoiT2.Location = new Point(40, hangNutY);
                btnGiaLapLoiT2.BackColor = Color.FromArgb(185, 28, 28);
                btnGiaLapLoiT2.BringToFront();
            }
            if (btnResetLoiT2 != null)
            {
                btnResetLoiT2.Text = "Tắt lỗi / Reset";
                btnResetLoiT2.Size = new Size(130, 30);
                btnResetLoiT2.Location = new Point(205, hangNutY);
                btnResetLoiT2.BackColor = Color.FromArgb(13, 148, 136);
                btnResetLoiT2.BringToFront();
            }
            if (btnStartCucBoT2 != null)
            {
                btnStartCucBoT2.Text = "▶ START cục bộ T2";
                btnStartCucBoT2.Size = new Size(155, 30);
                btnStartCucBoT2.Location = new Point(345, hangNutY);
                btnStartCucBoT2.BackColor = Color.FromArgb(22, 163, 74);
                btnStartCucBoT2.BringToFront();
            }
            if (btnStopCucBoT2 != null)
            {
                btnStopCucBoT2.Text = "■ STOP cục bộ T2";
                btnStopCucBoT2.Size = new Size(150, 30);
                btnStopCucBoT2.Location = new Point(510, hangNutY);
                btnStopCucBoT2.BackColor = Color.FromArgb(220, 38, 38);
                btnStopCucBoT2.BringToFront();
            }

            btnMoPhongCapPhanT2 = new Button
            {
                Name = "btnMoPhongCapPhanT2",
                Text = "Mô phỏng cấp phân T1->T2",
                Size = new Size(215, 30),
                Location = new Point(670, hangNutY),
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White
            };
            btnMoPhongCapPhanT2.FlatAppearance.BorderSize = 1;
            btnMoPhongCapPhanT2.FlatAppearance.BorderColor = Color.FromArgb(30, 41, 59);
            this.Controls.Add(btnMoPhongCapPhanT2);
            btnMoPhongCapPhanT2.BringToFront();

            // Hàng 2: Các nút Reset trực tiếp từng thanh ghi MW của PLC 2
            int hangResetMW_Y = hangNutY + 36;
            TaoNutResetMW_T2("btnResetMW30_T2", "Reset MW30 (Hầm=0)", 40, hangResetMW_Y, 145, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.XungBitPLC2(21, 5); // %M21.5 M_T2_ResetMW -> Reset CTU %DB2
                PlcDataShared.GhiMW_PLC2(40, 0);  // Reset %MW40 KL_Nap_CTU
                PlcDataShared.GhiMW_PLC2(30, 0);  // Reset %MW30 KL_ham_u
                PlcDataShared.DocDuLieuPLC2();
                CapNhatGiaoDienT2();
            });
            TaoNutResetMW_T2("btnResetMW32_T2", "Reset MW32 (Nhiệt=0)", 193, hangResetMW_Y, 150, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.XungBitPLC2(21, 5); // %M21.5 M_T2_ResetMW
                PlcDataShared.GhiMW_PLC2(32, 0);
                PlcDataShared.DocDuLieuPLC2();
                CapNhatGiaoDienT2();
            });
            TaoNutResetMW_T2("btnResetMW34_T2", "Reset MW34 (Ẩm=60)", 351, hangResetMW_Y, 145, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.XungBitPLC2(21, 5);
                PlcDataShared.GhiMW_PLC2(34, 60);
                PlcDataShared.DocDuLieuPLC2();
                CapNhatGiaoDienT2();
            });
            TaoNutResetMW_T2("btnResetMW36_T2", "Reset MW36 (Cân=0)", 504, hangResetMW_Y, 140, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.XungBitPLC2(21, 5);
                PlcDataShared.GhiMW_PLC2(36, 0);
                PlcDataShared.DocDuLieuPLC2();
                CapNhatGiaoDienT2();
            });
            TaoNutResetMW_T2("btnResetMW38_T2", "Reset MW38 (Bao=0)", 652, hangResetMW_Y, 140, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.GhiMW_PLC2(38, 0);
                soBaoCuTrenPLC = 0;
                PlcDataShared.DocDuLieuPLC2();
                CapNhatGiaoDienT2();
            });
            TaoNutResetMW_T2("btnResetAllMW_T2", "Reset ALL MW2", 800, hangResetMW_Y, 120, () =>
            {
                if (!KiemTraKetNoi()) return;
                PlcDataShared.XungBitPLC2(21, 5); // %M21.5 M_T2_ResetMW
                PlcDataShared.GhiMW_PLC2(40, 0);
                PlcDataShared.GhiMW_PLC2(30, 0);
                PlcDataShared.GhiMW_PLC2(32, 0);
                PlcDataShared.GhiMW_PLC2(34, 60);
                PlcDataShared.GhiMW_PLC2(36, 0);
                PlcDataShared.GhiMW_PLC2(38, 0);
                soBaoCuTrenPLC = 0;
                PlcDataShared.DocDuLieuPLC2();
                CapNhatGiaoDienT2();
            });

            int batDauLabelY = hangResetMW_Y + 36;

            if (lblHienThiMW_PLC2 != null)
            {
                lblHienThiMW_PLC2.AutoSize = true;
                lblHienThiMW_PLC2.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lblHienThiMW_PLC2.ForeColor = Color.FromArgb(15, 23, 42);
                lblHienThiMW_PLC2.Location = new Point(40, batDauLabelY);
            }

            if (lblTinHieuNhanT2 != null)
                lblTinHieuNhanT2.Location = new Point(40, batDauLabelY + 26);
            if (lblTinHieuGuiT2 != null && lblTinHieuNhanT2 != null)
            {
                lblTinHieuGuiT2.Left = lblTinHieuNhanT2.Left;
                lblTinHieuGuiT2.Top = lblTinHieuNhanT2.Bottom + 5;
            }
            if (lblTrangThaiT2 != null && lblTinHieuGuiT2 != null)
            {
                lblTrangThaiT2.Left = 40;
                lblTrangThaiT2.Top = lblTinHieuGuiT2.Bottom + 8;
            }
            if (lblKhoiLuongHam != null && lblTrangThaiT2 != null)
            {
                lblKhoiLuongHam.Left = 40;
                lblKhoiLuongHam.Top = lblTrangThaiT2.Bottom + 6;
            }
            if (lblNhietDoDoAm != null && lblKhoiLuongHam != null)
            {
                lblNhietDoDoAm.Left = 40;
                lblNhietDoDoAm.Top = lblKhoiLuongHam.Bottom + 6;
            }
            if (lblLoadcellBao != null && lblNhietDoDoAm != null)
            {
                lblLoadcellBao.Left = 40;
                lblLoadcellBao.Top = lblNhietDoDoAm.Bottom + 6;
            }
            if (lblCanhBaoLoiT2 != null && lblLoadcellBao != null)
            {
                lblCanhBaoLoiT2.Left = 40;
                lblCanhBaoLoiT2.Top = lblLoadcellBao.Bottom + 8;
                lblCanhBaoLoiT2.AutoSize = true;
            }

            foreach (Control c in this.Controls)
            {
                if (c is Label lbl && (lbl.Name == "label4" || lbl.Name == "label7"))
                    lbl.Visible = false;
            }
        }

        private void TaoNutResetMW_T2(string designName, string text, int x, int y, int width, Action onClick)
        {
            Button btn = new Button
            {
                Name = designName,
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 28),
                UseVisualStyleBackColor = false,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                BackColor = Color.FromArgb(88, 28, 135),
                ForeColor = Color.White
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(30, 41, 59);
            btn.Click += (s, e) => onClick();
            this.Controls.Add(btn);
            btn.BringToFront();
        }

        private bool KiemTraKetNoi()
        {
            if (!PlcDataShared.DaKetNoiPLC)
            {
                MessageBox.Show("Chưa kết nối PLC! Vui lòng quay lại màn hình chính và bấm 'KẾT NỐI 2 PLC'.", "Chưa kết nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private int XacDinhCongDoanThucTe()
        {
            if (!PlcDataShared.DaKetNoiPLC || !PlcDataShared.M_T2_ChoPhepChay || PlcDataShared.M_Loi_KetQuyTrinhU_T2)
                return -1;

            if (PlcDataShared.TrangThai_CongDoan_T2 > 0)
                return PlcDataShared.TrangThai_CongDoan_T2;

            return 0;
        }

        private void BtnStartCucBoT2_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            PlcDataShared.GhiBitPLC2(21, 4, false); // Tắt Stop %M21.4
            PlcDataShared.GhiBitPLC2(21, 3, true);  // Giữ Start cục bộ %M21.3 = 1
        }

        private void BtnStopCucBoT2_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            dangMoPhongCapPhanT2 = false;
            PlcDataShared.GhiBitPLC1(0, 5, false);  // Tắt cấp phân tay PLC 1
            PlcDataShared.GhiBitPLC2(21, 3, false); // Tắt Start cục bộ %M21.3 = 0
            PlcDataShared.GhiBitPLC2(21, 4, true);  // Giữ Stop cục bộ %M21.4 = 1
        }

        private void BtnMoPhongCapPhanT2_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;

            if (PlcDataShared.M_Loi_KetQuyTrinhU_T2 || PlcDataShared.M_Loi_KetQuyTrinhU_T1)
            {
                MessageBox.Show("Hệ thống đang bị khóa bởi lỗi! Vui lòng bấm 'Tắt lỗi / Reset' trước.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!dangMoPhongCapPhanT2 && (PlcDataShared.KhoiLuong_TrongHam >= 100 || PlcDataShared.guiden_plc1_0_BaoDay == 1))
            {
                MessageBox.Show("Hầm ủ đã đầy 100kg (guiden_plc1[0]=1), đang xử lý mẻ hiện tại!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dangMoPhongCapPhanT2 = !dangMoPhongCapPhanT2;

            if (dangMoPhongCapPhanT2)
            {
                PlcDataShared.GhiBitPLC2(21, 4, false);    // Nhả Stop cục bộ T2 (%M21.4)
                if (!PlcDataShared.M_HeThong_Run)
                    PlcDataShared.XungBitPLC1(0, 0);     // Bật Run PLC 1 (%M0.0)
                if (PlcDataShared.guidi_0_BatPlc2TuXa == 0)
                    PlcDataShared.GhiBitPLC1(0, 3, true); // Bật PLC 2 từ xa (%M0.3)

                PlcDataShared.GhiBitPLC1(0, 2, true);     // Bật Manual PLC 1 (%M0.2)
                PlcDataShared.GhiBitPLC1(0, 5, true);     // Kích cấp phân tay (%M0.5)
            }
            else
            {
                PlcDataShared.GhiBitPLC1(0, 5, false);    // Tắt cấp phân tay (%M0.5)
            }
        }

        private void TimerTram2_Tick(object sender, EventArgs e)
        {
            if (PlcDataShared.DaKetNoiPLC)
            {
                PlcDataShared.DocDuLieuPLC1();
                PlcDataShared.DocDuLieuPLC2();

                if (dangMoPhongCapPhanT2 && (PlcDataShared.KhoiLuong_TrongHam >= 100 || PlcDataShared.guiden_plc1_0_BaoDay == 1))
                {
                    dangMoPhongCapPhanT2 = false;
                    PlcDataShared.GhiBitPLC1(0, 5, false);
                }

                if (PlcDataShared.SoBao_DaDong > soBaoCuTrenPLC)
                {
                    dangXuatBaoTrenBangTai = true;
                    viTriBaoXuatX = 485;
                    soBaoCuTrenPLC = PlcDataShared.SoBao_DaDong;
                }
                else if (PlcDataShared.SoBao_DaDong == 0)
                {
                    soBaoCuTrenPLC = 0;
                }
            }

            int congDoan = XacDinhCongDoanThucTe();

            if (PlcDataShared.DaKetNoiPLC && (congDoan == 3 || dangXuatBaoTrenBangTai) && !PlcDataShared.M_Loi_KetQuyTrinhU_T2)
                viTriConLan = (viTriConLan + 5) % 16;

            if (dangXuatBaoTrenBangTai && !PlcDataShared.M_Loi_KetQuyTrinhU_T2)
            {
                viTriBaoXuatX += 18;
                if (viTriBaoXuatX > pnlMoPhongTram2.Width - 20)
                {
                    dangXuatBaoTrenBangTai = false;
                    viTriBaoXuatX = 485;
                }
            }

            CapNhatGiaoDienT2();
            pnlMoPhongTram2.Invalidate();
        }

        private void BtnGiaLapLoiT2_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            PlcDataShared.GhiBitPLC2(21, 2, !PlcDataShared.M_Sim_Loi_T2_Active); // %M21.2 M_T2_Sim_Loi
        }

        private void BtnResetLoiT2_Click(object sender, EventArgs e)
        {
            if (!KiemTraKetNoi()) return;
            dangMoPhongCapPhanT2 = false;
            PlcDataShared.GhiBitPLC1(0, 5, false);  // Tắt cấp phân tay PLC 1
            PlcDataShared.GhiBitPLC2(21, 2, false); // Tắt %M21.2
            PlcDataShared.XungBitPLC2(20, 6);        // Xung %M20.6 M_T2_NutResetLoi
        }

        private void CapNhatGiaoDienT2()
        {
            if (lblDongHoPLC2 != null)
                lblDongHoPLC2.Text = $"Bây giờ là: {PlcDataShared.LayChuoiThoiGianPLC()}";

            if (!PlcDataShared.DaKetNoiPLC)
            {
                if (lblCongDoanHienTai != null)
                {
                    lblCongDoanHienTai.Text = "CHƯA KẾT NỐI PLC — Bấm 'Quay lại' và kết nối 2 PLC trước";
                    lblCongDoanHienTai.BackColor = Color.FromArgb(71, 85, 105);
                }
                return;
            }

            if (btnGiaLapLoiT2 != null)
            {
                btnGiaLapLoiT2.UseVisualStyleBackColor = false;
                btnGiaLapLoiT2.Text = PlcDataShared.M_Sim_Loi_T2_Active
                    ? "ĐANG KÍCH %M21.2 = 1"
                    : "Kích lỗi T2 (%M21.2)";
                btnGiaLapLoiT2.BackColor = PlcDataShared.M_Sim_Loi_T2_Active ? Color.DarkOrange : Color.FromArgb(185, 28, 28);
                btnGiaLapLoiT2.ForeColor = Color.White;
            }

            if (btnMoPhongCapPhanT2 != null)
            {
                bool dangCap = dangMoPhongCapPhanT2 || PlcDataShared.guidi_1_DangCapPhan == 1;
                btnMoPhongCapPhanT2.UseVisualStyleBackColor = false;
                btnMoPhongCapPhanT2.Text = dangCap
                    ? "ĐANG CẤP PHÂN T1->T2 (Dừng)"
                    : "Mô phỏng cấp phân T1->T2";
                btnMoPhongCapPhanT2.BackColor = dangCap ? Color.FromArgb(217, 119, 6) : Color.FromArgb(37, 99, 235);
                btnMoPhongCapPhanT2.ForeColor = Color.White;
            }

            lblTinHieuNhanT2.Text = $"Nhận từ PLC 1 -> nhanduoc[0]={PlcDataShared.guidi_0_BatPlc2TuXa} | nhanduoc[1]={PlcDataShared.guidi_1_DangCapPhan} | nhanduoc[2]={PlcDataShared.guidi_2_KhoiLuongGui} kg";
            lblTinHieuGuiT2.Text = PlcDataShared.guiden_plc1_0_BaoDay == 1
                ? "Báo về PLC 1 -> guiden_plc1[0]=1 (ĐÃ ĐẦY / KHÔNG NHẬN PHÂN)"
                : "Báo về PLC 1 -> guiden_plc1[0]=0 (Sẵn sàng nhận phân)";

            if (lblHienThiMW_PLC2 != null)
            {
                lblHienThiMW_PLC2.Text = $"Thanh ghi MW PLC 2: MW30(Hầm ủ)={PlcDataShared.KhoiLuong_TrongHam}kg | MW32(Nhiệt)={PlcDataShared.NhietDo_UHoai}°C | MW34(Ẩm)={PlcDataShared.DoAm_Say}% | MW36(Cân)={PlcDataShared.KhoiLuong_Loadcell}kg | MW38(Bao)={PlcDataShared.SoBao_DaDong}";
            }

            int congDoan = XacDinhCongDoanThucTe();
            string moTaCongDoan;
            Color mauBanner;

            if (PlcDataShared.M_Loi_KetQuyTrinhU_T2)
            {
                moTaCongDoan = "CÔNG ĐOẠN: TẠM DỪNG DO LỖI KẸT QUY TRÌNH (%M20.5 = 1) - BẤM 'TẮT LỖI / RESET'";
                mauBanner = Color.FromArgb(185, 28, 28);
            }
            else if (congDoan == -1)
            {
                moTaCongDoan = "CÔNG ĐOẠN: ĐANG TẮT (Chờ PLC 1 bật guidi[0]=1 hoặc bấm START cục bộ)";
                mauBanner = Color.FromArgb(71, 85, 105);
            }
            else if (congDoan == 0)
            {
                string themTrangThai = (dangMoPhongCapPhanT2 || PlcDataShared.guidi_1_DangCapPhan == 1)
                    ? " [Đang nạp dần vào hầm từ Trạm 1]"
                    : "";
                moTaCongDoan = $"CÔNG ĐOẠN 0: ĐANG NẠP PHÂN VÀO HẦM Ủ ({PlcDataShared.KhoiLuong_TrongHam}/100 kg){themTrangThai}";
                mauBanner = Color.FromArgb(3, 105, 161);
            }
            else if (congDoan == 1)
            {
                moTaCongDoan = $"CÔNG ĐOẠN 1/3: ĐANG Ủ HOAI SINH HỌC — Đang tăng nhiệt ({PlcDataShared.NhietDo_UHoai}°C / 62°C)";
                mauBanner = Color.FromArgb(180, 83, 9);
            }
            else if (congDoan == 2)
            {
                moTaCongDoan = $"CÔNG ĐOẠN 2/3: ĐANG SẤY KHÔ — Đang giảm độ ẩm ({PlcDataShared.DoAm_Say}% -> 15%)";
                mauBanner = Color.FromArgb(161, 98, 7);
            }
            else
            {
                moTaCongDoan = $"CÔNG ĐOẠN 3/3: ĐANG CÂN LOADCELL & ĐÓNG BAO 25KG (Còn {PlcDataShared.KhoiLuong_TrongHam} kg trong hầm)";
                mauBanner = Color.FromArgb(21, 128, 61);
            }

            lblTrangThaiT2.Text = moTaCongDoan;
            if (lblCongDoanHienTai != null)
            {
                lblCongDoanHienTai.Text = moTaCongDoan;
                lblCongDoanHienTai.BackColor = mauBanner;
            }

            lblKhoiLuongHam.Text = $"Phân trong hầm ủ (MW30): {PlcDataShared.KhoiLuong_TrongHam} / 100 kg";
            lblNhietDoDoAm.Text = $"Nhiệt độ ủ (MW32): {PlcDataShared.NhietDo_UHoai} °C | Độ ẩm sấy (MW34): {PlcDataShared.DoAm_Say} %";
            lblLoadcellBao.Text = $"Cân Loadcell (MW36): {PlcDataShared.KhoiLuong_Loadcell} / 25 kg | Số bao đã đóng (MW38): {PlcDataShared.SoBao_DaDong} bao";

            if (PlcDataShared.M_Loi_KetQuyTrinhU_T2)
            {
                string nguyenNhan = PlcDataShared.M_Sim_Loi_T2_Active
                    ? "Kích giả lập %M21.2"
                    : "Tràn phân khi hầm đầy (guiden_plc1[0]=1 & nhanduoc[1]=1)";
                lblCanhBaoLoiT2.Text = $"CẢNH BÁO LỖI TRẠM 2 (%M20.5=1) [{nguyenNhan}]: Đã khóa toàn bộ thiết bị! Bấm 'Tắt lỗi / Reset'.";
                lblCanhBaoLoiT2.BackColor = Color.Red;
                lblCanhBaoLoiT2.ForeColor = Color.White;
            }
            else if (PlcDataShared.M_Sim_Loi_T2_Active || (PlcDataShared.guiden_plc1_0_BaoDay == 1 && PlcDataShared.guidi_1_DangCapPhan == 1))
            {
                lblCanhBaoLoiT2.Text = "CẢNH BÁO SỚM: Đang có tín hiệu lỗi trên Network 4 — Chờ Timer PLC đếm 3 giây để kích hoạt %M20.5...";
                lblCanhBaoLoiT2.BackColor = Color.DarkOrange;
                lblCanhBaoLoiT2.ForeColor = Color.White;
            }
            else
            {
                lblCanhBaoLoiT2.Text = "Trạng thái Trạm 2: Hoạt động bình thường";
                lblCanhBaoLoiT2.BackColor = Color.Transparent;
                lblCanhBaoLoiT2.ForeColor = Color.DarkGreen;
            }
        }

        private void PnlMoPhongTram2_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int congDoan = XacDinhCongDoanThucTe();

            if (PlcDataShared.DaKetNoiPLC && (dangMoPhongCapPhanT2 || PlcDataShared.guidi_1_DangCapPhan == 1) && !PlcDataShared.M_Loi_KetQuyTrinhU_T2)
            {
                g.FillEllipse(Brushes.SaddleBrown, 75, 16, 26, 14);
                g.DrawString("↓ Đang nạp dần (+10 kg/s)", new Font(this.Font, FontStyle.Bold), Brushes.DarkRed, 104, 14);
            }

            Color vienHamU = (congDoan == 1) ? Color.LimeGreen : Color.DarkOliveGreen;
            g.DrawRectangle(new Pen(vienHamU, congDoan == 1 ? 3 : 2), 15, 28, 150, 145);
            g.DrawString(congDoan == 1 ? "1. HẦM Ủ (ĐANG Ủ)" : "1. HẦM Ủ HOAI", this.Font, Brushes.Black, 28, 8);
            int caoPhan = (int)(135 * (Math.Min(PlcDataShared.KhoiLuong_TrongHam, 100) / 100.0));
            if (caoPhan > 0)
            {
                g.FillRectangle(Brushes.SaddleBrown, 18, 170 - caoPhan, 144, caoPhan);
            }
            g.DrawString($"{PlcDataShared.KhoiLuong_TrongHam} kg", this.Font, Brushes.White, 60, 82);
            if (congDoan == 1)
            {
                g.DrawString($"Ủ: {PlcDataShared.NhietDo_UHoai}/62°C", new Font(this.Font, FontStyle.Bold), Brushes.Yellow, 42, 104);
            }

            Brush mauSay = (congDoan == 2) ? Brushes.LightGoldenrodYellow : Brushes.WhiteSmoke;
            g.FillRectangle(mauSay, 190, 28, 150, 145);
            g.DrawRectangle(new Pen(Color.DarkOrange, congDoan == 2 ? 3 : 2), 190, 28, 150, 145);
            g.DrawString(congDoan == 2 ? "2. SẤY (ĐANG SẤY)" : "2. BUỒNG SẤY", this.Font, Brushes.Black, 208, 8);
            g.DrawString($"Nhiệt độ: {PlcDataShared.NhietDo_UHoai}°C", this.Font, Brushes.DarkSlateGray, 212, 72);
            g.DrawString($"Độ ẩm: {PlcDataShared.DoAm_Say}%", new Font(this.Font, FontStyle.Bold), Brushes.DarkRed, 218, 98);

            g.DrawString(congDoan == 3 ? "3. ĐÓNG BAO (ĐANG XẢ)" : "3. CÂN LOADCELL & ĐÓNG BAO 3D (25KG)", this.Font, Brushes.Black, 375, 8);

            Point[] pheuPoints = new Point[]
            {
                new Point(400, 26),
                new Point(470, 26),
                new Point(447, 50),
                new Point(423, 50)
            };
            g.FillPolygon(Brushes.LightSteelBlue, pheuPoints);
            g.DrawPolygon(new Pen(Color.SteelBlue, 2), pheuPoints);

            if (congDoan == 3 && PlcDataShared.KhoiLuong_Loadcell > 0 && !PlcDataShared.M_Loi_KetQuyTrinhU_T2)
            {
                using (SolidBrush brDongPhan = new SolidBrush(Color.FromArgb(109, 76, 65)))
                {
                    g.FillRectangle(brDongPhan, 428, 50, 14, 15);
                }
            }

            VeBaoPhan3D(g, 402, 62, 62, 95, 14, PlcDataShared.KhoiLuong_Loadcell, 25);

            int btX = 375, btY = 160, btW = Math.Max(260, pnlMoPhongTram2.Width - 390), btH = 16;
            g.FillRectangle(Brushes.DimGray, btX, btY, btW, btH);
            g.DrawRectangle(Pens.Black, btX, btY, btW, btH);
            for (int cx = btX + 8 + viTriConLan; cx < btX + btW - 8; cx += 16)
            {
                g.FillEllipse(Brushes.Silver, cx, btY + 3, 10, 10);
            }

            if (dangXuatBaoTrenBangTai)
            {
                VeBaoPhan3D(g, viTriBaoXuatX, 95, 44, 62, 10, 25, 25);
                g.DrawString("OK", new Font(this.Font, FontStyle.Bold), Brushes.DarkGreen, viTriBaoXuatX + 12, 78);
            }
        }

        private void VeBaoPhan3D(Graphics g, int x, int y, int w, int h, int depth, int khoiLuongHienTai, int khoiLuongMax)
        {
            double tyLeDay = Math.Min(1.0, Math.Max(0.0, (double)khoiLuongHienTai / khoiLuongMax));
            int chieuCaoPhan = (int)(h * tyLeDay);

            Rectangle matTruoc = new Rectangle(x, y, w, h);
            Point[] matHong = new Point[]
            {
                new Point(x + w, y),
                new Point(x + w + depth, y - depth / 2),
                new Point(x + w + depth, y + h - depth / 2),
                new Point(x + w, y + h)
            };
            Point[] miengBao = new Point[]
            {
                new Point(x, y),
                new Point(x + depth, y - depth / 2),
                new Point(x + w + depth, y - depth / 2),
                new Point(x + w, y)
            };

            using (SolidBrush brVoTruoc = new SolidBrush(Color.FromArgb(236, 239, 241)))
            using (SolidBrush brVoHong = new SolidBrush(Color.FromArgb(207, 216, 220)))
            using (SolidBrush brMieng = new SolidBrush(Color.FromArgb(220, 225, 228)))
            {
                g.FillRectangle(brVoTruoc, matTruoc);
                g.FillPolygon(brVoHong, matHong);
                g.FillPolygon(brMieng, miengBao);
            }

            if (chieuCaoPhan > 0)
            {
                int yPhan = y + h - chieuCaoPhan;

                Rectangle rectPhanTruoc = new Rectangle(x, yPhan, w, chieuCaoPhan);
                using (LinearGradientBrush brPhanTruoc = new LinearGradientBrush(
                    new Rectangle(x, y, w, h),
                    Color.FromArgb(141, 110, 99),
                    Color.FromArgb(78, 52, 46),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brPhanTruoc, rectPhanTruoc);
                }

                Point[] phanMatHong = new Point[]
                {
                    new Point(x + w, yPhan),
                    new Point(x + w + depth, yPhan - depth / 2),
                    new Point(x + w + depth, y + h - depth / 2),
                    new Point(x + w, y + h)
                };
                using (SolidBrush brPhanHong = new SolidBrush(Color.FromArgb(62, 39, 35)))
                {
                    g.FillPolygon(brPhanHong, phanMatHong);
                }

                if (tyLeDay < 1.0)
                {
                    Point[] matThoangPhan = new Point[]
                    {
                        new Point(x, yPhan),
                        new Point(x + depth, yPhan - depth / 2),
                        new Point(x + w + depth, yPhan - depth / 2),
                        new Point(x + w, yPhan)
                    };
                    using (SolidBrush brMatThoang = new SolidBrush(Color.FromArgb(161, 136, 127)))
                    {
                        g.FillPolygon(brMatThoang, matThoangPhan);
                    }
                }
            }

            using (Pen penVien = new Pen(Color.FromArgb(69, 90, 100), 1.8f))
            {
                g.DrawRectangle(penVien, matTruoc);
                g.DrawPolygon(penVien, matHong);
                g.DrawPolygon(penVien, miengBao);
            }

            string nhanKg = $"{khoiLuongHienTai}/{khoiLuongMax}kg";
            string nhanPhanTram = $"{(int)(tyLeDay * 100)}%";
            Brush mauChu = tyLeDay >= 0.45 ? Brushes.White : Brushes.Black;

            g.DrawString(nhanKg, new Font(this.Font.FontFamily, 8f, FontStyle.Bold), mauChu, x + 6, y + h / 2 - 12);
            g.DrawString(nhanPhanTram, new Font(this.Font.FontFamily, 8f, FontStyle.Regular), mauChu, x + 16, y + h / 2 + 4);
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (timerTram2 != null) timerTram2.Stop();
            if (PlcDataShared.DaKetNoiPLC && dangMoPhongCapPhanT2)
            {
                try { PlcDataShared.GhiBitPLC1(0, 5, false); } catch { }
            }
        }

        private void BtnQuayLaiT2_Click(object sender, EventArgs e)
        {
            if (timerTram2 != null) timerTram2.Stop();

            Form mainForm = Application.OpenForms["FormMain"];
            if (mainForm != null)
                mainForm.Show();
            else
                new FormMain().Show();

            this.Close();
        }

    }
}