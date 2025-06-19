using System;
using Avalonia.Media.Imaging;

namespace windowLogger.ViewModels;

public class DesignViewVm
{
    public Bitmap? AppIcon { get; set; } = new Bitmap("./example.png");
    public string? PrevAppName { get; set; } = "PrevAppName";
    public string? PrevAppPath { get; set; } = "PrevAppPath";
    public string? SessionTimeText { get; set; } = "00:00:00";

    public DesignViewVm()
    {
        Console.WriteLine("Design View VM ctor");
    }
}

public static class DesignData
{
    public static DesignViewVm Sample => new DesignViewVm();
}
