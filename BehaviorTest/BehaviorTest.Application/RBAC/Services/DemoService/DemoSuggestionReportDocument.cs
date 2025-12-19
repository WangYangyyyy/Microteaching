// Services/DemoSuggestionReportDocument.cs

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BehaviorTest.Application.RBAC.Services.DemoService
{
    public class DemoSuggestionReportDocument : IDocument
    {
        public string Title { get; init; } = "";
        public string BodyMarkdown { get; init; } = "";

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                
                page.DefaultTextStyle(x => x
                    .FontFamily("SimSun")   // 这里换成你实际的字体名
                    .FontSize(12)
                );

                page.Header().Column(col =>
                {
                    col.Item().Text(Title)
                        .FontSize(20)
                        .Bold()
                        .AlignCenter();

                    col.Item().Text($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9)
                        .AlignRight()
                        .FontColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingTop(15).Element(ComposeContent);

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("第 ").FontSize(9);
                    x.CurrentPageNumber().FontSize(9);
                    x.Span(" / ").FontSize(9);
                    x.TotalPages().FontSize(9);
                    x.Span(" 页").FontSize(9);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            var lines = BodyMarkdown
                .Replace("\r\n", "\n")
                .Split('\n');

            container.Column(col =>
            {
                foreach (var raw in lines)
                {
                    var line = raw.TrimEnd();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        col.Item().Height(5);
                        continue;
                    }

                    // H1
                    if (line.StartsWith("# ") && !line.StartsWith("## "))
                    {
                        col.Item().PaddingTop(10).Text(line[2..].Trim())
                            .FontSize(16)
                            .Bold();
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        continue;
                    }

                    // H2
                    if (line.StartsWith("## ") && !line.StartsWith("### "))
                    {
                        col.Item().PaddingTop(8).Text(line[3..].Trim())
                            .FontSize(14)
                            .Bold();
                        continue;
                    }

                    // H3
                    if (line.StartsWith("### "))
                    {
                        col.Item().PaddingTop(6).Text(line[4..].Trim())
                            .FontSize(12)
                            .Bold()
                            .FontColor(Colors.Grey.Darken2);
                        continue;
                    }

                    // 勾选清单 - [ ]
                    if (line.StartsWith("- [ ]") || line.StartsWith("* [ ]"))
                    {
                        var text = line[(line.IndexOf("]") + 1)..].Trim();

                        col.Item().Row(row =>
                        {
                            row.ConstantItem(12).Text("☐")
                                .FontSize(11);
                            row.RelativeItem().Text(text)
                                .FontSize(11)
                                .LineHeight(1.4f);
                        });
                        continue;
                    }

                    // 普通无序列表
                    if (line.StartsWith("- ") || line.StartsWith("* "))
                    {
                        var text = line[2..].Trim();
                        col.Item().Row(row =>
                        {
                            row.ConstantItem(10).Text("•")
                                .FontSize(11);
                            row.RelativeItem().Text(text)
                                .FontSize(11)
                                .LineHeight(1.4f);
                        });
                        continue;
                    }

                    // 普通段落
                    col.Item().Text(line)
                        .FontSize(11)
                        .LineHeight(1.4f);
                }
            });
        }
    }
}
