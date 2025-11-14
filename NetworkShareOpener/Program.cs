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


//const string uncRoot = @"\\192.168.0.10\C$";      // 管理共有のルート
//const string localTemp = @"C:\temp";               // 自分のPC
//const string remoteTemp = @"\\192.168.0.10\C$\temp"; // 相手側の temp

//const string username = @"SERVER-PC\Administrator";
//const string password = "your_password_here";

//try
//{
//    // ① 管理共有に接続
//    NetworkShareOpener.Connect(uncRoot, username, password);

//    // ② リモートの C:\temp をローカル C:\temp で「差し替え」
//    NetworkShareOpener.ReplaceFolder(localTemp, remoteTemp);

//    Console.WriteLine("リモート側 C:\\temp をローカルと差し替えました。");
//}
//catch (Exception ex)
//{
//    Console.WriteLine(ex.Message);
//}
