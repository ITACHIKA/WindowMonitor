using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using windowLogger.Services;
using ReactiveUI;

namespace windowLogger.ViewModels;

public class CardViewModel
{
	public Bitmap? CardAppIcon { get; set; }
	public string? CardAppName { get; set; }
	public string? CardAppPath { get; set; }

	public CardViewModel()
	{
		
	}

	public CardViewModel(Bitmap? cardAppIcon, string? cardAppName, string? cardAppPath)
	{
		this.CardAppIcon = cardAppIcon;
		this.CardAppName = cardAppName;
		this.CardAppPath = cardAppPath;
	}
}

public class SearchAppVm : ReactiveObject
{
	private const string CachePath = "cache/";
	public static ReactiveCommand<string?, Unit>? SearchCommand { get; set; }
	public ObservableCollection<CardViewModel> SearchResultCardCollection { get; set; } = [];

	public SearchAppVm()
	{
		SearchCommand = ReactiveCommand.CreateFromTask<string?>(async inquiryAppName =>
		{
			SearchResultCardCollection.Clear();
			if (inquiryAppName != null)
			{
				var InquiryResult = SessionStatService.AppDataInquire(inquiryAppName);
				foreach (var item in InquiryResult)
				{
					var appIcon = await LoadImg(item.AppHash);
					SearchResultCardCollection.Add(new CardViewModel(appIcon, item.AppName, item.AppPath));
				}
			}
		});
	}
	private async Task<Bitmap> LoadImg(ulong hash)
	{
		var path = CachePath + hash.ToString();
		Bitmap? AppIcon = null;
		try
		{
			await using var png = File.OpenRead(path);
			AppIcon = new Bitmap(png);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}

		return AppIcon;
	}
}