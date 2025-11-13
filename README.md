# 管理共有（Administrative Shares）と C# による自動アクセス

Windows では、`\\<IPアドレス>\C$` のようにアクセスできる仕組みがあり、これは **管理共有（Administrative Shares）** と呼ばれます。本 README は以下をまとめています。

1. 管理共有とは何か
2. 管理共有へのアクセス方法
3. セキュリティ上の注意
4. C# による自動認証 + エクスプローラー起動コード（完全版）

---

## 🔍 管理共有（Administrative Shares）とは

Windows には、管理者ユーザーがリモートからシステム管理を行うために、デフォルトで生成される「隠し共有フォルダ」が存在します。
通常の共有フォルダとは異なり、末尾に `$` が付いているのが特徴です。

| 共有名      | 対応フォルダ                         | 用途              |
| -------- | ------------------------------ | --------------- |
| `C$`     | Cドライブのルート                      | ドライブ全体への管理アクセス  |
| `ADMIN$` | Windowsシステムフォルダ (`C:\Windows`) | リモート管理用         |
| `IPC$`   | 名前付きパイプ通信                      | リモート接続の認証・通信に使用 |

---

## ⚙️ アクセス方法と認証

`\\192.168.0.10\C$` のようにアクセスすると、その端末の
**管理者権限を持つユーザーの資格情報（ユーザー名・パスワード）**
が必要になります。

以下のユーザーではアクセスできません：

* 管理者権限のないユーザー
* パスワード未設定のローカルユーザー（Windows ではパスワードなしは認証不可）

---

## 🔒 セキュリティ上の注意点

* 管理共有は通常非表示ですが、有効であればネットワークからアクセス可能です。
* 不要であればグループポリシーやレジストリで無効化できます。
* 無効化すると、意図しないリモート管理アクセスを防げます。
* `$` が付いている共有名は **隠し共有** で、ネットワーク一覧に表示されません。

---

# 🚀 C# で「認証 → エクスプローラーで共有フォルダを開く」完全自動コード

以下は、管理共有に自動ログインし、そのまま Windows エクスプローラーで開く C# のサンプルです。

---

## 🔧 ソースコード（認証 → エクスプローラー起動）

```csharp
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

class NetworkShareOpener
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
```

---

## 🎯 実際の使い方

```csharp
static void Main()
{
    string uncPath = @"\\192.168.0.10\C$";
    string username = @"SERVER-PC\Administrator";  // 例
    string password = "your_password_here";

    try
    {
        // ① 管理共有に接続（認証）
        NetworkShareOpener.Connect(uncPath, username, password);

        // ② Explorer を開く
        NetworkShareOpener.OpenExplorer(uncPath);

        Console.WriteLine("フォルダを開きました！");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}
```

---

## ⭐ このコードでできること

* パスワード入力なしで共有フォルダへ接続
* エラー時は例外メッセージで表示
* 認証後、エクスプローラーで自動的に開く
* `File`/`Directory` を使った追加処理もすぐ可能

---

## 🔒 注意事項

* `C$` 共有は **管理者権限が必須**
* ネットワーク設定・ファイアウォールによっては接続不可
* パスワードのハードコードは非推奨

  * 暗号化、資格情報マネージャなどの利用推奨