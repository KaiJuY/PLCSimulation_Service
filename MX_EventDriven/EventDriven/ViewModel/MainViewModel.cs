using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using EventDriven.Model;
using System.Linq;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace EventDriven.ViewModel
{
    /// <summary>
    /// 先驗證功能，先不要用MVVM開發
    /// </summary>
    public class MainViewModel
    {
        private readonly Services.EventManager _eventManager;
        protected MainWindow _mainwindow;

        public MainViewModel(MainWindow mainwindow)
        {
            _eventManager = new Services.EventManager();
            _mainwindow = mainwindow;
        }

        public void BrowseJsonFile()
        {
            OpenFileDialog openFileDlg = new OpenFileDialog();

            // 可選：設置初始目錄
            openFileDlg.InitialDirectory = Environment.CurrentDirectory;

            // 可選：設置檔案過濾器
            openFileDlg.Filter = "JSON 檔案 (*.json)|*.json";

            // 顯示檔案選擇對話框
            Nullable<bool> result = openFileDlg.ShowDialog();

            // 處理結果
            if (result == true)
            {
                _eventManager.JsonFileName = openFileDlg.FileName;
                _mainwindow.jsonPath.Text = _eventManager.JsonFileName.Split('\\').Last();
            }
        }

        public void StartFlow() => SignalChange(StartFromEventManager());

        public void EndFlow() => EndFromEventManager();

        public void ShowFlow()
        {
            try
            {
                _eventManager.TestReset();
                if (_eventManager.Test())
                    MessageBox.Show("Test Success");
                else
                    MessageBox.Show("Test Fail");
                _eventManager.TestReset();
                MessageBox.Show($"Read Final Read : {_eventManager.TestRead()}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 改變UI指示燈
        /// 綠色代表Load and Register成功
        /// 紅色代表Unregister或是失敗
        /// </summary>
        /// <param name="isOn"></param>
        private void SignalChange(bool isOn) => _mainwindow.flowSignal.Fill = isOn ? Brushes.Green : Brushes.Red;
        private bool StartFromEventManager()
        {
            if (!_eventManager.LinkToPLC()) return false;
            if(_eventManager.IsMonitoring) return false;
            if(!_eventManager.LoadWorkFlow()) return false;
            _eventManager.RegisterEvents();
            Thread thread = new Thread(RunMonitor);
            thread.Start();
            return true;
        }
        private void EndFromEventManager()
        {
            _eventManager.UnregisterEvents();
            SignalChange(false);
        }
        private void RunMonitor()
        {
            _eventManager.Monitor();
        }
    }      
}
