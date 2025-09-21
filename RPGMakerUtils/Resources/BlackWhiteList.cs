using RPGMakerUtils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RPGMakerUtils.Resources
{
    internal static class BlackWhiteList
    {
        /// <summary>
        /// Object Event whitelist of plugins and their associated allowed values.
        /// In data/CommonEvent.json or data/MapXX.json (if code 357 and key in the "parameters" object, translate )
        /// 如果 code:357 且 键 在 "parameters" 对象中，翻译该值。
        /// </summary>
        public static Dictionary<string, IEnumerable<string>> PluginObjectWhiteList { get; } = new Dictionary<string, IEnumerable<string>>()
        {
            { "DTextPicture", new string[] { "text" } }, // {"code":357,"indent":0,"parameters":["DTextPicture","dText","文字列ピクチャ準備",{"text":"ロレンチア\n","fontSize":"0"}]}
        };

        /// <summary>
        /// Array Event whitelist of plugins.
        /// In data/CommonEvent.json or data/MapXX.json (if code 356 and plugin is the first substring, translate it)
        /// 如果 code:356 且 键 是第一个子字符串，翻译它。
        /// </summary>
        public static string[] PluginArrayWhiteList { get; } = new string[]
        {
            "D_TEXT", // {"code":356,"indent":1,"parameters":["D_TEXT こんだけ注目集めといてSじゃなかったら・・・ 12"]}
        };

        /// <summary>
        /// Plugin whitelist of plugins and their associated allowed values.
        /// In js/Plugins.js 
        /// </summary>
        public static Dictionary<string, IEnumerable<TranslateTarget>> PluginsJsWhiteList { get; } = new Dictionary<string, IEnumerable<TranslateTarget>>
        {
            // The Key of  Dictionary is the name of the plugin
            // The Value is the parameters' name that you want to translate
            // If it is a simple string, use TranslateTarget(string)
            // If it is a string that contains an array of objects, use TranslateTarget(string, string[])
            
            /* Quick Example
             * 
             * CASE1: a simple field
             * new TranslateTarget("Buy Command")
             * {
             *   name: "YED_SkillShop",
             *   status: true,
             *   description: "v1.0.1 This plugin provides a skill shop for buying skills.",
             *   parameters: {
             *     "[Basic Setting]": "",
             *     "Default Price": "100",
             *     "[Visual Setting]": "",
             *     "Gold Cost Text": "購入価格",
             *     "Item Cost Text": "Requires",
             *     "Buy Command": "購入",
             *     "Cancel Command": "キャンセル",
             *     "Text Alignment": "center",
             *   },
             * },
             * 
             * CASE2: a field of object in string array
             * new TranslateTarget("baseItems", new string[] {"name"})
             * {
             *   name: "TorigoyaMZ_CommonMenu",
             *   status: true,
             *   description: "メニューからコモンイベント呼び出しプラグイン (v.1.2.0)",
             *   parameters: {
             *     base: "",
             *     baseItems:
             *       '["{\\"name\\":\\"ステータス\\",\\"commonEvent\\":\\"21\\",\\"switchId\\":\\"0\\",\\"visibility\\":\\"true\\",\\"note\\":\\"\\"}","{\\"name\\":\\"性格変更\\",\\"commonEvent\\":\\"22\\",\\"switchId\\":\\"0\\",\\"visibility\\":\\"true\\",\\"note\\":\\"\\"}"]',
             *   },
             * },
             * */
            {
                "TorigoyaMZ_CommonMenu",
                new TranslateTarget[] {
                    new TranslateTarget("baseItems", new string[] {"name"}), // We only want to translate the name field inside the array of objects
            } },
            { "LoadComSim", new TranslateTarget[] { new TranslateTarget("loadtext") }},
            { "OriginMenuStatus", new TranslateTarget[] { new TranslateTarget("command_name") }},
            {
                "BB_CustomSaveWindow", new TranslateTarget[] {
                    new TranslateTarget("Item1title"),
                    new TranslateTarget("Item2title"),
                    new TranslateTarget("Item3title"),
                    new TranslateTarget("Item4title"),
                    new TranslateTarget("Item5title"),
                    new TranslateTarget("Item6title"),
            } },
            {
                "SceneGlossary", new TranslateTarget[] {
                    new TranslateTarget("GlossaryInfo", new string[] { "CategoryHelp", "GlossaryHelp", "ConfirmHelp", "UsingHelp" }),
            } },
            {
                "YED_SkillShop", new TranslateTarget[] {
                    new TranslateTarget("Buy Command"),
                    new TranslateTarget("Gold Cost Text"),
                    new TranslateTarget("Item Cost Text"),
                    new TranslateTarget("Cancel Command"),
            } },
        };

        // /// <summary>
        // /// Black list of `System.json` keys that should not be translated.
        // /// </summary>
        // public static string[] SystemBlackList { get; } = { "titleBgm", "title2Name" };

        public static string[] SystemWhiteList { get; } = {
            "armorTypes", "currencyUnit", "elements", "equipTypes", "gameTitle",
            "skillTypes", "switches", "variables", "weaponTypes",
            "terms", // "basic", "commands", "params", "messages",
        };

        public static Regex SystemRegex { get; } = new Regex(@"bgm", RegexOptions.Compiled & RegexOptions.IgnoreCase);
    }
}
