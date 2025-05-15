﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.ObjectModel;
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
using System.Threading.Tasks;
using EventDriven.Model;
using System.Reflection;

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
        private string _executionResult;
        private string _lastTriggeredActionName;
        private ObservableCollection<TriggerInfo> _triggers;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TriggerInfo> Triggers
        {
            get { return _triggers; }
            set
            {
                if (_triggers != value)
                {
                    _triggers = value;
                    OnPropertyChanged();
                }
            }
        }
        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }
        public string ExecutionResult
        {
            get { return _executionResult; }
            set
            {
                if (_executionResult != value)
                {
                    _executionResult = value;
                    OnPropertyChanged();
                }
            }
        }
        public string LastTriggeredActionName
        {
            get { return _lastTriggeredActionName; }
            set
            {
                if (_lastTriggeredActionName != value)
                {
                    _lastTriggeredActionName = value;
                    OnPropertyChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel(MainWindow mainwindow)
        {
            _eventManager = new Services.EventManager();
            _mainwindow = mainwindow;
            _ioContainer = _eventManager.IOContainer;
            Triggers = new ObservableCollection<TriggerInfo>();            
            
            // 預設值
            Protocol = "Mx";
            CpuType = "QCPU";
            IpAddress = "192.168.31.100";
            Port = "";
            IsMxProtocol = Protocol == "Mx";
        }

        private void EventManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Services.EventManager.RegisteredTriggers))
            {
                foreach(var trigger in _eventManager.RegisteredTriggers)
                {
                    var existingTrigger = Triggers.FirstOrDefault(t => t.Name == trigger.Key);
                    if (existingTrigger == null)
                    {
                        _mainwindow.Dispatcher.Invoke(() => { 
                            // 如果不存在，則新增
                            Triggers.Add(new TriggerInfo
                            {
                                Name = trigger.Key,
                                IsExecuting = trigger.Value.IsExecuting,
                                CurrentStep = trigger.Value.CurrentStep,
                                TotalSteps = trigger.Value.TotalSteps,
                                Type = trigger.Value.Type,
                                Conditions = new ObservableCollection<ConditionInfo>(trigger.Value.Conditions)
                            });                        
                        });
                    }
                    else
                    {
                        // 如果已存在，則更新
                        _mainwindow.Dispatcher.Invoke(() => {
                            existingTrigger.IsExecuting = trigger.Value.IsExecuting;
                            existingTrigger.CurrentStep = trigger.Value.CurrentStep;
                            existingTrigger.TotalSteps = trigger.Value.TotalSteps;
                            existingTrigger.Type = trigger.Value.Type;
                            existingTrigger.Conditions = new ObservableCollection<ConditionInfo>(trigger.Value.Conditions);
                        });
                    }
                }
            }
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
                    IpAddress = _isMxProtocol == true ? "192.168.31.100" : "127.0.0.1";
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

        public async void StartFlow() => SignalChange(await StartFromEventManager());

        public void EndFlow() => EndFromEventManager();

        public void ClearButtons()
        {
            _mainwindow.Dispatcher.Invoke(() => _mainwindow.buttonPanel.Children.Clear());
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
                        Button button = null;
                        _mainwindow.Dispatcher.Invoke(() => {
                            button = new Button
                            {
                                Content = buttonConfig.ButtonContent,
                                Margin = new Thickness(5)
                            };

                            button.Click += (sender, e) =>
                            {
                                // 清空執行結果
                                ExecutionResult = $"{buttonConfig.ButtonContent}";
                                // 執行按鈕的動作
                                ExecuteActions(buttonConfig.Actions);
                            };

                            _mainwindow.buttonPanel.Children.Add(button);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading buttons: {ex.Message}");
            }
        }



        private async void ExecuteActions(List<Model.Action> actions)
        {
            try
            {
                await Task.Run(() => {
                    foreach (var action in actions)
                    {
                        if(!ExecuteFactory.GetExeAction(action.ActionName).Execute(action))
                        {
                            ExecutionResult += $" : 執行失敗, {action.ActionName}";
                            OnPropertyChanged(nameof(ExecutionResult));
                            break;
                        }
                        
                    }
                });
                ExecutionResult += $" : 執行成功";
                OnPropertyChanged(nameof(ExecutionResult));
            }
            catch (Exception ex)
            {
                ExecutionResult += $" : 執行失敗: {ex.Message}";
                OnPropertyChanged(nameof(ExecutionResult));
            }
        }
        /// <summary>
        /// 改變UI指示燈
        /// 綠色代表Load and Register成功
        /// 紅色代表Unregister或是失敗
        /// </summary>
        /// <param name="isOn"></param>
        public void SignalChange(bool isOn) => _mainwindow.Dispatcher.Invoke(() => _mainwindow.flowSignal.Fill = isOn ? Brushes.Green : Brushes.Red);
        private async Task<bool> StartFromEventManager()
        {
            if (!_eventManager.LinkToPLC()) return false;
            if (_eventManager.IsMonitoring) return false;
            if (!_eventManager.LoadWorkFlow()) return false;
            _eventManager.PropertyChanged += EventManager_PropertyChanged;
            await Task.Run(() => _eventManager.DoInitialActions());
            await Task.Run(() => _eventManager.RegisterEvents());
            Thread thread = new Thread(RunMonitor);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return true;
        }        
        private void EndFromEventManager()
        {
            _eventManager.UnregisterEvents();
            SignalChange(false);
            ClearTriggers();
        }
        private void ClearTriggers()
        {
            _eventManager.PropertyChanged -= EventManager_PropertyChanged;
            SpinWait.SpinUntil(() => false, 300);
            _mainwindow.Dispatcher.Invoke(() => Triggers = new ObservableCollection<TriggerInfo>());            
        }
        private void RunMonitor()
        {
            _eventManager.Monitor();
        }
    }
}
