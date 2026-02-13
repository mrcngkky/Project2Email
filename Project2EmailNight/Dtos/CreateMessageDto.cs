namespace Project2EmailNight.Dtos
{
    public class CreateMessageDto
    {
        public string ReceiverEmail { get; set; }
        public string Subject { get; set; }
        public string MessageDetail { get; set; }
    }
}