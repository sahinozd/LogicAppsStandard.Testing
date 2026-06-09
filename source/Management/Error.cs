namespace LogicApps.Management
{
    public record Error
    {
        public string? Code { get; set; }

        public string? Message { get; set; }
    }
}
