using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace windowLogger.Models;

public enum HiconState : int
{
    NORMAL = 0,
    NOUPDATE = 1,
}

[StructLayout(LayoutKind.Sequential)]
public struct FgAppInfoReturnStructRaw
{
    public HiconState returnState;
    public ulong appHash;
    public IntPtr appName;
    public IntPtr appNameW;
    public IntPtr appPathW;
}

public struct FgAppInfoReturnStruct
{
    public HiconState returnState;
    public ulong appHash;
    public string appName;
    public string appPath;
    public Stopwatch Sw;
}

public class AppSingleSessionData
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppSingleSessionData() {}
    public AppSingleSessionData(DateTime startTime, DateTime endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
}

[method: JsonConstructor]
public class AppSessionData(List<AppSingleSessionData> singleTimeDataList)
{
    public List<AppSingleSessionData> SingleTimeDataList { get; set; } = singleTimeDataList;

    public AppSessionData(AppSingleSessionData singleTimeData) : this([singleTimeData])
    {}
}

public class InquiredAppSessionData
{
    public string AppName { get; set; }
    public string AppPath { get; set; }
    public ulong AppHash { get; set; }
    public AppSessionData? AppSessionData { get; set; }

    public InquiredAppSessionData(string appName, string appPath,ulong appHash, AppSessionData appSessionData)
    {
        AppName = appName;
        AppPath = appPath;
        AppHash = appHash;
        AppSessionData = appSessionData;
    }
    public InquiredAppSessionData(string appName, string appPath,ulong appHash)
    {
        AppName = appName;
        AppPath = appPath;
        AppHash = appHash;
    }
}
