using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Linq;
using System.Windows.Media;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ServiceProcess;
using System.Threading;
using System.Xml;
using Newtonsoft.Json;


namespace ExeUIWPF
{
    public class ComboData
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
    }

    public partial class MainWindow : Window
    {
        List<ComboData> ListData = new List<ComboData>();
        List<ComboData> ServiceListData = new List<ComboData>();

        public MainWindow()
        {
            InitializeComponent();

            if(File.Exists(@"Data.txt"))
            {
                ListData = JsonConvert.DeserializeObject<List<ComboData>>(File.ReadAllText(@"Data.txt")).Where(x => x.Type == "Directory").ToList<ComboData>();
                ServiceListData = JsonConvert.DeserializeObject<List<ComboData>>(File.ReadAllText(@"Data.txt")).Where(x => x.Type == "Service").ToList<ComboData>();
            }
            comboBox.ItemsSource = ListData;
            comboBox.DisplayMemberPath = "Id";
            comboBox.SelectedValuePath = "Value";
            comboBoxServices.DisplayMemberPath = comboBoxServices.SelectedValuePath = "DisplayName";
            
            comboBoxServices.ItemsSource = ServiceController.GetServices();
            Thread backgroundUpdate = new Thread(backgroundProcess) { IsBackground = false };
            backgroundUpdate.Start();            
        }

        private void saveData_Click(object sender, RoutedEventArgs e)
        {
            var json = JsonConvert.SerializeObject(ListData.Concat(ServiceListData));

            File.WriteAllText(@"Data.txt", json);
        }

        private void backgroundProcess()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            while (true)
            {
                Dispatcher.Invoke(() =>
                {
                    cpuConsumption.Content = "CPU: " + cpuCounter.NextValue() + " %";
                    ramConsumption.Content = "RAM: " + ramCounter.NextValue() + "MB";
                    listView.Items.Clear();
                    foreach (ServiceController service in ServiceController.GetServices())
                    {
                        if (ServiceListData.Where(x => x.Value == service.DisplayName).ToList<ComboData>().Count != 0)
                        {
                            listView.Items.Add(service.DisplayName + " : " + service.Status);
                        }
                    }
                    listView.Items.Refresh();
                });
                Thread.Sleep(500);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();

            Nullable<bool> result = openFileDlg.ShowDialog();

            if (result == true)
            {
                textBoxPath.Text = openFileDlg.FileName;
                Regex re = new Regex(@"(([ \w]+(?=$))|([ \w]+(?=\.)))");
                Match m = re.Match(textBoxPath.Text);
                textBoxName.Text = m.Groups[0].Value.ToUpper();
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            string curFile = textBoxPath.Text;
            if (File.Exists(curFile))
            {
                try
                {
                    Process.Start(textBoxPath.Text);
                    textBoxPath.Text = textBoxName.Text = "";
                }
                catch(Exception ex)
                {
                    label.Content = ex.Message;
                    label.Background = Brushes.Red;
                    label.Foreground = Brushes.White;
                }
            }
            else if (Directory.Exists(curFile))
            {
                Process.Start("explorer.exe", curFile);
            }
            else
            {
                label.Content = "Invalid directory or file";
                label.Background = Brushes.Red;
                label.Foreground = Brushes.White;
            }
        }       

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxPath.Text != "" && textBoxName.Text != "")
            {
                if (ListData.Where(x => x.Value == textBoxName.Text).ToList<ComboData>().Count == 0)
                {
                    ListData.Add(new ComboData { Id = textBoxName.Text, Value = textBoxPath.Text, Type = "Directory" });
                    comboBox.ItemsSource = ListData;
                    comboBox.Items.Refresh();
                    label.Content = "Record Added Sucessfully";
                    textBoxPath.Text = "";
                    textBoxName.Text = "";
                    label.Background = Brushes.Green;
                    label.Foreground = Brushes.White;
                }
                else
                {
                    label.Content = "Record Already Exist";
                    label.Background = Brushes.Red;
                    label.Foreground = Brushes.White;
                }

            }
            else
            {
                label.Content = "Please Enter Name and Path";
                label.Background = Brushes.Red;
                label.Foreground = Brushes.White;
            }
        }

        private void comboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            textBoxPath.Text = Convert.ToString(comboBox.SelectedValue);
            textBoxName.Text = Convert.ToString(comboBox.SelectedItem);
        }

        private void folderBrowser_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new CommonOpenFileDialog
            {
                EnsurePathExists = true,
                IsFolderPicker = true,
                EnsureFileExists = false,
                AllowNonFileSystemItems = false,
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                textBoxPath.Text = Directory.Exists(dialog.FileName) ? dialog.FileName : Path.GetDirectoryName(dialog.FileName);

            Regex re = new Regex(@"(([ \w]+(?=$))|([ \w]+(?=\.)))");
            Match m = re.Match(textBoxPath.Text);
            textBoxName.Text = m.Groups[0].Value.ToUpper() + " (Directory)";

        }

        private void textBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
        private void listView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void comboBoxServices_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ServiceListData.Add(new ComboData() { Id = comboBoxServices.SelectedItem.ToString(), Value = comboBoxServices.SelectedValue.ToString(), Type = "Service"});
        }       
    }
}
