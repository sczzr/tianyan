using System.Collections.Generic;
using TianYanShop.MapGeneration.Data;

namespace TianYanShop.MapGeneration.Editor
{
    /// <summary>
    /// 国家编辑器
    /// </summary>
    public class StateEditor
    {
        private VoronoiGraph _graph;

        public StateEditor(VoronoiGraph graph)
        {
            _graph = graph;
        }

        public void UpdateStateBorders()
        {
        }

        public void MergeStates(int stateId1, int stateId2)
        {
        }

        public void SplitState(int stateId)
        {
        }

        public void AddProvince(int stateId, List<int> cells)
        {
        }

        public void RemoveProvince(int provinceId)
        {
        }
    }
}
