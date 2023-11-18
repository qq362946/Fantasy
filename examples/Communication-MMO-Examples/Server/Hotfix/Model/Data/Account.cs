using Microsoft.International.Converters.PinYinConverter;
namespace Fantasy;
public class Account : Entity
{
    public string AuthName { get; set; } = ""; // 登录唯一码
    public string Pw { get; set; } = ""; // 密码
    public string Phone { get; set; } = ""; // 手机号
    public string RegisterIp { get; set; } = ""; // 注册时IP
    public string LastLoginIp { get; set; } = ""; // 最后登录IP
    public long RegisterTime { get; set; } // 注册时间戳
    public long LastLoginTime { get; set; } // 最后登录时间戳
    public uint GateSceneId;

    public string TableName()
    {
        var upper = GetFirstLetter(AuthName);
        return $"{nameof(Account)}_First_{upper[0]}";
    }

    public static string TableName(string name)
    {
        var upper = GetFirstLetter(name);
        return $"{nameof(Account)}_First_{upper[0]}";
    }

    // 获取字符串的大写首字母
    public static string GetFirstLetter(string name)
    {
        char firstChar = name.First();

        // 数字或字母
        if (Char.IsDigit(firstChar) || ('a' <= firstChar && firstChar <= 'z') || ('A' <= firstChar && firstChar <= 'Z'))
        {
            return firstChar.ToString().ToUpper();
        }

        // 汉字
        try
        {
            ChineseChar cc = new ChineseChar(firstChar);
            if (cc.Pinyins.Count > 0 && cc.Pinyins[0].Length > 0)
            {
                return cc.Pinyins[0][0].ToString().ToUpper();
            }
        }
        catch (InvalidOperationException ex)
        {
            Log.Info($"Failed to determine the first letter for {name}. {ex.Message}");
        }
        
        // 如果无法识别，可以返回默认值
        return "00".ToUpper();
    }
}