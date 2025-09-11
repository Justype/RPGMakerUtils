/*:
 * @plugindesc A plugin to translate RPG Maker game text using a JSON dictionary.
 * @author Justype
 * @version 2.2.1 Broader
 *
 * @help
 * This plugin loads a 'translations.json' file from the 'www' folder and uses it to translate various text strings in the game.
 *
 * Features:
 * - Translates menu commands, item names, skill names, actor names, map names, and more.
 * - Supports both synchronous and Promise-based translation APIs for future-proof code.
 * - Automatically applies translations after database load.
 * - Integrates with event commands and message windows.
 *
 * Usage:
 * 1. Place 'translations.json' in your project's 'www' folder. The file should map original text to translated text.
 *    Example:
 *    {
 *      "Hello, world!": "Bonjour, le monde!",
 *      "This is a test.": "Ceci est un test."
 *    }
 * 2. Add this plugin to your project and place it below any plugins that add new menu commands or text.
 * 3. Use TranslationManager in your scripts:
 *    `const translated = TranslationManager.translate("Hello");`
 * API:
 *   TranslationManager.translate(text)                // Synchronous translation
 *   TranslationManager.getTranslatePromise(text)       // Promise-based translation
 *   TranslationManager.translateIfNeed(text, callback) // Callback style
 *
 * Place this plugin below any plugins that add new menu commands or text.
 *
 * @param translationPath
 * @text Translation JSON Path
 * @desc Path to the translations.json file (relative to www folder). Example: 'data/translations.json'
 * @default translations.json
 */

(function () {
    //#region TranslationManager
    function TranslationManager() {
        throw new Error('This is a static class');
    }

    TranslationManager._dict = null;
    TranslationManager._translatedSet = new Set();
    TranslationManager._lengthKeyDict = null;

    TranslationManager._dictPath = null;
    TranslationManager._isDictChanged = false;

    TranslationManager.isInitialized = function () {
        return this._dict !== null;
    };

    TranslationManager.initialize = function (dictionary) {
        this._dict = dictionary;
        this._translatedSet = new Set(Object.values(dictionary));
        this._translatedSet.add(""); // Ensure empty string is included
        this._dict[""] = ""; // Ensure empty string maps to itself

        // use linebreaks to split text (Only if both key and value have same number of lines)
        for (let key in this._dict) {
            if (key.includes('\n')) {
                const value = this._dict[key];
                const keyLines = key.split('\n');
                const valueLines = value.split('\n');
                if (keyLines.length === valueLines.length) {
                    for (let i = 0; i < keyLines.length; i++) {
                        if (keyLines[i] && valueLines[i]) {
                            this._dict[keyLines[i]] = valueLines[i];
                            this._translatedSet.add(valueLines[i]);
                        }
                    }
                }
            }
        }

        // Store keys by length for efficient partial matching
        this._lengthKeyDict = Object.keys(dictionary).reduce((acc, key) => {
            const length = key.length;
            if (!acc[length]) {
                acc[length] = [];
            }
            acc[length].push(key);
            return acc;
        }, {});
    };

    TranslationManager.translate = function (text, detectStartWhitespace = true, times = Number.POSITIVE_INFINITY, cache = true) {
        if (typeof text !== 'string' || !this._dict || this._translatedSet.has(text)) {
            return text;
        }

        // If text can be directly translated, do it
        if (this._dict[text]) {
            return this._dict[text];
        }

        // If has linebreaks, translate each line separately
        if (text.includes('\n')) {
            const lines = text.split('\n');
            const translatedLines = lines.map(line => this.translate(line, detectStartWhitespace, times));
            return translatedLines.join('\n');
        } else {
            if (detectStartWhitespace) {
                const match = text.match(/^\s+/);
                leadingSpaces = match ? match[0] : '';
                text = text.slice(leadingSpaces.length);
            }
            if (this._dict[text]) {
                return detectStartWhitespace ? leadingSpaces + this._dict[text] : this._dict[text];
            }
            // If not in the dictionary, try to find partial matches
            let translatedText = text;
            if (this._dict && this._lengthKeyDict) {
                let count = 0;
                const textLength = text.length;
                const lenSorted = Object.keys(this._lengthKeyDict)
                    .map(l => parseInt(l, 10))
                    .filter(l => l < textLength)
                    .sort((a, b) => b - a); // Descending order

                // Go through all possible length buckets
                outer: for (let len of lenSorted) {
                    for (let key of this._lengthKeyDict[len]) {
                        if (translatedText.includes(key)) {
                            translatedText = translatedText.replace(key, this._dict[key]);
                            count++;
                            if (count >= times) {
                                break outer;
                            }
                        }
                    }
                }

                if (cache && translatedText !== text) {
                    // Cache the new translation
                    this._dict[text] = translatedText;
                    if (!this._lengthKeyDict[text.length]) {
                        this._lengthKeyDict[text.length] = [];
                    }
                    this._lengthKeyDict[text.length].push(text);
                    // this._isDictChanged = true;
                }

                // No matter what, add to translated set to avoid re-processing
                this._translatedSet.add(translatedText);

                translatedText = detectStartWhitespace ? leadingSpaces + translatedText : translatedText;
            }
            return translatedText;
        }
    };

    // Async-style callback, but runs synchronously
    TranslationManager.translateIfNeed = function (text, callback) {
        const result = TranslationManager.translate(text);
        if (typeof callback === 'function') {
            callback(result);
        }
        return result;
    };

    // Returns a Promise that resolves with the translated text
    TranslationManager.getTranslatePromise = function (text) {
        return new Promise((resolve) => {
            const result = TranslationManager.translate(text);
            resolve(result);
        });
    };
    //#endregion

    //#region Data
    TranslationManager.translateDataMap = function () {
        if ($dataMap) {
            if ($dataMap.displayName) {
                $dataMap.displayName = TranslationManager.translate($dataMap.displayName);
            }
        }
    };

    TranslationManager.translateCommonData = function () {
        // Translate map names in $dataMapInfos
        if ($dataMapInfos) {
            $dataMapInfos.forEach(mapInfo => {
                if (mapInfo && mapInfo.name) {
                    mapInfo.name = TranslationManager.translate(mapInfo.name);
                }
            });
        }

        // Translate Actors' names and profiles
        if ($dataActors) {
            $dataActors.forEach(actor => {
                if (actor && actor.name) actor.name = TranslationManager.translate(actor.name);
                if (actor && actor.profile) actor.profile = TranslationManager.translate(actor.profile);
            });
        }

        // Translate Items, Skills, Weapons, Armors, States
        const dataArrays = [$dataItems, $dataSkills, $dataWeapons, $dataArmors, $dataStates];
        dataArrays.forEach(data => {
            if (data) {
                data.forEach(item => {
                    if (item && item.name) item.name = TranslationManager.translate(item.name);
                    if (item && item.description) item.description = TranslationManager.translate(item.description);
                });
            }
        });

        // Translate Enemies
        if ($dataEnemies) {
            $dataEnemies.forEach(enemy => {
                if (enemy && enemy.name) enemy.name = TranslationManager.translate(enemy.name);
            });
        }

        if ($dataSystem) {
            // Translate the System terms and messages
            if ($dataSystem.terms) {
                if ($dataSystem.terms.basic) {
                    $dataSystem.terms.basic.forEach((term, i) => {
                        $dataSystem.terms.basic[i] = TranslationManager.translate(term);
                    });
                }
                if ($dataSystem.terms.commands) {
                    $dataSystem.terms.commands.forEach((term, i) => {
                        $dataSystem.terms.commands[i] = TranslationManager.translate(term);
                    });
                }
                if ($dataSystem.terms.params) {
                    $dataSystem.terms.params.forEach((term, i) => {
                        $dataSystem.terms.params[i] = TranslationManager.translate(term);
                    });
                }
                if ($dataSystem.terms.messages) {
                    for (let key in $dataSystem.terms.messages) {
                        $dataSystem.terms.messages[key] = TranslationManager.translate($dataSystem.terms.messages[key]);
                    }
                }
            }

            // Translate game title
            if ($dataSystem.gameTitle) {
                $dataSystem.gameTitle = TranslationManager.translate($dataSystem.gameTitle);
            }
        }
    };
    //#endregion

    //#region Plugin
    function patchPlugin() {
        // TextResource (RPG Maker MZ)
        if (typeof TextResource !== 'undefined' && TextResource.getText) {
            const _TextResource_getText = TextResource.getText;
            TextResource.getText = function (label) {
                const text = _TextResource_getText.call(this, label);
                return TranslationManager.translate(text);
            };
        }
    };
    //#endregion

    //#region Initialization
    // --- Core Logic: Load the Translation Dictionary ---
    // This function is aliased to load our custom data before the game starts.
    const _DataManager_loadDatabase = DataManager.loadDatabase;
    DataManager.loadDatabase = function () {
        _DataManager_loadDatabase.call(this);
        // Get plugin parameters
        let parameters = PluginManager.parameters('JtJsonTranslationManager');
        let translationPath = parameters['translationPath'] || 'translations.json';
        TranslationManager._dictPath = translationPath;
        const xhr = new XMLHttpRequest();
        const url = translationPath;
        xhr.open('GET', url, false);
        xhr.overrideMimeType('application/json');
        xhr.onload = function () {
            if (xhr.status < 400) {
                TranslationManager.initialize(JSON.parse(xhr.responseText));
            }
        };
        xhr.onerror = function () {
            throw new Error('Failed to load ' + url);
        };
        xhr.send();
    };

    // Patch DataManager.onLoad to run translation after all database files are loaded
    const _DataManager_onLoad = DataManager.onLoad;
    DataManager._translationApplied = false;
    DataManager.onLoad = function (object) {
        _DataManager_onLoad.call(this, object);
        if (DataManager.isDatabaseLoaded() && !DataManager._translationApplied && TranslationManager.isInitialized()) {
            TranslationManager.translateCommonData();
            patchPlugin();
            DataManager._translationApplied = true;
        }

        // Translate map data when it's loaded
        if (object === $dataMap && TranslationManager.isInitialized()) {
            TranslationManager.translateDataMap();
        }
    };

    window.TranslationManager = TranslationManager;
    //#endregion

    //#region Patches
    // Thanks to the improvement in translate function, this is be acceptable now.
    // Window_Base.drawText is implemented in terms of Bitmap.drawText, so patching Bitmap.drawText should cover most cases.
    const _Bitmap_drawText = Bitmap.prototype.drawText;
    Bitmap.prototype.drawText = function (text, x, y, maxWidth, lineHeight, align) {
        if (text) {
            return _Bitmap_drawText.call(this, TranslationManager.translate(text), x, y, maxWidth, lineHeight, align);
        } else {
            return _Bitmap_drawText.call(this, text, x, y, maxWidth, lineHeight, align);
        }
    };

    const _Bitmap_measureTextWidth = Bitmap.prototype.measureTextWidth;
    Bitmap.prototype.measureTextWidth = function (text) {
        if (text) {
            return _Bitmap_measureTextWidth.call(this, TranslationManager.translate(text));
        } else {
            return _Bitmap_measureTextWidth.call(this, text);
        }
    };

    // // Alias the function that draws text in most windows
    // // This provides broad coverage for things like item descriptions, skill names, etc.
    // // drawTextEx will call convertEscapeCharacters, so we only need to patch one of them.
    // const _Window_Base_drawTextEx = Window_Base.prototype.drawTextEx;
    // Window_Base.prototype.drawTextEx = function(text, x, y) {
    //     if (text) {
    //         return _Window_Base_drawTextEx.call(this, TranslationManager.translate(text), x, y);
    //     } else {
    //         return _Window_Base_drawTextEx.call(this, text, x, y);
    //     }
    // };

    const _Window_Base_convertEscapeCharacters = Window_Base.prototype.convertEscapeCharacters;
    Window_Base.prototype.convertEscapeCharacters = function (text) {
        text = _Window_Base_convertEscapeCharacters.call(this, text);
        return TranslationManager.translate(text);
    };
    //#endregion
})();
