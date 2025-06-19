using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Tmds.DBus.Protocol;
using windowLogger.Models;

namespace windowLogger.Services;

public static class SessionStatService
{
    private const string DbPath = "./recdata.db";
    private const string DbConnCommand = $"Data Source={DbPath}";
    private static Dictionary<ulong, AppSessionData> AppSessionCollection { get; set; } = new();
    private static Dictionary<string, (ulong,string)> AppNameToHashPath { get; set; } = new();
    public static void InitRecord()
    {
        if (File.Exists(DbPath))
        {
            // Console.WriteLine("Found");
            try
            {
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
                    AppNameToHashPath.Add(RawAppName,(AppHash,RawAppPath));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Stat Service Init Err:" + e);
            }
        }
        else
        {
            Console.WriteLine("Not Found");
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
            }
            catch (Exception e)
            {
                Console.WriteLine("sqlite init err:" + e);
            }
        }
    }

    public static async Task AddRecord(string appName,string appPath, ulong appHash, AppSingleSessionData aSSD)
    {
        await Task.Run(async () =>
        {
            if (AppSessionCollection.TryGetValue(appHash, out var appSessionData))
            {
                // Console.WriteLine("Record Exist in DB");
                appSessionData.SingleTimeDataList.Add(aSSD);
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
                    string RawAppName = reader.GetString(1);
                    string RawAppPath = reader.GetString(2);
                    string RawDataJson = reader.GetString(3);
                    // Console.WriteLine(RawAppName);
                    // Console.WriteLine(RawAppHash);
                    // Console.WriteLine(RawDataJson);

                    AppSessionData RowSessionData = JsonSerializer.Deserialize<AppSessionData>(RawDataJson)!; // read the existing record
                    RowSessionData.SingleTimeDataList.Add(aSSD); // add a recorded session to list
                    await using var deleteCmd = DbConnection.CreateCommand();
                    deleteCmd.CommandText = "DELETE FROM SessionDataCollection WHERE AppHash = @appHash;";
                    deleteCmd.Parameters.AddWithValue("@appHash", RawAppHash);
                    await deleteCmd.ExecuteNonQueryAsync(); // delete row before re-inserting the updated one

                    var DbWriteBackCommand = DbConnection.CreateCommand();
                    DbWriteBackCommand.CommandText = @"
                    INSERT INTO SessionDataCollection (AppHash, AppName,AppPath, DataJson) VALUES (@appHash,@appName,@appPath,@dataJson);";
                    DbWriteBackCommand.Parameters.AddWithValue("@appName", RawAppName);
                    DbWriteBackCommand.Parameters.AddWithValue("@appHash", RawAppHash);
                    DbWriteBackCommand.Parameters.AddWithValue("@appPath", RawAppPath);
                    DbWriteBackCommand.Parameters.AddWithValue("@dataJson", JsonSerializer.Serialize(RowSessionData));
                    await DbWriteBackCommand.ExecuteNonQueryAsync();
                }
            }
            else
            {
                var temp = new AppSessionData(aSSD);
                AppSessionCollection.Add(appHash, temp);
                AppNameToHashPath.Add(appName, (appHash, appPath));
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
            }
        });
    }
    public static async Task DelRecord()
    {
        await Task.Run(async () =>
        {
            using var DbConnection = new SqliteConnection(DbConnCommand);
            DbConnection.Open();
            using var DbRemoveAllCommand = DbConnection.CreateCommand();
            DbRemoveAllCommand.CommandText = @"DELETE FROM SessionDataCollection;";
            await DbRemoveAllCommand.ExecuteNonQueryAsync();
            AppSessionCollection = [];
        });
    }

    public static List<InquiredAppSessionData> AppDataInquire(string appName)
    {
        var MatchAppSessionData = new List<InquiredAppSessionData>();
        var DictSearchResult = AppNameToHashPath.Where(kvp => kvp.Key.Contains(appName));
        foreach (var (AppName, ValueTuple) in DictSearchResult)
        {
            var AppHash = ValueTuple.Item1;
            var AppPath = ValueTuple.Item2;
            MatchAppSessionData.Add(new InquiredAppSessionData(AppName, AppPath,AppHash, AppSessionCollection[AppHash]));
        }
        return MatchAppSessionData;
    }
}