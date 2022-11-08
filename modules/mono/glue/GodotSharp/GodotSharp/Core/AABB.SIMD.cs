using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
namespace Godot;

public partial struct AABB
{
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CompareGreaterThanHorizontal(Vector128<real_t> a, Vector128<real_t> b)
    {
        Vector128<int> v = Sse2.CompareGreaterThan(a, b).AsInt32();
        return !Sse41.TestZ(v, v);
    }

#if REAL_T_IS_DOUBLE
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CompareGreaterThanHorizontal(Vector256<real_t> a, Vector256<real_t> b)
    {
        Vector256<int> v = Avx.CompareGreaterThan(a, b).AsInt32();
        return !Avx.TestZ(v, v);
    }

    [SkipLocalsInit]
    private static AABB IntersectionSimd(AABB dis, AABB with)
    {
        Vector256<real_t> srcMinVec = dis._position.ToSIMDVector();
        Vector256<real_t> srcSizeVec = dis._size.ToSIMDVector();
        Vector256<real_t> srcMaxVec = Avx.Add(srcMinVec, srcSizeVec);
        Vector256<real_t> dstMinVec = with._position.ToSIMDVector();
        Vector256<real_t> dstSizeVec = with._size.ToSIMDVector();
        Vector256<real_t> dstMaxVec = Avx.Add(dstMinVec, dstSizeVec);

        //we are combining cases here since TestZ is more expensive on AVX than it is on SSE
        Vector256<real_t> srcMinGtdstMax = Avx.CompareGreaterThan(srcMinVec, dstMaxVec);
        Vector256<real_t> dstMinGtsrcMax = Avx.CompareGreaterThan(dstMinVec, srcMaxVec);
        Vector256<int> cmp = Avx.Or(srcMinGtdstMax, dstMinGtsrcMax).AsInt32();

        if (!Avx.TestZ(cmp, cmp))
        {
            return new AABB();
        }

        Vector256<real_t> min = Avx.Max(srcMinVec, dstMinVec);
        Vector256<real_t> max = Avx.Min(srcMaxVec, dstMaxVec);
        Vector256<real_t> maxMinusMin = Avx.Subtract(max, min);

        unsafe
        {
            real_t[] aabb = new real_t[7];
            real_t* aabbPtr = (real_t*) Unsafe.AsPointer(ref aabb[0]);
            Avx.Store(aabbPtr, min);
            Avx.Store(aabbPtr + 3, maxMinusMin);
            return *(AABB*) aabbPtr; //we are returning ByValue here
        }
    }

    private static Vector256<real_t> _One = Vector256.Create(1D, 1D, 1D, 0D);

    [SkipLocalsInit]
    private static bool IntersectsSegmentSimd(AABB dis, Vector3 from, Vector3 to)
    {
        Vector256<real_t> segFromVec = from.ToSIMDVector();
        Vector256<real_t> segToVec = to.ToSIMDVector();
        Vector256<real_t> boxBeginVec = dis._position.ToSIMDVector();
        Vector256<real_t> boxEndVec = Avx.Add(boxBeginVec, dis._size.ToSIMDVector());

        //if (segFrom < segTo) && else
        Vector256<real_t> ifCaseComparison = Avx.CompareGreaterThan(segToVec, segFromVec);
        Vector256<real_t> elseCaseComparison = Avx.CompareGreaterThanOrEqual(segFromVec, segToVec);

        //Zero out else case
        Vector256<real_t> ifCaseSegFrom = Avx.And(segFromVec, ifCaseComparison);
        Vector256<real_t> ifCaseBoxEnd = Avx.And(boxEndVec, ifCaseComparison);
        Vector256<real_t> ifCaseBoxBegin = Avx.And(boxBeginVec, ifCaseComparison);
        Vector256<real_t> ifCaseSegTo = Avx.And(segToVec, ifCaseComparison);

        //Zero out if case
        Vector256<real_t> elseCaseSegFrom = Avx.And(segFromVec, elseCaseComparison);
        Vector256<real_t> elseCaseBoxEnd = Avx.And(boxEndVec, elseCaseComparison);
        Vector256<real_t> elseCaseBoxBegin = Avx.And(boxBeginVec, elseCaseComparison);
        Vector256<real_t> elseCaseSegTo = Avx.And(segToVec, elseCaseComparison);

        //we are combining cases here since TestZ is more expensive on AVX than it is on SSE
        Vector256<real_t> ifCaseSegFromGtBoxEnd = Avx.CompareGreaterThan(ifCaseSegFrom, ifCaseBoxEnd);
        Vector256<real_t> ifCaseBoxBeginGtSegTo = Avx.CompareGreaterThan(ifCaseBoxBegin, ifCaseSegTo);
        Vector256<real_t> elseCaseSegToGtBoxEnd = Avx.CompareGreaterThan(elseCaseSegTo, elseCaseBoxEnd);
        Vector256<real_t> elseCaseBoxBeginGtSegFrom = Avx.CompareGreaterThan(elseCaseBoxBegin, elseCaseSegFrom);

        Vector256<real_t> ifCaseGt = Avx.Or(ifCaseSegFromGtBoxEnd, ifCaseBoxBeginGtSegTo);
        Vector256<real_t> elseCaseGt = Avx.Or(elseCaseSegToGtBoxEnd, elseCaseBoxBeginGtSegFrom);

        Vector256<int> gt = Avx.Or(ifCaseGt, elseCaseGt).AsInt32();

        if (!Avx.TestZ(gt, gt))
        {
            return false;
        }

        Vector256<real_t> ifCaseOne = Avx.And(_One, ifCaseComparison);
        Vector256<real_t> elseCaseOne = Avx.And(_One, elseCaseComparison);

        //no need to include if else here, will only be used as right hand side of division
        Vector256<real_t> lengthVec = Avx.Subtract(segToVec, segFromVec);

        //min - max for if and else case
        Vector256<real_t> ifCaseBoxBeginGtSegFrom = Avx.CompareGreaterThan(ifCaseBoxBegin, ifCaseSegFrom);
        Vector256<real_t> ifCaseCMin = Avx.Divide(Avx.Subtract(ifCaseBoxBegin, ifCaseSegFrom), lengthVec);
        ifCaseCMin = Avx.And(ifCaseCMin, ifCaseBoxBeginGtSegFrom); //we use and instead of blend here since 1 & 1 → 0, no need to waste another cycle for blending

        Vector256<real_t> ifCaseSegToGtBoxEnd = Avx.CompareGreaterThan(ifCaseSegTo, ifCaseBoxEnd);
        Vector256<real_t> ifCaseCMax = Avx.Divide(Avx.Subtract(ifCaseBoxEnd, ifCaseSegFrom), lengthVec);
        ifCaseCMax = Avx.BlendVariable(ifCaseOne, ifCaseCMax, ifCaseSegToGtBoxEnd);

        Vector256<real_t> elseCaseSegFromGtBoxEnd = Avx.CompareGreaterThan(elseCaseSegFrom, elseCaseBoxEnd);
        Vector256<real_t> elseCaseCMin = Avx.Divide(Avx.Subtract(elseCaseBoxEnd, elseCaseSegFrom), lengthVec);
        elseCaseCMin = Avx.And(elseCaseCMin, elseCaseSegFromGtBoxEnd);

        Vector256<real_t> elseCaseBoxBeginGtSegTo = Avx.CompareGreaterThan(elseCaseBoxBegin, elseCaseSegTo);
        Vector256<real_t> elseCaseCMax = Avx.Divide(Avx.Subtract(elseCaseBoxBegin, elseCaseSegFrom), lengthVec);
        elseCaseCMax = Avx.BlendVariable(elseCaseOne, elseCaseCMax, elseCaseBoxBeginGtSegTo);

        //merge if / else case
        Vector256<real_t> cminVec = Avx.Or(ifCaseCMin, elseCaseCMin);
        Vector256<real_t> cmaxVec = Avx.Or(ifCaseCMax, elseCaseCMax);

        Vector128<real_t> max = Vector128.CreateScalarUnsafe(1d);
        Vector128<real_t> min = Vector128<real_t>.Zero;

        if (TestMinMax(cminVec, cmaxVec, ref min, ref max))
        {
            return false;
        }

        //<1 2 3 4> → <2 1 4 3>
        if (TestMinMax(Avx.Permute(cminVec, 0b0101), Avx.Permute(cmaxVec, 0b0101), ref min, ref max))
        {
            return false;
        }

        //<1 2 3 4> → <3 4 1 2>
        cminVec = Avx.Permute2x128(cminVec, cminVec, 1);
        cmaxVec = Avx.Permute2x128(cmaxVec, cmaxVec, 1);

        if (TestMinMax(cminVec, cmaxVec, ref min, ref max))
        {
            return false;
        }

        return true;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestMinMax(Vector256<real_t> cminVec, Vector256<real_t> cmaxVec, ref Vector128<real_t> min, ref Vector128<real_t> max)
    {
        Vector128<real_t> cmin128 = Unsafe.As<Vector256<real_t>, Vector128<real_t>>(ref cminVec); //Unsafe.As is about 2% faster than Avx.ExtractVector128(0)
        Vector128<real_t> cmax128 = Unsafe.As<Vector256<real_t>, Vector128<real_t>>(ref cmaxVec);
        min = Sse2.MaxScalar(cmin128, min);
        max = Sse2.MinScalar(cmax128, max);
        return CompareGreaterThanHorizontal(min, max);
    }
#else
    [SkipLocalsInit]
    private static AABB IntersectionSimd(AABB dis, AABB with)
    {
        Vector128<real_t> srcMinVec = dis._position.ToSIMDVector();
        Vector128<real_t> srcSizeVec = dis._size.ToSIMDVector();
        Vector128<real_t> srcMaxVec = Sse.Add(srcMinVec, srcSizeVec);
        Vector128<real_t> dstMinVec = with._position.ToSIMDVector();
        Vector128<real_t> dstSizeVec = with._size.ToSIMDVector();
        Vector128<real_t> dstMaxVec = Sse.Add(dstMinVec, dstSizeVec);

        Vector128<real_t> minVec = Sse.Max(srcMinVec, dstMinVec);
        Vector128<real_t> max = Sse.Min(srcMaxVec, dstMaxVec);
        Vector128<real_t> maxMinusMinVec = Sse.Subtract(max, minVec);

        if (CompareGreaterThanHorizontal(srcMinVec, dstMaxVec))
        {
            return new AABB();
        }

        if (CompareGreaterThanHorizontal(dstMinVec, srcMaxVec))
        {
            return new AABB();
        }

        unsafe
        {
            real_t[] aabb = new real_t[7];
            real_t* aabbPtr = (real_t*) Unsafe.AsPointer(ref aabb[0]);
            Sse.Store(aabbPtr, minVec);
            Sse.Store(aabbPtr + 3, maxMinusMinVec);
            return *(AABB*) aabbPtr; //we are returning ByValue here
        }
    }

    private static Vector128<real_t> _One = Vector128.Create(1f, 1f, 1f, 0f);

    [SkipLocalsInit]
    private static bool IntersectsSegmentSimd(AABB dis, Vector3 from, Vector3 to)
    {
        Vector128<real_t> segFromVec = from.ToSIMDVector();
        Vector128<real_t> segToVec = to.ToSIMDVector();
        Vector128<real_t> boxBeginVec = dis._position.ToSIMDVector();
        Vector128<real_t> boxEndVec = Sse.Add(boxBeginVec, dis._size.ToSIMDVector());

        //if (segFrom < segTo) && else
        Vector128<real_t> ifCaseComparison = Sse.CompareGreaterThan(segToVec, segFromVec);
        Vector128<real_t> elseCaseComparison = Sse.CompareGreaterThanOrEqual(segFromVec, segToVec);

        //Zero out else case
        Vector128<real_t> ifCaseSegFrom = Sse.And(segFromVec, ifCaseComparison);
        Vector128<real_t> ifCaseBoxEnd = Sse.And(boxEndVec, ifCaseComparison);
        Vector128<real_t> ifCaseBoxBegin = Sse.And(boxBeginVec, ifCaseComparison);
        Vector128<real_t> ifCaseSegTo = Sse.And(segToVec, ifCaseComparison);

        //Zero out if case
        Vector128<real_t> elseCaseSegFrom = Sse.And(segFromVec, elseCaseComparison);
        Vector128<real_t> elseCaseBoxEnd = Sse.And(boxEndVec, elseCaseComparison);
        Vector128<real_t> elseCaseBoxBegin = Sse.And(boxBeginVec, elseCaseComparison);
        Vector128<real_t> elseCaseSegTo = Sse.And(segToVec, elseCaseComparison);

        if (CompareGreaterThanHorizontal(ifCaseSegFrom, ifCaseBoxEnd))
        {
            return false;
        }
        if (CompareGreaterThanHorizontal(ifCaseBoxBegin, ifCaseSegTo))
        {
            return false;
        }
        if (CompareGreaterThanHorizontal(elseCaseSegTo, elseCaseBoxEnd))
        {
            return false;
        }
        if (CompareGreaterThanHorizontal(elseCaseBoxBegin, elseCaseSegFrom))
        {
            return false;
        }

        Vector128<real_t> ifCaseOne = Sse.And(_One, ifCaseComparison);
        Vector128<real_t> elseCaseOne = Sse.And(_One, elseCaseComparison);

        //no need to include if else here, will only be used as right hand side of division
        Vector128<real_t> lengthVec = Sse.Subtract(segToVec, segFromVec);

        //min - max for if and else case
        Vector128<real_t> ifCaseCMin;
        Vector128<real_t> ifCaseCMax;
        Vector128<real_t> elseCaseCMin;
        Vector128<real_t> elseCaseCMax;

        Vector128<real_t> ifCaseBoxBeginGtSegFrom = Sse.CompareGreaterThan(ifCaseBoxBegin, ifCaseSegFrom);
        ifCaseCMin = Sse.Divide(Sse.Subtract(ifCaseBoxBegin, ifCaseSegFrom), lengthVec);
        ifCaseCMin = Sse.And(ifCaseCMin, ifCaseBoxBeginGtSegFrom); //we use AND instead of BLEND here since 1 & 1 → 0, no need to waste another cycle for blending

        Vector128<real_t> ifCaseSegToGtBoxEnd = Sse.CompareGreaterThan(ifCaseSegTo, ifCaseBoxEnd);
        ifCaseCMax = Sse.Divide(Sse.Subtract(ifCaseBoxEnd, ifCaseSegFrom), lengthVec);
        ifCaseCMax = Sse41.BlendVariable(ifCaseOne, ifCaseCMax, ifCaseSegToGtBoxEnd);

        Vector128<real_t> elseCaseSegFromGtBoxEnd = Sse.CompareGreaterThan(elseCaseSegFrom, elseCaseBoxEnd);
        elseCaseCMin = Sse.Divide(Sse.Subtract(elseCaseBoxEnd, elseCaseSegFrom), lengthVec);
        elseCaseCMin = Sse.And(elseCaseCMin, elseCaseSegFromGtBoxEnd);

        Vector128<real_t> elseCaseBoxBeginGtSegTo = Sse.CompareGreaterThan(elseCaseBoxBegin, elseCaseSegTo);
        elseCaseCMax = Sse.Divide(Sse.Subtract(elseCaseBoxBegin, elseCaseSegFrom), lengthVec);
        elseCaseCMax = Sse41.BlendVariable(elseCaseOne, elseCaseCMax, elseCaseBoxBeginGtSegTo);

        //merge if / else case
        Vector128<real_t> cminVec = Sse.Or(ifCaseCMin, elseCaseCMin);
        Vector128<real_t> cmaxVec = Sse.Or(ifCaseCMax, elseCaseCMax);

        Vector128<real_t> max = Vector128.CreateScalarUnsafe(1f);
        Vector128<real_t> min = Vector128<real_t>.Zero;

        if (TestMinMax(cminVec, cmaxVec, ref min, ref max))
        {
            return false;
        }

        //<1 2 3 4> → <2 4 3 1>
        cminVec = Sse.Shuffle(cminVec, cminVec, 0b00_10_11_01);
        cmaxVec = Sse.Shuffle(cmaxVec, cmaxVec, 0b00_10_11_01);

        if (TestMinMax(cminVec, cmaxVec, ref min, ref max))
        {
            return false;
        }

        //<2 4 3 1> → <3 4 2 1>
        cminVec = Sse.Shuffle(cminVec, cminVec, 0b11_00_01_10);
        cmaxVec = Sse.Shuffle(cmaxVec, cmaxVec, 0b11_00_01_10);

        if (TestMinMax(cminVec, cmaxVec, ref min, ref max))
        {
            return false;
        }

        return true;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestMinMax(Vector128<real_t> cminVec, Vector128<real_t> cmaxVec, ref Vector128<real_t> min, ref Vector128<real_t> max)
    {
        min = Sse.MaxScalar(cminVec, min);
        max = Sse.MinScalar(cmaxVec, max);
        return CompareGreaterThanHorizontal(min, max);
    }
#endif
}
