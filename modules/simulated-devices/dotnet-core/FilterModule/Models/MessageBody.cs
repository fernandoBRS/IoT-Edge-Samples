namespace FilterModule.Models
{
    public class MessageBody
    {
        public Machine Machine { get;set; }
        public Ambient Ambient { get; set; }
        public string TimeCreated { get; set; }
    }
}