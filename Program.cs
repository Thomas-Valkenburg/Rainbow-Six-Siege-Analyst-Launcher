// Copyright © 2022 Thomas Valkenburg

using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Rainbow_Six_Siege_Analyst
{
    internal class Program
    {
        private static readonly string ConfigFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Rainbow Six Siege Analyst Launcher\\config.json";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        private static void Main() => new Program().Run().GetAwaiter().GetResult();

        private async Task Run()
        {
            await ConfirmAndFixPaths();

            while (IsFileLocked(ConfigFilePath))
            {
                await Task.Delay(250);
            }

            var config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(ConfigFilePath));

            var overwolfPath = config!.OverwolfPath!;

            if (File.Exists(overwolfPath))
            {
                Process.Start(overwolfPath);
            }

            var rainbowSixPath = config.SiegePath!;

            try
            {
                Process.Start(rainbowSixPath);
            }
            catch { /* Do nothing*/ }

            await Task.Delay(120000);

            var processes = Process.GetProcesses().ToList();

            if (!processes.Exists(x => x.ProcessName == "RainbowSix")) return;

            var rainbowSixProcess = processes.Find(x => x.ProcessName == "RainbowSix")!;
            var overwolfProcess = processes.Find(x => x.ProcessName == "Overwolf")!;
            var ubisoftProcess = processes.Find(x => x.ProcessName == "upc")!;
            var processesToStop = new List<Process> {overwolfProcess, ubisoftProcess};

            await rainbowSixProcess.WaitForExitAsync().ContinueWith(_ =>
            {
                processesToStop.ForEach(x => x.Kill());
            });
        }

        private static bool IsFileLocked(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open);
                stream.Close();
            }
            catch (IOException)
            {
                // the file is unavailable because it is still being written to
                return true;
            }

            //file is not locked
            return false;
        }

        private async Task ConfirmAndFixPaths()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!Directory.Exists(documents + "\\Rainbow Six Siege Analyst Launcher"))
            {
                Directory.CreateDirectory(documents + "\\Rainbow Six Siege Analyst Launcher");
            }

            Config? config = null;

            if (File.Exists(ConfigFilePath))
            {
                config = JsonConvert.DeserializeObject<Config>(
                    await File.ReadAllTextAsync(ConfigFilePath));
            }

            config ??= new Config();

            await Task.Run(() =>
            {
                config.OverwolfPath ??= DisplayPopUp(0);
                config.SiegePath ??= DisplayPopUp(1);
            }).ContinueWith(async _ =>
            {
                await File.WriteAllTextAsync(ConfigFilePath, JsonConvert.SerializeObject(config));
                await Task.Delay(100);
            });
        }

        private string DisplayPopUp(int i)
        {
            var def = i == 0 ? "C:\\Program Files (x86)\\Overwolf\\Overwolf.exe" : "D:\\SteamLibrary\\steamapps\\common\\Tom Clancy's Rainbow Six Siege\\RainbowSix.exe";
            var path = i == 0 ? "Overwolf" : "Rainbow Six Siege";

            var input = Interaction.InputBox($"Please type the installation folder for {path}", "Superior Rainbow Six Siege Analyst Launcher", def, 50, 50);

            return input;
        }
    }
}