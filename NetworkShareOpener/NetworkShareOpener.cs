using System;
using System.Diagnostics;
using System.IO;
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

    /// <summary>
    /// リモート側フォルダを一旦消してから、ローカルから丸ごとコピー（差し替え）
    /// </summary>
    public static void ReplaceFolder(string localFolder, string remoteFolder)
    {
        if (!Directory.Exists(localFolder))
        {
            throw new DirectoryNotFoundException($"ローカルフォルダが存在しません: {localFolder}");
        }

        // リモート側をまるっと削除（※危険なのでパスはよく確認！）
        if (Directory.Exists(remoteFolder))
        {
            Directory.Delete(remoteFolder, recursive: true);
        }

        CopyDirectory(localFolder, remoteFolder);
    }

    /// <summary>
    /// フォルダを再帰コピー（中身だけ）
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        // ルート作成
        Directory.CreateDirectory(destDir);

        // ファイルコピー
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var destPath = Path.Combine(destDir, relative);

            var destDirPath = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDirPath))
            {
                Directory.CreateDirectory(destDirPath);
            }

            File.Copy(file, destPath, overwrite: true);
        }
    }
}
