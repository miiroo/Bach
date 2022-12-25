using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;

namespace Baka {
    public partial class Form1 : Form {

        private List<string> vulnerability = new List<string>();
        private static List<string> logStrings = new List<string>();
        private List<string> logs = new List<string>();
        private List<Task> tasks = new List<Task>();
        private bool isPaused = false;
        private bool foundIssue = false;
        private bool isFinished = false;
        private int completed = 0;
        private int completedForBar = 0;
        public Form1() {
            InitializeComponent();
        }
#region Work_with_Files
        private void loadVulnerabilityToolStripMenuItem_Click(object sender, EventArgs e) {
            var filePath = string.Empty;
            listView1.Items.Clear();
            vulnerability.Clear();

            using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK) {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    //Read the contents of the file into a stream
                    var fileStream = openFileDialog.OpenFile();

                    using (StreamReader reader = new StreamReader(fileStream)) {
                        MessageBox.Show("File "+ openFileDialog.FileName + " is loaded");
                        while (!reader.EndOfStream)
                            vulnerability.Add(reader.ReadLine());
                    }
                }
            }

            for (int i =0; i<vulnerability.Count-1; i++) { 
                listView1.Items.Add(vulnerability[i]);
            }
        }
        private void saveLogsToolStripMenuItem_Click(object sender, EventArgs e) {

            using (SaveFileDialog saveFileDialog = new SaveFileDialog()) {
                StreamWriter myStream;
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
                    if ((myStream = new StreamWriter(saveFileDialog1.FileName)) != null) {
                        foreach (var log in listView2.Items) {
                            myStream.WriteLine(log);
                        }
                        myStream.Close();
                    }
                }
            }

        }

        private void loadLocalToolStripMenuItem_Click(object sender, EventArgs e) {
            logs.Clear();
            listView2.Items.Clear();
            isFinished = true;
            loadedFromInternet = false;
            Task.Delay(300);
            var filePath = string.Empty;
            logStrings.Clear();

            using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK) {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    //Read the contents of the file into a stream
                    var fileStream = openFileDialog.OpenFile();

                    using (StreamReader reader = new StreamReader(fileStream)) {
                        MessageBox.Show("File " + openFileDialog.FileName + " is loaded");
                        while (!reader.EndOfStream)
                            logStrings.Add(reader.ReadLine());
                    }

                }
            }
        }

        private void loadOnlineToolStripMenuItem_Click(object sender, EventArgs e) {
            listView2.Items.Clear();
            isFinished = true;
            Task.Delay(10000);
            isFinished = false;
            isPaused = false;
            Task.Run(() => loadFileFromInternet()); 
        }


        private bool loadedFromInternet = false;
        private static int lastString;

        public async void loadFileFromInternet() {
            lastString = 0;
            loadedFromInternet = true;
            logs.Clear();
            WebClient wc = new WebClient();
            wc.DownloadFileCompleted += new AsyncCompletedEventHandler(ReadFile);

            logStrings.Clear();
            lastListSize = 0;

            while (loadedFromInternet) {

                using (wc) {
                    wc.Headers.Add("a", "a");
                    try {
                       Uri uri = new Uri("https://raw.githubusercontent.com/miiroo/Bach/main/20221212_logs_3days_tab.txt");
                       wc.DownloadFileAsync(uri, @"./loggs.txt");
                    }
                    catch (Exception ex) {
                        Debug.WriteLine(ex.ToString());
                    }
                }

                await Task.Delay(5000);
            }

        }

        private static bool readed = false;
        public static void ReadFile(object sender, AsyncCompletedEventArgs e) {
            readed = false;
            var filePath = "./loggs.txt";
            var fileStream = File.OpenRead(filePath);
            Debug.WriteLine("File Downloaded and start reading");
            using (StreamReader reader = new StreamReader(fileStream)) {
                if (lastString == 0) {
                    while (!reader.EndOfStream) {
                        lastString++;
                        logStrings.Add(reader.ReadLine());
                    }
                }
                else {
                    int currentString = 0;
                    while (!reader.EndOfStream) {
                        if (currentString >= lastString) {
                            lastString++;
                            logStrings.Add(reader.ReadLine());
                        }
                        else {
                            reader.ReadLine();
                            currentString++;
                        }
                    }
                }
                readed = true;
                Debug.WriteLine("From reading file: "+logStrings.Count);
            }
            fileStream.Close();
        }

        #endregion

        private int lastListSize = 0;
        //Start search
        private async void button1_Click(object sender, EventArgs e) {
            //Check for empty files
            if (vulnerability.Count <= 0 || logStrings.Count <= 0) {
                MessageBox.Show("Please add vulnerabilities and file for check");
                return;
            }
            //Prepearing stuff before start searching
            if (tasks.Count != 0) tasks.Clear();

            isPaused = false;
            isFinished = false;
            completed = 0;
            completedForBar = 0;

            Task.Run(async () => onLogUpdate());
            //offline
            if (!loadedFromInternet) {
                Task.Run(async () => barUpdate());
                await Task.Run(() => {

                    logStrings.ForEach(x => {
                        tasks.Add(Task.Run(async () => {
                            await Task.Delay(100);
                            while (!isPaused) {
                                vulnerability.ForEach(y => {
                                    if (x.Contains(y)) {
                                        foundIssue = true;
                                        logs.Add("Vulnerability: " + y + " found in: " + x);
                                    }
                                });
                                completedForBar++;
                                break;
                            }
                        }));
                    });
                    Task.WaitAll(tasks.ToArray());

                    if (!foundIssue) {
                        logs.Add("No issue found");
                    }
                    Task.Delay(100);
                });
            }
            //online
            else {

                await Task.Run(async () => { 
                    
                    while (!isFinished) {
                        while (!readed) { }
                        if (!isPaused) {
                            Debug.WriteLine("From searching: "+logStrings.Count);
                            if (lastListSize < logStrings.Count) {
                                int count = logStrings.Count;
                                string x = "";
                                for (int i = lastListSize; i<count; i++) {
                                    x = logStrings[i].ToString();
                                    vulnerability.ForEach(y => {
                                        if (x.Contains(y)) {
                                            foundIssue = true;
                                            logs.Add("Vulnerability: " + y + " found in: " + x);
                                        }
                                    });
                                }
                                lastListSize = count; 
                            }
                        }
                        await Task.Delay(10000);
                    }
                });
            }
            Debug.WriteLine("Im done");
            isFinished = true;
            logStrings.Clear();
            ShowAll();
            logs.Clear();
        }

        //Bar update
        private async void barUpdate() {
            while (!isFinished) {
                UpdateBar();
            }
        }

 
        //Log updates
        private async void onLogUpdate() {
            while (!isFinished) {
                int count = logs.Count;
                if (completed < count) {
                    LogUpdate();
                    completed = count;
                }
            }
        }

        #region UI_update

        private void ShowAll() {
            listView2.Items.Clear();
            var count = logs.Count;
            for (int i = 0; i < count; i++) {
                listView2.Items.Add(logs[i]);
            }
        }
        private void LogUpdate() {
            if (this.InvokeRequired && this != null) {
                this.Invoke((Action)LogUpdate);
            }
            else {
                if (!isPaused && !isFinished)
                    ShowAll();
            }
        }
        private void UpdateBar() {
            if (this.InvokeRequired && this != null) {
                this.Invoke((Action)UpdateBar);
            }
            else {
                if (logStrings.Count > 0)
                    progressBar1.Value = (int)Math.Round(((float)(completedForBar) / (logStrings.Count)) * 100);
            }
        }
#endregion
        //Set pause
        private void button2_Click(object sender, EventArgs e) => isPaused = true;

        //Set unpause
        private void button4_Click(object sender, EventArgs e) => isPaused = false;

        //Clear logs
        private void button5_Click(object sender, EventArgs e) => listView2.Items.Clear();


    }
}