using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using GodotSharp.Benchmark;

BenchmarkSwitcher
    .FromTypes(new []{typeof(BenchAABBIntersection), typeof(BenchAABBIntersectsSegment), typeof(BenchAABBIntersectionBruteForce), typeof(BenchAABBIntersectsSegmentBruteForce)})
    .Run(args, DefaultConfig.Instance.AddJob(Job.Default.WithCustomBuildConfiguration("REAL_T_IS_DOUBLE").WithStrategy(RunStrategy.Throughput)));
