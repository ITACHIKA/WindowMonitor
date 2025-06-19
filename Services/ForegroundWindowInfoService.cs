using System.Runtime.InteropServices;
using windowLogger.Models;

namespace windowLogger.Services;

public static partial class ForegroundWindowInfoService
{
    [LibraryImport("window_log.dll")]
    private static partial FgAppInfoReturnStructRaw bgcapture_entry_nonc();

    public static FgAppInfoReturnStruct GetFgAppReturnInfo()
    {
        FgAppInfoReturnStruct FgAppInfo;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var FgAppInfoRaw = bgcapture_entry_nonc();
        string appName = Marshal.PtrToStringAnsi(FgAppInfoRaw.appName)!;
        string appPathW = Marshal.PtrToStringUni(FgAppInfoRaw.appPathW)!;
        ulong appHash = FgAppInfoRaw.appHash;
        HiconState returnState = FgAppInfoRaw.returnState;
        if (FgAppInfoRaw.returnState == HiconState.NORMAL)
        {
            // Console.WriteLine(appName);
            // Console.WriteLine(appPathW);
            // Console.WriteLine(FgAppInfoRaw.appHash.ToString());
            // Console.WriteLine(FgAppInfoRaw.returnState.ToString());
            FgAppInfo = new FgAppInfoReturnStruct { appName = appName, appHash = appHash, returnState = returnState, appPath = appPathW, Sw=sw};
        }
        else
        {
            FgAppInfo = new FgAppInfoReturnStruct { appName = "", appHash = 0, returnState = returnState, appPath = "",Sw=sw };
        }
        Marshal.FreeCoTaskMem(FgAppInfoRaw.appName);
        Marshal.FreeCoTaskMem(FgAppInfoRaw.appNameW);
        Marshal.FreeCoTaskMem(FgAppInfoRaw.appPathW);
        return FgAppInfo;
    }
}
