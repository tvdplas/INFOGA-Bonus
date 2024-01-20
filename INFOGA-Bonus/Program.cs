using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using System.Diagnostics;
using System.Drawing;

class Point : IComparable<Point>
{
    public double x;
    public double y;
    public readonly int xInt;
    public readonly int yInt;
    public const double Epsilon = 0.00000001;
    public Point(double x, double y)
    {
        this.x = x;
        this.y = y;
        this.xInt = (int)x;
        this.yInt = (int)y;
    }

    /// <summary>
    /// For vector a->b, check if c is to the left or on the line and further away
    /// </summary>
    public static bool IsLeftOf(Point a, Point b, Point c)
    {
        double dotp = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        if(dotp > Epsilon)
            return true;
        // trust werkt gewoon
        return (dotp > -Epsilon) && ((c.x - a.x) * (c.x - a.x) + (c.y - a.y) * (c.y - a.y) > (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y) + Epsilon);
    }
    public static bool operator >(Point a, Point b)
    {
        return (a.x > b.x) || (a.x == b.x && a.y > b.y);
    }
    public static bool operator <(Point a, Point b)
    {
        return (a.x < b.x || (a.x == b.x && a.y < b.y));
    }
    public static bool operator >=(Point a, Point b)
    {
        return (a.x > b.x || (a.x == b.x && a.y >= b.y));
    }
    public static bool operator <=(Point a, Point b)
    {
        return (a.x < b.x || (a.x == b.x && a.y <= b.y));
    }
    public int CompareTo(Point other)
    {
        if(this > other)
            return 1;
        if(other > this)
            return -1;
        return 0;
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

    private static double MIN_X = 0;
    private static double MAX_X = 1000;
    private static double MIN_Y = 0;
    private static double MAX_Y = 1000;
    private static int NUM_POINTS = 5_000;
    private static string DISTRIBUTION = "circle"; // possible values: uniform, normal, circle, circle_fixed
    public Problem()
    {
        points = new(NUM_POINTS);
        convexhull = new(NUM_POINTS);
        Random rnd = new Random();

        // Allow for 3.5 sigma of normal distribution, otherwise fall back to respective bound
        Normal normalX = new Normal((MIN_X + MAX_X) / 2, (MIN_X + MAX_X) / 7);
        Normal normalY = new Normal((MIN_Y + MAX_Y) / 2, (MIN_Y + MAX_Y) / 7);

        for (int i = 0; i < NUM_POINTS; i++)
        {
            double x = 0, y = 0;
            if (DISTRIBUTION == "uniform")
            {
                x = rnd.NextDouble() * (MAX_X - MIN_X) + MIN_X;
                y = rnd.NextDouble() * (MAX_Y - MIN_Y) + MIN_Y;
            }
            else if (DISTRIBUTION == "normal")
            {
                x = Math.Min(Math.Max(normalX.Sample(), MIN_X), MAX_X);
                y = Math.Min(Math.Max(normalY.Sample(), MIN_Y), MAX_Y);
            }
            else if (DISTRIBUTION.StartsWith("circle"))
            {
                var radius = Math.Min(MAX_X, MAX_Y) / 2;
                var angle = rnd.NextDouble() * 2 * Math.PI;
                var distance = DISTRIBUTION == "circle" ? rnd.NextDouble() * radius : radius;
                x = radius + distance * Math.Cos(angle);
                y = radius + distance * Math.Sin(angle);
            }
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

                if (targetPoint == null && !ReferenceEquals(p, currentPoint))
                {
                    targetPoint = p;
                    targetIndex = i;
                    continue;
                }

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
    public static void GrahamScan(Problem problem)
    {
        Point[] ps = new Point[problem.points.Count];
        for (int x = 0; x < problem.points.Count; x++)
            ps[x] = problem.points[x];
        Array.Sort(ps);
        scanPart(ps, problem.convexhull, true);
        scanPart(ps, problem.convexhull, false);
    }
    private static void scanPart(Point[] ps, IList<Edge> hull, bool upper)
    {
        //Scan through the points
        IList<Point> boundary = new List<Point>();
        int i;
        for (int x = 0; x < ps.Length; x++)
        {
            if (upper)
                i = x;
            else
                i = ps.Length - 1 - x;
            while (boundary.Count >= 2)
            {
                if (Point.IsLeftOf(boundary[boundary.Count - 2], ps[i], boundary[boundary.Count - 1]))
                    break;
                boundary.RemoveAt(boundary.Count - 1);
            }
            boundary.Add(ps[i]);
        }

        //Add the hull
        for (int x = 0; x < boundary.Count - 1; x++)
            hull.Add(new Edge(boundary[x], boundary[x + 1]));
    }
    public static void Sort(IList<Point> points)
    {
        quickSort(points, new Random(), 0, points.Count);
    }
    private static void quickSort(IList<Point> points, Random random, int start, int end)//Sort the interval [start, end) so with start but without end
    {
        if (end - start <= 10)//Now we use Insertion Sort
        {
            for (int x = start + 1; x < end; x++)
                for (int y = x; y > start; y--)
                {
                    if (points[y] >= points[y - 1])
                        break;
                    swap(points, y, y - 1);
                }
            return;
        }

        //Split the set
        int middle = random.Next(start, end);
        swap(points, start, middle);
        int firstgreat = start + 1;
        for (int x = start + 1; x < end; x++)
            if (points[x] < points[start])
            {
                swap(points, x, firstgreat);
                firstgreat++;
            }
        swap(points, start, firstgreat - 1);
        quickSort(points, random, start, firstgreat - 1);
        quickSort(points, random, firstgreat, end);
    }
    private static void swap(IList<Point> points, int i1, int i2)
    {
        Point temp = points[i1];
        points[i1] = points[i2];
        points[i2] = temp;
    }
}
class Program
{
    static void Main(string[] args)
    {
        Stopwatch sw = new Stopwatch();

        var NUM_RUNS = 1_000;
        double[][] times = new double[3][];
        times[0] = new double[NUM_RUNS];
        times[1] = new double[NUM_RUNS];
        times[2] = new double[NUM_RUNS];
        double[][] hullPoints = new double[2][];
        hullPoints[0] = new double[NUM_RUNS];
        hullPoints[1] = new double[NUM_RUNS];
        for (int i = 0; i < NUM_RUNS; i++)
        {
            if (i % 100 == 0)
                Console.WriteLine($"{i / (NUM_RUNS / 100)}% done");
            // Generate testcase
            sw.Restart();
            Problem pr = new Problem();
            sw.Stop();
            times[^1][i] = (double)sw.ElapsedMilliseconds;


            // Run testcase
            sw.Restart();
            Solvers.GiftWrap(pr);
            sw.Stop();
            times[0][i] = (double)sw.ElapsedMilliseconds;
            hullPoints[0][i] = pr.convexhull.Count();
            pr.convexhull.Clear();

            sw.Restart();
            Solvers.GrahamScan(pr);
            sw.Stop();
            times[1][i] = (double)sw.ElapsedMilliseconds;
            hullPoints[1][i] = pr.convexhull.Count();
            pr.convexhull.Clear();
        }

        (var testCaseMean, var testCaseStdDev) = times[2].MeanStandardDeviation();
        var testCaseCorrectedStdDev = testCaseStdDev / Math.Sqrt(NUM_RUNS);
        Console.WriteLine("---- Test case generation ----");
        Console.WriteLine($"Mean: {testCaseMean}\t StdDev (corrected): {testCaseCorrectedStdDev}\t");

        (var giftwrapMean, var giftwrapStdDev) = times[0].MeanStandardDeviation();
        var giftwrapCorrectedStdDev = giftwrapStdDev / Math.Sqrt(NUM_RUNS);
        (var giftwrapHPMean, var giftwrapHPStdDev) = hullPoints[0].MeanStandardDeviation();
        var giftwrapHPCorrectedStdDev = giftwrapHPStdDev / Math.Sqrt(NUM_RUNS);
        Console.WriteLine("---- Gift wrap ----");
        Console.WriteLine($"Mean: {giftwrapMean}\t StdDev (corrected): {giftwrapCorrectedStdDev}\t");
        Console.WriteLine($"Mean HP: {giftwrapHPMean}\t StdDev (corrected): {giftwrapHPCorrectedStdDev}\t");

        (var grahamMean, var grahamStdDev) = times[1].MeanStandardDeviation();
        var grahamCorrectedStdDev = grahamStdDev / Math.Sqrt(NUM_RUNS);
        (var grahamHPMean, var grahamHPStdDev) = hullPoints[0].MeanStandardDeviation();
        var grahamHPCorrectedStdDev = grahamHPStdDev / Math.Sqrt(NUM_RUNS);
        Console.WriteLine("---- Graham scan ----");
        Console.WriteLine($"Mean: {grahamMean}\t StdDev (corrected): {grahamCorrectedStdDev}\t");
        Console.WriteLine($"Mean HP: {grahamHPMean}\t StdDev (corrected): {grahamHPCorrectedStdDev}\t");

        //Problem pr = new Problem();
        //Solvers.GrahamScan(pr);
        //pr.Export();
        //Solvers.Sort(pr.points);
    }
}