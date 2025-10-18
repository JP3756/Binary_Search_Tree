using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tree.Models.Entities;
using Tree.Services;

namespace Tree.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BinaryTreeController : ControllerBase
    {
        private readonly IBinaryTreeService _service;

        public BinaryTreeController(IBinaryTreeService service)
        {
            _service = service;
        }

        // Requests
        public record CreateRootRequest(int Value);
        public record CreateChildRequest(Guid ParentId, Tree.Services.ChildSide Side, int Value);
        public record UpdateNodeRequest(int Value);

        // Response DTO (flattened to avoid recursively embedding subtrees)
        public record NodeDto(Guid Id, int Value, Guid? LeftId, Guid? RightId, Guid? ParentId);

        private NodeDto ToDto(TreeNode node)
        {
            var (parent, _) = _service.GetParentAndNode(node.Id);
            return new NodeDto(
                Id: node.Id,
                Value: node.Value,
                LeftId: node.Left?.Id,
                RightId: node.Right?.Id,
                ParentId: parent?.Id
            );
        }

        // Create root
        [HttpPost("root")]
        [ProducesResponseType(typeof(NodeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<NodeDto> CreateRoot([FromBody] CreateRootRequest req)
        {
            try
            {
                var node = _service.CreateRoot(req.Value);
                var dto = ToDto(node);
                return CreatedAtAction(nameof(GetNode), new { id = node.Id }, dto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        // Create child under parent on left/right
        [HttpPost("child")]
        [ProducesResponseType(typeof(NodeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<NodeDto> CreateChild([FromBody] CreateChildRequest req)
        {
            try
            {
                var node = _service.CreateChild(req.ParentId, req.Side, req.Value);
                var dto = ToDto(node);
                return CreatedAtAction(nameof(GetNode), new { id = node.Id }, dto);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is ArgumentException || ex is InvalidOperationException)
            {
                return BadRequest(ex.Message);
            }
        }

        // Read node by id
        [HttpGet("node/{id:guid}")]
        [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<NodeDto> GetNode([FromRoute] Guid id)
        {
            var node = _service.GetNode(id);
            return node is null ? NotFound() : Ok(ToDto(node));
        }

        // Read all (BFS) as flattened nodes
        [HttpGet("nodes")]
        [ProducesResponseType(typeof(IEnumerable<NodeDto>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<NodeDto>> GetAll()
        {
            var nodes = _service.GetAllNodesBreadthFirst().Select(ToDto);
            return Ok(nodes);
        }

        // Read traversal (preorder|inorder|postorder) as flattened nodes
        [HttpGet("traverse/{order}")]
        [ProducesResponseType(typeof(IEnumerable<NodeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<NodeDto>> Traverse([FromRoute] string order)
        {
            try
            {
                var nodes = _service.Traverse(order).Select(ToDto);
                return Ok(nodes);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Update node value
        [HttpPut("node/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Update([FromRoute] Guid id, [FromBody] UpdateNodeRequest req)
        {
            var ok = _service.Update(id, req.Value);
            return ok ? NoContent() : NotFound();
        }

        // Delete node (subtree)
        [HttpDelete("node/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete([FromRoute] Guid id)
        {
            var ok = _service.Delete(id);
            return ok ? NoContent() : NotFound();
        }

        // Get parent for a node
        [HttpGet("parent/{id:guid}")]
        [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<NodeDto> GetParent([FromRoute] Guid id)
        {
            var (parent, node) = _service.GetParentAndNode(id);
            if (node == null) return NotFound();
            return parent == null ? NoContent() : Ok(ToDto(parent));
        }

        // Get root
        [HttpGet("root")]
        [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<NodeDto?> GetRoot()
        {
            var root = _service.Root;
            if (root is null) return NoContent();
            return Ok(ToDto(root));
        }
    }
}
