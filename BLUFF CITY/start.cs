using System;
using System.Net.Sockets;
using System.Windows.Forms;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System.Net;
using System.Text;
using System.Threading;

namespace BLUFF_CITY
{
    public partial class start : Form
    {
        private Socket socket; //����
        private TcpClient client;
        private Thread receiveThread;

        public start()
        {
            InitializeComponent();

            InitializeArrays();

            ApplyTransparentBackgroundAndHideBorder();

        }

        private void LOGIN_Click(object sender, EventArgs e)
        {
            login loginForm = new login();
            loginForm.Show();

            this.Hide();
        }

        private void SIGN_UP_Click(object sender, EventArgs e)
        {
            SignUp SignUpForm = new SignUp();
            SignUpForm.Show();
        }

        private void start_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void ApplyTransparentBackgroundAndHideBorder()
        {
            // StartButtons �迭�� ���� ����� �����ϰ� �����ϰ� �������� ����
            foreach (var button in StartButtons)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = Color.Transparent;
                button.FlatAppearance.BorderSize = 0;
            }

            // StartTextBox �迭�� ���� ����� �����ϰ� ����
            foreach (var textBox in StartTextBox)
            {
                textBox.BorderStyle = BorderStyle.None; // �ؽ�Ʈ �ڽ� �׵θ� �����
            }
        }

        private void exit_Click(object sender, EventArgs e)
        {
            try
            {
                // ���� IP �ּҿ� ��Ʈ ��ȣ�� �����մϴ�.
                IPAddress serverIP = IPAddress.Parse("127.0.0.1");
                int port = 5252;
                // TCP/IP ������ �����ϰ� ������ �����մϴ�.
                client = new TcpClient();
                client.Connect(serverIP, port);
                // ������ ����� ���, ���� �����带 �����մϴ�.
                //receiveThread = new Thread(new ThreadStart(Receive));
                //receiveThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
