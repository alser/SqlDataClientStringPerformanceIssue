using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace SqlDataClientStringPerformanceIssue
{
    public static class Program
    {
        public async static Task Main()
        {
            // generating ~ 10 MB of text
            string text = LoremIpsum(15, 100_000);
            await Console.Out.WriteLineAsync("Text length: " + text.Length.ToString("N0", CultureInfo.InvariantCulture));

            await using var connection = SqlClientFactory.Instance.CreateConnection() ?? throw new InvalidOperationException("No connection");
            connection.ConnectionString = "Server=.;Database=master;User ID=sa;Password=Master1234";

            await connection.OpenAsync();

            // drop table LargeText if exists
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "IF EXISTS (SELECT * FROM [sysobjects] WHERE [name]='LargeText' AND [xtype]='U') DROP TABLE [LargeText]";
                await command.ExecuteNonQueryAsync();
            }

            // create table LargeText
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE [LargeText] ([ID] uniqueidentifier NOT NULL PRIMARY KEY CLUSTERED, [Text] nvarchar(max) NULL)";
                await command.ExecuteNonQueryAsync();
            }

            // insert generated text into LargeText
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO [LargeText] ([ID], [Text]) VALUES (NEWID(), @Text)";

                DbParameter textParameter = command.CreateParameter();
                textParameter.ParameterName = "Text";
                textParameter.DbType = DbType.String;
                textParameter.Value = text;
                command.Parameters.Add(textParameter);

                await command.ExecuteNonQueryAsync();
            }

            // select inserted text and measure time
            await using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT [Text] FROM [LargeText]";

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string selectedText = await selectCommand.ExecuteScalarAsync() as string;
            stopwatch.Stop();

            await Console.Out.WriteLineAsync("Selected text length: " + (selectedText?.Length ?? -1).ToString("N0", CultureInfo.InvariantCulture));
            await Console.Out.WriteLineAsync("Elapsed: " + stopwatch.Elapsed);
        }

        private static string LoremIpsum(int numWords, int numSentences)
        {
            var words = new[]
            {
                "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer",
                "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod",
                "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"
            };

            var random = new Random();
            var result = new StringBuilder();

            for (int i = 0; i < numSentences; i++)
            {
                for (int j = 0; j < numWords; j++)
                {
                    if (j > 0)
                    {
                        result.Append(' ');
                    }

                    result.Append(words[random.Next(words.Length)]);
                }

                result.Append(". ");
            }

            return result.ToString();
        }
    }
}