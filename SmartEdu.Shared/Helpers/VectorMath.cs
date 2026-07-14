using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.Helpers
{
    public static class VectorMath
    {
        public static double CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return 0;
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            if (na == 0 || nb == 0) return 0;
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }

        public static float[] MeanPool(IEnumerable<float[]> vectors)
        {
            var list = vectors.Where(v => v != null && v.Length > 0).ToList();
            if (list.Count == 0) return Array.Empty<float>();

            int dim = list[0].Length;
            var result = new float[dim];

            foreach (var v in list)
            {
                for (int i = 0; i < dim; i++)
                    result[i] += v[i];
            }

            for (int i = 0; i < dim; i++)
                result[i] /= list.Count;

            return result;
        }
    }
}
