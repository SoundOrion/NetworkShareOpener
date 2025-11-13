using System.Diagnostics;
using System.Runtime.InteropServices;

internal class NetworkShareOpener
{
    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(
        ref NETRESOURCE lpNetResource,
        string lpPassword,
        string lpUserName,
        int dwFlags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2(
        string lpName,
        int dwFlags,
        bool fForce);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NETRESOURCE
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string lpLocalName;
        public string lpRemoteName;
        public string lpComment;
        public string lpProvider;
    }

    private const int RESOURCETYPE_DISK = 1;

    /// <summary>
    /// UNCパスへ認証して接続
    /// </summary>
    public static void Connect(string uncPath, string username, string password)
    {
        NETRESOURCE nr = new NETRESOURCE
        {
            dwType = RESOURCETYPE_DISK,
            lpRemoteName = uncPath
        };

        int result = WNetAddConnection2(ref nr, password, username, 0);

        if (result != 0)
        {
            throw new InvalidOperationException($"接続に失敗しました。エラーコード: {result}");
        }
    }

    /// <summary>
    /// エクスプローラーを開く
    /// </summary>
    public static void OpenExplorer(string path)
    {
        Process.Start("explorer.exe", path);
    }
}
