using System;
using System.ComponentModel;

namespace CreateZugferd2_0_PdfDocument
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                CreateZugferd2_0_PdfDocument("blank.pdf", "zugferd-invoice.xml", "zugferd2_0_pdfFile.pdf");
            }
            catch (LicenseException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Creates the ZUGFeRD 2.0 PDF document.
        /// </summary>
        /// <param name="sourcePdfFilePath">Path to a source PDF document.</param>
        /// <param name="zugferdIvoiceFilePath">Path to a ZUGFeRD invoice.</param>
        /// <param name="destPdfFilePath">Path to a ZUGFeRD PDF document.</param>
        static void CreateZugferd2_0_PdfDocument(
            string sourcePdfFilePath,
            string zugferdIvoiceFilePath,
            string destPdfFilePath)
        {
            // copy source PDF document to a destination PDF document
            System.IO.File.Copy(sourcePdfFilePath, destPdfFilePath, true);

            // embed the ZUGFeRD invoice into PDF document
            EmbedZugferdIvoiceIntoPdfDocument(destPdfFilePath, zugferdIvoiceFilePath);

            // convert PDF document to PDF/A-3b document
            if (!ConvertPdfDocumentToPdfA3bDocument(destPdfFilePath))
                return;

            // update metadata in PDF document
            UpdatePdfDocumentMetadata(destPdfFilePath, zugferdIvoiceFilePath);
        }

        /// <summary>
        /// Embeds a ZUGFeRD invoice to a PDF document.
        /// </summary>
        static void EmbedZugferdIvoiceIntoPdfDocument(
            string zugferdPdfFilePath,
            string zugferdIvoiceFilePath)
        {
            // get name of file with ZUGFeRD invoice
            string zugferdIvoiceFileName = System.IO.Path.GetFileName(zugferdIvoiceFilePath);

            // open destination PDF document
            using (Vintasoft.Imaging.Pdf.PdfDocument pdfDoc = new Vintasoft.Imaging.Pdf.PdfDocument(zugferdPdfFilePath))
            {
                // if PDF document does NOT have embedded files
                if (pdfDoc.EmbeddedFiles == null)
                {
                    // creates a dictionary for embedded files
                    pdfDoc.EmbeddedFiles = new Vintasoft.Imaging.Pdf.Tree.PdfEmbeddedFileSpecificationDictionary(pdfDoc);
                }

                // create a PDF embedded file, which contains ZUGFeRD invoice
                Vintasoft.Imaging.Pdf.Tree.PdfEmbeddedFile embeddedFile = new Vintasoft.Imaging.Pdf.Tree.PdfEmbeddedFile(pdfDoc, zugferdIvoiceFilePath);
                // specify the subtype of PDF embedded file
                embeddedFile.Subtype = "text/xml";
                // specify the modification date of PDF embedded file
                embeddedFile.ModifyDate = DateTime.Now;

                // create the file specification for embedded file
                Vintasoft.Imaging.Pdf.Tree.PdfEmbeddedFileSpecification embeddedFileSpecification = new Vintasoft.Imaging.Pdf.Tree.PdfEmbeddedFileSpecification(zugferdIvoiceFileName, embeddedFile);
                // describe the embedded file
                embeddedFileSpecification.Description = zugferdIvoiceFileName;

                // get the file specification dictionary
                Vintasoft.Imaging.Pdf.BasicTypes.PdfDictionary fileSpecificationDictionary = embeddedFileSpecification.BasicObject as Vintasoft.Imaging.Pdf.BasicTypes.PdfDictionary;
                // add a Unicode text string that provides file specification to the file specification dictionary
                fileSpecificationDictionary.Add("UF", new Vintasoft.Imaging.Pdf.BasicTypes.PdfString(zugferdIvoiceFileName));

                // add the embedded file to the PDF document
                pdfDoc.EmbeddedFiles.Add(zugferdIvoiceFileName, embeddedFileSpecification);

                // get a dictionary containing a subset of the keys F, UF, DOS, Mac, and Unix from the file specification dictionary
                Vintasoft.Imaging.Pdf.BasicTypes.PdfDictionary efDictionary = fileSpecificationDictionary["EF"] as Vintasoft.Imaging.Pdf.BasicTypes.PdfDictionary;
                // add information about embedded file into the EF dictionary
                efDictionary.Add("UF", embeddedFile.IndirectReference);

                // save changes in PDF document
                pdfDoc.SaveChanges();
            }
        }

        /// <summary>
        /// Converts a PDF document to a PDF/A-3b document.
        /// </summary>
        static bool ConvertPdfDocumentToPdfA3bDocument(string filePath)
        {
            try
            {
                // create PDF/A-3b converter
                Vintasoft.Imaging.Pdf.Processing.PdfA.PdfAConverter converter = new Vintasoft.Imaging.Pdf.Processing.PdfA.PdfA3bConverter();
                converter.LzwFixupCompression = Vintasoft.Imaging.Pdf.PdfCompression.Zip;

                Vintasoft.Imaging.Processing.ProcessingState processingState = new Vintasoft.Imaging.Processing.ProcessingState();
                processingState.Progress += ProcessingState_Progress;

                // create temporary PDF file
                string tempPdfFile = System.IO.Path.GetTempFileName();

                // convert source PDF document and save to the temporary PDF file
                Vintasoft.Imaging.Processing.ConversionProfileResult conversionProfileResult = converter.Convert(filePath, tempPdfFile, processingState);

                // if PDF document is successfully converted to a PDF/A-3b
                if (conversionProfileResult.IsSuccessful)
                {
                    // copy the temporary PDF file over the source PDF file
                    System.IO.File.Copy(tempPdfFile, filePath, true);
                    return true;
                }
                else
                {
                    // for each conversion error
                    foreach (Vintasoft.Imaging.Processing.IProcessingCommandInfo cmd in conversionProfileResult.ActivatedTriggers.Keys)
                    {
                        // output information about error
                        Console.WriteLine(string.Format("Error: {0} ({1} matches)", cmd, conversionProfileResult.ActivatedTriggers[cmd].Count));
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error: {0}", ex.Message));
                return false;
            }
        }

        private static void ProcessingState_Progress(object sender, Vintasoft.Imaging.ProgressEventArgs e)
        {
        }

        /// <summary>
        /// Updates metadata in PDF document.
        /// </summary>
        static void UpdatePdfDocumentMetadata(string pdfFilePath, string zugferdIvoiceFilePath)
        {
            // open PDF document
            using (Vintasoft.Imaging.Pdf.PdfDocument pdfDoc = new Vintasoft.Imaging.Pdf.PdfDocument(pdfFilePath))
            {
                // get name of file with ZUGFeRD invoice
                string zugferdIvoiceFileName = System.IO.Path.GetFileName(zugferdIvoiceFilePath);

                // get PDF document metadata
                string origMeta = System.Text.Encoding.UTF8.GetString(pdfDoc.Metadata);

                // update metadata
                origMeta = origMeta.Replace("xmlns:dc=", " " + Properties.Resources.xmlPart1 + " " + Properties.Resources.xmlPart3 + " xmlns:dc=");
                origMeta = origMeta.Replace("</dc:creator>", " </dc:creator> " + Properties.Resources.xmlPart2);
                origMeta = origMeta.Replace("%filename%", zugferdIvoiceFileName);

                // save metadata back to PDF document
                pdfDoc.Metadata = System.Text.Encoding.UTF8.GetBytes(origMeta);

                // save changes in PDF document
                pdfDoc.SaveChanges();
            }
        }

    }
}
