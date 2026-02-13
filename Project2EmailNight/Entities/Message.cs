using System.ComponentModel.DataAnnotations;

namespace Project2EmailNight.Entities
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        public string SenderEmail { get; set; }
        public string ReceiverEmail { get; set; }
        public string Subject { get; set; }
        public string MessageDetail { get; set; }
        public DateTime SendDate { get; set; }
       
        public bool IsStatus { get; set; }  // Okundu bilgisi
        public bool IsStarred { get; set; } // Yıldız
        
        public bool IsImportant { get; set; } = false;      // Önemli işareti
        public bool IsDraft { get; set; } = false;          // Taslak mı?
        public DateTime? SnoozeUntil { get; set; }          
        public string Category { get; set; } = "Genel";     // Kategori (İş, Eğitim, Sosyal, Acil, Genel)

        // Dosya ekleri için Navigation Property
        public virtual List<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    }
}