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





やってることは結局「認証して UNC に接続 → Explorer で開く」なので、**VBScript / PowerShell / バッチ**でも全部できます。

C# の `WNetAddConnection2` は、手動でやるときの `net use` コマンドと同じ層のAPIなので、スクリプトでは `net use` を叩くのが一番ラクです。

---

## 1️⃣ バッチ（.bat / .cmd）

```bat
@echo off

set UNC=\\192.168.0.10\C$
set USER=SERVER-PC\Administrator
set PASS=your_password_here

rem ① 管理共有に接続（認証） /persistent:no = 再起動後に残さない
net use %UNC% %PASS% /user:%USER% /persistent:no

rem ② Explorer を開く
start "" explorer.exe %UNC%
```

> これだけで、C# でやってるのとほぼ同じことをコンソールだけで実現できます。

---

## 2️⃣ PowerShell 版

### シンプルに `net use` を呼ぶパターン（実用向け）

```powershell
$unc  = '\\192.168.0.10\C$'
$user = 'SERVER-PC\Administrator'
$pass = 'your_password_here'

# ① 接続
cmd /c "net use $unc $pass /user:$user /persistent:no" | Out-Null

# ② Explorer を開く
Start-Process explorer.exe -ArgumentList $unc
```

### ちゃんと PowerShell らしく Credential 使う版

```powershell
$unc  = '\\192.168.0.10\C$'
$user = 'SERVER-PC\Administrator'
$pass = 'your_password_here'

$sec  = ConvertTo-SecureString $pass -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential($user, $sec)

# 一時的なPSドライブとしてマウント（ドライブレターじゃなく論理ドライブ）
New-PSDrive -Name CAdmin -PSProvider FileSystem -Root $unc -Credential $cred | Out-Null

# フォルダを開く
Start-Process explorer.exe -ArgumentList $unc
```

---

## 3️⃣ VBScript 版

### `net use` を叩くシンプル版

```vbscript
Dim unc, user, pass
unc  = "\\192.168.0.10\C$"
user = "SERVER-PC\Administrator"
pass = "your_password_here"

Set sh = CreateObject("WScript.Shell")

' ① 接続
sh.Run "net use " & unc & " " & pass & " /user:" & user & " /persistent:no", 0, True

' ② Explorer を開く
sh.Run "explorer.exe " & unc, 1, False
```

### MapNetworkDrive を使う版（ドライブレターを使う）

ドライブレター使っても良ければ：

```vbscript
Dim unc, user, pass
unc  = "\\192.168.0.10\C$"
user = "SERVER-PC\Administrator"
pass = "your_password_here"

Set net = CreateObject("WScript.Network")

' Z: に割り当て（永続化しない）
net.MapNetworkDrive "Z:", unc, False, user, pass

' Explorer で開く
Set sh = CreateObject("WScript.Shell")
sh.Run "explorer.exe Z:", 1, False
```

---

## 🔚 まとめ

* C# → `WNetAddConnection2` 直叩き
* バッチ / PowerShell / VBScript → `net use` or `MapNetworkDrive` で同じことができる
* **やっていることの本質はまったく同じ**で、「ネットワークセッションを張ってから UNC を普通のフォルダとして使う」だけ

なので、

> *「C# 版ツール」「バッチ版」「PowerShell版」*
> を並行して持っておいても全然アリです。

「バッチ版か PowerShell 版も整えておきたい」ってなったら、
どういうUI（引数 / メニュー / 対話式）にしたいか言ってくれればそこも詰めますよ。
