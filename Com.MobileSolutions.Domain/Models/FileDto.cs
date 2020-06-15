using System;
using System.Collections.Generic;
using System.Text;

namespace Com.MobileSolutions.Domain.Models
{
    public class FileDto
    {
        public string UNIQ_ID { get; set; }

        public string UNIBILL_VERSION { get; set; }

        public string ACCT_LEVEL_1 { get; set; }

        public string ACCT_TYPE { get; set; }

        public string FILE_IDENTIFIER { get; set; }

        public string DATE_RECEIVED_FROM_SP { get; set; }

        public string UNIBIL_GEN_DT { get; set; }

        public string EDI_SENDER_ID { get; set; }

        public string EDI_RECEIVER_ID { get; set; }

        public string EDI_CONTROL_NUMBER { get; set; }

        public string MAP_USED { get; set; }

        public string SP_FILENAME { get; set; }

        public string SP_CUST_ID { get; set; }

        public string SP_ORIG_SYS { get; set; }

        public string SP_VERSION { get; set; }

        public string SP_RELEASE { get; set; }

        public string SP_PRODUCT { get; set; }

        public string SP_MEDIA_CREATION_DATE { get; set; }

        public string SP_DOCUMENT_ID { get; set; }

        public string SP_SUBSCRIPTION_ID { get; set; }

        public string SP_CUST_NAME { get; set; }

        public string FIRST_INV_IND { get; set; }
    }
}
