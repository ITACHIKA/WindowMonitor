using System;
using System.Collections.Generic;
using System.IO;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace windowLogger.ViewModels;

public partial class SettingPageVm : ReactiveObject
{
	[LibraryImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool AllocConsole();
	
	private Dictionary<string, string?> _configFile = new();
	public Interaction<Unit, Unit> UnacceptedNumbInputMsg { get; } = new();
	private const string CfgPath = "config.json";

	private string? _minRecSec;
	private string? _capIntv;
	private bool _useDebugConsoleIsChecked;

	public string? MinRecSec
	{
		get => _minRecSec;
		set => this.RaiseAndSetIfChanged(ref _minRecSec, value);
	}

	public string? CapIntv
	{
		get => _capIntv;
		set => this.RaiseAndSetIfChanged(ref _capIntv, value);
	}

	public bool UseDebugConsoleIsChecked
	{
		get => _useDebugConsoleIsChecked;
		set => this.RaiseAndSetIfChanged(ref _useDebugConsoleIsChecked, value);
	}

	public SettingPageVm()
	{
		LoadSettingOnInit();
	}

	public async void SaveConfigOnClick()
	{
		try
		{
			WindowInfoUpdateVm.MinimumRecordSessionSpan = int.Parse(MinRecSec ?? "a");
			WindowInfoUpdateVm.CaptureInterval = int.Parse(CapIntv ?? "a");
		}
		catch (FormatException)
		{
			Console.WriteLine("Format error");
			MinRecSec = _configFile["MinRecSec"];
			CapIntv = _configFile["CaptureInterval"];
			await UnacceptedNumbInputMsg.Handle(default(Unit));
			return;
		}
		catch (Exception e)
		{
			Console.WriteLine("save error:" + e);
			return;
		}


		try
		{
			_configFile["MinRecSec"] = MinRecSec!; // text will not be null here
			_configFile["CaptureInterval"] = CapIntv!;
			_configFile["UseDebugConsole"] = UseDebugConsoleIsChecked.ToString();
			var js = JsonSerializer.Serialize(_configFile);
			await File.WriteAllTextAsync(CfgPath, js);
		}
		catch (Exception e)
		{
			Console.WriteLine("js write error:" + e);
		}
	}

	private void LoadSettingOnInit()
	{
		Console.WriteLine("Loading setting");
		if (File.Exists(CfgPath))
		{
			Console.WriteLine("Found setting");
			try
			{
				var cfg = File.ReadAllText(CfgPath);
				_configFile = JsonSerializer.Deserialize<Dictionary<string, string>>(cfg)!;
			}
			catch
			{
				using (File.Create(CfgPath))
				{
				}

				_configFile.Add("UseDebugConsole", "True");
				_configFile.Add("MinRecSec", "0");
				_configFile.Add("CaptureInterval", "100");
				var js = JsonSerializer.Serialize(_configFile);
				File.WriteAllText(CfgPath, js);
			}
		}
		else
		{
			using (File.Create(CfgPath))
			{
			}

			_configFile.Add("UseDebugConsole", "True");
			_configFile.Add("MinRecSec", "0");
			_configFile.Add("CaptureInterval", "100");
			var cfg = JsonSerializer.Serialize(_configFile);
			File.WriteAllText(CfgPath, cfg);
		}
		
		if (_configFile["UseDebugConsole"] == "True")
		{
			AllocConsole();
			UseDebugConsoleIsChecked = true;
		}

		WindowInfoUpdateVm.MinimumRecordSessionSpan = int.Parse(_configFile["MinRecSec"]!);
		WindowInfoUpdateVm.CaptureInterval = int.Parse(_configFile["CaptureInterval"]!);
		MinRecSec = _configFile["MinRecSec"];
		CapIntv = _configFile["CaptureInterval"];
	}
}