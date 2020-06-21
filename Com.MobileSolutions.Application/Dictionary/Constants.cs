using System;
using System.Collections.Generic;
using System.Text;

namespace Com.MobileSolutions.Application.Dictionary
{
    public class Constants
    {
        public static string OverviewOfVoice = "Overview of Voice";
        public static string LevelOne = "1";
        public static string MoneySign = "$";
        public static string Comma = ",";
        public static string Hyphen = "-";
        public static string StringPipe = "|";
        public static string WhiteSpace = " ";
        public static char Pipe = '|';
        public static string LineSeparator = "%@";

        public static string FileName = @"C:\Users\kzuluaga\Documents\Ingeneo\Proyectos\6. EEUU\Pdf Reader\Test Documents\9842064636_PERFORMANCEFOODGROUP_2019-11-12_62027583100011\9842064636_PERFORMANCEFOODGROUP_2019-11-12_62027583100011.pdf";//201912_VerizonWireless_78590905400001.pdf
        public static string OutPutPath = @"C:\Users\kzuluaga\Documents\Ingeneo\Proyectos\6. EEUU\Pdf Reader\Test Documents\9842064636_PERFORMANCEFOODGROUP_2019-11-12_62027583100011\";

        public static string BalanceForwardDueImmediately = "Balance Forward";
        public static string CreditBalance = "Credit Balance";
        public static string PreviousBalance = "Previous Balance";
        public static string AccountNumber = "Account Number";
        public static string Payment = "Payment";
        public static string FormatedDate = "MM/dd/yyyy";
        public static string Adjustments = "Adjustments";
        public static string AccountChargesAndCredits = "Account Charges and Credits";
        public static string AccountChargesAndCreditsContinue = "Account Charges and Credits, continued";
        public static string MonthlyCharges = "Monthly Charges";
        public static string YourPlan = "Your Plan";
        public static string YourPlanMonthlyCharges = "Your Plan|Monthly Charges";
        public static string TotalCurrentChargesFor = "Total Current Charges for";
        public static string AccountMonthlyCharges = "Account Monthly Charges";
        public static string AccountUsageCharges = "Account Usage Charges";
        public static string Broadband = "Broadband Hotspot Management";
        public static string Subtotal = "Subtotal";
        public static string TaxesType = "TAXES/GOVT";
        public static string M2MTax = "M2M Tax";
        public static string SurchargesType = "SURCHARGES";
        public static string Equipment = "EQUIPMENT";
        public static string EquipmentCharges = "Equipment Charges";
        public static string OtherCharges = "Other Charges & Credits";
        public static string TaxesSurCharges = "Taxes Governmental Surcharges & Fees";
        public static string TotalCurrentCharges = "Total Current Charges";
        public static string VerizonWireless = "Verizon Wireless";
        public static string OverviewM2M = "Overview of Machine to Machine Activity";
        public static string OverviewM2MContinue = "Overview of Machine to Machine Activity, Continued";
        public static string PaymentsAdjustments = "Payments and Adjustments";
        public static string PaymentsAdjustmentsContinue = "Payments and Adjustments, continued";
        public static string AdjustmentsContinued = "Adjustments, continued";
        public static string TotalAdjustments = "Total Adjustments";
        public static string DevicePaymentCharge = "Device Payment Charges";
        public static string TotalDevicePaymentCharge = "Total Device Payment Charges";
        public static string TotalUsagePurchaseCharges = "Total Usage and Purchase Charges";
        public static string TotalCurrentChargesM2M = "Total Current Charges of Machine to Machine Activity";
        public static string TotalData = "Total Data";
        public static string TaxesGovernmentalSurcharges = "Taxes, Governmental Surcharges";
        public static string Surcharges = "Surcharges";
        public static string Tax = "Tax";
        public static string PurchasesTitle = "Date|Other Merchants|Description|Cost";
        public static string PurchasesTitle2 = "Date|Verizon Wireless|Description|Cost";
        public static string TotalPurchases = "Total Purchases";
        public static string InternationTitle = "International|Allowance|Used|Billable|Cost";
        public static string TotaInternational = "Total International";
        public static string UsagePurchaseCharges = "Usage and Purchase Charges";
        public static string DataTitle = "Data|Allowance|Used|Billable|Cost";
        public static string RoamingTitle = "Roaming|Allowance|Used|Billable|Cost";
        public static string TotalRoaming = "Total Roaming";
        public static string MessagingTitle = "Messaging|Allowance|Used|Billable|Cost";
        public static string VoiceTitle = "Voice|Allowance|Used|Billable|Cost";
        public static string QuickBillSummary = "Quick Bill Summary";
        public static string TotalVoice = "Total Voice";
        public static string TotalMessaging = "Total Messaging";
        public static string PurchasesHeaderTitle = "Purchases";
        public static string TotalChargesDuebyTitle = "Total Charges Due by";
        public static string TotalAmountTitle = "Total Amount";
        public static string OtherChargesCredits = "Other Charges and Credits";
        public static string RawYourPlanMonthlyCharges = "        Your Plan                                        Monthly Charges";
        //*******************************************************************************************************************
        //*********************************************** Regex *************************************************************
        //*******************************************************************************************************************
        public static string OtherChargesCreditsRegex = @"(([^\|][^\|]+)(\|\d{1,3} of \d{1,3})?)\|(\-?[0-9]*\.[0-9]{1,2})";
        public static string FinalValueRegex = @"^(\|)?(\-?\$[0-9]*(\.[0-9]{1,2})?)$";
        public static string SurchargesTitleRegex = @"Surcharges\+?$";
        public static string AdjustmentsTitleRegex = @"^(\||[^\|]*\||)(Adjustments|Adjustments, continued)$";
        public static string TaxesGovernmentalSurchargesFeesTitleRegex = @"Taxes, Governmental Surcharges and Fees\+?$";
        public static string SummaryRegex = @"^Summary for(.*): ([0-9]{3}-[0-9]{3}-[0-9]{4})";
        public static string AccountNumberRegex = @"Account Number\|(([0-9])*-([0-9])+)";
        public static string CreditBalanceRegex = @"Credit Balance\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*)";
        public static string BalanceForwardDueImmediatelyRegex = @"Balance Forward( Due Immediately)?\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*)";
        public static string MoneyRegex = @"(\(?[A-z0-9]*[.]?\)?\s?\|?|[&]\s|[-]\s)+(\-?\$[0-9]*(\.[0-9]{1,2})?)$";
        public static string OnlyMoneyRegex = @"(\-?\$[0-9]*(\.[0-9]{1,2})?)$";
        public static string MoneyRegexWithout = @"(\-?[0-9]*(\.[0-9]*))$";
        public static string MoneyRegexWithoutCero = @"(^\-?|^)(\.[0-9]*)$";
        public static string RemitRegex = @"(([A-Z]|\s)*),\s*([A-Z]*)\s*(([0-9]|-|\s)*)";
        public static string DetailsRegex = @"^\|([0-9]*)-([0-9]*)-([0-9]*)(.)*$";
        public static string usgsumRegex = @"^([A-z&\s]+)([0-9]{2}\/[0-9]{2}\/[0-9]{2}\:)*(\([0-9]{2}\/[0-9]{2}\s\-\s[0-9]{2}\/[0-9]{2}\))*(\|[A-z\s0-9-.]*)+$";
        public static string surTaxesRegex = @"([A-z$0-9.&%\s])\|(([0-9.])+)$";
        public static string occRegex = @"^([A-z$0-9.&%\s\/-]\|?)+([0-9.])+$";
        public static string M2MGetPlanName = @"^\|?([^\|]*)\|\d{1,3}\|(\-?\$([0-9]+,)?[0-9]*\.[0-9]*)\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*|\-\-)\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*|\-\-)\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*|\-\-)(\|\-?\$[0-9]*\.[0-9]*)\|--\|(\-?\$[0-9]*\.[0-9]*)";
        public static string M2MUsgsumAvoid = @"^\|?([^\|]+)\|\d of \d\|(\-?|^)\$(\.0*)\|--\|--\|--\|--$";
        public static string M2MDiscount = @"^\|?([^\|]*)\|\d{1,3} of \d{1,3}\|(\-?\$([0-9]+,)?[0-9]*\.[0-9]*)$";
        public static string M2MUsgsum = @"^\|?([^\|]+)\|\d{1,3} of \d{1,3}(\|((\-?|^)\$([0-9]*\.[0-9]*)|--))?\|((\-?|^)\$([0-9]*\.[0-9]*)|--)(\|([0-9]*\.?[0-9]*(GB|MB|KB)|--))?\|([0-9]*\.?[0-9]*(GB|MB|KB)|--)\|([0-9]*\.?[0-9]*(GB|MB|KB)|--)$";
        public static string M2MUsgsumMsg = @"^\|?(.+)\|\d{1,3} of \d{1,3}\|(\-?\$([0-9]*,)?([0-9]*)?\.[0-9]*|--)\|(([0-9]*,)?[0-9]*|--)\|(([0-9]*,)?[0-9]*|--)\|(([0-9]*,)?[0-9]*|--)$";
        public static string mrcDateRegex = @" ^ ([0-9]{1,2}\/[0-9]{1,2}\s\-\s[0-9]{1,2}\/[0-9]{1,2})$";
        public static string pageRegex = @"([0-9]{1,10})(\sof\s)([0-9]{1,10})";
        public static string NumberRegex = @"\d+";
        public static string AccountMonthlyRegex = @"^(\||)([^\|]+)(\||\s)(\d{1,2}\/\d{1,2} - \d{1,2}\/\d{1,2})\|(-?(\d|\,|\.)*)$";

        public static string UsgPurchesCharges2 = @"([^\|]*)\|?(gigabytes|megabytes|kilobytes)((\||\s)unlimited)?((\||\s)(([0-9]+,)*[0-9]*\.?[0-9]*|\-\-))?(\||\s)(([0-9]+,)*[0-9]*\.?[0-9]*|\-\-)(\||\s)(([0-9]+,){0,2}[0-9]*\.?[0-9]*|\-\-)(\||\s)(\-?\$?([0-9]*,)*[0-9]*\.[0-9]*|\-\-)$";

        public static string UsgPurchesCharges1 = @"([^\|]*)\|?(gigabytes|megabytes|kilobytes)( unlimited)?((\||\s)(([0-9]+,){0,2}[0-9]+\.?[0-9]*|\-\-))?\|(([0-9]+,)?[0-9]*\.?[0-9]*|\-\-)(\||\s)(([0-9]+,)?[0-9]*\.?[0-9]*|\-\-)(\||\s)(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*|\-\-)$";
        public static string DateRegex = @"\d{1,2}\/\d{1,2} - \d{1,2}\/\d{1,2}";
        public static string AdjustmentRegex = @"^(\||)([^\|]+)\|(-?(\d|\,|\.)*)$";
        public static string AdjustmentRegex1 = @"^(\||)(([^\|]+)\|(\d*\s?)?(for (\d{3}\-\d{3}\-\d{4})\s?)?on (\d{1,2}\/\d{1,2}\/\d{2}))\|(-?(\d|\,|\.)*)$";
        public static string AdjustmentRegex2 = @"^(\||)(([^\|]+) (\d{3}\-\d{3}\-\d{4})?(\s)?on (\d{1,2}\/\d{1,2}\/\d{2}))\|(-?(\d|\,|\.)*)$";
        public static string TotalCurrentChargesRegex = @"(Total Current Charges for [0-9]{3}-[0-9]{3}-[0-9]{4})\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*)$";
        public static string EquipmentChargesRegex = @"([^\|]+\|)?(Device Payment Agreement ([0-9]*) - Payment [0-9]{1,2} of [0-9]{1,2})\|\-?(([0-9]*)?\.[0-9]*)$";
        public static string EquipmentChargesRegex1 = @"([^\|]*\|)?([^\|]*)\|\-?(([0-9]*)?\.[0-9]*)$";
        public static string EquipmentChargesRegex2 = @"([^\|]*\|)?(Equipment Purchase\|(\d{1,2}\/\d{1,2}) [^\|]*\|([0-9]*))\|(\-?([0-9]*,)?([0-9]*)?\.[0-9]*)$";
        public static string EquipmentChargesRegex3 = @"([^\|]*\|)?([^\|]*\|\(one-time charge\))\|\-?(([0-9]*)?\.[0-9]*)$";
        public static string MonthlyChargesRegex = @"(([^\|]+)\|)?([^\|]+)\|([0-9]{2})\/([0-9]{2})\s\-\s([0-9]{2})\/([0-9]{2})\|(\-?[0-9]*(\.[0-9]*))$";
        public static string DevicePaymentChargeRegex = @"([^\|]+\|)?(Device Payment Buyout Charge \(\d{1,3} - \d{1,3}\) Agreement \d*)\|(\-?([0-9]+,)*[0-9]*(\.[0-9]*))$";
        public static string VoiceRegex = @"([^\|][^\|]+)\|(\-?[0-9]*\.[0-9]{1,2})";
        public static string BroadbandRegex = @"^\|(([^\|]+) (\d{1,2}\/\d{1,2}\/\d{1,2}))\|(-?\$?(\d|\,|\.)*)$";
        public static string DetailForRegex = @"^Detail\sfor\s([^:]+):\s(\d{3}\-\d{3}\-\d{4})$";
        public static string SurchargesRegex = @"([^\|]*\|)?([^\|]*)\|(\-?[0-9]*(\.[0-9]*))$";
        public static string SurchargesRegex2 = @"([^\|]*\|)?([^\|]*(\||\s)\d* of \d*)\|(\-?[0-9]*(\.[0-9]*))$";
        public static string TaxesGovernmentalSurchargesRegex = @"(([^\|]+)\|)?([^\|]+)\|(\-?[0-9]*(\.[0-9]*))$";
        public static string IndexRegex = @"^(\|[^\|]+)*\|([0-9]*)\|(-?\$(\d|\,|\.)*)(.+)";
        public static string IndexRegex2 = @"(^\|[0-9]{3}-[0-9]{3}-[0-9]{4})(([^\|\s\-]*|\||\s|\-)+)(\||\s|\-)?([0-9]{1,5})?(\||\s)(\-?\$[0-9]*\.[0-9]+)";
        public static string IndexRegex3 = @"(\d{1,5}$)";
        public static string PurchasesRegex = @"(\|[^\|]+)*\|?([0-9]{2})\/([0-9]{1,2})\|?([^\|]*\|[^\|]*)\|(\-?[0-9]*?\.[0-9]*)$";
        public static string InternationalMessageRegex = @"([^\|]*)\|?(messages( sent| rcvd)?)( unlimited)?(\|(([0-9]*,)?[0-9]*|\-\-))?\|(([0-9]*,)?[0-9]*|\-\-)\|(([0-9]*,)?[0-9]*|\-\-)\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*|\-\-)$";
        public static string InternationVoiceRegex = @"(|[^\|]+\|)([^\|]{2}[^\|]+|[^\|]+\|\d{2}\/\d{2})\|(-?\$?(\d|\,|\.)+)$";
        public static string InternationDataRegex = @"([^\|]*)\|?(gigabytes|megabytes|kilobytes)( unlimited)?((\||\s)(([0-9]+,){0,2}[0-9]+\.?[0-9]*|\-\-))?\|(([0-9]+,)?[0-9]*\.?[0-9]*|\-\-)(\||\s)(([0-9]+,)?[0-9]*\.?[0-9]*|\-\-)(\||\s)(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*|\-\-)$";
        public static string InternationDataSpecialCaseRegex = @"\|?([^\|]+ - (\d+) at \$ (\d+)\/(([0-9]+,)?[0-9]*\.?[0-9]*) (GB|MB|KB) (\d{1,2}\/\d{1,2} - \d{1,2}\/\d{1,2}))\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*|\-\-)$";
        public static string InternationalMinutesRegex = @"([^\|]*)\|?(minutes|calls)( unlimited)?(\|(([0-9]+,)*[0-9]*\.?[0-9]*|\-\-))?\|(([0-9]+,)*[0-9]*\.?[0-9]*|\-\-)\|(([0-9]+,)*[0-9]*\.?[0-9]*|\-\-)\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*|\-\-)$";
        public static string UsageVoiceRegex = @"([^\|]+)\|(minutes|calls)( unlimited)?(\|[0-9]*|\|--)?\|([0-9]*)\|([0-9]*|--)\|(\-?\$?[0-9]*\.[0-9]*|\-\-)$";
        public static string HeaderRegex = @"Bill Date\|([aA-zZ]* [0-9]{2}, [0-9]{4})";
        public static string NamingConventionRegex = @"[0-9]*_[^_]*_([0-9]{4}-[0-9]{2}-[0-9]{2}|[0-9]{2}-[0-9]{2}-[0-9]{4})_[0-9]+\.pdf";
        public static string AccountChargesCreditsTitle = @"Account Charges and Credits\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*)$";
        public static string AfterAccountChargesCreditsTitle = @"[^\|]+\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*)$";
        public static string AMCRegex = @"Account Monthly Charges$";
        public static string ACCCRegex = @"Account Charges and Credits, continued$";
        public static string PARegex = @"Payments and Adjustments$";
        public static string PACRegex = @"Payments and Adjustments, continued$";
        public static string TotalChargesDuebyRegex = @"[^\|]*\|?Total Charges Due by [aA-zZ]+ \d{2},? \d{4}?\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*)$";
        public static string TotalAmountRegex = @"[^\|]*\|?Total Amount(\sDue)?\|(\-?\$?([0-9]*,)?[0-9]*\.[0-9]*)$";
        public static string BillDateRegex = @"(\|?)(Bill Date\|)([A-z\s\-0-9\,]+)";
        public static string QuickBillSummaryDateRegex = @"(\|?)(Quick Bill Summary\|)([A-z\s\-0-9\,]+)";
        public static string DueDateRegex = @"([^\|]+)(\|[^\|]+)([\s|\|])(\d{1,2}\/\d{1,2}\/\d{1,2})";
        public static string LineSeparatorRegex = @"([^%@]+%@)";
        //*******************************************************************************************************************

        public static string Voice = "Voice";
        public static string Messaging = "Messaging";
        public static string Data = "Data";
        public static string International = "International";
        public static string InvoiceNumber = "Invoice Number";
        public static string Address = "P.O.";
        public static string City = "LEHIGH VALLEY";
        public static string DetailColumns = "|Charges by Cost Center|Number|Charges|Charges|Charges|Credits|and Fees|(includes Tax)|Charges|Usage|Usage|Usage|Roaming|Roaming|Roaming";
        public static string OverviewOfLines = "Overview of Lines";
        public static string SummaryFor = "Summary for";
        public static string DetailFor = "Detail for";
        public static string Summary = "Summary";
        public static string For = "for";

        //*******************************************************************************************************************
        //**************************************************Type*************************************************************
        //*******************************************************************************************************************
        public static string MRC = "mrc";
        public static string OCC = "occ";
        public static string USGSUM = "usgsum";
        public static string SUR = "sur";
        public static string TAX = "tax";
        public static string ADJBF = "adjbf";
        //*******************************************************************************************************************

        public static string USD = "USD";
        public static string DATA = "DATA";
        public static string VOICE = "VOICE";
        public static string MESSAGING = "MESSAGING";
        public static string PURCHASES = "PURCHASES";
        public static string ROAMING = "ROAMING";
        public static string ACCOUNT_CHARGES = "ACCOUNT CHARGES";
        public static string MONTHLY_CHARGES = "MONTHLY CHARGES";
        public static string INTERNATIONAL = "INTERNATIONAL";
        public static string Gigabytes = "gigabytes";
        public static string ChargesType_GB = "gb";
        public static string ChargesType_KB = "kb";
        public static string ChargesType_MB = "mb";
        public static string ChargesType_M = "m";
        public static string ChargesType_MSG = "msg";
        public static string Roaming = "Roaming";
    }
}
