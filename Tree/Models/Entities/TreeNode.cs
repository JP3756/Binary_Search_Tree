namespace Tree.Models.Entities
{
    public class TreeNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required int Value { get; set; }
        public TreeNode? Left { get; set; }
        public TreeNode? Right { get; set; }
    }
}
