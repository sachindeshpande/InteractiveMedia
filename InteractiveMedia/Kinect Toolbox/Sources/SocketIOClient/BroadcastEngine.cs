using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketIoClient
{
    using System.Threading;

    public class BroadcastEngine
    {
        private Thread _broadcastingThread;
        private const int THREAD_WAIT_TIME = 100;
        private bool _enableBroadcasting;
        private Queue<object> _broadcastQueue;

        private SocketIOClientWrapper socketClient;

        public BroadcastEngine()
        {
            socketClient = new SocketIOClientWrapper();
            socketClient.Execute();

            _broadcastQueue = new Queue<object>();
            _enableBroadcasting = true;
            _broadcastingThread = new Thread(StartBroadcasting);
            _broadcastingThread.Start();
        }

        public void StartBroadcasting()
        {
            while (_enableBroadcasting)
            {
                if(_broadcastQueue.Count == 0)
                    Thread.Sleep(THREAD_WAIT_TIME);
                else
                {
                    JSONSkeleton packet = (JSONSkeleton)_broadcastQueue.Dequeue();
                    socketClient.Send("JSONSkeleton",packet);
                }
            }
        }

        public void ScheduleForBroadcasting(object broadcastPacket)
        {
            _broadcastQueue.Enqueue(broadcastPacket);
        }
    }
}
