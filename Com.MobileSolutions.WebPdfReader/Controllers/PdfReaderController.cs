﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Com.MobileSolutions.Application.Dictionary;
using Com.MobileSolutions.Application.Helpers;
using Com.MobileSolutions.Domain.Models;
using Com.MobileSolutions.VerizonWirelessReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Spire.Pdf;

namespace Com.MobileSolutions.WebPdfReader.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfReaderController : ControllerBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The spire pdf document that handles the logic to read the pdf.
        /// </summary>
        private static PdfDocument document;

        /// <summary>
        /// Helper with usable methods for the pdf reading.
        /// </summary>
        private static ApplicationHelper helper;

        /// <summary>
        /// Verizon Wireless Pdf Reader;
        /// </summary>
        private static VerizonReader verizonReader;

        [HttpGet]
        public void Get()
        {
            var rng = new Random();
        }

        [HttpPost]
        public void Post(PathValues pathValues)
        {
            document = new PdfDocument();
            try
            {
                logger.Trace($"start processing file:{pathValues.Path} at {DateTime.Now.ToString("yyyy/MM/dd-hh:mm:ss")}");
                var fileName = Path.GetFileName(pathValues.Path);

                helper = new ApplicationHelper();
                verizonReader = new VerizonReader(document, pathValues.Path);
                FileDto file = new FileDto();
                HeaderDto header = new HeaderDto();
                List<DetailDto> details = new List<DetailDto>();
                var preBuildText = helper.Prebuild(document, pathValues.Path);
                if (preBuildText.Any())
                {
                    header = verizonReader.GetHeaderValues(preBuildText);
                    file = verizonReader.GetFileValues(fileName);
                    var detailList = helper.ReadDetails(document, pathValues);
                    details = verizonReader.GetDetailValues(detailList, document, pathValues.Path);
                }

                verizonReader.PlainTextConstructor(file, header, details, pathValues.Path, pathValues.OutputPath, pathValues.ProcessedFilesPath, pathValues.FailedFiles);
                logger.Trace($"finished processing file:{pathValues.Path} at {DateTime.Now.ToString("yyyy/MM/dd-hh:mm:ss")}");
            }
            catch (Exception ex)
            {
                document.Close();
                var fileName = Path.GetFileName(pathValues.Path);

                if (ex.Message == Constants.ErrorMessage1 || ex.Message == Constants.ErrorMessage2 || ex.Message == Constants.ErrorMessage3)
                {
                    System.IO.File.Move(pathValues.Path, $@"{pathValues.CorruptedFiles}\{fileName}", true);
                    logger.Error($"{ex.Message} in file {pathValues.Path} \\n\\n {ex.StackTrace}");
                }
                else
                {
                    System.IO.File.Move(pathValues.Path, $@"{pathValues.FailedFiles}\{fileName}", true);
                    logger.Error($"{ex.Message} in file {pathValues.Path} \\n\\n {ex.StackTrace}");
                }
            }
        }
    }
}