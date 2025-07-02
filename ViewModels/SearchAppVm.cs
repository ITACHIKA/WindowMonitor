using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Media.Imaging;
using windowLogger.Services;
using ReactiveUI;
using windowLogger.Models;

namespace windowLogger.ViewModels;

public class CardViewModel
{
	public Bitmap? CardAppIcon { get; set; }
	public string CardAppName { get; set; } = "";
	public string CardAppPath { get; set; } = "";
	public int CardAppIndex { get; set; } = -1;

	public CardViewModel()
	{
		
	}

	public CardViewModel(Bitmap? cardAppIcon, string cardAppName, string cardAppPath,int cardAppIndex)
	{
		this.CardAppIcon = cardAppIcon;
		this.CardAppName = cardAppName;
		this.CardAppPath = cardAppPath;
		this.CardAppIndex = cardAppIndex;
	}
}
 
public class SearchAppVm : ReactiveObject
{
	public static ReactiveCommand<string?, Unit>? SearchCommand { get; set; }
	public ObservableCollection<CardViewModel> SearchResultCardCollection { get; set; } = [];
	public List<InquiredAppSessionData>? InquiryResult { get; set; } = [];

	public SearchAppVm()
	{
		SearchCommand = ReactiveCommand.CreateFromTask<string?>(async inquiryAppName =>
		{
			SearchResultCardCollection.Clear();
			if (inquiryAppName != null)
			{
				InquiryResult = SessionStatService.AppDataInquireByName(inquiryAppName);
				var count = 0;
				foreach (var item in InquiryResult)
				{
					var appIcon = await SessionStatService.LoadImage(item.AppHash);
					SearchResultCardCollection.Add(new CardViewModel(appIcon, item.AppName, item.AppPath,count));
					count++;
				}
			}
		});
	}
}