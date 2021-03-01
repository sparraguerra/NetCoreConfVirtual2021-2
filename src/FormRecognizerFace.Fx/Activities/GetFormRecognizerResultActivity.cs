using Azure.AI.FormRecognizer.Models;
using FormRecognizerFace.FormRecognizer;
using FormRecognizerFace.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Facturae;
using Facturae32 = Facturae.Facturae_v3_2;
using System.Globalization;

namespace FormRecognizerFace.Fx
{
    public static class RecognizedFormExtensions
    {
        public static string GetFieldValue(this RecognizedForm form, string fieldName) 
                => form.Fields.FirstOrDefault(f => f.Key == fieldName).Value?.ValueData?.Text;
    }

    public class GetFormRecognizerResultActivity
    {
        private readonly IFormRecognizerService formRecognizerService;
        private readonly IBlobStorageRepository blobStorageRepository;
        private readonly IConfiguration configuration;
        private readonly CultureInfo cultureInfo = new CultureInfo("es-ES");

        public GetFormRecognizerResultActivity(IFormRecognizerService formRecognizerService, 
                                               IBlobStorageRepository blobStorageRepository, 
                                               IConfiguration configuration)
        {
            this.formRecognizerService = formRecognizerService;
            this.blobStorageRepository = blobStorageRepository;
            this.configuration = configuration;
        }

        [FunctionName(nameof(GetFormRecognizerResultActivity))]
        public async Task<string> RunGetformActivity([ActivityTrigger] Uri blobSasUri)
        {
            var (containerName, blobName) = blobStorageRepository.GetContainerAndNameFromUri(blobSasUri.ToString());
            var metadata = await blobStorageRepository.GetMetadataAsync(containerName, blobName);
            var form = (await formRecognizerService.AnalyzeCustomFormFromUri(blobSasUri, metadata["modelId"]))?.FirstOrDefault();

            return MappingFrom(form).Serialize();
        }

        private Facturae32.Facturae MappingFrom(RecognizedForm form)
        {             
            var facturae = new Facturae32.Facturae
            {
                FileHeader = CreateFileHeader(form),
                Parties = CreateParties(form),
                Invoices = CreateInvoices(form)
            };

            return facturae;
        }
        
        #region Header

        private Facturae32.FileHeaderType CreateFileHeader(RecognizedForm form) 
        {
            var fileHeader = new Facturae32.FileHeaderType()
            {
                SchemaVersion = Facturae32.SchemaVersionType.Item32,
                InvoiceIssuerType = Facturae32.InvoiceIssuerTypeType.TE,
                ThirdParty = AddThirdParty(),
                Modality = Facturae32.ModalityType.I,
                Batch = new Facturae32.BatchType()
                {
                    BatchIdentifier = $"{form.GetFieldValue("SellerParty.TaxIdentification.TaxIdentificationNumber")}" +
                                  $"{form.GetFieldValue("Invoices.Invoice.InvoiceHeader.InvoiceNumber")}" +
                                  $"{form.GetFieldValue("Invoices.Invoice.InvoiceHeader.InvoiceSeriesCode")}",
                    InvoiceCurrencyCode = Facturae32.CurrencyCodeType.EUR,
                    InvoicesCount = 1,
                    TotalInvoicesAmount = new Facturae32.AmountType()
                    {
                        TotalAmount = Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.InvoiceTotal"), cultureInfo)
                    },
                    TotalOutstandingAmount = new Facturae32.AmountType()
                    {
                        TotalAmount = Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.InvoiceTotal"), cultureInfo)
                    },
                    TotalExecutableAmount = new Facturae32.AmountType()
                    {
                        TotalAmount = Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.TotalExecutableAmount"), cultureInfo)
                    }
                }
            };


            if (!string.IsNullOrWhiteSpace(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.GeneralSurcharges.Charge.ChargeReason")))
            {
                var chargeAmount = new Facturae32.DoubleSixDecimalType(Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.GeneralSurcharges.Charge.ChargeAmount"), cultureInfo));


                fileHeader.Batch.TotalInvoicesAmount.TotalAmount += chargeAmount;
                fileHeader.Batch.TotalOutstandingAmount.TotalAmount += chargeAmount;
            }
            return fileHeader;
        }

        #endregion

        #region Parties

        private Facturae32.ThirdPartyType AddThirdParty() => new Facturae32.ThirdPartyType
        {
            TaxIdentification = new Facturae32.TaxIdentificationType()
            {
                PersonTypeCode = Facturae32.PersonTypeCodeType.J,
                ResidenceTypeCode = Facturae32.ResidenceTypeCodeType.R,
                TaxIdentificationNumber = configuration["AppSettings:TaxIdentification:TaxIdentificationNumber"]
            },
            Item = new Facturae32.LegalEntityType()
            {
                CorporateName = configuration["AppSettings:LegalEntity:CorporateName"],
                TradeName = configuration["AppSettings:LegalEntity:CorporateName"],
                RegistrationData = new Facturae32.RegistrationDataType()
                {
                    Book = configuration["AppSettings:LegalEntity:RegistrationData:Book"],
                    Folio = configuration["AppSettings:LegalEntity:RegistrationData:Folio"],
                    Section = configuration["AppSettings:LegalEntity:RegistrationData:Section"],
                    Sheet = configuration["AppSettings:LegalEntity:RegistrationData:Sheet"],
                    Volume = configuration["AppSettings:LegalEntity:RegistrationData:Volume"],
                    RegisterOfCompaniesLocation = configuration["AppSettings:LegalEntity:RegistrationData:RegisterOfCompaniesLocation"]
                },
                Item = new Facturae32.AddressType()
                {
                    Address = configuration["AppSettings:LegalEntity:AddressInSpain:Address"],
                    CountryCode = (Facturae32.CountryType)Enum.Parse(typeof(Facturae32.CountryType), configuration["AppSettings:LegalEntity:AddressInSpain:CountryCode"]),
                    PostCode = configuration["AppSettings:LegalEntity:AddressInSpain:PostCode"],
                    Province = configuration["AppSettings:LegalEntity:AddressInSpain:Province"],
                    Town = configuration["AppSettings:LegalEntity:AddressInSpain:Town"]
                },
                ContactDetails = new Facturae32.ContactDetailsType()
                {
                    Telephone = configuration["AppSettings:LegalEntity:ContactDetails:Telephone"],
                    TeleFax = configuration["AppSettings:LegalEntity:ContactDetails:TeleFax"],
                    WebAddress = configuration["AppSettings:LegalEntity:ContactDetails:WebAddress"],
                    ElectronicMail = configuration["AppSettings:LegalEntity:ContactDetails:ElectronicMail"],
                    CnoCnae = configuration["AppSettings:LegalEntity:ContactDetails:CnoCnae"]
                }
            }
        };

        private Facturae32.PartiesType CreateParties(RecognizedForm form) => new Facturae32.PartiesType()
        {
            SellerParty = CreateSellerParty(form),
            BuyerParty = CreateBuyerParty(form)
        };

        private Facturae32.BusinessType CreateSellerParty(RecognizedForm form) => new Facturae32.BusinessType()
        {
            TaxIdentification = new Facturae32.TaxIdentificationType()
            {
                PersonTypeCode = Facturae32.PersonTypeCodeType.J,
                ResidenceTypeCode = Facturae32.ResidenceTypeCodeType.R,
                TaxIdentificationNumber = $"{form.GetFieldValue("SellerParty.TaxIdentification.TaxIdentificationNumber")}"
            },
            Item = new Facturae32.LegalEntityType()
            {
                CorporateName = $"{form.GetFieldValue("SellerParty.LegalEntity.CorporateName")}",
                RegistrationData = new Facturae32.RegistrationDataType()
                {
                    Book = $"{form.GetFieldValue("SellerParty.LegalEntity.RegistrationData.Book")}",
                    RegisterOfCompaniesLocation = $"{form.GetFieldValue("SellerParty.LegalEntity.RegistrationData.RegisterOfCompaniesLocation")}",
                    Sheet = $"{form.GetFieldValue("SellerParty.LegalEntity.RegistrationData.Sheet")}",
                    Folio = $"{form.GetFieldValue("SellerParty.LegalEntity.RegistrationData.Folio")}",
                    Section = $"{form.GetFieldValue("SellerParty.LegalEntity.RegistrationData.Section")}",
                    Volume = $"{form.GetFieldValue("SellerParty.LegalEntity.RegistrationData.Volume")}",
                    AdditionalRegistrationData = $"{form.GetFieldValue("SellerParty.LegalEntity.RegistrationData.AdditionalRegistrationData")}"
                },
                Item = new Facturae32.AddressType()
                {
                    Address = $"{form.GetFieldValue("SellerParty.LegalEntity.AddressInSpain.Address")}",
                    CountryCode = Facturae32.CountryType.ESP,
                    PostCode = $"{form.GetFieldValue("SellerParty.LegalEntity.AddressInSpain.PostCode")}",
                    Province = $"{form.GetFieldValue("SellerParty.LegalEntity.AddressInSpain.Province")}",
                    Town = $"{form.GetFieldValue("SellerParty.LegalEntity.AddressInSpain.Town")}"
                }
            }
        };

        private Facturae32.BusinessType CreateBuyerParty(RecognizedForm form) => new Facturae32.BusinessType()
        {
            PartyIdentification = $"{form.GetFieldValue("BuyerParty.PartyIdentification")}",
            TaxIdentification = new Facturae32.TaxIdentificationType()
            {
                PersonTypeCode = Facturae32.PersonTypeCodeType.F,
                ResidenceTypeCode = Facturae32.ResidenceTypeCodeType.R,
                TaxIdentificationNumber = $"{form.GetFieldValue("BuyerParty.TaxIdentification.TaxIdentificationNumber")}"
            },
            Item = new Facturae32.IndividualType()
            { 
                Name = $"{form.GetFieldValue("BuyerParty.Individual.Name")}",
                FirstSurname = $"{form.GetFieldValue("BuyerParty.Individual.FirstSurname")}",
                SecondSurname  = $"{form.GetFieldValue("BuyerParty.Individual.SecondSurname")}",
                Item = new Facturae32.AddressType()
                {
                    Address = $"{form.GetFieldValue("BuyerParty.Individual.AddressInSpain.Address")}",
                    CountryCode = Facturae32.CountryType.ESP,
                    PostCode = $"{form.GetFieldValue("BuyerParty.Individual.AddressInSpain.PostCode")}",
                    Province = $"{form.GetFieldValue("BuyerParty.Individual.AddressInSpain.Province")}",
                    Town = $"{form.GetFieldValue("BuyerParty.Individual.AddressInSpain.Town")}"
                }
            }
        };

        #endregion

        #region Invoices

        private Facturae32.InvoiceType[] CreateInvoices(RecognizedForm form) => new Facturae32.InvoiceType[]
        {
            new Facturae32.InvoiceType()
            {
                InvoiceHeader = CreateInvoiceHeader(form),
                InvoiceIssueData = CreateInvoiceIssueData(form),
                TaxesOutputs = CreateTaxesOutputs(form),
                InvoiceTotals = CreateInvoiceTotals(form),
                Items = CreateInvoiceLines(form),
                PaymentDetails = CreatePaymentDetails(form)
            }
        };

        private Facturae32.InvoiceHeaderType CreateInvoiceHeader(RecognizedForm form) => new Facturae32.InvoiceHeaderType()
        {
            InvoiceSeriesCode = $"{form.GetFieldValue("Invoices.Invoice.InvoiceHeader.InvoiceSeriesCode")}",
            InvoiceNumber = $"{form.GetFieldValue("Invoices.Invoice.InvoiceHeader.InvoiceNumber")}",
            InvoiceDocumentType = Facturae32.InvoiceDocumentTypeType.FC,
            InvoiceClass = Facturae32.InvoiceClassType.OO
        };

        private Facturae32.InvoiceIssueDataType CreateInvoiceIssueData(RecognizedForm form) => new Facturae32.InvoiceIssueDataType()
        {
            IssueDate = DateTime.ParseExact($"{form.GetFieldValue("Invoices.Invoice.InvoiceIssueData.IssueDate")}", "dd/MM/yyyy", cultureInfo),
            InvoiceCurrencyCode = Facturae32.CurrencyCodeType.EUR,
            TaxCurrencyCode = Facturae32.CurrencyCodeType.EUR,
            LanguageName = Facturae32.LanguageCodeType.es
        };

        private Facturae32.TaxOutputType[] CreateTaxesOutputs(RecognizedForm form) => new Facturae32.TaxOutputType[]
            {
                new Facturae32.TaxOutputType()
                {
                    TaxTypeCode = Facturae32.TaxTypeCodeType.Item01,
                    TaxRate = new Facturae32.DoubleTwoDecimalType(
                                    Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.TaxesOutputs.Tax.TaxRate").Substring(1, 5), CultureInfo.InvariantCulture)
                                ),
                    TaxableBase = new Facturae32.AmountType()
                    {
                        TotalAmount  = new Facturae32.DoubleTwoDecimalType(
                                         Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.TaxesOutputs.Tax.TaxableBase.TotalAmount"), cultureInfo)
                                        )
                    },
                    TaxAmount = new Facturae32.AmountType()
                    {
                        TotalAmount  = new Facturae32.DoubleTwoDecimalType(
                                        Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.TaxesOutputs.Tax.TaxAmount.TotalAmount"), cultureInfo)
                                        )
                    }
                }
            };

        private Facturae32.InvoiceTotalsType CreateInvoiceTotals(RecognizedForm form)            
        {
            var totals = new Facturae32.InvoiceTotalsType()
            {
                TotalGrossAmount = new Facturae32.DoubleTwoDecimalType(Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.TaxesOutputs.Tax.TaxableBase.TotalAmount"), cultureInfo)),
                TotalGrossAmountBeforeTaxes = new Facturae32.DoubleTwoDecimalType(Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.TaxesOutputs.Tax.TaxableBase.TotalAmount"), cultureInfo)),
                TotalTaxOutputs = new Facturae32.DoubleTwoDecimalType(Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.TaxesOutputs.Tax.TaxAmount.TotalAmount"), cultureInfo)),
                TotalOutstandingAmount = new Facturae32.DoubleTwoDecimalType(Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.InvoiceTotal"), cultureInfo)),
                InvoiceTotal = new Facturae32.DoubleTwoDecimalType(Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.InvoiceTotal"), cultureInfo)),
                TotalExecutableAmount = new Facturae32.DoubleTwoDecimalType(Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.TotalExecutableAmount"), cultureInfo)),

            };

            if (!string.IsNullOrWhiteSpace(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.GeneralSurcharges.Charge.ChargeReason")))
            {
                totals.GeneralSurcharges = new[]
                {
                    new Facturae32.ChargeType()
                    {
                        ChargeReason = form.GetFieldValue("Invoices.Invoice.InvoiceTotals.GeneralSurcharges.Charge.ChargeReason"),
                        ChargeAmount = new Facturae32.DoubleSixDecimalType(Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.InvoiceTotals.GeneralSurcharges.Charge.ChargeAmount"), cultureInfo)),
                    }
                };
                totals.TotalGeneralSurcharges = totals.GeneralSurcharges.Sum(x => x.ChargeAmount);
                totals.TotalGeneralSurchargesSpecified = true;
                totals.TotalGrossAmountBeforeTaxes += totals.TotalGeneralSurcharges;
                totals.InvoiceTotal += totals.TotalGeneralSurcharges;
                totals.TotalOutstandingAmount += totals.TotalGeneralSurcharges;
            }
            return totals;
        }

        private Facturae32.InvoiceLineType[] CreateInvoiceLines(RecognizedForm form)
        {
            var columns = 6;
            var rows = form.Pages[0].Tables[0].RowCount;
            var table = FormatCellsToBidimensionalArray(form.Pages[0].Tables[0], rows , columns);
            var lines = new Facturae32.InvoiceLineType[rows - 1];
            var taxRate = Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.TaxesOutputs.Tax.TaxRate").Substring(1, 5), CultureInfo.InvariantCulture);
             
            for (int row = 1;row < rows;row++)
            {
                lines[row - 1] = new Facturae32.InvoiceLineType();
 
                lines[row - 1].ItemDescription = table[row, 1];
                lines[row - 1].Quantity = Convert.ToDouble(!string.IsNullOrWhiteSpace(table[row, 2]) ? table[row, 2] : "0", cultureInfo);
                lines[row - 1].GrossAmount = lines[row - 1].TotalCost = new Facturae32.DoubleSixDecimalType(Convert.ToDouble(table[row, 3], cultureInfo)); 
                if (lines[row - 1].Quantity != 0)
                {
                    double unitPrice = lines[row - 1].TotalCost / lines[row - 1].Quantity;
                    lines[row - 1].UnitPriceWithoutTax = new Facturae32.DoubleSixDecimalType(Math.Round(unitPrice, 2));
                }

                lines[row - 1].TaxesOutputs = new[]
                {
                    new Facturae32.InvoiceLineTypeTax()
                    {
                        TaxTypeCode = Facturae32.TaxTypeCodeType.Item01,
                        TaxRate = new Facturae32.DoubleTwoDecimalType(taxRate),
                        TaxableBase = new Facturae32.AmountType()
                        {
                            TotalAmount = new Facturae32.DoubleTwoDecimalType(Convert.ToDouble(table[row, 3], cultureInfo))
                        },
                        TaxAmount = new Facturae32.AmountType()
                        {
                            TotalAmount = new Facturae32.DoubleTwoDecimalType(Convert.ToDouble(table[row, 4], cultureInfo))
                        }
                    }
                };
            }
            return lines;
        }

        private string[,] FormatCellsToBidimensionalArray(FormTable table, int rows, int columns)
        {
           

            int index = 0;
            string[,] twoDimensionalArray = new string[rows, columns];

            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < columns; y++)
                {
                    twoDimensionalArray[x, y] = table.Cells[index].Text;
                    index++;
                }
            }

            return twoDimensionalArray;
        }
         

        private Facturae32.InstallmentType[] CreatePaymentDetails(RecognizedForm form) => new Facturae32.InstallmentType[]
        {
            new Facturae32.InstallmentType()
            {
                InstallmentDueDate =  DateTime.ParseExact($"{form.GetFieldValue("Invoices.Invoice.PaymentDetails.Installment.InstallmentDueDate")}", "dd/MM/yyyy", cultureInfo),
                InstallmentAmount = new Facturae32.DoubleTwoDecimalType(Convert.ToDouble(form.GetFieldValue("Invoices.Invoice.PaymentDetails.Installment.InstallmentAmount"), cultureInfo)),            }
        };

        #endregion
    }
}