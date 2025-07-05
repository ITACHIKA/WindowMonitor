using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using windowLogger.Services;

namespace windowLogger.Views;

public partial class AppInfoDetailWindow : Window , INotifyPropertyChanged
{
	public new event PropertyChangedEventHandler? PropertyChanged;
	private static List<ulong> _openedWindow = [];
	public ulong AppHash { get; set; }
	public string AppPath { get; set; }
	public string AppName { get; set; }
	public Bitmap AppIcon { get; set; }
	public TimeSpan AppTotalRuntime { get; set; }
	
	public string AppTotalRuntimeString => AppTotalRuntime.ToString();
	public string AppHashString { get; set; }
	public AppInfoDetailWindow(int referenceIndex)
	{
		InitializeComponent();
		this.Width = 640;
		this.Height = 400;
		DataContext = this;
		var currentApp = App.MainVm.SearchAppVm.InquiryResult![referenceIndex];
		AppHash = currentApp.AppHash;
		if(_openedWindow.Contains(AppHash))
		{
			this.Close();
			return;
		}
		_openedWindow.Add(AppHash);
		AppHashString = currentApp.AppHash.ToString();
		AppName = currentApp.AppName;
		AppPath = currentApp.AppPath;
		LoadIcon(AppHash);
		var AppHistory = currentApp.AppSessionData;
		foreach (var sePair in AppHistory.SingleTimeDataList)
		{
			AppTotalRuntime += sePair.EndTime-sePair.StartTime;
		}
		this.Closing+=(s, e) =>
		{
			_openedWindow.Remove(AppHash);
		};
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
		this.Show();
	}
	public sealed override void Show()
	{
		base.Show();
	}

	private async void LoadIcon(ulong ah)
	{
		AppIcon = await SessionStatService.LoadImage(ah);
	}
}