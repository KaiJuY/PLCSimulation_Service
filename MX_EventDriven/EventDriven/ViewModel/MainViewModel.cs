﻿using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using EventDriven.Model;
using System.Linq;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using static EventDriven.Services.EventManager;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EventDriven.ViewModel
{
    /// <summary>
    /// 先驗證功能，先不要用MVVM開發
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Services.EventManager _eventManager;
        protected MainWindow _mainwindow;
        private string _protocol;
        private string _cpuType;
        private string _ipAddress;
        private string _port;
        private bool _isMxProtocol;
        private readonly Model.IOContainer _ioContainer;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel(MainWindow mainwindow)
        {
            _eventManager = new Services.EventManager();
            _mainwindow = mainwindow;
            _ioContainer = _eventManager.IOContainer;

            // 預設值
            Protocol = "Mx";
            CpuType = "QCPU";
            IpAddress = "192.168.31.100";
            Port = "";
            IsMxProtocol = Protocol == "Mx";
        }

        public string Protocol
        {
            get { return _protocol; }
            set
            {
                if (_protocol != value)
                {
                    _protocol = value;
                    IsMxProtocol = value == "Mx";
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMxProtocol
        {
            get { return _isMxProtocol; }
            set
            {
                if (_isMxProtocol != value)
                {
                    _isMxProtocol = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CpuType
        {
            get { return _cpuType; }
            set
            {
                if (_cpuType != value)
                {
                    _cpuType = value;
                    IpAddress = _cpuType == "QCPU" ? "192.168.31.100" : "127.0.0.1";
                    OnPropertyChanged();                    
                }
            }
        }

        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged();                    
                }
            }
        }

        public string Port
        {
            get { return _port; }
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged();                    
                }
            }
        }
        public void UpdateIOContainer()
        {
            if (Protocol == "Mx")
            {
                _ioContainer.CreateMxControlModule(CpuType, IpAddress);
            }
            else if (Protocol == "Mc")
            {
                int portNumber = string.IsNullOrEmpty(Port) ? 7500 : int.Parse(Port);
                _ioContainer.CreateMcControlModule(IpAddress, portNumber);
            }
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

        public void ClearButtons()
        {
            _mainwindow.buttonPanel.Children.Clear();
        }

        public void ShowFlow()
        {
            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void LoadButtons()
        {
            try
            {
                string json = System.IO.File.ReadAllText(_eventManager.JsonFileName);
                var workFlow = System.Text.Json.JsonSerializer.Deserialize<Model.TriggerWorkFlowModel>(json);

                if (workFlow?.Buttons != null)
                {
                    foreach (var buttonConfig in workFlow.Buttons)
                    {
                        Button button = new Button
                        {
                            Content = buttonConfig.ButtonContent,
                            Margin = new Thickness(5)
                        };

                        button.Click += (sender, e) =>
                        {
                            // 執行按鈕的動作
                            ExecuteActions(buttonConfig.Actions);
                        };

                        _mainwindow.buttonPanel.Children.Add(button);
        }
    }
}
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading buttons: {ex.Message}");
            }
        }

        private void ExecuteActions(List<Model.Action> actions)
        {
            foreach (var action in actions)
            {
                ExecuteFactory.GetExeAction(action.ActionName).Execute(action);
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
