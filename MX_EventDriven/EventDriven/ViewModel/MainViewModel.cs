using System;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using EventDriven.Services;
using EventDriven.Model;
using System.Linq;

namespace EventDriven.ViewModel
{
    /// <summary>
    /// 先驗證功能，先不要用MVVM開發
    /// </summary>
    public class MainViewModel
    {
        private readonly Services.EventManager _eventManager;
        private IOContainer _iOContainer;
        protected MainWindow _mainwindow;

        public MainViewModel(MainWindow mainwindow)
        {
            _eventManager = new Services.EventManager();
            _mainwindow = mainwindow;
            _iOContainer = new IOContainer();
        }

        public void BrowseJsonFile()
        {
            OpenFileDialog openFileDlg = new OpenFileDialog();

            // 可選：設置初始目錄
            openFileDlg.InitialDirectory = @"C:\";

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

        public void EndFlow()
        {
            _mainwindow.flowSignal.Fill = Brushes.Red;
        }

        public void ShowFlow()
        {
            try
            {
                if (_iOContainer.IsConnected() == false)
                {
                    _iOContainer.Connect();
                    MessageBox.Show(_iOContainer.IsConnected().ToString());
                }
                else
                {
                    bool ret = _iOContainer.PrimaryHandShake("W", "4E0F", "W", "469F");
                    MessageBox.Show($"Hand Shake Result : {ret}");
                    _iOContainer.WriteInt("W", "4E0F", 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SignalChange(bool isOn) => _mainwindow.flowSignal.Fill = isOn ? Brushes.Green : Brushes.Red;
        private bool StartFromEventManager()
        {
            if(!_eventManager.LoadWorkFlow()) return false;
            
            return true;
        }
    }      
}
