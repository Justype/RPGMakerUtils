/*:
 * @plugindesc A plugin to translate RPG Maker game text using a JSON dictionary.
 * @author Justype
 * @version 2.3.0
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
    TranslationManager._escapeRegex = /\\[a-zA-Z0-9_]+\[[^\]]*\]|\\\{[^}]*\}/g; // Matches escape sequences like \V[1], \N[2], \{text}
    TranslationManager._personRegex = /¡¾([^¡¿]+)¡¿|<([^>]+)>/; // Matches ¡¾Name¡¿ or <Name>

    TranslationManager._dictPath = null;
    TranslationManager._isDictChanged = false;

    TranslationManager.isInitialized = function () {
        return this._dict !== null;
    };

    TranslationManager.splitKeyValueByRegex = function (key, regex, minLength = 0) {
        value = this._dict[key];
        if (regex.test(key) || regex.test(value)) {
            const keyParts = key.split(regex);
            const valueParts = value.split(regex);
            if (keyParts.length === valueParts.length) {
                for (let i = 0; i < keyParts.length; i++) {
                    if (keyParts[i] && valueParts[i]) {
                        if (keyParts[i].length <= minLength) continue; // Skip very short parts
                        this._dict[keyParts[i]] = valueParts[i];
                        this._translatedSet.add(valueParts[i]);
                    }
                }
            }
        }
    }

    TranslationManager.initialize = function (dictionary) {
        this._dict = dictionary;
        this._translatedSet = new Set(Object.values(dictionary));
        this._translatedSet.add(""); // Ensure empty string is included
        this._dict[""] = ""; // Ensure empty string maps to itself

        // Preprocess dictionary to handle names in ¡¾¡¿ or <>
        Object.keys(dictionary).forEach(key => {
            this.splitKeyValueByRegex(key, this._personRegex, 0);
        });
        // Preprocess dictionary to handle escape sequences
        Object.keys(dictionary).forEach(key => {
            this.splitKeyValueByRegex(key, this._escapeRegex, 0);
        });
        // Preprocess dictionary to handle line breaks
        Object.keys(dictionary).forEach(key => {
            this.splitKeyValueByRegex(key, /\r\n|\n|\r/, 3);
        });


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

    TranslationManager._translateRecursively = function (text, times = Number.POSITIVE_INFINITY, cache = true) {
        if (this._dict[text]) {
            return this._dict[text];
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

            translatedText = translatedText;
        }
        return translatedText;
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

            // try to handle escape sequences and translate only non-escape parts
            let rawParts = [];
            let escapeMatches = [];
            let lastIndex = 0;
            let m;
            while ((m = this._escapeRegex.exec(text)) !== null) {
                if (m.index > lastIndex) {
                    rawParts.push(text.slice(lastIndex, m.index));
                }
                escapeMatches.push(m[0]);
                lastIndex = m.index + m[0].length;
            }

            if (lastIndex < text.length) {
                rawParts.push(text.slice(lastIndex));
            }

            // Translate each raw part separately
            let translatedParts = rawParts.map(part => this._translateRecursively(part, times));

            // Reconstruct the full text with escape sequences in place
            let translatedText = '';
            let escapeIndex = 0;
            for (let part of translatedParts) {
                translatedText += part;
                if (escapeIndex < escapeMatches.length) {
                    translatedText += escapeMatches[escapeIndex++];
                }
            }

            return detectStartWhitespace ? leadingSpaces + translatedText : translatedText;
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

    // TranslationManager.saveDictionary = function () {
    //     // Use Electron's fs module to save the updated dictionary
    //     if (!this._isDictChanged || !this._dictPath) return;
    //     this._isDictChanged = false; // Reset the flag
    //     if (typeof require !== 'function') return; // Not in an environment that supports require
    //     const fs = require('fs');
    //     if (!fs) return;

    //     let dictPath = this._dictPath;
    //     if (Utils.RPGMAKER_NAME === "MV") {
    //         dictPath = 'www/' + dictPath;
    //     }
    //     const backupDictPath = dictPath.replace(/(\.json)?$/, '_backup.json');
    //     let jsonString = JSON.stringify(this._dict, null, 2);
    //     try {
    //         fs.writeFileSync(backupDictPath, jsonString, 'utf8');
    //         if (fs.existsSync(dictPath)) {
    //             fs.unlinkSync(dictPath);
    //         }
    //         fs.renameSync(backupDictPath, dictPath);
    //     } catch (err) {
    //         console.error("Error saving file:", err);
    //         if (fs.existsSync(backupDictPath)) {
    //             fs.unlinkSync(backupDictPath);
    //         }
    //     }
    // };
    //#endregion

    //#region Data
    // TranslationManager.translateEventCommandText = function (command) {
    //     switch (command.code) {
    //         case 101: // first parameter is the text (character name?)
    //             command.parameters[0] = TranslationManager.translate(command.parameters[0]);
    //             break;
    //         case 401: // Only one parameter which is the text
    //             command.parameters[0] = TranslationManager.translate(command.parameters[0]);
    //             break;
    //         case 102: // The first parameter is a list of choices
    //             command.parameters[0] = command.parameters[0].map(choice => TranslationManager.translate(choice));
    //             break;
    //         case 402: // second parameter is the text of the choices
    //             command.parameters[1] = TranslationManager.translate(command.parameters[1]);
    //             break;
    //         case 405: // Only one parameter (text)
    //             command.parameters[0] = TranslationManager.translate(command.parameters[0]);
    //             break;
    //         case 108: // Comment
    //         case 408: // Conditional Branch (comment)
    //             command.parameters[0] = TranslationManager.translate(command.parameters[0], detectStartWhitespace = false, times = 1);
    //             break;
    //     }
    // };

    TranslationManager.translateEventCommandComment = function (command) {
        if (command.code === 108) {
            command.parameters[0] = TranslationManager.translate(command.parameters[0], detectStartWhitespace = false, times = 1);
        }
    };

    // TranslationManager.translateEventCommandTextOld = function (command) {
    //     if ([101, 401, 102, 402, 405].includes(command.code) && Array.isArray(command.parameters)) {
    //         command.parameters = command.parameters.map(param => {
    //             if (typeof param === 'string') {
    //                 return TranslationManager.translate(param);
    //             } else if (Array.isArray(param)) {
    //                 return param.map(p => typeof p === 'string' ? TranslationManager.translate(p) : p);
    //             }
    //             return param;
    //         });
    //     }
    // };

    TranslationManager.translateDataMap = function () {
        if ($dataMap) {
            if ($dataMap.displayName) {
                $dataMap.displayName = TranslationManager.translate($dataMap.displayName);
            }
            // if ($dataMap.events) {
            //     $dataMap.events.forEach(event => {
            //         if (event && event.pages) {
            //             event.pages.forEach(page => {
            //                 if (page.list) {
            //                     page.list.forEach(TranslationManager.translateEventCommandComment);
            //                 }
            //             });
            //         }
            //     });
            // }
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

            // Also variable and switch names
            if ($dataSystem.variables) {
                $dataSystem.variables.forEach((variable, i) => {
                    if (variable) {
                        $dataSystem.variables[i] = TranslationManager.translate(variable);
                    }
                });
            }
            if ($dataSystem.switches) {
                $dataSystem.switches.forEach((sw, i) => {
                    if (sw) {
                        $dataSystem.switches[i] = TranslationManager.translate(sw);
                    }
                }
                );
            }

            // Translate game title
            if ($dataSystem.gameTitle) {
                $dataSystem.gameTitle = TranslationManager.translate($dataSystem.gameTitle);
            }
        }

        // // Translate event text and choices in common events
        // if ($dataCommonEvents) {
        //     $dataCommonEvents.forEach(event => {
        //         if (event && event.list) {
        //             event.list.forEach(TranslationManager.translateEventCommandComment);
        //         }
        //     });
        // }
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

        // TranslationManager.saveDictionary();
    };

    window.TranslationManager = TranslationManager;
    //#endregion

    //#region Patches
    // Choices and other command window text
    const _Window_Command_addCommand = Window_Command.prototype.addCommand;
    Window_Command.prototype.addCommand = function (name, symbol, enabled = true, ext = null) {
        _Window_Command_addCommand.call(this, TranslationManager.translate(name), symbol, enabled, ext);
    };

    // Message window text
    const _Window_Message_startMessage = Window_Message.prototype.startMessage;
    Window_Message.prototype.startMessage = function () {
        for (let i = 0; i < $gameMessage._texts.length; i++) {
            $gameMessage._texts[i] = TranslationManager.translate($gameMessage._texts[i]);
        }
        _Window_Message_startMessage.call(this);
    };

    // Battle log text ??
    const _Window_BattleLog_addText = Window_BattleLog.prototype.addText;
    Window_BattleLog.prototype.addText = function (text) {
        _Window_BattleLog_addText.call(this, TranslationManager.translate(text));
    };

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

    // const _Window_Base_drawText = Window_Base.prototype.drawText;
    // Window_Base.prototype.drawText = function (text, x, y, maxWidth, align) {
    //     if (text) {
    //         return _Window_Base_drawText.call(this, TranslationManager.translate(text), x, y, maxWidth, align);
    //     } else {
    //         return _Window_Base_drawText.call(this, text, x, y, maxWidth, align);
    //     }
    // };

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
