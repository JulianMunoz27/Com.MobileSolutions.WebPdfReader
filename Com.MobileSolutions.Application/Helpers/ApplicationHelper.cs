using Com.MobileSolutions.Application.Dictionary;
using Com.MobileSolutions.Application.Enums;
using Com.MobileSolutions.Domain.Models;
using NLog;
using Spire.Pdf;
using Spire.Pdf.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Com.MobileSolutions.Application.Helpers
{
    public class ApplicationHelper
    {
        private PdfDocument document;
        private int previousPage = 0;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public ApplicationHelper(PdfDocument document, string path)
        {
            this.document = document;
            this.document.LoadFromFile(path);
        }

        public ApplicationHelper()
        {
        }

        /// <summary>
        /// Builds the spire pdf document in order to have the minimum configurations for the text extractions.
        /// </summary>
        /// <param name="document"></param>
        /// <returns>A list of <see cref="string"/> with values to be extracted</returns>
        public List<string> Prebuild(PdfDocument document, string path)
        {
            document.LoadFromFile(path);
            PdfPageBase page;
            lock (this)
            {
                page = document.Pages[(int)PageType.Header];
            }
            var text = page.ExtractText().Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            List<string> list = new List<string>();

            for (int i = 1; i < text.Count; i++)
            {
                var trim = text[i].TrimStart().TrimEnd();

                //removes excessive spaces leaving just one
                RegexOptions options = RegexOptions.None;
                Regex regex = new Regex("[ ]{2,}", options);
                trim = regex.Replace(trim, "|");

                list.Add(trim);
            }

            return list;
        }

        public List<List<string>> ReadDetails(PdfDocument document, PathValues pathValues)
        {
            int lastDetailPage = 0;
            int errorCount = 0;
            List<string> buggedStrings = new List<string>();
            List<List<string>> detailList = new List<List<string>>();
            List<string> pageList = new List<string>();
            PdfPageCollection pages;

            lock (this)
            {
                document.LoadFromFile(pathValues.Path);
                pages = document.Pages;
            }

            ///Put every page in a list to be able to search certain values
            for (int page = 0; page < document.Pages.Count; page++)
            {
                var text = pages[page].ExtractText().Remove(0, 70);
                RegexOptions options = RegexOptions.None;
                Regex regex = new Regex("[ ]{2,}", options);
                text = regex.Replace(text, "|");
                pageList.Add(text);


                if (text.Contains("Total Current Charges") && text.Contains("|Charges by Cost Center|Number|Charges|Charges|Charges|Credits|and Fees|(includes Tax)|Charges|Usage|Usage|Usage|Roaming|Roaming|Roaming"))
                {
                    lastDetailPage = page;
                    break;
                }
            }

            //var text = pages[300].ExtractText().Remove(0, 70);

            //RegexOptions options = RegexOptions.None;
            //Regex regex = new Regex("[ ]{2,}", options);
            //text = regex.Replace(text, "|");
            //pageList.Add(text);

            //detailList.Add(DetailPageReader(document, 300));


            foreach (var page in pageList)
            {
                if (page.Contains(Constants.BreakdownOfCharges))
                {
                    var splittedPage = page.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToArray();
                    var index = GetBreakdownOfChargesArrayPosition(splittedPage);
                    var lineRegex = new Regex(@"(pg\s\d+)");

                    for (int line = index; line <= splittedPage.Length - 1; line++)
                    {
                        var indexValue = splittedPage[line];
                        if (lineRegex.IsMatch(indexValue) && !indexValue.Contains("Account Charges & Credits"))
                        {
                            var indexRegex = new Regex(@"\d+").Match(lineRegex.Match(indexValue).Value);
                            if (indexRegex.Success)
                            {
                                detailList.Add(DetailPageReader(document, Convert.ToInt32(indexRegex.Value)));
                            }
                        }
                        else if (indexValue.Contains("Total Current Charges"))
                        {
                            break;
                        }
                    }
                }

                if (page.Contains("|Overage Details") || page.Contains("|Overage Details|Overage Details, Continued"))
                {
                    var splittedPage = page.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                    detailList.Add(splittedPage);                
                }

                if (page.Contains(Constants.OverviewM2M))//Overview of Machine to Machine Activity
                {
                    var splittedPage = page.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                    detailList.Add(splittedPage);
                }

                //
                if (!page.Contains(Constants.QuickBillSummary) && (page.Contains(Constants.AccountMonthlyCharges) || page.Contains(Constants.AccountChargesAndCreditsContinue) ||
                    page.Contains(Constants.AccountChargesAndCredits) || page.Contains(Constants.PaymentsAdjustments) || page.Contains(Constants.PaymentsAdjustmentsContinue) ||
                    page.Contains(Constants.AdjustmentsContinued) || page.Contains(Constants.Adjustments) && page.Contains(Constants.TotalAdjustments)))//Overview of Machine to Machine Activity
                {
                    var splittedPage = page.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                    detailList.Add(splittedPage);
                }

                ///Realize if the page is a detail page or not
                if ((page.Contains(Constants.OverviewOfLines) || page.Contains(Constants.OverviewOfVoice)) && !page.Contains(Constants.BreakdownOfCharges))
                {
                    var splittedPage = page.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToArray();
                    var index = GetDetailsArrayPosition(splittedPage);
                    var lineRegex = new Regex(Constants.DetailsRegex);

                    for (int line = index; line <= splittedPage.Length - 1; line++)
                    {
                        //validates if the line is a detail or a cost center 
                        if (splittedPage[line].Split('|').Length > 2)
                        {

                            var indexValue = splittedPage[line];
                            if (lineRegex.IsMatch(indexValue))
                            {
                                var indexRegex = new Regex(Constants.IndexRegex2).Match(indexValue);
                                //TODO: some lines are bugged and doesn't contains the "|" before the page number so i have to ignore them until solved
                                if (indexRegex.Success)//if (int.TryParse(splittedPage[line].Split('|')[2], out int n))
                                {
                                    var indexGroup = indexRegex.Groups;
                                    if (!string.IsNullOrEmpty(indexGroup[5].ToString()))
                                    {
                                        previousPage = Convert.ToInt32(Convert.ToInt64(indexGroup[5].ToString()) > document.Pages.Count ? indexGroup[5].ToString().Substring(indexGroup[5].ToString().Length - previousPage.ToString().Length) : indexGroup[5].ToString());
                                        if (previousPage != 0)
                                        {
                                            detailList.Add(DetailPageReader(document, previousPage));
                                        }
                                    }
                                    else
                                    {
                                        var valueRegex = new Regex(Constants.IndexRegex3).Match(indexGroup[2].ToString());
                                        if (valueRegex.Success)
                                        {
                                            if (previousPage < 1)
                                            {
                                                var fix = splittedPage[line + 1];
                                                var fixRegex = new Regex(Constants.IndexRegex2).Match(fix);
                                                var fixGroup = fixRegex.Groups;

                                                if (!string.IsNullOrEmpty(fixGroup[5].ToString()))
                                                {
                                                    previousPage = Convert.ToInt32(Convert.ToInt64(valueRegex.Value) > document.Pages.Count ? valueRegex.Value.Substring(valueRegex.Value.Length - fixGroup[5].ToString().Length) : valueRegex.Value);
                                                }
                                            }
                                            else 
                                            {
                                                previousPage = Convert.ToInt32(Convert.ToInt64(valueRegex.Value) > Convert.ToInt64(document.Pages.Count) ? previousPageCheck(previousPage) ? valueRegex.Value.Substring(valueRegex.Value.ToString().Length - previousPage.ToString().Length - 1) : valueRegex.Value.Substring(valueRegex.Value.ToString().Length - previousPage.ToString().Length) : valueRegex.Value);
                                            }
                                            
                                            if (previousPage != 0)
                                            {
                                                detailList.Add(DetailPageReader(document, previousPage));
                                            }

                                        }
                                        else
                                        {
                                            var fileName = Path.GetFileName(pathValues.Path);
                                            System.IO.File.Move(pathValues.Path, $@"{pathValues.FailedFiles}\{fileName}");
                                            logger.Error("File index extraction failed");
                                        }
                                    }
                                }
                                else
                                {
                                    var fileName = Path.GetFileName(pathValues.Path);
                                    System.IO.File.Move(pathValues.Path, $@"{pathValues.FailedFiles}\{fileName}");
                                    logger.Error("File index extraction failed");
                                    errorCount++;
                                    buggedStrings.Add(splittedPage[line]);
                                }
                            }
                        }
                    }
                }
            }

            return detailList;
        }

        public List<string> RemoveComma(List<string> headerValues)
        {
            List<string> cleanList = new List<string>();

            foreach (var value in headerValues)
            {
                if (value.Contains(Constants.MoneySign))
                {
                    cleanList.Add(value.Replace(Constants.Comma, string.Empty));
                }
                else
                {
                    cleanList.Add(value);
                }
            }

            return cleanList;
        }

        public string RemoveMoney(string moneyString)
        {
            return moneyString.Replace(Constants.MoneySign, string.Empty);
        }

        public int GetDetailsArrayPosition(string[] details)
        {
            return Array.IndexOf(details, Constants.DetailColumns) + 1;
        }

        public int GetBreakdownOfChargesArrayPosition(string[] details)
        {
            return Array.IndexOf(details, Constants.BreakdownOfCharges) + 1;
        }

        public List<string> DetailPageReader(PdfDocument document, int pageNumber)
        {
            List<string> formattedPage = new List<string>();
            List<string> page;
            lock (this)
            {
                page = document.Pages[pageNumber - 1].ExtractText().Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var lineCount = 0;
            var YourPlan = page.FirstOrDefault(p => p.Contains(Constants.YourPlan));
            var firstYourPlan = Array.IndexOf(page.ToArray(), YourPlan);

            foreach (var line in page)
            {
                var len = line.Length;

                if (len > 52 && lineCount > firstYourPlan)
                {
                    var lineWithChars = line.Insert(52, Constants.LineSeparator);
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(lineWithChars.TrimStart().TrimEnd(), "|"));
                }
                else if(lineCount > firstYourPlan)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }
                else
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd() + Constants.LineSeparator, "|"));
                }
                lineCount++;
            }

            //foreach (var line in page)
            //{
            //    RegexOptions options = RegexOptions.None;
            //    Regex regex = new Regex("[ ]{2,}", options);
            //    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
            //}

            return formattedPage;
        }

        public List<string> getPageContent(int page)
        {
            return this.DetailPageReader(this.document, page);
        }

        public DetailDto GetTaxesGovermentalSurcharges(string surValues, string accountNumber, string mrcDataSpName, string serviceId)
        {
            DetailDto usgsumDetail = null;
            var taxesGovernmentalSurchargesRegex = new Regex(Constants.TaxesGovernmentalSurchargesRegex).Match(surValues);

            if (taxesGovernmentalSurchargesRegex.Success)
            {
                var regexGroup = taxesGovernmentalSurchargesRegex.Groups;
                usgsumDetail = new DetailDto();

                usgsumDetail.UNIQ_ID = Constants.TAX;
                usgsumDetail.CHG_CLASS = Constants.LevelOne;
                usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                usgsumDetail.SUBSCRIBER = serviceId;
                usgsumDetail.CHG_CODE_1 = regexGroup[3].ToString();
                usgsumDetail.CHG_AMT = Utils.NumberFormat(regexGroup[4].ToString());
                usgsumDetail.CURRENCY = Constants.USD;
                usgsumDetail.INFO_ONLY_IND = "N";
                usgsumDetail.SP_INV_RECORD_TYPE = Constants.TaxesType;

            }

            return usgsumDetail;
        }

        public DetailDto GetMessaging(string messagingValues, string mrcDataSpName, string serviceId, string accountNumber, string type)
        {
            DetailDto usgsumDetail = null;
            var messagingArray = messagingValues.Split(Constants.Pipe);

            var internationalMessageRegex = new Regex(Constants.InternationalMessageRegex).Match(messagingValues);

            if (internationalMessageRegex.Success)
            {
                usgsumDetail = new DetailDto();
                var purchasesRegexGroup = internationalMessageRegex.Groups;


                var planeName = purchasesRegexGroup[1].ToString();

                usgsumDetail.UNIQ_ID = Constants.USGSUM;
                usgsumDetail.CHG_CLASS = Constants.LevelOne;
                usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                usgsumDetail.SUBSCRIBER = serviceId;
                usgsumDetail.CHG_CODE_1 = planeName;
                usgsumDetail.CHG_QTY1_ALLOWED = messagingValues.Contains("unlimited") ? "" : Utils.NumberFormat(purchasesRegexGroup[6].ToString().Replace(Constants.Hyphen, string.Empty));
                usgsumDetail.CHG_QTY1_TYPE = Utils.GetChargesType(purchasesRegexGroup[2].ToString());
                usgsumDetail.CHG_QTY1_USED = Utils.NumberFormat(purchasesRegexGroup[8].ToString().Replace(Constants.Hyphen, string.Empty));
                usgsumDetail.CHG_QTY1_BILLED = purchasesRegexGroup[10].ToString().Replace(Constants.Hyphen, string.Empty).Equals(string.Empty) ? "0" : Utils.NumberFormat(purchasesRegexGroup[10].ToString());
                usgsumDetail.CHG_AMT = purchasesRegexGroup[12].ToString().Contains("--") ? "0" : Utils.NumberFormat(purchasesRegexGroup[12].ToString().Replace(Constants.MoneySign, string.Empty));
                usgsumDetail.CURRENCY = Constants.USD;
                usgsumDetail.INFO_ONLY_IND = "N";
                usgsumDetail.SP_INV_RECORD_TYPE = type;

            }

            return usgsumDetail;
        }


        public DetailDto GetAccountMonthlyCharges(string dataValues, string accountNumber, int dateDueMonth, int dateDueYear)
        {
            DetailDto taxData = null;

            var accountMonthlyRegex = new Regex(Constants.AccountMonthlyRegex).Match(dataValues);

            if (accountMonthlyRegex.Success)
            {
                var accountMonthly = accountMonthlyRegex.Groups;

                taxData = new DetailDto();



                var begMonth = Convert.ToInt32(accountMonthly[4].ToString().Split('-')[0].Split('/')[0]);
                var endMonth = Convert.ToInt32(accountMonthly[4].ToString().Split('-')[1].Split('/')[0]);

                var begYear = begMonth >= 1 && begMonth <= dateDueMonth ? dateDueYear : dateDueYear - 1;
                var endYear = endMonth >= 1 && endMonth <= dateDueMonth ? dateDueYear + 1 : dateDueYear;

                taxData.UNIQ_ID = Constants.MRC;
                taxData.CHG_CLASS = Constants.LevelOne;
                taxData.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                taxData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                taxData.CHG_CODE_1 = accountMonthly[2].ToString();
                taxData.BEG_CHG_DATE = accountMonthly[4].ToString().Split('-').Length > 1 ? new DateTime(begYear, begMonth, Convert.ToInt32(accountMonthly[4].ToString().Split('-')[0].Split('/')[1])).ToString("M/d/yyyy") : string.Empty;
                taxData.END_CHG_DATE = accountMonthly[4].ToString().Split('-').Length > 1 ? new DateTime(endYear, endMonth, Convert.ToInt32(accountMonthly[4].ToString().Split('-')[1].Split('/')[1])).ToString("M/d/yyyy") : string.Empty;
                taxData.CHG_AMT = Utils.NumberFormat(accountMonthly[5].ToString().Replace(Constants.MoneySign, string.Empty).Replace(",", string.Empty));

                taxData.CURRENCY = Constants.USD;
                taxData.INFO_ONLY_IND = "N";
                taxData.SP_INV_RECORD_TYPE = Constants.TaxesType;

            }

            return taxData;
        }


        public DetailDto GetSurcharges(string dataValues, string mrcDataSpName, string serviceId, string accountNumber)
        {
            var dateRegex = new Regex(Constants.SurchargesRegex2).Match(dataValues);

            DetailDto usgsumDetail = null;

            if (dateRegex.Success)
            {
                var surchargesGroup = dateRegex.Groups;

                usgsumDetail = new DetailDto();

                usgsumDetail.UNIQ_ID = Constants.SUR;
                usgsumDetail.CHG_CLASS = Constants.LevelOne;
                usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                usgsumDetail.ACCT_LEVEL_1 = "";
                usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                usgsumDetail.SUBSCRIBER = serviceId;
                usgsumDetail.CHG_CODE_1 = surchargesGroup[2].ToString().Replace(Constants.Pipe, ' ');
                usgsumDetail.CHG_AMT = Utils.NumberFormat(surchargesGroup[4].ToString());
                usgsumDetail.CURRENCY = Constants.USD;
                usgsumDetail.INFO_ONLY_IND = "N";
                usgsumDetail.SP_INV_RECORD_TYPE = Constants.SurchargesType;

            }
            else
            {
                var dateRegex2 = new Regex(Constants.SurchargesRegex).Match(dataValues);

                if (dateRegex2.Success)
                {
                    var surchargesGroup = dateRegex2.Groups;

                    usgsumDetail = new DetailDto();

                    usgsumDetail.UNIQ_ID = Constants.SUR;
                    usgsumDetail.CHG_CLASS = Constants.LevelOne;
                    usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                    usgsumDetail.ACCT_LEVEL_1 = "";
                    usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                    usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                    usgsumDetail.SUBSCRIBER = serviceId;
                    usgsumDetail.CHG_CODE_1 = surchargesGroup[2].ToString().Replace(Constants.Pipe, ' ');
                    usgsumDetail.CHG_AMT = Utils.NumberFormat(surchargesGroup[3].ToString());
                    usgsumDetail.CURRENCY = Constants.USD;
                    usgsumDetail.INFO_ONLY_IND = "N";
                    usgsumDetail.SP_INV_RECORD_TYPE = Constants.SurchargesType;

                }
            }

            return usgsumDetail;
        }

        public DetailDto GetData(string dataValues, string mrcDataSpName, string serviceId, bool share, string accountNumber, String type)
        {

            DetailDto usgsumDetail = null;

            if (dataValues.Split(Constants.Pipe).Length >= 3)
            {

                var usgPurchesChargesMatch = new Regex(Constants.UsgPurchesCharges2).Match(dataValues);

                if (usgPurchesChargesMatch.Success)
                {
                    usgsumDetail = new DetailDto();
                    var internationDataRegexGroup = usgPurchesChargesMatch.Groups;

                    usgsumDetail.UNIQ_ID = Constants.USGSUM;
                    usgsumDetail.CHG_CLASS = Constants.LevelOne;
                    usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                    usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                    usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                    usgsumDetail.SUBSCRIBER = serviceId;

                    usgsumDetail.CHG_CODE_1 = internationDataRegexGroup[1].ToString().Replace(Constants.Pipe, ' ');
                    usgsumDetail.CHG_QTY1_ALLOWED = internationDataRegexGroup[3].ToString().Contains("unlimited") ? "" : internationDataRegexGroup[7].ToString().Contains("--") ? "0" : Utils.NumberFormat(internationDataRegexGroup[7].ToString().Replace(",", string.Empty));
                    usgsumDetail.CHG_QTY1_TYPE = Utils.GetChargesType(internationDataRegexGroup[2].ToString());
                    usgsumDetail.CHG_QTY1_USED = internationDataRegexGroup[10].ToString().Contains("--") ? "0" : Utils.NumberFormat(internationDataRegexGroup[10].ToString().Replace(",", string.Empty));
                    usgsumDetail.CHG_QTY1_BILLED = internationDataRegexGroup[13].ToString().Contains("--") ? "0" : Utils.NumberFormat(internationDataRegexGroup[13].ToString().Equals(string.Empty) ? "0" : internationDataRegexGroup[13].ToString().Replace(",", string.Empty));
                    usgsumDetail.CHG_AMT = internationDataRegexGroup[16].ToString().Contains("--") | internationDataRegexGroup[16].ToString().Contains("**") ? "0" : Utils.NumberFormat(internationDataRegexGroup[16].ToString().Replace(Constants.MoneySign, string.Empty));

                    usgsumDetail.CURRENCY = Constants.USD;
                    usgsumDetail.SHARE_IND = share ? "True" : string.Empty;
                    usgsumDetail.SP_INV_RECORD_TYPE = type;
                    usgsumDetail.INFO_ONLY_IND = "N";

                }
            }
            else
            {
                var internationDataSpecialCaseRegex = new Regex(Constants.InternationDataSpecialCaseRegex).Match(dataValues);

                if (internationDataSpecialCaseRegex.Success)
                {

                    usgsumDetail = new DetailDto();
                    var internationDataRegexGroup = internationDataSpecialCaseRegex.Groups;

                    var used = Convert.ToInt32(internationDataRegexGroup[2].ToString()) * Convert.ToDecimal(internationDataRegexGroup[4].ToString());

                    usgsumDetail.UNIQ_ID = Constants.USGSUM;
                    usgsumDetail.CHG_CLASS = Constants.LevelOne;
                    usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                    usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                    usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                    usgsumDetail.SUBSCRIBER = serviceId;

                    usgsumDetail.CHG_CODE_1 = internationDataRegexGroup[1].ToString().Replace(Constants.Pipe, ' ');
                    usgsumDetail.CHG_QTY1_ALLOWED = "0";
                    usgsumDetail.CHG_QTY1_TYPE = internationDataRegexGroup[6].ToString().ToLower();
                    usgsumDetail.CHG_QTY1_USED = Utils.NumberFormat(used.ToString());
                    usgsumDetail.CHG_QTY1_BILLED = Utils.NumberFormat(used.ToString());
                    usgsumDetail.CHG_AMT = Utils.NumberFormat(internationDataRegexGroup[8].ToString().Replace(Constants.MoneySign, string.Empty));

                    usgsumDetail.CURRENCY = Constants.USD;
                    usgsumDetail.SHARE_IND = share ? "True" : string.Empty;
                    usgsumDetail.SP_INV_RECORD_TYPE = type;
                    usgsumDetail.INFO_ONLY_IND = "N";
                }
            }

            return usgsumDetail;
        }

        public DetailDto GetOtherChargesCredits(string detailValues, string mrcDataSpName, string serviceId, string accountNumber)
        {
            DetailDto usgsumDetail = null;

            var therChargesCreditsRegex = new Regex(Constants.OtherChargesCreditsRegex).Match(detailValues);

            if (therChargesCreditsRegex.Success)
            {
                usgsumDetail = new DetailDto();
                var voiceRegexGroup = therChargesCreditsRegex.Groups;
                usgsumDetail.UNIQ_ID = Constants.OCC;
                usgsumDetail.CHG_CLASS = Constants.LevelOne;
                usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                usgsumDetail.SUBSCRIBER = serviceId;

                usgsumDetail.CHG_CODE_1 = voiceRegexGroup[1].ToString().Replace(Constants.StringPipe, Constants.WhiteSpace);
                usgsumDetail.CHG_AMT = Utils.NumberFormat(voiceRegexGroup[4].ToString());

                usgsumDetail.CURRENCY = Constants.USD;
                usgsumDetail.SP_INV_RECORD_TYPE = Constants.Equipment;

            }

            return usgsumDetail;
        }


        public DetailDto GetRoaming(string detailValues, string mrcDataSpName, string serviceId, bool shared, string accountNumber)
        {
            DetailDto usgsumDetail = null;

            var usageVoiceRegex = new Regex(Constants.UsageVoiceRegex).Match(detailValues);

            if (usageVoiceRegex.Success)
            {
                usgsumDetail = new DetailDto();

                var voiceRegexGroup = usageVoiceRegex.Groups;
                usgsumDetail.UNIQ_ID = Constants.USGSUM;
                usgsumDetail.CHG_CLASS = Constants.LevelOne;
                usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                usgsumDetail.SUBSCRIBER = serviceId;

                usgsumDetail.CHG_CODE_1 = voiceRegexGroup[1].ToString().Replace("|", string.Empty);
                usgsumDetail.CHG_QTY1_TYPE = Utils.GetChargesType(voiceRegexGroup[2].ToString());
                usgsumDetail.CHG_QTY1_USED = voiceRegexGroup[5].ToString();
                usgsumDetail.CHG_QTY1_ALLOWED = voiceRegexGroup[3].ToString().Contains("unlimited") ? string.Empty : (voiceRegexGroup[4].ToString().Contains("--") ? string.Empty : voiceRegexGroup[4].ToString().Replace("|", string.Empty));
                usgsumDetail.CHG_QTY1_BILLED = voiceRegexGroup[3].ToString().Contains("unlimited") ? voiceRegexGroup[5].ToString() : voiceRegexGroup[6].ToString();
                usgsumDetail.CHG_AMT = voiceRegexGroup[7].ToString().Contains("--") ? "0" : string.IsNullOrEmpty(voiceRegexGroup[7].ToString()) ? "0" : Utils.NumberFormat(voiceRegexGroup[7].ToString());


                usgsumDetail.CURRENCY = Constants.USD;
                usgsumDetail.INFO_ONLY_IND = "N";
                usgsumDetail.SHARE_IND = shared ? "True" : string.Empty;
                usgsumDetail.SP_INV_RECORD_TYPE = Constants.ROAMING;

            }
            else
            {
                var voiceRegex = new Regex(Constants.VoiceRegex).Match(detailValues);

                if (voiceRegex.Success)
                {
                    usgsumDetail = new DetailDto();
                    var voiceRegexGroup = voiceRegex.Groups;
                    usgsumDetail.UNIQ_ID = Constants.USGSUM;
                    usgsumDetail.CHG_CLASS = Constants.LevelOne;
                    usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                    usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                    usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                    usgsumDetail.SUBSCRIBER = serviceId;

                    usgsumDetail.CHG_CODE_1 = voiceRegexGroup[1].ToString();
                    usgsumDetail.CHG_AMT = Utils.NumberFormat(voiceRegexGroup[2].ToString());

                    usgsumDetail.CURRENCY = Constants.USD;
                    usgsumDetail.SP_INV_RECORD_TYPE = Constants.ROAMING;

                }

            }

            return usgsumDetail;
        }

        public DetailDto GetVoice(string detailValues, string mrcDataSpName, string serviceId, string accountNumber, bool shared)
        {
            DetailDto usgsumDetail = null;

            var usageVoiceRegex = new Regex(Constants.UsageVoiceRegex).Match(detailValues);

            if (usageVoiceRegex.Success)
            {
                usgsumDetail = new DetailDto();

                var voiceRegexGroup = usageVoiceRegex.Groups;
                usgsumDetail.UNIQ_ID = Constants.USGSUM;
                usgsumDetail.CHG_CLASS = Constants.LevelOne;
                usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                usgsumDetail.SUBSCRIBER = serviceId;

                usgsumDetail.CHG_CODE_1 = voiceRegexGroup[1].ToString().Replace("|", string.Empty);

                usgsumDetail.CHG_QTY1_TYPE = Utils.GetChargesType(voiceRegexGroup[2].ToString());
                usgsumDetail.CHG_QTY1_USED = voiceRegexGroup[5].ToString();
                usgsumDetail.CHG_QTY1_ALLOWED = voiceRegexGroup[3].ToString().Contains("unlimited") ? string.Empty : (voiceRegexGroup[4].ToString().Contains("--") ? string.Empty : voiceRegexGroup[4].ToString().Replace("|", string.Empty));
                usgsumDetail.CHG_QTY1_BILLED = voiceRegexGroup[3].ToString().Contains("unlimited") ? (voiceRegexGroup[5].ToString().Contains("--") ? string.Empty : voiceRegexGroup[5].ToString()) : (voiceRegexGroup[6].ToString().Contains("--") ? string.Empty : voiceRegexGroup[6].ToString());
                usgsumDetail.CHG_AMT = voiceRegexGroup[7].ToString().Contains("--") ? "0" : string.IsNullOrEmpty(voiceRegexGroup[7].ToString()) ? "0" : Utils.NumberFormat(voiceRegexGroup[7].ToString());

                usgsumDetail.CURRENCY = Constants.USD;
                usgsumDetail.SHARE_IND = shared ? "True" : string.Empty;
                usgsumDetail.ROAM_IND = voiceRegexGroup[1].ToString().Contains("Roaming") ? "True" : string.Empty;
                usgsumDetail.INFO_ONLY_IND = "N";
                usgsumDetail.SP_INV_RECORD_TYPE = Constants.VOICE;

            }
            else
            {
                var voiceRegex = new Regex(Constants.VoiceRegex).Match(detailValues);

                if (voiceRegex.Success)
                {
                    usgsumDetail = new DetailDto();

                    var voiceRegexGroup = voiceRegex.Groups;
                    usgsumDetail.UNIQ_ID = Constants.USGSUM;
                    usgsumDetail.CHG_CLASS = Constants.LevelOne;
                    usgsumDetail.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                    usgsumDetail.ACCT_LEVEL_2 = Constants.VerizonWireless;
                    usgsumDetail.SP_NAME = mrcDataSpName.TrimStart();
                    usgsumDetail.SUBSCRIBER = serviceId;

                    usgsumDetail.CHG_CODE_1 = voiceRegexGroup[1].ToString();
                    usgsumDetail.CHG_AMT = Utils.NumberFormat(voiceRegexGroup[2].ToString());

                    usgsumDetail.CURRENCY = Constants.USD;
                    usgsumDetail.SP_INV_RECORD_TYPE = Constants.VOICE;
                }
            }


            return usgsumDetail;
        }

        public DetailDto GetPurchases(string detailValues, string accountNumber, string mrcDataSpName, string serviceId, int dateDueMonth, int dateDueYear)
        {
            DetailDto mrcData = null;

            var purchasesRegex = new Regex(Constants.PurchasesRegex).Match(detailValues);


            if (purchasesRegex.Success)
            {
                mrcData = new DetailDto();
                var purchasesRegexGroup = purchasesRegex.Groups;

                var begYear = Convert.ToInt32(purchasesRegexGroup[1].ToString()) >= 1 && Convert.ToInt32(purchasesRegexGroup[1].ToString()) <= dateDueMonth ? dateDueYear : dateDueYear - 1;

                mrcData.UNIQ_ID = Constants.OCC;
                mrcData.CHG_CLASS = Constants.LevelOne;
                mrcData.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                mrcData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                mrcData.SP_NAME = mrcDataSpName.TrimStart();
                mrcData.SUBSCRIBER = serviceId;
                mrcData.CHG_CODE_1 = purchasesRegexGroup[4].ToString().Replace(Constants.Pipe, ' ').Trim();

                mrcData.BEG_CHG_DATE = new DateTime(begYear, Convert.ToInt32(purchasesRegexGroup[1].ToString()), Convert.ToInt32(purchasesRegexGroup[2].ToString())).ToString("M/d/yyyy");
                mrcData.END_CHG_DATE = new DateTime(begYear, Convert.ToInt32(purchasesRegexGroup[1].ToString()), Convert.ToInt32(purchasesRegexGroup[2].ToString())).ToString("M/d/yyyy"); ;
                mrcData.CHG_AMT = Utils.NumberFormat(purchasesRegexGroup[6].ToString());
                mrcData.CURRENCY = Constants.USD;
                mrcData.SP_INV_RECORD_TYPE = Constants.PURCHASES;

            }

            return mrcData;
        }

        public string RemoveLeftSide(string line)
        {

            return line.Contains(Constants.LineSeparator) ? line.Substring(line.IndexOf("%@") + 2) : line;
        }

        public bool previousPageCheck(int previousPage)
        { 
            if ((previousPage >= 995 && previousPage <= 1000) || (previousPage >= 9995 && previousPage <= 10000))
            {
                return true;
            }

            return false;
        }
    }
}
