using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    public sealed class MapPlannedRoute
    {
        public readonly int From, To;
        public readonly float Bend;
        public readonly bool Added;
        public readonly Vector2[] Points;
        public MapPlannedRoute(int from, int to, float bend, bool added, Vector2[] points)
        { From = from; To = to; Bend = bend; Added = added; Points = points; }
    }

    public static class MapRoutePlan
    {
        public static List<MapPlannedRoute> Create(MapLayoutPoint[] nodes, int extraLinks = 2, float curveScale = 1)
        {
            var routes = new List<MapPlannedRoute>();
            for (int from = 0; from < nodes.Length; from++)
                foreach (int to in nodes[from].Next)
                {
                    var route = Find(nodes, routes, from, to, to != nodes[from].Next[0], curveScale);
                    if (route == null) throw new InvalidOperationException("Cannot route base edge " + from + " -> " + to);
                    routes.Add(route);
                }
            var candidates = Enumerable.Range(0, nodes.Length).SelectMany(from => Enumerable.Range(0, nodes.Length)
                .Where(to => nodes[to].Row == nodes[from].Row + 1 && !nodes[from].Next.Contains(to))
                .Select(to => new { From = from, To = to, Distance = (Position(nodes[from]) - Position(nodes[to])).sqrMagnitude }))
                .OrderBy(candidate => candidate.Distance).ThenBy(candidate => candidate.From).ThenBy(candidate => candidate.To);
            int added = routes.Count(route => route.Added);
            foreach (var candidate in candidates)
            {
                if (added >= extraLinks) break;
                var route = Find(nodes, routes, candidate.From, candidate.To, true, curveScale);
                if (route == null) continue;
                routes.Add(route);
                added++;
            }
            if (added < extraLinks) throw new InvalidOperationException("Too few safe inter-route links: " + added);
            Verify(nodes, routes);
            return routes;
        }

        private static MapPlannedRoute Find(MapLayoutPoint[] nodes, List<MapPlannedRoute> routes, int from, int to, bool added, float curveScale)
        {
            float preferred = (added ? .45f : nodes[from].Bend) * curveScale;
            foreach (float bend in new[] { preferred, -preferred, 0, .25f, -.25f, .75f, -.75f, 1.5f, -1.5f, 2f, -2f, 3f, -3f })
            {
                int finalRow = nodes.Max(node => node.Row);
                var points = MapRouteGeometry.VisiblePoints(Position(nodes[from]), Position(nodes[to]), bend,
                    nodes[from].Row == finalRow, nodes[to].Row == finalRow, 2);
                if (points.Length < 2) continue;
                if (HitsUnlinkedNode(nodes, from, to, bend)) continue;
                if (routes.Any(route => MapRouteGeometry.Crosses(points, route.Points))) continue;
                return new MapPlannedRoute(from, to, bend, added, points);
            }
            return null;
        }

        public static void Verify(MapLayoutPoint[] nodes, List<MapPlannedRoute> routes)
        {
            int finalRow = nodes.Max(node => node.Row);
            int boss = Array.FindIndex(nodes, node => node.Row == finalRow);
            if (nodes.Count(node => node.Row == finalRow) != 1) throw new InvalidOperationException("Expected one Boss.");
            foreach (var route in routes)
            {
                if (nodes[route.To].Row != nodes[route.From].Row + 1) throw new InvalidOperationException("Route skips a step or goes backward.");
                if (HitsUnlinkedNode(nodes, route.From, route.To, route.Bend))
                    throw new InvalidOperationException("Route overlaps an unlinked node.");
            }
            for (int first = 0; first < routes.Count; first++)
                for (int second = first + 1; second < routes.Count; second++)
                    if (MapRouteGeometry.Crosses(routes[first].Points, routes[second].Points)) throw new InvalidOperationException("Routes cross.");
            var distances = Enumerable.Repeat(-1, nodes.Length).ToArray();
            distances[boss] = 0;
            for (int row = finalRow - 1; row >= 0; row--)
                for (int index = 0; index < nodes.Length; index++)
                {
                    if (nodes[index].Row != row) continue;
                    var next = routes.Where(route => route.From == index).Select(route => distances[route.To]).Distinct().ToArray();
                    if (next.Length != 1 || next[0] < 0) throw new InvalidOperationException("Dead end or unequal remaining Boss steps.");
                    distances[index] = next[0] + 1;
                }
            if (nodes.Count(node => node.Row == 0) != MapLayout.EntryCount || distances.Where((distance, index) => nodes[index].Row == 0).Any(distance => distance != finalRow))
                throw new InvalidOperationException("Entrances have unequal Boss steps.");
            if (routes.Count(route => route.To == boss) < 3) throw new InvalidOperationException("Too few Boss predecessors.");
        }

        private static bool HitsUnlinkedNode(MapLayoutPoint[] nodes, int from, int to, float bend)
        {
            var fullCurve = MapRouteGeometry.SamplePoints(Position(nodes[from]), Position(nodes[to]), bend);
            int finalRow = nodes.Max(node => node.Row);
            for (int index = 0; index < nodes.Length; index++)
                if (index != from && index != to
                    && MapRouteGeometry.HitsNode(fullCurve, Position(nodes[index]), nodes[index].Row == finalRow, 2)) return true;
            return false;
        }

        private static Vector2 Position(MapLayoutPoint node) => new Vector2(node.X, node.Y);
    }
}
