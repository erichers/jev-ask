using JevAsk.Core.Ask;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JevAsk.Api.Brief;

public static class PdfBrief
{
    static PdfBrief()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Render(AskResponse response)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(style => style.FontSize(11).FontColor(Colors.BlueGrey.Darken4));
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text("Jev Ask").FontSize(22).SemiBold().FontColor("#0b1220");
                    column.Item().Text(response.Question).FontSize(13);
                    column.Item().Text($"{response.Probability * 100:0.0}%")
                        .FontSize(36)
                        .SemiBold()
                        .FontColor("#0f9f82");
                    column.Item().Text(
                        $"Range {response.BandLow * 100:0.0}% to {response.BandHigh * 100:0.0}%. " +
                        $"Spot {response.Spot:0.00} on {response.SpotDate:yyyy-MM-dd}. " +
                        $"Target {response.TargetPrice:0.00}.");
                    column.Item().Text(response.Reasoning);
                    column.Item().Text("Steps").SemiBold();
                    foreach (var step in response.Steps)
                    {
                        column.Item().Text($"{step.N}. {step.Title}. {step.Detail}");
                    }

                    column.Item().PaddingTop(8).Text(response.Disclaimer).FontSize(10);
                    column.Item().Text($"Parser: {response.Parser}. Data: {response.DataFreshness} ({response.DataOrigin}).").FontSize(9);
                    column.Item().Text("by Ulric studio").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }
}
