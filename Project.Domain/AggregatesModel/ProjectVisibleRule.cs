namespace Project.Domain.AggregatesModel
{
    public class ProjectVisibleRule
    {
        public int ProjectId { get; set; }
        public bool Visible { get; set; }
        public string Tags { get; set; }
    }
}