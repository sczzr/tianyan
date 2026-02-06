using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Editor.Tools;

namespace TianYanShop.MapGeneration.Editor
{
    /// <summary>
    /// 编辑器状态
    /// </summary>
    public class EditorState
    {
        public EditorToolType SelectedToolType { get; set; } = EditorToolType.HillBrush;
        public EditorTool SelectedTool { get; set; }
        public float BrushRadius { get; set; } = 10f;
        public float BrushStrength { get; set; } = 0.5f;
        public bool ShowGrid { get; set; } = false;
        public bool ShowCells { get; set; } = false;
    }
}
