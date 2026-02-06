using System.Collections.Generic;
using TianYanShop.MapGeneration.Data;

namespace TianYanShop.MapGeneration.Editor
{
    /// <summary>
    /// 特征编辑器
    /// </summary>
    public class FeatureEditor
    {
        private VoronoiGraph _graph;

        public FeatureEditor(VoronoiGraph graph)
        {
            _graph = graph;
        }

        public void UpdateFeatures()
        {
        }

        public void MergeFeatures(int featureId1, int featureId2)
        {
        }

        public void SplitFeature(int featureId)
        {
        }
    }
}
