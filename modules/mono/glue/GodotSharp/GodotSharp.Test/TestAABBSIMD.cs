using System.Runtime.Intrinsics.X86;
using Godot;
namespace GodotSharp.Test;

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Thread = System.Threading.Thread;

[TestFixture(TestOf = typeof(AABB))]
public class TestAABBSIMD
{
    [DatapointSource]
    private ((Vector3, Vector3), (Vector3, Vector3))[] _points = {
                                                                    ((new Vector3(1, 2, 3), new Vector3(10, 10, 10)), (new Vector3(3, 2, 1), new Vector3(10, 10, 10))),
                                                                    ((new Vector3(1, 2, 3), new Vector3(10, 10, 10)), (new Vector3(1, 2, 3), new Vector3(10, 10, 10))),
                                                                    ((new Vector3(100, 200, 300), new Vector3(10, 10, 10)), (new Vector3(-300, -200, -100), new Vector3(10, 10, 10))),
                                                                    ((new Vector3(100, 200, 300), new Vector3(10, 10, 10)), (new Vector3(-300, -200, -100), new Vector3(-300, -200, -100))),
                                                                };

    [Test]
    [Theory]
    public void IntersectsSegmentSIMD(((Vector3, Vector3), (Vector3, Vector3)) points)
    {
#pragma warning disable CS0183,CS0184
        if (points.Item1.Item1.x is double && Avx.IsSupported is false)
        {
            Assert.Ignore("AVX not supported!");
        }
        if (points.Item1.Item1.x is float && Sse41.IsSupported is false)
        {
            Assert.Ignore("SSE41 not supported!");
        }
#pragma warning restore CS0183,CS0184

        var a = new AABB(points.Item1.Item1, points.Item1.Item2);
        Assert.That(AABB.IntersectsSegmentSimd(a,points.Item2.Item1, points.Item2.Item2), Is.EqualTo(AABB.IntersectsSegmentSoftware(a, points.Item2.Item1, points.Item2.Item2)));
    }

    [Test]
    public void BruteForceSegementTest()
    {
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
                                                        bool simd = AABB.IntersectsSegmentSimd(a,v1, v2);
                                                        bool software = AABB.IntersectsSegmentSoftware(a,v1, v2);
                                                        if (simd == software)
                                                            continue;

                                                        software = AABB.IntersectsSegmentSoftware(a, v1, v2);
                                                        simd = AABB.IntersectsSegmentSimd(a, v1, v2);
                                                        string value = "simd was " + simd + " should be " + software;
                                                        value += "\nv1: " + v1;
                                                        value += "\nv2: " + v2;
                                                        value += "\nv3: " + v3;
                                                        value += "\nv4: " + v4;
                                                        Assert.Fail(value);
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
        Assert.Pass();
    }

    [Test]
    [Theory]
    public void IntersectionSIMD(((Vector3, Vector3), (Vector3, Vector3)) points)
    {
#pragma warning disable CS0183,CS0184
        if (points.Item1.Item1.x is double && Avx.IsSupported is false)
        {
            Assert.Ignore("AVX not supported!");
        }
        if (points.Item1.Item1.x is float && Sse41.IsSupported is false)
        {
            Assert.Ignore("SSE41 not supported!");
        }
#pragma warning restore CS0183,CS0184

        var a = new AABB(points.Item1.Item1, points.Item1.Item2);
        var b = new AABB(points.Item2.Item1, points.Item2.Item2);
        Assert.That(AABB.IntersectionSimd(a,b), Is.EqualTo(AABB.IntersectionSoftware(a,b)));
    }
}
