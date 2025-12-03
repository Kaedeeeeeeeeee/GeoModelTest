using UnityEngine;
using UnityEngine.EventSystems;

namespace WorkbenchSystem
{
    /// <summary>
    /// 显微镜预览区域的拖拽目标
    /// </summary>
    public class MicroscopeDropTarget : MonoBehaviour, IDropHandler
    {
        private MicroscopeController controller;

        public void Init(MicroscopeController controller)
        {
            this.controller = controller;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (controller == null) return;
            controller.HandleDrop(controller.DraggingSample);
        }
    }
}
