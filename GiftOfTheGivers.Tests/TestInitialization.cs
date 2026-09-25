using System.Runtime.CompilerServices;
using QuestPDF.Infrastructure;

internal static class TestInitialization
{
    [ModuleInitializer]
    internal static void InitializeQuestPdfLicense()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
}