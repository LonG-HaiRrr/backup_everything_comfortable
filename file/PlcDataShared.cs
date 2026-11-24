using System;
using System.Threading.Tasks;
using S7.Net;

namespace giaodien
{
    public static class PlcDataShared
    {
        public static Plc myPLC1; // CPU 1512C-1 PN - IP: 192.168.0.8
        public static Plc myPLC2; // CPU 1511C-1 PN - IP: 192.168.0.5

        public static bool DaKetNoiPLC =>
            myPLC1 != null && myPLC1.IsConnected &&
            myPLC2 != null && myPLC2.IsConnected;

        // Trạng thái CPU thực tế đang RUN hay STOP trên TIA / PLCSIM
        public static bool PLC1_DangRun { get; private set; } = false;
        public static bool PLC2_DangRun { get; private set; } = false;

        private static int giayPlc1Cu = -1;
        private static DateTime mocThoiGianGiayThayDoi = DateTime.Now;
        private static DateTime mocThoiGianKetNoi = DateTime.MinValue;
        private static int demLoiDocPlc1 = 0;
        private static int demLoiDocPlc2 = 0;

        public static void ResetTrangThaiVeMacDinh()
        {
            PLC1_DangRun = false;
            PLC2_DangRun = false;

            M_HeThong_Run = false;
            M_CheDo_Manual = false;
            M_Loi_KetQuyTrinhU_T1 = false;
            M_DenGio_ThuTrung = false;
            M_DenGio_CapPhan = false;
            M_DenGio_CapAnUong = false;
            M_KTO011_Active = false;
            M_TestChuKy_Active = false;
            M_HMI_KTO011 = false;
            M_Sim_Loi_T1_Active = false;

            Q_GatCam_XaNuoc = false;
            Q_HeThongSuoi = false;
            Q_QuatThongGio = false;
            Q_DenChieuSang = false;
            Q_BangTaiPhan = false;
            Q_BangTaiTrung = false;
            Q_KeoCam_CapNuoc = false;

            guidi_0_BatPlc2TuXa = 0;
            guidi_1_DangCapPhan = 0;
            guidi_2_KhoiLuongGui = 0;
            guiden_plc1_0_BaoDay = 0;

            M_T2_ChoPhepChay = false;
            M_T2_HamDay = false;
            TrangThai_CongDoan_T2 = 0;
            M_Loi_KetQuyTrinhU_T2 = false;
            M_Sim_Loi_T2_Active = false;
            Q_Bom_ViSinh = false;
            Q_Motor_DaoPhan = false;
            Q_Quat_DienTroSay = false;
            Q_BangTai_DongBao = false;
        }

        public static void DongKetNoiPLC()
        {
            try { if (myPLC1 != null && myPLC1.IsConnected) myPLC1.Close(); } catch { }
            try { if (myPLC2 != null && myPLC2.IsConnected) myPLC2.Close(); } catch { }
            myPLC1 = null;
            myPLC2 = null;
            demLoiDocPlc1 = 0;
            demLoiDocPlc2 = 0;
            giayPlc1Cu = -1;
            ResetTrangThaiVeMacDinh();
        }

        // === BIẾN TRUYỀN NHẬN GIỮA 2 PLC (Data_1 / Data_2) ===
        public static int guidi_0_BatPlc2TuXa = 0;   // DB1.DBW0
        public static int guidi_1_DangCapPhan = 0;    // DB1.DBW2
        public static int guidi_2_KhoiLuongGui = 0;   // DB1.DBW4
        public static int guiden_plc1_0_BaoDay = 0;   // DB1.DBW12

        // === THỜI GIAN THỰC ĐỌC TỪ PLC 1 (RD_LOC_T -> %MW40, %MW42, %MW44) ===
        public static int PLC_Gio = 0;                // %MW40
        public static int PLC_Phut = 0;               // %MW42
        public static int PLC_Giay = 0;               // %MW44

        // === BIẾN TRẠNG THÁI, INPUT/HMI & NGÕ RA TRẠM 1 ===
        public static bool M_HeThong_Run = false;        // %M10.0
        public static bool M_CheDo_Manual = false;       // %M10.1
        public static bool M_Loi_KetQuyTrinhU_T1 = false;// %M10.2
        public static bool M_DenGio_ThuTrung = false;    // %M10.4
        public static bool M_DenGio_CapPhan = false;     // %M10.5
        public static bool M_DenGio_CapAnUong = false;   // %M10.6
        public static bool M_KTO011_Active = false;      // %M11.4

        public static bool M_TestChuKy_Active = false;   // %M1.2
        public static bool M_HMI_KTO011 = false;         // %M1.4
        public static bool M_Sim_Loi_T1_Active = false;  // %M0.7

        public static bool Q_GatCam_XaNuoc = false;      // %Q0.0
        public static bool Q_HeThongSuoi = false;        // %Q0.1
        public static bool Q_QuatThongGio = false;       // %Q0.2
        public static bool Q_DenChieuSang = false;       // %Q0.3
        public static bool Q_BangTaiPhan = false;        // %Q0.4
        public static bool Q_BangTaiTrung = false;       // %Q0.5
        public static bool Q_KeoCam_CapNuoc = false;     // %Q0.6

        // === TOÀN BỘ BIẾN MW TRÊN PLC 1 ===
        public static int TongTrung_DaThu = 0;           // %MW20
        public static int TongSoKhayTrung = 0;         // %MW22
        public static int SoTrung_TrongKhay = 0;         // %MW24
        public static int Ret_RD_LOC_T = 0;              // %MW26
        public static int Thuong_ChiaGio_Phan = 0;       // %MW28
        public static int Thuong_ChiaGio_An = 0;         // %MW30
        public static int Thuong_BatQuat_Winter = 0;     // %MW32

        // === BIẾN TRẠNG THÁI & TOÀN BỘ BIẾN MW TRÊN PLC 2 ===
        public static bool M_T2_ChoPhepChay = false;     // %M20.0
        public static bool M_T2_HamDay = false;          // %M20.1
        public static int TrangThai_CongDoan_T2 = 0;     // 0=Chờ/Nhận, 1=Ủ, 2=Sấy, 3=Đóng bao
        public static bool M_Loi_KetQuyTrinhU_T2 = false;// %M20.5
        public static bool M_Sim_Loi_T2_Active = false;  // %M21.2
        public static bool Q_Bom_ViSinh = false;         // %Q0.0
        public static bool Q_Motor_DaoPhan = false;      // %Q0.1
        public static bool Q_Quat_DienTroSay = false;    // %Q0.2
        public static bool Q_BangTai_DongBao = false;    // %Q0.3

        public static int KhoiLuong_TrongHam = 0;        // %MW30
        public static int NhietDo_UHoai = 0;             // %MW32
        public static int DoAm_Say = 60;                 // %MW34
        public static int KhoiLuong_Loadcell = 0;        // %MW36
        public static int SoBao_DaDong = 0;              // %MW38

        public static string KetNoiHaiPLC()
        {
            try
            {
                // Luôn đóng socket cũ trước để tránh kẹt kết nối ảo khi PLCSIM vừa khởi động lại
                DongKetNoiPLC();

                myPLC1 = new Plc(CpuType.S71500, "192.168.0.8", 0, 1);
                myPLC1.ReadTimeout = 1500;
                myPLC1.WriteTimeout = 1500;
                myPLC1.Open();

                myPLC2 = new Plc(CpuType.S71500, "192.168.0.5", 0, 1);
                myPLC2.ReadTimeout = 1500;
                myPLC2.WriteTimeout = 1500;
                myPLC2.Open();

                if (DaKetNoiPLC)
                {
                    mocThoiGianKetNoi = DateTime.Now;
                    mocThoiGianGiayThayDoi = DateTime.Now;
                    giayPlc1Cu = -1;
                    PLC1_DangRun = true;
                    PLC2_DangRun = true;

                    DocDuLieuPLC1();
                    DocDuLieuPLC2();
                    return "OK";
                }

                DongKetNoiPLC();
                return "Không thể mở kết nối tới một trong hai PLC.";
            }
            catch (Exception ex)
            {
                DongKetNoiPLC();
                return ex.Message;
            }
        }

        private static short DocInt16AnToan(Plc plc, DataType dt, int db, int startByte)
        {
            object raw = plc.Read(dt, db, startByte, VarType.Int, 1);
            if (raw == null) return 0;
            ushort uVal = Convert.ToUInt16(raw);
            return unchecked((short)uVal);
        }

        public static void GhiMW_PLC1(int byteAddr, short value)
        {
            if (myPLC1 != null && myPLC1.IsConnected)
            {
                try
                {
                    myPLC1.Write(DataType.Memory, 0, byteAddr, unchecked((ushort)value));
                }
                catch { DongKetNoiPLC(); }
            }
        }

        public static void GhiMW_PLC2(int byteAddr, short value)
        {
            if (myPLC2 != null && myPLC2.IsConnected)
            {
                try
                {
                    myPLC2.Write(DataType.Memory, 0, byteAddr, unchecked((ushort)value));
                }
                catch { DongKetNoiPLC(); }
            }
        }

        public static void GhiBitPLC1(int byteAddr, int bitAddr, bool value)
        {
            if (myPLC1 != null && myPLC1.IsConnected)
            {
                try { myPLC1.WriteBit(DataType.Memory, 0, byteAddr, bitAddr, value); }
                catch { DongKetNoiPLC(); }
            }
        }

        public static void GhiBitPLC2(int byteAddr, int bitAddr, bool value)
        {
            if (myPLC2 != null && myPLC2.IsConnected)
            {
                try { myPLC2.WriteBit(DataType.Memory, 0, byteAddr, bitAddr, value); }
                catch { DongKetNoiPLC(); }
            }
        }

        public static async void XungBitPLC1(int byteAddr, int bitAddr)
        {
            if (myPLC1 != null && myPLC1.IsConnected)
            {
                try
                {
                    myPLC1.WriteBit(DataType.Memory, 0, byteAddr, bitAddr, true);
                    await Task.Delay(200);
                    if (myPLC1 != null && myPLC1.IsConnected)
                        myPLC1.WriteBit(DataType.Memory, 0, byteAddr, bitAddr, false);
                }
                catch { DongKetNoiPLC(); }
            }
        }

        public static async void XungBitPLC2(int byteAddr, int bitAddr)
        {
            if (myPLC2 != null && myPLC2.IsConnected)
            {
                try
                {
                    myPLC2.WriteBit(DataType.Memory, 0, byteAddr, bitAddr, true);
                    await Task.Delay(200);
                    if (myPLC2 != null && myPLC2.IsConnected)
                        myPLC2.WriteBit(DataType.Memory, 0, byteAddr, bitAddr, false);
                }
                catch { DongKetNoiPLC(); }
            }
        }

        public static void DocDuLieuPLC1()
        {
            if (myPLC1 == null || !myPLC1.IsConnected)
            {
                DongKetNoiPLC();
                return;
            }

            try
            {
                byte qb0 = Convert.ToByte(myPLC1.Read(DataType.Output, 0, 0, VarType.Byte, 1));
                Q_GatCam_XaNuoc = (qb0 & (1 << 0)) != 0;
                Q_HeThongSuoi = (qb0 & (1 << 1)) != 0;
                Q_QuatThongGio = (qb0 & (1 << 2)) != 0;
                Q_DenChieuSang = (qb0 & (1 << 3)) != 0;
                Q_BangTaiPhan = (qb0 & (1 << 4)) != 0;
                Q_BangTaiTrung = (qb0 & (1 << 5)) != 0;
                Q_KeoCam_CapNuoc = (qb0 & (1 << 6)) != 0;

                byte mb0 = Convert.ToByte(myPLC1.Read(DataType.Memory, 0, 0, VarType.Byte, 1));
                M_Sim_Loi_T1_Active = (mb0 & (1 << 7)) != 0;

                byte mb1 = Convert.ToByte(myPLC1.Read(DataType.Memory, 0, 1, VarType.Byte, 1));
                M_TestChuKy_Active = (mb1 & (1 << 2)) != 0;
                M_HMI_KTO011 = (mb1 & (1 << 4)) != 0;

                byte mb10 = Convert.ToByte(myPLC1.Read(DataType.Memory, 0, 10, VarType.Byte, 1));
                M_HeThong_Run = (mb10 & (1 << 0)) != 0;
                M_CheDo_Manual = (mb10 & (1 << 1)) != 0;
                M_Loi_KetQuyTrinhU_T1 = (mb10 & (1 << 2)) != 0;
                M_DenGio_ThuTrung = (mb10 & (1 << 4)) != 0;
                M_DenGio_CapPhan = (mb10 & (1 << 5)) != 0;
                M_DenGio_CapAnUong = (mb10 & (1 << 6)) != 0;

                byte mb11 = Convert.ToByte(myPLC1.Read(DataType.Memory, 0, 11, VarType.Byte, 1));
                M_KTO011_Active = (mb11 & (1 << 4)) != 0;

                TongTrung_DaThu = DocInt16AnToan(myPLC1, DataType.Memory, 0, 20);
                TongSoKhayTrung = DocInt16AnToan(myPLC1, DataType.Memory, 0, 22);
                SoTrung_TrongKhay = DocInt16AnToan(myPLC1, DataType.Memory, 0, 24);
                Ret_RD_LOC_T = DocInt16AnToan(myPLC1, DataType.Memory, 0, 26);
                Thuong_ChiaGio_Phan = DocInt16AnToan(myPLC1, DataType.Memory, 0, 28);
                Thuong_ChiaGio_An = DocInt16AnToan(myPLC1, DataType.Memory, 0, 30);
                Thuong_BatQuat_Winter = DocInt16AnToan(myPLC1, DataType.Memory, 0, 32);

                PLC_Gio = DocInt16AnToan(myPLC1, DataType.Memory, 0, 40);
                PLC_Phut = DocInt16AnToan(myPLC1, DataType.Memory, 0, 42);
                PLC_Giay = DocInt16AnToan(myPLC1, DataType.Memory, 0, 44);

                guidi_0_BatPlc2TuXa = DocInt16AnToan(myPLC1, DataType.DataBlock, 1, 0);
                guidi_1_DangCapPhan = DocInt16AnToan(myPLC1, DataType.DataBlock, 1, 2);
                guidi_2_KhoiLuongGui = DocInt16AnToan(myPLC1, DataType.DataBlock, 1, 4);
                guiden_plc1_0_BaoDay = DocInt16AnToan(myPLC1, DataType.DataBlock, 1, 12);

                // Giám sát nhịp sống CPU 1 thông qua giây đồng hồ PLC (%MW44)
                if (giayPlc1Cu == -1)
                {
                    giayPlc1Cu = PLC_Giay;
                    mocThoiGianGiayThayDoi = DateTime.Now;
                }
                else if (PLC_Giay != giayPlc1Cu)
                {
                    giayPlc1Cu = PLC_Giay;
                    mocThoiGianGiayThayDoi = DateTime.Now;
                    PLC1_DangRun = true;
                }
                else if ((DateTime.Now - mocThoiGianKetNoi).TotalSeconds > 2.5 &&
                         (DateTime.Now - mocThoiGianGiayThayDoi).TotalSeconds > 2.5)
                {
                    // Quá 2.5 giây mà %MW44 không nhảy -> CPU 1 đang STOP trên TIA / PLCSIM
                    PLC1_DangRun = false;
                    M_HeThong_Run = false;
                    Q_GatCam_XaNuoc = false;
                    Q_HeThongSuoi = false;
                    Q_QuatThongGio = false;
                    Q_DenChieuSang = false;
                    Q_BangTaiPhan = false;
                    Q_BangTaiTrung = false;
                    Q_KeoCam_CapNuoc = false;
                    guidi_0_BatPlc2TuXa = 0;
                    guidi_1_DangCapPhan = 0;
                    guidi_2_KhoiLuongGui = 0;
                }

                demLoiDocPlc1 = 0;
            }
            catch
            {
                demLoiDocPlc1++;
                if (demLoiDocPlc1 >= 2)
                {
                    DongKetNoiPLC();
                }
            }
        }

        public static void DocDuLieuPLC2()
        {
            if (myPLC2 == null || !myPLC2.IsConnected)
            {
                DongKetNoiPLC();
                return;
            }

            try
            {
                byte qb0 = Convert.ToByte(myPLC2.Read(DataType.Output, 0, 0, VarType.Byte, 1));
                Q_Bom_ViSinh = (qb0 & (1 << 0)) != 0;
                Q_Motor_DaoPhan = (qb0 & (1 << 1)) != 0;
                Q_Quat_DienTroSay = (qb0 & (1 << 2)) != 0;
                Q_BangTai_DongBao = (qb0 & (1 << 3)) != 0;

                byte mb20 = Convert.ToByte(myPLC2.Read(DataType.Memory, 0, 20, VarType.Byte, 1));
                M_T2_ChoPhepChay = (mb20 & (1 << 0)) != 0;
                M_T2_HamDay = (mb20 & (1 << 1)) != 0;
                bool dangU = (mb20 & (1 << 2)) != 0;
                bool dangSay = (mb20 & (1 << 3)) != 0;
                bool dangDongBao = (mb20 & (1 << 4)) != 0;
                M_Loi_KetQuyTrinhU_T2 = (mb20 & (1 << 5)) != 0;

                byte mb21 = Convert.ToByte(myPLC2.Read(DataType.Memory, 0, 21, VarType.Byte, 1));
                M_Sim_Loi_T2_Active = (mb21 & (1 << 2)) != 0;

                if (dangDongBao) TrangThai_CongDoan_T2 = 3;
                else if (dangSay) TrangThai_CongDoan_T2 = 2;
                else if (dangU) TrangThai_CongDoan_T2 = 1;
                else TrangThai_CongDoan_T2 = 0;

                KhoiLuong_TrongHam = DocInt16AnToan(myPLC2, DataType.Memory, 0, 30);
                NhietDo_UHoai = DocInt16AnToan(myPLC2, DataType.Memory, 0, 32);
                DoAm_Say = DocInt16AnToan(myPLC2, DataType.Memory, 0, 34);
                KhoiLuong_Loadcell = DocInt16AnToan(myPLC2, DataType.Memory, 0, 36);
                SoBao_DaDong = DocInt16AnToan(myPLC2, DataType.Memory, 0, 38);

                guidi_0_BatPlc2TuXa = DocInt16AnToan(myPLC2, DataType.DataBlock, 1, 0);
                guidi_1_DangCapPhan = DocInt16AnToan(myPLC2, DataType.DataBlock, 1, 2);
                guidi_2_KhoiLuongGui = DocInt16AnToan(myPLC2, DataType.DataBlock, 1, 4);
                guiden_plc1_0_BaoDay = DocInt16AnToan(myPLC2, DataType.DataBlock, 1, 12);

                // Nếu PLC 1 đã STOP trên TIA/PLCSIM hoặc PLC 1 lệnh bật mà PLC 2 không phản hồi -> CPU 2 đang STOP
                if (!PLC1_DangRun)
                {
                    PLC2_DangRun = false;
                    M_T2_ChoPhepChay = false;
                    TrangThai_CongDoan_T2 = 0;
                }
                else
                {
                    PLC2_DangRun = true;
                }

                demLoiDocPlc2 = 0;
            }
            catch
            {
                demLoiDocPlc2++;
                if (demLoiDocPlc2 >= 2)
                {
                    DongKetNoiPLC();
                }
            }
        }

        public static string LayChuoiThoiGianPLC()
        {
            if (DaKetNoiPLC)
                return $"{PLC_Gio:00}:{PLC_Phut:00}:{PLC_Giay:00} (PLC)";
            return ".........";
        }
    }
}