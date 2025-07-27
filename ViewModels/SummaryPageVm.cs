using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using ReactiveUI;
using windowLogger.Services;
using windowLogger.Models;

namespace windowLogger.ViewModels;

public class AppSessionSummaryData
{
	public string SummaryAppName { get; set; }
	public string SummaryAppHash { get; set; }
	public string SummaryAppPath { get; set; }
	public Bitmap SummaryAppIcon { get; set; }
	public TimeSpan AppTotalRunTime { get; set; }
	public AppSessionSummaryData(string appName, string appPath, ulong appHash, TimeSpan appTotalRunTime,Bitmap appIcon)
	{
		SummaryAppName = appName;
		SummaryAppPath = appPath;
		SummaryAppHash = appHash.ToString();
		AppTotalRunTime = appTotalRunTime;
		SummaryAppIcon = appIcon;
	}
}
public class SummaryPageVm : ReactiveObject
{
	public List<string> IgnoredAppList { get; set; } = ["LockApp"];
	private static Dictionary<ulong, AppSessionData> AppSessionCollection { get; set; }  = new();
	private static Dictionary<string, List<(ulong, string)>> AppNameToHashPath { get; set; } = new();

	public ObservableCollection<string> PeriodList { get; set; } = ["Last Week", "Last Month", "All times"];
	public ObservableCollection<AppSessionSummaryData> SummedAppStatistics { get; set; } = new();
	
	private ObservableCollection<AppSessionSummaryData> _sortedAppStatistics;

	public ObservableCollection<AppSessionSummaryData> SortedAppStatistics
	{
		get => _sortedAppStatistics;
		set => this.RaiseAndSetIfChanged(ref _sortedAppStatistics, value);
	}
	
	private string? _selectedPeriod;

	public string? SelectedPeriod
	{
		get => _selectedPeriod;
		set => this.RaiseAndSetIfChanged(ref _selectedPeriod, value);
	}
	
	public ReactiveCommand<Unit, Unit> SummarizeCommand { get; }
	public SummaryPageVm()
	{
		var fullDataset = SessionStatService.GetFullData();
		AppSessionCollection = fullDataset.Item1;
		AppNameToHashPath = fullDataset.Item2;
		var canExec = this.WhenAnyValue(x => x.SelectedPeriod).Select(selected => !string.IsNullOrEmpty(selected));
		SummarizeCommand = ReactiveCommand.Create(SummarizeGivenPeriod,canExec);
	}

	private async void SummarizeGivenPeriod()
	{
		SummedAppStatistics.Clear();
		Console.WriteLine("Summarizing given period");
		DateTime? periodStartTime = DateTime.MinValue;
		switch (SelectedPeriod)
		{
			case "Last Week": periodStartTime = DateTime.Now.AddDays(-7); break;
			case "Last Month": periodStartTime = DateTime.Now.AddMonths(-1); break;
		}

		foreach (KeyValuePair<string, List<(ulong, string)>> pair in AppNameToHashPath)
		{
			var AppName = pair.Key;
			foreach (var singleAppRecordWithSameName in pair.Value)
			{
				var AppHash = singleAppRecordWithSameName.Item1;
				var AppPath = singleAppRecordWithSameName.Item2;
			
				TimeSpan totalRunTime = TimeSpan.Zero;
				var AppSessionData = AppSessionCollection[AppHash];
				foreach (var singleRecord in AppSessionData.SingleTimeDataList)
				{
					if (singleRecord.StartTime >= periodStartTime)
					{
						totalRunTime+=singleRecord.EndTime-singleRecord.StartTime;
					}
				}
				SummedAppStatistics.Add(new AppSessionSummaryData(AppName,AppPath,AppHash,totalRunTime,await SessionStatService.LoadImage(AppHash)));
			}
		}
		SortedAppStatistics = new ObservableCollection<AppSessionSummaryData>(SummedAppStatistics.OrderByDescending(app => app.AppTotalRunTime));
	}
}