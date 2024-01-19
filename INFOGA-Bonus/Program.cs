using System.Diagnostics.Contracts;
using System.Drawing;
using System.Reflection.Metadata;

class Point
{
    public double x;
    public double y;
    public readonly int xInt;
    public readonly int yInt;

    public Point(double x, double y)
    {
        this.x = x;
        this.y = y;
        this.xInt = (int)x;
        this.yInt = (int)y;
    }

    public static bool IsLeftOf(Point a, Point b, Point c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) > 0;
    }
}

class Edge
{
    public Point p1;
    public Point p2;
    public Edge(Point p1, Point p2)
    {
        this.p1 = p1;
        this.p2 = p2;
    }
}

class Problem
{
    public List<Point> points;
    public List<Edge> convexhull;
    private Random rnd;

    private static double MIN_X = 0;
    private static double MAX_X = 500;
    private static double MIN_Y = 0;
    private static double MAX_Y = 500;
    private static int NUM_POINTS = 1000;

    public Problem()
    {
        points = new(NUM_POINTS);
        convexhull = new(NUM_POINTS);
        rnd = new Random();

        for (int i = 0; i < NUM_POINTS; i++)
        {
            double x = rnd.NextDouble() * (MAX_X - MIN_X) + MIN_X;
            double y = rnd.NextDouble() * (MAX_Y - MIN_Y) + MIN_Y;
            points.Add(new(x, y));
        }
    }

    // Note: Windows only
    public void Export()
    {
        if (!Directory.Exists("./exports")) Directory.CreateDirectory("./exports");
        string filepath = $"./exports/{DateTime.Now.ToString("hh-mm-ss.f")}.png";

        int w = (int)(MAX_X - MIN_X);
        int h = (int)(MAX_Y - MIN_Y);

        Bitmap bmp = new Bitmap(w, h);
        Graphics g = Graphics.FromImage(bmp);

        g.Clear(Color.White);


        Pen pointPen = new Pen(Color.Black, 1);
        foreach (var p in points)
        {
            int drawX = -(int)MIN_X + p.xInt;
            int drawY = -(int)MIN_Y + p.yInt;
            g.DrawEllipse(pointPen, drawX - 1, drawY - 1, 3, 3);
        }

        Pen edgePen = new Pen(Color.Red, 1);
        foreach (var e in convexhull)
        {
            g.DrawLine(edgePen, e.p1.xInt, e.p1.yInt, e.p2.xInt, e.p2.yInt);
        }

        bmp.Save(filepath);
    }
}

static class Solvers
{
    public static void GiftWrap(Problem problem)
    {
        var ps = problem.points.ToList();
        var startPoint = ps.MinBy(p => p.x);
        var currentPoint = startPoint;
        var targetIndex = -1;
        Point targetPoint = null;

        while (!ReferenceEquals(targetPoint, startPoint))
        {
            targetPoint = null;
            
            for (int i = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                if (targetPoint == null && !ReferenceEquals(p, currentPoint)) targetPoint = p;
                if (targetPoint != null && Point.IsLeftOf(currentPoint, targetPoint, p))
                {
                    targetPoint = p;
                    targetIndex = i;
                }
            }

            problem.convexhull.Add(new(currentPoint, targetPoint));
            currentPoint = targetPoint;

            var temp = ps[targetIndex];
            ps[targetIndex] = ps[^1];
            ps[^1] = temp;
            ps.RemoveAt(ps.Count - 1);
        }
    }
}
class Program
{
    static void Main(string[] args)
    {
        Problem pr = new Problem();
        Solvers.GiftWrap(pr);
        pr.Export();
    }
}