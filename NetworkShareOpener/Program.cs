using NetworkShareOpener;

string uncPath = @"\\192.168.0.10\C$";
string username = @"SERVER-PC\Administrator";  // 例
string password = "your_password_here";

try
{
    // ① 管理共有に接続（認証）
    ShareOpener.Connect(uncPath, username, password);

    // ② Explorer を開く
    ShareOpener.OpenExplorer(uncPath);

    Console.WriteLine("フォルダを開きました！");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

