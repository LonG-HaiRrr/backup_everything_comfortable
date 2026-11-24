using System;
using System.Drawing;
using System.Windows.Forms;

namespace giaodien
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            chkBatPlc2TuXa.CheckedChanged += ChkBatPlc2TuXa_CheckedChanged;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            chkBatPlc2TuXa.Checked = (PlcDataShared.guidi_0_BatPlc2TuXa == 1);
            CapNhatTrangThaiMain();
        }

        private void ChkBatPlc2TuXa_CheckedChanged(object sender, EventArgs e)
        {
            PlcDataShared.guidi_0_BatPlc2TuXa = chkBatPlc2TuXa.Checked ? 1 : 0;
            CapNhatTrangThaiMain();
        }

        private void CapNhatTrangThaiMain()
        {
            if (PlcDataShared.guidi_0_BatPlc2TuXa == 1)
            {
                lblTrangThaiPlc2Main.Text = "Trạng thái PLC 2: ĐÃ BẬT TỪ XA (guidi[0] = 1)";
                lblTrangThaiPlc2Main.ForeColor = Color.DarkGreen;
            }
            else
            {
                lblTrangThaiPlc2Main.Text = "Trạng thái PLC 2: ĐANG TẮT (guidi[0] = 0)";
                lblTrangThaiPlc2Main.ForeColor = Color.Red;
            }
        }

        private void btnMoForm1_Click(object sender, EventArgs e)
        {
            Form1 f1 = new Form1();
            f1.Show();
            this.Hide();
        }

        private void btnMoForm2_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.Show();
            this.Hide();
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}