using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using windowLogger.ViewModels;

namespace windowLogger.Views;

public partial class SettingWindow : ReactiveWindow<SettingPageVm>
{
    public SettingWindow()
    {
        InitializeComponent();
        DataContext = App.MainVm;

        this.WhenActivated(disposables =>
        {
            App.MainVm.SettingPageVm.UnacceptedNumbInputMsg.RegisterHandler(async interaction =>
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", "Number input not accepted", ButtonEnum.Ok);
                var result = await box.ShowAsPopupAsync(this);
                interaction.SetOutput(Unit.Default);
            }).DisposeWith(disposables);
        });

        this.Width = 300;
        this.Height = 250;
        this.CanResize = false;
        this.Closing += (s, e) =>
        {
            ((Window)s!).Hide();
            e.Cancel = true;
        };
        this.Hide();
    }

    public sealed override void Hide()
    {
        base.Hide();
    }
}