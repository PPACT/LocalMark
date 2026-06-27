using LocalMark.Model;

namespace LocalMark.Helper;

public interface IExportService
{
    ExportFormat Format { get; }
    void Export(IEnumerable<SourceData> sources, IEnumerable<MarkResult> marks, string outputDir);
}
