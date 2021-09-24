using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

public class TimeUtils
{
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
    private static bool tryParseTime(string str, out int[] res, int expectN = -1)
    {
        res = null;
        string trim = "";
        if (str != null && (trim = str.Trim()).Length > 0)
        {
            string[] arr = trim.Split(':');
            if (expectN > 0 && arr.Length != expectN)
                return false;
            int[] resArr = new int[arr.Length];
            for (int i = 0; i < arr.Length; ++i)
            {
                if (int.TryParse(arr[i], out int v))
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
    public static (long, long) GetTimeRangeByStr(string str)
    {
        var res = (-1, -1);
        string trim = "";
        if (str != null && (trim = str.Trim()).Length > 0)
        {
            string[] arr = trim.Split('-');
            if (tryParseTime(arr[0], out int[] start, 2) && tryParseTime(arr[1], out int[] end, 2))
            {
                var b = GetMSByTime(start[0], start[1]);
                var e = GetMSByTime(end[0], end[1]);
                if (e < b)
                {
                    return res;
                }
                return (b, e);
            }
        }
        return res;
    }

    public static long GetCurrDayTimestamp(long curr, long add = 28800000)
    {
        return curr % OneDayMs + add;
    }

    public static long GetRestTime(long curr, string t)
    {
        if (tryParseTime(t, out int[] time))
        {
            long curr_t = GetCurrDayTimestamp(curr);
            if (time.Length == 2)
            {
                return GetMSByTime(time[0], time[1]) - curr_t;
            }
            if (time.Length == 3)
            {
                return GetMSByTime(time[0], time[1], time[2]) - curr_t;
            }
        }
        return -1;
    }

    public static string FormatTimeBySecond(long s, char p = ':')
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
    public static (int, int, int) GetRangeIdxByTimestamp(List<string> range, long timeStamp, Func<string, (long, long)> parser = null)
    {
        int res = -1;
        if (range == null) return (res, -1, -1);
        if (parser == null) parser = GetTimeRangeByStr;

        var args = new List<Utils.Pair<long, long>>();
        range.ForEach((a) =>
        {
            var (b, e) = parser(a);
            args.Add(new Utils.Pair<long, long>(b, e));
        });
        return GetRangeIdxByTimestamp(args, timeStamp);
    }

    public static (int, int, int) GetRangeIdxByTimestamp(List<Utils.Pair<long, long>> range, long timeStamp)
    {
        int res = -1;
        if (range == null) return (res, -1, -1);
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
            else if (curr >= e)
            {
                behind = i;
            }
        }
        return (res, lessMin ? -1 : largeMax ? 1 : 0, behind);
    }
    public static long GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalMilliseconds);
    }
    #region Test
    //-------------------------------------------------------------------------------------------------------------------------------


    public static void Test()
    {
        var SeverTimeStampMilliSecond = GetTimeStamp();

        Assert.AreEqual(GetMSByTime(23, 59, 59, 1000), OneDayMs, "");
        var (b, e) = GetTimeRangeByStr("10:00-11:00");
        Assert.AreEqual(GetMSByTime(10, 00), b, "");
        Assert.AreEqual(GetMSByTime(11, 00), e, "");
        Assert.AreEqual(GetTimeRangeByStr("10:00-11"), (-1, -1), "");
        Assert.AreEqual(GetTimeRangeByStr("10:00-11:00:00"), (-1, -1), "");
        UnityEngine.Debug.Log(" GetRestTime(ServerTimeSeconds,\"19:00\") = "
            + FormatTimeBySecond(GetRestTime(SeverTimeStampMilliSecond, "19:00") / 1000));
        UnityEngine.Debug.Log(FormatTimeBySecond((GetCurrDayTimestamp(SeverTimeStampMilliSecond) - GetMSByTime(0, 0)) / 1000) + "");

        UnityEngine.Debug.Log("GetRangeIdxByTimestamp " + GetRangeIdxByTimestamp(new List<string>(
           new string[] { "10:00-12:00", "14:00-19:00", "20:00-21:00" }
        ), SeverTimeStampMilliSecond));
    }
    #endregion
}
