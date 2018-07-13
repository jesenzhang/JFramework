using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace Virivers
{
    [Serializable]
    public enum AssetRegexType
    {
        Asset = 1,
        Animation = 1 << 1,
        Animator = 1 << 2,
        AvatarMask = 1 << 3,
        Cubemap = 1 << 4,
        Flare = 1 << 5,
        Font = 1 << 6,
        GUISkin = 1 << 7,
        Material = 1 << 8,
        PhysicMaterial = 1 << 9,
        PhysicsMaterial2D = 1 << 10,
        RenderTexture = 1 << 11,
        Shader = 1 << 12,
        Scene = 1 << 13,
        Prefab = 1 << 14,
        Image = 1 << 15,
        Model = 1 << 16,
		Data = 1 << 17
    };


    /**
     * 正则表达式的辅助方法
     * */
    public class RegexUtility
    {
        private static Dictionary<int, string> regex = new Dictionary<int, string>()
        {
            {1 ,@"asset"},
            {1 << 1,@"anim"},
            {1 << 2,@"controller"},
            {1 << 3,@"mask"},
            {1 << 4,@"cubemap"},
            {1 << 5,@"flare"},
            {1 << 6,@"fontsettings"},
            {1 << 7,@"guiskin"},
            {1 << 8,@"mat"},
            {1 << 9,@"physicMaterial"},
            {1 << 10,@"physicMaterial2D"},
            {1 << 11,@"renderTexture"},
            {1 << 12,@"shader"},
            {1 << 13,@"unity"},
            {1 << 14,@"prefab"},
            {1 << 15,@"PNG|png|TGA|tga|JPG|jpg|JPEG|jpeg"},
            {1 << 16,@"FBX|fbx|OBJ|obj"},
			{1 << 17,@"XML|xmb|JSON|json|TXT|txt"}
        };

        /**
         * 通过传入的type生成表达式
         * */
        public static string generateRegex(AssetRegexType regexType)
        {
            string regexString = @"[.](";
            int regexInt = (int)regexType;

            foreach (KeyValuePair<int, string> kv in regex)
            {
                if((regexInt & kv.Key) == kv.Key)
                {
                    if(regexString == @"[.](")
                        regexString += kv.Value ;
                    else
                        regexString += "|" + kv.Value;
                }
            }

            regexString = regexString + ")$";
            return regexString;
        }
    }
}