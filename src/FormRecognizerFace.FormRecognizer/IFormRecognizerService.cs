using Azure.AI.FormRecognizer.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FormRecognizerFace.FormRecognizer
{
    public interface IFormRecognizerService
    {
        public Task<FormPageCollection> AnalyzeFormFromStream(Stream form);

        public Task<RecognizedFormCollection> AnalyzeCustomFormFromStream(Stream form, string modelId);

        public Task<RecognizedFormCollection> AnalyzeCustomFormFromUri(Uri formUri, string modelId);

        public Task<RecognizedFormCollection> AnalyzeReceiptFromStream(Stream receipt);
    }
}
