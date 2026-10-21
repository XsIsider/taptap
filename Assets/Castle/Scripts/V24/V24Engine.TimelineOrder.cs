using System;
using System.Collections.Generic;
using System.Linq;

namespace Castle.V24
{
    public sealed partial class V24Engine
    {
        /// <summary>仅打乱展示顺序，不改候选 ID、答案、草稿或存档；避免直接展示原答案顺序。</summary>
        public static int[] ShuffleTimelineClues(IEnumerable<int> candidates, Random random)
        {
            var source = candidates.Distinct().ToArray();
            var result = (int[])source.Clone();
            for (int i=result.Length-1;i>0;i--)
            {
                int next = random.Next(i+1);
                int value = result[i];result[i]=result[next];result[next]=value;
            }
            if (result.Length>1 && result.SequenceEqual(source))
            {
                int value=result[0];result[0]=result[1];result[1]=value;
            }
            return result;
        }
    }
}
