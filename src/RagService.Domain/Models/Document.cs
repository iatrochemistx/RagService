namespace RagService.Domain.Models;

public class Document
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public float[]? Vector { get; set; }
}
