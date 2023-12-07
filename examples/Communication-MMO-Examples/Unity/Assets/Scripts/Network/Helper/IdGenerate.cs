
using System.Net;
using System.Threading.Tasks;
using Fantasy;

namespace BestGame{
    public static class IdGenerate
    {
        private static int value;
    
        /// <summary>
        /// 生成指定区服的账号,参数为1以上的整数
        /// </summary>
        /// <param name="ZoneId">WorldId</param>
        /// <returns></returns>
        public static long GenerateId(uint ZoneId)
        {
            long time = TimeHelper.Now / 1000;
            //1540 2822 75   时间为10位数
            //区号取第11位数
            return (ZoneId * 100000000000 + time + ++value);
        }

        public static long GenerateId()
        {
            long time = TimeHelper.Now / 1000;
            //1540 2822 75   时间为10位数
            //区号取第11位数
            return (RandomHelper.RandUInt32() * 100000000000 + time + ++value);
        }

        /// <summary>
        /// 查询账号所在区服,参数为1以上的整数
        /// </summary>
        public static int GetZoneIdFromAuthId(long authID)
        {
            return (int)(authID/100000000000);
        }
    }
}


