using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Com.MobileSolutions.Application.Dictionary
{
    public class Utils
    {
        public static string GetChargesType(string charge)
        {
            string chargeCode = "";

            switch (charge)
            {
                case "gigabytes":
                    chargeCode = Constants.ChargesType_GB;
                    break;
                case "megabytes":
                    chargeCode = Constants.ChargesType_MB;
                    break;
                case "kilobytes":
                    chargeCode = Constants.ChargesType_KB;
                    break;
                case "minutes":
                    chargeCode = Constants.ChargesType_M;
                    break;
                case "calls":
                    chargeCode = Constants.ChargesType_M;
                    break;
                case "messages":
                    chargeCode = Constants.ChargesType_MSG;
                    break;
                case "messages sent":
                    chargeCode = Constants.ChargesType_MSG;
                    break;
                case "messages rcvd":
                    chargeCode = Constants.ChargesType_MSG;
                    break;
                default:
                    chargeCode = string.Empty;
                    break;
            }

            return chargeCode;
        }


        public static string RemoveTextFromNumber(String number)
        {
            var regex = new Regex(@"^([0-9]*\.?[0-9]*)(GB|MB|KB)").Match(number);

            return regex.Success ? regex.Groups[1].ToString() : number;
        }

        public static string NumberFormat(string currentNumber)
        {
            currentNumber = currentNumber.Replace(Constants.MoneySign, string.Empty).Replace(",", string.Empty).Replace("|", string.Empty);
            var surRegex = new Regex(Constants.MoneyRegexWithoutCero);
            if (currentNumber.Equals(string.Empty))
            {
                currentNumber = "0";
            }
            else if (!currentNumber.Equals(string.Empty) && surRegex.IsMatch(currentNumber))
            {
                var valueArray = currentNumber.Split(".");

                currentNumber = valueArray[0] + "0." + valueArray[1];
            }
            return currentNumber;
        }


        public static double RoundUp(double input, int places)
        {
            double multiplier = Math.Pow(10, Convert.ToDouble(places));
            return Math.Ceiling(input * multiplier) / multiplier;
        }

        public static string GetFileName(string path)
        {
            var pathArray = path.Split(@"\");
            var fileNameArray = pathArray[pathArray.Length - 1].Split(".");

            return fileNameArray[1];
        }

        public static int GetYear(int begMonth, int endMonth, int currentMonth, int currentYear)
        {
            int endYear = 0;
            if ((endMonth < begMonth && 1 <= endMonth && endMonth <= currentMonth && begMonth > currentMonth) ||
                (begMonth <= endMonth && 1 <= endMonth && endMonth <= currentMonth) ||
                (begMonth <= endMonth && 1 <= endMonth && endMonth > currentMonth))
            {
                endYear = currentYear;
            }
            else if (begMonth <= currentMonth && endMonth <= currentMonth)
            {
                endYear = currentYear + 1;
            }
            else if (begMonth <= endMonth && endMonth >= currentMonth && (endMonth - currentMonth) > 6)
            {
                endYear = currentYear - 1;
            }

            return endYear;
        }


        public static int GetAdjustmentYear(int begMonth, int currentMonth, int currentYear)
        {
            int endYear = currentYear;

            if (begMonth <= currentMonth)
            {
                endYear = currentYear;
            }
            else if (begMonth > currentMonth && (begMonth - currentMonth) > 6)
            {
                endYear = currentYear - 1;
            }
            else if (begMonth < currentMonth && (currentMonth - begMonth) > 6)
            {
                endYear = currentYear + 1;
            }


            return endYear;
        }

        public static string ConvertTextMonthDateFormat(string date)
        {
            date = date.Trim();
            var day = Convert.ToInt32(date.Split(" ")[1].Replace(",", string.Empty));
            var month = DateTime.ParseExact(date.Split(" ")[0], date.Split(" ")[0].Length < 4 ? "MMM" : "MMMM", CultureInfo.CurrentCulture).Month;
            var year = date.Split(" ").Length > 2 && date.Split(" ")[2] != string.Empty ? Convert.ToInt32(date.Split(" ")[2]) : DateTime.Now.Year;
            return new DateTime(year, month, day).ToString();
        }

        public static string FindTypeByName(string name)
        {
            var type = "";
            if (name.Contains(Constants.Surcharges))
            {
                type = Constants.SUR;
            }
            else if (name.Contains(Constants.Tax))
            {
                type = Constants.TAX;
            }
            else
            {
                type = Constants.ADJBF;
            }
            return type;
        }
    }
}
