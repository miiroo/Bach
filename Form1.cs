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
using System.Diagnostics;

namespace Baka {
    public partial class Form1 : Form {

        private List<string> vulnerability = new List<string>();
        private List<string> logStrings = new List<string>();
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
        private void loadFileToolStripMenuItem_Click(object sender, EventArgs e) {
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
        private void saveLogsToolStripMenuItem_Click(object sender, EventArgs e) {

            using (SaveFileDialog saveFileDialog = new SaveFileDialog()) {
                StreamWriter myStream;
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
                    if ((myStream = new StreamWriter(saveFileDialog1.FileName)) != null) {
                        foreach (var log in logs) {
                            myStream.WriteLine(log);
                        }
                        myStream.Close();
                    }
                }
            }

        }
#endregion

        //Start search
        private async void button1_Click(object sender, EventArgs e) {
            //Check for empty files
            if (vulnerability.Count <= 0 || logStrings.Count <= 0) {
                MessageBox.Show("Please add vulnerabilities and file for check");
                return;
            }
            //Prepearing stuff before start searching
            if (tasks.Count != 0) tasks.Clear();

            isFinished = false;
            completed = 0;
            completedForBar = 0;

            Task.Run(async () => onLogUpdate());
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
                if (completed < logs.Count) {
                    LogUpdate();
                    completed = logs.Count;
                }
            }
        }

#region UI_update

        private void ShowAll() {
            foreach (var log in logs) 
                listView2.Items.Add(log);
        }
        private void LogUpdate() {
            if (this.InvokeRequired && this != null) {
                this.Invoke((Action)LogUpdate);
            }
            else {
                listView2.Items.Add(logs.Last());
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