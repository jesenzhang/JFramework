using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace Virivers
{
    /**
     * 文件列表展示
     * */
    public class AssetFileEditor : BaseEditorWindow
    {
        public AssetsItem items;

        public List<string> Variants;

        protected override Type[] getViewListType()
        {
            return new Type[] { typeof(AssetFilePanel) };
        }

    }
}                                           