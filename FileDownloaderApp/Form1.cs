using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Threading;
using System.IO;

namespace FileDownloaderApp
{
    public partial class Form1 : Form
    {
        private WebClient webClient;
        private Thread downloadThread;
        private bool isPaused = false;
        private static TextBox tbUrl;
        private static TextBox tbPath;
        private static TextBox tbTag;
        private static TextBox tbSearch;
        private static NumericUpDown num;
        private static ListBox lB;

        public Form1()
        {
            InitializeComponent();

            tbUrl = textBox1;
            tbPath = textBox2;
            tbTag = textBox3;
            num = numericUpDown1;
            lB = listBox1;
            tbSearch = textBox4;

            UpdateFileList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (webClient != null && webClient.IsBusy)
            {
                MessageBox.Show("Загрузка уже выполняется.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string url = tbUrl.Text;
            string savePath = tbPath.Text;
            int threadCount = (int)num.Value;
            string tags = tbTag.Text;
            string fileName = Path.GetFileName(new Uri(url).LocalPath);

            webClient = new WebClient();
            downloadThread = new Thread(() => DownloadFile(url, savePath, threadCount, tags));
            downloadThread.Start();
        }

        private void DownloadFile(string url, string savePath, int threadCount, string tags)
        {
            try
            {
                Uri uri = new Uri(url);
                string fileName = Path.GetFileName(uri.LocalPath);

                if (!string.IsNullOrEmpty(tags))
                {
                    string sanitizedTags = string.Join("_", tags.Split(Path.GetInvalidFileNameChars()));
                    fileName = $"{Path.GetFileNameWithoutExtension(fileName)}[{sanitizedTags}]{Path.GetExtension(fileName)}";
                }

                webClient = new WebClient();
                webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;

                webClient.DownloadFileAsync(uri, Path.Combine(savePath, fileName), Tuple.Create(savePath, fileName));

                isPaused = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при скачивании файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                progressBar1.Value = e.ProgressPercentage;
            });
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                if (e.Cancelled)
                {
                    MessageBox.Show("Скачивание отменено.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (e.Error != null)
                {
                    MessageBox.Show($"Ошибка при скачивании файла: {e.Error.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Скачивание завершено успешно.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                UpdateFileList();
            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (webClient != null && webClient.IsBusy)
            {
                if (!isPaused)
                {
                    button2.Text = "Resume";
                    webClient.CancelAsync();
                    isPaused = true;
                    MessageBox.Show("Скачивание приостановлено.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    button2.Text = "Pause";
                    DownloadFile(tbUrl.Text, tbPath.Text, (int)num.Value, tbTag.Text);
                }
            }
            else
            {
                MessageBox.Show("Нет активного скачивания для приостановки.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (webClient != null)
            {
                webClient.CancelAsync();
                isPaused = false;
            }

            UpdateFileList();
            progressBar1.Value = 0;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (lB.SelectedItem != null)
            {
                string selectedFile = lB.SelectedItem.ToString();
                string fullPath = Path.Combine(tbPath.Text, selectedFile);

                try
                {
                    File.Delete(fullPath);
                    MessageBox.Show($"Файл {selectedFile} удален.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    UpdateFileList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите файл для удаления.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            progressBar1.Value = 0;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (webClient != null)
            {
                webClient.CancelAsync();
                isPaused = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            UpdateFileList();
        }

        private void UpdateFileList()
        {
            string savePath = tbPath.Text;

            if (Directory.Exists(savePath))
            {
                lB.Items.Clear();

                string[] files = Directory.GetFiles(savePath);
                foreach (string file in files)
                {
                    lB.Items.Add(Path.GetFileName(file));
                }
            }
            else
            {
                lB.Items.Clear();
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            string searchTerm = tbSearch.Text.ToLower();

            var filteredFiles = Directory.GetFiles(tbPath.Text)
                .Where(file => Path.GetFileName(file).ToLower().Contains(searchTerm))
                .ToArray();

            lB.Items.Clear();
            lB.Items.AddRange(filteredFiles.Select(file => Path.GetFileName(file)).ToArray());
        }
    }
}
