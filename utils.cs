using System;
using System.Globalization;

namespace common
{
    public class utils
    {
        static public float StringToFloat(String total)
        {
            float result = 0.0f;
            total = total.Trim();
            if (total.Length > 0)
            {
                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.NumberDecimalSeparator = ".";

                total = total.Replace(',', nfi.NumberDecimalSeparator[0]);
                try
                {
                    result = float.Parse(total, nfi);
                }
                catch (Exception /* e */)
                {
                }
            }
            return result;
        }
    }
}
