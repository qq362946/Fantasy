using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Fantasy{
    public static class StringHelper
    {
        private static readonly Regex SpecialCharRegExp1 =
            new Regex("[ \\[ \\] \\^ \\-_*×――(^)$%~!＠@＃#$…&%￥—+=<>《》!！??？:：•`·、。，；,.;/\'\"{}（）‘’“”-]");

        private static readonly Regex SpecialCharRegExp2 = new Regex("[^0-9a-zA-Z\u4e00-\u9fa5]");

        private static readonly Regex RegexAllChinese = new Regex("^[\u4e00-\u9fa5]+$");

        //private static readonly Regex RegexName = new Regex("^[\u4e00-\u9fa5a-zA-Z]+$");
        // 改为还可以包含数字
        private static readonly Regex RegexName = new Regex("^[\u4e00-\u9fa5a-zA-Z0-9]+$");

        

        private static readonly Regex RegexAccount = new Regex("^[a-zA-Z][0-9a-zA-Z]*$");

        private static readonly Regex RegexPassword = new Regex("^[A-Za-z0-9]+$");

        public static bool IsSpecialCharA(string str)
        {
            return SpecialCharRegExp1.IsMatch(str);
        }

        public static bool IsSpecialCharB(string str)
        {
            return SpecialCharRegExp2.IsMatch(str);
        }

        public static bool IsAllChinese(this string str)
        {
            return RegexAllChinese.IsMatch(str);
        }

        public static bool IsInSection(this string str, int a, int b)
        {
            var strLength = str.Length;
            return strLength >= a && strLength <= b;
        }

        public static uint IsNameValid(this string str)
        {
            if (!RegexName.IsMatch(str))
            {
                return ErrorCode.RoleCreate_NameInvalid;
            }

            if (str.HasSensitive())
            {
                return ErrorCode.RoleCreate_NameHasSensitive;
            }

            return ErrorCode.Success;
        }

        // 字符转坐标
        public static Vector3 ParseCoordinates(string input)
        {
            string[] parts = input.Split(',');

            if (parts.Length == 3 &&
                float.TryParse(parts[0], out float x) &&
                float.TryParse(parts[1], out float y) &&
                float.TryParse(parts[2], out float z))
            {
                return new Vector3(x, y, z);
            }
            else return Vector3.zero;
        }
        // 字符转四元数
        public static Quaternion ParseRotation(string input)
        {
            string[] angles = input.Split(',');

            if (angles.Length == 3 &&
                float.TryParse(angles[0], out float x) &&
                float.TryParse(angles[1], out float y) &&
                float.TryParse(angles[2], out float z))
            {
                // 将欧拉角转换为 Quaternion
                return Quaternion.Euler(x, y, z);
            }
            else return Quaternion.identity;
        }

        // 数字转中文
        public static string OneBitNumberToChinese(string num){
            string numStr = "123456789";
            string chineseStr = "一二三四五六七八九";
            string result = "";
            int numIndex=numStr.IndexOf(num);
            if(numIndex>-1){
                result=chineseStr.Substring(numIndex,1);
            }
            return result;
        }

        public static string ToChatStr(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            int len = str.Length;

            char[] chars = null;

            int removeCount = 0;

            for (int i = 0; i < len; i++)
            {
                char ch = str[i];

                if (ch == '\r' || ch == '\n')
                {
                    removeCount++;
                    continue;
                }

                // 没有移位直接跳过
                if (removeCount == 0)
                {
                    continue;
                }

                // 转成char数组
                if (chars == null)
                {
                    chars = str.ToCharArray();
                }

                // 位置前移
                chars[i - removeCount] = ch;
            }

            if (chars != null)
            {
                str = new string(chars, 0, len - removeCount);
            }

            return str.ReplaceSensitive();
        }

        /// <summary>
        /// 账号校验
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static uint IsAccountValid(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return ErrorCode.Error_AccountIsNull;
            }

            str = str.Trim();

            if (str.Length < ConstValue.Login_Account[0])
            {
                return ErrorCode.Error_AccountTooShort;
            }

            if (str.Length > ConstValue.Login_Account[1])
            {
                return ErrorCode.Error_AccountTooLong;
            }

            if (!RegexAccount.IsMatch(str))
            {
                return ErrorCode.Error_AccountInvalid;
            }

            return ErrorCode.Success;
        }

        public static uint IsPasswordValid(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return ErrorCode.Error_PwIsNull;
            }

            str = str.Trim();

            if (str.Length < ConstValue.Login_Password[0])
            {
                return ErrorCode.Error_PwTooShort;
            }

            if (str.Length > ConstValue.Login_Password[1])
            {
                return ErrorCode.Error_PwTooLong;
            }

            if (!RegexPassword.IsMatch(str))
            {
                return ErrorCode.Error_PwInvalid;
            }

            return ErrorCode.Success;
        }

        public static uint IsPhoneValid(this string str)
        {
            // 手机号检验
            bool b = TelephoneNumberVerifyHelper.checkMobile(str);

            if (!b)
            {
                return ErrorCode.Error_SendAccountPhoneCodePhoneInvalid;
            }

            return ErrorCode.Success;
        }

        public static string ToClientPhone(this string str)
        {
            var arr = str.ToCharArray();
            for (int i = 3; i < 7; i++)
            {
                arr[i] = '*';
            }

            return new string(arr);
        }
        
        /// <summary>
        /// 隐藏手机号中段部分
        /// </summary>
        /// <param name="_phone"></param>
        /// <returns></returns>
        public static string PhoneHideMiddle(this string _phone)
        {
            var result = _phone.IsPhoneValid();
            if (result != 0)
            {
                Log.Error("手机号解析不合理，无法隐藏中段部分");
                return _phone;
            }

            var phone_start = _phone.Substring(0,                 3);
            var phone_end   = _phone.Substring(_phone.Length - 4, 4);
            return $"{phone_start}****{phone_end}";
        }


        public static string ToMd5(this string str)
        {
            MD5           md5   = System.Security.Cryptography.MD5.Create();
            byte[]        bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            StringBuilder sb    = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
            }

            return sb.ToString();
        }
    }
}

