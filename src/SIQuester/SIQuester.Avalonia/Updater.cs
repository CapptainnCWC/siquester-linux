using SIQuester.ViewModel.PlatformSpecific;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace SIQuester.Avalonia;

/// <summary>Checks GitHub releases and installs a newer .deb package.</summary>
internal static class Updater
{
    public const string Repository = "CapptainnCWC/siquester-linux";

    public static async Task CheckAndUpdateAsync(PlatformManager ui)
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("siquester-linux-updater");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            using var response = await http.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ui.Inform("На GitHub пока нет опубликованных релизов.");
                return;
            }

            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? "";

            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var latest))
            {
                ui.ShowErrorMessage($"Не удалось разобрать версию релиза: {tag}");
                return;
            }

            var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

            if (latest <= new Version(current.Major, current.Minor, Math.Max(current.Build, 0)))
            {
                ui.Inform($"Установлена последняя версия ({current.Major}.{current.Minor}.{Math.Max(current.Build, 0)}).");
                return;
            }

            var asset = root.GetProperty("assets").EnumerateArray()
                .FirstOrDefault(a => a.GetProperty("name").GetString()?.EndsWith(".deb") == true);

            var releaseUrl = root.GetProperty("html_url").GetString();

            if (asset.ValueKind == JsonValueKind.Undefined)
            {
                ui.Inform($"Вышла версия {latest}, но .deb в релизе нет. Страница релиза: {releaseUrl}");
                return;
            }

            if (!ui.Confirm($"Доступна версия {latest}. Скачать и установить?"))
            {
                return;
            }

            var path = Path.Combine(Path.GetTempPath(), asset.GetProperty("name").GetString()!);
            using (var stream = await http.GetStreamAsync(asset.GetProperty("browser_download_url").GetString()))
            using (var file = File.Create(path))
            {
                await stream.CopyToAsync(file);
            }

            var psi = new ProcessStartInfo("pkexec", new[] { "apt", "install", "-y", path }) { UseShellExecute = false };
            using var process = Process.Start(psi);
            await process!.WaitForExitAsync();

            ui.Inform(process.ExitCode == 0
                ? "Обновление установлено. Перезапустите SIQuester."
                : $"Установка не удалась (код {process.ExitCode}). Файл: {path}");
        }
        catch (Exception exc)
        {
            ui.ShowErrorMessage("Не удалось проверить обновления: " + exc.Message);
        }
    }
}
