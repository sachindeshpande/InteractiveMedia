using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gestures.DataStructures
{
    using Newtonsoft.Json;

    public class MouseControllerAction
    {
        public bool mouseMove { get; set; }
        public int dx { get; set; }
        public int dy { get; set; }
    }


    [JsonObject(MemberSerialization.OptIn)]
    public class JSonMouseInfo
    {
        [JsonProperty]
        private MouseControllerAction _mouseAction;


        public JSonMouseInfo(MouseControllerAction mouseAction)
        {
            _mouseAction = mouseAction;
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
        public static JSonMouseInfo Deserialize(string jsonString)
        {
            return JsonConvert.DeserializeObject<JSonMouseInfo>(jsonString);
        }
    }
}
