using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Drawing;

namespace ARP_Alert
{
    public partial class Form1 : Form
    {
        ARPtable arp;
        Shifr gost;
        const string key = "bIyfDAkxmQCE9qhliRYO1Mt0pXVwgcZ7";
        Autorisation au;
        List<string> OdobryayuList;
        List<string> AsuzhdayuList;
        SmtpClient smtp;
        MailAddress from;
        MailAddress to;        
        public Form1()
        {
            InitializeComponent();
            arp = new ARPtable();
            gost = new Shifr();
            OdobryayuList = new List<string>();
            AsuzhdayuList = new List<string>();
            smtp = new SmtpClient("smtp.mail.ru", 25);
            smtp.Credentials = new NetworkCredential("arp.alert@mail.ru", "trela.pra");
            smtp.EnableSsl = true;
            from = new MailAddress("arp.alert@mail.ru");
            trackBar1.Scroll += trackBar1_Scroll;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateTable(dataGridView1);
            to = new MailAddress(Program.Data.Mail);           
        }

        private void UpdateTable(DataGridView dgv)
        {
            int sum = dgv.Columns.Count;
            for (int i = 0; i < sum; i++) dgv.Columns.RemoveAt(0);
            dgv.DataSource = arp.GetARP(Program.Data.IP, Program.Data.Login, Program.Data.Password, trackBar1.Value);
            DataGridViewCheckBoxColumn column = new DataGridViewCheckBoxColumn();{ column.HeaderText = "Разрешение"; }
            dgv.Columns.Insert(3, column);
            CheckUpdate(dgv);
        }

        private void CheckUpdate(DataGridView dgv)
        {
            int rows = dgv.Rows.Count;
            List<string> s = new List<string>();
            for (int i = 0; i < rows; i++)
                foreach (string x in OdobryayuList)
                    if (x == dgv[1, i].Value+"") dgv[3, i].Value = true;           
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (Convert.ToBoolean(dataGridView1[e.ColumnIndex, e.RowIndex].Value) && !OdobryayuList.Contains(dataGridView1[1, e.RowIndex].Value.ToString()))
                OdobryayuList.Add(dataGridView1[1, e.RowIndex].Value.ToString());
            else if (!Convert.ToBoolean(dataGridView1[e.ColumnIndex, e.RowIndex].Value))
                OdobryayuList.Remove(dataGridView1[1, e.RowIndex].Value.ToString());
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty) dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string s;
            using (StreamReader sr = new StreamReader("User.txt"))
            {
                s = sr.ReadToEnd();
                sr.Close();
            }            
            au = new Autorisation();
            if (s == "") au.Show(this);
            else
            {
                Program.Data.IP = s.Split('/')[0];
                Program.Data.Login = s.Split('/')[1];
                Program.Data.Password = s.Split('/')[2];
                Program.Data.Mail = s.Split('/')[3];
            }
            using (StreamReader sr = new StreamReader("Trust.mac", Encoding.Default))
            {
                s = sr.ReadToEnd();
                sr.Close();
            }
            s = gost.Decrypt(s, key);
            OdobryayuList = s.Split('/').ToList();
            OdobryayuList.Remove("");

            if (OdobryayuList.Count > 0)
                OdobryayuList[OdobryayuList.Count - 1] = OdobryayuList[OdobryayuList.Count - 1].Remove(17);

            notifyIcon1.BalloonTipTitle = "Arp-Alert";
            notifyIcon1.BalloonTipText = "Arp-Alert минимизирован";
            notifyIcon1.Text = "Arp-Alert";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Interval = 60000;
            timer1.Enabled = true;
            UpdateTable(dataGridView1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            au = new Autorisation();
            au.Show(this);
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            string s = "";
            //обновление данных о подключенных устройствах
            UpdateTable(dataGridView1);
            int rows = dataGridView1.Rows.Count;
            for (int i = 0; i < rows; i++)
            {
                //если пользователь не утвердил подключеное устройство
                //и отчет о новом подключения еще не высылался на почту
                if (!Convert.ToBoolean(dataGridView1[3, i].Value) &&
                    !AsuzhdayuList.Contains(dataGridView1[1, i].Value.ToString()))
                {
                    //составление текста отчета
                    s += "Нессанкционированное подключение от " + dataGridView1[0, i].Value 
                        + " " + dataGridView1[1, i].Value + " " + dataGridView1[2, i].Value + "\n";
                    //запоминание информации, что по данному устройству уже отсылался отчет
                    AsuzhdayuList.Add(dataGridView1[1, i].Value.ToString());
                }
            }
            //если отчет сформирован, то его отсылает на почту
            if (s.Length > 0)
                await SendMessageAsync("Внимание, произошло несанкцианированное подключение", s);            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            string s = "";
            //составление содержимого файла 
            foreach (string x in OdobryayuList)                         
                s += x + "/";
            //доверенные устройства существуют
            if (s != "")
            {
                s = s.Remove(s.Length - 1);
                //шифрование данных
                s = gost.Encrypt(s, key);
                //запись зашифрованного списка
                using (StreamWriter sw = new StreamWriter("Trust.mac",
                    false, Encoding.Default))
                {
                    sw.Write(s);
                    sw.Close();
                }
            }            
        }

        public async Task SendMessageAsync(string title, string text)
        {
            MailMessage mail = new MailMessage(from, to);
            mail.IsBodyHtml = true;
            mail.Subject = title;
            mail.Body = text;
            await smtp.SendMailAsync(mail);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            notifyIcon1.Visible = false;
            WindowState = FormWindowState.Normal;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(1000);
            }
            else if (FormWindowState.Normal == this.WindowState)
            { notifyIcon1.Visible = false; }
        }

        private void закрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            this.Close();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            trackBar1.Location = new Point(this.Width - label1.Width - 34, 34);
            label1.Location = new Point(this.Width - label1.Width - 37, 9);
            textBox1.Location = new Point(this.Width - label1.Width + 156 , 34);
            label2.Location = new Point(9, this.Height - label2.Height - 60);
            button3.Location = new Point(this.Width - button3.Width - 28, this.Height - button3.Height - 52);
            dataGridView1.Height = this.Height - 185;
            dataGridView1.Width = this.Width - 40;
            this.Width = this.Width < 620 ? 620 : this.Width;
            this.Height = this.Height < 477 ? 477 : this.Height;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBox1.Text = trackBar1.Value.ToString();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                trackBar1.Value = Convert.ToInt32(textBox1.Text);
            }
            catch
            {
                MessageBox.Show("Некорректные данные", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
