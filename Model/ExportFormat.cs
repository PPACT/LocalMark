namespace LocalMark.Model;

public enum ExportFormat
{
    LocalMark,  // 原有 JSON（含统计报告）
    COCO,       // MS COCO JSON
    YOLO,       // YOLO txt（归一化坐标）
    VOC         // Pascal VOC XML
}
