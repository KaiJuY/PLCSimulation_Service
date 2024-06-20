using PLCSumulation.SocketServiceLab;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace PLCSumulation.StreamProtocol
{
    public abstract class ProtocolHandler
    {
        private readonly BlockingCollection<WorkItem> _workQueue = new BlockingCollection<WorkItem>();
        //已經是一個執行緒安全的集合,不需要再加上lock
        private readonly Thread _workerThread;
        private readonly object _lock = new object();
        protected ISocketService SocketServer;

        public ProtocolHandler(ISocketSetting socketSetting)
        {
            SocketServer = SocketServiceFactory.CreateSocketService(socketSetting, HandleData);
            SocketServer.Start();
            _workerThread = new Thread(MonitorWorkQueue)
            {
                IsBackground = true
            };
            _workerThread.Start();
        }
        // 這個方法會被 SocketService 觸發,以Template Method Pattern實作
        // 這個方法會檢查訊息是否符合協定格式,解析訊息,並處理解析後的資料
        // 這個方法會呼叫 IsValidMessage, ParseMessage, ProcessParsedData 這三個抽象方法
        // 這三個抽象方法由子類別實作
        private void HandleData(object sender, SocketDataEventArgs e) 
        {
            Console.WriteLine("Received data from " + e.RemoteEndPoint);
            StringBuilder sb = new StringBuilder();
            foreach (var item in e.Data)
            {
                sb.Append(item.ToString("X2") + " ");
            }
            Console.WriteLine(sb.ToString());
            SocketServer.Send(Encoding.ASCII.GetBytes("Recived From Protocol"), e.RemoteEndPoint);
            _workQueue.Add(new WorkItem { Data = e.Data, RemoteEndPoint = e.RemoteEndPoint });
        }
        private void MonitorWorkQueue()
        {
            while (true)
            {
                WorkItem workItem;
                try
                {
                    // 從佇列取出工作項目
                    workItem = _workQueue.Take();
                }
                catch (InvalidOperationException)
                {
                    // 佇列已經被標記為完成
                    break;
                }

                // 將工作項目分派給工作者執行緒
                ThreadPool.QueueUserWorkItem(ProcessWorkItem, workItem);
            }
        }
        private void ProcessWorkItem(object state)
        {
            WorkItem workItem = (WorkItem)state;

            if (IsValidMessage(workItem.Data))
            {
                ParsedData parsedData = ParseMessage(workItem.Data);
                ProcessParsedData(parsedData, workItem.RemoteEndPoint);
            }
        }

        protected abstract bool IsValidMessage(byte[] data);
        protected abstract ParsedData ParseMessage(byte[] data);
        protected abstract void ProcessParsedData(ParsedData parsedData, EndPoint remoteEndPoint);
    }
}
