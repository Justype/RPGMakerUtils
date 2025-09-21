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
using System.Windows.Media;
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

            // Split the key by \n and add them to the dictionary
            foreach (var key in translations.Keys.ToList())
            {
                if (key.Contains('\n'))
                {
                    string[] lines = key.Split(new[] { "\n" }, StringSplitOptions.None);
                    string[] translatedLines = translations[key].Split(new[] { "\n" }, StringSplitOptions.None);
                    if (lines.Length == translatedLines.Length)
                    {
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (!Translations.ContainsKey(lines[i]) && lines[i].Length > 3) // Only add lines longer than 3 characters
                                Translations.Add(lines[i], translatedLines[i]);
                        }
                    }
                }
            }

            LengthKeyDict = translations.Keys.GroupBy(k => k.Length)
                                             .ToDictionary(g => g.Key, g => g.ToList());

        }

        /// <summary>
        /// Regex to match escape sequences like \V[1], \N[2], \G, \C[1], \I[45], \{ }, etc. {} can include line breaks.
        /// </summary>
        public static Regex EscapeRegex { get; } = new Regex(@"\\[a-zA-Z0-9_]+\[(?:[^\]\r\n]*)\]|\\\{(?:[^}]*)\}", RegexOptions.Compiled);

        public static Regex VariableOrNumberRegex { get; } = new Regex(@"^([a-zA-Z_][a-zA-Z0-9_]*|\d+(\.\d+)?)$", RegexOptions.Compiled);

        public static Regex PersonNameRegex { get; } = new Regex(@"<([^<>]+)>", RegexOptions.Compiled | RegexOptions.Singleline);

        public static Regex ObjectNoteRegex { get; } = new Regex(@"<([^<>:]+):([^<>]*)>", RegexOptions.Compiled | RegexOptions.Singleline);

        public static Regex LeadingSpacesRegex { get; } = new Regex(@"^(\\n)*[ 　]*", RegexOptions.Compiled);

        public static Regex TrailingSpacesRegex { get; } = new Regex(@"[ 　:：]*(\\n)*$", RegexOptions.Compiled);

        /// <summary>
        /// RPG Maker MV and MZ DataObject Files
        /// </summary>
        public static string[] DataObjectFiles { get; } = {
            "Actors.json", "Armors.json", "Classes.json", "Enemies.json", "Items.json",
            "MapInfos.json", "Skills.json", "States.json", "Weapons.json",
        };

        public bool UnsafeMode { get; set; } = false;

        public Dictionary<string, string> Translations { get; private set; } = new Dictionary<string, string>();

        public Dictionary<int, List<string>> LengthKeyDict { get; private set; } = new Dictionary<int, List<string>>();

        /// <summary>
        /// Do not use this function directly, use TranslateString instead.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="times"></param>
        /// <returns></returns>
        private string TranslateStringUsingKeyLength(string str, int times, bool directMatch = false)
        {
            // Handle null or empty input
            if (string.IsNullOrEmpty(str))
                return str;

            // Direct translation match
            string translation;
            if (Translations.TryGetValue(str, out translation))
                return translation;

            // Handle multiline strings recursively
            if (str.Contains('\n'))
            {
                var lines = str.Split(new[] { '\n' }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                    lines[i] = TranslateStringUsingKeyLength(lines[i], times, directMatch);

                return string.Join("\n", lines);
            }

            // Preserve leading/trailing spaces
            var leadingSpaces = LeadingSpacesRegex.Match(str).Value;
            var trailingSpaces = TrailingSpacesRegex.Match(str).Value;
            var trimmedStr = TrailingSpacesRegex.Replace(LeadingSpacesRegex.Replace(str, ""), "");

            // Try translating trimmed string
            string trimmedTranslation;
            if (Translations.TryGetValue(trimmedStr, out trimmedTranslation))
                return leadingSpaces + trimmedTranslation + trailingSpaces;

            // Return original if direct match requested
            if (directMatch)
                return str;

            // Handle person name pattern: e.g., "<name>text"
            var parts = PersonNameRegex.Split(str);
            if (parts.Length == 3)
            {
                var personName = parts[1];
                string nameTranslation;
                if (Translations.TryGetValue(personName, out nameTranslation))
                    parts[1] = nameTranslation;

                parts[2] = TranslateStringUsingKeyLength(parts[2], times, directMatch);

                return string.Format("{0}<{1}>{2}", parts[0], parts[1], parts[2]);
            }

            // Fuzzy translation using keys by descending length
            int replacements = 0;
            foreach (var len in LengthKeyDict.Keys.Where(len => len < str.Length).OrderByDescending(len => len))
            {
                foreach (var key in LengthKeyDict[len])
                {
                    if (!str.Contains(key)) continue;

                    str = str.Replace(key, Translations[key]);
                    replacements++;
                    if (replacements >= times)
                        return str;
                }
            }

            // Fallback: return original string
            return str;
        }

        /// <summary>
        /// If target string is in translations key, directly translate it. If not check all keys (ordered by length).
        /// </summary>
        /// <param name="str">target string</param>
        /// <param name="translations">translation dict</param>
        /// <returns></returns>
        private string TranslateString(string str, int times = int.MaxValue, bool directMatch = false)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            // Direct translation match
            string translation;
            if (Translations.TryGetValue(str, out translation))
                return translation;

            // Split the string by the escape sequences, keep the escape sequences to another array
            var splitsTranslated = EscapeRegex.Split(str).Select(s => TranslateStringUsingKeyLength(s, times, directMatch)).ToArray();
            var escapes = EscapeRegex.Matches(str).Cast<Match>().Select(m => m.Value).ToArray();

            // Reconstruct the string
            string result = "";
            int i_escape = 0;
            for (int i = 0; i < splitsTranslated.Length; i++)
            {
                result += splitsTranslated[i];
                if (i_escape < escapes.Length)
                {
                    result += escapes[i_escape];
                    i_escape++;
                }
            }

            return result;
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
                    token.Replace(TranslateString(token.ToString()));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Processes a command represented by the specified JSON token, performing recursive translation on certain
        /// parameters if the command meets specific criteria and unsafe mode is enabled.
        /// </summary>
        /// <remarks>This method only processes the command if <see cref="UnsafeMode"/> is enabled. It
        /// specifically handles "Command 122" and performs recursive translation on the fifth parameter if the
        /// operation type (fourth parameter) is set to "Script" (value 4).</remarks>
        /// <param name="token">The JSON token representing the command to process. Must be an object containing a "parameters" property
        /// with an array of values.</param>
        private void TranslateCommand122(JToken token)
        {
            if (!UnsafeMode)
                return;

            // Command 122: Control Variables
            if (token.Type == JTokenType.Object)
            {
                var jobject = token as JObject;
                if (jobject.ContainsKey("parameters"))
                {
                    var parameters = jobject["parameters"];
                    if (parameters is JArray && parameters.Count() > 0)
                    {
                        // 4th parameter is the operation type
                        // 0 Constant
                        // 1 Variable
                        // 2 Random
                        // 3 Game Data
                        // 4 Script
                        var operationType = (int)parameters[3];
                        if (operationType == 4 && parameters.Count() > 4)
                        {
                            string s = parameters[4].ToString();
                            // Only translate if it is a JS string '' or ""
                            if (s.Length > 0 &&
                                (s.StartsWith("'") && s.EndsWith("'")) ||
                                (s.StartsWith("\"") && s.EndsWith("\"")))
                            {
                                s = s.Substring(1, s.Length - 2);
                                if (Translations.ContainsKey(s))
                                    s = Translations[s];

                                parameters[4].Replace($"'{s}'");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Translate all string in parameters if code is Plugin Object.
        /// </summary>
        /// <param name="token"></param>
        private void TranslateCommand357(JToken token)
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
                        else if (UnsafeMode)
                            TranslateExactJTokenRecursively(parameters, tryParseJson: false);
                    }
                }
            }
        }

        /// <summary>
        /// Translate all string in parameters if code is Plugin Array.
        /// </summary>
        /// <param name="token"></param>
        private void TranslateCommand356(JToken token)
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
                            if (itemArray.Length > 0)
                            {
                                if (BlackWhiteList.PluginArrayWhiteList.Contains(itemArray[0]))
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
                                else if (UnsafeMode)
                                {
                                    // If the plugin is not in the whitelist, translate all string values fully matched
                                    for (int j = 1; j < itemArray.Length; j++)
                                    {
                                        // If the string is not a variable or number, translate it
                                        if (!VariableOrNumberRegex.IsMatch(itemArray[j]))
                                            itemArray[j] = TranslateString(itemArray[j], directMatch: true);
                                    }

                                    // Join the translated parts back into a single string
                                    parametersJArray[i].Replace(string.Join(" ", itemArray));
                                }
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
        private void TranslateJsonWithBlackList(JToken token, IEnumerable<string> blackList, Regex regex = null, bool isSkip = false)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var jobject = token as JObject;
                    var properties = jobject.Children<JProperty>().ToList();
                    for (int i = 0; i < properties.Count; i++)
                    {
                        // Use a local variable, don't overwrite isSkip in the parent scope!
                        bool childSkip = isSkip
                            || blackList.Contains(properties[i].Name)
                            || (regex != null && regex.IsMatch(properties[i].Name));
                        TranslateJsonWithBlackList(properties[i].Value, blackList, regex, childSkip);
                    }
                    break;
                case JTokenType.Array:
                    var items = token.Children().ToList();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        TranslateJsonWithBlackList(item, blackList, regex, isSkip);
                    }
                    break;
                case JTokenType.String:
                    if (!isSkip)
                        token.Replace(TranslateString(token.ToString()));
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
                        bool childTranslate = translate || whiteList.Contains(properties[i].Name);
                        TranslateJsonWithWhiteList(properties[i].Value, whiteList, childTranslate);
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
                        token.Replace(TranslateString(token.ToString()));
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
        public void TranslateGameEvents(JToken token, bool isCodeChildren = false, int times = int.MaxValue)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var jobject = token as JObject;
                    // If do not contain "code" and code is not in the DialogCode Array, return
                    if (jobject.ContainsKey("code"))
                    {
                        int code = (int)jobject["code"];
                        switch (code)
                        {
                            // case 101: // - 101: Mainly used for showing image?
                            case 102: // - 102: Show Choices
                            case 401: // - 401: Text Line (under Show Text)
                            case 402: // - 402: Choice Text
                            case 405: // - 405: Show Scrolling Text or Window
                                TranslateGameEvents(jobject["parameters"], true, times: times);
                                break;
                            case 108: // Comment
                                TranslateGameEvents(jobject["parameters"], true, times: 1);
                                break;
                            case 122:
                                TranslateCommand122(jobject);
                                break;
                            case 356:
                                TranslateCommand356(jobject);
                                break;
                            case 357:
                                TranslateCommand357(jobject);
                                break;
                            case 355: // Script
                            default:
                                break;
                        }
                    }
                    else if (jobject.ContainsKey("displayName"))
                        TranslateGameEvents(jobject["displayName"], true, times);

                    var properties = jobject.Children<JProperty>().ToList();
                    for (int i = 0; i < properties.Count; i++)
                        TranslateGameEvents(properties[i].Value, times: times);
                    break;
                case JTokenType.Array:
                    var items = token.Children().ToList();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        TranslateGameEvents(item, isCodeChildren, times);
                    }
                    break;
                case JTokenType.String:
                    if (isCodeChildren)
                        token.Replace(TranslateString(token.ToString(), times: times));
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
                    if (jobject.ContainsKey("nickname"))
                        TranslateGameObjects(jobject["nickname"], true);
                    if (jobject.ContainsKey("note"))
                    {
                        if (jobject["note"].Type == JTokenType.String)
                        {
                            string noteContent = jobject["note"].ToString();
                            if (UnsafeMode)
                            {
                                noteContent = ObjectNoteRegex.Replace(noteContent, match =>
                                {
                                    string tag = match.Groups[1].Value;       // tag name
                                    string content = match.Groups[2].Value; // text inside the tag
                                    string translatedContent = TranslateString(content);
                                    return $"<{tag}:{translatedContent}>";
                                });
                            }
                            else
                            {
                                #region SG Note Translation
                                // Translate <SG説明:xxxx> to <SG説明:translated>
                                noteContent = Regex.Replace(
                                    noteContent,
                                    @"<SG説明(\d*)\:(.*?)>",
                                    match =>
                                    {
                                        string number = match.Groups[1].Value;       // may be empty if no number
                                        string originalText = match.Groups[2].Value; // text inside the tag
                                        string translatedText = TranslateString(originalText);
                                        return $"<SG説明{number}:{translatedText}>";
                                    },
                                    RegexOptions.Singleline
                                );


                                // Translate <SGカテゴリ:xxxx> to <SGカテゴリ:translated>
                                noteContent = Regex.Replace(noteContent, @"<SGカテゴリ:(.*?)>", match =>
                                {
                                    string originalText = match.Groups[1].Value;
                                    string translatedText = TranslateString(originalText);
                                    return $"<SGカテゴリ:{translatedText}>";
                                }, RegexOptions.Singleline);
                                #endregion
                            }

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
                        token.Replace(TranslateString(token.ToString()));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Recursively translates JSON tokens, including nested objects, arrays, and strings,  replacing fully matched
        /// strings with their translated equivalents.
        /// </summary>
        /// <remarks>
        /// - For JSON objects, the method processes each property value recursively. <br/>
        /// - For JSON arrays, the method processes each item recursively. <br/>
        /// - For JSON strings, the method attempts to parse the string as a JSON object or array. If parsing succeeds, 
        /// the parsed token is translated recursively. If parsing fails, the string is translated directly. <br/>
        /// - The method modifies the input <paramref name="token"/> in place, replacing its content with the translated result.</remarks>
        /// <param name="token">The <see cref="JToken"/> to translate. This can be a JSON object, array, or string.</param>
        public void TranslateExactJTokenRecursively(JToken token, bool tryParseJson = false)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var jobject = token as JObject;
                    var properties = jobject.Children<JProperty>().ToList();
                    for (int i = 0; i < properties.Count; i++)
                        TranslateExactJTokenRecursively(properties[i].Value, tryParseJson);
                    break;
                case JTokenType.Array:
                    var items = token.Children().ToList();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        TranslateExactJTokenRecursively(item, tryParseJson);
                    }
                    break;
                case JTokenType.String:
                    // Try to parse the string as JSON array or object
                    var str = token.ToString();
                    if (tryParseJson)
                    {
                        if (str.Length > 1)
                        {
                            if ((str.StartsWith("'") && str.EndsWith("'")) ||
                             (str.StartsWith("\"") && str.EndsWith("\"")))
                            {
                                string innerStr = str.Substring(1, str.Length - 2);
                                string translatedInnerStr = TranslateString(innerStr, directMatch: true);
                                token.Replace($"'{translatedInnerStr}'");
                                return;
                            }
                            else
                            {
                                JToken parsedToken;
                                    try
                                    {
                                        parsedToken = JToken.Parse(str);
                                        // If parsing is successful, recursively translate the parsed token
                                        TranslateExactJTokenRecursively(parsedToken, tryParseJson);
                                        // Replace the original string with the translated JSON string
                                        token.Replace(parsedToken.ToString(Formatting.None));
                                        return;
                                    }
                                    catch (JsonReaderException)
                                    {
                                        // If parsing fails, it means the string is not a valid JSON structure
                                        // Proceed to translate it as a regular string
                                        token.Replace(TranslateString(token.ToString(), directMatch: true));
                                        return;
                                    }
                                }
                        }
                    }
                    else
                    {
                        token.Replace(TranslateString(token.ToString(), directMatch: true));
                    }
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
                            //TranslateJsonWithBlackList(jsonRoot, BlackWhiteList.SystemBlackList, BlackWhiteList.SystemRegex);
                            TranslateJsonWithWhiteList(jsonRoot, BlackWhiteList.SystemWhiteList);
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
                    else if (UnsafeMode)
                    {
                        // If the plugin is not in the whitelist, translate all string values fully matched
                        TranslateExactJTokenRecursively(pluginJObject, tryParseJson: true);
                    }
                }

                // Replace the original JSON string with the modified one
                string modifiedJsonString = jsonRoot.ToString(Formatting.None);
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
