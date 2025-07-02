using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using windowLogger.Models;
using windowLogger.Services;

namespace windowLogger.ViewModels;

public class WindowInfoUpdateVm : INotifyPropertyChanged
{
    private const string CachePath = "cache/";
    private readonly string _processName = Process.GetCurrentProcess().ProcessName;
    public event PropertyChangedEventHandler? PropertyChanged;
    public Bitmap? AppIcon { get; set; }
    public string? PrevAppName { get; set; }
    public string? PrevAppPath { get; set; }
    private ulong PrevAppHash { get; set; }
    private double SessionTimeMillis { get; set; }
    private TimeSpan SessionTimeFormat { get; set; }
    public string? SessionTimeText { get; set; }

    public static int MinimumRecordSessionSpan { get; set; }
    private bool FirstRun { get; set; } = true;
    public static int CaptureInterval { get; set; }
    public List<string> IgnoredAppList { get; set; } = [""];
    

    private static DateTime SessionStartTime { get; set; } = DateTime.Now;
    private static DateTime LastUpdateTime { get; set; } 
    
    public void StartTimer()
    {
        TimedInfoService.OnInfoUpdate += InfoUpdateHandler;
        TimedInfoService.StartTimer(CaptureInterval);
    }
    public void StopTimer()
    {
        TimedInfoService.StopTimer();
        PrevAppName = null;
        PrevAppPath = null;
        FirstRun = true;
        SessionTimeText = null;
        SessionTimeMillis = 0;
        AppIcon = null;
        TimedInfoService.OnInfoUpdate -= InfoUpdateHandler;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
    private async void InfoUpdateHandler(FgAppInfoReturnStruct fgAppInfo)
    {
        if (fgAppInfo.returnState == HiconState.NORMAL && fgAppInfo.appName!=PrevAppName)
        {
            if (!fgAppInfo.appName.Contains(_processName))
            {
                Console.WriteLine("Time for DLL run:"+fgAppInfo.Sw.Elapsed.TotalMilliseconds);
                if (FirstRun)
                {
                    Console.WriteLine("first Run");
                    SessionTimeMillis = fgAppInfo.Sw.Elapsed.TotalMilliseconds;
                    FirstRun = false;
                    SessionStartTime = DateTime.Now;
                }
                else
                {
                    if ((DateTime.Now - LastUpdateTime).TotalSeconds >= MinimumRecordSessionSpan)
                    {
                        Console.WriteLine("Update On Fg WIND");
                        SessionTimeMillis = fgAppInfo.Sw.Elapsed.TotalMilliseconds;
                        var SingleSessionTime = new AppSingleSessionData(SessionStartTime, DateTime.Now);
                        SessionStartTime = DateTime.Now;
                        await SessionStatService.AddRecord(PrevAppName!,PrevAppPath!, PrevAppHash, SingleSessionTime);
                    }
                    else
                    {
                        SessionTimeMillis = DateTime.Now.Subtract(SessionStartTime).TotalMilliseconds;
                    }
                }
                AppIcon = await SessionStatService.LoadImage(fgAppInfo.appHash);
                PrevAppPath = fgAppInfo.appPath;
                PrevAppName = fgAppInfo.appName;
                PrevAppHash = fgAppInfo.appHash;
            }
            LastUpdateTime = DateTime.Now;
        }
        else if (!FirstRun)
        {
            SessionTimeMillis = DateTime.Now.Subtract(SessionStartTime).TotalMilliseconds;
        }
        SessionTimeFormat = TimeSpan.FromMilliseconds(SessionTimeMillis);
        SessionTimeText = SessionTimeFormat.ToString(@"hh\:mm\:ss\.f");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
    public static void InitRecord()
    {
        SessionStatService.InitRecord();
    }
    public static async Task DeleteRecord()
    {
        await SessionStatService.DelRecord();
    }
    public static void DeleteCache()
    {
        if (!Directory.Exists(CachePath)) return;
        var pics = Directory.GetFiles(CachePath);
        foreach (var pic in pics)
        {
            try
            {
                File.Delete(pic);
            }
            catch (Exception e)
            {
                Console.WriteLine("Clear cache err:" + e);
            }
        }
    }
}
