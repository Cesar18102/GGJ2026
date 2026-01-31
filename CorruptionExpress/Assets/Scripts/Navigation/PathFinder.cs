using System.Collections.Generic;

public static class GraphPathfinder2D
{
    public static List<NavNode2D> FindPath(NavNode2D start, NavNode2D target)
    {
        if (start == null || target == null)
        {
            return null;
        }

        if (start == target)
        {
            return new() { start };
        }

        Queue<NavNode2D> queue = new();
        Dictionary<NavNode2D, NavNode2D> cameFrom = new();

        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();
            if (currentNode == target)
            {
                break;
            }

            foreach (var neighbour in currentNode.GetNeigbours())
            {
                if (neighbour == null || cameFrom.ContainsKey(neighbour))
                {
                    continue;
                }

                cameFrom[neighbour] = currentNode;
                queue.Enqueue(neighbour);
            }
        }

        if (!cameFrom.ContainsKey(target))
        {
            return null;
        }

        List<NavNode2D> path = new();
        for (NavNode2D node = target; node != null; node = cameFrom[node])
        {
            path.Add(node);
        }
        path.Reverse();
        return path;
    }
}