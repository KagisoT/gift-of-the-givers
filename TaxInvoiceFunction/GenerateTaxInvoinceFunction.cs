using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;

namespace TaxInvoiceFunction
{
    internal class GenerateTaxInvoinceFunction
    {
    }
}using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TaxInvoiceFunction
{
    public class GenerateTaxInvoiceFunction
    {
        private readonly HttpClient _httpClient;

        public GenerateTaxInvoiceFunction(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        // ============================================================
        // GET:
        // http://localhost:7282/api/taxinvoice/{id}
        //
        // Generates and returns a PDF tax invoice.
        // ============================================================

        [Function("GenerateTaxInvoice")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "taxinvoice/{id:int}")]
            HttpRequestData req,
            int id)
        {
            try
            {
                if (id <= 0)
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.BadRequest,
                        "A valid donation ID is required.");
                }

                // ----------------------------------------------------
                // Get donation information.
                //
                // The URL below should point to your MVC/API endpoint.
                // Change this if your donation API uses another URL.
                // ----------------------------------------------------

                var donationApiUrl =
                    $"http://localhost:5000/api/donations/{id}";

                var response =
                    await _httpClient.GetAsync(donationApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.NotFound,
                        "The requested donation could not be found.");
                }

                var json =
                    await response.Content.ReadAsStringAsync();

                var donation =
                    JsonSerializer.Deserialize<DonationDto>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (donation == null)
                {
                    return await CreateErrorResponse(
                        req,
                        HttpStatusCode.NotFound,
                        "Donation information could not be loaded.");
                }

                // ----------------------------------------------------
                // Generate tax certificate reference if one does not
                // already exist.
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                    donation.TaxCertificateReference))
                {
                    donation.TaxCertificateReference =
                        GenerateTaxCertificateReference();
                }

                // ----------------------------------------------------
                // Generate PDF
                // ----------------------------------------------------

                var pdfBytes =
                    GenerateTaxInvoicePdf(donation);

                var result =
                    req.CreateResponse(HttpStatusCode.OK);

                result.Headers.Add(
                    "Content-Type",
                    "application/pdf");

                result.Headers.Add(
                    "Content-Disposition",
                    $"inline; filename=\"GiftOfTheGivers-Tax-Invoice-{donation.DonationId}.pdf\"");

                await result.Body.WriteAsync(pdfBytes);

                return result;
            }
            catch (HttpRequestException)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadGateway,
                    "The donation service could not be reached.");
            }
            catch (Exception ex)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.InternalServerError,
                    $"Unable to generate tax invoice: {ex.Message}");
            }
        }

        // ============================================================
        // GET:
        // http://localhost:7282/api/taxinvoice/{id}/reference
        //
        // Returns only the tax certificate reference.
        // ============================================================

        [Function("GenerateTaxCertificateReference")]
        public async Task<HttpResponseData> GenerateReference(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "taxinvoice/{id:int}/reference")]
            HttpRequestData req,
            int id)
        {
            if (id <= 0)
            {
                return await CreateErrorResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "A valid donation ID is required.");
            }

            var reference =
                GenerateTaxCertificateReference();

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            response.Headers.Add(
                "Content-Type",
                "application/json");

            await response.WriteStringAsync(
                JsonSerializer.Serialize(
                    new
                    {
                        donationId = id,
                        taxCertificateReference = reference
                    }));

            return response;
        }

        // ============================================================
        // PDF GENERATION
        // ============================================================

        private static byte[] GenerateTaxInvoicePdf(
            DonationDto donation)
        {
            QuestPDF.Settings.License =
                LicenseType.Community;

            var document =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);

                        page.Margin(40);

                        page.DefaultTextStyle(
                            x => x.FontSize(10));

                        // ------------------------------------------------
                        // HEADER
                        // ------------------------------------------------

                        page.Header()
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Column(column =>
                                    {
                                        column.Item()
                                            .Text(
                                                "GIFT OF THE GIVERS")
                                            .FontSize(24)
                                            .Bold()
                                            .FontColor(
                                                Colors.Red.Darken2);

                                        column.Item()
                                            .Text(
                                                "Foundation")
                                            .FontSize(12)
                                            .SemiBold();

                                        column.Item()
                                            .Text(
                                                "Humanitarian Relief & Development")
                                            .FontSize(9)
                                            .FontColor(
                                                Colors.Grey.Darken1);
                                    });

                                row.ConstantItem(170)
                                    .AlignRight()
                                    .Column(column =>
                                    {
                                        column.Item()
                                            .Text(
                                                "TAX INVOICE")
                                            .FontSize(18)
                                            .Bold()
                                            .FontColor(
                                                Colors.Red.Darken2);

                                        column.Item()
                                            .Text(
                                                $"Invoice #{donation.DonationId}")
                                            .FontSize(10);

                                        column.Item()
                                            .Text(
                                                donation.DonationDate
                                                    .ToString("dd MMMM yyyy"))
                                            .FontSize(10);
                                    });
                            });

                        // ------------------------------------------------
                        // CONTENT
                        // ------------------------------------------------

                        page.Content()
                            .PaddingTop(30)
                            .Column(column =>
                            {
                                // ----------------------------------------
                                // Organisation
                                // ----------------------------------------

                                column.Item()
                                    .Background(
                                        Colors.Grey.Lighten4)
                                    .Padding(15)
                                    .Column(info =>
                                    {
                                        info.Item()
                                            .Text(
                                                "Gift of the Givers Foundation")
                                            .Bold()
                                            .FontSize(12);

                                        info.Item()
                                            .Text(
                                                "South Africa");

                                        info.Item()
                                            .Text(
                                                "Humanitarian Relief Organisation");

                                        info.Item()
                                            .Text(
                                                "Donation Tax Certificate");
                                    });

                                column.Item()
                                    .PaddingTop(25);

                                // ----------------------------------------
                                // Donation details
                                // ----------------------------------------

                                column.Item()
                                    .Text("DONATION DETAILS")
                                    .FontSize(13)
                                    .Bold()
                                    .FontColor(
                                        Colors.Red.Darken2);

                                column.Item()
                                    .PaddingTop(10)
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(
                                            columns =>
                                            {
                                                columns.ConstantColumn(180);
                                                columns.RelativeColumn();
                                            });

                                        AddTableRow(
                                            table,
                                            "Donation Reference",
                                            $"DON-{donation.DonationId:D6}");

                                        AddTableRow(
                                            table,
                                            "Tax Certificate Reference",
                                            donation.TaxCertificateReference
                                                ?? "N/A");

                                        AddTableRow(
                                            table,
                                            "Donation Date",
                                            donation.DonationDate
                                                .ToString("dd MMMM yyyy HH:mm"));

                                        AddTableRow(
                                            table,
                                            "Donation Type",
                                            donation.DonationType);

                                        AddTableRow(
                                            table,
                                            "Currency",
                                            donation.Currency);

                                        AddTableRow(
                                            table,
                                            "Payment Amount",
                                            $"{donation.Currency} {donation.Amount:N2}");
                                    });

                                column.Item()
                                    .PaddingTop(30);

                                // ----------------------------------------
                                // Donor
                                // ----------------------------------------

                                column.Item()
                                    .Text("DONOR INFORMATION")
                                    .FontSize(13)
                                    .Bold()
                                    .FontColor(
                                        Colors.Red.Darken2);

                                column.Item()
                                    .PaddingTop(10)
                                    .Background(
                                        Colors.Grey.Lighten4)
                                    .Padding(15)
                                    .Column(donor =>
                                    {
                                        donor.Item()
                                            .Text(
                                                donation.IsAnonymous
                                                    ? "Anonymous Donor"
                                                    : (
                                                        string.IsNullOrWhiteSpace(
                                                            donation.DonorName)
                                                            ? "Registered Donor"
                                                            : donation.DonorName))
                                            .Bold()
                                            .FontSize(12);

                                        if (!donation.IsAnonymous &&
                                            !string.IsNullOrWhiteSpace(
                                                donation.DonorEmail))
                                        {
                                            donor.Item()
                                                .Text(
                                                    donation.DonorEmail);
                                        }
                                    });

                                column.Item()
                                    .PaddingTop(30);

                                // ----------------------------------------
                                // Total
                                // ----------------------------------------

                                column.Item()
                                    .AlignRight()
                                    .Background(
                                        Colors.Red.Darken2)
                                    .Padding(15)
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text("TOTAL DONATION")
                                            .Bold()
                                            .FontColor(
                                                Colors.White);

                                        row.ConstantItem(150)
                                            .AlignRight()
                                            .Text(
                                                $"{donation.Currency} {donation.Amount:N2}")
                                            .Bold()
                                            .FontSize(16)
                                            .FontColor(
                                                Colors.White);
                                    });

                                column.Item()
                                    .PaddingTop(30);

                                // ----------------------------------------
                                // Notice
                                // ----------------------------------------

                                column.Item()
                                    .Background(
                                        Colors.Grey.Lighten4)
                                    .Padding(15)
                                    .Column(note =>
                                    {
                                        note.Item()
                                            .Text(
                                                "Donation Certificate Notice")
                                            .Bold();

                                        note.Item()
                                            .PaddingTop(5)
                                            .Text(
                                                "This document is a generated "
                                                + "donation tax certificate "
                                                + "placeholder for the Gift of "
                                                + "the Givers application. "
                                                + "The final tax treatment and "
                                                + "official certificate details "
                                                + "must be verified by the "
                                                + "organisation.");
                                    });
                            });

                        // ------------------------------------------------
                        // FOOTER
                        // ------------------------------------------------

                        page.Footer()
                            .AlignCenter()
                            .Column(column =>
                            {
                                column.Item()
                                    .Text(
                                        "Thank you for supporting humanitarian relief.")
                                    .Bold()
                                    .FontColor(
                                        Colors.Red.Darken2);

                                column.Item()
                                    .Text(
                                        "Gift of the Givers Foundation")
                                    .FontSize(8)
                                    .FontColor(
                                        Colors.Grey.Darken1);

                                column.Item()
                                    .Text(
                                        $"Generated: {DateTime.UtcNow:dd MMMM yyyy HH:mm} UTC")
                                    .FontSize(8)
                                    .FontColor(
                                        Colors.Grey.Darken1);
                            });
                    });
                });

            return document.GeneratePdf();
        }

        // ============================================================
        // TABLE HELPER
        // ============================================================

        private static void AddTableRow(
            TableDescriptor table,
            string label,
            object value)
        {
            table.Cell()
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(8)
                .Text(label)
                .SemiBold();

            table.Cell()
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(8)
                .Text(value?.ToString() ?? "N/A");
        }

        // ============================================================
        // TAX REFERENCE
        // ============================================================

        private static string GenerateTaxCertificateReference()
        {
            return
                $"TAX-{DateTime.UtcNow:yyyyMMddHHmmss}-" +
                $"{Guid.NewGuid():N}"
                    .Substring(0, 25)
                    .ToUpperInvariant();
        }

        // ============================================================
        // ERROR RESPONSE
        // ============================================================

        private static async Task<HttpResponseData>
            CreateErrorResponse(
                HttpRequestData request,
                HttpStatusCode statusCode,
                string message)
        {
            var response =
                request.CreateResponse(statusCode);

            response.Headers.Add(
                "Content-Type",
                "application/json");

            await response.WriteStringAsync(
                JsonSerializer.Serialize(
                    new
                    {
                        success = false,
                        message
                    }));

            return response;
        }
    }

    // ================================================================
    // DONATION DTO
    // ================================================================

    public class DonationDto
    {
        public int DonationId { get; set; }

        public string? UserId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "ZAR";

        public string DonationType { get; set; } = "OneTime";

        public DateTime DonationDate { get; set; }

        public string? TaxCertificateReference { get; set; }

        public bool IsAnonymous { get; set; }

        public string? DonorName { get; set; }

        public string? DonorEmail { get; set; }
    }
}
