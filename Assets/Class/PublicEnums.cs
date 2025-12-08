using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets
{
    public static class PublicEnums
    {
        public enum ColorType
        {
            Red,
            Blue,
            Green,
            Yellow,
            Purple,
            Orange,
            Black,
            White,
            Pink
        }

        public enum ComboTier : int
        {
            Super = 0,
            Mega = 1,
            Ultra = 2,
        }
    }
}
