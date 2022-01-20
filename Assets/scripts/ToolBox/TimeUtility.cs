using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Assertions;

public sealed class TimeUtility
{
    //private static long s_AverageRoundtripTime = 0;
    //private static long s_RemoteTimeOffset = 0;
    private static TimeUtility s_Instance = new TimeUtility();

    private TimeUtility()
    {
        m_StartTimeUs = GetElapsedTimeUs();
    }
    private long m_StartTimeUs = 0;
    //private long m_ClientTickTimeUs = 0;
    //private long m_ClientDeltaTime = 0;

    private static long m_SeverTimeStampMilliSecondOffset;
    public static void SetSeverTimeStampMilliSecondOffset(long time)
    {
        m_SeverTimeStampMilliSecondOffset = time - ClientTimeStampMilliSecond;
    }

    public static long ClientTimeStampMilliSecond => (long)(DateTime.Now.AddHours(-8) - new DateTime(1970, 1, 1)).TotalMilliseconds;
    public static long SeverTimeStampMilliSecond => m_SeverTimeStampMilliSecondOffset + ClientTimeStampMilliSecond;
    public static long ServerTimeSeconds => SeverTimeStampMilliSecond / 1000;
    public static double CurTimestamp => SeverTimeStampMilliSecond / 1000;
    public static long GetLocalMilliseconds()
    {
        return (GetElapsedTimeUs() - s_Instance.m_StartTimeUs) / 1000;
    }
    public static long GetElapsedTimeUs()
    {
        return DateTime.Now.Ticks / 10;
    }
    public static void GetDHMS(long allSeconds, out int day, out int hour, out int minute, out int seconds)
    {
        day = 0;
        hour = 0;
        minute = 0;
        seconds = 0;
        if (allSeconds >= 0) //天,
        {
            day = (int)allSeconds / 86400;
            hour = (int)(allSeconds - day * 86400) / 3600;
            minute = (int)(allSeconds - day * 86400 - hour * 3600) / 60;
            seconds = (int)(allSeconds - day * 86400 - hour * 3600 - minute * 60);
        }
    }

    public static DateTime GetCurrDateTime()
    {
        return GetDateTimeFromTimeStamp(SeverTimeStampMilliSecond);
    }

    public static DateTime GetDateTimeFromTimeStamp(long milliSeconds)
    {
        DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
        TimeSpan time = TimeSpan.FromMilliseconds(milliSeconds);
        return startTime.Add(time);
    }

    public static DateTime GetDateTimeFromTimeStampEx(long milliSeconds)
    {
        DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        TimeSpan time = TimeSpan.FromMilliseconds(milliSeconds);
        return startTime.Add(time);
    }

    public static long GetTimeStampFromDateTime(DateTime dateTime)
    {
        TimeSpan ts = dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        double temp = ts.TotalMilliseconds;
        return (long)temp;
    }

    /// <summary>
    /// 字符串转DateTime
    /// </summary>
    /// <param name="dateStr"></param>
    /// <returns></returns>
    public static DateTime GetDateTimeByStr(string dateStr)
    {
        return string.IsNullOrEmpty(dateStr) ? DateTime.MinValue : Convert.ToDateTime(dateStr);
    }

    public static DateTime ParseDate(string str,string Pattern = "yyyy/MM/dd")
    {
        System.Globalization.DateTimeFormatInfo dtFormat = new System.Globalization.DateTimeFormatInfo();
        dtFormat.ShortDatePattern = Pattern;
        return Convert.ToDateTime(str, dtFormat);
    }
    /// <summary>
    /// 当前服务器时间是否在时间范围内
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <returns></returns>
    public static bool InOpenedTimeSpan(DateTime startTime, DateTime endTime)
    {
        var currDateTime = GetCurrDateTime();
        return (currDateTime <= endTime && currDateTime >= startTime);
    }

    /// <summary>
    /// 当前服务器时间是否在时间范围内
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <returns></returns>
    public static bool InOpenedTimeSpan(int startSeconds, int endSeconds)
    {
        return (ServerTimeSeconds <= endSeconds && ServerTimeSeconds >= startSeconds);
    }

    //获取当前时间0点
    public static long GetZeroTime(long iTimeIn)
    {
        //时间戳0的时间（时区是北京时间，服务器和数据库要设置对）
        long iDay = GetDayCount(iTimeIn);
        long iTime = 1451836800 + (iDay * 86400);
        return iTime;
    }

    //获取今天是基准天的第几天，用户每日重置的逻辑
    public static long GetDayCount(long iTime)
    {
        //1451836800 2016年1月4号
        long iDay = (iTime - 1451836800) / 86400;
        return iDay;
    }

    public static long GetZeroMonth(int iTimeIn)
    {
        DateTime cDateTime = TimeStampToDateTime(iTimeIn);
        System.DateTime zeroTime = new System.DateTime(cDateTime.Year, cDateTime.Month, 1, 0, 0, 0);
        return TimeUtility.GetTimeStampFromDateTime(zeroTime);
    }

    //获取当前时间的周一0点
    public static int GetZeroWeek(int iTimeIn)
    {
        int iWeek = GetWeekCount(iTimeIn);
        int iTime = iTimeStart + (iWeek * 604800);
        return iTime;
    }
    //获取今天是基准天的第几周
    public static int GetWeekCount(int iTime)
    {
        //1451836800 2016年1月4号 周一0点
        int iWeek = (iTime - iTimeStart) / 604800;
        return iWeek;
    }

    //1451836800 2016年1月4号
    public const int iTimeStart = 1451836800;
    private static readonly System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0));

    public static DateTime TimeStampToDateTime(int timeStamp)
    {
        return startTime.AddSeconds(timeStamp);
    }

    /// <summary>
    /// 获取两位数字的标准时间形式
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public static string GetNormalNum(int num)
    {
        if (num >= 10)
            return num.ToString();
        else
            return string.Format("0{0}", num);
    }

    public static readonly long OneDayMs = 86400000;

    public static long GetMSByTime(int h, int m, int s = 0, int ms = 0)
    {
        return ((long)ms) + ((long)s) * 1000 + ((long)m) * 60 * 1000 + ((long)h) * 3600 * 1000;
    }
    /// <summary>
    /// 格式 10:00:00 或 10:00
    /// </summary>
    /// <param name="s"></param>
    private static bool tryParseTime(string str,out int[] res, int expectN = -1)
    {
        res = null;
        string trim = "";
        if (str != null && (trim = str.Trim()).Length > 0)
        {
            string[] arr = trim.Split(':');
            if (expectN > 0 && arr.Length != expectN)
                return false;
            int[] resArr = new int[arr.Length];
            for(int i = 0;i < arr.Length;++i)
            {
                if(int.TryParse(arr[i],out int v))
                {
                    resArr[i] = v;
                } 
                else
                {
                    return false;
                }
            }
            res = resArr;
            return true;
        }
        return false;
    }
    /// <summary>
    /// 格式 10:00-11:59
    /// </summary>
    /// <returns></returns>
    public static (long,long) GetTimeRangeByStr(string str)
    {
        var res = (-1, -1);
        string trim = "";
        if(str != null && (trim = str.Trim()).Length > 0)
        {
            string[] arr = trim.Split('-');
            if(tryParseTime(arr[0], out int[] start, 2) && tryParseTime(arr[1],out int[] end,2))
            {
                var b = GetMSByTime(start[0], start[1]);
                var e = GetMSByTime(end[0], end[1]);
                if(e < b)
                {
                    return res;
                }
                return (b,e);
            }
        }
        return res;
    }

    public static long GetCurrDayTimestamp(long curr,long add = 28800000)
    {
        return curr % OneDayMs + add;
    }

    public static long GetRestTime(long curr,string t)
    {
        if(tryParseTime(t,out int[] time))
        {
            long curr_t = GetCurrDayTimestamp(curr);
            if(time.Length == 2)
            {
                return GetMSByTime(time[0], time[1]) - curr_t;
            }
            if (time.Length == 3)
            {
                return GetMSByTime(time[0], time[1],time[2]) - curr_t;
            }
        }
        return -1;
    }
   
    public static string FormatTimeBySecond(long s,char p = ':')
    {
        if (s < 0) return "";
        long h = s / 3600;
        long m = s % 3600 / 60;
        long sec = s % 3600 % 60;
        StringBuilder sb = new StringBuilder();
        sb.Append(h < 10 ? string.Format("0{0}", h) : h.ToString());
        sb.Append(p);
        sb.Append(m < 10 ? string.Format("0{0}", m) : m.ToString());
        sb.Append(p);
        sb.Append(sec < 10 ? string.Format("0{0}", sec) : sec.ToString());
        return sb.ToString();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="range">["11:00-12:00",...]</param>
    /// <param name="timeStamp">当前的毫秒值</param>
    /// <returns></returns>
    public static (int,int,int) GetRangeIdxByTimestamp(List<string> range,long timeStamp,Func<string,(long,long)> parser = null)
    {
        int res = -1;
        if (range == null) return (res,-1,-1);
        if (parser == null) parser = GetTimeRangeByStr;

        var args = new List<Pair<long, long>>();
        range.ForEach((a) =>
       {
           var (b,e) = parser(a);
           args.Add(new Pair<long, long>(b, e));
       });
        return GetRangeIdxByTimestamp(args, timeStamp);
    }

    public static (int, int,int) GetRangeIdxByTimestamp(List<Pair<long,long>> range, long timeStamp)
    {
        int res = -1;
        if (range == null) return (res, -1,-1);
        var curr = GetCurrDayTimestamp(timeStamp);
        bool lessMin = false;
        bool largeMax = false;
        int behind = -1;
        for (int i = 0; i < range.Count; ++i)
        {
            var b = range[i].first;
            var e = range[i].second;
            if (b == -1 && e == -1) continue;
            if (i == 0) lessMin = curr < b;
            if (i == range.Count) largeMax = curr >= e;
            if (curr >= b && curr < e)
            {
                return (i, 0, -1);
            }
            else if(curr >= e)
            {
                behind = i;
            }
        }
        return (res, lessMin ? -1 : largeMax ? 1 : 0,behind);
    }
    #region Test
    //-------------------------------------------------------------------------------------------------------------------------------


    public static void Test()
    {
        Assert.AreEqual(GetMSByTime(23, 59, 59, 1000),OneDayMs,"");
        var (b,e) = GetTimeRangeByStr("10:00-11:00");
        Assert.AreEqual(GetMSByTime(10, 00), b, "");
        Assert.AreEqual(GetMSByTime(11, 00), e, "");
        Assert.AreEqual(GetTimeRangeByStr("10:00-11"), (-1, -1), "");
        Assert.AreEqual(GetTimeRangeByStr("10:00-11:00:00"), (-1, -1), "");
        UnityEngine.Debug.Log(" GetRestTime(ServerTimeSeconds,\"19:00\") = " 
            + FormatTimeBySecond(GetRestTime(SeverTimeStampMilliSecond, "19:00") / 1000));
        UnityEngine.Debug.Log(FormatTimeBySecond((GetCurrDayTimestamp(SeverTimeStampMilliSecond) - GetMSByTime(0, 0))/1000) + "");

        UnityEngine.Debug.Log("GetRangeIdxByTimestamp " + GetRangeIdxByTimestamp(new List<string>(
           new string[] { "10:00-12:00", "14:00-19:00","20:00-21:00"  }
        ), SeverTimeStampMilliSecond));
    }
    #endregion
}

public sealed class TimeSnapshot
{
    public static void Start()
    {
        Instance.Start_();
    }
    public static long End()
    {
        return Instance.End_();
    }
    public static long DoCheckPoint()
    {
        return Instance.DoCheckPoint_();
    }

    private void Start_()
    {
        m_LastSnapshotTime = TimeUtility.GetElapsedTimeUs();
        m_StartTime = m_LastSnapshotTime;
    }
    private long End_()
    {
        m_EndTime = TimeUtility.GetElapsedTimeUs();
        return m_EndTime - m_StartTime;
    }
    private long DoCheckPoint_()
    {
        long curTime = TimeUtility.GetElapsedTimeUs();
        long ret = curTime - m_LastSnapshotTime;
        m_LastSnapshotTime = curTime;
        return ret;
    }

    private long m_StartTime = 0;
    private long m_LastSnapshotTime = 0;
    private long m_EndTime = 0;

    private static TimeSnapshot Instance
    {
        get
        {
            if (null == s_Instance)
            {
                s_Instance = new TimeSnapshot();
            }
            return s_Instance;
        }
    }

    [ThreadStatic]
    private static TimeSnapshot s_Instance = null;

    

}
