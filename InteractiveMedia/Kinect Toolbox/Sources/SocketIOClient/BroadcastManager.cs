using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketIoClient
{
    public class BroadcastManager
    {
        private static BroadcastEngine _broadcastEngine;

        public static BroadcastEngine GetBroadcastEngine()
        {
            if (_broadcastEngine == null)
            {
                _broadcastEngine = new BroadcastEngine();
            }

            return _broadcastEngine;
        }
    }
}
