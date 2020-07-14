using Com.MobileSolutions.Application.Dictionary;
using Com.MobileSolutions.Application.Enums;
using Com.MobileSolutions.Application.Helpers;
using Com.MobileSolutions.Domain.Models;
using Ionic.Zip;
using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Com.MobileSolutions.VerizonWirelessReader
{
    public class VerizonReader
    {
        /// <summary>
        /// Helper with usable methods for the pdf reading.
        /// </summary>
        ApplicationHelper helper;

        private string accountNumber;
        private int dateDueMonth;
        private int dateDueDay;
        private int dateDueYear;
        private string invoiceNumber;
        private decimal lineTotal = System.Convert.ToDecimal(0.0);
        private decimal accountTotal = System.Convert.ToDecimal(0.0);
        private decimal reconciliationValue = 0;
        private string acctType = string.Empty;
        private PdfDocument document;
        private List<String> noReconciledLine = new List<String>();

        /// <summary>
        /// Verizon Reader class constructor.
        /// </summary>
        public VerizonReader(PdfDocument document, string path)
        {
            helper = new ApplicationHelper(document, path);
            this.document = document;
        }


        public FileDto GetFileValues(string fileName)
        {

            var file = new FileDto();

            file.UNIQ_ID = ""; // definir
            file.UNIBILL_VERSION = "2"; // definir
            file.ACCT_LEVEL_1 = this.accountNumber.Replace(Constants.Hyphen, string.Empty); // account number
            file.ACCT_TYPE = "A"; // definir
            file.FILE_IDENTIFIER = "837337";
            file.DATE_RECEIVED_FROM_SP = DateTime.Now.ToString("dd/MM/yyyy");
            file.UNIBIL_GEN_DT = DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
            file.EDI_SENDER_ID = "";
            file.EDI_RECEIVER_ID = "";
            file.EDI_CONTROL_NUMBER = "";
            file.MAP_USED = "pdf_verizon_wireless";
            file.SP_FILENAME = new StringBuilder().Append(invoiceNumber).Append("_").Append(DateTime.Now.ToString("yyyyMMddHHmmssffff")).Append("_").Append(this.accountNumber.Replace(Constants.Hyphen, string.Empty)).Append(".txt").ToString();

            file.SP_CUST_ID = ""; //recibir el standard de Luis
            file.SP_ORIG_SYS = Constants.VerizonWireless;
            file.SP_VERSION = "";
            file.SP_RELEASE = "";
            file.SP_PRODUCT = "";
            file.SP_MEDIA_CREATION_DATE = "";
            file.SP_DOCUMENT_ID = "";
            file.SP_SUBSCRIPTION_ID = "";
            file.SP_CUST_NAME = "";
            file.FIRST_INV_IND = "";

            return file;
        }

        /// <summary>
        /// Get header values from list and creates the headerDto.
        /// </summary>
        /// <param name="headerValues">Header Values</param>
        /// <returns>An instance of <see cref="HeaderDto"/></returns>
        public HeaderDto GetHeaderValues(List<string> headerValues)
        {
            lock (this)
            {
                var billDateText = headerValues.FirstOrDefault(p => p.Contains("Bill Date"));
                if (!string.IsNullOrEmpty(billDateText))
                {
                    var detailArray = headerValues.ToArray();
                    var dataStartPosArray = Array.IndexOf(detailArray, billDateText);

                    var headerRegex = Regex.Match(detailArray[dataStartPosArray], Constants.HeaderRegex);

                    if (headerRegex.Success)
                    {
                        var headerRegexGroup = headerRegex.Groups;
                        dateDueMonth = Convert.ToDateTime(headerRegexGroup[1].ToString()).Month;
                        dateDueDay = Convert.ToDateTime(headerRegexGroup[1].ToString()).Day;
                        dateDueYear = Convert.ToDateTime(headerRegexGroup[1].ToString()).Year;

                    }
                    //DateTime(begYear, Convert.ToInt32(voiceGroup[4].ToString()), Convert.ToInt32(voiceGroup[5].ToString())).ToString("M/d/yyyy");
                }


                headerValues = helper.RemoveComma(headerValues);

                List<String> creditBalanceMatch = headerValues.FindAll(b => b.Contains(Constants.CreditBalance));
                var creditBalance = "0";
                foreach (String element in creditBalanceMatch)
                {
                    var creditBalanceRegex = Regex.Match(element, Constants.CreditBalanceRegex);

                    if (creditBalanceRegex.Success)
                    {
                        creditBalance = Utils.NumberFormat(creditBalanceRegex.Groups[1].ToString().Replace(Constants.MoneySign, string.Empty));
                        break;
                    }
                }

                if (creditBalance.Equals("0"))
                {
                    List<String> balanceForwardDueImmediatelyMatch = headerValues.FindAll(b => b.Contains(Constants.BalanceForwardDueImmediately));
                    foreach (String element in balanceForwardDueImmediatelyMatch)
                    {
                        var balanceForwardDueImmediatelyRegex = Regex.Match(element, Constants.BalanceForwardDueImmediatelyRegex);

                        if (balanceForwardDueImmediatelyRegex.Success)
                        {
                            creditBalance = Utils.NumberFormat(balanceForwardDueImmediatelyRegex.Groups[2].ToString().Replace(Constants.MoneySign, string.Empty));
                            break;
                        }
                    }
                }

                var accountNumberMatch = headerValues.LastOrDefault(p => p.Contains(Constants.AccountNumber));
                this.accountNumber = accountNumberMatch != null ? Regex.Match(accountNumberMatch, Constants.AccountNumberRegex).Groups[1].ToString() : string.Empty;

                var totMrcChgsMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.MonthlyCharges));
                var totMrcChgs = totMrcChgsMatch != null ? Regex.Match(totMrcChgsMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                var totOccPurchasesMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.PurchasesHeaderTitle));
                var totOccPurchases = totOccPurchasesMatch != null ? Regex.Match(totOccPurchasesMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";


                var totOccChgsMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.EquipmentCharges));
                var totOccChgs = totOccChgsMatch != null ? Regex.Match(totOccChgsMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                List<String> temp = headerValues.FindAll(b => b.Contains(Constants.TotalAmountTitle));
                var totalAmountDue = "0";
                foreach (String element in temp)
                {
                    var totalAmountRegex = Regex.Match(element, Constants.TotalAmountRegex);

                    if (totalAmountRegex.Success)
                    {
                        totalAmountDue = totalAmountRegex.Groups[2].ToString().Replace(Constants.MoneySign, string.Empty);
                        break;
                    }
                }


                List<String> totalChargesDuebyMatch = headerValues.FindAll(b => b.Contains(Constants.TotalChargesDuebyTitle));
                foreach (String element in totalChargesDuebyMatch)
                {
                    var totalAmountRegex = Regex.Match(element, Constants.TotalChargesDuebyRegex);

                    if (totalAmountRegex.Success)
                    {
                        totalAmountDue = totalAmountRegex.Groups[1].ToString().Replace(Constants.MoneySign, string.Empty);
                        break;
                    }
                }


                var otherChargesMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.OtherCharges));
                var otherCharges = otherChargesMatch != null ? Regex.Match(otherChargesMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                var taxesSurChargesMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.TaxesSurCharges));
                var taxesSurCharges = taxesSurChargesMatch != null ? Regex.Match(taxesSurChargesMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                var accountChangesAndCreditsMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.AccountChargesAndCredits));



                var accountChangesAndCredits = "0";


                if (accountChangesAndCreditsMatch != null)
                {
                    var accountChangesAndCreditsRegex = new Regex(Constants.AccountChargesCreditsTitle).Match(accountChangesAndCreditsMatch);
                    if (accountChangesAndCreditsRegex.Success)
                    {
                        accountChangesAndCredits = accountChangesAndCreditsRegex.Groups[1].ToString().Replace(Constants.MoneySign, string.Empty);
                    }
                    else
                    {
                        var detailArray = headerValues.ToArray();
                        var yourPlanWordArray = Array.IndexOf(detailArray, accountChangesAndCreditsMatch);
                        var afterAccountChangesAndCreditsRegex = new Regex(Constants.AfterAccountChargesCreditsTitle).Match(detailArray[yourPlanWordArray + 1]);

                        if (afterAccountChangesAndCreditsRegex.Success)
                        {
                            accountChangesAndCredits = afterAccountChangesAndCreditsRegex.Groups[1].ToString().Replace(Constants.MoneySign, string.Empty);
                        }
                    }
                }
                //var accountChangesAndCredits = accountChangesAndCreditsMatch != null ? Regex.Match(accountChangesAndCreditsMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                var totTaxSur = Convert.ToString(System.Convert.ToDecimal(otherCharges) + System.Convert.ToDecimal(taxesSurCharges));//+ System.Convert.ToDecimal(accountChangesAndCredits)

                var voiceMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.Voice));
                var voice = voiceMatch != null ? Regex.Match(voiceMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                var messagingMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.Messaging));
                var messaging = messagingMatch != null ? Regex.Match(messagingMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                var dataMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.Data));
                var data = dataMatch != null ? Regex.Match(dataMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                var internationalMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.International));
                var international = internationalMatch != null ? Regex.Match(internationalMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                var roamingMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.Roaming));
                var roaming = internationalMatch != null ? Regex.Match(internationalMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";


                var previousBalanceMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.PreviousBalance));
                var paymentMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.Payment));

                var adjustmentMatch = headerValues.FirstOrDefault(p => p.Contains(Constants.Adjustments));

                var totUsageChgs = System.Convert.ToDecimal(voice) + System.Convert.ToDecimal(messaging) + System.Convert.ToDecimal(data) + System.Convert.ToDecimal(international) + System.Convert.ToDecimal(roaming);

                this.invoiceNumber = Regex.Match(headerValues.FirstOrDefault(p => p.Contains(Constants.InvoiceNumber)), Constants.NumberRegex).ToString();

                var dueDate = headerValues.FirstOrDefault(new Regex(Constants.DueDateRegex).IsMatch);
                var dueDateString = !string.IsNullOrEmpty(dueDate) ? Regex.Match(dueDate, Constants.DueDateRegex).Groups[4].Value : string.Empty;
                var billDate = headerValues.FirstOrDefault(new Regex(Constants.BillDateRegex).IsMatch);
                var billDateString = Regex.Match(billDate, Constants.BillDateRegex).Groups[3].Value;
                var quickBillDate = headerValues.FirstOrDefault(new Regex(Constants.QuickBillSummaryDateRegex).IsMatch);
                var quickBillDateString = Regex.Match(quickBillDate, Constants.QuickBillSummaryDateRegex).Groups[3].Value;
                var quickBillStartDate = !string.IsNullOrEmpty(quickBillDateString) ? quickBillDateString.Split("-")[0] : string.Empty;
                var quickBillEndDate = !string.IsNullOrEmpty(quickBillDateString) ? quickBillDateString.Split("-")[1] : string.Empty;
                var quickBillStartYear = !string.IsNullOrEmpty(quickBillStartDate) ? Convert.ToDateTime(Utils.ConvertTextMonthDateFormat(quickBillStartDate)).Month >= 1 && Convert.ToDateTime(Utils.ConvertTextMonthDateFormat(quickBillStartDate)).Month <= this.dateDueMonth ? this.dateDueYear : this.dateDueYear - 1 : 0;
                var quickBillEndYear = !string.IsNullOrEmpty(quickBillEndDate) ? Utils.GetYear(Convert.ToDateTime(Utils.ConvertTextMonthDateFormat(quickBillStartDate)).Month, Convert.ToDateTime(Utils.ConvertTextMonthDateFormat(quickBillEndDate)).Month, this.dateDueMonth, this.dateDueYear) : 0;

                HeaderDto header = new HeaderDto();
                header.UNIQ_ID = string.Empty;
                header.ACCT_LEVEL = Constants.LevelOne;
                header.ACCT_LEVEL_1 = accountNumber.Replace(Constants.Hyphen, string.Empty);
                header.ACCT_LEVEL_2 = string.Empty;
                header.SP_INV_NUM = invoiceNumber;

                header.INV_DATE = !string.IsNullOrEmpty(billDateString) ? Convert.ToDateTime(Utils.ConvertTextMonthDateFormat(billDateString)).ToString(Constants.FormatedDate) : string.Empty;//Bill Date
                header.BILL_PERIOD_START = !string.IsNullOrEmpty(quickBillStartDate) && quickBillStartYear != 0 ? new DateTime(quickBillStartYear, Convert.ToDateTime(Utils.ConvertTextMonthDateFormat(quickBillStartDate)).Month, Convert.ToDateTime(Utils.ConvertTextMonthDateFormat(quickBillStartDate)).Day).ToString(Constants.FormatedDate) : string.Empty;//Quick bill summary (fechas)
                header.BILL_PERIOD_END = !string.IsNullOrEmpty(quickBillEndDate) && quickBillEndYear != 0 ? new DateTime(quickBillEndYear, Convert.ToDateTime(Utils.ConvertTextMonthDateFormat(quickBillEndDate)).Month, Convert.ToDateTime(Utils.ConvertTextMonthDateFormat(quickBillEndDate)).Day).ToString(Constants.FormatedDate) : string.Empty;//Quick bill summary (fechas)
                header.DUE_DATE = !string.IsNullOrEmpty(dueDateString) ? Convert.ToDateTime(Regex.Match(dueDate, Constants.DueDateRegex).Groups[4].Value).ToString(Constants.FormatedDate) : header.INV_DATE;
                header.DATE_ISSUED = string.Empty;

                //var begYear = Convert.ToInt32(voiceGroup[4].ToString()) >= 1 && Convert.ToInt32(voiceGroup[4].ToString()) <= this.dateDueMonth ? this.dateDueYear : this.dateDueYear - 1;

                //var endYear = Utils.GetYear(Convert.ToInt32(voiceGroup[4].ToString()), Convert.ToInt32(voiceGroup[6].ToString()), this.dateDueMonth, this.dateDueYear);

                header.PREV_BILL_AMT = previousBalanceMatch != null ? Regex.Match(previousBalanceMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";


                header.PMTS_RCVD = paymentMatch != null ? Regex.Match(paymentMatch, Constants.MoneyRegex).Groups[2].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                header.PMTS_APP_THRU_DATE = DateTime.Now.ToString(Constants.FormatedDate);


                header.BAL_FWD_ADJ = adjustmentMatch != null ? Regex.Match(adjustmentMatch, Constants.Adjustments + "\\|" + Constants.OnlyMoneyRegex).Groups[1].ToString().Replace(Constants.MoneySign, string.Empty) : "0";

                header.BAL_FWD = creditBalance.Replace(Constants.MoneySign, string.Empty);

                header.TOT_NEW_CHG_ADJ = "0"; //Definir
                header.TOT_NEW_CHGS = Convert.ToString(System.Convert.ToDecimal(accountChangesAndCredits) + System.Convert.ToDecimal(totMrcChgs) + System.Convert.ToDecimal(totOccChgs) + totUsageChgs + System.Convert.ToDecimal(totTaxSur));
                header.TOT_AMT_DUE_ADJ = "0"; //Definir
                header.TOT_AMT_DUE = totalAmountDue;
                header.TOT_MRC_CHGS = totMrcChgs;
                header.TOT_OCC_CHGS = Convert.ToString(System.Convert.ToDecimal(totOccChgs) + System.Convert.ToDecimal(totOccPurchases));
                header.TOT_USAGE_CHGS = Convert.ToString(totUsageChgs - System.Convert.ToDecimal(totOccPurchases));
                header.TOT_TAXSUR = totTaxSur;
                header.TOT_DISC_AMT = "0"; //Definir
                header.SP_ACCT_STATUS_IND = string.Empty;
                header.SP_NAME = Constants.VerizonWireless;
                //header.SP_REMIT_ADDR_1 = headerValues.FirstOrDefault(p => p.Contains(Constants.Address));
                header.SP_REMIT_ADDR_2 = string.Empty;
                header.SP_REMIT_ADDR_3 = string.Empty;
                header.SP_REMIT_ADDR_4 = string.Empty;
                header.SP_REMIT_CITY = "";// Regex.Match(headerValues.FirstOrDefault(p => p.Contains(Constants.City)), Constants.RemitRegex).Groups[1].ToString();
                header.SP_REMIT_STATE = "";// "PA";// Regex.Match(headerValues.FirstOrDefault(p => p.Contains(Constants.City)), Constants.RemitRegex).Groups[3].ToString();
                header.SP_REMIT_ZIP = "";// "18002-5505";// Regex.Match(headerValues.FirstOrDefault(p => p.Contains(Constants.City)), Constants.RemitRegex).Groups[4].ToString();
                header.SP_REMIT_COUNTRY = string.Empty;
                header.SP_INQUIRY_TEL_NUM = string.Empty;
                header.BILLED_COMPANY_NAME = string.Empty;
                header.BILLED_COMPANY_ADDR_1 = ""; ///Definir como obtener del documento, está en una tabla en la parte izquierda
                /*header.BILLED_COMPANY_ADDR_2 = "AIR MEDICAL GROUP HOLDINGS LLC"; ///Definir como obtener del documento, está en una tabla en la parte izquierda
                header.BILLED_COMPANY_ADDR_3 = "TELIGISTICS"; ///Definir como obtener del documento, está en una tabla en la parte izquierda
                header.BILLED_COMPANY_ADDR_4 = "1700 RESEARCH BLVD STE 102"; ///Definir como obtener del documento, está en una tabla en la parte izquierda
                header.BILLED_COMPANY_CITY = "ROCKVILLE"; ///Definir como obtener del documento, está en una tabla en la parte izquierda en la parte inferior, debería usarse la regex RemitRegex para extraer los valores al identificar como obtenerlos
                header.BILLED_COMPANY_STATE = "MD"; ///Definir como obtener del documento, está en una tabla en la parte izquierda en la parte inferior, debería usarse la regex RemitRegex para extraer los valores al identificar como obtenerlos
                header.BILLED_COMPANY_ZIP = "20850-6121"; ///Definir como obtener del documento, está en una tabla en la parte izquierda en la parte inferior, debería usarse la regex RemitRegex para extraer los valores al identificar como obtenerlos
                */
                header.CURRENCY = Constants.USD;
                header.SP_TOT_NEW_CHGS = header.TOT_NEW_CHGS;
                header.SP_TOT_AMT_DUE = header.TOT_AMT_DUE;
                header.SP_BAL_FWD = header.BAL_FWD;
                header.UDF = string.Empty;

                this.reconciliationValue = System.Convert.ToDecimal(header.TOT_AMT_DUE) - (System.Convert.ToDecimal(header.BAL_FWD_ADJ) - System.Convert.ToDecimal(header.BAL_FWD));

                return header;
            }
        }

        public List<DetailDto> GetDetailValues(List<List<string>> details, PdfDocument document, string path)
        {
            var result = new List<DetailDto>();
            var usgsumResult = new List<DetailDto>();
            var surTaxesResult = new List<DetailDto>();
            var occResult = new List<DetailDto>();
            var adjAccountResult = new List<DetailDto>();
            var adjResult = new List<DetailDto>();
            var moneyRegex = new Regex(Constants.OnlyMoneyRegex);
            var finalValueRegex = new Regex(Constants.FinalValueRegex);
            var currentSection = "";
            var M2MPlanName = "";

            foreach (var detail in details)
            {
                var detailArray = detail.ToArray();

                var spNameChecker = detail.FirstOrDefault(p => p.Contains(Constants.SummaryFor));

                var accountMonthlyChargesSum = System.Convert.ToDecimal(0.0);

                var serviceId = "";
                var lineNumber = "";
                if (spNameChecker != null)
                {
                    var mrcDataSpName = "";
                    var totalCurrentChargesFor = detail.FirstOrDefault(d => d.Contains(Constants.TotalCurrentChargesFor));
                    var totalUsagePurchaseCharges = detail.FirstOrDefault(d => d.Contains(Constants.TotalUsagePurchaseCharges));

                    var summaryRegex = new Regex(Constants.SummaryRegex).Match(spNameChecker);

                    var summaryGroup = summaryRegex.Groups;
                    mrcDataSpName = summaryGroup[1].ToString().Replace(Constants.Pipe, ' ').Trim();
                    serviceId = summaryGroup[2].ToString().Replace("-", string.Empty);
                    lineNumber = summaryGroup[2].ToString();

                    var usagePurchesFind = detail.FirstOrDefault(d => d.Contains(Constants.UsagePurchaseCharges));
                    var yourPlanMonthlyFind = detail.FirstOrDefault(d => d.Contains(Constants.YourPlanMonthlyCharges));

                    if (usagePurchesFind == null && yourPlanMonthlyFind == null)
                    {
                        var yourPlanWord = detail.FirstOrDefault(d => d.Contains(Constants.YourPlan));
                        var yourPlanWordArray = Array.IndexOf(detailArray, yourPlanWord);

                        var planName = "";
                        do
                        {
                            planName = detailArray[yourPlanWordArray + 1];
                            yourPlanWordArray++;
                        }
                        while (string.IsNullOrEmpty(planName) || planName.Contains(Constants.PlanFrom));
                        
                        
                        DetailDto mrcData = new DetailDto();


                        mrcData.UNIQ_ID = Constants.MRC;
                        mrcData.CHG_CLASS = Constants.LevelOne;
                        mrcData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                        mrcData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                        mrcData.SP_NAME = mrcDataSpName.TrimStart();
                        mrcData.SUBSCRIBER = serviceId;
                        mrcData.CHG_CODE_1 = planName.Replace(Constants.LineSeparator, string.Empty);
                        mrcData.CHG_CODE_2 = planName.Replace(Constants.LineSeparator, string.Empty);
                        mrcData.CHG_QTY1_BILLED = "0";

                        mrcData.CHG_AMT = "0";
                        mrcData.CURRENCY = Constants.USD;
                        mrcData.INFO_ONLY_IND = "N";
                        mrcData.SP_INV_RECORD_TYPE = Constants.MONTHLY_CHARGES;

                        result.Add(mrcData);


                        DetailDto currentCharguesData = new DetailDto();


                        currentCharguesData.UNIQ_ID = Constants.MRC;
                        currentCharguesData.CHG_CLASS = Constants.LevelOne;
                        currentCharguesData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                        currentCharguesData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                        currentCharguesData.SP_NAME = mrcDataSpName.TrimStart();
                        currentCharguesData.SUBSCRIBER = serviceId;
                        currentCharguesData.CHG_CODE_1 = $"{Constants.TotalCurrentChargesFor} {serviceId}";
                        currentCharguesData.CHG_QTY1_BILLED = "0";

                        currentCharguesData.CHG_AMT = "0";
                        currentCharguesData.CURRENCY = Constants.USD;
                        currentCharguesData.INFO_ONLY_IND = "N";

                        result.Add(currentCharguesData);
                    }
                    else
                    {
                        var pageRegex = new Regex(Constants.pageRegex);
                        var pageValidation = pageRegex.Match(detailArray[1]);
                        var pageNumber = Convert.ToInt32(pageValidation.Value.Split("of")[0]);

                        List<DetailDto> temp = adjAccountResult.FindAll(b => b.SUBSCRIBER.Equals(serviceId.Replace("-", string.Empty)));

                        foreach (DetailDto element in temp)
                        {
                            element.SP_NAME = mrcDataSpName;
                            accountMonthlyChargesSum += System.Convert.ToDecimal(element.CHG_AMT);
                            adjResult.Add(element);
                            adjAccountResult.Remove(element);
                        }

                        /*
                         Through the follow do - while we get the number of pages that a line detail has.
                         */
                        this.accountTotal = 0;
                        var pageContent = detail;
                        do
                        {
                            detailArray = pageContent.ToArray();
                            totalCurrentChargesFor = pageContent.FirstOrDefault(d => d.Contains(Constants.TotalCurrentChargesFor));
                            yourPlanMonthlyFind = pageContent.FirstOrDefault(d => d.Contains(Constants.YourPlanMonthlyCharges));

                            //*************************************************************************************************************************
                            //***************************************** Your Plan|Monthly Charges *****************************************************
                            //*************************************************************************************************************************

                            if (!string.IsNullOrEmpty(yourPlanMonthlyFind))
                            {
                                var firstOcc = pageContent.FirstOrDefault(d => d.Contains(Constants.YourPlanMonthlyCharges));
                                var occPosArray = Array.IndexOf(detailArray, firstOcc);

                                var planName = detailArray[occPosArray + 2].Contains("Plan from") ? detailArray[occPosArray + 3].Split(Constants.Pipe)[0] : detailArray[occPosArray + 2].Split(Constants.Pipe)[0];
                                planName = planName.Replace(Constants.LineSeparator, string.Empty);
                                while (!finalValueRegex.IsMatch(helper.RemoveLeftSide(detailArray[occPosArray + 1]).Split(Constants.Pipe)[helper.RemoveLeftSide(detailArray[occPosArray + 1]).Split(Constants.Pipe).Length - 1]))
                                {//Total Voice
                                    var mrcValues = helper.RemoveLeftSide(detailArray[occPosArray + 1]);
                                    DetailDto usgsumDetail = new DetailDto();


                                    var monthlyChargesRegex = new Regex(Constants.MonthlyChargesRegex);

                                    if (monthlyChargesRegex.IsMatch(mrcValues))
                                    {

                                        var voiceGroup = monthlyChargesRegex.Match(mrcValues).Groups;

                                        var date = mrcValues.Split(Constants.Pipe).Length == 3 ? mrcValues.Split(Constants.Pipe)[1] : mrcValues.Split(Constants.Pipe)[2];

                                        DetailDto mrcData = new DetailDto();

                                        if (planName.Equals(string.Empty))
                                        {
                                            planName = voiceGroup[3].ToString();
                                        }

                                        var begYear = Convert.ToInt32(voiceGroup[4].ToString()) >= 1 && Convert.ToInt32(voiceGroup[4].ToString()) <= this.dateDueMonth ? this.dateDueYear : this.dateDueYear - 1;

                                        var endYear = Utils.GetYear(Convert.ToInt32(voiceGroup[4].ToString()), Convert.ToInt32(voiceGroup[6].ToString()), this.dateDueMonth, this.dateDueYear);

                                        mrcData.UNIQ_ID = Constants.MRC;
                                        mrcData.CHG_CLASS = Constants.LevelOne;
                                        mrcData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                                        mrcData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                        mrcData.SP_NAME = mrcDataSpName.TrimStart();
                                        mrcData.SUBSCRIBER = serviceId;
                                        mrcData.CHG_CODE_1 = voiceGroup[3].ToString();
                                        mrcData.CHG_CODE_2 = planName;
                                        mrcData.BEG_CHG_DATE = new DateTime(begYear, Convert.ToInt32(voiceGroup[4].ToString()), Convert.ToInt32(voiceGroup[5].ToString())).ToString("M/d/yyyy");
                                        mrcData.END_CHG_DATE = new DateTime(endYear, Convert.ToInt32(voiceGroup[6].ToString()), Convert.ToInt32(voiceGroup[7].ToString())).ToString("M/d/yyyy");
                                        mrcData.CHG_AMT = Utils.NumberFormat(voiceGroup[8].ToString());
                                        mrcData.CURRENCY = Constants.USD;
                                        mrcData.INFO_ONLY_IND = "N";
                                        mrcData.SP_INV_RECORD_TYPE = Constants.MonthlyCharges.ToUpper();

                                        this.lineTotal += System.Convert.ToDecimal(Utils.NumberFormat(voiceGroup[8].ToString()).Replace(",", string.Empty));
                                        this.accountTotal += System.Convert.ToDecimal(Utils.NumberFormat(voiceGroup[8].ToString()).Replace(",", string.Empty));

                                        result.Add(mrcData);
                                    }

                                    occPosArray++;
                                }
                            }


                            //***************************************************************************************************************************
                            //************************************************** EquipmentCharges *******************************************************
                            //***************************************************************************************************************************

                            if (!string.IsNullOrEmpty(pageContent.FirstOrDefault(d => d.Contains(Constants.EquipmentCharges))))
                            {
                                var firstOcc = pageContent.FirstOrDefault(d => d.Contains(Constants.EquipmentCharges));
                                var occStartPosArray = Array.IndexOf(detailArray, firstOcc);

                                while (!moneyRegex.IsMatch(helper.RemoveLeftSide(detailArray[occStartPosArray + 1]).Split(Constants.Pipe)[helper.RemoveLeftSide(detailArray[occStartPosArray + 1]).Split(Constants.Pipe).Length - 1]))
                                {
                                    DetailDto equipment = this.GetEquipmentCharges(helper.RemoveLeftSide(detailArray[occStartPosArray + 1]), mrcDataSpName, serviceId);

                                    if (equipment != null)
                                    {
                                        occResult.Add(equipment);
                                        this.accountTotal += System.Convert.ToDecimal(equipment.CHG_AMT);
                                        //this.lineTotal += System.Convert.ToDecimal(equipment.CHG_AMT);
                                    }


                                    occStartPosArray++;
                                }
                            }


                            //***************************************************************************************************************************
                            //********************************************** Usage and Purchase Charges**************************************************
                            //***************************************************************************************************************************
                            if (!string.IsNullOrEmpty(usagePurchesFind))
                            {

                                //***************************************************************************************************************************
                                //********************************************************* Voice ***********************************************************
                                //***************************************************************************************************************************
                                var voiceFind = pageContent.FirstOrDefault(d => d.Contains(Constants.VoiceTitle));
                                if (!string.IsNullOrEmpty(voiceFind))
                                {
                                    var firstdetail = pageContent.FirstOrDefault(d => d.Contains(Constants.VoiceTitle));
                                    var detailPosArray = Array.IndexOf(detailArray, firstdetail);

                                    while (!detailArray[detailPosArray + 1].Contains(Constants.TotalVoice))
                                    {//Total Voice

                                        var removedText = helper.RemoveLeftSide(detailArray[detailPosArray + 1]);
                                        var shared = ((detailPosArray + 1 < detailArray.Length && removedText.Contains("(shared)")) || (detailPosArray + 2 < detailArray.Length && helper.RemoveLeftSide(detailArray[detailPosArray + 2]).Contains("(shared)") && helper.RemoveLeftSide(detailArray[detailPosArray + 2]).Split(Constants.Pipe).Length < 3)) ? true : false;


                                        DetailDto voiceTemp = helper.GetVoice(removedText, mrcDataSpName, serviceId, this.accountNumber, shared);

                                        if (voiceTemp != null)
                                        {
                                            this.lineTotal += System.Convert.ToDecimal(voiceTemp.CHG_AMT);
                                            this.accountTotal += System.Convert.ToDecimal(voiceTemp.CHG_AMT);
                                            usgsumResult.Add(voiceTemp);
                                        }

                                        lock (this)
                                        {
                                            if (detailPosArray + 1 == detailArray.Length - 1)
                                            {

                                                break;
                                            }
                                            detailPosArray++;
                                        }
                                    }
                                }

                                //***************************************************************************************************************************
                                //************************************************** Messaging section ******************************************************
                                //***************************************************************************************************************************


                                var messageFind = pageContent.FirstOrDefault(d => d.Contains(Constants.MessagingTitle));
                                if (!string.IsNullOrEmpty(messageFind))
                                {

                                    var detailPosArray = Array.IndexOf(detailArray, messageFind);

                                    int values = detailPosArray;

                                    while (!detailArray[values + 1].Contains(Constants.TotalMessaging))
                                    {//Total Voice

                                        var removedText = helper.RemoveLeftSide(detailArray[values + 1]);
                                        var messagingValues = helper.GetMessaging(removedText, mrcDataSpName, serviceId, accountNumber, Constants.MESSAGING);

                                        if (messagingValues != null)
                                        {
                                            usgsumResult.Add(messagingValues);
                                            this.lineTotal += System.Convert.ToDecimal(messagingValues.CHG_AMT);
                                            this.accountTotal += System.Convert.ToDecimal(messagingValues.CHG_AMT);
                                        }


                                        if (values + 1 == detailArray.Length - 1)
                                        {

                                            break;
                                        }

                                        values++;
                                    }
                                }

                                //***************************************************************************************************************************************************************************
                                //*************************************************************** Data Usage ************************************************************************************************
                                //***************************************************************************************************************************************************************************

                                var dataFind = pageContent.FirstOrDefault(d => d.Contains(Constants.DataTitle));
                                if (!string.IsNullOrEmpty(dataFind))
                                {
                                    var firstData = pageContent.FirstOrDefault(d => d.Contains(Constants.DataTitle));
                                    var dataStartPosArray = Array.IndexOf(detailArray, firstData);


                                    while (!detailArray[dataStartPosArray + 1].Contains(Constants.TotalData))
                                    {//Total Voice

                                        var removedText = helper.RemoveLeftSide(detailArray[dataStartPosArray + 1]);
                                        var share = ((dataStartPosArray + 1 < detailArray.Length && removedText.Contains("(shared)")) || (dataStartPosArray + 2 < detailArray.Length && helper.RemoveLeftSide(detailArray[dataStartPosArray + 2]).Contains("(shared)")) || (dataStartPosArray + 3 < detailArray.Length && helper.RemoveLeftSide(detailArray[dataStartPosArray + 3]).Contains("(shared)"))) ? true : false;
                                        var usgsumDetail = helper.GetData(removedText, mrcDataSpName, serviceId, share, this.accountNumber, Constants.DATA);


                                        if (usgsumDetail != null)
                                        {
                                            usgsumResult.Add(usgsumDetail);
                                            this.lineTotal += !string.IsNullOrEmpty(usgsumDetail.CHG_AMT) ? System.Convert.ToDecimal(usgsumDetail.CHG_AMT) : 0;
                                            this.accountTotal += !string.IsNullOrEmpty(usgsumDetail.CHG_AMT) ? System.Convert.ToDecimal(usgsumDetail.CHG_AMT) : 0;
                                        }



                                        if (dataStartPosArray + 1 == detailArray.Length - 1)
                                        {

                                            break;
                                        }
                                        dataStartPosArray++;
                                    }
                                }

                                //***************************************************************************************************************************************************************************
                                //************************************************************************Purchases******************************************************************************************
                                //***************************************************************************************************************************************************************************
                                var purchasesTitle = pageContent.FirstOrDefault(d => d.Contains(Constants.PurchasesTitle));
                                if (!string.IsNullOrEmpty(purchasesTitle) || !string.IsNullOrEmpty(pageContent.FirstOrDefault(d => d.Contains(Constants.PurchasesTitle2))))
                                {
                                    var firstdetail = !string.IsNullOrEmpty(purchasesTitle) ? purchasesTitle : pageContent.FirstOrDefault(d => d.Contains(Constants.PurchasesTitle2));

                                    var detailPosArray = Array.IndexOf(detailArray, firstdetail);

                                    //while (!detailArray[detailPosArray + 1].Contains(Constants.TotalPurchases))
                                    while (!finalValueRegex.IsMatch(helper.RemoveLeftSide(detailArray[detailPosArray + 1]).Split(Constants.Pipe)[helper.RemoveLeftSide(detailArray[detailPosArray + 1]).Split(Constants.Pipe).Length - 1]))
                                    {


                                        var detailValues = helper.GetPurchases(helper.RemoveLeftSide(detailArray[detailPosArray + 1]), this.accountNumber, mrcDataSpName, serviceId, dateDueMonth, dateDueYear);

                                        if (detailValues != null)
                                        {
                                            occResult.Add(detailValues);
                                            this.lineTotal += System.Convert.ToDecimal(detailValues.CHG_AMT);
                                            this.accountTotal += System.Convert.ToDecimal(detailValues.CHG_AMT);
                                        }

                                        if (detailPosArray + 1 == detailArray.Length - 1)
                                        {

                                            break;
                                        }

                                        detailPosArray++;
                                    }
                                }



                                //***************************************************************************************************************************
                                //************************************************** Roaming ****************************************************************
                                //***************************************************************************************************************************


                                var roamingFind = pageContent.FirstOrDefault(d => d.Contains(Constants.RoamingTitle));
                                if (!string.IsNullOrEmpty(roamingFind))
                                {

                                    var detailPosArray = Array.IndexOf(detailArray, roamingFind);


                                    while (!detailArray[detailPosArray + 1].Contains(Constants.TotalRoaming))
                                    {//Total Voice

                                        //var share = ((dataStartPosArray + 1 < detailArray.Length && detailArray[dataStartPosArray + 1].Contains("(shared)")) || (dataStartPosArray + 2 < detailArray.Length && detailArray[dataStartPosArray + 2].Contains("(shared)")) || (dataStartPosArray + 3 < detailArray.Length && detailArray[dataStartPosArray + 3].Contains("(shared)"))) ? true : false;
                                        var share = ((detailPosArray + 1 < detailArray.Length && helper.RemoveLeftSide(detailArray[detailPosArray + 1]).Contains("(shared)")) || (detailPosArray + 2 < detailArray.Length && helper.RemoveLeftSide(detailArray[detailPosArray + 2]).Contains("(shared)") && helper.RemoveLeftSide(detailArray[detailPosArray + 2]).Split(Constants.Pipe).Length < 3)) ? true : false;

                                        DetailDto usgsumDetail = helper.GetRoaming(detailArray[detailPosArray + 1], mrcDataSpName, serviceId, share, this.accountNumber);

                                        if (usgsumDetail != null)
                                        {
                                            usgsumResult.Add(usgsumDetail);
                                            this.lineTotal += System.Convert.ToDecimal(usgsumDetail.CHG_AMT);
                                            this.accountTotal += System.Convert.ToDecimal(usgsumDetail.CHG_AMT);
                                        }


                                        if (detailPosArray + 1 == detailArray.Length - 1)
                                        {
                                            break;
                                        }

                                        detailPosArray++;
                                    }
                                }



                                //***************************************************************************************************************************************************************************
                                //****************************************************************International Section**************************************************************************************
                                //***************************************************************************************************************************************************************************

                                if (!string.IsNullOrEmpty(pageContent.FirstOrDefault(d => d.Contains(Constants.InternationTitle))))
                                {
                                    var firstdetail = pageContent.FirstOrDefault(d => d.Contains(Constants.InternationTitle));
                                    var detailPosArray = Array.IndexOf(detailArray, firstdetail);

                                    var planName = string.Empty;
                                    while (!helper.RemoveLeftSide(detailArray[detailPosArray + 1]).Contains(Constants.TotaInternational))
                                    {//Total Voice


                                        var detailValues = helper.RemoveLeftSide(detailArray[detailPosArray + 1]);
                                        DetailDto usgsumData = new DetailDto();



                                        if (new Regex(Constants.InternationalMessageRegex).IsMatch(detailValues))
                                        {

                                            var messagingValuesInter = helper.GetMessaging(detailValues, mrcDataSpName, serviceId, accountNumber, Constants.INTERNATIONAL);

                                            if (messagingValuesInter != null)
                                            {
                                                messagingValuesInter.CHG_CODE_1 = this.CheckInternationEspecialCase(planName, messagingValuesInter.CHG_CODE_1);
                                                planName = messagingValuesInter.CHG_CODE_1;

                                                usgsumResult.Add(messagingValuesInter);
                                                this.lineTotal += System.Convert.ToDecimal(messagingValuesInter.CHG_AMT);
                                                this.accountTotal += System.Convert.ToDecimal(messagingValuesInter.CHG_AMT);
                                            }
                                        }
                                        else
                                        {
                                            if (new Regex(Constants.InternationDataRegex).IsMatch(detailValues))
                                            {

                                                var usgsumDetail = helper.GetData(detailValues, mrcDataSpName, serviceId, false, this.accountNumber, Constants.INTERNATIONAL);


                                                if (usgsumDetail != null)
                                                {
                                                    usgsumDetail.CHG_CODE_1 = this.CheckInternationEspecialCase(planName, usgsumDetail.CHG_CODE_1);
                                                    planName = usgsumDetail.CHG_CODE_1;

                                                    usgsumResult.Add(usgsumDetail);
                                                    this.lineTotal += !string.IsNullOrEmpty(usgsumDetail.CHG_AMT) ? System.Convert.ToDecimal(usgsumDetail.CHG_AMT) : 0;
                                                    this.accountTotal += !string.IsNullOrEmpty(usgsumDetail.CHG_AMT) ? System.Convert.ToDecimal(usgsumDetail.CHG_AMT) : 0;
                                                }
                                            }
                                            else
                                            {

                                                var internationMinutesRegex = new Regex(Constants.InternationalMinutesRegex).Match(detailValues);

                                                if (internationMinutesRegex.Success)
                                                {

                                                    var internationMinutesRegexGroup = internationMinutesRegex.Groups;

                                                    usgsumData.UNIQ_ID = Constants.USGSUM;
                                                    usgsumData.CHG_CLASS = Constants.LevelOne;
                                                    usgsumData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                                                    usgsumData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                                    usgsumData.SP_NAME = mrcDataSpName.TrimStart();
                                                    usgsumData.SUBSCRIBER = serviceId;

                                                    usgsumData.CHG_CODE_1 = internationMinutesRegexGroup[1].ToString().Replace(Constants.Pipe, ' ');
                                                    usgsumData.CHG_QTY1_TYPE = Utils.GetChargesType(internationMinutesRegexGroup[2].ToString());
                                                    usgsumData.CHG_QTY1_ALLOWED = internationMinutesRegexGroup[3].ToString().Contains("unlimited") ? "" : internationMinutesRegexGroup[5].ToString();
                                                    usgsumData.CHG_QTY1_USED = internationMinutesRegexGroup[7].ToString().Contains("--") || string.IsNullOrEmpty(internationMinutesRegexGroup[7].ToString()) ? "" : Utils.NumberFormat(internationMinutesRegexGroup[7].ToString());
                                                    usgsumData.CHG_QTY1_BILLED = internationMinutesRegexGroup[9].ToString().Contains("--") || string.IsNullOrEmpty(internationMinutesRegexGroup[9].ToString()) ? "" : Utils.NumberFormat(internationMinutesRegexGroup[9].ToString());
                                                    usgsumData.CHG_AMT = internationMinutesRegexGroup[11].ToString().Contains("--") ? "" : Utils.NumberFormat(internationMinutesRegexGroup[11].ToString().Replace(Constants.MoneySign, string.Empty));

                                                    usgsumData.CURRENCY = Constants.USD;
                                                    usgsumData.SP_INV_RECORD_TYPE = Constants.INTERNATIONAL;

                                                    usgsumData.CHG_CODE_1 = this.CheckInternationEspecialCase(planName, usgsumData.CHG_CODE_1);
                                                    planName = usgsumData.CHG_CODE_1;

                                                    usgsumResult.Add(usgsumData);

                                                    this.lineTotal += !string.IsNullOrEmpty(usgsumData.CHG_AMT) ? System.Convert.ToDecimal(usgsumData.CHG_AMT) : 0;
                                                    this.accountTotal += !string.IsNullOrEmpty(usgsumData.CHG_AMT) ? System.Convert.ToDecimal(usgsumData.CHG_AMT) : 0;
                                                }
                                                else
                                                {


                                                    if (new Regex(Constants.InternationDataSpecialCaseRegex).IsMatch(detailValues))
                                                    {
                                                        var usgsumDetail = helper.GetData(detailValues, mrcDataSpName, serviceId, false, this.accountNumber, Constants.INTERNATIONAL);

                                                        if (usgsumDetail != null)
                                                        {
                                                            usgsumResult.Add(usgsumDetail);
                                                            this.lineTotal += !string.IsNullOrEmpty(usgsumDetail.CHG_AMT) ? System.Convert.ToDecimal(usgsumDetail.CHG_AMT) : 0;
                                                            this.accountTotal += !string.IsNullOrEmpty(usgsumDetail.CHG_AMT) ? System.Convert.ToDecimal(usgsumDetail.CHG_AMT) : 0;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var internationalVoiceRegex = new Regex(Constants.InternationVoiceRegex).Match(detailValues);

                                                        if (internationalVoiceRegex.Success)
                                                        {

                                                            var internationalVoiceRegexGroup = internationalVoiceRegex.Groups;

                                                            usgsumData.UNIQ_ID = Constants.USGSUM;
                                                            usgsumData.CHG_CLASS = Constants.LevelOne;
                                                            usgsumData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                                                            usgsumData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                                            usgsumData.SP_NAME = mrcDataSpName.TrimStart();
                                                            usgsumData.SUBSCRIBER = serviceId;
                                                            usgsumData.CHG_CODE_1 = internationalVoiceRegexGroup[2].ToString().Replace(Constants.Pipe, ' ');

                                                            usgsumData.CHG_AMT = Utils.NumberFormat(internationalVoiceRegexGroup[3].ToString().Replace(Constants.MoneySign, string.Empty));
                                                            usgsumData.CURRENCY = Constants.USD;
                                                            usgsumData.SP_INV_RECORD_TYPE = Constants.INTERNATIONAL;

                                                            usgsumResult.Add(usgsumData);

                                                            this.lineTotal += System.Convert.ToDecimal(usgsumData.CHG_AMT);
                                                            this.accountTotal += System.Convert.ToDecimal(usgsumData.CHG_AMT);
                                                        }
                                                        else
                                                        {

                                                            if (!detailValues.Contains("Usage While"))
                                                            {
                                                                planName = $"{planName} {detailValues.Replace(Constants.Pipe, ' ')}";
                                                            }

                                                        }
                                                    }

                                                }
                                            }
                                        }



                                        if (detailPosArray + 1 == detailArray.Length - 1)
                                        {
                                            break;
                                        }

                                        detailPosArray++;
                                    }
                                }

                            }


                            //***************************************************************************************************************************************************************************
                            //*************************************************************** OtherChargesCredits ************************************************************************************************
                            //***************************************************************************************************************************************************************************

                            var OtherChargesFind = pageContent.FirstOrDefault(d => d.Contains(Constants.OtherChargesCredits));
                            if (!string.IsNullOrEmpty(OtherChargesFind))
                            {
                                var firstOtherCharges = pageContent.FirstOrDefault(d => d.Contains(Constants.OtherChargesCredits));
                                var firstOtherChargesStartPosArray = Array.IndexOf(detailArray, firstOtherCharges);

                                while (!moneyRegex.IsMatch(helper.RemoveLeftSide(detailArray[firstOtherChargesStartPosArray + 1]).Split(Constants.Pipe)[helper.RemoveLeftSide(detailArray[firstOtherChargesStartPosArray + 1]).Split(Constants.Pipe).Length - 1]))
                                {

                                    var usgsumDetail = helper.GetOtherChargesCredits(helper.RemoveLeftSide(detailArray[firstOtherChargesStartPosArray + 1]), mrcDataSpName, serviceId, this.accountNumber);


                                    if (usgsumDetail != null)
                                    {
                                        occResult.Add(usgsumDetail);
                                        this.lineTotal += !string.IsNullOrEmpty(usgsumDetail.CHG_AMT) ? System.Convert.ToDecimal(usgsumDetail.CHG_AMT) : 0;
                                        this.accountTotal += !string.IsNullOrEmpty(usgsumDetail.CHG_AMT) ? System.Convert.ToDecimal(usgsumDetail.CHG_AMT) : 0;
                                    }

                                    if (firstOtherChargesStartPosArray + 1 == detailArray.Length - 1)
                                    {
                                        break;
                                    }
                                    firstOtherChargesStartPosArray++;
                                }
                            }



                            //***************************************************************************************************************************************************************************
                            //**************************************************************** Surcharges section ***************************************************************************************
                            //***************************************************************************************************************************************************************************
                            var firstSurcharges = "";
                            var surPosArray = 0;
                            foreach (var item in pageContent.FindAll(d => d.Contains(Constants.Surcharges)))
                            {
                                if (new Regex(Constants.SurchargesTitleRegex).Match(item).Success)
                                {
                                    firstSurcharges = item;
                                    surPosArray = Array.IndexOf(detailArray, firstSurcharges);
                                    break;
                                }
                            }


                            if (!string.IsNullOrEmpty(firstSurcharges))
                            {

                                while (!finalValueRegex.IsMatch(helper.RemoveLeftSide(detailArray[surPosArray + 1]).Split(Constants.Pipe)[helper.RemoveLeftSide(detailArray[surPosArray + 1]).Split(Constants.Pipe).Length - 1]) && !helper.RemoveLeftSide(detailArray[surPosArray + 1]).Contains(Constants.OtherChargesCredits))
                                {//Total Voice


                                    var usgsumDetail = helper.GetSurcharges(helper.RemoveLeftSide(detailArray[surPosArray + 1]), mrcDataSpName, serviceId, this.accountNumber);

                                    if (usgsumDetail != null)
                                    {
                                        surTaxesResult.Add(usgsumDetail);

                                        this.lineTotal += System.Convert.ToDecimal(usgsumDetail.CHG_AMT);
                                        this.accountTotal += System.Convert.ToDecimal(usgsumDetail.CHG_AMT);
                                    }


                                    if (surPosArray + 1 == detailArray.Length - 1)
                                    {
                                        break;
                                    }
                                    surPosArray++;
                                }
                            }

                            //***************************************************************************************************************************************************************************
                            //******************************************************** Taxes, Governmental Surcharges ***********************************************************************************
                            //***************************************************************************************************************************************************************************
                            var lastTaxesGovernmentalSurcharges = pageContent.LastOrDefault(d => d.Contains(Constants.TaxesGovernmentalSurcharges));
                            if (!string.IsNullOrEmpty(lastTaxesGovernmentalSurcharges) && new Regex(Constants.TaxesGovernmentalSurchargesFeesTitleRegex).Match(lastTaxesGovernmentalSurcharges).Success)
                            {

                                var taxesPosArray = Array.IndexOf(detailArray, lastTaxesGovernmentalSurcharges);

                                int values = taxesPosArray;

                                while (!finalValueRegex.IsMatch(helper.RemoveLeftSide(detailArray[values + 1]).Split(Constants.Pipe)[helper.RemoveLeftSide(detailArray[values + 1]).Split(Constants.Pipe).Length - 1]))
                                {//Total Voice

                                    var surValues = helper.GetTaxesGovermentalSurcharges(helper.RemoveLeftSide(detailArray[values + 1]), accountNumber, mrcDataSpName, serviceId);

                                    if (surValues != null)
                                    {
                                        surTaxesResult.Add(surValues);
                                        this.accountTotal += System.Convert.ToDecimal(surValues.CHG_AMT);
                                        this.lineTotal += System.Convert.ToDecimal(surValues.CHG_AMT);
                                    }

                                    if (values + 1 == detailArray.Length - 1)
                                    {
                                        break;
                                    }
                                    values++;
                                }
                            }



                            //***************************************************************************************************************************************************
                            //******************************************************** Total Current Charges For ****************************************************************
                            //***************************************************************************************************************************************************

                            if (!string.IsNullOrEmpty(totalCurrentChargesFor))
                            {

                                var ftotalCurrentChargesRegex = new Regex(Constants.TotalCurrentChargesRegex).Match(totalCurrentChargesFor).Groups;


                                DetailDto mrcTotal = new DetailDto();

                                var totalValue = System.Convert.ToDecimal(ftotalCurrentChargesRegex[2].ToString().Replace(Constants.MoneySign, string.Empty).Replace(",", string.Empty)) + System.Convert.ToDecimal(accountMonthlyChargesSum);

                                mrcTotal.UNIQ_ID = Constants.MRC;
                                mrcTotal.CHG_CLASS = Constants.LevelOne;
                                mrcTotal.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                                mrcTotal.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                mrcTotal.SP_NAME = mrcDataSpName.TrimStart();
                                mrcTotal.SUBSCRIBER = serviceId;
                                mrcTotal.CHG_CODE_1 = ftotalCurrentChargesRegex[1].ToString();
                                mrcTotal.CHG_AMT = totalValue.ToString();
                                mrcTotal.CURRENCY = Constants.USD;
                                mrcTotal.INFO_ONLY_IND = "Y";
                                //mrcTotal.SP_INV_RECORD_TYPE = Constants.MonthlyCharges.ToUpper();
                                mrcTotal.UDF = "";

                                result.Add(mrcTotal);

                                if(System.Convert.ToDecimal(ftotalCurrentChargesRegex[2].ToString().Replace(Constants.MoneySign, string.Empty).Replace(",", string.Empty)) != this.accountTotal)
                                {
                                    noReconciledLine.Add(serviceId);
                                }

                            }
                            pageNumber++;
                            pageContent = helper.getPageContent(pageNumber);
                        }
                        while (totalCurrentChargesFor == null);

                    }
                }
                else
                {
                    // ***************************************************************************************************************************************
                    // **********************************************Account Level M2M Section ***************************************************************
                    // ***************************************************************************************************************************************

                    // Here you could find all code that handle that M2M records
                    if (!string.IsNullOrEmpty(detail.FirstOrDefault(d => d.Contains(Constants.OverviewM2M))) ||
                        !string.IsNullOrEmpty(detail.FirstOrDefault(d => d.Contains(Constants.OverviewM2MContinue))))//Overview of Machine to Machine Activity
                    {
                        var firstM2m = detail.FirstOrDefault(d => d.Contains(Constants.OverviewM2M));
                        var firstM2mIndex = Array.IndexOf(detailArray, firstM2m);


                        this.acctType = "M2M";

                        while (!detailArray[firstM2mIndex + 1].Contains(Constants.TotalCurrentChargesM2M))
                        {
                            var dataValues = helper.RemoveLeftSide(detailArray[firstM2mIndex + 1]);
                            var dataValueArray = dataValues.Split(Constants.Pipe);
                            if (dataValueArray.Length >= 4)
                            {

                                var matchName = new Regex(Constants.M2MGetPlanName).Match(dataValues);

                                if (matchName.Success && !dataValues.Contains(Constants.Subtotal))
                                {
                                    M2MPlanName = matchName.Groups[1].ToString().Replace(Constants.LineSeparator, string.Empty);
                                    //(string type, string planName, string chgQty1Type, string chgQty1Used, string chgQty1Allowed, string chgQty1Billed, string chgAmt, string spInvRecordType)


                                    result.Add(this.SetValue(Constants.MRC, M2MPlanName, string.Empty, string.Empty, string.Empty, string.Empty, Utils.NumberFormat(matchName.Groups[2].ToString().Replace(Constants.MoneySign, string.Empty)), Constants.MonthlyCharges.ToUpper()));


                                    if (!matchName.Groups[6].ToString().Equals("--"))
                                        surTaxesResult.Add(this.SetValue(Constants.OCC, $"Equipment on {M2MPlanName}", string.Empty, string.Empty, string.Empty, string.Empty, Utils.NumberFormat(matchName.Groups[6].ToString().Replace(Constants.MoneySign, string.Empty)), Constants.Equipment));

                                    if (!matchName.Groups[8].ToString().Equals("--"))
                                        surTaxesResult.Add(this.SetValue(Constants.SUR, $"Surcharges on {M2MPlanName}", string.Empty, string.Empty, string.Empty, string.Empty, Utils.NumberFormat(matchName.Groups[8].ToString().Replace(Constants.MoneySign, string.Empty)), Constants.SurchargesType));

                                    if (!matchName.Groups[10].ToString().Equals("--"))
                                        surTaxesResult.Add(this.SetValue(Constants.TAX, $"Tax on {M2MPlanName}", string.Empty, string.Empty, string.Empty, string.Empty, Utils.NumberFormat(matchName.Groups[10].ToString().Replace(Constants.MoneySign, string.Empty)), Constants.TaxesType));

                                }
                                else
                                {

                                    var matchDiscount = new Regex(Constants.M2MDiscount).Match(dataValues);
                                    if (matchDiscount.Success && !dataValues.Contains(Constants.Subtotal))
                                    {
                                        result.Add(this.SetValue(Constants.MRC, matchDiscount.Groups[1].ToString(), string.Empty, string.Empty, string.Empty, string.Empty, matchDiscount.Groups[2].ToString(), Constants.MonthlyCharges.ToUpper()));

                                    }
                                    else
                                    {
                                        var m2mUsgMatch = new Regex(Constants.M2MUsgsum).Match(dataValues);
                                        if (m2mUsgMatch.Success)
                                        {
                                            var m2mUsgMatchGroup = m2mUsgMatch.Groups;
                                            var chgQty1Used = m2mUsgMatchGroup[12].ToString().Contains("--") || string.IsNullOrEmpty(m2mUsgMatchGroup[12].ToString()) ? "0" : Utils.RemoveTextFromNumber(m2mUsgMatchGroup[12].ToString());
                                            var chgQty1Allowed = m2mUsgMatchGroup[10].ToString().Contains("--") || string.IsNullOrEmpty(m2mUsgMatchGroup[10].ToString()) ? "0" : Utils.RemoveTextFromNumber(m2mUsgMatchGroup[10].ToString());
                                            var chgQty1Billed = m2mUsgMatchGroup[14].ToString().Contains("--") || string.IsNullOrEmpty(m2mUsgMatchGroup[14].ToString()) ? "0" : Utils.RemoveTextFromNumber(m2mUsgMatchGroup[14].ToString());
                                            var chgAmt = m2mUsgMatchGroup[8].ToString().Contains("--") || string.IsNullOrEmpty(m2mUsgMatchGroup[8].ToString()) ? "0" : Utils.NumberFormat(m2mUsgMatchGroup[8].ToString());
                                            //(string type, string planName, string chgQty1Type, string chgQty1Used, string chgQty1Allowed, string chgQty1Billed, string chgAmt, string spInvRecordType)
                                            usgsumResult.Add(this.SetValue(Constants.USGSUM, m2mUsgMatchGroup[1].ToString(), Constants.ChargesType_GB, chgQty1Used, chgQty1Allowed, chgQty1Billed, chgAmt, Constants.DATA));
                                        }
                                        else
                                        {
                                            var m2mUsgsumMsgMatch = new Regex(Constants.M2MUsgsumMsg).Match(dataValues);
                                            if (m2mUsgsumMsgMatch.Success)
                                            {
                                                var m2mUsgsumMsgMatchGroup = m2mUsgsumMsgMatch.Groups;
                                                //(string type, string planName, string chgQty1Type, string chgQty1Used, string chgQty1Allowed, string chgQty1Billed, string chgAmt, string spInvRecordType)
                                                usgsumResult.Add(this.SetValue(Constants.USGSUM, m2mUsgsumMsgMatchGroup[1].ToString(), Constants.ChargesType_MSG, m2mUsgsumMsgMatchGroup[7].ToString().Replace(Constants.Hyphen, string.Empty), m2mUsgsumMsgMatchGroup[5].ToString().Replace(Constants.Hyphen, string.Empty), m2mUsgsumMsgMatchGroup[9].ToString().Replace(Constants.Hyphen, string.Empty), m2mUsgsumMsgMatchGroup[2].ToString().Replace(Constants.Hyphen, string.Empty), Constants.MESSAGING));
                                            }
                                            else if (dataValues.Contains(Constants.Subtotal))
                                            {

                                                M2MPlanName = "";
                                            }
                                        }
                                    }
                                }

                            }

                            if (firstM2mIndex + 1 == detailArray.Length - 1)
                            {
                                break;
                            }
                            firstM2mIndex++;
                        }
                        continue;
                    }



                    // ***************************************************************************************************************************************
                    // ************************************* Account Level Account Charges and Credits Section ***********************************************
                    // ***************************************************************************************************************************************
                    var firstAccountMonthly = "";
                    var firstAccountMonthlyIndex = 0;
                    var accountChargesAndCreditsContinueSearch = detail.FirstOrDefault(d => d.Contains(Constants.AccountChargesAndCreditsContinue));
                    if (!string.IsNullOrEmpty(accountChargesAndCreditsContinueSearch))
                    {
                        firstAccountMonthly = detail.FirstOrDefault(d => d.Contains(accountChargesAndCreditsContinueSearch));
                        firstAccountMonthlyIndex = firstAccountMonthly != null ? Array.IndexOf(detailArray, firstAccountMonthly) : 0;
                    }
                    else
                    {
                        var accountChargesAndCreditsSearch = detail.FirstOrDefault(d => d.Contains(Constants.AccountChargesAndCredits));
                        if (!string.IsNullOrEmpty(accountChargesAndCreditsSearch))
                        {
                            firstAccountMonthly = detail.FirstOrDefault(d => d.Contains(accountChargesAndCreditsSearch));
                            firstAccountMonthlyIndex = firstAccountMonthly != null ? Array.IndexOf(detailArray, firstAccountMonthly) : 0;
                        }
                    }


                    if (!string.IsNullOrEmpty(firstAccountMonthly))
                    {

                        while (!detailArray[firstAccountMonthlyIndex + 1].Contains("Total Account Charges and Credits"))
                        {

                            if (detailArray[firstAccountMonthlyIndex + 1].Contains(Constants.AccountMonthlyCharges) || currentSection.Equals(Constants.AccountMonthlyCharges))
                            {
                                currentSection = string.Empty;

                                while (!detailArray[firstAccountMonthlyIndex + 1].Contains(Constants.Subtotal))
                                {

                                    DetailDto taxData = helper.GetAccountMonthlyCharges(detailArray[firstAccountMonthlyIndex + 1], this.accountNumber, dateDueMonth, dateDueYear);

                                    if (taxData != null)
                                    {
                                        this.lineTotal += System.Convert.ToDecimal(taxData.CHG_AMT);
                                        surTaxesResult.Add(taxData);
                                    }


                                    if (firstAccountMonthlyIndex + 1 == detailArray.Length - 1)
                                    {
                                        currentSection = Constants.AccountMonthlyCharges;
                                        break;
                                    }

                                    firstAccountMonthlyIndex++;
                                }
                            }
                            else if (detailArray[firstAccountMonthlyIndex + 1].Contains(Constants.DevicePaymentCharge) || currentSection.Equals(Constants.DevicePaymentCharge))
                            {
                                currentSection = string.Empty;

                                while (!detailArray[firstAccountMonthlyIndex + 1].Contains(Constants.TotalDevicePaymentCharge))
                                {

                                    DetailDto occData = this.GetEquipmentCharges(detailArray[firstAccountMonthlyIndex + 1], string.Empty, serviceId);

                                    if (occData != null)
                                        occResult.Add(occData);

                                    if (firstAccountMonthlyIndex + 1 == detailArray.Length - 1)
                                    {
                                        currentSection = Constants.DevicePaymentCharge;
                                        break;
                                    }

                                    firstAccountMonthlyIndex++;
                                }
                            }
                            else if (detailArray[firstAccountMonthlyIndex + 1].Contains(Constants.AccountUsageCharges) || currentSection.Equals(Constants.AccountUsageCharges))
                            {
                                currentSection = string.Empty;

                                while (!detailArray[firstAccountMonthlyIndex + 1].Contains(Constants.Subtotal))
                                {

                                    DetailDto occData = this.GetAccountUsage(detailArray[firstAccountMonthlyIndex + 1], string.Empty, serviceId, Constants.USGSUM);

                                    if (occData != null)
                                        occResult.Add(occData);

                                    if (firstAccountMonthlyIndex + 1 == detailArray.Length - 1)
                                    {
                                        currentSection = Constants.AccountUsageCharges;
                                        break;
                                    }

                                    firstAccountMonthlyIndex++;
                                }
                            }
                            else
                            {
                                var licenseRegex = new Regex(Constants.LicenseRegex).Match(detailArray[firstAccountMonthlyIndex + 1]);
                                if (licenseRegex.Success)
                                {
                                    var licenseGroups = licenseRegex.Groups;

                                    string planName = detailArray[firstAccountMonthlyIndex];//+ " " + 
                                    DetailDto taxData = new DetailDto();

                                    taxData.UNIQ_ID = Constants.OCC;
                                    taxData.CHG_CLASS = Constants.LevelOne;
                                    taxData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                                    taxData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                    taxData.CHG_CODE_1 = licenseGroups[1].ToString().Replace('|', ' ').Trim();
                                    taxData.CHG_CODE_2 = planName.Replace('|', ' ').Trim();
                                    taxData.CHG_AMT = Utils.NumberFormat(licenseGroups[5].ToString().Replace(Constants.MoneySign, string.Empty));

                                    taxData.CURRENCY = Constants.USD;
                                    taxData.SP_INV_RECORD_TYPE = Constants.ACCOUNT_CHARGES;

                                    occResult.Add(taxData);
                                    this.lineTotal += System.Convert.ToDecimal(taxData.CHG_AMT);
                                }
                                else
                                {
                                    if (!detailArray[firstAccountMonthlyIndex + 1].Contains(Constants.Subtotal))
                                    {
                                        var accountLevenTaxRegex = new Regex(Constants.InternationVoiceRegex).Match(detailArray[firstAccountMonthlyIndex + 1]);
                                        if (accountLevenTaxRegex.Success)
                                        {
                                            var accountLevenTaxGroups = accountLevenTaxRegex.Groups;

                                            DetailDto taxData = new DetailDto();

                                            taxData.UNIQ_ID = Constants.TAX;
                                            taxData.CHG_CLASS = Constants.LevelOne;
                                            taxData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                                            taxData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                            taxData.CHG_CODE_1 = accountLevenTaxGroups[2].ToString().Replace('|', ' ');
                                            taxData.CHG_AMT = Utils.NumberFormat(accountLevenTaxGroups[3].ToString().Replace(Constants.MoneySign, string.Empty));

                                            taxData.CURRENCY = Constants.USD;
                                            taxData.INFO_ONLY_IND = "N";
                                            taxData.SP_INV_RECORD_TYPE = Constants.ACCOUNT_CHARGES;

                                            surTaxesResult.Add(taxData);
                                        }
                                        else
                                        {
                                            var accountMonthlyRegex = new Regex(Constants.BroadbandRegex);
                                            var dataValues = detailArray[firstAccountMonthlyIndex + 1];

                                            if (accountMonthlyRegex.IsMatch(dataValues))
                                            {
                                                var accountMonthly = accountMonthlyRegex.Match(dataValues).Groups;
                                                var dataValueArray = detailArray[firstAccountMonthlyIndex + 1].Split(Constants.Pipe);


                                                var begMonth = Convert.ToInt32(accountMonthly[3].ToString().Split('-')[0].Split('/')[0]);
                                                var endMonth = Convert.ToInt32(accountMonthly[3].ToString().Split('-')[1].Split('/')[0]);

                                                var begYear = begMonth >= 1 && begMonth <= this.dateDueMonth ? this.dateDueYear : this.dateDueYear - 1;
                                                var endYear = endMonth >= 1 && endMonth <= this.dateDueMonth ? this.dateDueYear : this.dateDueYear - 1;


                                                DetailDto taxData = new DetailDto();

                                                taxData.UNIQ_ID = Constants.TAX;
                                                taxData.CHG_CLASS = Constants.LevelOne;
                                                taxData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                                                taxData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                                taxData.CHG_CODE_1 = accountMonthly[1].ToString();
                                                taxData.BEG_CHG_DATE = accountMonthly[3].ToString().Split('-').Length > 1 ? new DateTime(begYear, begMonth, Convert.ToInt32(accountMonthly[3].ToString().Split('-')[0].Split('/')[1])).ToString("M/d/yyyy") : string.Empty;
                                                taxData.END_CHG_DATE = accountMonthly[3].ToString().Split('-').Length > 1 ? new DateTime(endYear, endMonth, Convert.ToInt32(accountMonthly[3].ToString().Split('-')[1].Split('/')[1])).ToString("M/d/yyyy") : string.Empty;
                                                taxData.CHG_AMT = Utils.NumberFormat(accountMonthly[4].ToString().Replace(Constants.MoneySign, string.Empty));

                                                taxData.CURRENCY = Constants.USD;
                                                taxData.INFO_ONLY_IND = "N";
                                                taxData.SP_INV_RECORD_TYPE = Constants.TaxesType;

                                                surTaxesResult.Add(taxData);

                                            }
                                        }
                                    }
                                }
                                
                            }

                            if (firstAccountMonthlyIndex + 1 == detailArray.Length - 1)
                            {
                                break;
                            }

                            firstAccountMonthlyIndex++;
                        }
                    }



                    // ***************************************************************************************************************************************
                    // *************************************Account Level Adjustments Section ****************************************************************
                    // ***************************************************************************************************************************************
                    if (!string.IsNullOrEmpty(detail.FirstOrDefault(d => d.Contains(Constants.Adjustments))) ||
                        !string.IsNullOrEmpty(detail.FirstOrDefault(d => d.Contains(Constants.PaymentsAdjustments))) ||
                        !string.IsNullOrEmpty(detail.FirstOrDefault(d => d.Contains(Constants.PaymentsAdjustmentsContinue))) ||
                        !string.IsNullOrEmpty(detail.FirstOrDefault(d => d.Contains(Constants.AdjustmentsContinued))))
                    {

                        var firstPayment = "";
                        var firstPaymentIndex = 0;
                        foreach (var item in detail.FindAll(d => d.Contains(Constants.Adjustments)))
                        {
                            if (new Regex(Constants.AdjustmentsTitleRegex).Match(item).Success)
                            {
                                firstPayment = item;
                                firstPaymentIndex = Array.IndexOf(detailArray, firstPayment);
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(firstPayment))
                        {
                            while (!detailArray[firstPaymentIndex + 1].Contains(Constants.TotalAdjustments))
                            {

                                var dataValues = detailArray[firstPaymentIndex + 1];

                                DetailDto occData = GetAdjustmentDevices(detailArray[firstPaymentIndex + 1]);

                                if (occData != null)
                                    adjAccountResult.Add(occData);


                                if (firstPaymentIndex + 1 == detailArray.Length - 1)
                                {
                                    break;
                                }

                                firstPaymentIndex++;
                            }
                        }

                    }
                }
                GC.Collect();
            }

            // Add the Total Current Charges for Account level lines that doesn't have monthly Charges.
            result.AddRange(GenerateTotalCurrentCharges(adjAccountResult));

            result.AddRange(occResult);
            result.AddRange(usgsumResult);
            result.AddRange(surTaxesResult);
            result.AddRange(adjResult);
            result.AddRange(adjAccountResult);

            return result;
        }

        private DetailDto GetAccountUsage(string dataValues, string mrcDataSpName, string serviceId, string type)
        {

            DetailDto accountData = null;
            var accountLevenTaxRegex = new Regex(Constants.InternationVoiceRegex).Match(dataValues);
            if (accountLevenTaxRegex.Success)
            {
                var accountLevenTaxGroups = accountLevenTaxRegex.Groups;

                accountData = new DetailDto();

                accountData.UNIQ_ID = type;
                accountData.CHG_CLASS = Constants.LevelOne;
                accountData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                accountData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                accountData.CHG_CODE_1 = accountLevenTaxGroups[2].ToString().Replace('|', ' ');
                accountData.CHG_AMT = Utils.NumberFormat(accountLevenTaxGroups[3].ToString().Replace(Constants.MoneySign, string.Empty));

                accountData.CURRENCY = Constants.USD;
                accountData.INFO_ONLY_IND = "N";
                accountData.SP_INV_RECORD_TYPE = Constants.ACCOUNT_CHARGES;

                this.lineTotal += System.Convert.ToDecimal(accountData.CHG_AMT);
            }

            return accountData;
        }

        private string CheckInternationEspecialCase(string currentPlanName, string structurePlanName)
        {
            var planName = structurePlanName;
            if (string.IsNullOrEmpty(structurePlanName.Trim()))
            {
                planName = currentPlanName;
            }

            var dateRegex = new Regex(Constants.DateRegex).Match(structurePlanName);
            if (dateRegex.Success)
            {
                planName = $"{currentPlanName} {structurePlanName}";
            }

            return planName;
        }

        private DetailDto GetEquipmentCharges(string dataValues, string mrcDataSpName, string serviceId)
        {
            DetailDto occData = null;

            var occRegex = new Regex(Constants.EquipmentChargesRegex);
            var occRegex2 = new Regex(Constants.EquipmentChargesRegex2);
            var occRegex3 = new Regex(Constants.EquipmentChargesRegex3);

            var occRegex2Match = occRegex2.Match(dataValues);


            if (occRegex2Match.Success)
            {
                occData = new DetailDto();
                var occGroups = occRegex2Match.Groups;

                var endYear = Utils.GetAdjustmentYear(Convert.ToInt32(occGroups[3].ToString().Split('/')[0]), this.dateDueMonth, this.dateDueYear);


                occData.UNIQ_ID = Constants.OCC;
                occData.CHG_CLASS = Constants.LevelOne;
                occData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                occData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                occData.SP_NAME = mrcDataSpName.TrimStart();
                occData.SUBSCRIBER = serviceId;
                occData.CHG_CODE_1 = occGroups[2].ToString().Replace(Constants.Pipe, ' ');
                occData.ACTIVITY_COMP_DATE = new DateTime(endYear, Convert.ToInt32(occGroups[3].ToString().Split('/')[0]), Convert.ToInt32(occGroups[3].ToString().Split('/')[1])).ToString("M/d/yyyy");
                occData.BEG_CHG_DATE = new DateTime(endYear, Convert.ToInt32(occGroups[3].ToString().Split('/')[0]), Convert.ToInt32(occGroups[3].ToString().Split('/')[1])).ToString("M/d/yyyy");
                occData.CHG_AMT = Utils.NumberFormat(occGroups[5].ToString().Replace(Constants.MoneySign, string.Empty));
                occData.SP_SO_NUM = occGroups[4].ToString();
                occData.CURRENCY = Constants.USD;
                occData.INFO_ONLY_IND = "N";
                occData.SP_INV_RECORD_TYPE = Constants.Equipment;

                this.lineTotal += System.Convert.ToDecimal(Utils.NumberFormat(occGroups[5].ToString()).Replace(",", string.Empty));
            }
            else
            {
                var occRegexMatch = occRegex.Match(dataValues);
                if (occRegexMatch.Success)
                {
                    occData = new DetailDto();
                    var occGroups = occRegexMatch.Groups;

                    occData.UNIQ_ID = Constants.OCC;
                    occData.CHG_CLASS = Constants.LevelOne;
                    occData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                    occData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                    occData.SP_NAME = mrcDataSpName.TrimStart();
                    occData.SUBSCRIBER = serviceId;
                    occData.CHG_CODE_1 = occGroups[2].ToString();
                    occData.CHG_AMT = Utils.NumberFormat(occGroups[4].ToString().Replace(",", string.Empty));
                    occData.SP_SO_NUM = occGroups[3].ToString();
                    occData.CURRENCY = Constants.USD;
                    occData.INFO_ONLY_IND = "N";
                    occData.SP_INV_RECORD_TYPE = Constants.Equipment;


                    this.lineTotal += System.Convert.ToDecimal(occData.CHG_AMT);

                }

                else
                {
                    var occRegex3Match = occRegex3.Match(dataValues);

                    if (occRegex3Match.Success)
                    {
                        occData = new DetailDto();
                        var occGroups = occRegex3Match.Groups;

                        occData.UNIQ_ID = Constants.OCC;
                        occData.CHG_CLASS = Constants.LevelOne;
                        occData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                        occData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                        occData.SP_NAME = mrcDataSpName.TrimStart();
                        occData.SUBSCRIBER = serviceId;
                        occData.CHG_CODE_1 = occGroups[2].ToString().Replace(Constants.Pipe, ' ');
                        occData.CHG_AMT = Utils.NumberFormat(occGroups[3].ToString().Replace(",", string.Empty));
                        occData.CURRENCY = Constants.USD;
                        occData.INFO_ONLY_IND = "N";
                        occData.SP_INV_RECORD_TYPE = Constants.Equipment;


                        this.lineTotal += System.Convert.ToDecimal(occData.CHG_AMT);
                    }
                    else
                    {
                        var accountMonthlyRegex = new Regex(Constants.DevicePaymentChargeRegex);
                        if (accountMonthlyRegex.IsMatch(dataValues))
                        {
                            occData = new DetailDto();
                            var accountMonthly = accountMonthlyRegex.Match(dataValues).Groups;

                            occData.UNIQ_ID = Constants.OCC;
                            occData.CHG_CLASS = Constants.LevelOne;
                            occData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                            occData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                            occData.CHG_CODE_1 = accountMonthly[2].ToString();
                            occData.SP_NAME = mrcDataSpName.TrimStart();
                            occData.SUBSCRIBER = serviceId;
                            occData.CHG_AMT = Utils.NumberFormat(accountMonthly[3].ToString().Replace(Constants.MoneySign, string.Empty));

                            occData.CURRENCY = Constants.USD;
                            occData.INFO_ONLY_IND = "N";
                            occData.SP_INV_RECORD_TYPE = Constants.ACCOUNT_CHARGES;

                            this.lineTotal += System.Convert.ToDecimal(occData.CHG_AMT);
                        }
                        else
                        {
                            var equipmentRegex = new Regex(Constants.EquipmentChargesRegex1).Match(dataValues);
                            if (equipmentRegex.Success && dataValues.Contains("Device"))
                            {
                                occData = new DetailDto();
                                var accountMonthly = equipmentRegex.Groups;

                                occData.UNIQ_ID = Constants.OCC;
                                occData.CHG_CLASS = Constants.LevelOne;
                                occData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                                occData.ACCT_LEVEL_2 = Constants.VerizonWireless;
                                occData.CHG_CODE_1 = accountMonthly[2].ToString();
                                occData.SP_NAME = mrcDataSpName.TrimStart();
                                occData.SUBSCRIBER = serviceId;
                                occData.CHG_AMT = Utils.NumberFormat(accountMonthly[3].ToString().Replace(Constants.MoneySign, string.Empty));

                                occData.CURRENCY = Constants.USD;
                                occData.INFO_ONLY_IND = "N";
                                occData.SP_INV_RECORD_TYPE = Constants.Equipment;

                                this.lineTotal += System.Convert.ToDecimal(occData.CHG_AMT);
                            }
                        }
                    }
                }
            }
            return occData;
        }

        // ***************************************************************************************************************************************
        // *************************************Account Level Adjustments Section ****************************************************************
        // ***************************************************************************************************************************************
        private DetailDto GetAdjustmentDevices(string dataValues)
        {

            var accountMonthlyRegex = new Regex(Constants.AdjustmentRegex);
            var accountMonthlyRegex1 = new Regex(Constants.AdjustmentRegex1);
            var accountMonthlyRegex2 = new Regex(Constants.AdjustmentRegex2);

            DetailDto taxData = null;

            if (accountMonthlyRegex1.IsMatch(dataValues))
            {
                taxData = new DetailDto();
                var accountMonthly = accountMonthlyRegex1.Match(dataValues).Groups;
                var dataValueArray = dataValues.Split(Constants.Pipe);

                DateTime parsedDate;

                DateTime.TryParseExact(accountMonthly[7].ToString(), "MM/dd/yy", null,
                        DateTimeStyles.None, out parsedDate);

                taxData.UNIQ_ID = Constants.ADJBF;
                taxData.CHG_CLASS = Constants.LevelOne;
                taxData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                taxData.CHG_CODE_1 = accountMonthly[3].ToString().Replace(Constants.Pipe, ' ');
                taxData.SUBSCRIBER = accountMonthly[6].ToString().Replace("-", string.Empty);
                taxData.BEG_CHG_DATE = parsedDate.ToString("M/d/yyyy");
                taxData.CHG_AMT = Utils.NumberFormat(accountMonthly[8].ToString()).Replace(",", string.Empty);

                taxData.CURRENCY = Constants.USD;
                taxData.INFO_ONLY_IND = "N";

                this.lineTotal += System.Convert.ToDecimal(taxData.CHG_AMT);

            }
            else if (accountMonthlyRegex2.IsMatch(dataValues))
            {
                taxData = new DetailDto();
                var accountMonthly = accountMonthlyRegex2.Match(dataValues).Groups;

                DateTime parsedDate;

                DateTime.TryParseExact(accountMonthly[6].ToString(), "MM/dd/yy", null,
                        DateTimeStyles.None, out parsedDate);

                taxData.UNIQ_ID = Constants.ADJBF;
                taxData.CHG_CLASS = Constants.LevelOne;
                taxData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                taxData.CHG_CODE_1 = accountMonthly[3].ToString();
                taxData.SUBSCRIBER = accountMonthly[4].ToString().Replace("-", string.Empty);
                taxData.BEG_CHG_DATE = parsedDate.ToString("M/d/yyyy");
                taxData.CHG_AMT = Utils.NumberFormat(accountMonthly[7].ToString().Replace(",", string.Empty));

                taxData.CURRENCY = Constants.USD;
                taxData.INFO_ONLY_IND = "N";

                this.lineTotal += System.Convert.ToDecimal(taxData.CHG_AMT);

            }
            else if (accountMonthlyRegex.IsMatch(dataValues))
            {
                taxData = new DetailDto();

                var accountMonthly = accountMonthlyRegex.Match(dataValues).Groups;
                var dataValueArray = dataValues.Split(Constants.Pipe);


                var dateString = accountMonthly[3].ToString().Split(' ')[accountMonthly[3].ToString().Split(' ').Length - 1];

                taxData.UNIQ_ID = Utils.FindTypeByName(dataValues);
                taxData.CHG_CLASS = Constants.LevelOne;
                taxData.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                taxData.CHG_CODE_1 = accountMonthly[2].ToString();
                taxData.CHG_AMT = Utils.NumberFormat(accountMonthly[3].ToString().Replace(",", string.Empty));

                taxData.CURRENCY = Constants.USD;
                taxData.INFO_ONLY_IND = "N";

                this.lineTotal += System.Convert.ToDecimal(taxData.CHG_AMT);

            }

            return taxData;

        }

        private List<DetailDto> GenerateTotalCurrentCharges(List<DetailDto> accountLevelAdjustments)
        {
            var result = new List<DetailDto>();

            List<DetailDto> temp = accountLevelAdjustments.FindAll(b => !b.SUBSCRIBER.Equals(string.Empty));

            foreach (DetailDto element in temp)
            {
                DetailDto mrcTotal = new DetailDto();


                mrcTotal.UNIQ_ID = Constants.MRC;
                mrcTotal.CHG_CLASS = Constants.LevelOne;
                mrcTotal.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                mrcTotal.ACCT_LEVEL_2 = Constants.VerizonWireless;
                mrcTotal.SP_NAME = element.SP_NAME;
                mrcTotal.SUBSCRIBER = element.SUBSCRIBER;
                mrcTotal.CHG_CODE_1 = $"Total Current Charges for {element.SUBSCRIBER}";
                mrcTotal.CHG_AMT = element.CHG_AMT;
                mrcTotal.CURRENCY = Constants.USD;
                mrcTotal.INFO_ONLY_IND = "Y";
                //mrcTotal.SP_INV_RECORD_TYPE = Constants.MonthlyCharges.ToUpper();
                mrcTotal.UDF = "";

                result.Add(mrcTotal);
            }

            return result;
        }

        public DetailDto SetValue(string type, string planName, string chgQty1Type, string chgQty1Used, string chgQty1Allowed, string chgQty1Billed, string chgAmt, string spInvRecordType)
        {
            lock (this)
            {
                DetailDto detailDto = new DetailDto();

                detailDto.UNIQ_ID = type;
                detailDto.CHG_CLASS = Constants.LevelOne;
                detailDto.ACCT_LEVEL = this.accountNumber.Replace(Constants.Hyphen, string.Empty);
                detailDto.ACCT_LEVEL_2 = Constants.VerizonWireless;
                detailDto.CHG_CODE_1 = planName;
                detailDto.CHG_QTY1_TYPE = chgQty1Type;
                detailDto.CHG_QTY1_USED = Utils.NumberFormat(chgQty1Used);
                detailDto.CHG_QTY1_ALLOWED = Utils.NumberFormat(chgQty1Allowed);
                detailDto.CHG_QTY1_BILLED = Utils.NumberFormat(chgQty1Billed);
                detailDto.CHG_AMT = chgAmt.Contains("--") ? "0" : Utils.NumberFormat(chgAmt.Replace(",", string.Empty).Replace(Constants.MoneySign, string.Empty));
                detailDto.CURRENCY = Constants.USD;
                detailDto.INFO_ONLY_IND = "N";
                detailDto.SP_INV_RECORD_TYPE = spInvRecordType;
                detailDto.UDF = "";


                this.lineTotal += System.Convert.ToDecimal(detailDto.CHG_AMT);

                return detailDto;
            }

        }


        public int getLineDetailNumPage(int page)
        {

            int numPages = 0;
            string pageText = "";
            do
            {
                numPages++;
                pageText = document.Pages[page].ExtractText();
            }
            while (!pageText.Contains(Constants.TotalCurrentChargesFor));
            return numPages;
        }


        public void PlainTextConstructor(FileDto file, HeaderDto header, List<DetailDto> details, string sourcePath, string outPutPath, string processedFilesPath)
        {
            lock (this)
            {
                string path = $@"{outPutPath}\{file.SP_FILENAME}";

                header.ACCT_TYPE = this.acctType;

                List<string> headerValues = new List<string>();
                List<string> fileValues = new List<string>();
                List<List<string>> detailFather = new List<List<string>>();
                if (!File.Exists(path))
                {
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        foreach (var item in file.GetType().GetProperties())
                        {
                            fileValues.Add(item.GetValue(file, null).ToString());
                        }

                        foreach (var item in header.GetType().GetProperties())
                        {
                            headerValues.Add(item.GetValue(header, null).ToString());
                        }

                        foreach (var detail in details)
                        {
                            List<string> detailSon = new List<string>();

                            foreach (var item in detail.GetType().GetProperties())
                            {
                                detailSon.Add(item.GetValue(detail, null).ToString());
                            }

                            detailFather.Add(detailSon);
                        }

                        sw.WriteLine("901|2");
                        sw.WriteLine("902|1|UNIQ_ID|UNIBILL_VERSION|ACCT_LEVEL_1|ACCT_TYPE|FILE_IDENTIFIER|DATE_RECEIVED_FROM_SP|UNIBIL_GEN_DT|EDI_SENDER_ID|EDI_RECEIVER_ID|EDI_CONTROL_NUMBER|MAP_USED|SP_FILENAME|SP_CUST_ID|SP_ORIG_SYS|SP_VERSION|SP_RELEASE|SP_PRODUCT|SP_MEDIA_CREATION_DATE|SP_DOCUMENT_ID|SP_SUBSCRIPTION_ID|SP_CUST_NAME|FIRST_INV_IND");
                        sw.WriteLine("902|2|UNIQ_ID|ACCT_LEVEL|ACCT_LEVEL_1|ACCT_LEVEL_2|SP_INV_NUM|ACCT_TYPE|INV_DATE|BILL_PERIOD_START|BILL_PERIOD_END|DUE_DATE|DATE_ISSUED|PREV_BILL_AMT|PMTS_RCVD|PMTS_APP_THRU_DATE|BAL_FWD_ADJ|BAL_FWD|TOT_NEW_CHG_ADJ|TOT_NEW_CHGS|TOT_AMT_DUE_ADJ|TOT_AMT_DUE|TOT_MRC_CHGS|TOT_OCC_CHGS|TOT_USAGE_CHGS|TOT_TAXSUR|TOT_DISC_AMT|SP_ACCT_STATUS_IND|SP_NAME|SP_REMIT_ADDR_1|SP_REMIT_ADDR_2|SP_REMIT_ADDR_3|SP_REMIT_ADDR_4|SP_REMIT_CITY|SP_REMIT_STATE|SP_REMIT_ZIP|SP_REMIT_COUNTRY|SP_INQUIRY_TEL_NUM|BILLED_COMPANY_NAME|BILLED_COMPANY_ADDR_1|BILLED_COMPANY_ADDR_2|BILLED_COMPANY_ADDR_3|BILLED_COMPANY_ADDR_4|BILLED_COMPANY_CITY|BILLED_COMPANY_STATE|BILLED_COMPANY_ZIP|BILLED_COMPANY_COUNTRY|ACNA|CURRENCY|SP_CODE|OLD_ACCT_NUM|CONTRACT_ID|CONTRACT_EFF_DATE|CONTRACT_END_DATE|CONTRACT_COMMITMENT|CONTRACT_COMMITMENT_MET|SP_INV_LINE_NUM|SP_INV_RECORD_TYPE|SP_TOT_NEW_CHGS|SP_TOT_AMT_DUE|SP_BAL_FWD|UDF");
                        sw.WriteLine("902|3|UNIQ_ID|CHG_CLASS|ACCT_LEVEL|ACCT_LEVEL_1|ACCT_LEVEL_2|SP_NAME|SUBSCRIBER|SP_SERV_ID|SP_SERV_ID_TYPE|REL_SP_FAC_ID|BTN|SUPERUSOC|CLASS_OF_SVC_USOC|CHG_CODE_1|CHG_DESC_1|CHG_CODE_2|CHG_DESC_2|CHG_CODE_3|CHG_DESC_3|CHARGE_CATEGORY|ACTIVITY_TYPE|SVC_TYPE|SVC_SUB_TYPE|ACTIVITY_COMP_DATE|SVC_ESTABLISH_DATE|BEG_CHG_DATE|END_CHG_DATE|CHG_QTY1_TYPE|CHG_QTY1_USED|CHG_QTY1_ALLOWED|CHG_QTY1_BILLED|CHG_QTY2_TYPE|CHG_QTY2_USED|CHG_RATE|CHG_AMT|CHG_BASIS|DISC_PCT|PRORATE_FACTOR|CURRENCY|INFO_ONLY_IND|SITE_A_ID|SITE_A_ADDR_1|SITE_A_ADDR_2|SITE_A_ADDR_3|SITE_A_ADDR_4|SITE_A_ADDR_CITY|SITE_A_ADDR_ST|SITE_A_ADDR_CNTRY|SITE_A_ADDR_ZIP|SITE_A_NPA_NXX|SITE_Z_ID|SITE_Z_ADDR_1|SITE_Z_ADDR_2|SITE_Z_ADDR_3|SITE_Z_ADDR_4|SITE_Z_ADDR_CITY|SITE_Z_ADDR_ST|SITE_Z_ADDR_CNTRY|SITE_Z_ADDR_ZIP|SITE_Z_NPA_NXX|FAC_BW|FAC_BW_UNIT_TYPE|DISC_PLAN|RATE_PLAN|CONTRACT_ID|CONTRACT_EFF_DATE|CONTRACT_END_DATE|ORIG_INV_DATE|CFA|JUR|JUR_STATE_OR_CNTRY|TAX_JUR|SP_SO_NUM|CUST_SO_NUM|CUST_CHG_CODE|PIC|PIC_NAME|LPIC|LPIC_NAME|CUST_V_COORD|CUST_H_COORD|SP_V_COORD|SP_H_COORD|CUST_CLLI|SP_CLLI|TAX_ID_NUM|CALL_TYPE|PROD_TYPE|DIR_IND|SHARE_IND|CURR_PRIR_IND|RATE_PERIOD|ROAM_IND|BAND|SP_INV_LINE_NUM|SP_INV_RECORD_TYPE|UDF");
                        sw.WriteLine("902|4|UNIQ_ID|ACCT_LEVEL|ACCT_LEVEL_1|ACCT_LEVEL_2|SP_NAME|SUBSCRIBER|SP_SERV_ID|SP_SERV_ID_TYPE|CHG_CODE_1|CHG_DESC_1|CHG_QTY1_BILLED|CHG_RATE|CHG_UNIT_TYP|CHG_AMT|CURRENCY|BEG_CHG_DATE|END_CHG_DATE|INFO_ONLY_IND|SITE_A_ID|FR_NUM|FR_CITY|FR_ST|FR_CNTRY|TO_NUM|TO_CITY|TO_ST|TO_CNTRY|SVC_TYPE|SVC_SUB_TYPE|DISC_PLAN|RATE_PLAN|RATE_CODE|RATE_PERIOD|DIR_IND|HOME_ROAM_IND|SP_INV_LINE_NUM|SP_INV_RECORD_TYPE|JUR|CALL_TYPE|PROD_TYPE|CALL_COMP_CODE|FEAT_CODE|CUST_CHG_CODE|CALLL_TERM_TYP|TRANSLATED_NUM|UDF|USG_BAND|AIR_CHG_AMT|LD_CHG_AMT|ROAM_CHG_AMT|TAX_SUR_CHG_AMT|ROAM_TAX_CHG_AMT|FEAT_CHG_AMT|DISC_CHG_AMT|MSG_CHG_AMT|DATA_CHG_AMT|VIDEO_CHG_AMT");
                        sw.WriteLine("902|5|UNIQ_ID|ACCT_LEVEL|ACCT_LEVEL_1|ACCT_LEVEL_2|SP_INV_LINE_NUM|SP_INV_RECORD_TYPE|NOTE");
                        sw.WriteLine("902|6|UNIQ_ID|ACCT_LEVEL|ACCT_LEVEL_1|ACCT_LEVEL_2|CONT_TYPE|CONT_NAME|CONT_EMAIL|CONT_NUMBER|CONT_FAX|CONT_COMMENTS|CONT_URL|CONT_HOURS");
                        sw.WriteLine("902|7|UNIQ_ID|ACCT_LEVEL|ACCT_LEVEL_1|ACCT_LEVEL_2|SP_INV_NUM|SP_BILL_PD_ST|SP_BILL_PD_END|WRLS_ID|USER_NAME|CCID|PLAN_DESC|ACCT_CHG_CRDS|MO_ACC_CHGS|USG_CHGS|EQUIP_CHGS|OCC_CHGS|TAX_SUR_CHGS|TOT_CHGS|VOICE_ALLOW|VOICE_PLAN_USG|VOICE_M2M_USG|VOICE_NW_USG|VOICE_RM_USG|DATA_ALLOW_KB|DATA_USG_KB|TXT_ALLOW|TXT_USG|OTHER_NON_BILL_USG|TEXT_CHGS|CURRENCY|SP_INV_LINE_NUM|SP_INV_RECORD_TYPE");
                        sw.WriteLine("903|7");
                        sw.WriteLine("904");
                        sw.WriteLine("1|" + string.Join(Constants.Pipe, fileValues));
                        sw.WriteLine("2|" + string.Join(Constants.Pipe, headerValues));
                        var count = detailFather.Count();


                        if (this.lineTotal != this.reconciliationValue)
                        {
                            var differences = this.lineTotal - this.reconciliationValue;
                            sw.WriteLine($"3||mrc|1|{this.accountNumber.Replace(Constants.Hyphen, string.Empty)}||Verizon Wireless||||||||||||||||||||||||||||{ this.reconciliationValue}|{this.lineTotal}|{differences}|||USD|N||||||||||||||||||||||||||||||||||||||||||||||||||||||||RECONCILIATION|");
                        }

                        foreach (var son in detailFather)
                        {
                            sw.WriteLine("3||" + string.Join(Constants.Pipe, son));
                        }
                    }
                }

                if (this.noReconciledLine.Count > 0)
                {
                    var lineWithError = $@"{outPutPath}\Error_{file.SP_FILENAME.Replace(".txt", string.Empty)}_no_reconciled.txt";
                    using (StreamWriter sw = File.CreateText(lineWithError))
                    {
                        foreach (var line in noReconciledLine)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }


                var zipName = $@"{outPutPath}\MSSPDF_{file.SP_FILENAME.Replace(".txt", ".zip")}";

                using (ZipFile zip = new ZipFile())
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zip.AddFile(path, "");
                    zip.AddFile(sourcePath, "");
                    zip.Save(zipName);
                }

                var sourceFileName = Path.GetFileName(sourcePath);

                if (File.Exists($@"{processedFilesPath}\{sourceFileName}"))
                {
                    File.Delete($@"{processedFilesPath}\{sourceFileName}");
                }

                File.Move(sourcePath, $@"{processedFilesPath}\{sourceFileName}");
                File.Delete(path);
            }
        }
    }
}
