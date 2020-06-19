using Com.MobileSolutions.Application.Dictionary;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace Com.MobileSolutions.VerizonTest
{
    public class RegexTest
    {
        private string M2MUsgsum;
        private string M2MGetPlanName;
        private string M2MDiscount;
        private string Voice;
        private string EquipmentCharges;
        private string EquipmentCharges2;
        private string EquipmentCharges3;
        private string DevicePaymentCharge;
        private string UsageVoice;
        private string UsgPurchaseCharges2;
        private string Adjustment;
        private string Adjustment1;
        private string Adjustment2;
        private string InternationalMessage;

        [SetUp]
        public void Setup()
        {
            M2MUsgsum = Constants.M2MUsgsum;
            M2MGetPlanName = Constants.M2MGetPlanName;
            M2MDiscount = Constants.M2MDiscount;
            Voice = Constants.VoiceRegex;
            EquipmentCharges = Constants.EquipmentChargesRegex;
            EquipmentCharges2 = Constants.EquipmentChargesRegex2;
            EquipmentCharges3 = Constants.EquipmentChargesRegex3;
            DevicePaymentCharge = Constants.DevicePaymentChargeRegex;
            UsageVoice = Constants.UsageVoiceRegex;
            UsgPurchaseCharges2 = Constants.UsgPurchesCharges2;
            Adjustment = Constants.AdjustmentRegex;
            Adjustment1 = Constants.AdjustmentRegex1;
            Adjustment2 = Constants.AdjustmentRegex2;
            InternationalMessage = Constants.InternationalMessageRegex;
        }

        [Test]
        public void M2MUsgsum_Regex_ReturnsTrue()
        {
            string[] arrayScenario = { 
                "GIGABYTE USAGE|1 of 91|--|.105GB|--", 
                "GIGABYTE USAGE|91 of 91|--|455.000GB|184.209GB|--",
                "|10GB/ $10/GB|1 of 1|$.00|$10.00|10.000GB|10.229GB|.229GB"
            };

            var firstScenario = new Regex(M2MUsgsum).Match(arrayScenario[0]);
            Assert.IsTrue(firstScenario.Success);

            if (firstScenario.Success)
            {
                var firstScenarioGroup = firstScenario.Groups;
                Assert.AreEqual(firstScenarioGroup[1].ToString(), "GIGABYTE USAGE");
                Assert.AreEqual(firstScenarioGroup[12].ToString(), ".105GB");
            }

            var firstScenario1 = new Regex(M2MUsgsum).Match(arrayScenario[1]);
            Assert.IsTrue(firstScenario1.Success);

            if (firstScenario1.Success)
            {
                var firstScenarioGroup = firstScenario1.Groups;
                Assert.AreEqual(firstScenarioGroup[1].ToString(), "GIGABYTE USAGE");
                Assert.AreEqual(firstScenarioGroup[10].ToString(), "455.000GB");
                Assert.AreEqual(firstScenarioGroup[12].ToString(), "184.209GB");
            }

            var firstScenario2 = new Regex(M2MUsgsum).Match(arrayScenario[2]);
            Assert.IsTrue(firstScenario2.Success);


            if (firstScenario2.Success)
            {
                var firstScenarioGroup = firstScenario2.Groups;
                Assert.AreEqual(firstScenarioGroup[1].ToString(), "10GB/ $10/GB");
                Assert.AreEqual(firstScenarioGroup[6].ToString(), "$10.00");
                Assert.AreEqual(firstScenarioGroup[10].ToString(), "10.000GB");
                Assert.AreEqual(firstScenarioGroup[12].ToString(), "10.229GB");
                Assert.AreEqual(firstScenarioGroup[14].ToString(), ".229GB");
            }



        }

        [Test]
        public void M2MGetPlanName_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(M2MGetPlanName).IsMatch("|MACHINE TO MACHINE 30GB SHARE|10|$1,800.00|--|--|$.80|$.00|--|$.00|--|--|--");            

            Assert.IsTrue(firstScenario);
        }

        [Test]
        public void M2MDiscount_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(M2MDiscount).IsMatch("|22% ACCESS DISCOUNT|10 of 10|-$396.00");

            Assert.IsTrue(firstScenario);
        }

        [Test]
        public void Voice_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(Voice).IsMatch("Email & Web Unlimited|Long Distance - Verizon Wireless|21.56");

            Assert.IsTrue(firstScenario);
        }

        [Test]
        public void EquipmentCharges_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(EquipmentCharges).IsMatch("Device Payment Agreement 1313728964 - Payment 3 of 24|18.74");

            Assert.IsTrue(firstScenario);
        }

        [Test]
        public void EquipmentCharges2_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(EquipmentCharges2).IsMatch("Equipment Purchase|11/27 Nacs Vision W Cn Wpr|002176821|1,181.11");

            Assert.IsTrue(firstScenario);
        }

        [Test]
        public void EquipmentCharges3_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(EquipmentCharges3).IsMatch("Device Payment|FL State Sales Tax|(one-time charge)|21.00");
            var secondScenario = new Regex(EquipmentCharges3).IsMatch("Orange Cnty Sales Tax|(one-time charge)|1.75");

            Assert.IsTrue(firstScenario);
            Assert.IsTrue(secondScenario);
        }

        [Test]
        public void DevicePaymentCharge_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(DevicePaymentCharge).IsMatch("Device Payment Buyout Charge (3 - 24) Agreement 1313610652|412.28");

            Assert.IsTrue(firstScenario);
        }

        [Test]
        public void UsageVoice_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(UsageVoice).IsMatch("Calling Plan (01/11 - 02/05)|minutes unlimited|21|--|--");
            var secondScenario = new Regex(UsageVoice).IsMatch("Unlimitedmonthlygigabyte|411 Search|calls|--|1|1|1.99");
            var thirdScenario = new Regex(UsageVoice).IsMatch("Email & Web Unlimited|Night/Weekend|minutes unlimited|1|--|--");
            var fourthScenario = new Regex(UsageVoice).IsMatch("Mobile to Mobile|minutes unlimited|245|--|--");
            var fifthScenario = new Regex(UsageVoice).IsMatch("Unlimited|OFFPEAK|Calling Plan|minutes unlimited|43|--|--");
            var sixthScenario = new Regex(UsageVoice).IsMatch("Nationwide BUS Data SHR 2GB|Mobile to Mobile|minutes unlimited|709|--|--");
            var seventhScenario = new Regex(UsageVoice).IsMatch("Unlimited|Text Message|Shared|minutes|450|207|--|--");
            var eighthScenario = new Regex(UsageVoice).IsMatch("Email & Web Unlimited|Night/Weekend|minutes unlimited|1|--|--");
            var ninthScenario = new Regex(UsageVoice).IsMatch("Mobile to Mobile|minutes unlimited|245|--|--");
            var tenthdScenario = new Regex(UsageVoice).IsMatch("Unlimited|OFFPEAK|Calling Plan|minutes unlimited|43|--|--");
            var eleventhScenario = new Regex(UsageVoice).IsMatch("Nationwide BUS Data SHR 2GB|Mobile to Mobile|minutes unlimited|709|--|--");

            Assert.IsTrue(firstScenario);
            Assert.IsTrue(secondScenario);
            Assert.IsTrue(thirdScenario);
            Assert.IsTrue(fourthScenario);
            Assert.IsTrue(fifthScenario);
            Assert.IsTrue(sixthScenario);
            Assert.IsTrue(seventhScenario);
            Assert.IsTrue(eighthScenario);
            Assert.IsTrue(ninthScenario);
            Assert.IsTrue(tenthdScenario);
            Assert.IsTrue(eleventhScenario);
        }

        [Test]
        public void UsgPurchaseCharges2_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(UsgPurchaseCharges2).IsMatch("Gigabyte Usage|gigabytes|.002|--|--");
            var secondScenario = new Regex(UsgPurchaseCharges2).IsMatch("Gigabyte Usage|gigabytes|160.000|14.073|--|--");

            Assert.IsTrue(firstScenario);
            Assert.IsTrue(secondScenario);
        }

        [Test]
        public void Adjustment_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(Adjustment).IsMatch("|State Tax Adjustment|-48.75");
            var secondScenario = new Regex(Adjustment).IsMatch("Charles McGuire  III|2028606570");

            Assert.IsTrue(firstScenario);
            Assert.IsTrue(secondScenario);
        }

        [Test]
        public void Adjustment1_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(Adjustment1).IsMatch("|Device Payment Return - Agreement|1313726799on 01/17/20|-431.02");

            Assert.IsTrue(firstScenario);
        }

        [Test]
        public void Adjustment2_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(Adjustment2).IsMatch("|Device Payment Plan Return - Agreement from 01/15/20 on 01/15/20|-749.99");

            Assert.IsTrue(firstScenario);
        }

        [Test]
        public void InternationalMessage_Regex_ReturnsTrue()
        {
            var firstScenario = new Regex(InternationalMessage).IsMatch("UNL Picture/Video MSG|International Messages - Sent|messages|1|1|$.50");
            var secondScenario = new Regex(InternationalMessage).IsMatch("International Messages -|messages|3|--|--");
            var thirdScenario = new Regex(InternationalMessage).IsMatch("Get details for usage charges at|Text - Rcv'd|messages|--|6|6|1.20");
            var fourthScenario = new Regex(InternationalMessage).IsMatch("$10.00per GBafter allowance|Text|messages unlimited|2|--|--");
            var fifthScenario = new Regex(InternationalMessage).IsMatch("International Messages -|messages|4|--|--");
            var sixthScenario = new Regex(InternationalMessage).IsMatch("International Messages - Sent|messages|2|2|$1.00");
            var seventhScenario = new Regex(InternationalMessage).IsMatch("International Messages -|messages|1|1|$.05");
            var eighthScenario = new Regex(InternationalMessage).IsMatch("International Messages - Sent|messages|2|2|$1.00");
            var ninthScenario = new Regex(InternationalMessage).IsMatch("International Messages -|messages|1|1|$.05");

            Assert.IsTrue(firstScenario);
            Assert.IsTrue(secondScenario);
            Assert.IsTrue(thirdScenario);
            Assert.IsTrue(fourthScenario);
            Assert.IsTrue(fifthScenario);
            Assert.IsTrue(sixthScenario);
            Assert.IsTrue(seventhScenario);
            Assert.IsTrue(eighthScenario);
            Assert.IsTrue(ninthScenario);
        }
    }
}