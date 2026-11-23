namespace PublicSafetyHub.Models.Documents;

public class Document
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public string Body { get; set; }

    public string Url { get; set; }
}
