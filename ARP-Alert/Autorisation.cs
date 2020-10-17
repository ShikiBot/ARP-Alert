using System;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ARP_Alert
{
    public partial class Autorisation : Form
    {
        StreamWriter sw;        

        public Autorisation()
        {
            InitializeComponent();            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "") MessageBox.Show("Введите IP-адрес!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            else
            {
                Program.Data.IP = textBox1.Text;
                Program.Data.Login = maskedTextBox2.Text;
                Program.Data.Password = maskedTextBox3.Text;
                Program.Data.Mail = maskedTextBox4.Text;
                char[] wr = $"{Program.Data.IP}/{Program.Data.Login}/{Program.Data.Password}/{Program.Data.Mail}".ToCharArray();
                sw.Write(wr, 0, wr.Length);
                this.Close();
            }
        }

        private void Autorisation_Load(object sender, EventArgs e)
        {
            using (StreamWriter sr = new StreamWriter("Trust.mac", false)) { sr.Close(); }
            label7.Text = ("Для начала укажите почтовый адрес на который приложению\nследует отсылать оповещения о вторжениях:");
            sw = new StreamWriter("User.txt", false, Encoding.Default);
        }

        private void Autorisation_FormClosing(object sender, FormClosingEventArgs e)
        {
            sw.Close();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != 8 && e.KeyChar != 46)
                e.Handled = true;
        }
    }
}
