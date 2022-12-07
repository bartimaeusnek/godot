
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Godot;
namespace GodotSharp.Benchmark;

[SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net60)]
public class BenchAABBIntersection
{
    [Benchmark]
    public AABB Simd()
        => AABB.IntersectionSimd(new AABB(1, 2, 3, 10, 10, 10),
                                 new AABB(3, 2, 1, 10, 10, 10));

    [Benchmark(Baseline = true)]
    public AABB Software()
        => AABB.IntersectionSoftware(new AABB(1, 2, 3, 10, 10, 10),
                                     new AABB(3, 2, 1, 10, 10, 10));
}

[SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net60)]
public class BenchAABBIntersectsSegment
{
    [Benchmark]
    public bool Simd()
        => AABB.IntersectsSegmentSimd(new AABB(1, 2, 3, 10, 10, 10),
                                      new Vector3(3, 2, 1),
                                      new Vector3(10, 10, 10));

    [Benchmark(Baseline = true)]
    public bool Software()
        => AABB.IntersectsSegmentSoftware(
               new AABB(1, 2, 3, 10, 10, 10),
               new Vector3(3, 2, 1),
               new Vector3(10, 10, 10)
               );
}

[SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net60)]
public class BenchAABBIntersectsSegmentBruteForce
{
    [Benchmark(Baseline = true)]
    public bool Software()
    {
        var ret = false;
        const int num = 2;

        for (int i = -num; i <= num; i++)
        {
            for (int j = -num; j <= num; j++)
            {
                for (int k = -num; k <= num; k++)
                {
                    var v1 = new Vector3(i, j, k);
                    for (int l = -num; l <= num; l++)
                    {
                        for (int m = -num; m <= num; m++)
                        {
                            for (int n = -num; n <= num; n++)
                            {
                                var v2 = new Vector3(l, n, m);
                                for (int o = -num; o <= num; o++)
                                {
                                    for (int p = -num; p <= num; p++)
                                    {
                                        for (int q = -num; q <= num; q++)
                                        {
                                            var v3 = new Vector3(o, p, q);
                                            for (int v = -num; v <= num; v++)
                                            {
                                                for (int w = -num; w <= num; w++)
                                                {
                                                    for (int x = -num; x <= num; x++)
                                                    {
                                                        var v4 = new Vector3(v, w, x);
                                                        var a = new AABB(v3, v4);
                                                        ret = AABB.IntersectsSegmentSoftware(a, v1, v2);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return ret;
    }

    [Benchmark]
    public bool Simd()
    {
        var ret = false;
        const int num = 2;

        for (int i = -num; i <= num; i++)
        {
            for (int j = -num; j <= num; j++)
            {
                for (int k = -num; k <= num; k++)
                {
                    var v1 = new Vector3(i, j, k);
                    for (int l = -num; l <= num; l++)
                    {
                        for (int m = -num; m <= num; m++)
                        {
                            for (int n = -num; n <= num; n++)
                            {
                                var v2 = new Vector3(l, n, m);
                                for (int o = -num; o <= num; o++)
                                {
                                    for (int p = -num; p <= num; p++)
                                    {
                                        for (int q = -num; q <= num; q++)
                                        {
                                            var v3 = new Vector3(o, p, q);
                                            for (int v = -num; v <= num; v++)
                                            {
                                                for (int w = -num; w <= num; w++)
                                                {
                                                    for (int x = -num; x <= num; x++)
                                                    {
                                                        var v4 = new Vector3(v, w, x);
                                                        var a = new AABB(v3, v4);
                                                        ret = AABB.IntersectsSegmentSimd(a, v1, v2);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return ret;
    }
}

[SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net60)]
public class BenchAABBIntersectionBruteForce
{
    [Benchmark(Baseline = true)]
    public AABB Software()
    {
        AABB ret = new ();
        const int num = 2;

        for (int i = -num; i <= num; i++)
        {
            for (int j = -num; j <= num; j++)
            {
                for (int k = -num; k <= num; k++)
                {
                    var v1 = new Vector3(i, j, k);
                    for (int l = -num; l <= num; l++)
                    {
                        for (int m = -num; m <= num; m++)
                        {
                            for (int n = -num; n <= num; n++)
                            {
                                var v2 = new Vector3(l, n, m);
                                for (int o = -num; o <= num; o++)
                                {
                                    for (int p = -num; p <= num; p++)
                                    {
                                        for (int q = -num; q <= num; q++)
                                        {
                                            var v3 = new Vector3(o, p, q);
                                            for (int v = -num; v <= num; v++)
                                            {
                                                for (int w = -num; w <= num; w++)
                                                {
                                                    for (int x = -num; x <= num; x++)
                                                    {
                                                        var v4 = new Vector3(v, w, x);
                                                        var a = new AABB(v3, v4);
                                                        ret = AABB.IntersectionSoftware(a, new AABB(v1, v2));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return ret;
    }

    [Benchmark]
    public AABB SIMD()
    {
        AABB ret = new ();
        const int num = 2;

        for (int i = -num; i <= num; i++)
        {
            for (int j = -num; j <= num; j++)
            {
                for (int k = -num; k <= num; k++)
                {
                    var v1 = new Vector3(i, j, k);
                    for (int l = -num; l <= num; l++)
                    {
                        for (int m = -num; m <= num; m++)
                        {
                            for (int n = -num; n <= num; n++)
                            {
                                var v2 = new Vector3(l, n, m);
                                for (int o = -num; o <= num; o++)
                                {
                                    for (int p = -num; p <= num; p++)
                                    {
                                        for (int q = -num; q <= num; q++)
                                        {
                                            var v3 = new Vector3(o, p, q);
                                            for (int v = -num; v <= num; v++)
                                            {
                                                for (int w = -num; w <= num; w++)
                                                {
                                                    for (int x = -num; x <= num; x++)
                                                    {
                                                        var v4 = new Vector3(v, w, x);
                                                        var a = new AABB(v3, v4);
                                                        ret = AABB.IntersectionSimd(a, new AABB(v1, v2));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return ret;
    }
}
