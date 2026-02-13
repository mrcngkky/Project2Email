using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project2EmailNight.Entities
{
    public class MessageAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        // Foreign Key
        public int MessageId { get; set; }

        public string FileName { get; set; }          // Orijinal dosya adı
        public string FilePath { get; set; }          // Sunucudaki dosya yolu
        public long FileSize { get; set; }            // Dosya boyutu (byte)
        public string ContentType { get; set; }       // MIME type (image/png, application/pdf, vb.)
        public DateTime UploadedDate { get; set; }    // Yüklenme tarihi

        
        [ForeignKey("MessageId")]
        public virtual Message Message { get; set; }
    }
}