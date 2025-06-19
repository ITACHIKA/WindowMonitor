namespace windowLogger.ViewModels;

public class MainVm
{
	public SearchAppVm SearchAppVm { get; set; } = new SearchAppVm();
	public WindowInfoUpdateVm WindowInfoUpdateVm { get; set; } = new WindowInfoUpdateVm();
	public SettingPageVm SettingPageVm { get; set; } = new SettingPageVm();
}