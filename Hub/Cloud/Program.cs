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
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices;
using Microsoft.Rest;

namespace Cloud
{
    /// <summary>Program</summary>
    public class Program
    {
        /// <summary>HubConnectionString</summary>
        private static string HubConnectionString = "";

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
            Option intOption = new Option<int>(
                alias: "--int-option",
                description: "An option whose argument is parsed as an int.",
                getDefaultValue: () => 42);
            Option stringOption = new Option<string>(
                "--string-option",
                "An option whose argument is parsed as a string.");

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
            IEnumerable<Twin> devices = null;
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(HubConnectionString);
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(HubConnectionString);

            switch (value)
            {
                case EnumMenu.SendC2D:
                    // 送信
                    devices = await GetDeviceIdAsync(registryManager);
                    await SendC2DAsync(serviceClient, devices);
                    break;

                case EnumMenu.UpdateTwinTags:
                    // 初期化
                    devices = await GetDeviceIdAsync(registryManager);
                    await UpdateTwinTagsAsync(registryManager, devices);
                    break;

                case EnumMenu.QueryTwinTags:
                    // 更新確認
                    await QueryTwinTagsAsync(registryManager);
                    break;

                default:
                    break;
            }

            // 終了処理
            await serviceClient.CloseAsync();
            await registryManager.CloseAsync();
        }
        #endregion

        #region Methods

        /// <summary>GetDeviceIdAsync</summary>
        /// <param name="registryManager">RegistryManager</param>
        /// <returns>IEnumerable(Twin)</returns>
        private static async Task<IEnumerable<Twin>> GetDeviceIdAsync(RegistryManager registryManager)
        {
            var query = registryManager.CreateQuery("SELECT * FROM devices", 100);

            if (query.HasMoreResults)
            {
                IEnumerable<Twin> devices = await query.GetNextAsTwinAsync();
                Console.WriteLine("Devices : {0}", string.Join(", ", devices.Select(t => t.DeviceId)));
                return devices;
            }
            else
            {
                return null;
            }
        }

        /// <summary>SendC2DAsync</summary>
        /// <param name="deviceClient">ServiceClient</param>
        /// <param name="devices">IEnumerable(Twin)</param>
        /// <returns>Task</returns>
        private static async Task SendC2DAsync(
            ServiceClient serviceClient, IEnumerable<Twin> devices)
        {
            // メッセージ生成
            Message message = new Message(Encoding.ASCII.GetBytes(
                String.Format("Cloud to device message: {0}", DateTime.Now.Ticks)));

            // メッセージ確認レベル
            message.Ack = DeliveryAcknowledgement.Full;

            // message.Properties // 読み取り専用

            // メッセージ配信
            foreach (Twin twin in devices)
            {
                await serviceClient.SendAsync(twin.DeviceId, message);
            }
        }

        /// <summary>UpdateTwinTagsAsync</summary>
        /// <param name="registryManager">RegistryManager</param>
        /// <returns>Task</returns>
        public static async Task UpdateTwinTagsAsync(
            RegistryManager registryManager, IEnumerable<Twin> devices)
        {
            foreach (Twin twin in devices)
            {
                string patch =
                    @"{
                        tags: {
                            location: {
                                region: 'US',
                                plant: 'Redmond43'
                            }
                        }
                    }";

                await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);
            }
        }

        /// <summary>QueryTwinTagsAsync</summary>
        /// <param name="registryManager">RegistryManager</param>
        /// <returns>Task</returns>
        public static async Task QueryTwinTagsAsync(RegistryManager registryManager)
        {
            // Redmond43に居るDeviceの一覧
            IQuery query = registryManager.CreateQuery(
                "SELECT * FROM devices WHERE tags.location.plant = 'Redmond43'", 100);

            IEnumerable<Twin> devices = await query.GetNextAsTwinAsync();

            Console.WriteLine("Devices in Redmond43: {0}",
              string.Join(", ", devices.Select(t => t.DeviceId)));

            // Redmond43に居るDeviceのうちcellularになっているDeviceの一覧
            query = registryManager.CreateQuery(
                "SELECT * FROM devices WHERE tags.location.plant = 'Redmond43' AND properties.reported.connectivity.type = 'cellular'", 100);

            devices = await query.GetNextAsTwinAsync();

            Console.WriteLine("Devices in Redmond43 using cellular network: {0}",
              string.Join(", ", devices.Select(t => t.DeviceId)));
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
            SendC2D,
            UpdateTwinTags,
            QueryTwinTags
        }
        #endregion
    }
}
