using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using windowLogger.ViewModels;

namespace windowLogger.Views;

public partial class MainWindow : Window
{
    private SettingWindow? SettingWindow{ get; set; }
    private DesignViewVm? DesignVm { get; set; }
    public List<ulong> OpenedWindowHashList { get; set; }
    private Boolean _firstOpen = true;
    public MainWindow()
    {
        InitializeComponent();
        this.Width = 800;
        this.Height = 500;
        this.Closing += (s, e) =>
        {
            ((Window)s!).Hide();
            e.Cancel = true;
        };
        this.Opened += (_, __) =>
        {
            if (_firstOpen)
            {
                _firstOpen = false;
                this.Hide();
            }
        };
        if (!Design.IsDesignMode)
        {
            SettingWindow = new SettingWindow();
            WindowInfoUpdateVm.InitRecord();
            DataContext = App.MainVm;
            App.MainVm.WindowInfoUpdateVm.StartTimer();
            ClearHis.Click += ClearHisOnClick;
            ClearCac.Click += ClearCacOnClick;
            ViewSet.Click += ViewSetOnClick;
            RecStart.Click += RecStartOnClick;
            RecStop.Click += RecStopOnClick;
            this.SizeChanged += Window_SizeChanged;
            SearchTextBox.GotFocus += SearchTextBoxOnFocus;
            SearchTextBox.LostFocus += SearchTextBoxLostFocus;
            SearchTextBox.KeyDown += SearchTextBoxOnKeyDown;
            TotalSummaryButton.Click += TotalSummaryButtonOnClick;
        }
        else
        {
            DesignVm = new DesignViewVm();
            DataContext = DesignVm;
            Console.WriteLine("Design mode");
        }
    }

    public sealed override void Hide()
    {
        base.Hide();
    }

    private void TotalSummaryButtonOnClick(object sender, RoutedEventArgs e)
    {
        var SummaryWindow = new SummaryPageView();
    }
    private void SearchResultCardOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CardViewModel card })
        {
            Console.WriteLine(card.CardAppIndex);
            var CardDetailWindow = new AppInfoDetailWindow(card.CardAppIndex);
        }
    }
    private async void SearchTextBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key != Key.Enter) return;
            var input = SearchTextBox.Text ?? "";
            await SearchAppVm.SearchCommand!.Execute(input);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void Window_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var sideLen = Math.Min(Math.Min(256, this.Bounds.Width / 3), this.Bounds.Height / 3);
        AppIcon.Width = sideLen;
        AppIcon.Height = sideLen;
    }
    private static async void ClearHisOnClick(object? sender, RoutedEventArgs a)
    {
        try
        {
            await WindowInfoUpdateVm.DeleteRecord();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    private static void ClearCacOnClick(object? sender, RoutedEventArgs a)
    {
        WindowInfoUpdateVm.DeleteCache();
    }
    private void ViewSetOnClick(object? sender, RoutedEventArgs a)
    {
        SettingWindow?.Show();
    }

    private void RecStartOnClick(object? sender, RoutedEventArgs a)
    {
        App.MainVm?.WindowInfoUpdateVm.StartTimer();
    }
    private void RecStopOnClick(object? sender, RoutedEventArgs a)
    {
        App.MainVm?.WindowInfoUpdateVm.StopTimer();
    }

    private void SearchTextBoxOnFocus(object? sender, RoutedEventArgs a)
    {
        SearchTextBox.Text = "";
    }
    private void SearchTextBoxLostFocus(object? sender, RoutedEventArgs a)
    {
        SearchTextBox.Text = "Search for record";
    }
}