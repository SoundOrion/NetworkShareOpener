const string uncPath = @"\\192.168.0.10\C$";
const string username = @"SERVER-PC\Administrator";  // 例
const string password = "your_password_here";

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
