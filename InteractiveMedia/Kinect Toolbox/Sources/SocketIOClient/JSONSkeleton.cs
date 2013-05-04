using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SocketIoClient
{
    using Microsoft.Kinect;

    using Newtonsoft.Json;

    using SocketIoClient;

    [JsonObject(MemberSerialization.OptIn)]
    public class JSONSkeleton
    {
        [JsonProperty]
        private Skeleton _skeletonObj;

        public JSONSkeleton()
        {

            _skeletonObj = new Skeleton();
        }

        public JSONSkeleton(Skeleton skeletonObj)
        {
            _skeletonObj = skeletonObj;

        }

        /*
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public void OutputToFile()
        {
            logger.Trace("#################################");
            logger.Trace("Outputting Skeleton JSON Object");
            logger.Trace(ToJsonString());
            logger.Trace("#################################");          
        }
        */

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
        public static JSONSkeleton Deserialize(string jsonString)
        {
            return JsonConvert.DeserializeObject<JSONSkeleton>(jsonString);
        }
    }
}
