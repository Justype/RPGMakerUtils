using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RPGMakerUtils.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Xml.Linq;

namespace RPGMakerUtils.Resources
{
    internal class RPGMakerMVZTranslator
    {
        public RPGMakerMVZTranslator(string dictJsonPath) :
            this(JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText(dictJsonPath)))
        {
        }

        public RPGMakerMVZTranslator(Dictionary<string, string> translations)
        {
            Translations = translations;
            OrderedTranslationKeys = translations.Keys.OrderByDescending(k => k.Length).ToList();
        }

        /// <summary>
        /// RPG Maker MV and MZ Dialog Code
        /// - 101: Show Text
        /// - 102: Show Choices
        /// - 401: Text Line (under Show Text)
        /// - 402: Choice Text
        /// - 405: Show Scrolling Text or Window
        /// </summary>
        public static int[] DialogCode { get; } = { 101, 102, 401, 402, 405 };

        public static int PluginArrayCode { get; } = 356;

        public static int PluginObjectCode { get; } = 357;

        /// <summary>
        /// RPG Maker MV and MZ DataObject Files
        /// </summary>
        public static string[] DataObjectFiles { get; } = {
            "Actors.json", "Armors.json", "Classes.json", "Enemies.json", "Items.json",
            "MapInfos.json", "Skills.json", "States.json", "Weapons.json",
        };

        public Dictionary<string, string> Translations { get; private set; } = new Dictionary<string, string>();

        public List<string> OrderedTranslationKeys { get; private set; } = new List<string>();

        /// <summary>
        /// If target string is in translations key, directly translate it. If not check all keys (ordered by length).
        /// </summary>
        /// <param name="str">target string</param>
        /// <param name="translations">translation dict</param>
        /// <returns></returns>
        private string TranslateString(string str)
        {
            if (str == null || str.Length == 0)
                return str;
            if (Translations.ContainsKey(str))
                return Translations[str];
            foreach (string key in OrderedTranslationKeys)
                str = str.Replace(key, Translations[key]);
            return str;
        }

        private void TranslateAllValueString(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var jobject = token as JObject;
                    var properties = jobject.Children<JProperty>().ToList();
                    for (int i = 0; i < properties.Count; i++)
                        TranslateAllValueString(properties[i].Value);
                    break;
                case JTokenType.Array:
                    var items = token.Children().ToList();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        TranslateAllValueString(item);
                    }
                    break;
                case JTokenType.String:
                    token.Replace(TranslateString(token.ToString().Trim()));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Translate all string in parameters if code is Plugin Object.
        /// </summary>
        /// <param name="token"></param>
        private void TranslateRPGMakerPluginObject(JToken token)
        {
            // Make sure the First pluginToken in `parameters` is in the key of PluginObjectWhiteList
            if (token.Type == JTokenType.Object)
            {
                var jobject = token as JObject;
                if (jobject.ContainsKey("parameters"))
                {
                    var parameters = jobject["parameters"];
                    if (parameters is JArray && parameters.Count() > 0)
                    {
                        var firstItem = parameters[0].ToString();
                        if (BlackWhiteList.PluginObjectWhiteList.ContainsKey(firstItem))
                            TranslateJsonWithWhiteList(parameters, BlackWhiteList.PluginObjectWhiteList[firstItem]);
                    }
                }
            }
        }

        /// <summary>
        /// Translate all string in parameters if code is Plugin Array.
        /// </summary>
        /// <param name="token"></param>
        private void TranslateRPGMakerPluginArray(JToken token)
        {
            // Make sure the First pluginToken in `parameters` is in the key of PluginArrayWhiteList
            if (token.Type == JTokenType.Object)
            {
                var jobject = token as JObject;
                if (jobject.ContainsKey("parameters"))
                {
                    var parameters = jobject["parameters"];
                    if (parameters is JArray && parameters.Count() > 0)
                    {
                        JArray parametersJArray = parameters as JArray;
                        for (int i = 0; i < parametersJArray.Count; i++)
                        {
                            var itemString = parametersJArray[i].ToString();

                            string[] itemArray = itemString.Split(' ');
                            if (itemArray.Length > 0 && BlackWhiteList.PluginArrayWhiteList.Contains(itemArray[0]))
                            {
                                // Translate the rest of the string after the first element
                                for (int j = 1; j < itemArray.Length; j++)
                                {
                                    // If the string is not a number, translate it
                                    if (!int.TryParse(itemArray[j], out _))
                                        itemArray[j] = TranslateString(itemArray[j]);
                                }
                                // Join the translated parts back into a single string
                                parametersJArray[i].Replace(string.Join(" ", itemArray));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Translate all string in parameters by a black list.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="blackList"></param>
        /// <param name="isSkip"></param>
        private void TranslateJsonWithBlackList(JToken token, IEnumerable<string> blackList, bool isSkip = false)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var jobject = token as JObject;
                    var properties = jobject.Children<JProperty>().ToList();
                    for (int i = 0; i < properties.Count; i++)
                    {
                        if (blackList.Contains(properties[i].Name))
                            isSkip = true;
                        TranslateJsonWithBlackList(properties[i].Value, blackList, isSkip);
                        isSkip = false;
                    }
                    break;
                case JTokenType.Array:
                    var items = token.Children().ToList();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        TranslateJsonWithBlackList(item, blackList, isSkip);
                    }
                    break;
                case JTokenType.String:
                    if (!isSkip)
                        token.Replace(TranslateString(token.ToString().Trim()));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Translate all string in parameters by a white list.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="whiteList"></param>
        /// <param name="translate"></param>
        private void TranslateJsonWithWhiteList(JToken token, IEnumerable<string> whiteList, bool translate = false)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var jobject = token as JObject;
                    var properties = jobject.Children<JProperty>().ToList();
                    for (int i = 0; i < properties.Count; i++)
                    {
                        if (whiteList.Contains(properties[i].Name))
                            translate = true;
                        TranslateJsonWithWhiteList(properties[i].Value, whiteList, translate);
                        translate = false;
                    }
                    break;
                case JTokenType.Array:
                    var items = token.Children().ToList();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        TranslateJsonWithWhiteList(item, whiteList, translate);
                    }
                    break;
                case JTokenType.String:
                    if (translate)
                        token.Replace(TranslateString(token.ToString().Trim()));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// translate all string in parameters if code is Dialog Or Choice.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="isCodeChildren"></param>
        public void TranslateGameEvents(JToken token, bool isCodeChildren = false)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var jobject = token as JObject;
                    // If do not contain "code" and code is not in the DialogCode Array, return
                    if (jobject.ContainsKey("code"))
                    {
                        int code = (int)jobject["code"];
                        if (DialogCode.Contains(code))
                            TranslateGameEvents(jobject["parameters"], true);
                        else if (code == PluginObjectCode)
                            TranslateRPGMakerPluginObject(jobject);
                        else if (code == PluginArrayCode)
                            TranslateRPGMakerPluginArray(jobject);
                    }
                    else if (jobject.ContainsKey("displayName"))
                        TranslateGameEvents(jobject["displayName"], true);

                    var properties = jobject.Children<JProperty>().ToList();
                    for (int i = 0; i < properties.Count; i++)
                        TranslateGameEvents(properties[i].Value);
                    break;
                case JTokenType.Array:
                    var items = token.Children().ToList();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        TranslateGameEvents(item, isCodeChildren);
                    }
                    break;
                case JTokenType.String:
                    if (isCodeChildren)
                        token.Replace(TranslateString(token.ToString().Trim()));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// translate all string if its key is name, description, profile, or message[0-9]+
        /// </summary>
        /// <param name="token"></param>
        /// <param name="translations"></param>
        /// <param name="is_noun"></param>
        private void TranslateGameObjects(JToken token, bool is_noun = false)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var jobject = token as JObject;
                    // If do not contain "code" and code is not in the DialogCode Array, return
                    if (jobject.ContainsKey("name"))
                        TranslateGameObjects(jobject["name"], true);
                    if (jobject.ContainsKey("description"))
                        TranslateGameObjects(jobject["description"], true);
                    if (jobject.ContainsKey("note"))
                    {
                        if (jobject["note"].Type == JTokenType.String)
                        {
                            string noteContent = jobject["note"].ToString();

                            #region SG Note Translation
                            // Translate <SG説明:xxxx> to <SG説明:translated>
                            noteContent = Regex.Replace(noteContent, @"<SG説明:(.*?)>", match =>
                            {
                                string originalText = match.Groups[1].Value;
                                string translatedText = TranslateString(originalText);
                                return $"<SG説明:{translatedText}>";
                            }, RegexOptions.Singleline);

                            // Translate <SGカテゴリ:xxxx> to <SGカテゴリ:translated>
                            noteContent = Regex.Replace(noteContent, @"<SGカテゴリ:(.*?)>", match =>
                            {
                                string originalText = match.Groups[1].Value;
                                string translatedText = TranslateString(originalText);
                                return $"<SGカテゴリ:{translatedText}>";
                            }, RegexOptions.Singleline);
                            #endregion

                            jobject["note"].Replace(noteContent);
                        }
                    }
                    if (jobject.ContainsKey("profile"))
                        TranslateGameObjects(jobject["profile"], true);

                    List<string> messageKeys = jobject.Properties()
                                                      .Where(p => p.Name.StartsWith("message"))
                                                      .Select(p => p.Name)
                                                      .ToList();

                    foreach (var key in messageKeys)
                        TranslateGameObjects(jobject[key], true);

                    var properties = jobject.Children<JProperty>().ToList();
                    for (int i = 0; i < properties.Count; i++)
                        TranslateGameObjects(properties[i].Value);
                    break;
                case JTokenType.Array:
                    var items = token.Children().ToList();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        TranslateGameObjects(item, is_noun);
                    }
                    break;
                case JTokenType.String:
                    if (is_noun)
                        token.Replace(TranslateString(token.ToString().Trim()));
                    break;
                default:
                    break;
            }
        }

        public bool TranslateAllGameData(IEnumerable<GameDataFile> gameDataFiles)
        {
            bool isFailed = false;

            foreach (var dataFile in gameDataFiles)
            {
                try
                {
                    if (dataFile.FilePath.EndsWith("json"))
                    {
                        string json = File.ReadAllText(dataFile.FilePath);
                        var jsonRoot = JToken.Parse(json);

                        if (dataFile.FileName == "System.json")
                            TranslateJsonWithBlackList(jsonRoot, BlackWhiteList.SystemBlackList);
                        else if (dataFile.FileName.StartsWith("CommonEvents"))
                            TranslateGameEvents(jsonRoot);
                        else if (DataObjectFiles.Contains(dataFile.FileName))
                            TranslateGameObjects(jsonRoot);
                        else if (dataFile.FileName.StartsWith("Map") && dataFile.FileName.EndsWith(".json"))
                            TranslateGameEvents(jsonRoot);
                        else
                            TranslateGameEvents(jsonRoot);

                        string translatedJson = jsonRoot.ToString(Formatting.None);
                        File.WriteAllText(dataFile.FilePath, translatedJson);
                        dataFile.IsDone = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("#### Translation Error: " + Environment.NewLine + ex.Message);
                    isFailed = false;
                }
            }
            return !isFailed;
        }

        public bool TranslatePluginsJs(string pluginsJsPath)
        {
            try
            {
                string jsContent = File.ReadAllText(pluginsJsPath);
                string pattern = @"var \$plugins\s*=\s*(?s)(?<json>\[.*\]);?";
                Match match = Regex.Match(jsContent, pattern, RegexOptions.Singleline);
                if (!match.Success)
                    return false;

                string jsonString = match.Groups["json"].Value;

                JArray jsonRoot = JArray.Parse(jsonString);

                if (jsonRoot == null || jsonRoot.Count == 0)
                    return false;

                foreach (var pluginToken in jsonRoot.Children())
                {
                    if (pluginToken.Type != JTokenType.Object)
                        continue;

                    var pluginJObject = pluginToken as JObject;

                    if (pluginJObject.ContainsKey("name") && BlackWhiteList.PluginsJsWhiteList.ContainsKey(pluginJObject["name"].ToString()))
                    {
                        var parameters = pluginJObject["parameters"];
                        if (parameters.Type != JTokenType.Object)
                            continue;

                        JObject parametersJObject = parameters as JObject;

                        foreach (TranslateTarget target in BlackWhiteList.PluginsJsWhiteList[pluginJObject["name"].ToString()])
                        {
                            if (target.TokenType == JTokenType.String)
                            {
                                if (parametersJObject.ContainsKey(target.Name))
                                    TranslateJsonWithWhiteList(parametersJObject[target.Name], new string[] { target.Name }, true);
                            }
                            // If the target is an array, check if it has sub-targets
                            // and the parameters are in a string of JArray
                            else if (target.TokenType == JTokenType.Array && target.SubTargets != null)
                            {
                                if (!parametersJObject.ContainsKey(target.Name))
                                    continue;

                                try
                                {
                                    JArray targetJArray = JArray.Parse(parametersJObject[target.Name].ToString());
                                    for (int i = 0; i < targetJArray.Count; i++)
                                    {
                                        JObject item = JObject.Parse(targetJArray[i].ToString());
                                        TranslateJsonWithWhiteList(item, target.SubTargets);
                                        targetJArray[i] = item.ToString(Formatting.None);
                                    }

                                    parametersJObject[target.Name] = targetJArray.ToString(Formatting.None);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }

                // Replace the original JSON string with the modified one
                string modifiedJsonString = jsonRoot.ToString(Formatting.Indented);
                File.WriteAllText(
                    pluginsJsPath,
                    Regex.Replace(jsContent, pattern, $"var $plugins = {modifiedJsonString};", RegexOptions.Singleline)
                );

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("#### Translation Error: " + Environment.NewLine + ex.Message);
                return false;
            }
        }
    }
}
