namespace DevBlogData.Data;

internal static class DatabaseConfig
{
    // DevBlog.Api'nin kullandığı SQLite dosyasına işaret eder (bkz. src/DevBlog.Api/appsettings.json).
    // Çalışma dizini (CWD) başlatma yöntemine göre değişebildiğinden (ör. proje dizininden `dotnet run`,
    // repo kökünden `dotnet run --project`, MCP Inspector, doğrudan derlenmiş .dll) CWD'ye göre relatif bir
    // yol kullanılmıyor; bunun yerine bu derlemenin bulunduğu dizine (bin/Debug/net10.0) göre sabitleniyor.
    public static readonly string SqliteDbPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "DevBlog.Api", "devblog.db"));
}
