using Com.MobileSolutions.Application.Dictionary;
using Com.MobileSolutions.Application.Enums;
using Com.MobileSolutions.Domain.Models;
using Spire.Pdf;
using Spire.Pdf.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Com.MobileSolutions.Application.Helpers
{
    public class ApplicationHelper
    {
        private PdfDocument document;
        private int previousPage = 0;
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

        public List<List<string>> ReadDetails(PdfDocument document, string path)
        {
            int lastDetailPage = 0;
            int errorCount = 0;
            List<string> buggedStrings = new List<string>();
            List<List<string>> detailList = new List<List<string>>();
            List<string> pageList = new List<string>();
            PdfPageCollection pages;

            lock (this)
            {
                document.LoadFromFile(path);
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


            //var text = pages[18015].ExtractText().Remove(0, 70);
            //RegexOptions options = RegexOptions.None;
            //Regex regex = new Regex("[ ]{2,}", options);
            //text = regex.Replace(text, "|");
            //pageList.Add(text);

            //detailList.Add(DetailPageReader(document, 18015));

            foreach (var page in pageList)
            {
                if (page.Contains(Constants.OverviewM2M))//Overview of Machine to Machine Activity
                {
                    var splittedPage = page.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                    detailList.Add(splittedPage);
                }

                //
                if (!page.Contains(Constants.QuickBillSummary) && (page.Contains(Constants.AccountMonthlyCharges) || page.Contains(Constants.AccountChargesAndCreditsContinue) ||
                    page.Contains(Constants.AccountChargesAndCredits) || page.Contains(Constants.PaymentsAdjustments) || page.Contains(Constants.PaymentsAdjustmentsContinue) ||
                    page.Contains(Constants.AdjustmentsContinued)))//Overview of Machine to Machine Activity
                {
                    var splittedPage = page.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                    detailList.Add(splittedPage);
                }

                ///Realize if the page is a detail page or not
                if (page.Contains(Constants.OverviewOfLines) || page.Contains(Constants.OverviewOfVoice))
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
                                            previousPage = Convert.ToInt32(Convert.ToInt32(valueRegex.Value) > document.Pages.Count ? valueRegex.Value.Substring(valueRegex.Value.ToString().Length - previousPage.ToString().Length) : valueRegex.Value);
                                            if (previousPage != 0)
                                            {
                                                detailList.Add(DetailPageReader(document, previousPage));
                                            }

                                        }
                                        else
                                        {
                                            Console.WriteLine(splittedPage[line]);
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(splittedPage[line]);
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

        public List<string> DetailPageReader(PdfDocument document, int pageNumber)
        {
            List<string> formattedPage = new List<string>();
            List<string> page;
            lock (this)
            {
                page = document.Pages[pageNumber - 1].ExtractText().Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var lineCount = 0;

            foreach (var line in page)
            {
                var len = line.Length;
                if (len > 57 && lineCount >= 4)
                {
                    var lineWithChars = line.Insert(57, "%@");
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(lineWithChars.TrimStart().TrimEnd(), "|"));
                }
                else
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
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

        public List<DetailDto> NextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber)
        {
            DetailDto detail = new DetailDto();
            List<DetailDto> detailList = new List<DetailDto>();
            List<string> formattedPage = new List<string>();
            string pageText;

            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }

            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }

                var detailArray = formattedPage.ToArray();

                var firstdetail = formattedPage.FirstOrDefault(d => d.Contains(Constants.VoiceTitle));
                var detailPosArray = Array.IndexOf(detailArray, firstdetail);

                var voiceFind = detailArray.FirstOrDefault(d => d.Contains(Constants.VoiceTitle));
                if (!string.IsNullOrEmpty(voiceFind))
                {

                    while (!detailArray[detailPosArray + 1].Contains("Total Voice"))
                    {//Total Voice

                        var shared = (detailArray[detailPosArray + 1].Contains("(shared)") || detailArray[detailPosArray + 2].Contains("(shared)") && detailArray[detailPosArray + 2].Split(Constants.Pipe).Length < 3) ? true : false;

                        DetailDto voiceTemp = this.GetVoice(detailArray[detailPosArray + 1], subscriber, lineNumber.Replace(Constants.Hyphen, string.Empty), accountNumber, shared);


                        if (voiceTemp != null)
                        {
                            detailList.Add(voiceTemp);
                        }

                        if (detailPosArray + 1 == detailArray.Length - 1)
                        {
                            // search for other pages
                            var pageRegex = new Regex(Constants.pageRegex);
                            var pageValidation = pageRegex.Match(detailArray[1]);
                            var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);

                            var nextPageList = NextDetailPageExtraction(pageNumber, lineNumber, subscriber, (int)DetailType.VOICE, accountNumber);

                            nextPageList.ForEach(val => detailList.Add(val));

                            break;
                        }

                        detailPosArray++;
                    }
                }
            }
            return detailList;
        }

        public List<DetailDto> MessagingNextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber)
        {
            DetailDto detail = new DetailDto();
            List<DetailDto> detailList = new List<DetailDto>();
            List<string> formattedPage = new List<string>();
            string pageText;
            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }

            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();


                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }

                var detailArray = formattedPage.ToArray();

                var messageFind = formattedPage.FirstOrDefault(d => d.Contains(Constants.MessagingTitle));
                if (!string.IsNullOrEmpty(messageFind))
                {
                    var detailPosArray = Array.IndexOf(detailArray, messageFind);

                    int values = detailPosArray;

                    while (!detailArray[values + 1].Contains("Total Messaging"))
                    {//Total Voice

                        var messagingValues = this.GetMessaging(detailArray[values + 1], subscriber.TrimStart(), lineNumber.Replace(Constants.Hyphen, string.Empty), accountNumber, Constants.MESSAGING);

                        if (messagingValues != null)
                        {
                            detailList.Add(messagingValues);
                        }

                        if (values + 1 == detailArray.Length - 1)
                        {
                            // search for other pages
                            var pageRegex = new Regex(Constants.pageRegex);
                            var pageValidation = pageRegex.Match(detailArray[1]);
                            var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);

                            lock (this)
                            {
                                var nextPageList = MessagingNextDetailPageExtraction(pageNumber, lineNumber, subscriber.TrimStart(), (int)DetailType.MESSAGING, accountNumber);

                                nextPageList.ForEach(val => detailList.Add(val));
                            }
                            break;
                        }

                        values++;
                    }
                }

            }
            return detailList;
        }


        public List<DetailDto> RoamingNextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber)
        {
            DetailDto detail = new DetailDto();
            List<DetailDto> detailList = new List<DetailDto>();
            List<string> formattedPage = new List<string>();

            string pageText;
            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }

            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();


                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }

                var detailArray = formattedPage.ToArray();

                var firstdetail = formattedPage.FirstOrDefault(d => d.Contains(Constants.RoamingTitle));
                if (!string.IsNullOrEmpty(firstdetail))
                {
                    var detailPosArray = Array.IndexOf(detailArray, firstdetail);


                    while (!detailArray[detailPosArray + 1].Contains(Constants.TotalRoaming))
                    {//Total Voice


                        var share = (detailArray[detailPosArray + 1].Contains("(shared)") || detailArray[detailPosArray + 2].Contains("(shared)") && detailArray[detailPosArray + 2].Split(Constants.Pipe).Length < 3) ? true : false;

                        DetailDto usgsumDetail = this.GetRoaming(detailArray[detailPosArray + 1], subscriber, lineNumber.Replace(Constants.Hyphen, string.Empty), share, accountNumber);

                        if (usgsumDetail != null)
                        {
                            detailList.Add(usgsumDetail);
                        }

                        if (detailPosArray + 1 == detailArray.Length - 1)
                        {
                            // search for other pages
                            var pageRegex = new Regex(Constants.pageRegex);
                            var pageValidation = pageRegex.Match(detailArray[1]);
                            var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);
                            lock (this)
                            {
                                var nextPageList = RoamingNextDetailPageExtraction(pageNumber, lineNumber, subscriber, (int)DetailType.VOICE, accountNumber);

                                nextPageList.ForEach(val => detailList.Add(val));
                            }

                            break;
                        }

                        detailPosArray++;
                    }
                }
            }
            return detailList;
        }

        public List<DetailDto> DataNextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber)
        {
            DetailDto detail = new DetailDto();
            List<DetailDto> detailList = new List<DetailDto>();
            List<string> formattedPage = new List<string>();
            string pageText;
            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }

            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();


                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }

                var detailArray = formattedPage.ToArray();

                var firstdetail = formattedPage.FirstOrDefault(d => d.Contains(Constants.DataTitle));

                if (!string.IsNullOrEmpty(firstdetail))
                {
                    var detailPosArray = Array.IndexOf(detailArray, firstdetail);

                    while (!detailArray[detailPosArray + 1].Contains(Constants.TotalData))
                    {//Total Voice


                        var share = (detailArray[detailPosArray + 1].Contains("(shared)") || detailArray[detailPosArray + 2].Contains("(shared)") && detailArray[detailPosArray + 2].Split(Constants.Pipe).Length < 3) ? true : false;
                        var usgsumDetail = this.GetData(detailArray[detailPosArray + 1], subscriber, lineNumber.Replace(Constants.Hyphen, string.Empty), share, accountNumber, Constants.DATA);


                        if (usgsumDetail != null)
                        {
                            detailList.Add(usgsumDetail);
                        }

                        if (detailPosArray + 1 == detailArray.Length - 1)
                        {
                            // search for other pages
                            var pageRegex = new Regex(Constants.pageRegex);
                            var pageValidation = pageRegex.Match(detailArray[1]);
                            var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);

                            var nextPageList = NextDetailPageExtraction(pageNumber, lineNumber, subscriber, (int)DetailType.DATA, accountNumber);

                            nextPageList.ForEach(val => detailList.Add(val));

                            break;
                        }
                        detailPosArray++;
                    }
                }
            }
            return detailList;
        }

        public List<DetailDto> OtherChargesCreditsNextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber)
        {
            DetailDto detail = new DetailDto();
            List<DetailDto> detailList = new List<DetailDto>();
            List<string> formattedPage = new List<string>();
            string pageText;
            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }

            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();


                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }

                var detailArray = formattedPage.ToArray();

                var firstdetail = formattedPage.FirstOrDefault(d => d.Contains(Constants.OtherChargesCredits));

                if (!string.IsNullOrEmpty(firstdetail))
                {
                    var detailPosArray = Array.IndexOf(detailArray, firstdetail);
                    var moneyRegex = new Regex(Constants.OnlyMoneyRegex);
                    while (!moneyRegex.IsMatch(detailArray[detailPosArray + 1].Split(Constants.Pipe)[detailArray[detailPosArray + 1].Split(Constants.Pipe).Length - 1]))
                    {//Total Voice


                        var usgsumDetail = this.GetOtherChargesCredits(detailArray[detailPosArray + 1], subscriber, lineNumber.Replace(Constants.Hyphen, string.Empty), accountNumber);


                        if (usgsumDetail != null)
                        {
                            detailList.Add(usgsumDetail);
                        }

                        if (detailPosArray + 1 == detailArray.Length - 1)
                        {
                            // search for other pages
                            var pageRegex = new Regex(Constants.pageRegex);
                            var pageValidation = pageRegex.Match(detailArray[1]);
                            var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);

                            var nextPageList = OtherChargesCreditsNextDetailPageExtraction(pageNumber, lineNumber, subscriber, (int)DetailType.DATA, accountNumber);

                            nextPageList.ForEach(val => detailList.Add(val));

                            break;
                        }
                        detailPosArray++;
                    }
                }
            }
            return detailList;
        }


        public List<DetailDto> SurchargesNextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber)
        {
            DetailDto detail = new DetailDto();
            List<DetailDto> detailList = new List<DetailDto>();
            List<string> formattedPage = new List<string>();

            var finalValueRegex = new Regex(Constants.FinalValueRegex);

            string pageText;
            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }

            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();


                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }

                // With this validation we check if this pag if is of the same persone

                var firstSurcharges = "";
                foreach (var item in formattedPage.FindAll(d => d.Contains(Constants.Surcharges)))
                {
                    if (new Regex(Constants.SurchargesTitleRegex).Match(item).Success)
                    {
                        firstSurcharges = item;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(firstSurcharges))
                {


                    var detailArray = formattedPage.ToArray();

                    var surPosArray = Array.IndexOf(detailArray, firstSurcharges);


                    while (!finalValueRegex.IsMatch(detailArray[surPosArray + 1].Split(Constants.Pipe)[detailArray[surPosArray + 1].Split(Constants.Pipe).Length - 1]) &&
                        !detailArray[surPosArray + 1].Contains(Constants.OtherChargesCredits))
                    {//Total Voice


                        var usgsumDetail = this.GetSurcharges(detailArray[surPosArray + 1], subscriber, lineNumber.Replace(Constants.Hyphen, string.Empty), accountNumber);

                        if (usgsumDetail != null)
                        {
                            detailList.Add(usgsumDetail);

                        }

                        if (surPosArray + 1 == detailArray.Length - 1)
                        {
                            // search for other pages
                            var pageRegex = new Regex(Constants.pageRegex);
                            var pageValidation = pageRegex.Match(detailArray[1]);
                            var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);

                            var nextPageList = SurchargesNextDetailPageExtraction(pageNumber, lineNumber, subscriber, (int)DetailType.SURCHARGES, accountNumber);

                            nextPageList.ForEach(val => detailList.Add(val));

                            break;
                        }
                        surPosArray++;
                    }

                }
            }
            return detailList;
        }

        public List<DetailDto> TaxesGovernmentalSurchargesNextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber)
        {

            List<DetailDto> detailList = new List<DetailDto>();


            DetailDto detail = new DetailDto();
            List<string> formattedPage = new List<string>();

            var finalValueRegex = new Regex(Constants.FinalValueRegex);

            string pageText;
            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }

            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }

                // With this validation we check if this pag if is of the same persone

                if (!string.IsNullOrEmpty(formattedPage.FirstOrDefault(d => d.Contains(Constants.TaxesGovernmentalSurcharges))))
                {

                    var detailArray = formattedPage.ToArray();

                    var firstTaxes = formattedPage.LastOrDefault(d => d.Contains(Constants.TaxesGovernmentalSurcharges));
                    var taxesPosArray = Array.IndexOf(detailArray, firstTaxes);


                    while (!finalValueRegex.IsMatch(detailArray[taxesPosArray + 1].Split(Constants.Pipe)[detailArray[taxesPosArray + 1].Split(Constants.Pipe).Length - 1]))
                    {

                        var surValues = this.GetTaxesGovermentalSurcharges(detailArray[taxesPosArray + 1], accountNumber, subscriber, lineNumber.Replace(Constants.Hyphen, string.Empty));

                        if (surValues != null)
                        {
                            detailList.Add(surValues);
                        }

                        if (taxesPosArray + 1 == detailArray.Length - 1)
                        {
                            // search for other pages
                            var pageRegex = new Regex(Constants.pageRegex);
                            var pageValidation = pageRegex.Match(detailArray[1]);
                            var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);

                            var nextPageList = TaxesGovernmentalSurchargesNextDetailPageExtraction(pageNumber, lineNumber, subscriber, (int)DetailType.TAXESGOVT, accountNumber);

                            nextPageList.ForEach(val => detailList.Add(val));

                            break;
                        }

                        taxesPosArray++;
                    }
                }
            }
            return detailList;
        }

        public List<DetailDto> PurchaseNextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber, int dateDueMonth, int dateDueYear)
        {
            DetailDto detail = new DetailDto();
            List<string> formattedPage = new List<string>();

            List<DetailDto> detailList = new List<DetailDto>();

            string pageText;
            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }

            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }

                var purchaseValue = formattedPage.FirstOrDefault(d => d.Contains(Constants.PurchasesTitle) || d.Contains(Constants.PurchasesTitle2));

                if (purchaseValue != null)
                {

                    var detailArray = formattedPage.ToArray();

                    var firstdetail = purchaseValue;
                    var detailPosArray = Array.IndexOf(detailArray, firstdetail);


                    int values = detailPosArray;

                    while (!detailArray[values + 1].Contains(Constants.TotalPurchases))//Total Voice
                    {


                        var detailValues = this.GetPurchases(detailArray[values + 1], accountNumber, subscriber, lineNumber.Replace(Constants.Hyphen, string.Empty), dateDueMonth, dateDueYear);

                        if (detailValues != null)
                        {
                            detailList.Add(detailValues);
                        }

                        if (values + 1 == detailArray.Length - 1)
                        {
                            // search for other pages
                            var pageRegex = new Regex(Constants.pageRegex);
                            var pageValidation = pageRegex.Match(detailArray[1]);
                            var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);

                            var nextPageList = PurchaseNextDetailPageExtraction(pageNumber, lineNumber, subscriber, detailType, accountNumber, dateDueMonth, dateDueYear);

                            nextPageList.ForEach(val => detailList.Add(val));

                            break;
                        }

                        values++;
                    }
                }
            }

            return detailList;
        }

        public List<DetailDto> TotalCurrentChargesNextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber, decimal accountMonthlyChargesSum)
        {
            DetailDto detail = new DetailDto();
            List<string> formattedPage = new List<string>();

            List<DetailDto> detailList = new List<DetailDto>();

            string pageText;
            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }


            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }

                var totalCurrentChargesFor = formattedPage.FirstOrDefault(d => d.Contains(Constants.TotalCurrentChargesFor));
                if (!string.IsNullOrEmpty(totalCurrentChargesFor))
                {

                    var pageRegex = new Regex(Constants.TotalCurrentChargesRegex).Match(totalCurrentChargesFor).Groups;


                    var detailArray = formattedPage.ToArray();

                    DetailDto mrcTotal = new DetailDto();

                    var mrcTotalChgAmt = formattedPage.FirstOrDefault(d => d.Contains(Constants.TotalCurrentChargesFor)).Split(Constants.Pipe);

                    var totalValue = System.Convert.ToDecimal(pageRegex[2].ToString().Replace(Constants.MoneySign, string.Empty).Replace(",", string.Empty)) + accountMonthlyChargesSum;

                    mrcTotal.UNIQ_ID = Constants.MRC;
                    mrcTotal.CHG_CLASS = Constants.LevelOne;
                    mrcTotal.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                    mrcTotal.ACCT_LEVEL_2 = Constants.VerizonWireless;
                    mrcTotal.SP_NAME = subscriber;
                    mrcTotal.SUBSCRIBER = lineNumber.Replace(Constants.Hyphen, string.Empty);
                    mrcTotal.CHG_CODE_1 = pageRegex[1].ToString();
                    mrcTotal.CHG_AMT = totalValue.ToString();
                    mrcTotal.CURRENCY = Constants.USD;
                    mrcTotal.INFO_ONLY_IND = "Y";
                    mrcTotal.SP_INV_RECORD_TYPE = Constants.MonthlyCharges.ToUpper();
                    mrcTotal.UDF = "";

                    detailList.Add(mrcTotal);

                }
            }
            return detailList;
        }

        public List<DetailDto> InternationNextDetailPageExtraction(int page, string lineNumber, string subscriber, int detailType, string accountNumber)
        {
            DetailDto detail = new DetailDto();
            List<string> formattedPage = new List<string>();

            List<DetailDto> detailList = new List<DetailDto>();

            string pageText;
            lock (this)
            {
                pageText = document.Pages[page].ExtractText();
            }

            if (!pageText.Contains(Constants.YourPlanMonthlyCharges))
            {
                var nextPage = pageText.Remove(0, 70).Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                foreach (var line in nextPage)
                {
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    formattedPage.Add(regex.Replace(line.TrimStart().TrimEnd(), "|"));
                }
                if (!string.IsNullOrEmpty(formattedPage.FirstOrDefault(d => d.Contains(Constants.InternationTitle))))
                {
                    var detailArray = formattedPage.ToArray();
                    var firstdetail = formattedPage.FirstOrDefault(d => d.Contains(Constants.InternationTitle));
                    var detailPosArray = Array.IndexOf(detailArray, firstdetail);

                    while (!detailArray[detailPosArray + 1].Contains(Constants.TotaInternational))
                    {//Total Voice


                        var detailValues = detailArray[detailPosArray + 1];

                        if (new Regex(Constants.InternationalMessageRegex).IsMatch(detailValues))
                        {

                            var messagingValuesInter = this.GetMessaging(detailArray[detailPosArray + 1], subscriber, lineNumber.Replace(Constants.Hyphen, string.Empty), accountNumber, Constants.INTERNATIONAL);

                            if (messagingValuesInter != null)
                            {
                                detailList.Add(messagingValuesInter);
                            }
                        }
                        else
                        {

                            if (new Regex(Constants.InternationDataRegex).IsMatch(detailValues))
                            {

                                var usgsumDetail = this.GetData(detailValues, subscriber, lineNumber.Replace(Constants.Hyphen, string.Empty), false, accountNumber, Constants.INTERNATIONAL);


                                if (usgsumDetail != null)
                                {
                                    detailList.Add(usgsumDetail);
                                }
                            }
                            else
                            {
                                DetailDto usgsumData = new DetailDto();

                                var internationMinutesRegex = new Regex(Constants.InternationalMinutesRegex).Match(detailValues);

                                if (internationMinutesRegex.Success)
                                {

                                    var internationMinutesRegexGroup = internationMinutesRegex.Groups;

                                    usgsumData.UNIQ_ID = Constants.USGSUM;
                                    usgsumData.CHG_CLASS = Constants.LevelOne;
                                    usgsumData.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                                    usgsumData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                    usgsumData.SP_NAME = subscriber.TrimStart();
                                    usgsumData.SUBSCRIBER = lineNumber.Replace(Constants.Hyphen, string.Empty);

                                    usgsumData.CHG_CODE_1 = internationMinutesRegexGroup[1].ToString().Replace(Constants.Pipe, ' ');
                                    usgsumData.CHG_QTY1_ALLOWED = internationMinutesRegexGroup[3].ToString().Contains("unlimited") ? "" : internationMinutesRegexGroup[5].ToString();
                                    usgsumData.CHG_QTY1_TYPE = Utils.GetChargesType(internationMinutesRegexGroup[2].ToString());
                                    usgsumData.CHG_QTY1_USED = internationMinutesRegexGroup[6].ToString().Replace(Constants.Hyphen, string.Empty);
                                    usgsumData.CHG_QTY1_BILLED = internationMinutesRegexGroup[7].ToString().Replace(Constants.Hyphen, string.Empty).Equals(string.Empty) ? "0" : internationMinutesRegexGroup[7].ToString();
                                    usgsumData.CHG_AMT = Utils.NumberFormat(internationMinutesRegexGroup[8].ToString().Replace(Constants.Hyphen, string.Empty).Replace(Constants.MoneySign, string.Empty));

                                    usgsumData.CURRENCY = Constants.USD;
                                    usgsumData.SP_INV_RECORD_TYPE = Constants.INTERNATIONAL;

                                    detailList.Add(usgsumData);
                                }
                                else
                                {
                                    var internationalVoiceRegex = new Regex(Constants.InternationVoiceRegex).Match(detailValues);

                                    if (internationalVoiceRegex.Success)
                                    {
                                        var internationalVoiceRegexGroup = internationalVoiceRegex.Groups;

                                        usgsumData.UNIQ_ID = Constants.USGSUM;
                                        usgsumData.CHG_CLASS = Constants.LevelOne;
                                        usgsumData.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                                        usgsumData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                        usgsumData.SP_NAME = subscriber.TrimStart();
                                        usgsumData.SUBSCRIBER = lineNumber.Replace(Constants.Hyphen, string.Empty);
                                        usgsumData.CHG_CODE_1 = internationalVoiceRegexGroup[2].ToString().Replace(Constants.Pipe, ' ');

                                        usgsumData.CHG_AMT = Utils.NumberFormat(internationalVoiceRegexGroup[3].ToString().Replace(Constants.MoneySign, string.Empty));
                                        usgsumData.CURRENCY = Constants.USD;
                                        usgsumData.SP_INV_RECORD_TYPE = Constants.INTERNATIONAL;

                                        detailList.Add(usgsumData);
                                    }
                                }
                            }
                        }




                        if (detailPosArray + 1 == detailArray.Length - 1)
                        {
                            // search for other pages
                            var pageRegex = new Regex(Constants.pageRegex);
                            var pageValidation = pageRegex.Match(detailArray[1]);
                            var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);

                            var nextPageList = InternationNextDetailPageExtraction(pageNumber, lineNumber, subscriber, (int)DetailType.VOICE, accountNumber);

                            nextPageList.ForEach(val => detailList.Add(val));

                            break;
                        }

                        detailPosArray++;
                    }
                }

            }
            return detailList;
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
                    usgsumDetail.CHG_AMT = internationDataRegexGroup[16].ToString().Contains("--") ? "0" : Utils.NumberFormat(internationDataRegexGroup[16].ToString().Replace(Constants.MoneySign, string.Empty));

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

                var begYear = Convert.ToInt32(purchasesRegexGroup[2].ToString()) >= 1 && Convert.ToInt32(purchasesRegexGroup[2].ToString()) <= dateDueMonth ? dateDueYear : dateDueYear - 1;

                mrcData.UNIQ_ID = Constants.OCC;
                mrcData.CHG_CLASS = Constants.LevelOne;
                mrcData.ACCT_LEVEL = accountNumber.Replace(Constants.Hyphen, string.Empty);
                mrcData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                mrcData.SP_NAME = mrcDataSpName.TrimStart();
                mrcData.SUBSCRIBER = serviceId;
                mrcData.CHG_CODE_1 = purchasesRegexGroup[4].ToString().Replace(Constants.Pipe, ' ').Trim();

                mrcData.BEG_CHG_DATE = new DateTime(begYear, Convert.ToInt32(purchasesRegexGroup[2].ToString()), Convert.ToInt32(purchasesRegexGroup[3].ToString())).ToString("M/d/yyyy");
                mrcData.END_CHG_DATE = new DateTime(begYear, Convert.ToInt32(purchasesRegexGroup[2].ToString()), Convert.ToInt32(purchasesRegexGroup[3].ToString())).ToString("M/d/yyyy"); ;
                mrcData.CHG_AMT = Utils.NumberFormat(purchasesRegexGroup[5].ToString());
                mrcData.CURRENCY = Constants.USD;
                mrcData.SP_INV_RECORD_TYPE = Constants.PURCHASES;

            }

            return mrcData;
        }

        public string RemoveLeftSide(string line)
        {
            var cutRegex = new Regex(@".*(?=%@)");
            var convertedLine = cutRegex.Match(line).Value;
            return !string.IsNullOrEmpty(convertedLine) ? line.Replace(convertedLine, string.Empty).Replace("%@", string.Empty) : line.Replace("%@", string.Empty);
        }
    }
}
