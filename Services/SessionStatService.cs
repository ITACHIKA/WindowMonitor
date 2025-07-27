using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Documents;
using Avalonia.Media.Imaging;
using Microsoft.Data.Sqlite;
using windowLogger.Models;

namespace windowLogger.Services;

public static class SessionStatService
{
    private const string DbPath = "./recdata.db";
    private const string DbConnCommand = $"Data Source={DbPath}";
    private static Dictionary<ulong, AppSessionData> AppSessionCollection { get; set; } = new();
    private static Dictionary<string, List<(ulong,string)>> AppNameToHashPath { get; set; } = new();
    
    private static Channel<Func<Task>> _writeQueue = Channel.CreateUnbounded<Func<Task>>();

    static SessionStatService()
    {
        _ = WriteTaskProcessor();
    }

    private static void AddWriteTask(Func<Task> writeTask)
    {
        _writeQueue.Writer.TryWrite(writeTask);
    }

    private static async Task WriteTaskProcessor()
    {
        await foreach (var writeTask in _writeQueue.Reader.ReadAllAsync())
        {
            Console.WriteLine("Receive write task");
            for (var attempt = 0;; attempt++)
            {
                try
                {
                    await writeTask();
                    break;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode==5)
                {
                    if (attempt > 3)
                    {
                        throw;
                    }
                    Console.WriteLine("sqlite db lock, retry count="+attempt.ToString());
                    await Task.Delay(attempt * 10);
                }
            }
        }
    }
    
    private static void EnableDbWAL()
    {
        using var db = new SqliteConnection(DbConnCommand);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL";
        var result = cmd.ExecuteScalar();
        if (!string.Equals(result?.ToString(), "wal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Failed to enable WAL mode.");
        }
    }
    public static void InitRecord()
    {
        if (File.Exists(DbPath))
        {
            // Console.WriteLine("Found");
            try
            {
                EnableDbWAL();
                using var DbConnection = new SqliteConnection(DbConnCommand);
                DbConnection.Open();
                using var readerCommand = DbConnection.CreateCommand();
                readerCommand.CommandText = "SELECT * FROM SessionDataCollection;";
                var reader = readerCommand.ExecuteReader();
                while (reader.Read())
                {
                    string RawAppHash = reader.GetString(0);
                    string RawAppName = reader.GetString(1);
                    string RawAppPath = reader.GetString(2);
                    string RawDataJson = reader.GetString(3);
                    ulong AppHash = ulong.Parse(RawAppHash);
                    AppSessionData RowSessionData = JsonSerializer.Deserialize<AppSessionData>(RawDataJson)!;
                    AppSessionCollection.Add(AppHash, RowSessionData);
                    try
                    {
                        AppNameToHashPath.Add(RawAppName, new List<(ulong, string)> { (AppHash, RawAppPath) });
                    }
                    catch (ArgumentException)
                    {
                        AppNameToHashPath[RawAppName].Add((AppHash, RawAppPath));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Stat Service Init Err:" + e);
            }
        }
        else
        {
            //Console.WriteLine("Not Found");
            try
            {
                using var DbConnection = new SqliteConnection(DbConnCommand);
                DbConnection.Open();
                using var DbInitCommand = DbConnection.CreateCommand();
                DbInitCommand.CommandText = @"
                CREATE TABLE SessionDataCollection (
                    AppHash TEXT PRIMARY KEY,
                    AppName TEXT,
                    AppPath TEXT,
                    DataJson TEXT
                );";
                DbInitCommand.ExecuteNonQuery();
                EnableDbWAL();
            }
            catch (Exception e)
            {
                Console.WriteLine("sqlite init err:" + e);
            }
        }
    }

    public static async Task AddRecord(string appName,string appPath, ulong appHash, AppSingleSessionData appSingleSessionData)
    {
        if (AppSessionCollection.TryGetValue(appHash, out var appSessionData))
        {
            //Console.WriteLine("Record Exist in DB");
            appSessionData.SingleTimeDataList.Add(appSingleSessionData);
            await using var DbConnection = new SqliteConnection(DbConnCommand);
            await DbConnection.OpenAsync();
            await using var DbReadRowCommand = DbConnection.CreateCommand();
            DbReadRowCommand.CommandText = @"
                SELECT * FROM SessionDataCollection WHERE AppHash = @appHash;";
            DbReadRowCommand.Parameters.AddWithValue("@appHash", appHash.ToString());
            var reader = await DbReadRowCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string RawAppHash = reader.GetString(0);
                string RawDataJson = reader.GetString(3);
                // Console.WriteLine(RawAppName);
                // Console.WriteLine(RawAppHash);
                // Console.WriteLine(RawDataJson);
                AddWriteTask(async () =>
                {
                    await using var writeConn = new SqliteConnection(DbConnCommand);
                    await writeConn.OpenAsync();
                    // AppSessionData RowSessionData = JsonSerializer.Deserialize<AppSessionData>(RawDataJson)!; // read the existing record
                    // RowSessionData.SingleTimeDataList.Add(appSingleSessionData); // add a recorded session to list
                    await using var updateCmd = writeConn.CreateCommand();
                    updateCmd.CommandText = @"UPDATE SessionDataCollection SET DataJson=@dataJson WHERE AppHash = @appHash;";
                    updateCmd.Parameters.AddWithValue("@appHash", RawAppHash);
                    updateCmd.Parameters.AddWithValue("@dataJson", JsonSerializer.Serialize(AppSessionCollection[appHash]));
                    await updateCmd.ExecuteNonQueryAsync();
                });

            }
        }
        else
        {
            //Console.WriteLine("Not Found in DB");
            var temp = new AppSessionData(appSingleSessionData);
            AppSessionCollection.Add(appHash, temp);
            try
            {
                AppNameToHashPath.Add(appName, new List<(ulong, string)> { (appHash, appPath) });
            }
            catch (ArgumentException)
            {
                AppNameToHashPath[appName].Add((appHash, appPath));
            }
            AddWriteTask(async () =>
            {
                await using var DbConnection = new SqliteConnection(DbConnCommand);
                await DbConnection.OpenAsync();
                await using var DbAddRowCommand = DbConnection.CreateCommand();
                DbAddRowCommand.CommandText = @"
                INSERT INTO SessionDataCollection (AppHash, AppName,AppPath, DataJson) VALUES (@appHash,@appName,@appPath,@dataJson);";
                DbAddRowCommand.Parameters.AddWithValue("@appName", appName);
                DbAddRowCommand.Parameters.AddWithValue("@appHash", appHash.ToString());
                DbAddRowCommand.Parameters.AddWithValue("@appPath", appPath);
                DbAddRowCommand.Parameters.AddWithValue("@dataJson", JsonSerializer.Serialize(temp));
                await DbAddRowCommand.ExecuteNonQueryAsync();
            });
        }
    }
    public static async Task DelRecord()
    {
        await Task.Run(async () =>
        {
            await using var DbConnection = new SqliteConnection(DbConnCommand);
            DbConnection.Open();
            await using var DbRemoveAllCommand = DbConnection.CreateCommand();
            DbRemoveAllCommand.CommandText = @"DELETE FROM SessionDataCollection;";
            await DbRemoveAllCommand.ExecuteNonQueryAsync();
            AppSessionCollection.Clear();
            AppNameToHashPath.Clear();
        });
    }

    public static List<InquiredAppSessionData> AppDataInquireByName(string appName)
    {
        var MatchAppSessionData = new List<InquiredAppSessionData>();
        var DictSearchResult = AppNameToHashPath.Where(kvp => kvp.Key.Contains(appName,StringComparison.OrdinalIgnoreCase));
        foreach (var (AppName, ValueList) in DictSearchResult)
        {
            foreach (var appRecordSameName in ValueList)
            {
                var AppHash = appRecordSameName.Item1;
                var AppPath = appRecordSameName.Item2;
                MatchAppSessionData.Add(new InquiredAppSessionData(AppName, AppPath,AppHash, AppSessionCollection[AppHash]));
            }

        }
        return MatchAppSessionData;
    }

    public static (Dictionary<ulong, AppSessionData>,Dictionary<string, List<(ulong,string)>>) GetFullData()
    {
        return (AppSessionCollection,AppNameToHashPath);
    }

    private static Dictionary<ulong,Bitmap?> _imageCache = new();
    private const string CachePath = "cache/";
    public static async Task<Bitmap> LoadImage(ulong appHash)
    {
        if (_imageCache.TryGetValue(appHash, out var image))
        {
            return image!; // doesn't matter if app has no icon
        }
        var path = CachePath + appHash.ToString();
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
        _imageCache.Add(appHash,AppIcon);
        return AppIcon!;
    }
}