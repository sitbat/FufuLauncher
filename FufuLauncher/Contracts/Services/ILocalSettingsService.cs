using System.Threading.Tasks;

namespace FufuLauncher.Contracts.Services
{
    public interface ILocalSettingsService
    {
        Task<object?> ReadSettingAsync(string key);
        Task SaveSettingAsync<T>(string key, T value);
    }
}