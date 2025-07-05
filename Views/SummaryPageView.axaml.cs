using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using windowLogger.ViewModels;

namespace windowLogger.Views;

public partial class SummaryPageView : ReactiveWindow<SummaryPageVm>
{
	private static bool _existInstance=false;
	public SummaryPageView()
	{
		if (_existInstance) return;
		_existInstance = true;
		InitializeComponent();
		this.DataContext = new SummaryPageVm();
		this.Width = 640;
		this.Height = 480;
		this.Closed += (_ ,_) => _existInstance = false;
		this.Show();
	}
}