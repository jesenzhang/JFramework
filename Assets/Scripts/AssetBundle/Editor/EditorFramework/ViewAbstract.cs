using UnityEditor;
using UnityEngine;
namespace Virivers
{
    /**
     * 显示窗体的抽象类
     * */
    public abstract class ViewAbstract
    {
        protected EditorWindow parent;

        public EditorWindow Parent
        {
            set{ parent = value; }
            get{ return parent; }
        }

        /**
         * Init Function
         * */
        virtual public void Init() { }

        virtual public void reset() { }

        /**
         * Drawing Function running OnGUI
         * */
        public abstract void Drawing();
    }
}