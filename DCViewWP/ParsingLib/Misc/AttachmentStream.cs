using System.IO;
namespace DCView.Misc
{
    public class AttachmentStream
    {
        public string Filename { get; set; }
        public Stream Stream { get; set; }
        public string ContentType { get; set; }

        public AttachmentStream()
        {
            ContentType = "application/octet-stream";
        }
    }
}