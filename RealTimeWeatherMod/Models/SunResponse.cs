using System;
using System.Collections.Generic;

namespace ChillWithYou.EnvSync.Models
{
    [Serializable]
    public class SunResponse
    {
        public List<SunResult> results;
    }

    [Serializable]
    public class SunResult
    {
        public List<SunData> sun;
    }

    [Serializable]
    public class SunData
    {
        public string date;
        public string sunrise;
        public string sunset;
    }
}
