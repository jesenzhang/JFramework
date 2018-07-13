using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;

namespace Virivers
{
    /**
     * 基础的编辑器窗口封装
     * */
    public abstract class BaseEditorWindow : EditorWindow
    {
        //  渲染分块的显示窗体
        List<ViewAbstract> views;

        /**
         * 返回多个按照顺序显示的类型列表
         * [typeof(ViewAbstract),typeof(ViewAbstract)]
         * */
        abstract protected Type[] getViewListType();

        /**
         * 初始化
         * */
        public BaseEditorWindow()
        {
            views = new List<ViewAbstract>();
            Type[] viewTypes = getViewListType();
            for (int i = 0; i < viewTypes.Length; i++)
            {
                ViewAbstract view = (ViewAbstract)Activator.CreateInstance(viewTypes[i]);
                view.Parent = this;
                views.Add(view);
            }
        }

        public void Reset()
        {
            for (int i = 0; i < views.Count; i++)
            {
                views[i].reset();
            }
        }

        /**
         * 绘制
         * */
        void OnGUI()
        {
            for (int i = 0; i < views.Count; i++)
            {
                views[i].Drawing();
            }
        }
    }
}