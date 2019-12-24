using System;
using Newtonsoft.Json.Linq;

namespace SideloadIPA
{
    public class AppleException : Exception
    {
        public AppleException(JToken serverAnswer) : base(serverAnswer["em"].ToString())
        {
            HResult = int.Parse(serverAnswer["ec"].ToString());
        }
    }
}