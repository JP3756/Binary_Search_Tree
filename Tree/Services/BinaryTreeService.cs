using System;
using System.Collections.Generic;
using Tree.Models.Entities;

namespace Tree.Services
{
    public enum ChildSide { Left, Right }

    public interface IBinaryTreeService
    {
        TreeNode? Root { get; }
        TreeNode CreateRoot(int value);
        TreeNode CreateChild(Guid parentId, ChildSide side, int value);
        TreeNode? GetNode(Guid id);
        (TreeNode? parent, TreeNode? node) GetParentAndNode(Guid id);
        bool Update(Guid id, int value);
        bool Delete(Guid id); // deletes subtree rooted at id
        IEnumerable<TreeNode> Traverse(string order); // preorder | inorder | postorder
        IEnumerable<TreeNode> GetAllNodesBreadthFirst();
    }

    public class BinaryTreeService : IBinaryTreeService
    {
        private readonly object _lock = new();
        private TreeNode? _root;

        private readonly Dictionary<Guid, TreeNode> _nodes = new();
        private readonly Dictionary<Guid, Guid?> _parent = new();

        public TreeNode? Root
        {
            get { lock (_lock) { return _root; } }
        }

        public TreeNode CreateRoot(int value)
        {
            lock (_lock)
            {
                if (_root != null) throw new InvalidOperationException("Root already exists");
                var node = new TreeNode { Value = value };
                _root = node;
                _nodes[node.Id] = node;
                _parent[node.Id] = null;
                return node;
            }
        }

        public TreeNode CreateChild(Guid parentId, ChildSide side, int value)
        {
            lock (_lock)
            {
                if (!_nodes.TryGetValue(parentId, out var parent))
                    throw new KeyNotFoundException("Parent not found");

                var existing = side == ChildSide.Left ? parent.Left : parent.Right;
                if (existing != null)
                    throw new InvalidOperationException($"Parent already has a {side.ToString().ToLowerInvariant()} child");

                var node = new TreeNode { Value = value };
                if (side == ChildSide.Left) parent.Left = node; else parent.Right = node;

                _nodes[node.Id] = node;
                _parent[node.Id] = parent.Id;
                return node;
            }
        }

        public TreeNode? GetNode(Guid id)
        {
            lock (_lock)
            {
                return _nodes.TryGetValue(id, out var node) ? node : null;
            }
        }

        public (TreeNode? parent, TreeNode? node) GetParentAndNode(Guid id)
        {
            lock (_lock)
            {
                if (!_nodes.TryGetValue(id, out var node)) return (null, null);
                TreeNode? parent = null;
                if (_parent.TryGetValue(id, out var pid) && pid.HasValue)
                    parent = _nodes[pid.Value];
                return (parent, node);
            }
        }

        public bool Update(Guid id, int value)
        {
            lock (_lock)
            {
                if (!_nodes.TryGetValue(id, out var node)) return false;
                node.Value = value;
                return true;
            }
        }

        public bool Delete(Guid id)
        {
            lock (_lock)
            {
                if (!_nodes.TryGetValue(id, out var node)) return false;

                if (_parent.TryGetValue(id, out var pid))
                {
                    if (pid is null)
                    {
                        _root = null;
                    }
                    else
                    {
                        var parent = _nodes[pid.Value];
                        if (parent.Left?.Id == id) parent.Left = null;
                        if (parent.Right?.Id == id) parent.Right = null;
                    }
                }

                RemoveFromIndexRecursive(node);
                return true;
            }
        }

        private void RemoveFromIndexRecursive(TreeNode node)
        {
            if (node.Left != null) RemoveFromIndexRecursive(node.Left);
            if (node.Right != null) RemoveFromIndexRecursive(node.Right);
            _nodes.Remove(node.Id);
            _parent.Remove(node.Id);
        }

        public IEnumerable<TreeNode> Traverse(string order)
        {
            lock (_lock)
            {
                var root = _root;
                var list = new List<TreeNode>();
                if (root == null) return list;
                switch (order.ToLowerInvariant())
                {
                    case "inorder":
                        InOrder(root, list);
                        break;
                    case "preorder":
                        PreOrder(root, list);
                        break;
                    case "postorder":
                        PostOrder(root, list);
                        break;
                    default:
                        throw new ArgumentException("Order must be preorder, inorder or postorder");
                }
                return list;
            }
        }

        public IEnumerable<TreeNode> GetAllNodesBreadthFirst()
        {
            lock (_lock)
            {
                var list = new List<TreeNode>();
                var q = new Queue<TreeNode>();
                if (_root != null) q.Enqueue(_root);
                while (q.Count > 0)
                {
                    var n = q.Dequeue();
                    list.Add(n);
                    if (n.Left != null) q.Enqueue(n.Left);
                    if (n.Right != null) q.Enqueue(n.Right);
                }
                return list;
            }
        }

        private static void InOrder(TreeNode node, List<TreeNode> list)
        {
            if (node.Left != null) InOrder(node.Left, list);
            list.Add(node);
            if (node.Right != null) InOrder(node.Right, list);
        }
        private static void PreOrder(TreeNode node, List<TreeNode> list)
        {
            list.Add(node);
            if (node.Left != null) PreOrder(node.Left, list);
            if (node.Right != null) PreOrder(node.Right, list);
        }
        private static void PostOrder(TreeNode node, List<TreeNode> list)
        {
            if (node.Left != null) PostOrder(node.Left, list);
            if (node.Right != null) PostOrder(node.Right, list);
            list.Add(node);
        }
    }
}
