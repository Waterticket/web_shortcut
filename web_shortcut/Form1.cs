using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace web_shortcut
{
    public partial class Form1 : Form
    {
        WebServer ws;
        public static List<string> domains;
        ListViewItem.ListViewSubItem curSB;
        ListViewItem curItem;
        bool cancelEdit;
        string fileName = "savedata.txt";
        string ws_location = "127.231.201.203";
        public static Dictionary<string, string> redirect_table;
        bool _systemShutdown = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            listView1.MultiSelect = false;
            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            textBox1.Hide();
            domains = new List<string>();

            FileInfo fileInfo = new FileInfo(fileName);
            if(fileInfo.Exists)
            {
                LoadData(fileName);
                this.Opacity = 0;
                this.ShowInTaskbar = false;
                notifyIcon1.Visible = true;
            }
            else
            {
                ListViewItem item = new ListViewItem("ㄴㅇㅂ");
                item.SubItems.Add("https://www.naver.com");
                listView1.Items.Add(item);

                SaveData(fileName);
                LoadData(fileName);
            }

            ws = new WebServer(SendResponse, string.Format("http://{0}/", ws_location));
            ws.Run();
        }

        public static (Dictionary<string, string> headers, string response, int status) SendResponse(HttpListenerRequest request)
        {
            int status;
            string response = "<head><link rel=\"icon\" href=\"data:;base64,iVBORw0KGgo=\"></head><body>Loading..</body>";
            Dictionary<string, string> headers = new Dictionary<string, string>();
            IdnMapping idn = new IdnMapping();

            if (domains.Contains(request.UserHostName))
            {
                string real_domain = idn.GetUnicode(request.UserHostName);
                string return_url = redirect_table[real_domain];

                status = 307;
                response = "";
                headers.Add("Location", return_url);
                Debug.WriteLine("Target: "+real_domain);
            }
            else
            {
                status = 400;
                response = "<body>shortcut query failed.</body>";
            }

            return (headers, response, status);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            ws.Stop();
            SaveData(fileName);
            LoadData(fileName);
            ws = new WebServer(SendResponse, string.Format("http://{0}/", ws_location));
            ws.Run();
            Debug.WriteLine("Reloaded.");
        }

        public static void punyCodeConvert()
        {
            IdnMapping idn = new IdnMapping();
            List<string> res = new List<string>(domains);
            domains = new List<string>();
            foreach (var name in res)
            {
                try
                {
                    string punyCode = idn.GetAscii(name);
                    domains.Add(punyCode);
                }
                catch (ArgumentException)
                {
                    Debug.WriteLine("{0} is not a valid domain name.", name);
                }
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            curItem = listView1.GetItemAt(e.X, e.Y);
            if (curItem == null)
                return;

            curSB = curItem.GetSubItemAt(e.X, e.Y);
            int idxSub = curItem.SubItems.IndexOf(curSB);

            int lLeft = curSB.Bounds.Left + 2;
            int lWidth = curSB.Bounds.Width;
            textBox1.SetBounds(lLeft + listView1.Left, curSB.Bounds.Top + listView1.Top, lWidth, curSB.Bounds.Height);

            textBox1.Text = curSB.Text;
            textBox1.Show();
            textBox1.Focus();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.Enter:
                    cancelEdit = false;
                    e.Handled = true;
                    textBox1.Hide();
                    break;
                case System.Windows.Forms.Keys.Escape:
                    cancelEdit = true;
                    e.Handled = true;
                    textBox1.Hide();
                    break;
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            textBox1.Hide();
            if (cancelEdit == false)
            {
                if (textBox1.Text.Trim() != "")
                {
                    curSB.Text = textBox1.Text;

                    int idxSub = curItem.SubItems.IndexOf(curSB);
                    int idx = curItem.Index;

                    Debug.WriteLine(curSB.Text);  // Something To Do
                }
            }
            else
            {
                cancelEdit = false;
            }

            listView1.Focus();
        }

        private void addList_Click(object sender, EventArgs e)
        {
            ListViewItem item = new ListViewItem("(예약어 입력)");
            item.SubItems.Add("(URL 입력)");
            listView1.Items.Add(item);
        }

        private void removeList_Click(object sender, EventArgs e)
        {
            try
            {
                listView1.SelectedItems[0].Remove();
            }
            catch(Exception ex)
            { ex.ToString(); }
        }

        private void LoadData(string fileName)
        {
            IdnMapping idn = new IdnMapping();
            listView1.Items.Clear();
            listView1.Refresh();
            redirect_table = new Dictionary<string, string>();

            // StreamReader를 이용하여 문자판독기를 생성합니다.
            using (TextReader tReader = new StreamReader(fileName))
            {
                // 파일의 내용을 모두 읽어와 줄바꿈을 기준으로 배열형태로 쪼갭니다.
                string[] stringLines
                    = tReader.ReadToEnd().Replace("\n", "").Split((char)Keys.Enter);

                // 한줄씩 가져와서..
                foreach (string stringLine in stringLines)
                {
                    // 빈 문자열이 아니면..
                    if (stringLine != string.Empty)
                    {
                        // 구분자를 이용해서 배열형태로 쪼갭니다.
                        string[] stringArray = stringLine.Split(';');

                        // 아이템을 구성합니다.
                        ListViewItem item = new ListViewItem(stringArray[0]);
                        item.SubItems.Add(stringArray[1]);

                        // ListView에 아이템을 추가합니다.
                        listView1.Items.Add(item);

                        // 예약어 리스트에 추가
                        domains.Add(stringArray[0]);

                        // 전송 테이블에 추가
                        redirect_table.Add(idn.GetUnicode(idn.GetAscii(stringArray[0])), stringArray[1]);
                    }
                }
            }

            punyCodeConvert();
            hostEdit();
        }

        private void SaveData(string fileName)
        {
            // StreamWriter를 이용하여 문자작성기를 생성합니다.
            using (TextWriter tWriter = new StreamWriter(fileName))
            {
                // ListView의 Item을 하나씩 가져와서..
                foreach (ListViewItem item in listView1.Items)
                {
                    // 원하는 형태의 문자열로 한줄씩 기록합니다.
                    tWriter.WriteLine(string.Format("{0};{1}", item.Text, item.SubItems[1].Text));
                }
            }
            hostEdit();
        }

        public void hostEdit()
        {
            try
            {
                string modify_str = "";
                foreach (string item in domains)
                {
                    modify_str += string.Format("{0} {1} {2}", ws_location, item, System.Environment.NewLine);
                }

                FileStream fileStream = new FileStream(@"C:\Windows\System32\drivers\etc\hosts", FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
                string fileContents;
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    fileContents = reader.ReadToEnd();
                    reader.Close();
                }
                string replaceWith = "$NL$";
                string removedBreaks = fileContents.Replace("\r\n", replaceWith).Replace("\n", replaceWith).Replace("\r", replaceWith);
                string output = System.Text.RegularExpressions.Regex.Replace(removedBreaks, "##SHORT_HOST##(.*?)##SHORT_HOST_END##", string.Format("##SHORT_HOST##$NL${0}##SHORT_HOST_END##", modify_str));
                output = output.Replace(replaceWith, System.Environment.NewLine);
                fileStream.Close();

                fileStream = new FileStream(@"C:\Windows\System32\drivers\etc\hosts", FileMode.Create, FileAccess.Write, FileShare.Read);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                streamWriter.Write(output);
                streamWriter.Close();
                fileStream.Close();
            }catch(Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_systemShutdown) // 트레이 아이콘의 컨텍스트 메뉴를 통해 프로그램 종료가 선택된경우 true 
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
                this.Visible = false; // 화면을 닫지 않고 단지 숨길 뿐이다.
                notifyIcon1.Visible = true;
            }
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Tag != null && e.ClickedItem.Tag.ToString().Equals("EXIT"))
            {
                _systemShutdown = true;
                this.Close();
                this.Dispose();
                Properties.Settings.Default.Save();
                Application.Exit();
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Opacity = 1;
            this.ShowInTaskbar = true;
            this.Visible = true;
            notifyIcon1.Visible = false;
        }
    }
}
