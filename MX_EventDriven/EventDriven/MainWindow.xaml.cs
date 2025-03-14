﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EventDriven.ViewModel;

namespace EventDriven
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Model.IOContainer _ioContainer;
        protected MainViewModel _viewModel;
        public MainWindow()
        {
            InitializeComponent();
            _ioContainer = new Model.IOContainer();
            _viewModel = new ViewModel.MainViewModel(this, _ioContainer);
            DataContext = _viewModel;
        }
        /// <summary>
        /// Borwse JSON File
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.BrowseJsonFile();
        }
        /// <summary>
        /// Start Flow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // 檢查必要的資料是否已填寫
            if (!AreRequiredFieldsFilled())
            {
                // 顯示警告視窗
                MessageBox.Show("請填寫所有必要的資料！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // 不執行後續行為
            }
            _viewModel.UpdateIOContainer();
            // 在此處建立物件並執行後續行為
            _viewModel.StartFlow();
            _viewModel.LoadButtons();
        }

        // 檢查必要的資料是否已填寫的方法
        private bool AreRequiredFieldsFilled()
        {
            string protocol = protocolComboBox.SelectedValue as string;

            if (string.IsNullOrEmpty(protocol))
            {
                return false; // Protocol 未選擇
            }

            if (protocol == "Mx")
            {
                if (string.IsNullOrEmpty(cpuTypeComboBox.SelectedValue as string) || string.IsNullOrEmpty(ipAddressTextBox.Text))
                {
                    return false; // Mx Protocol 且 CpuType 或 IP 未填寫
                }
            }
            else if (protocol == "Mc")
            {
                if (string.IsNullOrEmpty(ipAddressTextBox.Text) || string.IsNullOrEmpty(portTextBox.Text))
                {
                    return false; // Mc Protocol 且 IP 或 Port 未填寫
                }
            }

            return true; // 所有必要的欄位都已填寫
        }
        /// <summary>
        /// End Flow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            _viewModel.EndFlow();
            _viewModel.ClearButtons();
        }
        /// <summary>
        /// Try to do Test Flow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowFlow();
        }
    }
}
