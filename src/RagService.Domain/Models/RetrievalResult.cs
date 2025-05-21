namespace RagService.Domain.Models;

public class RetrievalResult
{
    public List<string> Documents { get; set; } = new();
    public string? Response { get; set; }
}
