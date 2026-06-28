using LocalMark.Model;

namespace LocalMark.Helper;

public class ImportResult
{
    public SourceData Source { get; set; } = new();
    public List<AnnotationBox> Boxes { get; set; } = [];
}

public interface IImportService
{
    List<ImportResult> Import(string datasetPath);
}

