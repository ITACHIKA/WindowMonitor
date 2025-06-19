using System;
using System.Threading;
using System.Threading.Tasks;
using windowLogger.Models;

namespace windowLogger.Services;

public class TimedInfoService
{
    private static Timer? _timer;

    public static int ConfiguredMillis { get; set; }
    public static event Action<FgAppInfoReturnStruct>? OnInfoUpdate;
    public static async Task<FgAppInfoReturnStruct> FgAppInfoInquiry()
    {
        return await Task.Run(() => ForegroundWindowInfoService.GetFgAppReturnInfo());
    }
    public static void StartTimer(int millis)
    {
        ConfiguredMillis = millis;
        if (_timer == null)
        {
            _timer = new Timer(async _ =>
            {
                try
                {
                    var FgAppInfo = await FgAppInfoInquiry();
                    OnInfoUpdate?.Invoke(FgAppInfo);
                }
                catch (Exception e)
                {
                    Console.WriteLine("FgAppInfoInquiery timed Err: " + e);
                }
            }, null, 0, millis);
        }
        else
        {
            _timer.Change(0, millis);
        }
    }
    public static void StopTimer()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
    }
    public static void ReleaseTimer()
    {
        _timer?.Dispose();
    }
}