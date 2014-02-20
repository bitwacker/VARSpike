using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VARSpike
{
    public class MatrixDefinition
    {
        public Vector<int> Size { get; set; }


        public Func<int, int, string> GetHeading { get; set; }
        public Func<Vector<int>, string> GetCell { get; set; }
    }


    public class MatrixDefinitionBySet : MatrixDefinition
    {
        protected MatrixDefinitionBySet()
        {
            GetHeading = GetHeadingBySet;
            GetCell = GetCellBySet;
        }

        public List<List<object>> Sets { get; protected set; }

        public Func<Vector<object>, string> RenderCell { get; set; }

        private string GetCellBySet(Vector<int> cell)
        {
            try
            {
                var element = new Vector<object>();
                var setIdx = 0;
                foreach (int i in cell)
                {
                    element.Add(Sets[setIdx][i]);
                    setIdx++;
                }
                return RenderCell(element);
            }
            catch (Exception ex)
            {
                return "[ERR] " + cell + ex;
            }

        }

        private string GetHeadingBySet(int setIdx, int itemIdx)
        {
            return TextHelper.ToCell(Sets[setIdx][itemIdx]);
        }

        public static MatrixDefinitionBySet Define(Func<Vector<object>, string> renderCell, params IEnumerable[] sets)
        {
            var result = new List<List<object>>();
            foreach (var set in sets)
            {
                var l = new List<object>();
                foreach (var item in set)
                {
                    l.Add(item);
                }
                result.Add(l);
            }
            return new MatrixDefinitionBySet()
            {
                Sets = result,
                Size = new Vector<int>(result.Select(x => x.Count())),
                RenderCell = renderCell
            };
        }
    }

    public class MatrixDefinitionBySet2D<T1, T2> : MatrixDefinitionBySet
    {
        public static MatrixDefinitionBySet Define(Func<T1, T2, string> renderCell, IEnumerable<T1> set1, IEnumerable<T2> set2)
        {
            var result = new List<List<object>>();
            result.Add(new List<object>(set1.Cast<object>()));
            result.Add(new List<object>(set2.Cast<object>()));

            return new MatrixDefinitionBySet2D<T1, T2>()
            {
                Sets = result,
                Size = new Vector<int>(result.Select(x => x.Count())),
                RenderCell = (vector) => renderCell((T1)vector[0], (T2)vector[1])
            };
        }
    }

    public class MatrixDefinitionBySet3D<T1, T2, T3> : MatrixDefinitionBySet
    {
        public static MatrixDefinitionBySet Define(Func<T1, T2, T3, string> renderCell, IEnumerable<T1> set1, IEnumerable<T2> set2, IEnumerable<T3> set3)
        {
            var result = new List<List<object>>();
            result.Add(new List<object>(set1.Cast<object>()));
            result.Add(new List<object>(set2.Cast<object>()));
            result.Add(new List<object>(set3.Cast<object>()));

            return new MatrixDefinitionBySet3D<T1, T2, T3>()
            {
                Sets = result,
                Size = new Vector<int>(result.Select(x => x.Count())),
                RenderCell = (vector) => renderCell((T1)vector[0], (T2)vector[1], (T3)vector[2])
            };
        }
    }
}