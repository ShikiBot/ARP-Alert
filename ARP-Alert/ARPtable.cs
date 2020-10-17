using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Data;

namespace ARP_Alert
{
    class ARPtable
    {
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestinationIP, int SourceIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);
        private DataTable dt;

        public ARPtable()
        {            
        }

        private string ConvertIpToMAC(IPAddress ip)
        {
            byte[] addr = new byte[6];
            int length = addr.Length;
            // TODO: Проверить, что результат - NO_ERROR
            SendARP(ip.GetHashCode(), 0, addr, ref length);
            //Преобразует базовые типы данных в массив байтов
            //и массив байтов в базовые типы данных
            return BitConverter.ToString(addr, 0, 6);
        }

        private string ConvertMacToName(string mac)
        {
            List<string> macList = LoadListFromFile("maclist.txt");
            string pattern = mac.Substring(0, 8) + ".*";
            foreach (var entry in macList)
            {
                Match found = Regex.Match(entry, pattern);
                if (found.Success)
                {
                    return found.Value.Split('|')[1];
                }
            }
            return "Unknown";
        }

        private static List<string> LoadListFromFile(string filename)
        {
            List<string> list = new List<string>();
            foreach (var x in File.ReadAllLines(filename))
                list.Add(x.Trim());
            return list;
        }

        public DataTable GetARP(string ip, string login, string password, int await)
        {
            string s;
            string[] ss;
            string[] sss;
            dt = new DataTable();
            dt.Columns.Add("Адрес в интернете");
            dt.Columns.Add("Физический адрес");
            dt.Columns.Add("Тип");
            IPAddress ipAD = IPAddress.Parse(ip);
            //получение названия компании, раазработавшей роутер
            string companyName = ConvertMacToName(ConvertIpToMAC(ipAD));            
            //подключение к телнету
            TelnetConnection telnet = new TelnetConnection(ip, 23);            
            //у разных компаний разный синтаксис команд
            switch (companyName)
            {
                case "Zyxel Communications Corporation":{
                        login = login == "" ? "admin" : login;
                        password = password == "" ? "admin" : password;
                        telnet.Login(login, password, await);
                        telnet.WriteLine("show ip arp");
                        s = telnet.Read();
                        ss = s.Split('\n');
                        for (int i = 4; i < ss.Length - 1; i++)
                        {
                            while (ss[i].Contains("  "))
                            {
                                ss[i] = ss[i].Replace("  ", " ");
                            }
                            sss = ss[i].Split(' ');
                            //MessageBox.Show(sss[1] + " " + sss[2].Replace(":", "-") + " " + ConvertMacToName(sss[2].Replace(":", "-").ToUpper())); 
                            dt.Rows.Add(new object[] { sss[1], sss[2], ConvertMacToName(sss[2].Replace(":", "-").ToUpper()) });

                        }}
                    break;
                case "zte corporation":
                    //если пользователь не задал логин и пароль
                    //используются стандартные значения
                    login = login == "" ? "root" : login;
                    password = password == "" ? "root" : password;
                    //авторизация на роутере
                    telnet.Login(login, password, await);
                    //команда или список команд для получения arp таблицы
                    telnet.WriteLine("cat proc/net/arp");
                    //чтение ответа
                    s = telnet.Read();
                    ss = s.Split('\n');
                    //перевод ответа в удобный для обработки формат
                    for (int i = 2; i < ss.Length - 1; i++)
                    {
                        while (ss[i].Contains("  "))
                        {
                            ss[i] = ss[i].Replace("  ", " ");
                        }
                        sss = ss[i].Split(' ');                        
                        if (!sss[0].Contains("172"))
                            dt.Rows.Add(new object[] { sss[0], sss[3], 
                                ConvertMacToName(sss[3].Replace(":", "-").ToUpper()) });
                    }
                    break;
                default:
                    return null;
            }
            telnet.Close(); //закрытие подключения с телнетом
            return dt;
        }
    }
}
