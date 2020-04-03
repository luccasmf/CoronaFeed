using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoronaFeed
{
    public static class Extensions
    {
        public static bool HasKeywords(this string text)
        {
            string[] keywords = new string[] { "corona", "covid", "pandemia" };



            foreach (var key in keywords)
            {
                if (text.ToLower().Contains(key))
                    return true;
            }

            return false;
        }
    }
}
