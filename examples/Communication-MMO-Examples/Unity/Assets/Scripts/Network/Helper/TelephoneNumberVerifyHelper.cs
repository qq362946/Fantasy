// ============================================================================================
// FileName： TelephoneNumberVerifyHelper.cs
// Description : 手机号，验证
// Create Time: 2022年07月15日 14：23
// ============================================================================================

using System.Text.RegularExpressions;

namespace Fantasy{
        public enum PhoneOperators
    {
        None = 0,
        
        /// <summary>
        /// 中国移动
        /// </summary>
        CHINA_MOBILE = 1, 
        
        /// <summary>
        /// 中国联通
        /// </summary>
        CHINA_UNICOM = 2,
        
        /// <summary>
        /// 中国电信
        /// </summary>
        CHINA_TELECOM = 3,
        
    }


    public class TelephoneNumberVerifyHelper
    {
        /*中国移动号码格式验证 手机段：134(0-8),135,136,137,138,139,147,148,150,151,152,157,158,159,172,178,182,183,184,187,188,195,197,198,1440,1703,1705,1706*/
        private static readonly string CHINA_MOBILE_PATTERN = "(?:^(?:\\+86)?1(?:34|3[5-9]|4[78]|5[0-27-9]|7[28]|8[2-478]|9[578])\\d{8}$)|(?:^(?:\\+86)?1440\\d{7}$)|(?:^(?:\\+86)?170[356]\\d{7}$)";

        /*中国联通号码格式验证 手机段：130,131,132,140,145,146,155,156,166,185,186,171,175,176,196,1704,1707,1708,1709*/
        private static readonly string CHINA_UNICOM_PATTERN = "(?:^(?:\\+86)?1(?:3[0-2]|4[056]|5[56]|66|7[156]|8[56]|96)\\d{8}$)|(?:^(?:\\+86)?170[47-9]\\d{7}$)";

        /*中国电信号码格式验证 手机段：133,149,153,177,173,180,181,189,190,191,193,199,1349,1410,1700,1701,1702*/
        private static readonly string CHINA_TELECOM_PATTERN = "(?:^(?:\\+86)?1(?:33|49|53|7[37]|8[019]|9[0139])\\d{8}$)|(?:^(?:\\+86)?1349\\d{7}$)|(?:^(?:\\+86)?1410\\d{7}$)|(?:^(?:\\+86)?170[0-2]\\d{7}$)";

        /*截止2022年2月,中国大陆四家运营商以及虚拟运营商手机号码正则验证*/
        private static readonly string CHINA_PATTERN = "(?:^(?:\\+86)?1(?:3[0-9]|4[01456879]|5[0-35-9]|6[2567]|7[0-8]|8[0-9]|9[0-35-9])\\d{8}$)";


        
        /// <summary>
        /// 中国移动手机号码校验
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static bool checkChinaMobile(string mobile)
        {
            if (string.IsNullOrEmpty(mobile))
            {
                return false;
            }
            Regex dReg = new Regex(CHINA_MOBILE_PATTERN);

            if (dReg.IsMatch(mobile))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// 中国联通手机号码校验
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static bool checkChinaUnicom(string mobile) 
        {
            if (string.IsNullOrEmpty(mobile))
            {
                return false;
            }
            Regex dReg = new Regex(CHINA_UNICOM_PATTERN);

            if (dReg.IsMatch(mobile))
            {
                return true;
            }
            return false;
        }
        

        /// <summary>
        /// 中国电信手机号码校验
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static bool checkChinaTelecom(string mobile) 
        {
            if (string.IsNullOrEmpty(mobile))
            {
                return false;
            }
            Regex dReg = new Regex(CHINA_TELECOM_PATTERN);

            if (dReg.IsMatch(mobile))
            {
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 属于中国大陆四家运营商或虚拟运营商的手机号码
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static bool checkChineseMobile(string mobile)
        {
            if (string.IsNullOrEmpty(mobile))
            {
                return false;
            }
            Regex dReg = new Regex(CHINA_PATTERN);

            if (dReg.IsMatch(mobile))
            {
                return true;
            }
            return false;
        }
        
        

        /// <summary>
        ///  获取中国大陆手机号所属的运营商,如果都不是返回 None
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static PhoneOperators checkMobileBelong(string mobile) 
        {
            if (!checkChineseMobile(mobile))
            {
                return PhoneOperators.None; 
            }

            if (checkChinaMobile(mobile))
            {
                return PhoneOperators.CHINA_MOBILE;
            }


            if (checkChinaUnicom(mobile))
            {
                return PhoneOperators.CHINA_UNICOM;
            }

            if (checkChinaTelecom(mobile))
            {
                return PhoneOperators.CHINA_TELECOM;
            }
            return PhoneOperators.None;
        }

        
        
        /// <summary>
        ///  获取是否为
        /// </summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        public static bool checkMobile(string mobile) 
        {
            if (!checkChineseMobile(mobile))
            {
                return false;
            }

            if (checkChinaMobile(mobile))
            {
                return true;
            }


            if (checkChinaUnicom(mobile))
            {
                return true;
            }

            if (checkChinaTelecom(mobile))
            {
                return true;
            }

            return false;
        }
    }
}

