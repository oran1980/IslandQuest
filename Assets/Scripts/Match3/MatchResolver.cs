using System.Collections.Generic;

namespace IslandQuest.Match3
{
    /// <summary>
    /// Turns <see cref="MatchFinder"/>'s flat set of matched cells into
    /// connected <see cref="MatchGroup"/>s, so the rest of the engine can
    /// reason about "this blob of 5" instead of "these 5 unrelated cells
    /// that each individually happen to be matched". See design.md §3.1b.
    /// </summary>
    public static class MatchResolver
    {
        public static IReadOnlyList<MatchGroup> FindMatchGroups(Board board)
        {
            var matchedCells = MatchFinder.FindMatchedCells(board);
            var visited = new HashSet<(int Row, int Col)>();
            var groups = new List<MatchGroup>();

            foreach (var cell in matchedCells)
            {
                if (visited.Contains(cell))
                    continue;

                var type = board[cell.Row, cell.Col].Type;
                var component = FloodFill(board, matchedCells, visited, cell, type);
                groups.Add(new MatchGroup(type, component));
            }

            return groups;
        }

        /// <summary>
        /// 4-directional flood fill, restricted to cells that are both in
        /// <paramref name="matchedCells"/> and the same <paramref name="type"/>.
        /// The type/membership check happens before a cell is pushed onto the
        /// stack (not after popping) so a same-coordinate cell that fails the
        /// check is never marked visited — otherwise a matched cell of a
        /// different color sitting adjacent to this component would get
        /// skipped when the outer loop later tries to start its own group
        /// from it.
        /// </summary>
        private static HashSet<(int Row, int Col)> FloodFill(
            Board board,
            HashSet<(int Row, int Col)> matchedCells,
            HashSet<(int Row, int Col)> visited,
            (int Row, int Col) start,
            TileType type)
        {
            var component = new HashSet<(int Row, int Col)>();
            var stack = new Stack<(int Row, int Col)>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!component.Add(current))
                    continue;

                visited.Add(current);

                foreach (var neighbor in Neighbors(board, current))
                {
                    if (component.Contains(neighbor)) continue;
                    if (!matchedCells.Contains(neighbor)) continue;
                    if (board[neighbor.Row, neighbor.Col].Type != type) continue;
                    stack.Push(neighbor);
                }
            }

            return component;
        }

        private static IEnumerable<(int Row, int Col)> Neighbors(Board board, (int Row, int Col) cell)
        {
            if (cell.Row > 0) yield return (cell.Row - 1, cell.Col);
            if (cell.Row < board.Rows - 1) yield return (cell.Row + 1, cell.Col);
            if (cell.Col > 0) yield return (cell.Row, cell.Col - 1);
            if (cell.Col < board.Columns - 1) yield return (cell.Row, cell.Col + 1);
        }
    }
}
