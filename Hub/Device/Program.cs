//**********************************************************************************
//* 単純CLIサンプル アプリ
//**********************************************************************************

// テスト用サンプルなので、必要に応じて流用 or 削除して下さい。

//**********************************************************************************
//* クラス名        ：Program
//* クラス日本語名  ：単純CLIサンプル アプリ
//*
//* 作成日時        ：－
//* 作成者          ：開発基盤部会
//* 更新履歴        ：
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  20xx/xx/xx  ＸＸ ＸＸ         ＸＸＸＸ
//**********************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Threading;
using System.Diagnostics;

using System.CommandLine;
using System.CommandLine.Invocation;

using Sharprompt;

using Newtonsoft;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace Device
{
    /// <summary>Program</summary>
    public class Program
    {
        /// <summary>deviceConnectionString</summary>
        private static string deviceConnectionString = "";

        /// <summary>
        /// 'async main' は C# 7.1 以上の言語バージョンが必要（→ VS 2019）。
        /// </summary>
        /// <param name="args">string[]</param>
        /// <returns>Task<(int)</returns>
        static async Task<int> Main(string[] args)
        {
            #region Create a root command
            Command rootCommand = new RootCommand("My Device app");

            // with some options
            Option intOption = new Option<int>
                (alias: "--int-option",
                description: "An option whose argument is parsed as an int.",
                getDefaultValue: () => 42);
            Option stringOption = new Option<string>
                ("--string-option", "An option whose argument is parsed as a string.");

            rootCommand.Add(intOption);
            rootCommand.Add(stringOption);

            // with some arguments
            /*
            Argument intArgument = new Argument<int>
                (name: "intArgument",
                description: "An argument that is parsed as an int.",
                getDefaultValue: () => 42);
            Argument stringArgument = new Argument<string>
                ("stringArgument", "An argument that is parsed as a string.");

            rootCommand.Add(intArgument);
            rootCommand.Add(stringArgument);
            */

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler
                .Create<int, string, int, string, IConsole, CancellationToken>(Program.RootCommand);
            #endregion

            // テストの実行
            await Program.Test(rootCommand);

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }

        #region Command
        /// <summary>RootCommand</summary>
        /// <param name="intOption"></param>
        /// <param name="stringOption"></param>
        /// <param name="intArgument"></param>
        /// <param name="stringArgument"></param>
        /// <param name="console">IConsole</param>
        /// <param name="token">CancellationToken</param>
        private static async Task RootCommand(
            int intOption, string stringOption,
            int intArgument, string stringArgument,
            IConsole console, CancellationToken token)
        {
            Console.WriteLine("command interactive (Ctrl-C terminate)");

            Prompt.ColorSchema.Answer = ConsoleColor.DarkRed;
            Prompt.ColorSchema.Select = ConsoleColor.DarkCyan;
            Console.OutputEncoding = Encoding.UTF8;

            EnumMenu value = Prompt.Select<EnumMenu>("Select enum value");

            // 開始処理
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);

            switch (value)
            {
                case EnumMenu.SendD2C:
                    // 送信
                    await SendD2CAsync(deviceClient);
                    break;
                case EnumMenu.ReceiveC2D:
                    // 受信
                    await ReceiveC2DAsync(deviceClient);
                    break;
                case EnumMenu.UpdateTwinProperties:
                    // 受信
                    await UpdateTwinPropertiesAsync(deviceClient);
                    break;
                default:
                    break;
            }

            Console.WriteLine("Device simulator finished.");

            // 終了処理
            await deviceClient.CloseAsync();
            //await deviceClient.DisposeAsync();
        }
        #endregion

        #region Methods

        /// <summary>SendD2CAsync</summary>
        /// <param name="deviceClient">DeviceClient</param>
        /// <returns>Task</returns>
        private static async Task SendD2CAsync(DeviceClient deviceClient)
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };
            Console.WriteLine("Press control-C to exit.");

            // 送信処理
            int i = 0;
            while (!cts.IsCancellationRequested)
            {
                string messageBody = "counter : " + i;
                Message message = new Message(Encoding.ASCII.GetBytes(messageBody));

                // メッセージルーティングで使用する。
                message.Properties.Add("messageType", (i % 3 == 0) ? "maintenance" : "");

                await deviceClient.SendEventAsync(message);
                Console.WriteLine($"{DateTime.Now} > Sending message: {messageBody}");

                await Task.Delay(1000);
                i++;
            }
        }

        /// <summary>ReceiveC2DAsync</summary>
        /// <param name="deviceClient">DeviceClient</param>
        /// <returns>Task</returns>
        private static async Task ReceiveC2DAsync(DeviceClient deviceClient)
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };
            Console.WriteLine("Press control-C to exit.");

            // 受信処理
            while (!cts.IsCancellationRequested)
            {
                Message message = await deviceClient.ReceiveAsync(new TimeSpan(0, 3, 0));

                if (message != null)
                {
                    // 受信メッセージは黄色
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    // メッセージ
                    Console.WriteLine(
                        "Received message: {0}",
                        Encoding.ASCII.GetString(message.GetBytes()));

                    // メッセージ色をリセット
                    Console.ResetColor();

                    // Tx的な（受信したMSGをQueueから削除
                    await deviceClient.CompleteAsync(message);
                }
            }
        }

        /// <summary>UpdateTwinPropertiesAsync</summary>
        /// <param name="deviceClient">DeviceClient</param>
        /// <returns>Task</returns>
        public static async Task UpdateTwinPropertiesAsync(DeviceClient deviceClient)
        {
            Console.WriteLine("Sending connectivity data as reported property");

            await deviceClient.GetTwinAsync();
            TwinCollection reportedProperties, connectivity;
            reportedProperties = new TwinCollection();
            connectivity = new TwinCollection();
            connectivity["type"] = "cellular";
            reportedProperties["connectivity"] = connectivity;
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        #endregion

        #region Test
        /// <summary>Test</summary>
        /// <param name="rootCommand">Command</param>
        /// <returns>Task</returns>
        private static async Task Test(Command rootCommand)
        {
            // デバッグ実行時だけ実行
            if (!Debugger.IsAttached) return;

            // await rootCommand.InvokeAsync("--hoge");

            //  Device [options] [<intArgument> <stringArgument>]
            await rootCommand.InvokeAsync("");
        }
        #endregion

        #region SELECT

        /// <summary>EnumMenu</summary>
        private enum EnumMenu
        {
            SendD2C,
            ReceiveC2D,
            UpdateTwinProperties

        }
        #endregion
    }
}
